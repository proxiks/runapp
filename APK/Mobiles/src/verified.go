package payments

import (
    "context"
    "time"
    "google.golang.org/api/androidpublisher/v3"
)

type VerifiedSubscription struct {
    UserID        string    `json:"user_id"`
    PurchaseToken string    `json:"purchase_token"`
    SKU           string    `json:"sku"`
    ExpiresAt     time.Time `json:"expires_at"`
    IsActive      bool      `json:"is_active"`
    AutoRenew     bool      `json:"auto_renew"`
}

type Verifier struct {
    androidPublisher *androidpublisher.Service
    playPackageName  string
}

func NewVerifier(serviceAccountJSON []byte, packageName string) (*Verifier, error) {
    ctx := context.Background()
    service, err := androidpublisher.NewService(ctx)
    if err != nil {
        return nil, err
    }
    return &Verifier{
        androidPublisher: service,
        playPackageName:  packageName,
    }, nil
}

func (v *Verifier) VerifyGooglePlayPurchase(ctx context.Context, token, sku string) (*VerifiedSubscription, error) {
    // Call Google Play Developer API to validate
    purchase, err := v.androidPublisher.Purchases.Subscriptions.Get(
        v.playPackageName,
        sku,
        token,
    ).Context(ctx).Do()
    
    if err != nil {
        return nil, err
    }
    
    return &VerifiedSubscription{
        PurchaseToken: token,
        SKU:           sku,
        ExpiresAt:     time.UnixMilli(purchase.ExpiryTimeMillis),
        IsActive:      purchase.PaymentState == 1, // 1 = paid
        AutoRenew:     purchase.AutoRenewing,
    }, nil
}

func (v *Verifier) GrantVerifiedStatus(ctx context.Context, userID string, sub *VerifiedSubscription) error {
    // Store in DB, activate badge, update Redis cache
    // OCaml or Python can handle the DB layer if you prefer
    return db.UpsertVerifiedSubscription(ctx, userID, sub)
}
