cmake_minimum_required(VERSION 3.20)
project(lyfron_webrtc)

set(CMAKE_CXX_STANDARD 17)

# Find WebRTC
find_package(webrtc REQUIRED)

# Find OpenSSL for Lyfron crypto
find_package(OpenSSL REQUIRED)

add_library(lyfron_webrtc SHARED
    lyfron_webrtc.ii
    lyfron_crypto.cc
)

target_link_libraries(lyfron_webrtc
    webrtc
    OpenSSL::SSL
    OpenSSL::Crypto
)

# Windows specific
if(WIN32)
    target_link_libraries(lyfron_webrtc
        ws2_32
        winmm
        secur32
        crypt32
        iphlpapi
        dmoguids
        strmiids
        msdmo
        wmcodecdspuuid
    )
endif()

set_target_properties(lyfron_webrtc PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin
)