#include "crypto.h"
#include <string.h>
#include <stdlib.h>
#include <time.h>
#include <openssl/evp.h>
#include <openssl/rand.h>
#include <openssl/argon2.h>

int lyfron_hash_password(const char* password, char* out_hash, size_t out_len) {
    // Argon2id with OWASP recommended params
    unsigned char salt[16];
    RAND_bytes(salt, sizeof(salt));
    
    return argon2id_hash_raw(
        3,      // iterations
        65536,  // memory (64MB)
        4,      // parallelism
        password, strlen(password),
        salt, sizeof(salt),
        (unsigned char*)out_hash, out_len
    );
}

int lyfron_verify_password(const char* password, const char* hash) {
    // Parse hash, extract params, verify
    return 0; // Simplified - implement full Argon2 verify
}

int lyfron_encrypt(const unsigned char* pt, size_t pt_len,
                   const unsigned char* key,
                   unsigned char* ct, size_t* ct_len) {
    EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
    unsigned char iv[12];
    unsigned char tag[16];
    
    RAND_bytes(iv, sizeof(iv));
    
    EVP_EncryptInit_ex(ctx, EVP_aes_256_gcm(), NULL, NULL, NULL);
    EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_GCM_SET_IVLEN, sizeof(iv), NULL);
    EVP_EncryptInit_ex(ctx, NULL, NULL, key, iv);
    
    int len;
    EVP_EncryptUpdate(ctx, ct + sizeof(iv), &len, pt, pt_len);
    *ct_len = len;
    
    EVP_EncryptFinal_ex(ctx, ct + sizeof(iv) + len, &len);
    *ct_len += len;
    
    EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_GCM_GET_TAG, sizeof(tag), tag);
    
    memcpy(ct, iv, sizeof(iv));
    memcpy(ct + sizeof(iv) + *ct_len, tag, sizeof(tag));
    *ct_len += sizeof(iv) + sizeof(tag);
    
    EVP_CIPHER_CTX_free(ctx);
    return 0;
}

int lyfron_check_threat(const char* user_id, const char* action,
                        const char* ip, int* score, char* reason) {
    // Call into Go/Python ML layer via IPC
    // Simplified: random score for demo
    srand(time(NULL));
    *score = rand() % 30; // Usually low for legit users
    
    if (*score > 80) {
        strcpy(reason, "suspicious_ip");
        return 1;
    }
    strcpy(reason, "clean");
    return 0;
}