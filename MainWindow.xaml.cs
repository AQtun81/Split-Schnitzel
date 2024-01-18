using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Tools;

namespace Split_Schnitzel;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    #if DEBUG
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int AllocConsole();
    #endif
    
    // Window Manipulation Methods
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();
    
    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowPos(IntPtr hWnd, WindowInsertAfter hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);
    
    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public enum WindowInsertAfter : int
    {
        HWND_TOPMOST = -1,
        HWND_TOP = 0,
        HWND_BOTTOM = 1,
        HWND_NOTOPMOST = -2
    }

    public enum WindowFlags : int
    {
        SWP_ASYNCWINDOWPOS = 0x4000,
        SWP_DEFERERASE = 0x2000,
        SWP_DRAWFRAME = 0x0020,
        SWP_FRAMECHANGED = 0x0020,
        SWP_HIDEWINDOW = 0x0080,
        SWP_NOACTIVATE = 0x0010,
        SWP_NOCOPYBITS = 0x0100,
        SWP_NOMOVE = 0x0002,
        SWP_NOOWNERZORDER = 0x0200,
        SWP_NOREDRAW = 0x0008,
        SWP_NOREPOSITION = 0x0200,
        SWP_NOSENDCHANGING = 0x0400,
        SWP_NOSIZE = 0x0001,
        SWP_NOZORDER = 0x0004,
        SWP_SHOWWINDOW = 0x0040,
    }

    private static IntPtr? thisWindowHandle;
    private static IntPtr? managedLeftWindow;
    private static IntPtr? managedRightWindow;
    
    public MainWindow()
    {
        InitializeComponent();
        
        #if DEBUG
        AllocConsole();
        Console.WriteLine("Debug Console Initialized");
        #endif

        CompositionTarget.Rendering += UpdateMangedWindows;
    }

    private void OnWindowAssignLeft(object sender, RoutedEventArgs e)
    {
        Button btn = (Button) sender;
        OnWindowAssign(true);
    }

    private void OnWindowAssignRight(object sender, RoutedEventArgs e)
    {
        OnWindowAssign(false);
    }

    private async void OnWindowAssign(bool isLeft)
    {
        IntPtr targetWindow;
        if (isLeft)
        {
            managedLeftWindow = await PollForegroundWindow();
            targetWindow = (IntPtr) managedLeftWindow;
        }
        else
        {
            managedRightWindow = await PollForegroundWindow();
            targetWindow = (IntPtr) managedRightWindow;
        }

        // grid size
        Console.WriteLine($"Grid Size: {MainGrid.RenderSize.Width}x{MainGrid.RenderSize.Height}");
        
        // grid position
        Point gridPosition = MainGrid.PointToScreen(new Point(0, 0));
        Console.WriteLine($"Grid Position: {gridPosition.X}x{gridPosition.Y}");

        
        // panel size
        Size panelSize = new(Math.Round(MainGrid.RenderSize.Width * 0.5f), MainGrid.RenderSize.Height);
        Console.WriteLine($"Grid Panel Size: {panelSize.Width}x{panelSize.Height}");
        
        // panel position
        Point panelPosition = gridPosition;
        if (!isLeft) panelPosition.X += panelSize.Width;
        Console.WriteLine($"Grid Position: {panelPosition.X}x{panelPosition.Y}");

        // remove window borders
        // -16 means window style changes
        // 0x00080000 is for no windows styles
        SetWindowLong(targetWindow, -16, 0x00080000);
        
        // set window position
        SetWindowPos(targetWindow, WindowInsertAfter.HWND_NOTOPMOST, (int) panelPosition.X, (int) panelPosition.Y, (int) panelSize.Width, (int) panelSize.Height, 0);
    }

    private async Task<IntPtr> PollForegroundWindow()
    {
        // make sure this window's handle is not null
        thisWindowHandle ??= this.GetHandle();
        IntPtr foregroundWindow = (IntPtr) thisWindowHandle;
        
        // poll until window handle is different from ours
        while (foregroundWindow == thisWindowHandle)
        {
            foregroundWindow = GetForegroundWindow();
            await Task.Delay(TimeSpan.FromMilliseconds(50)); // 20 times per second
            Console.WriteLine("While");
        }

        return foregroundWindow;
    }

    private void UpdateMangedWindows(object? sender, EventArgs eventArgs)
    {
        if (managedLeftWindow is not null) UpdateWindow((IntPtr) managedLeftWindow, true);
        if (managedRightWindow is not null) UpdateWindow((IntPtr) managedRightWindow, false);
        
        void UpdateWindow(IntPtr handle, bool isLeft)
        {
            // grid position
            Point gridPosition = MainGrid.PointToScreen(new Point(0, 0));
            
            // panel size
            Size panelSize = new(Math.Round(MainGrid.RenderSize.Width * 0.5f), MainGrid.RenderSize.Height);
        
            // panel position
            Point panelPosition = gridPosition;
            if (!isLeft) panelPosition.X += panelSize.Width;
            
            SetWindowPos(handle, WindowInsertAfter.HWND_TOPMOST,
                (int) panelPosition.X,
                (int) panelPosition.Y, 
                (int) panelSize.Width,
                (int) panelSize.Height,
                (int) WindowFlags.SWP_NOACTIVATE | (int) WindowFlags.SWP_SHOWWINDOW);
        }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // revert window style changes
        if (managedLeftWindow is not null) SetWindowLong((IntPtr)managedLeftWindow, -16, 0x00800000 | 0x00C00000 | 0x00080000 | 0x00020000);
        if (managedRightWindow is not null) SetWindowLong((IntPtr)managedRightWindow, -16, 0x00800000 | 0x00C00000 | 0x00080000 | 0x00020000);
    }
}