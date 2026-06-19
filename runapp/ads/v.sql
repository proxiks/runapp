CREATE TABLE verified_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id VARCHAR(64) NOT NULL REFERENCES users(id),
    purchase_token VARCHAR(512) NOT NULL UNIQUE,
    sku VARCHAR(64) NOT NULL DEFAULT 'jatinbook_verified_monthly',
    status VARCHAR(32) NOT NULL CHECK (status IN ('active', 'expired', 'cancelled', 'grace')),
    started_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    auto_renew BOOLEAN DEFAULT true,
    payment_provider VARCHAR(32) NOT NULL DEFAULT 'google_play',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_verified_user ON verified_subscriptions(user_id);
CREATE INDEX idx_verified_expires ON verified_subscriptions(expires_at) WHERE status = 'active';