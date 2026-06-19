#include <openssl/evp.h>
#include <openssl/aes.h>
#include <string.h>

// Called from Go, used by RunApp for local data encryption
int lyfron_encrypt_local(const unsigned char *plaintext, int plaintext_len,
                         const unsigned char *key,
                         unsigned char *ciphertext) {
    EVP_CIPHER_CTX *ctx = EVP_CIPHER_CTX_new();
    int len, ciphertext_len;
    
    unsigned char iv[16] = {0}; // In production, use random IV
    
    EVP_EncryptInit_ex(ctx, EVP_aes_256_gcm(), NULL, key, iv);
    EVP_EncryptUpdate(ctx, ciphertext, &len, plaintext, plaintext_len);
    ciphertext_len = len;
    EVP_EncryptFinal_ex(ctx, ciphertext + len, &len);
    ciphertext_len += len;
    
    EVP_CIPHER_CTX_free(ctx);
    return ciphertext_len;
}