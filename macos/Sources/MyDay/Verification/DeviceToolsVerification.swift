import Foundation

private final class DeviceTestResult<T> {
    let semaphore=DispatchSemaphore(value:0)
    private let lock=NSLock();private var value:Result<T,Error>?
    func finish(_ result:Result<T,Error>){lock.lock();value=result;lock.unlock();semaphore.signal()}
    func read()throws->T {guard semaphore.wait(timeout:.now()+20) == .success else{throw WindowsCompatibility.fail("Native device transfer timed out")};lock.lock();defer{lock.unlock()};return try value!.get()}
}

@MainActor enum DeviceToolsVerification {
    static func run(root:URL)throws {
        let token=String(repeating:"a",count:43)
        for host in ["10.0.0.1","127.0.0.1","192.168.0.1","172.16.0.1"]{try MacVerification.check(DeviceAddress.privateIP(host),"native private IP policy")}
        for text in ["http://8.8.8.8/#"+token,"http://localhost/#"+token,"https://192.168.0.1/#"+token,"http://user@192.168.0.1/#"+token,"http://192.168.0.1/path#"+token]{do{_=try DeviceAddress.parse(text);throw MacVerification.Failure.check("bad device invitation accepted")}catch is CompatibilityError{}}
        let data=try CompatibilityVerification.data(CompatibilityVerification.fixture())
        let server=try MacDeviceLink(host:"127.0.0.1",data:data);defer{server.close()}
        let started=DeviceTestResult<String>();Task.detached{do{started.finish(.success(try await server.start()))}catch{started.finish(.failure(error))}}
        let invitation=try started.read()
        let downloaded=DeviceTestResult<Data>();Task.detached{do{downloaded.finish(.success(try await DeviceAddress.download(invitation)))}catch{downloaded.finish(.failure(error))}}
        let received=try downloaded.read();try MacVerification.check(received==data,"native app device transfer preserves complete backup and photo bytes")
        let upload=DeviceTestResult<Int>();Task.detached{do{var parts=URLComponents(string:invitation)!;parts.fragment=nil;parts.path="/upload";var request=URLRequest(url:parts.url!);request.httpMethod="POST";request.setValue("application/json",forHTTPHeaderField:"Content-Type");request.setValue("Bearer "+server.token,forHTTPHeaderField:"Authorization");request.httpBody=data;let (_,response)=try await URLSession.shared.data(for:request);upload.finish(.success((response as! HTTPURLResponse).statusCode))}catch{upload.finish(.failure(error))}}
        let status=try upload.read();try MacVerification.check(status==200 && server.incoming==data,"native received backup stays pending until explicit import")
        server.close();try MacVerification.check(!server.active && server.incoming==nil,"closing device link revokes access")
        let dir=root.appendingPathComponent("automatic-test")
        for day in 1...5{try MacVerification.check(AutomaticBackup.capture(dir,data:{data},day:String(format:"2030-01-%02d",day)),"daily automatic backup saved")}
        var copies=try AutomaticBackup.files(dir);try MacVerification.check(copies.count==3 && copies.last?.lastPathComponent=="daily-2030-01-03.json","automatic backup keeps newest three days")
        let unrelated=AutomaticBackup.folder(dir).appendingPathComponent("my-copy.json");try Data("keep".utf8).write(to:unrelated)
        _=AutomaticBackup.capture(dir,data:{data},day:"2030-01-06");copies=try AutomaticBackup.files(dir)
        try MacVerification.check(copies.count==3 && FileManager.default.fileExists(atPath:unrelated.path),"automatic retention ignores unrelated files")
        try AutomaticBackup.setEnabled(dir,false);try MacVerification.check(!AutomaticBackup.capture(dir,data:{data},day:"2030-01-07"),"automatic backup off keeps copies")
        let blocked=root.appendingPathComponent("automatic-blocked");try FileManager.default.createDirectory(at:blocked,withIntermediateDirectories:true);try Data("blocked".utf8).write(to:AutomaticBackup.folder(blocked))
        try MacVerification.check(!AutomaticBackup.capture(blocked,data:{data}) && !AutomaticBackup.state(blocked).error.isEmpty,"backup failures are separate from normal record saving")
        print("Native device link and bounded automatic backups passed")
    }
}
