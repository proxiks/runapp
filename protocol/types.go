package protocol

import (
	"encoding/binary"
	"errors"
	"fmt"
	"hash/crc32"
)

const (
	Magic      uint32 = 0x4C594652 // "LYFR"
	Version    uint16 = 1
	MaxPayload uint16 = 65535
)

type PacketType uint8

const (
	PktHandshakeChallenge PacketType = 0x01
	PktHandshakeResponse  PacketType = 0x02
	PktHeartbeat          PacketType = 0x03
	PktHeartbeatAck       PacketType = 0x04
	PktIntegrityReport    PacketType = 0x05
	PktIntegrityViolation PacketType = 0x06
	PktViolation          PacketType = 0xFF
)

var (
	ErrInvalidMagic      = errors.New("invalid magic")
	ErrVersionMismatch   = errors.New("version mismatch")
	ErrPayloadTooLarge   = errors.New("payload exceeds maximum")
	ErrChecksumFailed    = errors.New("checksum verification failed")
	ErrSequenceOutOfOrder = errors.New("sequence out of order")
)

type Header struct {
	Magic       uint32
	Version     uint16
	PayloadLen  uint16
	PacketType  uint8
	Sequence    uint32
	Checksum    uint32
}

func (h *Header) Size() int { return 16 } // 4+2+2+1+4+4 padded to 16

func (h *Header) Marshal() []byte {
	buf := make([]byte, 16)
	binary.LittleEndian.PutUint32(buf[0:4], h.Magic)
	binary.LittleEndian.PutUint16(buf[4:6], h.Version)
	binary.LittleEndian.PutUint16(buf[6:8], h.PayloadLen)
	buf[8] = h.PacketType
	binary.LittleEndian.PutUint32(buf[9:13], h.Sequence)
	binary.LittleEndian.PutUint32(buf[12:16], h.Checksum)
	return buf
}

func (h *Header) Unmarshal(data []byte) error {
	if len(data) < 16 {
		return errors.New("header too short")
	}
	h.Magic = binary.LittleEndian.Uint32(data[0:4])
	h.Version = binary.LittleEndian.Uint16(data[4:6])
	h.PayloadLen = binary.LittleEndian.Uint16(data[6:8])
	h.PacketType = data[8]
	h.Sequence = binary.LittleEndian.Uint32(data[9:13])
	h.Checksum = binary.LittleEndian.Uint32(data[12:16])
	return nil
}

func (h *Header) Validate() error {
	if h.Magic != Magic {
		return ErrInvalidMagic
	}
	if h.Version != Version {
		return ErrVersionMismatch
	}
	if h.PayloadLen > MaxPayload {
		return ErrPayloadTooLarge
	}
	return nil
}

func CalculateChecksum(hdr *Header, payload []byte) uint32 {
	// Clone header, zero magic and checksum for calculation
	temp := *hdr
	temp.Magic = 0
	temp.Checksum = 0

	data := temp.Marshal()
	
	table := crc32.MakeTable(crc32.IEEE)
	crc := crc32.ChecksumIEEE(data)
	if len(payload) > 0 {
		crc = crc32.Update(crc, table, payload)
	}
	return crc
}

func VerifyChecksum(hdr *Header, payload []byte) error {
	expected := CalculateChecksum(hdr, payload)
	if expected != hdr.Checksum {
		return ErrChecksumFailed
	}
	return nil
}

// Typed payload helpers
type HandshakeChallenge struct {
	Nonce        [32]byte
	Timestamp    uint64
	ServerPubKey [32]byte
}

type HandshakeResponse struct {
	ClientPubKey  [32]byte
	Proof         [64]byte
	ClientVersion uint32
	IntegrityHash [32]byte
}

type HeartbeatPayload struct {
	ClientTimestamp uint64
	SessionToken    [16]byte
	CPULoad         uint16
	MemUsage        uint16
}

type IntegrityReport struct {
	SectionHash  [32]byte
	SectionCount uint32
	TickCount    uint32
}

type ViolationReport struct {
	ViolationType uint8
	ViolationCode uint32
	Context       [56]byte
}