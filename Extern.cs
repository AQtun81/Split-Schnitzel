using System.Runtime.InteropServices;

namespace Split_Schnitzel;

public class Extern
{
    // Window Manipulation Methods
    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    
    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public static IntPtr SetWindowPos(IntPtr hWnd, WindowInsertAfter hWndInsertAfter, int x, int y, int cx, int cy, int wFlags) =>
        SetWindowPos(hWnd, (IntPtr) hWndInsertAfter, x, y, cx, cy, wFlags);
    
    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);
    
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
        
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommand nCmdShow);
        
    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);
        
    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr hWnd);
}

public enum WindowFlags : int
{
    AsyncWindowPos = 0x4000,
    Defererase = 0x2000,
    DrawFrame = 0x0020,
    FrameChanged = 0x0020,
    HideWindow = 0x0080,
    NoActivate = 0x0010,
    NoCopyBits = 0x0100,
    NoMove = 0x0002,
    NoOwnerZOrder = 0x0200,
    NoRedraw = 0x0008,
    NoReposition = 0x0200,
    NoSendChanging = 0x0400,
    NoSize = 0x0001,
    NoZOrder = 0x0004,
    ShowWindow = 0x0040,
}

public enum WindowInsertAfter : int
{
    NoTopmost = -2,
    Topmost = -1,
    Top = 0,
    Bottom = 1
}

public enum ShowWindowCommand : int
{
    Hide,
    ShowNormal,
    ShowMinimized,
    ShowMaximized,
    ShowNoActivate,
    Show,
    Minimize,
    ShowMinNoActive,
    ShowNa,
    Restore,
    ShowDefault,
    ForceMinimize
}