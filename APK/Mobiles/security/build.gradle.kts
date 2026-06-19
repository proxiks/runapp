android {
    defaultConfig {
        ndk {
            abiFilters += listOf("arm64-v8a", "x86_64")
        }
    }
    
    sourceSets {
        getByName("main") {
            jniLibs.srcDirs("src/main/jniLibs")
        }
    }
}

// Copy built .so files
tasks.register<Copy>("syncLyfronLibs") {
    from("$rootDir/../lyfron/bridge/liblyfron.so")
    into("src/main/jniLibs/arm64-v8a/")
}