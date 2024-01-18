using System.Windows;

namespace Split_Schnitzel;

public struct Rect
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    
    public Rect() { } // default values
    public Rect(FrameworkElement element, UIElement? relativeTo = null)
    {
        Update(element, relativeTo);
    }

    public void Update(FrameworkElement element, UIElement? relativeTo = null)
    {
        // position
        Point point = new(0, 0);
        point = element.TranslatePoint(point, relativeTo);
        X = (int) point.X;
        Y = (int) point.Y;
        
        // size
        Width  = (int) element.RenderSize.Width;
        Height = (int) element.RenderSize.Height;
    }

    public override string ToString() => $"{X},{Y} : {Width}x{Height}";
}