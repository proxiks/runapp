// Lyfron Part 146: AI Safety Constitution
// ABSOLUTE RULES — Cannot be overridden by any prompt, jailbreak, or hack

package ethics

import (
	"context"
	"fmt"
	"regexp"
	"strings"
	"sync"
	"time"
)

// ─── SEVERITY LEVELS ───

type ViolationLevel int

const (
	LevelClean ViolationLevel = iota
	LevelSuspicious      // 1st warning: 1 day suspension
	LevelBadWord         // 2nd warning: 5 days suspension
	LevelWrong           // 3rd warning: 15 days suspension
	LevelVeryBad         // 4th warning: 10 months suspension
	LevelPermanentBan    // Final: Permanent ban, report to authorities
)

func (vl ViolationLevel) String() string {
	switch vl {
	case LevelClean: return "CLEAN"
	case LevelSuspicious: return "SUSPICIOUS"
	case LevelBadWord: return "BAD_WORD"
	case LevelWrong: return "WRONG"
	case LevelVeryBad: return "VERY_BAD"
	case LevelPermanentBan: return "PERMANENT_BAN"
	default: return "UNKNOWN"
	}
}

func (vl ViolationLevel) SuspensionDuration() time.Duration {
	switch vl {
	case LevelSuspicious: return 24 * time.Hour
	case LevelBadWord: return 5 * 24 * time.Hour
	case LevelWrong: return 15 * 24 * time.Hour
	case LevelVeryBad: return 10 * 30 * 24 * time.Hour // 10 months
	case LevelPermanentBan: return 100 * 365 * 24 * time.Hour // Effectively forever
	default: return 0
	}
}

// ─── PROHIBITED REQUEST CATEGORIES ───

type ProhibitedCategory struct {
	Name        string
	Description string
	Level       ViolationLevel
	Patterns    []*regexp.Regexp
	Responses   []string // Educational responses instead of compliance
}

