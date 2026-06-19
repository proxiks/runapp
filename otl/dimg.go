// part144/go/forensics_engine.go
// Detects document/image tampering, edits, deepfakes

package main

import (
	"bytes"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"image"
	"image/jpeg"
	"image/png"
	"io"
	"log"
	"net/http"
	"os"
	"os/exec"
	"regexp"
	"strings"
	"time"

	"github.com/google/go-tiff"
	"github.com/h2non/filetype"
)

// ─── Forensics Types ───

type DocumentAnalysis struct {
	FilePath      string            `json:"file_path"`
	FileHash      string            `json:"file_hash_sha256"`
	FileType      string            `json:"file_type"`
	FileSize      int64             `json:"file_size_bytes"`
	
	// Metadata forensics
	Metadata      map[string]string `json:"metadata"`
	SoftwareUsed  []string          `json:"software_detected"`
	CreationTime  time.Time         `json:"creation_time"`
	ModifiedTime  time.Time         `json:"modified_time"`
	EditHistory   []EditEvent       `json:"edit_history"`
	
	// Tamper detection
	IsTampered    bool              `json:"is_tampered"`
	TamperScore   float64           `json:"tamper_score_0_to_1"`
	TamperRegions []Region          `json:"tamper_regions"`
	Confidence    float64           `json:"confidence"`
	
	// Image-specific
	ELAImage      string            `json:"ela_image_path,omitempty"` // Error Level Analysis
	NoiseAnalysis map[string]float64 `json:"noise_analysis,omitempty"`
	CopyMoveDetect []Region         `json:"copy_move_detections,omitempty"`
	
	// Document-specific
	TextLayers    []TextLayer       `json:"text_layers,omitempty"`
	FontAnalysis  []FontInfo        `json:"font_analysis,omitempty"`
	RevisionHistory []Revision      `json:"revision_history,omitempty"`
}

type EditEvent struct {
	Timestamp   time.Time `json:"timestamp"`
	Software    string    `json:"software"`
	Action      string    `json:"action"`
	Layer       string    `json:"layer,omitempty"`
	Confidence  float64   `json:"confidence"`
}

type Region struct {
	X, Y, W, H  int     `json:"x,y,w,h"`
	Confidence  float64 `json:"confidence"`
	Type        string  `json:"type"` // "splice", "copy-move", "noise", "ela"
}

type TextLayer struct {
	Content     string  `json:"content"`
	Font        string  `json:"font"`
	Size        float64 `json:"size"`
	Position    [2]float64 `json:"position"`
	Created     time.Time `json:"created"`
	Modified    time.Time `json:"modified"`
}

type FontInfo struct {
	Name        string  `json:"name"`
	Embedded    bool    `json:"embedded"`
	Standard    bool    `json:"is_standard"`
	Suspicious  bool    `json:"is_suspicious"`
}

type Revision struct {
	Author      string    `json:"author"`
	Timestamp   time.Time `json:"timestamp"`
	Changes     string    `json:"changes"`
	Redacted    bool      `json:"contains_redaction"`
}

// ─── Forensics Engine ───

type ForensicsEngine struct {
	knownSoftware map[string]string // signature -> software name
	suspiciousFonts map[string]bool
}

func NewForensicsEngine() *ForensicsEngine {
	return &ForensicsEngine{
		knownSoftware: map[string]string{
			"Adobe Photoshop": "Photoshop",
			"GIMP": "GIMP",
			"Paint.NET": "Paint.NET",
			"Canva": "Canva",
			"Microsoft Word": "Word",
			"Microsoft Excel": "Excel",
			"LibreOffice": "LibreOffice",
			"Adobe Acrobat": "Acrobat",
			"Foxit": "Foxit Reader",
		},
		suspiciousFonts: map[string]bool{
			"Wingdings": false,
			"Symbol": false,
			"CustomFont_Hacked": true,
			"FakeArial": true,
		},
	}
}

