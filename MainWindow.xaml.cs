using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using HandyControl.Controls;
using HandyControl.Tools;
using Split_Schnitzel.Configuration;
using Window = System.Windows.Window;

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

        Config.LoadConfig();
    }

    private void OnWindowAssignLeft(object sender, RoutedEventArgs e) => OnWindowAssign((VisualTreeHelper.GetParent((Button) sender) as DashedBorder)!);
    private void OnWindowAssignRight(object sender, RoutedEventArgs e) => OnWindowAssign((VisualTreeHelper.GetParent((Button) sender) as DashedBorder)!);

    private async void OnWindowAssign(DashedBorder targetElement)
    {
        int windowSlot = 0;
        for (int i = 0; i < managedWindows.Length; i++)
        {
            if (managedWindows[i] is not null) continue;
            windowSlot = i;
        }
        
        // set window picking visuals
        targetElement.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
        
        // get target window
        IntPtr targetWindow = await PollForegroundWindow();

        // make sure we don't already manage this window
        foreach (ManagedWindow? window in managedWindows)
        {
            if (window is null) continue;
            if (window.Handle == targetWindow)
            {
                targetElement.BorderBrush = new SolidColorBrush(Colors.DimGray);
                return;
            }
        }

        // assign target window
        BindWindowToTargetElement(windowSlot, targetWindow, targetElement);

        // disable window picking visuals
        targetElement.BorderBrush = new SolidColorBrush(Colors.Transparent);
        ((Button)targetElement.Child).IsEnabled = false;
        
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

    private void BindWindowToTargetElement(int windowSlot, IntPtr targetWindow, FrameworkElement targetElement)
    {
        // assign target window
        managedWindows[windowSlot] = new ManagedWindow(targetWindow, targetElement);

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
    }

    private void UpdateMangedWindows(object? sender, EventArgs eventArgs)
    {
        // restore main window if one of managed windows gets focused
        // return if window is minimized and shouldn't be restored
        if (WindowState == WindowState.Minimized && !ShouldRestoreWindow()) return;
        
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
            // dispose closed windows
            if (!target.IsValid()) target.Dispose();
            
            // ensure that window is in it's normal state (not minimized or maximized)
            if (target.IsMinimized() || target.IsMaximized()) Extern.ShowWindow(target.Handle, ShowWindowCommand.ShowNormal);
            
            // set window position to our target
            target.SetPos(
                target.TargetPosition.X + (int) screenPosition.X,
                target.TargetPosition.Y + (int) screenPosition.Y,
                target.TargetPosition.Width,
                target.TargetPosition.Height);
        }

        bool ShouldRestoreWindow()
        {
            // restore main window if one of managed windows gets focused
            if (WindowState != WindowState.Minimized) return false;
            IntPtr foregroundWindow = Extern.GetForegroundWindow();
            
            foreach (ManagedWindow? window in managedWindows)
            {
                if (window is null) continue;
                if (window.Handle != foregroundWindow) continue;
                WindowState = WindowState.Normal;
                return true;
            }

            return false;
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

        if (Config.Autostart.AutostartEnabled)
        {
            // start and capture applications
            Console.WriteLine("Autostart Enabled");
            StartAndCaptureApplication(Config.Autostart.LeftPanelApplication, LeftPanel);
            StartAndCaptureApplication(Config.Autostart.RightPanelApplication, RightPanel);
        }
        
        // set preferences
        
        // set split position
        GridLengthConverter glc = new();
        LeftSplit.Width = (GridLength) (glc.ConvertFromString($"{Config.Preferences.SplitterPosition * 100}*") ?? GridLength.Auto);
        RightSplit.Width = (GridLength) (glc.ConvertFromString($"{(1f - Config.Preferences.SplitterPosition) * 100}*") ?? GridLength.Auto);
        
        // set splitter width
        if (Config.Preferences.SplitterWidth >= 1) GridSplitter.Width = Config.Preferences.SplitterWidth;
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
        
        foreach (ManagedWindow? window in managedWindows)
        {
            window?.SetZIndex(WindowInsertAfter.NoTopmost);
        }
    }
    
    private void OnWindowActivated(object? sender, EventArgs e)
    {
        Console.WriteLine("Window Activated");
        ActiveWindowFlags = (int) WindowFlags.NoActivate | (int) WindowFlags.ShowWindow;
        ZIndexWindowFlag = WindowInsertAfter.Topmost;

        foreach (ManagedWindow? window in managedWindows)
        {
            window?.SetZIndex(WindowInsertAfter.Topmost);
        }
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

    private void OnWindowStateChanged(object? sender, EventArgs e) => OnWindowStateChanged();
    private void OnWindowStateChanged()
    {
        switch (WindowState)
        {
            case WindowState.Minimized: SetAllManagedWindowState(ShowWindowCommand.Minimize); break;
            case WindowState.Normal:    SetAllManagedWindowState(ShowWindowCommand.ShowNormal); break;
            case WindowState.Maximized: SetAllManagedWindowState(ShowWindowCommand.ShowNormal); break;
        }

        void SetAllManagedWindowState(ShowWindowCommand command)
        {
            foreach (ManagedWindow? window in managedWindows)
            {
                if (window is null) continue;
                Extern.ShowWindow(window.Handle, command);
            }
        }
    }
}