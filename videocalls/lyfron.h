#ifndef LYFRON_H
#define LYFRON_H

#include "crypto.h"
#include <string>
#include <memory>

namespace Lyfron {

class SecurityEngine {
public:
    static SecurityEngine& instance();
    
    std::string hashPassword(const std::string& password);
    bool verifyPassword(const std::string& password, const std::string& hash);
    
    std::string encryptLocal(const std::string& data, const std::string& key);
    std::string decryptLocal(const std::string& data, const std::string& key);
    
    struct ThreatResult {
        int score;
        std::string reason;
        bool blocked;
    };
    
    ThreatResult checkThreat(const std::string& userId, 
                             const std::string& action,
                             const std::string& ip);
    
private:
    SecurityEngine() = default;
};

} // namespace Lyfron

#endif