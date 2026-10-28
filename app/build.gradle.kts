import java.util.Properties

plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("org.jetbrains.kotlin.plugin.compose")
}
val localSettings = Properties().apply {
    val settings = rootProject.file("local.properties")
    if (settings.exists()) settings.inputStream().use { load(it) }
}
fun integrationSetting(name: String) = providers.gradleProperty(name)
    .orElse(providers.environmentVariable(name)).orElse(localSettings.getProperty(name, "")).get().trim()
fun quoted(value: String) = "\"" + value.replace("\\", "\\\\").replace("\"", "\\\"") + "\""
val supabaseUrl = integrationSetting("MYDAY_SUPABASE_URL")
val supabaseKey = integrationSetting("MYDAY_SUPABASE_KEY")
val kakaoNativeKey = integrationSetting("MYDAY_KAKAO_NATIVE_KEY")
require(supabaseKey.isEmpty() || supabaseKey.startsWith("sb_publishable_")) { "Use a Supabase publishable key, never a secret or service role key." }
android {
    namespace = "com.myday.diary"
    compileSdk = 35
    defaultConfig {
        applicationId = "com.myday.diary"
        minSdk = 26
        targetSdk = 35
        versionCode = 14
        versionName = "0.14.0"
        manifestPlaceholders["appLabel"] = "마이데이"
        buildConfigField("String", "SUPABASE_URL", quoted(supabaseUrl))
        buildConfigField("String", "SUPABASE_KEY", quoted(supabaseKey))
        buildConfigField("String", "KAKAO_NATIVE_KEY", quoted(kakaoNativeKey))
        manifestPlaceholders["kakaoRedirectScheme"] = if(kakaoNativeKey.matches(Regex("[a-fA-F0-9]{32}"))) "kakao$kakaoNativeKey" else "myday-kakao-unconfigured"
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions { jvmTarget = "17" }
    buildFeatures { compose = true; buildConfig = true }
    buildTypes.getByName("debug") {
        applicationIdSuffix = ".friends.preview"
        versionNameSuffix = "-friends-preview"
        manifestPlaceholders["appLabel"] = "마이데이 친구"
    }
    val signingPath=System.getenv("MYDAY_ANDROID_KEYSTORE")
    if(!signingPath.isNullOrBlank()) {
        signingConfigs.create("mydayDistribution") {
            storeFile=file(signingPath)
            storePassword=System.getenv("MYDAY_ANDROID_KEYSTORE_PASSWORD")
            keyAlias="myday-preview"
            keyPassword=System.getenv("MYDAY_ANDROID_KEYSTORE_PASSWORD")
        }
        buildTypes.getByName("release").signingConfig=signingConfigs.getByName("mydayDistribution")
    }
}
dependencies {
    implementation("com.kakao.sdk:v2-user:2.26.0")
    implementation("com.google.zxing:core:3.5.3")
    implementation(platform("androidx.compose:compose-bom:2025.04.01"))
    implementation("androidx.activity:activity-compose:1.10.1")
    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.foundation:foundation")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.8.7")
    implementation("androidx.lifecycle:lifecycle-viewmodel-ktx:2.8.7")
    implementation("androidx.lifecycle:lifecycle-runtime-compose:2.8.7")
    debugImplementation("androidx.compose.ui:ui-tooling-preview")
    debugImplementation("androidx.compose.ui:ui-tooling")
    testImplementation("junit:junit:4.13.2")
    testImplementation("org.json:json:20240303")
    androidTestImplementation(platform("androidx.compose:compose-bom:2025.04.01"))
    androidTestImplementation("androidx.compose.ui:ui-test-junit4")
    androidTestImplementation("androidx.test:runner:1.6.2")
    androidTestImplementation("androidx.test.ext:junit:1.2.1")
    debugImplementation("androidx.compose.ui:ui-test-manifest")
}

tasks.withType<Test>().configureEach {
    systemProperty("myday.interop.output",layout.buildDirectory.file("interop/android-to-windows.json").get().asFile.absolutePath)
}
