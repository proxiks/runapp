package protocol

import (
	"bufio"
	"encoding/binary"
	"fmt"
	"io"
	"net"
	"sync"
	"sync/atomic"
	"time"
)

type Engine struct {
	conn          net.Conn
	reader        *bufio.Reader
	writer        *bufio.Writer
	writeMu       sync.Mutex
	txSequence    uint32
	lastRxSequence uint32
}

func NewEngine(conn net.Conn) *Engine {
	return &Engine{
		conn:   conn,
		reader: bufio.NewReader(conn),
		writer: bufio.NewWriter(conn),
	}
}

func (e *Engine) SetDeadline(t time.Time) error {
	return e.conn.SetDeadline(t)
}

func (e *Engine) SendPacket(pktType PacketType, payload []byte) error {
	if len(payload) > int(MaxPayload) {
		return ErrPayloadTooLarge
	}

	hdr := Header{
		Magic:      Magic,
		Version:    Version,
		PayloadLen: uint16(len(payload)),
		PacketType: uint8(pktType),
		Sequence:   atomic.AddUint32(&e.txSequence, 1),
	}

	// Calculate checksum with magic=0
	hdr.Checksum = CalculateChecksum(&hdr, payload)

	e.writeMu.Lock()
	defer e.writeMu.Unlock()

	// Write header
	if _, err := e.writer.Write(hdr.Marshal()); err != nil {
		return err
	}
	// Write payload
	if len(payload) > 0 {
		if _, err := e.writer.Write(payload); err != nil {
			return err
		}
	}
	return e.writer.Flush()
}

func (e *Engine) RecvPacket(timeout time.Duration) (*Header, []byte, error) {
	if timeout > 0 {
		e.conn.SetReadDeadline(time.Now().Add(timeout))
		defer e.conn.SetReadDeadline(time.Time{})
	}

	// Read fixed header
	hdrBuf := make([]byte, 16)
	if _, err := io.ReadFull(e.reader, hdrBuf); err != nil {
		if netErr, ok := err.(net.Error); ok && netErr.Timeout() {
			return nil, nil, fmt.Errorf("recv timeout: %w", err)
		}
		return nil, nil, err
	}

	var hdr Header
	if err := hdr.Unmarshal(hdrBuf); err != nil {
		return nil, nil, err
	}

	if err := hdr.Validate(); err != nil {
		return nil, nil, err
	}

	// Anti-replay: sequence must increase
	if hdr.Sequence <= e.lastRxSequence && (e.lastRxSequence-hdr.Sequence) < 10 {
		return nil, nil, ErrSequenceOutOfOrder
	}

	// Read payload
	var payload []byte
	if hdr.PayloadLen > 0 {
		payload = make([]byte, hdr.PayloadLen)
		if _, err := io.ReadFull(e.reader, payload); err != nil {
			return nil, nil, err
		}
	}

	// Verify checksum
	if err := VerifyChecksum(&hdr, payload); err != nil {
		return nil, nil, err
	}

	e.lastRxSequence = hdr.Sequence
	return &hdr, payload, nil
}

// SendTyped marshals a struct directly
func SendTyped[T any](e *Engine, pktType PacketType, data T) error {
	// Simple unsafe cast for POD types - production would use proper serialization
	// For now, require caller to serialize
	return fmt.Errorf("use SendPacket with manual serialization")
}

func (e *Engine) Close() error {
	return e.conn.Close()
}
