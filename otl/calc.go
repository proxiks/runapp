// part145/go/analytics_engine.go
// Calculates game health, player interest, server load, bans, revenue

package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"math"
	"net/http"
	"sort"
	"sync"
	"time"

	"github.com/go-redis/redis/v8"
	"github.com/prometheus/client_golang/prometheus"
)

// ─── Core Metrics ───

type GameMetrics struct {
	GameID           string    `json:"game_id"`
	GameName         string    `json:"game_name"`
	Timestamp        time.Time `json:"timestamp"`
	
	// Player counts
	OnlinePlayers    int64     `json:"online_players"`
	PeakPlayers24h   int64     `json:"peak_players_24h"`
	UniquePlayers24h int64     `json:"unique_players_24h"`
	TotalAccounts    int64     `json:"total_accounts"`
	NewPlayers24h    int64     `json:"new_players_24h"`
	ReturningPlayers int64     `json:"returning_players_24h"`
	
	// Server health
	ActiveServers    int       `json:"active_servers"`
	ServerCapacity   int       `json:"server_capacity"`
	ServerLoadPct    float64   `json:"server_load_percent"`
	AvgLatencyMs     float64   `json:"avg_latency_ms"`
	P99LatencyMs     float64   `json:"p99_latency_ms"`
	PacketLossPct    float64   `json:"packet_loss_percent"`
	RegionDistribution map[string]int64 `json:"region_distribution"`
	
	// Security
	ActiveBans       int64     `json:"active_bans"`
	Bans24h          int64     `json:"bans_24h"`
	ExploitAttempts  int64     `json:"exploit_attempts_24h"`
	CheatDetections  int64     `json:"cheat_detections_24h"`
	FalsePositives   int64     `json:"false_positives_24h"`
	
	// Engagement
	AvgSessionMin    float64   `json:"avg_session_minutes"`
	RetentionD1      float64   `json:"retention_d1"`   // Day 1
	RetentionD7      float64   `json:"retention_d7"`   // Day 7
	RetentionD30     float64   `json:"retention_d30"`  // Day 30
	PlayerScoreAvg   float64   `json:"player_score_avg"`
	ChatMessages24h  int64     `json:"chat_messages_24h"`
	Reports24h       int64     `json:"reports_24h"`
	
	// Revenue (if applicable)
	Revenue24hUSD    float64   `json:"revenue_24h_usd"`
	ARPU             float64   `json:"arpu"` // Average revenue per user
	ARPPU            float64   `json:"arppu"` // Average revenue per paying user
}

type BanCalculator struct {
	mu sync.RWMutex
	
	// Ban tracking
	BansByReason    map[string]int64 `json:"bans_by_reason"`
	BansByDuration  map[string]int64 `json:"bans_by_duration"`
	AppealsPending  int64            `json:"appeals_pending"`
	AppealsApproved int64            `json:"appeals_approved"`
	AppealsDenied   int64            `json:"appeals_denied"`
	
	// Abuse patterns
	FlyHackers      int64 `json:"fly_hackers"`
	SpeedHackers    int64 `json:"speed_hackers"`
	AimbotUsers     int64 `json:"aimbot_users"`
	WallhackUsers   int64 `json:"wallhack_users"`
	ExploitAbusers  int64 `json:"exploit_abusers"`
	ChatViolators   int64 `json:"chat_violators"`
	ServerCrashers  int64 `json:"server_crashers"`
	
	// Recidivism
	RepeatOffenders int64   `json:"repeat_offenders"`
	RecidivismRate  float64 `json:"recidivism_rate"`
}

type LyfronCalculator struct {
	redis      *redis.Client
	games      map[string]*GameMetrics
	banCalc    *BanCalculator
	mu         sync.RWMutex
	
	// Prometheus metrics
	onlinePlayers   *prometheus.GaugeVec
	serverLoad      *prometheus.GaugeVec
	banCounter      *prometheus.CounterVec
	latencyHist     *prometheus.HistogramVec
}

