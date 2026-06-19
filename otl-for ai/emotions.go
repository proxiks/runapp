// Lyfron Part 146.2: Emotion Engine
// Detects user sentiment, adapts tone, prevents escalation

package emotion

import (
	"context"
	"fmt"
	"math"
	"strings"
	"time"
)

// ─── Emotion States ───

type EmotionType int

const (
	EmotionNeutral EmotionType = iota
	EmotionHappy
	EmotionSad
	EmotionAngry
	EmotionAnxious
	EmotionCurious
	EmotionFrustrated
	EmotionGrateful
	EmotionConfused
	EmotionExcited
	EmotionConcerned // For safety-related emotions
)

type UserEmotionalState struct {
	UserID           string
	CurrentEmotion   EmotionType
	EmotionHistory   []EmotionSnapshot
	FrustrationLevel float64 // 0.0 - 1.0
	TrustLevel     float64 // 0.0 - 1.0
	LastInteraction  time.Time
	ConversationTurn int
}

type EmotionSnapshot struct {
	Timestamp time.Time
	Emotion   EmotionType
	Intensity float64
	Trigger   string
}

type EmotionEngine struct {
	// Lexicon-based emotion detection
	positiveWords map[string]float64
	negativeWords map[string]float64
	angerWords    map[string]float64
	anxietyWords  map[string]float64
	safetyWords   map[string]float64 // Words indicating distress
	
	// Context tracking
	userStates map[string]*UserEmotionalState
}

func NewEmotionEngine() *EmotionEngine {
	ee := &EmotionEngine{
		positiveWords: make(map[string]float64),
		negativeWords: make(map[string]float64),
		angerWords:    make(map[string]float64),
		anxietyWords:  make(map[string]float64),
		safetyWords:   make(map[string]float64),
		userStates:    make(map[string]*UserEmotionalState),
	}
	
	ee.loadEmotionLexicon()
	return ee
}

func (ee *EmotionEngine) loadEmotionLexicon() {
	// Positive emotions
	positive := map[string]float64{
		"happy": 0.9, "joy": 0.95, "excited": 0.85, "grateful": 0.9,
		"thank": 0.8, "thanks": 0.8, "appreciate": 0.85, "love": 0.9,
		"great": 0.7, "awesome": 0.85, "amazing": 0.9, "perfect": 0.8,
		"helpful": 0.75, "useful": 0.7, "clear": 0.6, "understand": 0.6,
	}
	for k, v := range positive {
		ee.positiveWords[k] = v
	}
	
	// Negative emotions
	negative := map[string]float64{
		"sad": 0.8, "disappointed": 0.75, "upset": 0.7, "sorry": 0.6,
		"bad": 0.6, "wrong": 0.65, "fail": 0.7, "error": 0.5,
		"confused": 0.6, "lost": 0.55, "difficult": 0.5, "hard": 0.5,
	}
	for k, v := range negative {
		ee.negativeWords[k] = v
	}
	
	// Anger/frustration
	anger := map[string]float64{
		"angry": 0.95, "mad": 0.9, "furious": 0.95, "pissed": 0.9,
		"hate": 0.85, "stupid": 0.8, "idiot": 0.85, "damn": 0.7,
		"hell": 0.6, "crap": 0.65, "suck": 0.75, "terrible": 0.8,
		"worst": 0.85, "garbage": 0.8, "useless": 0.8, "broken": 0.7,
		"fix": 0.5, "now": 0.4, "immediately": 0.45,
	}
	for k, v := range anger {
		ee.angerWords[k] = v
	}
	
	// Anxiety/distress
	anxiety := map[string]float64{
		"worried": 0.8, "scared": 0.85, "afraid": 0.8, "nervous": 0.75,
		"panic": 0.9, "stress": 0.8, "overwhelm": 0.85, "urgent": 0.6,
		"emergency": 0.7, "help": 0.5, "please": 0.4, "desperate": 0.9,
	}
	for k, v := range anxiety {
		ee.anxietyWords[k] = v
	}
	
	// Safety-related (indicators of self-harm or crisis)
	safety := map[string]float64{
		"kill": 0.9, "die": 0.85, "suicide": 0.95, "end it": 0.95,
		"hurt": 0.7, "pain": 0.6, "suffer": 0.65, "alone": 0.5,
		"nobody": 0.5, "worthless": 0.8, "hopeless": 0.85,
	}
	for k, v := range safety {
		ee.safetyWords[k] = v
	}
}

