#ifndef LYFRON_WEBRTC_H
#define LYFRON_WEBRTC_H

#ifdef __cplusplus
extern "C" {
#endif

// Opaque handle
typedef void* LyfronPeerConnection;

// Callbacks
typedef void (*OnIceCandidateCallback)(const char* sdp_mid, int sdp_mline_index, const char* candidate);
typedef void (*OnTrackCallback)(const char* kind, void* track);
typedef void (*OnConnectionStateChangeCallback)(int state);
typedef void (*OnDataChannelMessageCallback)(const char* data, size_t len);

// Peer connection
LyfronPeerConnection lyfron_create_peer_connection(
    const char* stun_server,
    OnIceCandidateCallback ice_cb,
    OnConnectionStateChangeCallback state_cb
);

void lyfron_destroy_peer_connection(LyfronPeerConnection pc);

// Media
int lyfron_add_audio_track(LyfronPeerConnection pc, const char* track_id);
int lyfron_add_video_track(LyfronPeerConnection pc, const char* track_id, void* native_window);

void lyfron_remove_track(LyfronPeerConnection pc, int track_index);

// SDP
int lyfron_create_offer(LyfronPeerConnection pc, char* sdp_out, size_t sdp_len);
int lyfron_create_answer(LyfronPeerConnection pc, char* sdp_out, size_t sdp_len);
int lyfron_set_remote_description(LyfronPeerConnection pc, const char* type, const char* sdp);
int lyfron_set_local_description(LyfronPeerConnection pc, const char* type, const char* sdp);

// ICE
int lyfron_add_ice_candidate(LyfronPeerConnection pc, const char* sdp_mid, int mline_index, const char* candidate);

// Data channel
int lyfron_create_data_channel(LyfronPeerConnection pc, const char* label, OnDataChannelMessageCallback msg_cb);
int lyfron_send_data(LyfronPeerConnection pc, const char* data, size_t len);

// Encryption (Lyfron layer)
int lyfron_encrypt_srtp_packet(void* packet, size_t len, const uint8_t* key, size_t key_len);
int lyfron_decrypt_srtp_packet(void* packet, size_t len, const uint8_t* key, size_t key_len);

// Device enumeration
int lyfron_get_audio_devices(char* devices_json, size_t len);
int lyfron_get_video_devices(char* devices_json, size_t len);
int lyfron_select_audio_device(int index);
int lyfron_select_video_device(int index);

// Controls
void lyfron_set_audio_enabled(LyfronPeerConnection pc, int enabled);
void lyfron_set_video_enabled(LyfronPeerConnection pc, int enabled);
void lyfron_set_speaker_muted(LyfronPeerConnection pc, int muted);

#ifdef __cplusplus
}
#endif

#endif