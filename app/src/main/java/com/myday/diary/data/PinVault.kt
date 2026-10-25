package com.myday.diary.data
import java.io.File
import java.security.SecureRandom
import java.security.MessageDigest
import java.util.Base64
import javax.crypto.SecretKeyFactory
import javax.crypto.spec.PBEKeySpec
import org.json.JSONObject
class PinVault(private val root:File) {
 private val file=File(root,"screen-lock.json")
 fun state():JSONObject? {if(!file.exists())return null;require(file.length()<=4096);return JSONObject(file.readText()).also{require(Base64.getDecoder().decode(it.getString("Salt")).size==16&&Base64.getDecoder().decode(it.getString("Hash")).size==32&&it.getInt("Failures") in 0..5&&it.getLong("RetryAfter")>=0)}}
 fun enabled()=state()!=null
 private fun save(s:JSONObject){root.mkdirs();val temp=File(root,"screen-lock.tmp");try{java.io.FileOutputStream(temp).use{it.write(s.toString().toByteArray());it.fd.sync()};java.nio.file.Files.move(temp.toPath(),file.toPath(),java.nio.file.StandardCopyOption.ATOMIC_MOVE,java.nio.file.StandardCopyOption.REPLACE_EXISTING)}finally{temp.delete()}}
 fun verify(pin:String):Boolean {val s=state() ?: return true;check(System.currentTimeMillis()>=s.getLong("RetryAfter")){"5회 실패하면 30초 후 다시 시도할 수 있어요."};val correct=MessageDigest.isEqual(Base64.getDecoder().decode(s.getString("Hash")),derive(pin,Base64.getDecoder().decode(s.getString("Salt"))));val failures=if(correct)0 else s.getInt("Failures")+1;s.put("Failures",if(failures>=5)0 else failures).put("RetryAfter",if(failures>=5)System.currentTimeMillis()+30000 else if(correct)0 else s.getLong("RetryAfter"));save(s);return correct}
 fun set(old:String,pin:String){check(verify(old)){"현재 PIN이 맞지 않아요."};require(valid(pin)){"PIN은 숫자 6~12자리예요."};val salt=ByteArray(16).also{SecureRandom().nextBytes(it)};save(JSONObject().put("Salt",Base64.getEncoder().encodeToString(salt)).put("Hash",Base64.getEncoder().encodeToString(derive(pin,salt))).put("Failures",0).put("RetryAfter",0))}
 fun disable(old:String){check(verify(old)){"현재 PIN이 맞지 않아요."};check(!file.exists()||file.delete())}
 companion object {fun valid(pin:String)=pin.length in 6..12&&pin.all{it in '0'..'9'};fun derive(pin:String,salt:ByteArray):ByteArray {val spec=PBEKeySpec(pin.toCharArray(),salt,100000,256);return try{SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256").generateSecret(spec).encoded}finally{spec.clearPassword()}}}
}