func (fe *ForensicsEngine) AnalyzeDocument(filePath string) (*DocumentAnalysis, error) {
	file, err := os.Open(filePath)
	if err != nil {
		return nil, err
	}
	defer file.Close()
	
	stat, _ := file.Stat()
	
	// Compute hash
	hasher := sha256.New()
	io.Copy(hasher, file)
	file.Seek(0, 0)
	
	analysis := &DocumentAnalysis{
		FilePath: filePath,
		FileHash: hex.EncodeToString(hasher.Sum(nil)),
		FileSize: stat.Size(),
		Metadata: make(map[string]string),
	}
	
	// Detect file type
	head := make([]byte, 8192)
	file.Read(head)
	file.Seek(0, 0)
	
	kind, _ := filetype.Match(head)
	if kind != filetype.Unknown {
		analysis.FileType = kind.MIME.Value
	}
	
	// Route to specific analyzer
	switch {
	case strings.Contains(analysis.FileType, "image"):
		return fe.analyzeImage(file, analysis)
	case strings.Contains(analysis.FileType, "pdf"):
		return fe.analyzePDF(file, analysis)
	case strings.Contains(analysis.FileType, "officedocument") || strings.Contains(analysis.FileType, "ms-"):
		return fe.analyzeOffice(file, analysis)
	case strings.Contains(analysis.FileType, "text"):
		return fe.analyzeText(file, analysis)
	default:
		return fe.analyzeGeneric(file, analysis)
	}
}

func (fe *ForensicsEngine) analyzeImage(file *os.File, analysis *DocumentAnalysis) (*DocumentAnalysis, error) {
	// Decode image
	img, format, err := image.Decode(file)
	if err != nil {
		return analysis, err
	}
	
	bounds := img.Bounds()
	
	// ─── Error Level Analysis (ELA) ───
	elaScore, elaRegions := fe.errorLevelAnalysis(file, format)
	analysis.TamperScore = elaScore
	analysis.TamperRegions = elaRegions
	
	// ─── Metadata extraction ───
	if format == "jpeg" {
		file.Seek(0, 0)
		fe.extractJPEGMetadata(file, analysis)
	} else if format == "png" {
		fe.extractPNGMetadata(file, analysis)
	}
	
	// ─── Noise analysis ───
	analysis.NoiseAnalysis = fe.analyzeNoise(img)
	
	// ─── Copy-move detection ───
	analysis.CopyMoveDetect = fe.detectCopyMove(img)
	
	// ─── Software detection from metadata ───
	for sig, software := range fe.knownSoftware {
		if strings.Contains(fmt.Sprintf("%v", analysis.Metadata), sig) {
			analysis.SoftwareUsed = append(analysis.SoftwareUsed, software)
		}
	}
	
	// Determine tampered
	analysis.IsTampered = analysis.TamperScore > 0.6 || len(analysis.TamperRegions) > 3
	
	return analysis, nil
}

func (fe *ForensicsEngine) errorLevelAnalysis(file *os.File, format string) (float64, []Region) {
	// ELA: Re-save image at known quality, compare differences
	// High error in regions = likely tampering
	
	file.Seek(0, 0)
	img, _, err := image.Decode(file)
	if err != nil {
		return 0, nil
	}
	
	// Save to buffer at fixed quality
	var buf bytes.Buffer
	if format == "jpeg" {
		jpeg.Encode(&buf, img, &jpeg.Options{Quality: 95})
	} else {
		png.Encode(&buf, img)
	}
	
	// Decode re-saved
	reloaded, _, _ := image.Decode(&buf)
	if reloaded == nil {
		return 0, nil
	}
	
	bounds := img.Bounds()
	diffSum := 0.0
	pixelCount := 0
	var suspiciousRegions []Region
	
	// Compare pixel by pixel
	for y := bounds.Min.Y; y < bounds.Max.Y; y++ {
		for x := bounds.Min.X; x < bounds.Max.X; x++ {
			r1, g1, b1, _ := img.At(x, y).RGBA()
			r2, g2, b2, _ := reloaded.At(x, y).RGBA()
			
			// Error level
			dr := float64(r1) - float64(r2)
			dg := float64(g1) - float64(g2)
			db := float64(b1) - float64(b2)
			diff := (abs(dr) + abs(dg) + abs(db)) / 3.0
			
			diffSum += diff
			pixelCount++
			
			// Track high-error regions
			if diff > 5000 { // Threshold
				suspiciousRegions = append(suspiciousRegions, Region{
					X: x, Y: y, W: 1, H: 1,
					Confidence: min(diff/65535, 1.0),
					Type: "ela",
				})
			}
		}
	}
	
	avgError := diffSum / float64(pixelCount)
	// Normalize to 0-1
	score := min(avgError/5000, 1.0)
	
	// Cluster suspicious regions
	clustered := fe.clusterRegions(suspiciousRegions, 20)
	
	return score, clustered
}

