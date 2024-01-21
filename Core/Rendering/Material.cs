using Microsoft.Xna.Framework.Graphics;

namespace Praxis.Core;

public enum MaterialType
{
    Opaque,
    Transparent
}

/// <summary>
/// Container for an Effect, graphics state, and a set of parameters to apply to that effect when rendering
/// </summary>
public class Material
{
    public MaterialType type;
    public BlendState blendState;
    public DepthStencilState dsState;
    public RasterizerState rasterState;
    public Effect effect;
    public int technique;

    public Material(Effect effect)
    {
        this.blendState = BlendState.Opaque;
        this.dsState = DepthStencilState.Default;
        this.rasterState = RasterizerState.CullClockwise;
        this.type = MaterialType.Opaque;
        this.effect = effect;
        this.technique = 0;
    }
}