import tensorflow as tf
import numpy as np
from lyfron.bridge import go_bridge

model = tf.keras.models.load_model('/data/lyfron/models/content_moderation_v3.tflite')

def analyze_content(video_bytes: bytes, user_context: dict) -> dict:
    """
    Called from Go via gRPC/ctypes
    Returns risk score 0.0-1.0
    """
    features = extract_features(video_bytes)
    prediction = model.predict(np.array([features]))
    
    risk_score = float(prediction[0][1])
    
    return {
        "risk_score": risk_score,
        "flags": get_flags(risk_score),
        "action": "block" if risk_score > 0.85 else "challenge" if risk_score > 0.6 else "allow"
    }

def get_flags(score: float) -> list:
    if score > 0.9: return ["explicit", "auto_ban"]
    if score > 0.7: return ["suspicious", "manual_review"]
    return []