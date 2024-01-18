using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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

    // window handles
    private static IntPtr? thisWindowHandle;
    private static readonly ManagedWindow?[] managedWindows = new ManagedWindow?[2];
    private static bool isDraggingGridSplitter = false;
    
    // window flags
    public static int ActiveWindowFlags = (int) WindowFlags.NoActivate | (int) WindowFlags.ShowWindow;
    public static WindowInsertAfter ZIndexWindowFlag = WindowInsertAfter.Topmost;

    public MainWindow()
    {
        InitializeComponent();
        
        #if DEBUG
        AllocConsole();
        Console.WriteLine("Debug Console Initialized");
        #endif
    }

    private void OnWindowAssignLeft(object sender, RoutedEventArgs e) => OnWindowAssign(VisualTreeHelper.GetParent((Button) sender) as UIElement ?? MainGrid);
    private void OnWindowAssignRight(object sender, RoutedEventArgs e) => OnWindowAssign(VisualTreeHelper.GetParent((Button) sender) as UIElement ?? MainGrid);

    private async void OnWindowAssign(UIElement targetElement)
    {
        int windowSlot = 0;
        for (int i = 0; i < managedWindows.Length; i++)
        {
            if (managedWindows[i] is not null) continue;
            windowSlot = i;
        }

        IntPtr targetWindow = await PollForegroundWindow();
        managedWindows[windowSlot] = new ManagedWindow(targetWindow, (FrameworkElement) targetElement);

        // get screen position
        Point screenPosition = MainGrid.PointToScreen(new Point(0, 0));
        
        // make window visible above our one
        ActiveWindowFlags = (int) WindowFlags.NoActivate | (int)WindowFlags.ShowWindow;
        ZIndexWindowFlag = WindowInsertAfter.NoTopmost;

        // set window position
        ManagedWindow target = managedWindows[windowSlot]!;
        target.SetPos(
            target.TargetPosition.X + (int) screenPosition.X,
            target.TargetPosition.Y + (int) screenPosition.Y,
            target.TargetPosition.Width,
            target.TargetPosition.Height);
        
        async Task<IntPtr> PollForegroundWindow()
        {
            // make sure this window's handle is not null
            thisWindowHandle ??= this.GetHandle();
            IntPtr foregroundWindow = (IntPtr) thisWindowHandle;
        
            // poll until window handle is different from ours
            while (foregroundWindow == thisWindowHandle)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50)); // 20 times per second
                foregroundWindow = Extern.GetForegroundWindow();
                Console.WriteLine($"Polling window handle [{foregroundWindow}]");
            }

            return foregroundWindow;
        }
    }

    private void UpdateMangedWindows(object? sender, EventArgs eventArgs)
    {
        // get screen position
        Point screenPosition = MainGrid.PointToScreen(new Point(0, 0));

        if (isDraggingGridSplitter) RecalculatePositions();
        
        foreach (ManagedWindow? window in managedWindows)
        {
            if (window is null) continue;
            UpdateWindow(window);
        }
        
        void UpdateWindow(ManagedWindow target)
        {
            target.SetPos(
                target.TargetPosition.X + (int) screenPosition.X,
                target.TargetPosition.Y + (int) screenPosition.Y,
                target.TargetPosition.Width,
                target.TargetPosition.Height);
        }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // revert window style changes
        foreach (ManagedWindow? window in managedWindows) window?.Dispose();
    }
    
    private void OnWindowLoad(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering += UpdateMangedWindows;
        GetWindow(this).KeyDown += OnWindowInput;
    }

    private void OnWindowInput(object sender, KeyEventArgs e)
    {
        // release all bound windows
        if (e.Key == Key.Tab)
        {
            foreach (ManagedWindow? window in managedWindows) window?.Dispose();
            for (int i = 0; i < managedWindows.Length; i++) managedWindows[i] = null;
        }
    }
    
    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        Console.WriteLine("Window Deactivated");
        ActiveWindowFlags = (int) WindowFlags.NoActivate;
        ZIndexWindowFlag = WindowInsertAfter.NoTopmost;
    }
    
    private void OnWindowActivated(object? sender, EventArgs e)
    {
        Console.WriteLine("Window Activated");
        ActiveWindowFlags = (int) WindowFlags.NoActivate | (int) WindowFlags.ShowWindow;
        ZIndexWindowFlag = WindowInsertAfter.Topmost;
    }
    
    private void RecalculatePositions(object sender, object e)
    {
        RecalculatePositions();
        isDraggingGridSplitter = false;
    }
    private void RecalculatePositions()
    {
        // recalculate positions
        foreach (ManagedWindow? window in managedWindows) window?.UpdateTarget();
    }

    private void GridSplitterDragStarted(object sender, DragStartedEventArgs dragStartedEventArgs)
    {
        isDraggingGridSplitter = true;
    }
}