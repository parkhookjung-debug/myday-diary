plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("org.jetbrains.kotlin.plugin.compose")
}
android {
    namespace = "com.myday.diary"
    compileSdk = 35
    defaultConfig {
        applicationId = "com.myday.diary"
        minSdk = 26
        targetSdk = 35
        versionCode = 6
        versionName = "0.6.0"
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions { jvmTarget = "17" }
    buildFeatures { compose = true }
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
    implementation(platform("androidx.compose:compose-bom:2025.04.01"))
    implementation("androidx.activity:activity-compose:1.10.1")
    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.foundation:foundation")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.8.7")
    implementation("androidx.lifecycle:lifecycle-viewmodel-ktx:2.8.7")
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
