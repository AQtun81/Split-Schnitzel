using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
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

    // window handles
    private static IntPtr? thisWindowHandle;
    private static ManagedWindow?[] managedWindows = new ManagedWindow?[2];

    private enum WindowSlot : byte
    {
        Left,
        Right
    }
    
    public MainWindow()
    {
        InitializeComponent();
        
        #if DEBUG
        AllocConsole();
        Console.WriteLine("Debug Console Initialized");
        #endif

        CompositionTarget.Rendering += UpdateMangedWindows;
    }

    private void OnWindowAssignLeft(object sender, RoutedEventArgs e) => OnWindowAssign(WindowSlot.Left);
    private void OnWindowAssignRight(object sender, RoutedEventArgs e) => OnWindowAssign(WindowSlot.Right);

    private async void OnWindowAssign(WindowSlot position)
    {
        IntPtr targetWindow = await PollForegroundWindow();
        managedWindows[(int)position] = new ManagedWindow(targetWindow);

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
        if (position == WindowSlot.Right) panelPosition.X += panelSize.Width;
        Console.WriteLine($"Grid Position: {panelPosition.X}x{panelPosition.Y}");
        
        // set window position
        managedWindows[(int)position].SetPos(
            (int) panelPosition.X,
            (int) panelPosition.Y,
            (int) panelSize.Width,
            (int) panelSize.Height);
    }

    private async Task<IntPtr> PollForegroundWindow()
    {
        // make sure this window's handle is not null
        thisWindowHandle ??= this.GetHandle();
        IntPtr foregroundWindow = (IntPtr) thisWindowHandle;
        
        // poll until window handle is different from ours
        while (foregroundWindow == thisWindowHandle)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50)); // 20 times per second
            foregroundWindow = GetForegroundWindow();
            Console.WriteLine($"Polling window handle [{foregroundWindow}]");
        }

        return foregroundWindow;
    }

    private void UpdateMangedWindows(object? sender, EventArgs eventArgs)
    {
        for (int i = 0; i < managedWindows.Length; i++)
        {
            if (managedWindows[i] is null) continue;
            UpdateWindow(managedWindows[i], i == 1);
        }
        
        void UpdateWindow(ManagedWindow target, bool right)
        {
            // grid position
            Point gridPosition = MainGrid.PointToScreen(new Point(0, 0));
            
            // panel size
            Size panelSize = new(Math.Round(MainGrid.RenderSize.Width * 0.5f), MainGrid.RenderSize.Height);
        
            // panel position
            Point panelPosition = gridPosition;
            if (right) panelPosition.X += panelSize.Width;
            
            target.SetPos(
                (int) panelPosition.X,
                (int) panelPosition.Y,
                (int) panelSize.Width,
                (int) panelSize.Height);
        }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // revert window style changes
        foreach (ManagedWindow? window in managedWindows) window?.Dispose();
    }
}