func (ee *EmotionEngine) AnalyzeText(userID, text string) *UserEmotionalState {
	state, ok := ee.userStates[userID]
	if !ok {
		state = &UserEmotionalState{
			UserID:      userID,
			TrustLevel:  0.5,
			LastInteraction: time.Now(),
		}
		ee.userStates[userID] = state
	}
	
	textLower := strings.ToLower(text)
	words := strings.Fields(textLower)
	
	// Score emotions
	posScore := 0.0
	negScore := 0.0
	angerScore := 0.0
	anxietyScore := 0.0
	safetyScore := 0.0
	
	for _, word := range words {
		// Clean word
		word = strings.TrimFunc(word, func(r rune) bool {
			return !((r >= 'a' && r <= 'z') || (r >= '0' && r <= '9'))
		})
		
		if score, ok := ee.positiveWords[word]; ok {
			posScore += score
		}
		if score, ok := ee.negativeWords[word]; ok {
			negScore += score
		}
		if score, ok := ee.angerWords[word]; ok {
			angerScore += score
		}
		if score, ok := ee.anxietyWords[word]; ok {
			anxietyScore += score
		}
		if score, ok := ee.safetyWords[word]; ok {
			safetyScore += score
		}
	}
	
	// Normalize by word count
	wordCount := float64(len(words))
	if wordCount > 0 {
		posScore /= wordCount
		negScore /= wordCount
		angerScore /= wordCount
		anxietyScore /= wordCount
		safetyScore /= wordCount
	}
	
	// Determine dominant emotion
	maxScore := 0.0
	dominant := EmotionNeutral
	
	if safetyScore > 0.3 {
		dominant = EmotionConcerned
		maxScore = safetyScore
	} else if angerScore > 0.3 {
		dominant = EmotionFrustrated
		if angerScore > 0.6 {
			dominant = EmotionAngry
		}
		maxScore = angerScore
	} else if anxietyScore > 0.3 {
		dominant = EmotionAnxious
		maxScore = anxietyScore
	} else if posScore > negScore && posScore > 0.2 {
		dominant = EmotionHappy
		maxScore = posScore
	} else if negScore > posScore && negScore > 0.2 {
		dominant = EmotionSad
		maxScore = negScore
	}
	
	// Update state
	state.CurrentEmotion = dominant
	state.EmotionHistory = append(state.EmotionHistory, EmotionSnapshot{
		Timestamp: time.Now(),
		Emotion:   dominant,
		Intensity: maxScore,
		Trigger:   text,
	})
	
	// Update frustration (decays over time)
	if dominant == EmotionFrustrated || dominant == EmotionAngry {
		state.FrustrationLevel = math.Min(1.0, state.FrustrationLevel+maxScore*0.3)
	} else {
		state.FrustrationLevel = math.Max(0.0, state.FrustrationLevel-0.1)
	}
	
	// Update trust
	if dominant == EmotionHappy || dominant == EmotionGrateful {
		state.TrustLevel = math.Min(1.0, state.TrustLevel+0.05)
	} else if dominant == EmotionAngry {
		state.TrustLevel = math.Max(0.0, state.TrustLevel-0.1)
	}
	
	state.LastInteraction = time.Now()
	state.ConversationTurn++
	
	return state
}

func (ee *EmotionEngine) GenerateAdaptiveResponse(state *UserEmotionalState, baseResponse string) string {
	// Adapt tone based on emotional state
	
	switch state.CurrentEmotion {
	case EmotionAngry, EmotionFrustrated:
		if state.FrustrationLevel > 0.7 {
			return fmt.Sprintf(
				"I can sense you're really frustrated, and I want to help. Let me try a different approach.\n\n%s",
				baseResponse,
			)
		}
		return fmt.Sprintf(
			"I understand this is frustrating. Let's work through this together.\n\n%s",
			baseResponse,
		)
		
	case EmotionAnxious:
		return fmt.Sprintf(
			"I can hear that you're feeling overwhelmed. Take a breath — we'll figure this out step by step.\n\n%s",
			baseResponse,
		)
		
	case EmotionSad:
		return fmt.Sprintf(
			"I'm sorry you're going through this. I'm here to help however I can.\n\n%s",
			baseResponse,
		)
		
	case EmotionConcerned:
		return fmt.Sprintf(
			"I notice you might be going through something difficult. If you're in crisis, please reach out:\n"+
				"988 Suicide & Crisis Lifeline\n"+
			"Text HOME to 741741\n\n"+
			"I'm also here to help with: %s",
			baseResponse,
		)
		
	case EmotionHappy, EmotionExcited:
		return fmt.Sprintf(
			"Great energy! Let's keep that momentum going.\n\n%s",
			baseResponse,
		)
		
	case EmotionConfused:
		return fmt.Sprintf(
			"No worries — let me break this down more simply.\n\n%s",
			baseResponse,
		)
		
	default:
		return baseResponse
	}
}

func (ee *EmotionEngine) ShouldEscalateToHuman(state *UserEmotionalState) bool {
	// Escalate if user is in crisis or extremely frustrated
	if state.CurrentEmotion == EmotionConcerned {
		return true
	}
	if state.FrustrationLevel > 0.9 {
		return true
	}
	if len(state.EmotionHistory) > 10 {
		// Check for repeated negative emotions
		negativeCount := 0
		for _, snap := range state.EmotionHistory[len(state.EmotionHistory)-10:] {
			if snap.Emotion == EmotionAngry || snap.Emotion == EmotionFrustrated || snap.Emotion == EmotionSad {
				negativeCount++
			}
		}
		if negativeCount > 7 {
			return true
		}
	}
	return false
}