func NewLyfronCalculator(redisAddr string) *LyfronCalculator {
	lc := &LyfronCalculator{
		redis: redis.NewClient(&redis.Options{Addr: redisAddr, DB: 1}),
		games: make(map[string]*GameMetrics),
		banCalc: &BanCalculator{
			BansByReason:   make(map[string]int64),
			BansByDuration: make(map[string]int64),
		},
	}
	
	// Prometheus metrics
	lc.onlinePlayers = prometheus.NewGaugeVec(prometheus.GaugeOpts{
		Name: "lyfron_online_players", Help: "Current online players",
	}, []string{"game_id", "region"})
	
	lc.serverLoad = prometheus.NewGaugeVec(prometheus.GaugeOpts{
		Name: "lyfron_server_load_percent", Help: "Server load percentage",
	}, []string{"game_id", "server_id"})
	
	lc.banCounter = prometheus.NewCounterVec(prometheus.CounterOpts{
		Name: "lyfron_bans_total", Help: "Total bans by reason",
	}, []string{"game_id", "reason", "duration"})
	
	lc.latencyHist = prometheus.NewHistogramVec(prometheus.HistogramOpts{
		Name: "lyfron_latency_ms", Help: "Latency distribution",
		Buckets: []float64{10, 25, 50, 100, 250, 500, 1000},
	}, []string{"game_id", "region"})
	
	prometheus.MustRegister(lc.onlinePlayers, lc.serverLoad, lc.banCounter, lc.latencyHist)
	
	return lc
}

// ─── Real-time Calculations ───

func (lc *LyfronCalculator) CalculateGameHealth(gameID string) *GameHealthReport {
	lc.mu.RLock()
	metrics, ok := lc.games[gameID]
	lc.mu.RUnlock()
	
	if !ok {
		return nil
	}
	
	report := &GameHealthReport{
		GameID:    gameID,
		Timestamp: time.Now(),
		Overall:   "HEALTHY",
		Score:     100,
	}
	
	// Server overload check
	if metrics.ServerLoadPct > 90 {
		report.Overall = "CRITICAL"
		report.Score -= 40
		report.Alerts = append(report.Alerts, Alert{
			Severity: "CRITICAL",
			Message:  fmt.Sprintf("Server overload: %.1f%% capacity", metrics.ServerLoadPct),
			Action:   "Scale up servers immediately or enable queue system",
		})
	} else if metrics.ServerLoadPct > 75 {
		report.Overall = "WARNING"
		report.Score -= 20
		report.Alerts = append(report.Alerts, Alert{
			Severity: "WARNING",
			Message:  fmt.Sprintf("High server load: %.1f%%", metrics.ServerLoadPct),
			Action:   "Prepare auto-scaling",
		})
	}
	
	// Latency check
	if metrics.P99LatencyMs > 500 {
		report.Score -= 15
		report.Alerts = append(report.Alerts, Alert{
			Severity: "WARNING",
			Message:  fmt.Sprintf("High P99 latency: %.1fms", metrics.P99LatencyMs),
			Action:   "Check network routing and server regions",
		})
	}
	
	// Exploit wave detection
	if metrics.ExploitAttempts > 1000 {
		report.Score -= 25
		report.Alerts = append(report.Alerts, Alert{
			Severity: "CRITICAL",
			Message:  fmt.Sprintf("Exploit wave detected: %d attempts in 24h", metrics.ExploitAttempts),
			Action:   "Enable emergency anti-cheat mode, increase scan frequency",
		})
	}
	
	// Player interest calculation
	interestScore := lc.calculateInterestScore(metrics)
	report.PlayerInterest = interestScore
	
	// Revenue projection
	report.RevenueProjected30d = metrics.Revenue24hUSD * 30 * (1 + (interestScore-50)/100)
	
	// Ban effectiveness
	report.BanEffectiveness = lc.calculateBanEffectiveness(gameID)
	
	// DAU/MAU ratio (stickiness)
	if metrics.TotalAccounts > 0 {
		report.Stickiness = float64(metrics.UniquePlayers24h) / float64(metrics.TotalAccounts) * 100
	}
	
	// Normalize score
	if report.Score < 0 { report.Score = 0 }
	if report.Score > 100 { report.Score = 100 }
	
	if report.Score < 50 && report.Overall == "HEALTHY" {
		report.Overall = "DEGRADED"
	}
	
	return report
}

