package com.myday.diary

import android.app.Application
import com.myday.diary.social.CommunityConnection
import com.myday.diary.social.FriendsClient
import com.kakao.sdk.common.KakaoSdk

class MyDayApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        CommunityConnection.initialize(this)
        FriendsClient.initialize(this)
        if(BuildConfig.ONLINE_FRIENDS_ENABLED && BuildConfig.KAKAO_NATIVE_KEY.matches(Regex("[a-fA-F0-9]{32}"))) KakaoSdk.init(this,BuildConfig.KAKAO_NATIVE_KEY)
    }
}
