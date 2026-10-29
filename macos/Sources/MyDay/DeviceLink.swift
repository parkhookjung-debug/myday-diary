import Foundation
import Network
import Security
import Darwin

enum DeviceAddress {
    static func privateIP(_ host: String)->Bool {
        let parts=host.split(separator:".",omittingEmptySubsequences:false)
        guard parts.count==4,parts.allSatisfy({!$0.isEmpty && $0.allSatisfy({$0.isASCII && $0.isNumber})}),parts.allSatisfy({Int($0).map{(0...255).contains($0)} ?? false}) else{return false}
        let b=parts.map{Int($0)!};return b[0]==127 || b[0]==10 || b[0]==192 && b[1]==168 || b[0]==172 && (16...31).contains(b[1])
    }
    static func parse(_ input: String)throws->URL {
        guard input.utf8.count<=256,let parts=URLComponents(string:input.trimmingCharacters(in:.whitespacesAndNewlines)),parts.scheme=="http",let host=parts.host,privateIP(host),parts.user==nil,parts.password==nil,parts.path=="/",parts.query==nil,let token=parts.percentEncodedFragment,token.range(of:"^[A-Za-z0-9_-]{43}$",options:.regularExpression) != nil,(parts.port==nil || (1...65535).contains(parts.port!)),let url=parts.url else{throw WindowsCompatibility.fail("같은 Wi-Fi의 MyDay 연결 주소를 입력해 주세요.")};return url
    }
    static func addresses()->[String] {
        var first:UnsafeMutablePointer<ifaddrs>?;guard getifaddrs(&first)==0 else{return []};defer{freeifaddrs(first)}
        var result=[String]();var node=first
        while let current=node {defer{node=current.pointee.ifa_next};guard let addr=current.pointee.ifa_addr,addr.pointee.sa_family==UInt8(AF_INET),current.pointee.ifa_flags & UInt32(IFF_UP) != 0,current.pointee.ifa_flags & UInt32(IFF_LOOPBACK)==0 else{continue}
            var buffer=[CChar](repeating:0,count:Int(NI_MAXHOST));if getnameinfo(addr,socklen_t(addr.pointee.sa_len),&buffer,socklen_t(buffer.count),nil,0,NI_NUMERICHOST)==0{let host=String(cString:buffer);if privateIP(host) && !result.contains(host){result.append(host)}}
        };return result
    }
    static func validate(_ data: Data)throws {guard data.count<=WindowsCompatibility.maxBytes else{throw CocoaError(.fileReadTooLarge)};_=try WindowsCompatibility.decode(data)}
    static func download(_ address: String)async throws->Data {
        let url=try parse(address);var parts=URLComponents(url:url,resolvingAgainstBaseURL:false)!;let token=parts.fragment!;parts.fragment=nil;parts.path="/download"
        let reader=DeviceDownload();return try await reader.load(parts.url!,token:token)
    }
}

private final class DeviceDownload:NSObject,URLSessionDataDelegate,URLSessionTaskDelegate {
    private var data=Data();private var result:CheckedContinuation<Data,Error>?;private var session:URLSession?
    func load(_ url: URL,token: String)async throws->Data {
        try await withCheckedThrowingContinuation {continuation in
            result=continuation;let config=URLSessionConfiguration.ephemeral;config.timeoutIntervalForRequest=30;config.timeoutIntervalForResource=120;config.httpCookieStorage=nil;config.connectionProxyDictionary=[:]
            let session=URLSession(configuration:config,delegate:self,delegateQueue:nil);self.session=session
            var request=URLRequest(url:url);request.setValue("Bearer "+token,forHTTPHeaderField:"Authorization");session.dataTask(with:request).resume()
        }
    }
    func urlSession(_ session:URLSession,task:URLSessionTask,willPerformHTTPRedirection response:HTTPURLResponse,newRequest request:URLRequest,completionHandler:@escaping(URLRequest?)->Void){completionHandler(nil)}
    func urlSession(_ session:URLSession,dataTask:URLSessionDataTask,didReceive response:URLResponse,completionHandler:@escaping(URLSession.ResponseDisposition)->Void) {
        guard (response as? HTTPURLResponse)?.statusCode==200,response.expectedContentLength<=Int64(WindowsCompatibility.maxBytes) else{completionHandler(.cancel);return};completionHandler(.allow)
    }
    func urlSession(_ session:URLSession,dataTask:URLSessionDataTask,didReceive bytes:Data) {if data.count+bytes.count>WindowsCompatibility.maxBytes{dataTask.cancel();return};data.append(bytes)}
    func urlSession(_ session:URLSession,task:URLSessionTask,didCompleteWithError error:Error?) {
        defer{session.finishTasksAndInvalidate();self.session=nil;result=nil}
        if let error{result?.resume(throwing:error);return}
        do{try DeviceAddress.validate(data);result?.resume(returning:data)}catch{result?.resume(throwing:error)}
    }
}

