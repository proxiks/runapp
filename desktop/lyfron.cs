// In your C# desktop app
using System.Runtime.InteropServices;

public class LyfronNative
{
    [DllImport("lyfron.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int lyfron_check_threat(
        string userId, 
        string action, 
        string ip, 
        out int score, 
        StringBuilder reason);
}