using FontStashSharp;

namespace Praxis.Core;

/// <summary>
/// Container for a loaded truetype font
/// </summary>
public class Font : IDisposable
{
    private readonly FontSystem _fontSystem;

    public Font(byte[] fontData)
    {
        _fontSystem = new FontSystem();
        _fontSystem.AddFont(fontData);
    }

    public DynamicSpriteFont GetFont(int size)
    {
        return _fontSystem.GetFont(size);
    }

    public void Dispose()
    {
        _fontSystem.Dispose();
    }
}
