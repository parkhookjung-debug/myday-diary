import Foundation
import SwiftUI

@MainActor
enum LoungeVerification {
    static func check(_ value: Bool, _ label: String) throws {
        if !value { throw LoungeFailure(status: 0, message: "검사 실패: \(label)") }
    }
    static func addresses() throws {
        for address in ["https://chat.example.com/", "http://127.0.0.1:8765/", "http://192.168.0.10:8765/"] {
            _ = try LoungeClient.parseAddress(address)
        }
        for address in ["http://example.com/", "https://user:secret@example.com/", "https://example.com/path", "https://example.com/?token=x", "file:///tmp/chat"] {
            do { _ = try LoungeClient.parseAddress(address) }
            catch { continue }
            throw LoungeFailure(status: 0, message: "허용하면 안 되는 주소: \(address)")
        }
    }
    static func run(address: String, code: String) async throws {
        try addresses()
        let a = try LoungeClient(address: address), b = try LoungeClient(address: address)
        var failure: Error?
        do {
            let joined = try await a.join(nickname: "맥 연결검사 가", code: code)
            _ = try await b.join(nickname: "맥 연결검사 나", code: code)
            try check(joined.nickname == "맥 연결검사 가", "native session")
            let nonce = UUID().uuidString.replacingOccurrences(of: "-", with: "").lowercased()
            let sent = try await a.send(text: "Windows와 함께 🌱\r\n맥에서도 기록해요", id: nonce)
            let retried = try await a.send(text: "Windows와 함께 🌱\r\n맥에서도 기록해요", id: nonce)
            try check(sent.id == retried.id, "idempotent send")
            let page = try await b.poll(after: 0, wait: 0)
            try check(page.messages.filter { $0.clientId == nonce }.count == 1, "shared history")
            try check(page.messages.contains { $0.text == "Windows와 함께 🌱\n맥에서도 기록해요" }, "Unicode and CRLF")
            try check(page.users.count == 2, "presence")
            let rejected = try LoungeClient(address: address)
            do {
                _ = try await rejected.join(nickname: "잘못된 코드", code: "wrong-code")
                throw LoungeFailure(status: 0, message: "Invalid invitation accepted")
            } catch let error as LoungeFailure { try check(error.status == 403, "invalid invitation") }
            await rejected.leave()
            let duplicate = try LoungeClient(address: address)
            do {
                _ = try await duplicate.join(nickname: joined.nickname, code: code)
                throw LoungeFailure(status: 0, message: "Duplicate nickname accepted")
            } catch let error as LoungeFailure { try check(error.status == 409, "duplicate nickname") }
            await duplicate.leave()
            let task = Task { try await b.poll(after: page.cursor, wait: 20) }
            task.cancel()
            do { _ = try await task.value; throw LoungeFailure(status: 0, message: "Cancelled request completed") }
            catch is CancellationError { }
            await b.leave()
            do {
                _ = try await b.poll(after: 0, wait: 0)
                throw LoungeFailure(status: 0, message: "Logged out session accepted")
            } catch let error as LoungeFailure { try check(error.status == 401, "logout") }
        } catch { failure = error }
        await a.leave(); await b.leave()
        if let failure { throw failure }
        print("macOS native chat contract passed: sessions, Unicode, retry, presence, cancellation, logout")
    }
    static func render() throws {
        let model = LoungeModel()
        model.nickname = "상몬 기록가"; model.connected = true
        model.users = ["상몬 기록가", "Windows 친구"]
        model.status = "같은 채팅방에서 만났어요. ⌘ + Enter로 전송해요."
        model.messages = [
            LoungeMessage(id: 1, nickname: "Windows 친구", text: "윈도우에서 인사해! 🌷", sentAt: "2026-10-17T00:00:00Z", clientId: "a"),
            LoungeMessage(id: 2, nickname: "상몬 기록가", text: "맥에서도 같은 이야기가 보여!", sentAt: "2026-10-17T00:00:01Z", clientId: "b")
        ]
        try MacVerification.render(LoungeView(model: model), name: "chat", width: 920, height: 720)
        try MacVerification.render(LoungeView(model: model), name: "chat-compact", width: 800, height: 640)
    }
}
