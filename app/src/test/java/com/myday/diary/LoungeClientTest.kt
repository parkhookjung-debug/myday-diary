package com.myday.diary

import com.myday.diary.chat.*
import org.json.JSONObject
import org.junit.Test
import org.junit.Assert.*
import java.net.ServerSocket
import java.net.InetAddress
import java.io.DataInputStream
import java.util.concurrent.atomic.AtomicReference
import java.util.concurrent.atomic.AtomicBoolean
import kotlin.concurrent.thread

class LoungeClientTest {
    @Test fun addressValidationKeepsPlainHttpOnPrivateNetworks() {
        listOf("http://127.0.0.1:8765/","http://10.0.2.2:8765","http://192.168.1.5:8765/","http://172.16.2.3:8765/","https://example.org/").forEach {LoungeClient.parseAddress(it)}
        listOf("http://example.org/","http://172.32.0.1/","http://user:pass@127.0.0.1/","http://127.0.0.1/api","http://127.0.0.1/?secret=1","file:///tmp/a").forEach {value->
            try {LoungeClient.parseAddress(value);fail("Accepted $value")}catch(_:IllegalArgumentException){}catch(_:IllegalStateException){}
        }
    }
    @Test fun sessionSendPollCleanupAndResponseLimits() {
        val server=ServerSocket(0,8,InetAddress.getByName("127.0.0.1"))
        val sent=AtomicReference("");val left=AtomicBoolean(false);val mode=AtomicReference("normal")
        val failure=AtomicReference<Throwable?>(null)
        thread(isDaemon=true,name="myday-test-http") {
            while(!server.isClosed) {
                val socket=runCatching {server.accept()}.getOrNull() ?: break
                socket.use {
                    try {
                        it.soTimeout=5000
                        val input=it.getInputStream().buffered()
                        fun line(): String {val out=StringBuilder();while(true){val b=input.read();if(b<0 || b==10)break;if(b!=13)out.append(b.toChar())};return out.toString()}
                        val request=line().split(' ');val headers=mutableMapOf<String,String>()
                        while(true) {val row=line();if(row.isEmpty())break;headers[row.substringBefore(':').lowercase()]=row.substringAfter(':').trim()}
                        val bytes=ByteArray(headers["content-length"]?.toInt() ?: 0);DataInputStream(input).readFully(bytes)
                        val payload=String(bytes,Charsets.UTF_8);val path=request[1];val method=request[0]
                        if(path!="/api/sessions")assertEquals("Bearer test-token",headers["authorization"])
                        val response=when {
                            path=="/api/sessions"->{assertEquals("local-test-only",JSONObject(payload).getString("joinCode"));"""{"token":"test-token","nickname":"친구","room":"라운지","maxText":2000}"""}
                            method=="DELETE"->{left.set(true);"""{"ok":true}"""}
                            method=="POST"->{sent.set(JSONObject(payload).getString("text"));"""{"id":1,"nickname":"친구","text":${JSONObject.quote(sent.get())}}"""}
                            mode.get()=="large"->"x".repeat(ChatContract.ChatMaxResponseBytes+1)
                            else->"""{"messages":[{"id":1,"nickname":"친구","text":${JSONObject.quote(sent.get())}}],"users":["친구"],"cursor":1,"reset":false,"pollAfterMs":0}"""
                        }
                        val body=response.toByteArray(Charsets.UTF_8)
                        val head="HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: ${body.size}\r\nConnection: close\r\n\r\n"
                        runCatching {it.getOutputStream().apply {write(head.toByteArray());write(body);flush()}}
                    } catch(e: Throwable) {failure.set(e)}
                }
            }
        }
        try {
            val c=LoungeClient("http://127.0.0.1:${server.localPort}/")
            assertEquals("친구",c.join("친구","local-test-only"));c.send("세 기기에서 안녕 👋","a".repeat(32))
            val page=c.poll(0);assertEquals(sent.get(),page.messages.single().text);assertEquals(1L,page.cursor)
            mode.set("large");try{c.poll(1);fail("Unbounded response accepted")}catch(_:IllegalArgumentException){}
            c.leave();assertTrue(left.get());failure.get()?.let {throw it}
            try{c.poll(0);fail("Disconnected session used")}catch(_:java.util.concurrent.CancellationException){}
        }finally {server.close()}
    }
}
