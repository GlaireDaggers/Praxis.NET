using System.Text.Json.Serialization;

namespace Praxis.Core;

public struct Margins
{
    [JsonPropertyName("left")]
    public int Left { get; set; }
    
    [JsonPropertyName("top")]
    public int Top { get; set; }
    
    [JsonPropertyName("right")]
    public int Right { get; set; }
    
    [JsonPropertyName("bottom")]
    public int Bottom { get; set; }

    public Margins(int left, int top, int right, int bottom)
    {
        Top = top;
        Left = left;
        Right = right;
        Bottom = bottom;
    }
}
