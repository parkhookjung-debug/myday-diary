package com.myday.diary.social
import com.myday.diary.BuildConfig
import org.junit.Assert.*
import org.junit.Test
class OfflineIntegrationTest {
    @Test fun integrationCannotUsePublicCredentialsOrSdkCallbacks(){
        assertFalse(BuildConfig.ONLINE_FRIENDS_ENABLED)
        assertEquals("",BuildConfig.SUPABASE_URL)
        assertEquals("",BuildConfig.SUPABASE_KEY)
        assertEquals("",BuildConfig.KAKAO_NATIVE_KEY)
        assertFalse(CommunityConnection.configured)
    }
}
