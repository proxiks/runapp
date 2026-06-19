#ifndef LYFRON_CRYPTO_H
#define LYFRON_CRYPTO_H

#ifdef __cplusplus
extern "C" {
#endif

// Argon2 password hashing
int lyfron_hash_password(const char* password, char* out_hash, size_t out_len);
int lyfron_verify_password(const char* password, const char* hash);

// AES-256-GCM for local data
int lyfron_encrypt(const unsigned char* plaintext, size_t pt_len,
                   const unsigned char* key,
                   unsigned char* ciphertext, size_t* ct_len);
int lyfron_decrypt(const unsigned char* ciphertext, size_t ct_len,
                   const unsigned char* key,
                   unsigned char* plaintext, size_t* pt_len);

// Threat score (0-100)
int lyfron_check_threat(const char* user_id, const char* action, 
                        const char* ip, int* score, char* reason);

#ifdef __cplusplus
}
#endif

#endif