func (lc *LyfronCalculator) calculateInterestScore(m *GameMetrics) float64 {
	// Composite interest score 0-100
	score := 50.0 // Base
	
	// Session length factor
	score += (m.AvgSessionMin - 30) * 0.5
	
	// Retention factor
	score += m.RetentionD1 * 0.2
	score += m.RetentionD7 * 0.3
	score += m.RetentionD30 * 0.5
	
	// Growth factor
	if m.NewPlayers24h > 0 {
		growthRate := float64(m.NewPlayers24h) / float64(m.TotalAccounts) * 100
		score += growthRate * 2
	}
	
	// Engagement
	score += float64(m.ChatMessages24h) / float64(m.UniquePlayers24h) * 0.1
	
	// Security health (negative if too many exploits)
	score -= float64(m.ExploitAttempts) / 100
	
	// Normalize
	if score > 100 { score = 100 }
	if score < 0 { score = 0 }
	
	return score
}

func (lc *LyfronCalculator) calculateBanEffectiveness(gameID string) float64 {
	lc.banCalc.mu.RLock()
	defer lc.banCalc.mu.RUnlock()
	
	totalBans := int64(0)
	for _, v := range lc.banCalc.BansByReason {
		totalBans += v
	}
	
	if totalBans == 0 {
		return 100.0 // No bans needed = perfect
	}
	
	// Lower recidivism = higher effectiveness
	effectiveness := 100.0 - lc.banCalc.RecidivismRate*100
	
	// False positive penalty
	if lc.games[gameID] != nil {
		fpRate := float64(lc.games[gameID].FalsePositives) / float64(totalBans)
		effectiveness -= fpRate * 50
	}
	
	if effectiveness < 0 { effectiveness = 0 }
	return effectiveness
}

func (lc *LyfronCalculator) ProcessBan(playerID, reason, gameID string, severity int) {
	lc.banCalc.mu.Lock()
	defer lc.banCalc.mu.Unlock()
	
	// Determine ban duration based on severity and rules
	duration := lc.calculateBanDuration(reason, severity)
	
	ban := &BanRecord{
		PlayerID:  playerID,
		GameID:    gameID,
		Reason:    reason,
		Severity:  severity,
		Duration:  duration,
		IssuedAt:  time.Now(),
		ExpiresAt: time.Now().Add(duration),
	}
	
	// Update counters
	lc.banCalc.BansByReason[reason]++
	lc.banCalc.BansByDuration[duration.String()]++
	
	// Specific tracking
	switch reason {
	case "fly_hack":
		lc.banCalc.FlyHackers++
		// 4 days for fly hack
		ban.Duration = 96 * time.Hour
		ban.ExpiresAt = time.Now().Add(ban.Duration)
	case "server_abuse", "server_crash":
		lc.banCalc.ServerCrashers++
		// Permanent for server abuse
		ban.Duration = 100 * 365 * 24 * time.Hour
		ban.ExpiresAt = time.Now().Add(ban.Duration)
		ban.Permanent = true
	case "chat_bad_word":
		// 2 days for bad words
		ban.Duration = 48 * time.Hour
		ban.ExpiresAt = time.Now().Add(ban.Duration)
	case "speed_hack":
		lc.banCalc.SpeedHackers++
		ban.Duration = 168 * time.Hour // 7 days
	case "aimbot":
		lc.banCalc.AimbotUsers++
		ban.Duration = 720 * time.Hour // 30 days
	case "wallhack":
		lc.banCalc.WallhackUsers++
		ban.Duration = 720 * time.Hour
	}
	
	// Store in Redis
	banJSON, _ := json.Marshal(ban)
	lc.redis.LPush(context.Background(), fmt.Sprintf("lyfron:bans:%s", gameID), banJSON)
	
	// Update Prometheus
	lc.banCounter.WithLabelValues(gameID, reason, ban.Duration.String()).Inc()
	
	log.Printf("[CALCULATOR] Ban issued: %s | Reason: %s | Duration: %v", playerID, reason, ban.Duration)
}

