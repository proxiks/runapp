#include "protocol.h"
#include "handshake.h"
#include <sodium.h>
#include <windows.h>

bool LyfronClient::PerformHandshake(SOCKET sock) {
    lyfron::ProtocolEngine proto;
    if (!proto.Attach(sock)) return false;

    // --- Receive Challenge ---
    uint8_t pkt_type;
    std::vector<uint8_t> payload;
    
    if (!proto.RecvPacket(pkt_type, payload, 10000)) {
        return false;
    }
    if (pkt_type != PKT_HANDSHAKE_CHALLENGE || payload.size() != sizeof(HandshakeChallenge)) {
        return false;
    }

    HandshakeChallenge challenge;
    memcpy(&challenge, payload.data(), sizeof(challenge));

    // --- Generate Keys & Derive Secret ---
    uint8_t clientPrivKey[32], clientPubKey[32], sharedSecret[32];
    randombytes_buf(clientPrivKey, 32);
    crypto_scalarmult_base(clientPubKey, clientPrivKey);
    crypto_scalarmult(sharedSecret, clientPrivKey, challenge.server_pubkey);

    // --- Build Response ---
    HandshakeResponse response;
    memcpy(response.client_pubkey, clientPubKey, 32);
    response.client_version = LYFRON_VERSION;

    // HMAC-SHA512 proof
    crypto_auth_hmacsha512(response.proof, challenge.nonce, 32, sharedSecret);

    // Integrity hash
    CalculateIntegrityHash(response.integrity_hash);

    // --- Send Response ---
    if (!proto.SendPacket(PKT_HANDSHAKE_RESPONSE,
                          reinterpret_cast<uint8_t*>(&response), 
                          sizeof(response))) {
        return false;
    }

    // Store shared secret for future encryption
    memcpy(sharedSecret_, sharedSecret, 32);
    handshakeComplete_ = true;
    
    return true;
}