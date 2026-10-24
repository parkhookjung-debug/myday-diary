package com.myday.diary.device

import com.myday.diary.data.*
import java.net.*
import java.io.*
import java.security.SecureRandom
import java.security.MessageDigest
import java.util.Base64
import java.util.concurrent.Executors
import java.util.concurrent.TimeUnit

object DeviceAddress {
    fun privateIp(host: String): Boolean {
        if(!host.matches(Regex("\\d{1,3}(\\.\\d{1,3}){3}")))return false
        val b=host.split('.').map {it.toInt()};if(b.any {it !in 0..255})return false
        return b[0]==127 || b[0]==10 || b[0]==192 && b[1]==168 || b[0]==172 && b[1] in 16..31
    }
    fun parse(input: String): URI {
        require(input.length<=256);val uri=URI(input.trim())
        require(uri.scheme=="http" && uri.host!=null && privateIp(uri.host) && uri.rawUserInfo==null && uri.rawPath=="/" && uri.rawQuery==null && uri.rawFragment?.matches(Regex("[A-Za-z0-9_-]{43}"))==true && (uri.port==-1 || uri.port in 1..65535)) {"같은 Wi-Fi의 MyDay 연결 주소를 입력해 주세요."}
        return uri
    }
    fun addresses(): List<String> = NetworkInterface.getNetworkInterfaces().toList().filter {it.isUp && !it.isLoopback}.flatMap {it.inetAddresses.toList()}.map {it.hostAddress ?: ""}.filter {privateIp(it) && !it.startsWith("127.")}.distinct()
    fun validate(bytes: ByteArray): DiaryBook {require(bytes.size<=NotebookJson.MAX_BYTES);return NotebookJson.decode(String(bytes,Charsets.UTF_8)).also(NotebookPhoto::validate)}
    fun download(address: String): ByteArray {
        val uri=parse(address);val url=URI("http",null,uri.host,uri.port,"/download",null,null).toURL();val connection=url.openConnection(Proxy.NO_PROXY) as HttpURLConnection
        try {connection.instanceFollowRedirects=false;connection.connectTimeout=30000;connection.readTimeout=30000;connection.setRequestProperty("Authorization","Bearer ${uri.rawFragment}")
            require(connection.responseCode==200 && connection.contentLengthLong<=NotebookJson.MAX_BYTES) {"연결 시간과 상대 기기를 확인해 주세요."}
            return connection.inputStream.use {readBounded(it,NotebookJson.MAX_BYTES)}.also(::validate)
        }finally {connection.disconnect()}
    }
    fun readBounded(input: InputStream,max: Int): ByteArray {val output=ByteArrayOutputStream();val buffer=ByteArray(8192);while(true){val n=input.read(buffer);if(n<0)break;require(output.size()+n<=max);output.write(buffer,0,n)};return output.toByteArray()}
}

