using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Controls;

namespace Split_Schnitzel;

public class ManagedWindow : IDisposable
{
    public Rect TargetPosition = new();
    public readonly IntPtr Handle;
    
    private readonly int previousStyle;
    private readonly FrameworkElement target;
    private readonly UIElement? relativeTo;
    
    private const int WINDOW_STYLE = -16;

    public ManagedWindow(IntPtr handle, FrameworkElement target, UIElement? relativeTo = null)
    {
        // window handle and style
        Handle = handle;
        previousStyle = Extern.GetWindowLong(handle, WINDOW_STYLE);
        Extern.SetWindowLong(handle, WINDOW_STYLE, 0x00080000);

        // target position
        this.target = target;
        this.relativeTo = relativeTo;
        TargetPosition.Update(target, relativeTo);
    }

    public void SetPos(int x, int y, int cx, int cy)
    {
        Extern.SetWindowPos(Handle, MainWindow.ZIndexWindowFlag, x, y, cx, cy, MainWindow.ActiveWindowFlags);
    }

    public void SetZIndex(WindowInsertAfter zIndexFlag)
    {
        Extern.SetWindowPos(Handle, zIndexFlag, 0, 0, 800, 480, (int) WindowFlags.NoActivate | (int) WindowFlags.NoMove | (int) WindowFlags.NoSize);
    }

    public bool IsMinimized()
    {
        return Extern.IsIconic(Handle);
    }

    public bool IsMaximized()
    {
        int windowStyle = Extern.GetWindowLong(Handle, WINDOW_STYLE);
        return (windowStyle & (1 << 24)) != 0;
    }

    public bool IsValid()
    {
        return Extern.IsWindow(Handle);
    }

    public void Dispose()
    {
        // restore window style and disable always on top
        Extern.SetWindowLong(Handle, WINDOW_STYLE, previousStyle);
        SetZIndex(WindowInsertAfter.NoTopmost);
        
        // restore window picking button
        ((Button)((DashedBorder)target).Child).IsEnabled = true;
        ((DashedBorder)target).BorderBrush = new SolidColorBrush(Colors.DimGray);
    }

    public void UpdateTarget() => TargetPosition.Update(target, relativeTo);
}