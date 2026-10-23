package com.myday.diary

import com.myday.diary.chat.*
import com.sun.net.httpserver.HttpServer
import org.json.JSONObject
import org.junit.Test
import org.junit.Assert.*
import java.net.InetSocketAddress

class LoungeClientTest {
    @Test fun addressValidationKeepsPlainHttpOnPrivateNetworks() {
        listOf("http://127.0.0.1:8765/","http://10.0.2.2:8765","http://192.168.1.5:8765/","http://172.16.2.3:8765/","https://example.org/").forEach {LoungeClient.parseAddress(it)}
        listOf("http://example.org/","http://172.32.0.1/","http://user:pass@127.0.0.1/","http://127.0.0.1/api","http://127.0.0.1/?secret=1","file:///tmp/a").forEach {value->
            try {LoungeClient.parseAddress(value);fail("Accepted $value")}catch(_:IllegalArgumentException){}catch(_:IllegalStateException){}
        }
    }
    @Test fun sessionSendPollCleanupAndResponseLimits() {
        val server=HttpServer.create(InetSocketAddress("127.0.0.1",0),0)
        var sent="";var left=false;var mode="normal"
        server.createContext("/api/") {e->
            val payload=e.requestBody.bufferedReader().use {it.readText()}
            if(e.requestURI.path!="/api/sessions")assertEquals("Bearer test-token",e.requestHeaders.getFirst("Authorization"))
            val response=when {
                e.requestURI.path=="/api/sessions"->{assertEquals("local-test-only",JSONObject(payload).getString("joinCode"));"""{"token":"test-token","nickname":"친구","room":"라운지","maxText":2000}"""}
                e.requestMethod=="DELETE"->{left=true;"""{"ok":true}"""}
                e.requestMethod=="POST"->{sent=JSONObject(payload).getString("text");"""{"id":1,"nickname":"친구","text":${JSONObject.quote(sent)}}"""}
                mode=="large"->"x".repeat(ChatContract.ChatMaxResponseBytes+1)
                else->"""{"messages":[{"id":1,"nickname":"친구","text":${JSONObject.quote(sent)}}],"users":["친구"],"cursor":1,"reset":false,"pollAfterMs":0}"""
            }
            e.responseHeaders.set("Content-Type","application/json")
            val bytes=response.toByteArray();e.sendResponseHeaders(200,bytes.size.toLong());runCatching {e.responseBody.use {it.write(bytes)}};e.close()
        }
        server.start()
        try {
            val c=LoungeClient("http://127.0.0.1:${server.address.port}/")
            assertEquals("친구",c.join("친구","local-test-only"));c.send("세 기기에서 안녕 👋","a".repeat(32))
            val page=c.poll(0);assertEquals(sent,page.messages.single().text);assertEquals(1L,page.cursor)
            mode="large";try{c.poll(1);fail("Unbounded response accepted")}catch(_:IllegalArgumentException){}
            c.leave();assertTrue(left)
            try{c.poll(0);fail("Disconnected session used")}catch(_:java.util.concurrent.CancellationException){}
        }finally {server.stop(0)}
    }
}
