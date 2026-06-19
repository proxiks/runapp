package main

import (
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"log"
	"net"
	"time"

	"lyfron/server/internal/auth"
	"lyfron/server/internal/protocol"
)

func main() {
	expectedHashHex := "0000000000000000000000000000000000000000000000000000000000000000"
	var expectedHash [32]byte
	decoded, _ := hex.DecodeString(expectedHashHex)
	copy(expectedHash[:], decoded)

	hm, err := auth.NewHandshakeManager(expectedHash)
	if err != nil {
		log.Fatal(err)
	}

	listener, err := net.Listen("tcp", ":13337")
	if err != nil {
		log.Fatal(err)
	}
	fmt.Println("Lyfron Server v1 listening on :13337")

	for {
		conn, err := listener.Accept()
		if err != nil {
			continue
		}
		go handleClient(conn, hm)
	}
}

func handleClient(conn net.Conn, hm *auth.HandshakeManager) {
	defer conn.Close()

	engine := protocol.NewEngine(conn)
	engine.SetDeadline(time.Now().Add(30 * time.Second))

	// --- Step 1: Send Challenge ---
	challenge, err := hm.GenerateChallenge()
	if err != nil {
		log.Printf("Failed to generate challenge: %v", err)
		return
	}

	challengePayload := make([]byte, 72) // 32 + 8 + 32
	copy(challengePayload[0:32], challenge.Nonce[:])
	binary.LittleEndian.PutUint64(challengePayload[32:40], challenge.Timestamp)
	copy(challengePayload[40:72], challenge.ServerPubKey[:])

	if err := engine.SendPacket(protocol.PktHandshakeChallenge, challengePayload); err != nil {
		log.Printf("Failed to send challenge: %v", err)
		return
	}

	// --- Step 2: Receive Response ---
	hdr, payload, err := engine.RecvPacket(10 * time.Second)
	if err != nil {
		log.Printf("Handshake failed: %v", err)
		return
	}

	if hdr.PacketType != uint8(protocol.PktHandshakeResponse) {
		log.Printf("Expected handshake response, got %d", hdr.PacketType)
		return
	}

	if len(payload) != 132 { // 32 + 64 + 4 + 32
		log.Printf("Invalid response size: %d", len(payload))
		return
	}

	var response auth.HandshakeResponse
	copy(response.ClientPubKey[:], payload[0:32])
	copy(response.Proof[:], payload[32:96])
	response.ClientVersion = binary.LittleEndian.Uint32(payload[96:100])
	copy(response.IntegrityHash[:], payload[100:132])

	// --- Step 3: Verify ---
	session, err := hm.VerifyResponse(&challenge, &response)
	if err != nil {
		log.Printf("Handshake verification failed: %v", err)
		// Send violation and disconnect
		violation := []byte{0x01, 0x00, 0x00, 0x00, 0x00} // type + code
		engine.SendPacket(protocol.PktViolation, violation)
		return
	}

	log.Printf("Client authenticated. Session established.")
	session.LastHeartbeat = time.Now()

	// --- Step 4: Enter main loop ---
	for {
		hdr, payload, err := engine.RecvPacket(30 * time.Second)
		if err != nil {
			log.Printf("Client disconnected: %v", err)
			return
		}

		switch protocol.PacketType(hdr.PacketType) {
		case protocol.PktHeartbeat:
			session.LastHeartbeat = time.Now()
			// Send ACK
			ackPayload := make([]byte, 8)
			binary.LittleEndian.PutUint64(ackPayload, uint64(time.Now().UnixMilli()))
			engine.SendPacket(protocol.PktHeartbeatAck, ackPayload)

		case protocol.PktIntegrityReport:
			log.Printf("Received integrity report: %x", payload[:32])
			// TODO: Validate section hashes

		case protocol.PktIntegrityViolation:
			log.Printf("INTEGRITY VIOLATION from client: %x", payload)
			// Immediate ban logic here
			return

		default:
			log.Printf("Unknown packet type: %d", hdr.PacketType)
		}
	}
}