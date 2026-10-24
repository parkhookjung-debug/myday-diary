import Foundation

struct LoungeSession: Decodable { let token: String; let nickname: String; let room: String; let maxText: Int }
struct LoungeMessage: Decodable, Identifiable {
    let id: Int64; let nickname: String; let text: String; let sentAt: String; let clientId: String
}
struct LoungePage: Decodable {
    let messages: [LoungeMessage]; let users: [String]; let cursor: Int64; let reset: Bool
    let pollAfterMs: Int?
}
struct LoungeFailure: LocalizedError {
    let status: Int; let message: String
    var errorDescription: String? { message }
}
private struct LoungeErrorBody: Decodable { let error: String }
private final class LoungeRedirectBlocker: NSObject, URLSessionTaskDelegate, @unchecked Sendable {
    func urlSession(_ session: URLSession, task: URLSessionTask, willPerformHTTPRedirection response: HTTPURLResponse,
                    newRequest request: URLRequest, completionHandler: @escaping (URLRequest?) -> Void) {
        completionHandler(nil)
    }
}

// The transport has no access to diary files, photos, backup data or the journal store.
actor LoungeClient {
    let address: URL
    private var token: String?
    private let session: URLSession

    static func parseAddress(_ value: String) throws -> URL {
        guard let parts = URLComponents(string: value), let url = parts.url,
              let scheme = parts.scheme?.lowercased(), ["https", "http"].contains(scheme),
              let host = parts.host?.lowercased(), !host.isEmpty,
              parts.user == nil, parts.password == nil, parts.query == nil, parts.fragment == nil,
              parts.path == "" || parts.path == "/" else {
            throw LoungeFailure(status: 0, message: "서버 주소는 https://주소/ 형식으로 입력해 주세요.")
        }
        let numbers = host.split(separator: ".", omittingEmptySubsequences: false).compactMap { Int($0) }
        let ipv4 = numbers.count == 4 && numbers.allSatisfy { (0...255).contains($0) }
        let local = ["localhost", "::1", "[::1]"].contains(host) || (ipv4 && (
            numbers[0] == 127 || numbers[0] == 10 || numbers[0] == 192 && numbers[1] == 168
            || numbers[0] == 172 && (16...31).contains(numbers[1])))
        guard scheme == "https" || local else {
            throw LoungeFailure(status: 0, message: "인터넷 서버에는 HTTPS가 필요해요. HTTP는 로컬·사설 IP 테스트용이에요.")
        }
        return url
    }
    init(address: String) throws {
        self.address = try Self.parseAddress(address)
        let configuration = URLSessionConfiguration.ephemeral
        configuration.timeoutIntervalForRequest = 30
        configuration.timeoutIntervalForResource = 35
        configuration.httpShouldSetCookies = false
        configuration.urlCache = nil
        session = URLSession(configuration: configuration, delegate: LoungeRedirectBlocker(), delegateQueue: nil)
    }
    deinit { session.invalidateAndCancel() }
    private func request<T: Decodable>(_ path: String, method: String = "GET", body: [String: String]? = nil) async throws -> T {
        try Task.checkCancellation()
        guard let url = URL(string: path, relativeTo: address)?.absoluteURL else {
            throw LoungeFailure(status: 0, message: "서버 주소가 올바르지 않아요.")
        }
        var request = URLRequest(url: url)
        request.httpMethod = method
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        if let token { request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization") }
        if let body {
            request.setValue("application/json; charset=utf-8", forHTTPHeaderField: "Content-Type")
            request.httpBody = try JSONEncoder().encode(body)
        }
        let (bytes, response) = try await session.bytes(for: request)
        guard let http = response as? HTTPURLResponse else {
            throw LoungeFailure(status: 0, message: "서버 응답을 확인할 수 없어요.")
        }
        var data = Data()
        for try await byte in bytes {
            if data.count >= DesktopContract.chatMaxResponseBytes { throw LoungeFailure(status: 0, message: "서버 응답이 너무 커요.") }
            data.append(byte)
        }
        try Task.checkCancellation()
        guard (200...299).contains(http.statusCode) else {
            let error = try? JSONDecoder().decode(LoungeErrorBody.self, from: data)
            let message = error.flatMap { $0.error.count <= 500 ? $0.error : nil }
                ?? "서버에 연결하지 못했어요 (\(http.statusCode))."
            throw LoungeFailure(status: http.statusCode, message: message)
        }
        guard http.value(forHTTPHeaderField: "Content-Type")?.lowercased().hasPrefix("application/json") == true else {
            throw LoungeFailure(status: 0, message: "채팅 API 응답이 아니에요. 서버 주소를 확인해 주세요.")
        }
        return try JSONDecoder().decode(T.self, from: data)
    }
    func join(nickname: String, code: String) async throws -> LoungeSession {
        let joined: LoungeSession = try await request("/api/sessions", method: "POST", body: ["nickname": nickname, "joinCode": code])
        // Keep the token before validation so cleanup can leave even if a malformed reply is rejected.
        if !joined.token.isEmpty && joined.token.utf8.count <= DesktopContract.chatTokenLimit { token = joined.token }
        guard token != nil, !joined.nickname.isEmpty, joined.nickname.unicodeScalars.count <= DesktopContract.chatNicknameLimit, joined.maxText == DesktopContract.chatMaxText else {
            throw LoungeFailure(status: 0, message: "입장 응답이 올바르지 않아요.")
        }
        return joined
    }
    private func validate(_ message: LoungeMessage) throws {
        guard message.id > 0, message.nickname.unicodeScalars.count <= DesktopContract.chatNicknameLimit, message.text.utf16.count <= DesktopContract.chatMaxTextUtf16,
              message.sentAt.utf8.count <= 40 else { throw LoungeFailure(status: 0, message: "메시지 형식이 올바르지 않아요.") }
    }
    func poll(after: Int64, wait: Int = 20) async throws -> LoungePage {
        let page: LoungePage = try await request("/api/messages?after=\(after)&wait=\(wait)")
        guard page.messages.count <= DesktopContract.chatMaxPage, page.users.count <= DesktopContract.chatMaxUsers, page.cursor >= 0,
              (0...DesktopContract.chatMaxPollDelayMs).contains(page.pollAfterMs ?? 0), page.users.allSatisfy({ $0.unicodeScalars.count <= DesktopContract.chatNicknameLimit }) else {
            throw LoungeFailure(status: 0, message: "메시지 목록이 올바르지 않아요.")
        }
        for message in page.messages { try validate(message) }
        return page
    }
    func send(text: String, id: String) async throws -> LoungeMessage {
        let sent: LoungeMessage = try await request("/api/messages", method: "POST", body: ["text": text, "clientId": id])
        try validate(sent)
        return sent
    }
    func leave() async {
        guard token != nil else { return }
        struct OK: Decodable { let ok: Bool }
        // Cleanup runs in an uncancelled task, with its own three-second deadline.
        _ = await Task.detached { [self] in
            await withTaskGroup(of: Void.self) { group in
                group.addTask { let _: OK? = try? await self.request("/api/session", method: "DELETE") }
                group.addTask { try? await Task.sleep(nanoseconds: 3_000_000_000) }
                await group.next()
                group.cancelAll()
            }
        }.value
        token = nil
    }
}
