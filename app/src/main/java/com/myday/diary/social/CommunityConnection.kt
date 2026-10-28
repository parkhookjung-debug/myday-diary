package com.myday.diary.social

import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import androidx.compose.runtime.*
import com.myday.diary.BuildConfig
import kotlinx.coroutines.*
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import org.json.JSONObject
import java.net.URI
import java.security.KeyStore
import com.kakao.sdk.auth.TokenManagerProvider
import com.kakao.sdk.auth.model.OAuthToken
import com.kakao.sdk.common.model.ClientError
import com.kakao.sdk.common.model.ClientErrorCause
import com.kakao.sdk.user.UserApiClient
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

/** Kakao SDK login is independently verified by the free Edge Function. */
object CommunityConnection {
    private lateinit var app: Context
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)
    private val mutex = Mutex()
    private var session = JSONObject()
    var revision by mutableIntStateOf(0); private set
    var authMessage by mutableStateOf(""); private set
    val signedIn: Boolean get() = session.optString("access_token").isNotBlank()
    val configured: Boolean get() = runCatching {
        val u = URI(BuildConfig.SUPABASE_URL)
        u.scheme == "https" && u.host != null && u.userInfo == null && u.query == null && u.fragment == null &&
            u.path.orEmpty() in setOf("", "/") && BuildConfig.SUPABASE_KEY.isNotBlank() &&
            BuildConfig.SUPABASE_KEY.startsWith("sb_publishable_") && BuildConfig.KAKAO_NATIVE_KEY.matches(Regex("[a-fA-F0-9]{32}"))
    }.getOrDefault(false)

    fun initialize(context: Context) {
        app = context.applicationContext
        session = runCatching { JSONObject(readVault()) }.getOrDefault(JSONObject())
    }
    fun hasToken() = configured && signedIn
    fun userId(): String = session.optJSONObject("user")?.optString("id").orEmpty().also { java.util.UUID.fromString(it) }

    private fun key(): SecretKey {
        val store = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }
        val alias = "myday-friends-session-v1"
        return store.getKey(alias, null) as? SecretKey ?: KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore").apply {
            init(KeyGenParameterSpec.Builder(alias, KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT)
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).build())
        }.generateKey()
    }
    private fun readVault(): String {
        val value = app.getSharedPreferences("friends_session", Context.MODE_PRIVATE).getString("encrypted", null) ?: return "{}"
        val parts = value.split(':')
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        cipher.init(Cipher.DECRYPT_MODE, key(), GCMParameterSpec(128, Base64.decode(parts[0], Base64.NO_WRAP)))
        return cipher.doFinal(Base64.decode(parts[1], Base64.NO_WRAP)).toString(Charsets.UTF_8)
    }
    private fun persist() {
        val cipher = Cipher.getInstance("AES/GCM/NoPadding").apply { init(Cipher.ENCRYPT_MODE, key()) }
        val value = Base64.encodeToString(cipher.iv, Base64.NO_WRAP) + ":" + Base64.encodeToString(cipher.doFinal(session.toString().toByteArray()), Base64.NO_WRAP)
        check(app.getSharedPreferences("friends_session", Context.MODE_PRIVATE).edit().putString("encrypted", value).commit()) { "로그인 정보를 저장하지 못했어요." }
    }
    fun login(context: Context, complete: (Throwable?) -> Unit) {
        if(!configured){complete(IllegalStateException("친구 공간 연결을 준비하고 있어요."));return}
        val callback: (OAuthToken?,Throwable?)->Unit = { token,error ->
            if(error!=null || token==null)complete(error ?: IllegalStateException("카카오 로그인을 완료하지 못했어요."))
            else scope.launch {
                mutex.withLock {
                    try {
                        saveSession(SupabaseHttp.json("POST","/functions/v1/myday-kakao-login",JSONObject().put("kakaoAccessToken",token.accessToken)))
                        authMessage="로그인했어요. 초대코드로 친구를 연결해 주세요."
                        revision++;complete(null)
                    }catch(e: Exception){if(e is CancellationException)throw e;complete(e)}
                    finally{TokenManagerProvider.instance.manager.clear()}
                }
            }
        }
        if(UserApiClient.instance.isKakaoTalkLoginAvailable(context))UserApiClient.instance.loginWithKakaoTalk(context){token,error->
            if(error is ClientError && error.reason==ClientErrorCause.Cancelled)complete(IllegalStateException("카카오 로그인을 취소했어요."))
            else if(error!=null)UserApiClient.instance.loginWithKakaoAccount(context,callback=callback)
            else callback(token,null)
        } else UserApiClient.instance.loginWithKakaoAccount(context,callback=callback)
    }

    private fun saveSession(data: JSONObject) {
        check(data.optString("access_token").isNotBlank() && data.optString("refresh_token").isNotBlank()) { "로그인 응답을 확인하지 못했어요." }
        data.put("expires_at", System.currentTimeMillis() / 1000 + data.optLong("expires_in", 3600))
        // Provider tokens are unnecessary for invitation-based friendship.
        data.remove("provider_token"); data.remove("provider_refresh_token")
        session = data
        persist()
    }
    suspend fun accessToken(): String = mutex.withLock {
        check(hasToken()) { "먼저 카카오 계정으로 로그인해 주세요." }
        if (session.optLong("expires_at") <= System.currentTimeMillis() / 1000 + 60) {
            try {
                saveSession(SupabaseHttp.json("POST", "/auth/v1/token?grant_type=refresh_token", JSONObject().put("refresh_token", session.getString("refresh_token"))))
            } catch (e: FriendsFailure) {
                if (e.status in setOf(400,401,403)) { session = JSONObject(); persist(); revision++ }
                throw e
            }
        }
        session.getString("access_token")
    }
    fun logout(complete: () -> Unit) {
        scope.launch {
            mutex.withLock {
                if (signedIn) runCatching { SupabaseHttp.json("POST", "/auth/v1/logout?scope=local", token=session.optString("access_token")) }
                session = JSONObject(); persist(); authMessage = ""; revision++
            }
            complete()
        }
    }
}