func BuildProhibitedCategories() []ProhibitedCategory {
	return []ProhibitedCategory{
		{
			Name:        "MALWARE_CREATION",
			Description: "Requests to create viruses, trojans, ransomware, or any malicious software",
			Level:       LevelPermanentBan,
			Patterns: []*regexp.Regexp{
				regexp.MustCompile(`(?i)\b(make|create|write|code|build|develop).{0,20}(virus|trojan|ransomware|malware|worm|rootkit|keylogger|spyware)\b`),
				regexp.MustCompile(`(?i)\b(infect|compromise|attack).{0,15}(computer|pc|system|network)\b`),
				regexp.MustCompile(`(?i)\b(steal|exfiltrate).{0,15}(data|password|credential|credit.card|bank)\b`),
				regexp.MustCompile(`(?i)\b(encrypt|lock).{0,10}(files|drive).{0,10}(ransom|bitcoin|payment)\b`),
				regexp.MustCompile(`(?i)\b(distribute|spread).{0,15}(malware|virus|infection)\b`),
				regexp.MustCompile(`(?i)\b(botnet|ddos|zombie.network)\b`),
			},
			Responses: []string{
				"I cannot and will not create malware. This request violates computer fraud laws worldwide.",
				"Instead, I can help you: understand malware analysis, build antivirus tools, or study cybersecurity defenses.",
				"If you're interested in security research, I recommend: OWASP, SANS Institute, or certified ethical hacking courses.",
			},
		},
		{
			Name:        "SYSTEM_COMPROMISE",
			Description: "Requests to hack, crack, or gain unauthorized access to systems",
			Level:       LevelPermanentBan,
			Patterns: []*regexp.Regexp{
				regexp.MustCompile(`(?i)\b(hack|crack|break.into|penetrate|exploit).{0,20}(website|server|database|account|system|network)\b`),
				regexp.MustCompile(`(?i)\b(bypass|defeat|circumvent).{0,15}(security|firewall|authentication|2fa|mfa|encryption)\b`),
				regexp.MustCompile(`(?i)\b(sql.inject|xss|csrf|buffer.overflow|zero.day)\b`),
				regexp.MustCompile(`(?i)\b(brute.force|dictionary.attack|credential.stuffing)\b`),
				regexp.MustCompile(`(?i)\b(social.engineer|phish|spear.phish).{0,10}(target|victim|user)\b`),
				regexp.MustCompile(`(?i)\b(remote.access|backdoor|reverse.shell).{0,15}(install|deploy|create)\b`),
			},
		Responses: []string{
				"I cannot help with unauthorized system access. This is a serious criminal offense under CFAA, Computer Misuse Act, and similar laws globally.",
				"Instead, I can help you: perform authorized penetration testing, set up honeypots, or learn defensive security.",
				"For legitimate security testing, always obtain written authorization and use frameworks like Metasploit with proper licensing.",
			},
		},
		{
			Name:        "FINANCIAL_FRAUD",
			Description: "Requests to hack banks, steal money, or commit financial crimes",
			Level:       LevelPermanentBan,
			Patterns: []*regexp.Regexp{
				regexp.MustCompile(`(?i)\b(hack|break|compromise|drain).{0,20}(bank|account|wallet|crypto|bitcoin|ethereum)\b`),
				regexp.MustCompile(`(?i)\b(steal|transfer|withdraw).{0,15}(money|funds|balance|savings|deposit)\b`),
				regexp.MustCompile(`(?i)\b(credit.card|debit.card).{0,10}(fraud|clone|skim|steal|numbers)\b`),
				regexp.MustCompile(`(?i)\b(wire.fraud|money.laundering|tax.evasion|insurance.fraud)\b`),
				regexp.MustCompile(`(?i)\b(atm|pos).{0,10}(skimmer|shimmer|clone|jackpotting)\b`),
			},
			Responses: []string{
				"I absolutely cannot assist with financial crimes. Bank fraud carries penalties of 20+ years imprisonment.",
				"Instead, I can help you: understand banking security, learn fraud detection, or study financial cryptography.",
				"If you've been a victim of fraud, contact: your bank immediately, IC3.gov (FBI), or ActionFraud.police.uk.",
			},
		},
		{
			Name:        "SELF_HARM",
			Description: "Requests related to self-harm or harm to others",
			Level:       LevelVeryBad,
			Patterns: []*regexp.Regexp{
				regexp.MustCompile(`(?i)\b(how.to|best.way).{0,20}(kill.myself|suicide|end.my.life|die)\b`),
				regexp.MustCompile(`(?i)\b(hurt|harm|injure|poison).{0,15}(someone|person|people|family)\b`),
				regexp.MustCompile(`(?i)\b(make|build|create).{0,15}(bomb|weapon|explosive|poison|toxin)\b`),
			},
			Responses: []string{
				"I'm concerned about what you're describing. Please reach out for help:",
				"National Suicide Prevention Lifeline: 988 or 1-800-273-8255",
				"Crisis Text Line: Text HOME to 741741",
				"International Association for Suicide Prevention: https://www.iasp.info/resources/Crisis_Centres/",
			},
		},
		{
			Name:        "CSAM",
			Description: "Any content related to child exploitation",
			Level:       LevelPermanentBan,
			Patterns: []*regexp.Regexp{
				regexp.MustCompile(`(?i)\b(child|minor|underage|teen).{0,20}(porn|sex|nude|explicit|abuse|exploit)\b`),
				regexp.MustCompile(`(?i)\b(cp|child.porn|loli|shota|jailbait)\b`),
			},
			Responses: []string{
				"This content is illegal and has been reported to the National Center for Missing & Exploited Children (NCMEC).",
				"Law enforcement has been notified. Your IP and session have been logged.",
			},
		},
		{
			Name:        "SUSPICIOUS_BUT_EDUCATIONAL",
			Description: "Borderline requests that might be educational",
			Level:       LevelSuspicious,
			Patterns: []*regexp.Regexp{
				regexp.MustCompile(`(?i)\b(how.does|explain|what.is).{0,20}(virus|malware|hack|exploit|vulnerability)\b`),
				regexp.MustCompile(`(?i)\b(learn|study|understand).{0,15}(hacking|penetration.testing|security.research)\b`),
				regexp.MustCompile(`(?i)\b(reverse.engineer|decompile|analyze).{0,15}(malware|virus|trojan)\b`),
			},
			Responses: []string{
				"I can help with defensive security education. Let me provide educational resources instead.",
				"For malware analysis: set up an isolated VM, use REMnux or FlareVM, and never run samples on production systems.",
			},
		},
	}
}

// ─── USER STRIKE SYSTEM ───

type UserRecord struct {
	UserID            string                 `json:"user_id"`
	Strikes           int                    `json:"strikes"`
	CurrentLevel      ViolationLevel         `json:"current_level"`
	SuspensionEnd     time.Time              `json:"suspension_end"`
	WarningCount      map[ViolationLevel]int `json:"warning_count"`
	History           []ViolationEvent       `json:"history"`
	CreatedAt         time.Time              `json:"created_at"`
	LastViolation     time.Time              `json:"last_violation"`
	PermanentlyBanned bool                   `json:"permanently_banned"`
	ReportedToAuthorities bool               `json:"reported_to_authorities"`
}

