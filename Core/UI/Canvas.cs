namespace Praxis.Core;

/// <summary>
/// Root container of widgets to be drawn to the screen
/// </summary>
public class Canvas : Widget
{
    public void DrawUI(UIRenderer renderer)
    {
        renderer.Begin();
        DrawInternal(renderer);
        renderer.End();
    }

    public override void UpdateLayout(UIRenderer renderer)
    {
        int screenWidth = renderer.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = renderer.GraphicsDevice.PresentationParameters.BackBufferHeight;

        // calculate dimensions
        _cachedRect.X = (int)left.Calculate(screenWidth);
        _cachedRect.Y = (int)top.Calculate(screenHeight);
        _cachedRect.Width = (int)width.Calculate(screenWidth);
        _cachedRect.Height = (int)height.Calculate(screenHeight);

        foreach (var child in Children)
        {
            child.UpdateLayout(renderer);
        }
    }
}
