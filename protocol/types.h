#ifndef LYFRON_PROTOCOL_H
#define LYFRON_PROTOCOL_H

#include <stdint.h>

#define LYFRON_MAGIC        0x4C594652  // "LYFR"
#define LYFRON_VERSION      1
#define LYFRON_MAX_PAYLOAD  65535

enum LyfronPacketType : uint8_t {
    PKT_HANDSHAKE_CHALLENGE = 0x01,
    PKT_HANDSHAKE_RESPONSE  = 0x02,
    PKT_HEARTBEAT           = 0x03,
    PKT_HEARTBEAT_ACK       = 0x04,
    PKT_INTEGRITY_REPORT    = 0x05,
    PKT_INTEGRITY_VIOLATION = 0x06,
    PKT_VIOLATION           = 0xFF
};

#pragma pack(push, 1)

struct LyfronHeader {
    uint32_t magic;           // 0x4C594652
    uint16_t version;         // Must match LYFRON_VERSION
    uint16_t payload_len;     // Max LYFRON_MAX_PAYLOAD
    uint8_t  packet_type;     // LyfronPacketType
    uint32_t sequence;        // Monotonic, anti-replay
    uint32_t checksum;        // CRC32 of header+payload (magic=0 for calc)
};

// --- Payload Structures ---

struct HandshakeChallenge {
    uint8_t  nonce[32];
    uint64_t timestamp;
    uint8_t  server_pubkey[32];
};

struct HandshakeResponse {
    uint8_t  client_pubkey[32];
    uint8_t  proof[64];
    uint32_t client_version;
    uint8_t  integrity_hash[32];
};

struct HeartbeatPayload {
    uint64_t client_timestamp;
    uint32_t session_token[4];  // 128-bit session ID
    uint16_t cpu_load;          // 0-1000 (0.1% precision)
    uint16_t mem_usage;         // MB
};

struct IntegrityReport {
    uint8_t  section_hash[32];  // SHA-256 of .text section
    uint32_t section_count;
    uint32_t tick_count;        // Windows GetTickCount64 low bits
};

struct ViolationReport {
    uint8_t  violation_type;
    uint32_t violation_code;
    uint8_t  context[56];       // Arbitrary context data
};

#pragma pack(pop)

// Helper to calculate header+payload size
static inline size_t PacketSize(uint16_t payload_len) {
    return sizeof(LyfronHeader) + payload_len;
}

#endif