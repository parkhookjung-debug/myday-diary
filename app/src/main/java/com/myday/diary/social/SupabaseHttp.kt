package com.myday.diary.social

import com.myday.diary.BuildConfig
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.ByteArrayOutputStream
import java.net.HttpURLConnection
import java.net.URI

/** Only the public project key belongs in the APK. All access uses RLS / RPC. */
object SupabaseHttp {
    suspend fun bytes(method: String, route: String, body: ByteArray? = null, contentType: String = "application/json", token: String = ""): ByteArray = withContext(Dispatchers.IO) {
        check(CommunityConnection.configured) { "친구 공간 연결을 준비하고 있어요." }
        check(route.startsWith('/') && !route.startsWith("//"))
        val connection = URI(BuildConfig.SUPABASE_URL.trimEnd('/') + route).toURL().openConnection() as HttpURLConnection
        try {
            connection.requestMethod = method
            connection.instanceFollowRedirects = false
            connection.connectTimeout = 15000; connection.readTimeout = 30000
            connection.setRequestProperty("apikey", BuildConfig.SUPABASE_KEY)
            if (token.isNotBlank()) connection.setRequestProperty("Authorization", "Bearer $token")
            connection.setRequestProperty("Accept", "application/json")
            if (body != null) {
                require(body.size <= 2 * 1024 * 1024) { "보낼 사진과 글의 크기를 줄여 주세요." }
                connection.setRequestProperty("Content-Type", contentType)
                connection.doOutput = true; connection.setFixedLengthStreamingMode(body.size)
                connection.outputStream.use { it.write(body) }
            }
            val status = connection.responseCode
            val input = if (status in 200..299) connection.inputStream else connection.errorStream
            val bytes = input?.use { stream ->
                val output = ByteArrayOutputStream(); val buffer = ByteArray(8192)
                while (true) { val n = stream.read(buffer); if (n < 0) break; require(output.size()+n <= 2*1024*1024) { "응답이 너무 커요." }; output.write(buffer,0,n) }
                output.toByteArray()
            } ?: byteArrayOf()
            if (status !in 200..299) {
                val error = runCatching { JSONObject(bytes.toString(Charsets.UTF_8)) }.getOrDefault(JSONObject())
                if(error.optString("code")=="28000")throw FriendsFailure(401,"카카오 계정으로 다시 로그인해 주세요.")
                val message = when {
                    error.optString("code") == "P0001" -> error.optString("message").take(200)
                    status == 401 -> "로그인이 만료됐어요. 다시 로그인해 주세요."
                    status == 403 -> "이 기록에 접근할 수 없어요. 친구 연결을 확인해 주세요."
                    status == 413 -> "사진과 글의 크기를 줄여 주세요."
                    status == 429 -> "요청이 많아요. 잠시 후 다시 시도해 주세요."
                    else -> "친구 공간에 연결하지 못했어요. 잠시 후 다시 시도해 주세요."
                }
                throw FriendsFailure(status,message)
            }
            bytes
        } finally { connection.disconnect() }
    }
    suspend fun json(method: String, route: String, body: JSONObject? = null, token: String = ""): JSONObject {
        val raw = bytes(method, route, body?.toString()?.toByteArray(Charsets.UTF_8), token=token)
        return if (raw.isEmpty()) JSONObject() else JSONObject(raw.toString(Charsets.UTF_8)).also {
            if (it.has("error")) throw FriendsFailure(400,it.optString("error").take(200))
        }
    }
}
