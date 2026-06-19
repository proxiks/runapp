package main

import "C"
import "lyfron/security"

//export CheckThreat
func CheckThreat(userID *C.char) *C.char {
    result := security.Analyze(C.GoString(userID))
    return C.CString(result)
}

func main() {}
