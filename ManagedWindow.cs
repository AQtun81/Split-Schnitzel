using System.Windows;

namespace Split_Schnitzel;

public class ManagedWindow : IDisposable
{
    public Rect TargetPosition = new();
    
    private readonly IntPtr handle;
    private readonly int previousStyle;
    private readonly FrameworkElement target;
    private readonly UIElement? relativeTo;
    
    private const int WINDOW_STYLE = -16;

    public ManagedWindow(IntPtr handle, FrameworkElement target, UIElement? relativeTo = null)
    {
        // window handle and style
        this.handle = handle;
        previousStyle = Extern.GetWindowLong(handle, WINDOW_STYLE);
        Extern.SetWindowLong(handle, WINDOW_STYLE, 0x00080000);

        // target position
        this.target = target;
        this.relativeTo = relativeTo;
        TargetPosition.Update(target, relativeTo);
    }

    public void SetPos(int x, int y, int cx, int cy)
    {
        Extern.SetWindowPos(handle, MainWindow.ZIndexWindowFlag, x, y, cx, cy, MainWindow.ActiveWindowFlags);
    }

    public void Dispose()
    {
        Extern.SetWindowLong(handle, WINDOW_STYLE, previousStyle);
        Extern.SetWindowPos(handle, WindowInsertAfter.NoTopmost, 0, 0, 800, 480, (int) WindowFlags.NoActivate | (int) WindowFlags.NoMove | (int) WindowFlags.NoSize);
    }

    public void UpdateTarget() => TargetPosition.Update(target, relativeTo);
}