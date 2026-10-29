package com.myday.diary.social

import android.content.Context
import android.util.Base64
import com.myday.diary.data.DiaryBlock
import com.myday.diary.data.NotebookPhoto
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import java.time.LocalDate
import java.util.UUID

data class FriendProfile(val id: String, val nickname: String)
data class FriendRequest(val id: String, val nickname: String, val incoming: Boolean)
data class FriendInvite(val code: String, val expiresAt: String)
data class SharedPost(val ownerId: String, val nickname: String, val date: String, val title: String,
    val mood: String, val blockCount: Int, val photoCount: Int, val updatedAt: String)
data class SharedPostDetail(val summary: SharedPost, val blocks: List<DiaryBlock>)
class FriendsFailure(val status: Int, message: String) : Exception(message)

class FriendsClient {
    companion object {
        private lateinit var app: Context
        fun initialize(context: Context) { app=context.applicationContext }
        fun formatCode(code: String) = code.chunked(4).joinToString("-")
    }
    private fun id(value: String) = value.also { UUID.fromString(it) }
    private suspend fun rpc(name: String, body: JSONObject=JSONObject()) = SupabaseHttp.json("POST", "/rest/v1/rpc/myday_$name", body, CommunityConnection.accessToken())
    private fun profile(json: JSONObject) = FriendProfile(id(json.getString("id")),json.getString("nickname").take(40))
    private fun post(json: JSONObject) = SharedPost(id(json.getString("ownerId")),json.getString("nickname").take(40),
        json.getString("date").also { LocalDate.parse(it) },json.getString("title").take(80),json.getString("mood").take(40),
        json.getInt("blockCount"),json.getInt("photoCount"),json.getString("updatedAt"))
    private fun <T> array(a: JSONArray, mapper: (JSONObject)->T): List<T> { require(a.length()<=5000); return (0 until a.length()).map { mapper(a.getJSONObject(it)) } }
    suspend fun me(): FriendProfile { val user=profile(rpc("me")); flushCleanup(); return user }
    suspend fun friends() = array(rpc("friends").getJSONArray("friends"),::profile)
    suspend fun requests() = array(rpc("requests").getJSONArray("requests")) { FriendRequest(id(it.getString("id")),it.getString("nickname").take(40),it.getBoolean("incoming")) }
    suspend fun createInvite(): FriendInvite { val json=rpc("create_invite"); return FriendInvite(json.getString("code"),json.getString("expiresAt")) }
    suspend fun redeem(code: String) { rpc("redeem_invite",JSONObject().put("p_code",code.take(80))) }
    suspend fun answer(requestId: String, accept: Boolean) { rpc("answer_request",JSONObject().put("p_id",id(requestId)).put("p_accept",accept)) }
    suspend fun removeFriend(friendId: String) { rpc("remove_friend",JSONObject().put("p_id",id(friendId))) }
    suspend fun posts(mine: Boolean, ownerId: String?=null) = array(rpc("posts",JSONObject().put("p_mine",mine).put("p_owner",ownerId?.let(::id) ?: JSONObject.NULL)).getJSONArray("entries"),::post)
    private suspend fun detailJson(owner: String,date: String) = rpc("detail",JSONObject().put("p_owner",id(owner)).put("p_date",date.also { LocalDate.parse(it) }))
    suspend fun detail(value: SharedPost): SharedPostDetail {
        val json=detailJson(value.ownerId,value.date)
        val a=json.getJSONArray("blocks"); require(a.length() in 1..30)
        val token=CommunityConnection.accessToken(); val blocks=mutableListOf<DiaryBlock>()
        for(i in 0 until a.length()) {
            val b=a.getJSONObject(i)
            val photo=if(!b.has("photoPath"))null else {
                val path=b.getString("photoPath")
                require(path.matches(Regex("${Regex.escape(value.ownerId)}/[a-f0-9-]{36}/[a-f0-9-]{36}\\.jpg")))
                val bytes=SupabaseHttp.bytes("GET","/storage/v1/object/authenticated/diary-photos/$path",token=token)
                withContext(Dispatchers.IO) { Base64.encodeToString(bytes,Base64.NO_WRAP) }.also { require(it.length<=NotebookPhoto.MAX_CHARACTERS) }
            }
            blocks+=DiaryBlock(id=b.getString("id"),type=b.getString("type"),title=b.getString("title"),text=b.getString("text"),checked=b.getBoolean("checked"),photo=photo)
        }
        require(blocks.count{it.photo!=null}<=6)
        return SharedPostDetail(post(json),blocks)
    }
    private suspend fun photoPaths(date: LocalDate?=null): List<String> {
        val a=rpc("own_photo_paths",JSONObject().put("p_date",date?.toString() ?: JSONObject.NULL)).getJSONArray("paths")
        return (0 until a.length()).map{a.getString(it)}
    }
    suspend fun publish(date: LocalDate,draft: SharedDiaryDraft) {
        val owner=CommunityConnection.userId(); val publication=UUID.randomUUID().toString()
        val old=photoPaths(date); val uploaded=mutableListOf<String>(); val body=draft.json()
        try {
            val blocks=body.getJSONArray("blocks")
            for(i in 0 until blocks.length()) {
                val b=blocks.getJSONObject(i)
                if(b.has("photo")) {
                    val bytes=withContext(Dispatchers.IO) { Base64.decode(b.getString("photo"),Base64.NO_WRAP) }
                    require(bytes.size<=2*1024*1024) { "사진은 한 장에 2MB까지 공개할 수 있어요." }
                    val path="$owner/$publication/${UUID.randomUUID()}.jpg"
                    uploaded+=path
                    SupabaseHttp.bytes("POST","/storage/v1/object/diary-photos/$path",bytes,"image/jpeg",CommunityConnection.accessToken())
                    b.remove("photo"); b.put("photoPath",path)
                }
            }
            rpc("publish",JSONObject().put("p_date",date.toString()).put("p_payload",body))
        } catch(e: Exception) {
            // A lost publish response is ambiguous. Never delete a committed photo.
            runCatching { queueCleanup(uploaded-photoPaths().toSet()); flushCleanup() }
            throw e
        }
        queueCleanup(old-uploaded.toSet()); flushCleanup()
    }
    suspend fun unpublish(date: LocalDate) {
        val paths=photoPaths(date); rpc("unpublish",JSONObject().put("p_date",date.toString())); queueCleanup(paths); flushCleanup()
    }
    suspend fun clearShared() {
        val paths=photoPaths(); rpc("clear_shared"); queueCleanup(paths); flushCleanup()
    }
    private fun queueCleanup(paths: List<String>) {
        val owner=CommunityConnection.userId(); val prefs=app.getSharedPreferences("friends_photo_cleanup",Context.MODE_PRIVATE)
        val old=JSONArray(prefs.getString(owner,"[]"))
        val all=((0 until old.length()).map{old.getString(it)}+paths).filter{it.startsWith("$owner/")}.distinct()
        prefs.edit().putString(owner,JSONArray(all).toString()).apply()
    }
    private suspend fun flushCleanup() {
        // Content access was revoked by the RPC before Storage cleanup begins.
        runCatching {
            val owner=CommunityConnection.userId(); val prefs=app.getSharedPreferences("friends_photo_cleanup",Context.MODE_PRIVATE)
            val a=JSONArray(prefs.getString(owner,"[]")); val pending=(0 until a.length()).map{a.getString(it)}
            for(batch in pending.chunked(100)) {
                val live=photoPaths().toSet()
                val stale=batch.filter{it !in live && it.matches(Regex("${Regex.escape(owner)}/[a-f0-9-]{36}/[a-f0-9-]{36}\\.jpg"))}
                if(stale.isNotEmpty())SupabaseHttp.bytes("DELETE","/storage/v1/object/diary-photos",JSONObject().put("prefixes",JSONArray(stale)).toString().toByteArray(),token=CommunityConnection.accessToken())
                val remaining=JSONArray(prefs.getString(owner,"[]")); val next=(0 until remaining.length()).map{remaining.getString(it)}-batch.toSet()
                prefs.edit().putString(owner,JSONArray(next).toString()).apply()
            }
        }
    }
}
