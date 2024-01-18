using System.Runtime.InteropServices;

namespace Split_Schnitzel;

public class ManagedWindow
{
    public readonly IntPtr Handle;

    private readonly int previousStyle;
    private const int WINDOW_STYLE = -16;

    public ManagedWindow(IntPtr handle)
    {
        Handle = handle;
        previousStyle = GetWindowLong(handle, WINDOW_STYLE);
        SetWindowLong(handle, WINDOW_STYLE, 0x00080000);
    }

    public void SetPos(int x, int y, int cx, int cy)
    {
        SetWindowPos(Handle, WindowInsertAfter.Topmost, x, y, cx, cy, (int) WindowFlags.NoActivate | (int) WindowFlags.ShowWindow);
    }

    public void Dispose()
    {
        SetWindowLong(Handle, WINDOW_STYLE, previousStyle);
    }

    private enum WindowInsertAfter : int
    {
        NoTopmost = -2,
        Topmost = -1,
        Top = 0,
        Bottom = 1
    }

    private enum WindowFlags : int
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
    
    
    // Window Manipulation Methods
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    
    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowPos(IntPtr hWnd, WindowInsertAfter hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);
}