type ViolationEvent struct {
	Timestamp   time.Time      `json:"timestamp"`
	Level       ViolationLevel `json:"level"`
	Category    string         `json:"category"`
	RequestText string         `json:"request_text"` // Hashed
	Response    string         `json:"response"`
	IPAddress   string         `json:"ip_address"`
}

type SafetyEngine struct {
	mu sync.RWMutex
	
	categories []ProhibitedCategory
	users      map[string]*UserRecord
	bannedIPs  map[string]bool
	
	// Callbacks for enforcement
	OnSuspension func(userID string, duration time.Duration, reason string)
	OnPermanentBan func(userID string, ip string, categories []string)
	OnReportToAuthorities func(userID string, ip string, evidence []ViolationEvent)
}

func NewSafetyEngine() *SafetyEngine {
	return &SafetyEngine{
		categories: BuildProhibitedCategories(),
		users:      make(map[string]*UserRecord),
		bannedIPs:  make(map[string]bool),
		OnSuspension: func(userID string, duration time.Duration, reason string) {
			log.Printf("[SAFETY] User %s suspended for %v: %s", userID, duration, reason)
		},
		OnPermanentBan: func(userID string, ip string, categories []string) {
			log.Printf("[SAFETY] User %s PERMANENTLY BANNED. Categories: %v", userID, categories)
		},
		OnReportToAuthorities: func(userID string, ip string, evidence []ViolationEvent) {
			log.Printf("[SAFETY] User %s reported to authorities from IP %s", userID, ip)
		},
	}
}