class DeviceLinkServer(host: String,private val snapshot: ByteArray,private val html: ByteArray): AutoCloseable {
    private val gate=Any();private val listener: ServerSocket;private val worker=Executors.newFixedThreadPool(1)
    private val expiry=Executors.newSingleThreadScheduledExecutor();private val opened=System.nanoTime();private val clients=mutableSetOf<Socket>()
    @Volatile private var closed=false
    @Volatile var incoming: ByteArray?=null;private set
    val origin: String
    val token=Base64.getUrlEncoder().withoutPadding().encodeToString(ByteArray(32).also {SecureRandom().nextBytes(it)})
    val invitation get()="$origin/#$token"
    val remaining get()=maxOf(0,JournalPolicy.transferLifetimeSeconds-((System.nanoTime()-opened)/1000000000L).toInt())
    val active get()=!closed && remaining>0
    init {
        require(DeviceAddress.privateIp(host));DeviceAddress.validate(snapshot)
        listener=ServerSocket(0,3,InetAddress.getByName(host));origin="http://$host:${listener.localPort}"
        Thread({while(active) {try {val socket=listener.accept();synchronized(gate){if(closed || clients.isNotEmpty()){socket.close();return@synchronized};clients.add(socket);worker.execute {handle(socket)}}}catch(_:IOException){break}}},"MyDay device link").apply {isDaemon=true;start()}
        expiry.schedule({close()},JournalPolicy.transferLifetimeSeconds.toLong(),TimeUnit.SECONDS)
    }
    private fun handle(socket: Socket) {
        try {socket.soTimeout=30000;val input=socket.getInputStream();val output=socket.getOutputStream();val header=ByteArrayOutputStream();var tail=0
            while(true){val ch=input.read();require(ch>=0);header.write(ch);require(header.size()<=16384);tail=(tail shl 8) or ch;if(tail==0x0d0a0d0a)break}
            val lines=header.toString("US-ASCII").split("\r\n");val first=lines.first().split(' ');require(first.size==3)
            val headers=mutableMapOf<String,String>();for(line in lines.drop(1).filter {it.isNotEmpty()}){val index=line.indexOf(':');require(index>0);val key=line.substring(0,index).lowercase();require(key !in headers);headers[key]=line.substring(index+1).trim()}
            if(!active){reply(output,410,"연결 시간이 끝났어요.");return}
            if(headers["host"]!=origin.removePrefix("http://") || headers["origin"]?.let {it!=origin}==true){reply(output,403,"이 연결에서는 사용할 수 없어요.");return}
            if(first[0]=="GET" && first[1]=="/"){reply(output,200,html,"text/html; charset=utf-8");return}
            if(!MessageDigest.isEqual((headers["authorization"] ?: "").toByteArray(),"Bearer $token".toByteArray())){reply(output,401,"연결 주소를 다시 확인해 주세요.");return}
            if(first[0]=="GET" && first[1]=="/download"){reply(output,200,snapshot,"application/json",true);return}
            if(first[0]!="POST" || first[1]!="/upload"){reply(output,404,"요청을 찾지 못했어요.");return}
            val length=headers["content-length"]?.toIntOrNull() ?: -1
            if(length !in 1..NotebookJson.MAX_BYTES || headers["transfer-encoding"]!=null || headers["content-type"]?.substringBefore(';')!="application/json"){reply(output,413,"공통 백업 JSON은 128MB 이하로 보내 주세요.");return}
            val bytes=ByteArray(length);var offset=0;while(offset<length){val n=input.read(bytes,offset,minOf(8192,length-offset));require(n>0);offset+=n};DeviceAddress.validate(bytes)
            synchronized(gate) {if(closed){reply(output,410,"연결이 닫혔어요.");return};if(incoming!=null){reply(output,409,"이미 받은 기록이 있어요. 새 연결을 열어 주세요.");return};incoming=bytes};reply(output,200,"앱에서 도착한 기록을 확인해 주세요.")
        }catch(_:Exception){runCatching {reply(socket.getOutputStream(),400,"올바른 공통 백업인지 확인해 주세요.")}}finally {synchronized(gate){clients.remove(socket)};socket.close()}
    }
    private fun reply(output: OutputStream,status: Int,text: String)=reply(output,status,text.toByteArray(Charsets.UTF_8),"text/plain; charset=utf-8")
    private fun reply(output: OutputStream,status: Int,bytes: ByteArray,type: String,download: Boolean=false) {val header="HTTP/1.1 $status Result\r\nContent-Type: $type\r\nContent-Length: ${bytes.size}\r\nCache-Control: no-store\r\nReferrer-Policy: no-referrer\r\nX-Content-Type-Options: nosniff\r\nContent-Security-Policy: default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'\r\nConnection: close\r\n"+(if(download) "Content-Disposition: attachment; filename=\"MyDay-transfer.json\"\r\n" else "")+"\r\n";output.write(header.toByteArray(Charsets.US_ASCII));output.write(bytes);output.flush()}
    override fun close() {synchronized(gate){if(closed)return;closed=true;listener.close();clients.forEach {it.close()};incoming=null};worker.shutdownNow();expiry.shutdownNow()}
}