func (lc *LyfronCalculator) calculateBanDuration(reason string, severity int) time.Duration {
	// Lyfron strict rules
	switch severity {
	case 1: // Minor
		return 24 * time.Hour
	case 2: // Moderate
		return 48 * time.Hour
	case 3: // Serious
		return 168 * time.Hour // 7 days
	case 4: // Severe
		return 720 * time.Hour // 30 days
	case 5: // Critical
		return 100 * 365 * 24 * time.Hour // Permanent
	default:
		return 24 * time.Hour
	}
}

// ─── Types ───

type GameHealthReport struct {
	GameID             string   `json:"game_id"`
	Timestamp          time.Time `json:"timestamp"`
	Overall            string   `json:"overall_status"`
	Score              float64  `json:"health_score"`
	PlayerInterest     float64  `json:"player_interest_score"`
	RevenueProjected30d float64 `json:"revenue_projected_30d"`
	Stickiness         float64  `json:"stickiness_percent"`
	BanEffectiveness   float64  `json:"ban_effectiveness"`
	Alerts             []Alert  `json:"alerts"`
}

type Alert struct {
	Severity string `json:"severity"`
	Message  string `json:"message"`
	Action   string `json:"recommended_action"`
}

type BanRecord struct {
	PlayerID  string        `json:"player_id"`
	GameID    string        `json:"game_id"`
	Reason    string        `json:"reason"`
	Severity  int           `json:"severity"`
	Duration  time.Duration `json:"duration"`
	IssuedAt  time.Time     `json:"issued_at"`
	ExpiresAt time.Time     `json:"expires_at"`
	Permanent bool          `json:"permanent"`
	Appealed  bool          `json:"appealed"`
	AppealStatus string     `json:"appeal_status,omitempty"`
}

// ─── HTTP API ───

func (lc *LyfronCalculator) StartAPI(addr string) {
	mux := http.NewServeMux()
	mux.HandleFunc("/v145/game/health", lc.handleGameHealth)
	mux.HandleFunc("/v145/game/metrics", lc.handleGameMetrics)
	mux.HandleFunc("/v145/ban/issue", lc.handleIssueBan)
	mux.HandleFunc("/v145/ban/stats", lc.handleBanStats)
	mux.HandleFunc("/v145/calculate/interest", lc.handleInterestCalc)
	mux.HandleFunc("/v145/calculate/revenue", lc.handleRevenueCalc)
	mux.HandleFunc("/v145/servers/status", lc.handleServerStatus)
	
	log.Printf("[CALCULATOR] Analytics API on %s", addr)
	log.Fatal(http.ListenAndServe(addr, mux))
}

func (lc *LyfronCalculator) handleGameHealth(w http.ResponseWriter, r *http.Request) {
	gameID := r.URL.Query().Get("game_id")
	if gameID == "" {
		http.Error(w, "game_id required", http.StatusBadRequest)
		return
	}
	
	report := lc.CalculateGameHealth(gameID)
	if report == nil {
		http.Error(w, "game not found", http.StatusNotFound)
		return
	}
	
	json.NewEncoder(w).Encode(report)
}

func (lc *LyfronCalculator) handleBanStats(w http.ResponseWriter, r *http.Request) {
	lc.banCalc.mu.RLock()
	defer lc.banCalc.mu.RUnlock()
	
	json.NewEncoder(w).Encode(lc.banCalc)
}
