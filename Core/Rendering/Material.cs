namespace Praxis.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
    public RuntimeResource<Effect> effect;
    public int technique;

    private Dictionary<string, int> _intParams = new Dictionary<string, int>();
    private Dictionary<string, float> _floatParams = new Dictionary<string, float>();
    private Dictionary<string, Vector2> _vec2Params = new Dictionary<string, Vector2>();
    private Dictionary<string, Vector3> _vec3Params = new Dictionary<string, Vector3>();
    private Dictionary<string, Vector4> _vec4Params = new Dictionary<string, Vector4>();
    private Dictionary<string, Matrix> _matrixParams = new Dictionary<string, Matrix>();
    private Dictionary<string, Texture> _texParams = new Dictionary<string, Texture>();

    public Material(RuntimeResource<Effect> effect)
    {
        this.blendState = BlendState.Opaque;
        this.dsState = DepthStencilState.Default;
        this.rasterState = RasterizerState.CullClockwise;
        this.type = MaterialType.Opaque;
        this.effect = effect;
        this.technique = 0;
    }

    public void SetParameter(string name, int value)
    {
        _intParams.Add(name, value);
    }

    public void SetParameter(string name, float value)
    {
        _floatParams.Add(name, value);
    }

    public void SetParameter(string name, Vector2 value)
    {
        _vec2Params.Add(name, value);
    }

    public void SetParameter(string name, Vector3 value)
    {
        _vec3Params.Add(name, value);
    }
    
    public void SetParameter(string name, Vector4 value)
    {
        _vec4Params.Add(name, value);
    }

    public void SetParameter(string name, Matrix value)
    {
        _matrixParams.Add(name, value);
    }

    public void SetParameter(string name, Texture value)
    {
        _texParams.Add(name, value);
    }

    internal void ApplyParameters()
    {
        foreach (var p in _intParams)
        {
            effect.Value.Parameters[p.Key]?.SetValue(p.Value);
        }

        foreach (var p in _floatParams)
        {
            effect.Value.Parameters[p.Key]?.SetValue(p.Value);
        }

        foreach (var p in _vec2Params)
        {
            effect.Value.Parameters[p.Key]?.SetValue(p.Value);
        }

        foreach (var p in _vec3Params)
        {
            effect.Value.Parameters[p.Key]?.SetValue(p.Value);
        }

        foreach (var p in _vec4Params)
        {
            effect.Value.Parameters[p.Key]?.SetValue(p.Value);
        }

        foreach (var p in _matrixParams)
        {
            effect.Value.Parameters[p.Key]?.SetValue(p.Value);
        }

        foreach (var p in _texParams)
        {
            effect.Value.Parameters[p.Key]?.SetValue(p.Value);
        }
    }
}