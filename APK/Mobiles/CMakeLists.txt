cmake_minimum_required(VERSION 3.18.1)
project("lyfron_webrtc")

set(CMAKE_CXX_STANDARD 17)

# WebRTC prebuilt library path
set(WEBRTC_ROOT ${CMAKE_SOURCE_DIR}/../../webrtc)

add_library(lyfron_webrtc SHARED
    webrtc_native.cpp
)

target_include_directories(lyfron_webrtc PRIVATE
    ${WEBRTC_ROOT}/include
    ${WEBRTC_ROOT}/include/third_party/abseil-cpp
    ${WEBRTC_ROOT}/include/third_party/libyuv/include
)

target_link_libraries(lyfron_webrtc
    ${WEBRTC_ROOT}/lib/libwebrtc.a
    log
    android
    OpenSLES
    EGL
    GLESv2
)

# ====== AUTO-DOWNLOAD WEBRTC PREBUILT ======
set(WEBRTC_VERSION "m120")
set(WEBRTC_URL "https://github.com/webrtc-sdk/android/releases/download/${WEBRTC_VERSION}/libwebrtc.aar")
set(WEBRTC_AAR "${CMAKE_BINARY_DIR}/webrtc.aar")
set(WEBRTC_EXTRACT_DIR "${CMAKE_BINARY_DIR}/webrtc_extracted")

# Download if not exists
if(NOT EXISTS ${WEBRTC_AAR})
    message(STATUS "Downloading WebRTC Android prebuilt...")
    file(DOWNLOAD ${WEBRTC_URL} ${WEBRTC_AAR} SHOW_PROGRESS)
endif()

# Extract AAR (it's a ZIP)
if(NOT EXISTS ${WEBRTC_EXTRACT_DIR})
    message(STATUS "Extracting WebRTC AAR...")
    file(MAKE_DIRECTORY ${WEBRTC_EXTRACT_DIR})
    execute_process(
        COMMAND ${CMAKE_COMMAND} -E tar xf ${WEBRTC_AAR}
        WORKING_DIRECTORY ${WEBRTC_EXTRACT_DIR}
    )
endif()

# Find extracted files
file(GLOB WEBRTC_JNI_DIR "${WEBRTC_EXTRACT_DIR}/jni/*")
set(WEBRTC_INCLUDE_DIR "${WEBRTC_EXTRACT_DIR}/jni/include")

# ====== BUILD YOUR NATIVE LIB ======
add_library(lyfron_webrtc SHARED
    webrtc_native.cpp
)

# WebRTC headers
target_include_directories(lyfron_webrtc PRIVATE
    ${WEBRTC_INCLUDE_DIR}
    ${WEBRTC_INCLUDE_DIR}/third_party/abseil-cpp
    ${WEBRTC_INCLUDE_DIR}/third_party/libyuv/include
)

# Link against extracted .so
find_library(WEBRTC_LIB webrtc HINTS ${WEBRTC_JNI_DIR})

target_link_libraries(lyfron_webrtc
    ${WEBRTC_LIB}
    log
    android
    OpenSLES
    EGL
    GLESv2
    z
)

target_compile_options(lyfron_webrtc PRIVATE
    -DWEBRTC_ANDROID
    -DWEBRTC_POSIX
    -DWEBRTC_LINUX
    -DABSL_ALLOCATOR_NOTHROW=1
)

# JavaVM access
target_compile_options(lyfron_webrtc PRIVATE -DWEBRTC_ANDROID)