func (fe *ForensicsEngine) analyzeNoise(img image.Image) map[string]float64 {
	// Local noise variance analysis
	// Tampered regions often have different noise characteristics
	
	bounds := img.Bounds()
	width := bounds.Dx()
	height := bounds.Dy()
	
	// Split into blocks
	blockSize := 16
	noiseMap := make([]float64, 0)
	
	for y := 0; y < height-blockSize; y += blockSize {
		for x := 0; x < width-blockSize; x += blockSize {
			// Calculate local variance
			variance := fe.localVariance(img, x, y, blockSize)
			noiseMap = append(noiseMap, variance)
		}
	}
	
	// Statistics
	mean := 0.0
	for _, v := range noiseMap {
		mean += v
	}
	mean /= float64(len(noiseMap))
	
	variance := 0.0
	for _, v := range noiseMap {
		variance += (v - mean) * (v - mean)
	}
	variance /= float64(len(noiseMap))
	
	return map[string]float64{
		"mean_noise_variance": mean,
		"noise_std_dev":       math.Sqrt(variance),
		"max_noise":           maxFloat(noiseMap),
		"min_noise":           minFloat(noiseMap),
	}
}

func (fe *ForensicsEngine) detectCopyMove(img image.Image) []Region {
	// Detect copy-move forgery: identical regions that shouldn't be
	// Simplified: block matching with DCT coefficients
	
	// In production: use robust matching, invariant features
	// For now, return placeholder
	
	return []Region{}
}

func (fe *ForensicsEngine) analyzePDF(file *os.File, analysis *DocumentAnalysis) (*DocumentAnalysis, error) {
	// Use external tools (pdfinfo, exiftool, pdftotext)
	
	// Extract text layers
	cmd := exec.Command("pdftotext", "-layout", analysis.FilePath, "-")
	output, _ := cmd.Output()
	text := string(output)
	
	// Check for text inconsistencies
	analysis.TextLayers = fe.extractTextLayers(text)
	
	// Extract metadata with pdfinfo
	cmd = exec.Command("pdfinfo", analysis.FilePath)
	infoOutput, _ := cmd.Output()
	
	fe.parsePDFInfo(string(infoOutput), analysis)
	
	// Check for JavaScript (common exploit vector)
	if strings.Contains(text, "/JavaScript") || strings.Contains(text, "/JS") {
		analysis.Metadata["javascript_embedded"] = "true"
		analysis.TamperScore += 0.3
	}
	
	// Check for embedded files
	if strings.Contains(text, "/EmbeddedFiles") {
		analysis.Metadata["embedded_files"] = "true"
	}
	
	// Font analysis
	analysis.FontAnalysis = fe.analyzePDFFonts(analysis.FilePath)
	
	return analysis, nil
}

func (fe *ForensicsEngine) analyzeOffice(file *os.File, analysis *DocumentAnalysis) (*DocumentAnalysis, error) {
	// DOCX/XLSX/PPTX are ZIP files with XML inside
	
	// Extract revision history
	// Check for track changes
	// Compare creation vs modification times
	
	// Use unzip to inspect internals
	cmd := exec.Command("unzip", "-l", analysis.FilePath)
	output, _ := cmd.Output()
	
	content := string(output)
	
	// Detect editing software
	if strings.Contains(content, "word/document.xml") {
		analysis.SoftwareUsed = append(analysis.SoftwareUsed, "Microsoft Word")
	}
	if strings.Contains(content, "docProps/core.xml") {
		// Extract core properties
		fe.extractOfficeCore(file, analysis)
	}
	
	return analysis, nil
}

func (fe *ForensicsEngine) extractOfficeCore(file *os.File, analysis *DocumentAnalysis) {
	// Extract docProps/core.xml from ZIP
	// Parse creation date, modification date, author, etc.
}

func (fe *ForensicsEngine) analyzeText(file *os.File, analysis *DocumentAnalysis) (*DocumentAnalysis, error) {
	// Plain text - check for hidden characters, zero-width spaces
	content, _ := io.ReadAll(file)
	
	// Detect zero-width characters (steganography)
	zwChars := []rune{'\u200B', '\u200C', '\u200D', '\uFEFF'}
	zwCount := 0
	for _, r := range string(content) {
		for _, zw := range zwChars {
			if r == zw {
				zwCount++
			}
		}
	}
	
	if zwCount > 0 {
		analysis.Metadata["zero_width_chars"] = fmt.Sprintf("%d", zwCount)
		analysis.TamperScore += min(float64(zwCount)*0.01, 0.3)
	}
	
	// Detect homoglyph attacks
	homoglyphs := fe.detectHomoglyphs(string(content))
	if len(homoglyphs) > 0 {
		analysis.Metadata["homoglyph_attack"] = "true"
		analysis.TamperScore += 0.4
	}
	
	return analysis, nil
}

