#ifndef LYFRON_PROTOCOL_H_CPP
#define LYFRON_PROTOCOL_H_CPP

#include "protocol.h"
#include <winsock2.h>
#include <vector>
#include <cstdint>

namespace lyfron {

enum class ProtocolError {
    OK = 0,
    INVALID_MAGIC,
    VERSION_MISMATCH,
    PAYLOAD_TOO_LARGE,
    CHECKSUM_FAILED,
    SEQUENCE_OUT_OF_ORDER,
    TIMEOUT,
    CONNECTION_CLOSED,
    ENCRYPTION_ERROR
};

class ProtocolEngine {
public:
    ProtocolEngine();
    ~ProtocolEngine();

    // Initialize with socket, returns false on fatal error
    bool Attach(SOCKET sock);
    void Detach();

    // Send a packet with given payload
    bool SendPacket(uint8_t type, const uint8_t* payload, uint16_t payload_len);
    
    // Receive a full packet (blocking with timeout)
    // Returns true if packet received, false on error
    // 'out_payload' is resized to fit payload
    bool RecvPacket(uint8_t& out_type, std::vector<uint8_t>& out_payload, uint32_t timeout_ms);

    // Template helpers for typed payloads
    template<typename T>
    bool SendTyped(uint8_t type, const T& data) {
        static_assert(std::is_pod<T>::value, "T must be POD");
        return SendPacket(type, reinterpret_cast<const uint8_t*>(&data), sizeof(T));
    }

    template<typename T>
    bool RecvTyped(uint8_t& out_type, T& out_data, uint32_t timeout_ms) {
        static_assert(std::is_pod<T>::value, "T must be POD");
        std::vector<uint8_t> payload;
        if (!RecvPacket(out_type, payload, timeout_ms)) return false;
        if (payload.size() != sizeof(T)) return false;
        memcpy(&out_data, payload.data(), sizeof(T));
        return true;
    }

    uint32_t GetLastSequence() const { return last_rx_sequence_; }
    ProtocolError GetLastError() const { return last_error_; }

private:
    SOCKET sock_;
    uint32_t tx_sequence_;
    uint32_t last_rx_sequence_;
    ProtocolError last_error_;
    bool encrypted_;  // Future: Part 6

    uint32_t CalculateChecksum(const LyfronHeader* hdr, const uint8_t* payload);
    bool ValidateHeader(const LyfronHeader* hdr);
    bool SendRaw(const void* data, size_t len);
    bool RecvRaw(void* data, size_t len, uint32_t timeout_ms);
    bool SetSocketTimeout(uint32_t ms);
};

} // namespace lyfron

#endif