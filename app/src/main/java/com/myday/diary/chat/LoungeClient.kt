package com.myday.diary.chat

import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URI
import java.io.ByteArrayOutputStream

data class LoungeMessage(val id: Long,val nickname: String,val text: String)
data class LoungePage(val messages: List<LoungeMessage>,val users: List<String>,val cursor: Long,val reset: Boolean,val delay: Long)
class LoungeFailure(val status: Int,message: String): Exception(message)

/** Only chat requests. This class has no access to diaries, images or stored credentials. */
class LoungeClient(address: String) {
    companion object {
        fun parseAddress(value: String): URI {
            val uri=runCatching {URI(value)}.getOrElse {error("서버 주소를 확인해 주세요.")}
            val host=uri.host?.lowercase() ?: error("서버 주소를 확인해 주세요.")
            require(uri.scheme in listOf("http","https") && uri.userInfo==null && uri.query==null && uri.fragment==null && uri.path in listOf("","/") && uri.port in -1..65535) {"서버의 기본 주소만 입력해 주세요."}
            val n=host.split('.').mapNotNull(String::toIntOrNull)
            val local=host in listOf("localhost","::1","[::1]") || n.size==4 && n.all {it in 0..255} && (n[0]==127 || n[0]==10 || n[0]==192 && n[1]==168 || n[0]==172 && n[1] in 16..31)
            require(uri.scheme=="https" || local) {"HTTP는 로컬·사설 IP에서만 사용할 수 있어요."}
            return uri
        }
    }
    private val base=parseAddress(address)
    @Volatile private var token: String?=null
    @Volatile private var active: HttpURLConnection?=null
    @Volatile private var stopped=false
    private fun request(path: String,method: String="GET",body: JSONObject?=null,cleanup: Boolean=false): JSONObject {
        if(stopped && !cleanup)throw java.util.concurrent.CancellationException()
        val c=base.resolve(path).toURL().openConnection() as HttpURLConnection
        if(!cleanup)active=c
        try {
            c.instanceFollowRedirects=false;c.connectTimeout=if(cleanup)2000 else 8000;c.readTimeout=if(cleanup)2000 else 25000;c.requestMethod=method;c.useCaches=false
            c.setRequestProperty("Accept","application/json");token?.let {c.setRequestProperty("Authorization","Bearer $it")}
            if(body!=null) {c.doOutput=true;c.setRequestProperty("Content-Type","application/json; charset=utf-8");c.outputStream.use {it.write(body.toString().toByteArray(Charsets.UTF_8))}}
            val status=c.responseCode
            val raw=(if(status in 200..299)c.inputStream else c.errorStream)?.use {input->val out=ByteArrayOutputStream();val buffer=ByteArray(4096)
                while(true) {val count=input.read(buffer);if(count<0)break;require(out.size()+count<=ChatContract.ChatMaxResponseBytes) {"서버 응답이 너무 커요."};out.write(buffer,0,count)};out.toString("UTF-8")} ?: "{}"
            if(status !in 200..299)throw LoungeFailure(status,runCatching {JSONObject(raw).optString("error").take(500)}.getOrDefault("").ifBlank {"서버 연결 실패 ($status)"})
            require(c.contentType?.lowercase()?.startsWith("application/json")==true) {"채팅 서버 응답이 아니에요."}
            if(stopped && !cleanup)throw java.util.concurrent.CancellationException()
            return JSONObject(raw)
        } finally {c.disconnect();if(active===c)active=null}
    }
    fun join(nickname: String,code: String): String {
        require(nickname.codePointCount(0,nickname.length) in 2..ChatContract.ChatNicknameLimit && code.length in 1..100) {"닉네임과 초대코드를 확인해 주세요."}
        val o=request("/api/sessions","POST",JSONObject().put("nickname",nickname).put("joinCode",code))
        val t=o.getString("token");require(t.length in 1..ChatContract.ChatTokenLimit);token=t
        require(o.getInt("maxText")==ChatContract.ChatMaxText)
        return o.getString("nickname").also {require(it.codePointCount(0,it.length) in 1..ChatContract.ChatNicknameLimit)}
    }
    private fun message(o: JSONObject): LoungeMessage {
        val m=LoungeMessage(o.getLong("id"),o.getString("nickname"),o.getString("text"))
        require(m.id>0 && m.nickname.codePointCount(0,m.nickname.length)<=ChatContract.ChatNicknameLimit && m.text.length<=ChatContract.ChatMaxTextUtf16)
        return m
    }
    fun poll(cursor: Long): LoungePage {
        val o=request("/api/messages?after=$cursor&wait=20");val a=o.getJSONArray("messages");val u=o.getJSONArray("users");val delay=o.optLong("pollAfterMs",0)
        require(a.length()<=ChatContract.ChatMaxPage && u.length()<=ChatContract.ChatMaxUsers && delay in 0..ChatContract.ChatMaxPollDelayMs.toLong())
        return LoungePage((0 until a.length()).map {message(a.getJSONObject(it))},(0 until u.length()).map {u.getString(it).also {name->require(name.codePointCount(0,name.length)<=ChatContract.ChatNicknameLimit)}},o.getLong("cursor").also {require(it>=0)},o.getBoolean("reset"),delay)
    }
    fun send(text: String,id: String) {require(text.isNotBlank() && text.codePointCount(0,text.length)<=ChatContract.ChatMaxText);message(request("/api/messages","POST",JSONObject().put("text",text).put("clientId",id)))}
    fun stop() {stopped=true;active?.disconnect()}
    fun leave() {stopped=true;if(token!=null)runCatching {request("/api/session","DELETE",cleanup=true)};token=null}
}
