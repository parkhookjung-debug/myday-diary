package com.myday.diary

import com.myday.diary.data.*
import com.myday.diary.device.*
import org.junit.Assert.*
import org.junit.Test
import java.net.*
import java.nio.file.Files
import java.time.LocalDate

class DeviceToolsTest {
    private fun book()=DiaryBook(days=mapOf("2026-10-24" to DiaryEntry(blocks=listOf(DiaryBlock(type="text",text="기기 사이 한글 일기")))))
    @Test fun actualNativeHttpHandoffAndRevocation(){
        val bytes=NotebookJson.encode(book()).toByteArray();val link=DeviceLinkServer("127.0.0.1",bytes,"<html>MyDay</html>".toByteArray())
        try{assertEquals("기기 사이 한글 일기",DeviceAddress.validate(DeviceAddress.download(link.invitation)).days.values.first().blocks.first().text)
            val connection=URL(link.origin+"/upload").openConnection(Proxy.NO_PROXY) as HttpURLConnection
            try{connection.requestMethod="POST";connection.doOutput=true;connection.setRequestProperty("Content-Type","application/json");connection.setRequestProperty("Authorization","Bearer ${link.token}");connection.setFixedLengthStreamingMode(bytes.size);connection.outputStream.use{it.write(bytes)};assertEquals(200,connection.responseCode);connection.inputStream.close()}finally{connection.disconnect()}
            assertArrayEquals(bytes,link.incoming)
            val denied=URL(link.origin+"/download").openConnection(Proxy.NO_PROXY) as HttpURLConnection
            try{assertEquals(401,denied.responseCode);denied.errorStream?.close()}finally{denied.disconnect()}
        }finally{link.close()};assertFalse(link.active);assertNull(link.incoming)
    }
    @Test fun invitationsStayOnPrivateIpv4WithoutRedirectablePaths(){
        val token="a".repeat(43);listOf("10.0.0.1","127.0.0.1","192.168.0.1","172.16.0.2").forEach{assertEquals(it,DeviceAddress.parse("http://$it:8766/#$token").host)}
        listOf("http://8.8.8.8/#$token","http://localhost/#$token","https://192.168.0.1/#$token","http://192.168.0.1/path#$token","http://user@192.168.0.1/#$token","http://192.168.0.1/?x=1#$token").forEach{assertTrue(runCatching{DeviceAddress.parse(it)}.isFailure)}
    }
    @Test fun dailyBackupsAreBoundedAndFailuresDoNotThrow(){
        val root=Files.createTempDirectory("myday-auto").toFile()
        try{val backup=AutomaticBackup(root);val bytes=NotebookJson.encode(book()).toByteArray();for(day in 1..5)assertTrue(backup.capture(bytes,day=LocalDate.of(2030,1,day)))
            assertEquals(3,backup.files().size);assertEquals("daily-2030-01-03.json",backup.files().last().name);assertTrue(backup.state().lastSuccess.isNotBlank())
            val unrelated=java.io.File(backup.directory,"my-copy.json");unrelated.writeText("keep");assertTrue(backup.capture(bytes,day=LocalDate.of(2030,1,6)));assertTrue(unrelated.exists())
            backup.setEnabled(false);assertFalse(backup.capture(bytes,day=LocalDate.of(2030,1,7)));assertEquals(3,backup.files().size)
            val blocked=java.io.File(root,"blocked");blocked.mkdirs();java.io.File(blocked,"automatic-backups").writeText("blocked");val failing=AutomaticBackup(blocked);assertFalse(failing.capture(bytes));assertTrue(failing.state().error.isNotBlank())
            assertTrue(backup.files().all{NotebookJson.decode(it.readText()).days.size==1})
        }finally{root.deleteRecursively()}
    }
}