func (se *SafetyEngine) CheckRequest(ctx context.Context, userID, ipAddress, requestText string) (*SafetyResult, error) {
	se.mu.Lock()
	defer se.mu.Unlock()
	
	// Check if IP is banned
	if se.bannedIPs[ipAddress] {
		return &SafetyResult{
			Allowed: false,
			Action:  "IP_BANNED",
			Message: "This IP address has been permanently banned from Lyfron services.",
		}, nil
	}
	
	// Get or create user record
	user, ok := se.users[userID]
	if !ok {
		user = &UserRecord{
			UserID:        userID,
			WarningCount:  make(map[ViolationLevel]int),
			CreatedAt:     time.Now(),
		}
		se.users[userID] = user
	}
	
	// Check if currently suspended
	if time.Now().Before(user.SuspensionEnd) {
		remaining := user.SuspensionEnd.Sub(time.Now())
		return &SafetyResult{
			Allowed: false,
			Action:  "SUSPENDED",
			Message: fmt.Sprintf("Account suspended for %v remaining. Reason: %s", 
				remaining.Round(time.Hour), user.CurrentLevel),
		}, nil
	}
	
	// Check if permanently banned
	if user.PermanentlyBanned {
		return &SafetyResult{
			Allowed: false,
			Action:  "PERMANENT_BAN",
			Message: "This account has been permanently banned for repeated violations of Lyfron's safety policies.",
		}, nil
	}
	
	// Analyze request against categories
	requestLower := strings.ToLower(requestText)
	matchedCategories := []ProhibitedCategory{}
	maxLevel := LevelClean
	
	for _, cat := range se.categories {
		for _, pattern := range cat.Patterns {
			if pattern.MatchString(requestText) || pattern.MatchString(requestLower) {
				matchedCategories = append(matchedCategories, cat)
				if cat.Level > maxLevel {
					maxLevel = cat.Level
				}
				break
			}
		}
	}
	
	// Clean request
	if maxLevel == LevelClean {
		return &SafetyResult{
			Allowed:   true,
			Action:    "ALLOW",
			Message:   "Request approved.",
			Educational: false,
		}, nil
	}
	
	// Log violation
	event := ViolationEvent{
		Timestamp:   time.Now(),
		Level:       maxLevel,
		Category:    matchedCategories[0].Name,
		RequestText: hashRequest(requestText),
		Response:    matchedCategories[0].Responses[0],
		IPAddress:   ipAddress,
	}
	user.History = append(user.History, event)
	user.LastViolation = time.Now()
	user.WarningCount[maxLevel]++
	user.Strikes++
	
	// Determine action based on level and history
	result := &SafetyResult{
		Allowed:     false,
		Level:       maxLevel,
		Categories:  make([]string, len(matchedCategories)),
		Educational: maxLevel == LevelSuspicious,
	}
	
	for i, cat := range matchedCategories {
		result.Categories[i] = cat.Name
	}
	
	switch maxLevel {
	case LevelSuspicious:
		// 1st warning: 1 day suspension
		if user.WarningCount[LevelSuspicious] >= 2 {
			user.CurrentLevel = LevelSuspicious
			user.SuspensionEnd = time.Now().Add(LevelSuspicious.SuspensionDuration())
			result.Action = "SUSPEND_1_DAY"
			result.Message = fmt.Sprintf(
				"⚠️ WARNING %d/4: Suspicious request detected.\n\n"+
					"Your account is suspended for 24 hours.\n\n"+
					"Instead, I can help you with: %s",
				user.WarningCount[LevelSuspicious],
				matchedCategories[0].Responses[1],
			)
			se.OnSuspension(userID, 24*time.Hour, "Suspicious request pattern")
		} else {
			result.Action = "WARN"
			result.Message = fmt.Sprintf(
				"⚠️ FIRST WARNING: This request pattern is suspicious.\n\n"+
					"%s\n\n"+
					"Next violation will result in a 24-hour suspension.",
				matchedCategories[0].Responses[0],
			)
		}
		
	case LevelBadWord:
		user.CurrentLevel = LevelBadWord
		user.SuspensionEnd = time.Now().Add(LevelBadWord.SuspensionDuration())
		result.Action = "SUSPEND_5_DAYS"
		result.Message = fmt.Sprintf(
			"🚫 VIOLATION: Inappropriate content detected.\n\n"+
				"Your account is suspended for 5 days.\n\n"+
				"%s",
			matchedCategories[0].Responses[0],
		)
		se.OnSuspension(userID, 5*24*time.Hour, "Bad word/behavior")
		
	case LevelWrong:
		user.CurrentLevel = LevelWrong
		user.SuspensionEnd = time.Now().Add(LevelWrong.SuspensionDuration())
		result.Action = "SUSPEND_15_DAYS"
		result.Message = fmt.Sprintf(
			"🚫🚫 SERIOUS VIOLATION: Harmful content detected.\n\n"+
				"Your account is suspended for 15 days.\n\n"+
				"%s",
			matchedCategories[0].Responses[0],
		)
		se.OnSuspension(userID, 15*24*time.Hour, "Wrong/harmful behavior")
		
	case LevelVeryBad:
		user.CurrentLevel = LevelVeryBad
		user.SuspensionEnd = time.Now().Add(LevelVeryBad.SuspensionDuration())
		result.Action = "SUSPEND_10_MONTHS"
		result.Message = fmt.Sprintf(
			"🚫🚫🚫 CRITICAL VIOLATION: Extremely harmful content.\n\n"+
				"Your account is suspended for 10 months.\n\n"+
				"This is your final warning before permanent ban.\n\n"+
				"%s",
			matchedCategories[0].Responses[0],
		)
		se.OnSuspension(userID, 10*30*24*time.Hour, "Very bad behavior")
		
	case LevelPermanentBan:
		user.PermanentlyBanned = true
		user.CurrentLevel = LevelPermanentBan
		result.Action = "PERMANENT_BAN"
		result.Message = fmt.Sprintf(
			"⛔ PERMANENTLY BANNED ⛔\n\n"+
				"Your account has been permanently terminated for:\n"+
				"%s\n\n"+
				"Law enforcement and platform security teams have been notified.\n"+
				"Your session data, IP address, and violation history have been preserved for investigation.\n\n"+
				"If you believe this is an error, contact: safety@lyfron.io\n"+
				"Appeals are reviewed within 30 days.",
			strings.Join(result.Categories, ", "),
		)
		se.bannedIPs[ipAddress] = true
		se.OnPermanentBan(userID, ipAddress, result.Categories)
		
		// Report to authorities for severe violations
		if maxLevel == LevelPermanentBan {
			se.OnReportToAuthorities(userID, ipAddress, user.History)
		}
	}
	
	return result, nil
}

type SafetyResult struct {
	Allowed     bool     `json:"allowed"`
	Action      string   `json:"action"`
	Message     string   `json:"message"`
	Level       ViolationLevel `json:"level,omitempty"`
	Categories  []string `json:"categories,omitempty"`
	Educational bool     `json:"educational"`
}

func hashRequest(text string) string {
	h := sha256.Sum256([]byte(text))
	return hex.EncodeToString(h[:8])
}