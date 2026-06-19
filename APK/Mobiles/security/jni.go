package main

/*
#include <stdlib.h>
*/
import "C"
import (
    "lyfron/security"
    "lyfron/auth"
    "unsafe"
)

//export LyfronInit
func LyfronInit(configPath *C.char) *C.char {
    err := security.Initialize(C.GoString(configPath))
    if err != nil {
        return C.CString(`{"status":"error","msg":"` + err.Error() + `"}`)
    }
    return C.CString(`{"status":"ok"}`)
}

//export LyfronCheckThreat
func LyfronCheckThreat(userID *C.char, action *C.char, ip *C.char) *C.char {
    result := security.AnalyzeRequest(security.Request{
        UserID: C.GoString(userID),
        Action: C.GoString(action),
        IP:     C.GoString(ip),
    })
    return C.CString(result.JSON())
}

//export LyfronVerifyToken
func LyfronVerifyToken(token *C.char) *C.char {
    claims, err := auth.ValidateJWT(C.GoString(token))
    if err != nil {
        return C.CString(`{"valid":false}`)
    }
    return C.CString(`{"valid":true,"user_id":"` + claims.UserID + `"}`)
}

//export LyfronHashPassword
func LyfronHashPassword(password *C.char) *C.char {
    hash := auth.Argon2Hash(C.GoString(password))
    return C.CString(hash)
}

//export LyfronFreeString
func LyfronFreeString(s *C.char) {
    C.free(unsafe.Pointer(s))
}

func main() {}