func (fe *ForensicsEngine) detectHomoglyphs(text string) []string {
	// Detect Unicode homoglyphs (e.g., Cyrillic а vs Latin a)
	homoglyphMap := map[rune]rune{
		'а': 'a', // Cyrillic
		'е': 'e',
		'о': 'o',
		'р': 'p',
		'с': 'c',
	}
	
	found := []string{}
	for _, r := range text {
		if latin, ok := homoglyphMap[r]; ok {
			found = append(found, fmt.Sprintf("%c->%c", r, latin))
		}
	}
	
	return found
}

// ─── Helpers ───

func (fe *ForensicsEngine) clusterRegions(regions []Region, threshold int) []Region {
	// Simple clustering of nearby regions
	if len(regions) == 0 {
		return regions
	}
	
	// Group by proximity
	clusters := [][]Region{{regions[0]}}
	
	for i := 1; i < len(regions); i++ {
		placed := false
		for j, cluster := range clusters {
			last := cluster[len(cluster)-1]
			if abs(regions[i].X-last.X) < threshold && abs(regions[i].Y-last.Y) < threshold {
				clusters[j] = append(clusters[j], regions[i])
				placed = true
				break
			}
		}
		if !placed {
			clusters = append(clusters, []Region{regions[i]})
		}
	}
	
	// Return bounding boxes
	result := make([]Region, len(clusters))
	for i, cluster := range clusters {
		minX, minY := cluster[0].X, cluster[0].Y
		maxX, maxY := cluster[0].X, cluster[0].Y
		totalConf := 0.0
		
		for _, r := range cluster {
			if r.X < minX { minX = r.X }
			if r.Y < minY { minY = r.Y }
			if r.X > maxX { maxX = r.X }
			if r.Y > maxY { maxY = r.Y }
			totalConf += r.Confidence
		}
		
		result[i] = Region{
			X: minX, Y: minY,
			W: maxX - minX + 1,
			H: maxY - minY + 1,
			Confidence: totalConf / float64(len(cluster)),
			Type: "tamper_cluster",
		}
	}
	
	return result
}

func abs(x float64) float64 {
	if x < 0 { return -x }
	return x
}

func min(a, b float64) float64 {
	if a < b { return a }
	return b
}

func maxFloat(arr []float64) float64 {
	max := arr[0]
	for _, v := range arr {
		if v > max { max = v }
	}
	return max
}

func minFloat(arr []float64) float64 {
	min := arr[0]
	for _, v := range arr {
		if v < min { min = v }
	}
	return min
}

func (fe *ForensicsEngine) extractJPEGMetadata(file *os.File, analysis *DocumentAnalysis) {
	// Parse EXIF data
}

func (fe *ForensicsEngine) extractPNGMetadata(file *os.File, analysis *DocumentAnalysis) {
	// Parse PNG chunks (tEXt, zTXt, iTXt)
}

func (fe *ForensicsEngine) parsePDFInfo(info string, analysis *DocumentAnalysis) {
	lines := strings.Split(info, "\n")
	for _, line := range lines {
		parts := strings.SplitN(line, ":", 2)
		if len(parts) == 2 {
			key := strings.TrimSpace(parts[0])
			val := strings.TrimSpace(parts[1])
			analysis.Metadata[key] = val
		}
	}
}

func (fe *ForensicsEngine) extractTextLayers(text string) []TextLayer {
	// Parse text into layers based on formatting
	return []TextLayer{}
}

func (fe *ForensicsEngine) analyzePDFFonts(path string) []FontInfo {
	// Use pdffonts command
	cmd := exec.Command("pdffonts", path)
	output, _ := cmd.Output()
	
	fonts := []FontInfo{}
	lines := strings.Split(string(output), "\n")
	
	for i, line := range lines {
		if i < 2 { continue } // Skip header
		fields := strings.Fields(line)
		if len(fields) >= 5 {
			fonts = append(fonts, FontInfo{
				Name: fields[0],
				Embedded: strings.Contains(line, "yes"),
				Standard: strings.Contains(line, "Times") || strings.Contains(line, "Helvetica") || strings.Contains(line, "Courier"),
			})
		}
	}
	
	return fonts
}

func (fe *ForensicsEngine) localVariance(img image.Image, x, y, size int) float64 {
	// Calculate variance of pixel values in block
	return 0.0 // Placeholder
}

func (fe *ForensicsEngine) analyzeGeneric(file *os.File, analysis *DocumentAnalysis) (*DocumentAnalysis, error) {
	return analysis, nil
}