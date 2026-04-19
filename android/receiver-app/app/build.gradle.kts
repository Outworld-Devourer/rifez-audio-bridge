import java.util.Properties

val localProperties = Properties().apply {
    val localFile = rootProject.file("local.properties")
    if (localFile.exists()) {
        localFile.inputStream().use { load(it) }
    }
}

val signingProperties = Properties().apply {
    val signingFile = file(System.getProperty("user.home") + "\\.rifez\\rifez-signing.properties")
    if (signingFile.exists()) {
        signingFile.inputStream().use { load(it) }
    }
}

val rifezStoreFile = signingProperties.getProperty("RIFEZ_STORE_FILE")
val rifezStorePassword = signingProperties.getProperty("RIFEZ_STORE_PASSWORD")
val rifezKeyAlias = signingProperties.getProperty("RIFEZ_KEY_ALIAS")
val rifezKeyPassword = signingProperties.getProperty("RIFEZ_KEY_PASSWORD")

plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.compose)
}

android {
    namespace = "com.rifez.phoneaudio"

    compileSdk {
        version = release(36)
    }

    defaultConfig {
        applicationId = "com.rifez.phoneaudio"
        minSdk = 26
        targetSdk = 36
        versionCode = 1
        versionName = "0.1.0-demo"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    signingConfigs {
        create("release") {
            if (
                !rifezStoreFile.isNullOrBlank() &&
                !rifezStorePassword.isNullOrBlank() &&
                !rifezKeyAlias.isNullOrBlank() &&
                !rifezKeyPassword.isNullOrBlank()
            ) {
                storeFile = file(rifezStoreFile)
                storePassword = rifezStorePassword
                keyAlias = rifezKeyAlias
                keyPassword = rifezKeyPassword
            }
        }
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            isShrinkResources = false
            signingConfig = signingConfigs.getByName("release")
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }

    buildFeatures {
        compose = true
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation("androidx.lifecycle:lifecycle-runtime-compose:2.10.0")

    implementation(libs.androidx.activity.compose)

    implementation(platform(libs.androidx.compose.bom))
    implementation(libs.androidx.compose.ui)
    implementation(libs.androidx.compose.ui.graphics)
    implementation(libs.androidx.compose.ui.tooling.preview)
    implementation(libs.androidx.compose.material3)

    implementation("com.google.flatbuffers:flatbuffers-java:24.3.25")

    testImplementation(libs.junit)

    androidTestImplementation(libs.androidx.junit)
    androidTestImplementation(libs.androidx.espresso.core)
    androidTestImplementation(platform(libs.androidx.compose.bom))
    androidTestImplementation(libs.androidx.compose.ui.test.junit4)

    debugImplementation(libs.androidx.compose.ui.tooling)
    debugImplementation(libs.androidx.compose.ui.test.manifest)
}