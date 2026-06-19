/**
 * Lyfron Part 148: Gatekeeper UI Service
 * Shows security dialog when suspicious file is launched
 */

#include <windows.h>
#include <stdio.h>
#include <string.h>

#define LYFRON_GK_DEVICE_PATH "\\\\.\\LyfronGatekeeper"

#define IOCTL_GK_CHECK_FILE     0x80002000
#define IOCTL_GK_USER_RESPONSE  0x80002004

#pragma pack(push, 1)
typedef struct {
    WCHAR FilePath[520];
    WCHAR FileName[256];
    UCHAR FileHash[32];
    BOOL IsDownload;
    WCHAR DownloadURL[2048];
    BOOL RequiresAdmin;
} GK_CHECK_REQUEST;

typedef struct {
    ULONG Action;
    WCHAR WarningMessage[1024];
    WCHAR ThreatDetails[2048];
    ULONG Confidence;
} GK_CHECK_RESPONSE;
#pragma pack(pop)

// Custom dialog procedure
INT_PTR CALLBACK LyfronSecurityDlgProc(HWND hwndDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    static GK_CHECK_RESPONSE* g_Response = NULL;
    static HANDLE g_ProcessId = NULL;
    
    switch (uMsg) {
        case WM_INITDIALOG: {
            g_Response = (GK_CHECK_RESPONSE*)lParam;
            g_ProcessId = (HANDLE)GetWindowLongPtr(hwndDlg, GWLP_USERDATA);
            
            // Set dialog icon
            HICON hIcon = LoadIcon(NULL, IDI_WARNING);
            SendMessage(hwndDlg, WM_SETICON, ICON_BIG, (LPARAM)hIcon);
            
            // Set warning text
            SetDlgItemTextW(hwndDlg, IDC_WARNING_TEXT, g_Response->WarningMessage);
            
            // Set confidence meter
            HWND hConfidence = GetDlgItem(hwndDlg, IDC_CONFIDENCE_BAR);
            SendMessage(hConfidence, PBM_SETRANGE, 0, MAKELPARAM(0, 10000));
            SendMessage(hConfidence, PBM_SETPOS, g_Response->Confidence, 0);
            
            // Color code: red for >8000, yellow for >5000, green otherwise
            if (g_Response->Confidence > 8000) {
                SendMessage(hConfidence, PBM_SETBARCOLOR, 0, RGB(255, 0, 0));
            } else if (g_Response->Confidence > 5000) {
                SendMessage(hConfidence, PBM_SETBARCOLOR, 0, RGB(255, 165, 0));
            }
            
            // Disable Start button for 5 seconds (anti-misclick)
            EnableWindow(GetDlgItem(hwndDlg, IDC_BTN_START), FALSE);
            SetTimer(hwndDlg, 1, 5000, NULL);
            
            // Center dialog
            RECT rcDlg, rcDesktop;
            GetWindowRect(hwndDlg, &rcDlg);
            GetWindowRect(GetDesktopWindow(), &rcDesktop);
            SetWindowPos(hwndDlg, NULL,
                (rcDesktop.right - rcDlg.right + rcDlg.left) / 2,
                (rcDesktop.bottom - rcDlg.bottom + rcDlg.top) / 2,
                0, 0, SWP_NOSIZE | SWP_NOZORDER);
            
            // Play alert sound
            MessageBeep(MB_ICONWARNING);
            
            return TRUE;
        }
        
        case WM_TIMER:
            if (wParam == 1) {
                EnableWindow(GetDlgItem(hwndDlg, IDC_BTN_START), TRUE);
                KillTimer(hwndDlg, 1);
            }
            return TRUE;
        
        case WM_COMMAND:
            switch (LOWORD(wParam)) {
                case IDC_BTN_START: {
                    // User clicked START - activate containment
                    HANDLE hDevice = CreateFileA(LYFRON_GK_DEVICE_PATH, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
                    if (hDevice != INVALID_HANDLE_VALUE) {
                        struct {
                            ULONG Response;
                            HANDLE PID;
                        } req = { 1, g_ProcessId };
                        
                        DWORD bytesReturned;
                        DeviceIoControl(hDevice, IOCTL_GK_USER_RESPONSE, &req, sizeof(req), NULL, 0, &bytesReturned, NULL);
                        CloseHandle(hDevice);
                    }
                    
                    // Show confirmation
                    MessageBoxW(hwndDlg,
                        L"Lyfron Containment Mode Activated\n\n"
                        L"The application will run with maximum security restrictions:\n"
                        L"• No administrator access\n"
                        L"• Network access blocked\n"
                        L"• File system restricted\n"
                        L"• All activity monitored\n"
                        L"• Reported to Microsoft Security Intelligence",
                        L"Lyfron Security", MB_OK | MB_ICONINFORMATION);
                    
                    EndDialog(hwndDlg, IDOK);
                    return TRUE;
                }
                
                case IDC_BTN_CANCEL:
                case IDCANCEL: {
                    // User clicked CANCEL - block execution
                    HANDLE hDevice = CreateFileA(LYFRON_GK_DEVICE_PATH, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
                    if (hDevice != INVALID_HANDLE_VALUE) {
                        struct {
                            ULONG Response;
                            HANDLE PID;
                        } req = { 0, g_ProcessId };
                        
                        DWORD bytesReturned;
                        DeviceIoControl(hDevice, IOCTL_GK_USER_RESPONSE, &req, sizeof(req), NULL, 0, &bytesReturned, NULL);
                        CloseHandle(hDevice);
                    }
                    
                    EndDialog(hwndDlg, IDCANCEL);
                    return TRUE;
                }
            }
            break;
    }
    
    return FALSE;
}

// Dialog resource IDs (would be in .rc file)
#define IDC_WARNING_TEXT    1001
#define IDC_CONFIDENCE_BAR  1002
#define IDC_BTN_START       1003
#define IDC_BTN_CANCEL      IDCANCEL

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
    // Parse command line: lyfron_gk_ui.exe <pid> <filepath>
    int argc;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    
    if (argc < 3) return 1;
    
    HANDLE targetPID = (HANDLE)(ULONG_PTR)wcstoul(argv[1], NULL, 10);
    WCHAR* filePath = argv[2];
    
    // Query kernel driver for threat assessment
    HANDLE hDevice = CreateFileA(LYFRON_GK_DEVICE_PATH, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
    if (hDevice == INVALID_HANDLE_VALUE) return 1;
    
    GK_CHECK_REQUEST req = {0};
    wcscpy_s(req.FilePath, 520, filePath);
    
    // Extract filename
    WCHAR* fileName = wcsrchr(filePath, L'\\');
    if (fileName) fileName++;
    else fileName = filePath;
    wcscpy_s(req.FileName, 256, fileName);
    
    GK_CHECK_RESPONSE resp = {0};
    DWORD bytesReturned;
    
    BOOL ok = DeviceIoControl(hDevice, IOCTL_GK_CHECK_FILE, &req, sizeof(req), &resp, sizeof(resp), &bytesReturned, NULL);
    CloseHandle(hDevice);
    
    if (!ok) return 1;
    
    if (resp.Action == 1) {
        // Already blocked - show notification
        MessageBoxW(NULL, resp.WarningMessage, L"Lyfron Security - Blocked", MB_OK | MB_ICONSTOP);
        return 0;
    }
    
    if (resp.Action == 2) {
        // Show security dialog
        DialogBoxParam(hInstance, MAKEINTRESOURCE(IDD_SECURITY_DIALOG), NULL, LyfronSecurityDlgProc, (LPARAM)&resp);
    }
    
    LocalFree(argv);
    return 0;
}