private final class DeviceStartGate: @unchecked Sendable {
    private let lock=NSLock()
    private var resumed=false
    func claim()->Bool {lock.lock();defer{lock.unlock()};guard !resumed else{return false};resumed=true;return true}
}

final class MacDeviceLink {
    private let listener:NWListener
    private let queue=DispatchQueue(label:"MyDay.device-link")
    private let lock=NSLock()
    private let snapshot:Data
    private let host:String
    private var connections:[UUID:NWConnection]=[:]
    private var uploaded:Data?
    private var closed=false
    private let opened=DispatchTime.now().uptimeNanoseconds
    private var origin=""
    let token:String
    private var expiry:DispatchSourceTimer?
    var invitation:String {lock.lock();defer{lock.unlock()};return origin+"/#"+token}
    var remaining:Int {max(0,JournalPolicy.transferLifetimeSeconds-Int((DispatchTime.now().uptimeNanoseconds-opened)/1_000_000_000))}
    var active:Bool {lock.lock();defer{lock.unlock()};return !closed && remaining>0}
    var incoming:Data? {lock.lock();defer{lock.unlock()};return uploaded}
    init(host:String,data:Data)throws {
        guard DeviceAddress.privateIP(host) else{throw WindowsCompatibility.fail("사설 IPv4 주소를 선택해 주세요.")};try DeviceAddress.validate(data);snapshot=data;self.host=host
        var random=[UInt8](repeating:0,count:32);guard SecRandomCopyBytes(kSecRandomDefault,32,&random)==errSecSuccess else{throw CocoaError(.coderInvalidValue)}
        token=Data(random).base64EncodedString().replacingOccurrences(of:"+",with:"-").replacingOccurrences(of:"/",with:"_").replacingOccurrences(of:"=",with:"")
        let parameters=NWParameters.tcp;parameters.requiredLocalEndpoint = .hostPort(host:NWEndpoint.Host(host),port:.any);listener=try NWListener(using:parameters)
        listener.newConnectionHandler = {[weak self]connection in self?.accept(connection)}
        let timer=DispatchSource.makeTimerSource(queue:queue);timer.schedule(deadline:.now()+Double(JournalPolicy.transferLifetimeSeconds));timer.setEventHandler{[weak self]in self?.close()};timer.resume();expiry=timer
    }
    func start()async throws->String {
        try await withCheckedThrowingContinuation{continuation in
            let gate=DeviceStartGate()
            listener.stateUpdateHandler = {[weak self]state in guard let self else{return};switch state {
                case .ready:if gate.claim(){let port=self.listener.port!.rawValue;self.lock.lock();self.origin="http://\(self.host):\(port)";self.lock.unlock();continuation.resume(returning:self.invitation)}
                case .failed(let error):if gate.claim(){continuation.resume(throwing:error)};self.close()
                case .cancelled:if gate.claim(){continuation.resume(throwing:CancellationError())}
                default:break
            }}
            listener.start(queue:queue)
        }
    }
    private func accept(_ connection:NWConnection) {
        let id=UUID();lock.lock();if closed || !connections.isEmpty {lock.unlock();connection.cancel();return};connections[id]=connection;lock.unlock()
        connection.start(queue:queue);queue.asyncAfter(deadline:.now()+120){[weak self]in self?.finish(id)}
        var buffer=Data();var responded=false
        func read(){connection.receive(minimumIncompleteLength:1,maximumLength:8192){[weak self]data,_,complete,error in
            guard let self,!responded else{return};if let data{buffer.append(data)}
            if buffer.count>WindowsCompatibility.maxBytes+16384 {responded=true;self.reply(id,413,text:"백업은 128MB 이하로 보내 주세요.");return}
            guard let split=buffer.range(of:Data([13,10,13,10])) else{if buffer.count>16384 || complete || error != nil {responded=true;self.reply(id,400,text:"요청을 확인해 주세요.")}else{read()};return}
            if split.upperBound>16384 {responded=true;self.reply(id,431,text:"요청이 너무 커요.");return}
            let header=String(decoding:buffer[..<split.lowerBound],as:UTF8.self).components(separatedBy:"\r\n");let first=header[0].split(separator:" ").map(String.init)
            guard first.count==3 else{responded=true;self.reply(id,400,text:"요청을 확인해 주세요.");return}
            var headers=[String:String]();for line in header.dropFirst(){guard let colon=line.firstIndex(of:":") else{responded=true;self.reply(id,400,text:"요청을 확인해 주세요.");return};let key=line[..<colon].lowercased();guard headers[key]==nil else{responded=true;self.reply(id,400,text:"요청을 확인해 주세요.");return};headers[key]=line[line.index(after:colon)...].trimmingCharacters(in:.whitespaces)}
            guard self.active else{responded=true;self.reply(id,410,text:"연결 시간이 끝났어요.");return}
            guard headers["host"]==self.origin.replacingOccurrences(of:"http://",with:""),headers["origin"]==nil || headers["origin"]==self.origin else{responded=true;self.reply(id,403,text:"이 연결에서는 사용할 수 없어요.");return}
            if first[0]=="GET" && first[1]=="/"{responded=true;let page=(Bundle.main.url(forResource:"device-link",withExtension:"html") ?? Bundle.module.url(forResource:"device-link",withExtension:"html"))!;self.reply(id,200,data:(try? Data(contentsOf:page)) ?? Data(),type:"text/html; charset=utf-8");return}
            let given=Array((headers["authorization"] ?? "").utf8),expected=Array(("Bearer "+self.token).utf8);var difference=given.count ^ expected.count;for i in 0..<min(given.count,expected.count){difference |= Int(given[i]^expected[i])}
            guard difference==0 else{responded=true;self.reply(id,401,text:"연결 주소를 다시 확인해 주세요.");return}
            if first[0]=="GET" && first[1]=="/download"{responded=true;self.reply(id,200,data:self.snapshot,type:"application/json",download:true);return}
            guard first[0]=="POST",first[1]=="/upload" else{responded=true;self.reply(id,404,text:"요청을 찾지 못했어요.");return}
            guard headers["transfer-encoding"]==nil,headers["content-type"]?.components(separatedBy:";").first=="application/json",let text=headers["content-length"],let length=Int(text),(1...WindowsCompatibility.maxBytes).contains(length) else{responded=true;self.reply(id,413,text:"공통 백업 JSON은 128MB 이하로 보내 주세요.");return}
            if buffer.count-split.upperBound<length {if complete || error != nil{responded=true;self.reply(id,400,text:"전송이 끊겼어요.")}else{read()};return}
            responded=true;let bytes=Data(buffer[split.upperBound..<split.upperBound+length])
            do{try DeviceAddress.validate(bytes);self.lock.lock();if self.uploaded != nil || self.closed{self.lock.unlock();self.reply(id,409,text:"이미 받은 기록이 있어요. 새 연결을 열어 주세요.");return};self.uploaded=bytes;self.lock.unlock();self.reply(id,200,text:"앱에서 도착한 기록을 확인해 주세요.")}catch{self.reply(id,400,text:"올바른 공통 백업인지 확인해 주세요.")}
        }};read()
    }
    private func reply(_ id:UUID,_ code:Int,text:String){reply(id,code,data:Data(text.utf8),type:"text/plain; charset=utf-8")}
    private func reply(_ id:UUID,_ code:Int,data:Data,type:String,download:Bool=false) {
        lock.lock();let connection=connections[id];lock.unlock();guard let connection else{return}
        let header="HTTP/1.1 \(code) Result\r\nContent-Type: \(type)\r\nContent-Length: \(data.count)\r\nCache-Control: no-store\r\nReferrer-Policy: no-referrer\r\nX-Content-Type-Options: nosniff\r\nContent-Security-Policy: default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'\r\nConnection: close\r\n"+(download ? "Content-Disposition: attachment; filename=\"MyDay-transfer.json\"\r\n" : "")+"\r\n"
        connection.send(content:Data(header.utf8)+data,completion:.contentProcessed{[weak self]_ in self?.finish(id)})
    }
    private func finish(_ id:UUID){lock.lock();let connection=connections.removeValue(forKey:id);lock.unlock();connection?.cancel()}
    func close(){lock.lock();if closed{lock.unlock();return};closed=true;let clients=Array(connections.values);connections.removeAll();uploaded=nil;lock.unlock();listener.cancel();clients.forEach{$0.cancel()};expiry?.cancel();expiry=nil}
    deinit{close()}
}
