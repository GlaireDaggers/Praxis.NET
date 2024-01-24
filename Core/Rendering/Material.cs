namespace Praxis.Core;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Design;
using Microsoft.Xna.Framework.Graphics;
using ResourceCache.Core;

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
    private class MaterialData
    {
        [JsonPropertyName("type")]
        public MaterialType Type { get; set; } = MaterialType.Opaque;

        [JsonPropertyName("effect")]
        public string? Effect { get; set; }

        [JsonPropertyName("effectTechnique")]
        public string? EffectTechnique { get; set; }

        [JsonPropertyName("effectTechniqueSkinned")]
        public string? EffectTechniqueSkinned { get; set; }

        [JsonPropertyName("colorBlendFunc")]
        public BlendFunction ColorBlendFunction { get; set; } = BlendFunction.Add;

        [JsonPropertyName("alphaBlendFunc")]
        public BlendFunction AlphaBlendFunction { get; set; } = BlendFunction.Add;

        [JsonPropertyName("colorSrcBlend")]
        public Blend ColorSourceBlend { get; set; } = Blend.One;

        [JsonPropertyName("alphaSrcBlend")]
        public Blend AlphaSourceBlend { get; set; } = Blend.One;
        
        [JsonPropertyName("colorDstBlend")]
        public Blend ColorDestBlend { get; set; } = Blend.Zero;

        [JsonPropertyName("alphaDstBlend")]
        public Blend AlphaDestBlend { get; set; } = Blend.Zero;

        [JsonPropertyName("blendColor")]
        public Color BlendColor { get; set; } = new Color(0, 0, 0, 0);

        [JsonPropertyName("writeR")]
        public bool WriteR { get; set; } = true;

        [JsonPropertyName("writeG")]
        public bool WriteG { get; set; } = true;

        [JsonPropertyName("writeB")]
        public bool WriteB { get; set; } = true;

        [JsonPropertyName("writeA")]
        public bool WriteA { get; set; } = true;

        [JsonPropertyName("depthEnable")]
        public bool DepthEnable { get; set; } = true;
        
        [JsonPropertyName("depthWriteEnable")]
        public bool DepthWriteEnable { get; set; } = true;
        
        [JsonPropertyName("depthCompare")]
        public CompareFunction DepthCompare { get; set; } = CompareFunction.LessEqual;

        [JsonPropertyName("stencilEnable")]
        public bool StencilEnable { get; set; } = false;

        [JsonPropertyName("stencilCompare")]
        public CompareFunction StencilCompare { get; set; } = CompareFunction.Always;

        [JsonPropertyName("stencilPass")]
        public StencilOperation StencilPass { get; set; } = StencilOperation.Keep;

        [JsonPropertyName("stencilFail")]
        public StencilOperation StencilFail { get; set; } = StencilOperation.Keep;

        [JsonPropertyName("stencilDepthFail")]
        public StencilOperation StencilDepthFail { get; set; } = StencilOperation.Keep;

        [JsonPropertyName("twoSidedStencil")]
        public bool TwoSidedStencil { get; set; } = false;

        [JsonPropertyName("ccwStencilCompare")]
        public CompareFunction CCWStencilCompare { get; set; } = CompareFunction.Always;

        [JsonPropertyName("ccwStencilPass")]
        public StencilOperation CCWStencilPass { get; set; } = StencilOperation.Keep;

        [JsonPropertyName("ccwStencilFail")]
        public StencilOperation CCWStencilFail { get; set; } = StencilOperation.Keep;

        [JsonPropertyName("ccwStencilDepthFail")]
        public StencilOperation CCWStencilDepthFail { get; set; } = StencilOperation.Keep;
        
        [JsonPropertyName("stencilMask")]
        public int StencilMask { get; set; } = int.MaxValue;
        
        [JsonPropertyName("stencilWriteMask")]
        public int StencilWriteMask { get; set; } = int.MaxValue;
        
        [JsonPropertyName("stencilRef")]
        public int StencilRef { get; set; } = 0;
        
        [JsonPropertyName("cullMode")]
        public CullMode CullMode { get; set; } = CullMode.CullClockwiseFace;
        
        [JsonPropertyName("depthBias")]
        public float DepthBias { get; set; } = 0f;
        
        [JsonPropertyName("fillMode")]
        public FillMode FillMode { get; set; } = FillMode.Solid;
        
        [JsonPropertyName("msaa")]
        public bool MSAA { get; set; } = true;
        
        [JsonPropertyName("scissorTestEnable")]
        public bool ScissorTestEnable { get; set; } = false;
        
        [JsonPropertyName("slopeScaleDepthBias")]
        public float SlopeScaleDepthBias { get; set; } = 0f;

        [JsonPropertyName("intParams")]
        public Dictionary<string, int> IntParams { get; set; } = new Dictionary<string, int>();

        [JsonPropertyName("floatParams")]
        public Dictionary<string, float> FloatParams { get; set; } = new Dictionary<string, float>();

        [JsonPropertyName("vec2Params")]
        public Dictionary<string, Vector2> Vec2Params { get; set; } = new Dictionary<string, Vector2>();

        [JsonPropertyName("vec3Params")]
        public Dictionary<string, Vector3> Vec3Params { get; set; } = new Dictionary<string, Vector3>();

        [JsonPropertyName("vec4Params")]
        public Dictionary<string, Vector4> Vec4Params { get; set; } = new Dictionary<string, Vector4>();

        [JsonPropertyName("tex2DParams")]
        public Dictionary<string, string> Tex2DParams { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("texCubeParams")]
        public Dictionary<string, string> TexCubeParams { get; set; } = new Dictionary<string, string>();
    }

    public MaterialType type;
    public BlendState blendState;
    public DepthStencilState dsState;
    public RasterizerState rasterState;
    public RuntimeResource<Effect> effect;
    public string? technique;
    public string? techniqueSkinned;

    private Dictionary<string, int> _intParams = new Dictionary<string, int>();
    private Dictionary<string, float> _floatParams = new Dictionary<string, float>();
    private Dictionary<string, Vector2> _vec2Params = new Dictionary<string, Vector2>();
    private Dictionary<string, Vector3> _vec3Params = new Dictionary<string, Vector3>();
    private Dictionary<string, Vector4> _vec4Params = new Dictionary<string, Vector4>();
    private Dictionary<string, Matrix> _matrixParams = new Dictionary<string, Matrix>();
    private Dictionary<string, RuntimeResource<Texture2D>> _tex2DParams = new Dictionary<string, RuntimeResource<Texture2D>>();
    private Dictionary<string, RuntimeResource<Texture3D>> _tex3DParams = new Dictionary<string, RuntimeResource<Texture3D>>();
    private Dictionary<string, RuntimeResource<TextureCube>> _texCubeParams = new Dictionary<string, RuntimeResource<TextureCube>>();

    /// <summary>
    /// Deserialize material from a JSON stream
    /// </summary>
    public static Material Deserialize(PraxisGame game, Stream stream)
    {
        var settings = new JsonSerializerOptions
        {
            Converters = 
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new JsonVector2Converter(),
                new JsonVector3Converter(),
                new JsonVector4Converter(),
                new JsonColorConverter()
            }
        };

        var materialData = JsonSerializer.Deserialize<MaterialData>(stream, settings)!;
        var effect = game.Resources.Load<Effect>(materialData.Effect!);

        var writeChannels = ColorWriteChannels.None;

        if (materialData.WriteR) writeChannels |= ColorWriteChannels.Red;
        if (materialData.WriteG) writeChannels |= ColorWriteChannels.Green;
        if (materialData.WriteB) writeChannels |= ColorWriteChannels.Blue;
        if (materialData.WriteA) writeChannels |= ColorWriteChannels.Alpha;

        var material = new Material(effect)
        {
            _intParams = materialData.IntParams,
            _floatParams = materialData.FloatParams,
            _vec2Params = materialData.Vec2Params,
            _vec3Params = materialData.Vec3Params,
            _vec4Params = materialData.Vec4Params,
            type = materialData.Type,
            technique = materialData.EffectTechnique,
            techniqueSkinned = materialData.EffectTechniqueSkinned,
            blendState = new BlendState
            {
                ColorBlendFunction = materialData.ColorBlendFunction,
                AlphaBlendFunction = materialData.AlphaBlendFunction,
                ColorSourceBlend = materialData.ColorSourceBlend,
                ColorDestinationBlend = materialData.ColorDestBlend,
                AlphaSourceBlend = materialData.AlphaSourceBlend,
                AlphaDestinationBlend = materialData.AlphaDestBlend,
                BlendFactor = materialData.BlendColor,
                ColorWriteChannels = writeChannels,
                ColorWriteChannels1 = writeChannels,
                ColorWriteChannels2 = writeChannels,
                ColorWriteChannels3 = writeChannels
            },
            dsState = new DepthStencilState
            {
                DepthBufferEnable = materialData.DepthEnable,
                DepthBufferWriteEnable = materialData.DepthWriteEnable,
                DepthBufferFunction = materialData.DepthCompare,
                StencilEnable = materialData.StencilEnable,
                StencilFunction = materialData.StencilCompare,
                StencilPass = materialData.StencilPass,
                StencilFail = materialData.StencilFail,
                StencilDepthBufferFail = materialData.StencilDepthFail,
                TwoSidedStencilMode = materialData.TwoSidedStencil,
                CounterClockwiseStencilFunction = materialData.CCWStencilCompare,
                CounterClockwiseStencilPass = materialData.CCWStencilPass,
                CounterClockwiseStencilFail = materialData.CCWStencilFail,
                CounterClockwiseStencilDepthBufferFail = materialData.CCWStencilDepthFail,
                StencilMask = materialData.StencilMask,
                StencilWriteMask = materialData.StencilWriteMask,
                ReferenceStencil = materialData.StencilRef
            },
            rasterState = new RasterizerState
            {
                CullMode = materialData.CullMode,
                DepthBias = materialData.DepthBias,
                FillMode = materialData.FillMode,
                MultiSampleAntiAlias = materialData.MSAA,
                ScissorTestEnable = materialData.ScissorTestEnable,
                SlopeScaleDepthBias = materialData.SlopeScaleDepthBias
            }
        };

        foreach (var tex in materialData.Tex2DParams)
        {
            material.SetParameter(tex.Key, game.Resources.Load<Texture2D>(tex.Value));
        }

        foreach (var tex in materialData.TexCubeParams)
        {
            material.SetParameter(tex.Key, game.Resources.Load<TextureCube>(tex.Value));
        }

        return material;
    }

    public Material(RuntimeResource<Effect> effect)
    {
        this.blendState = BlendState.Opaque;
        this.dsState = DepthStencilState.Default;
        this.rasterState = RasterizerState.CullClockwise;
        this.type = MaterialType.Opaque;
        this.effect = effect;
    }

    public void SetParameter(string name, int value)
    {
        _intParams[name] = value;
    }

    public void SetParameter(string name, float value)
    {
        _floatParams[name] = value;
    }

    public void SetParameter(string name, Vector2 value)
    {
        _vec2Params[name] = value;
    }

    public void SetParameter(string name, Vector3 value)
    {
        _vec3Params[name] = value;
    }
    
    public void SetParameter(string name, Vector4 value)
    {
        _vec4Params[name] = value;
    }

    public void SetParameter(string name, Matrix value)
    {
        _matrixParams[name] = value;
    }

    public void SetParameter(string name, RuntimeResource<Texture2D> value)
    {
        _tex2DParams[name] = value;
    }

    public void SetParameter(string name, RuntimeResource<Texture3D> value)
    {
        _tex3DParams[name] = value;
    }

    public void SetParameter(string name, RuntimeResource<TextureCube> value)
    {
        _texCubeParams[name] = value;
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

        foreach (var p in _tex2DParams)
        {
            effect.Value.Parameters[p.Key]?.SetValue(p.Value.Value);
        }

        foreach (var p in _tex3DParams)
        {
            effect.Value.Parameters[p.Key]?.SetValue(p.Value.Value);
        }

        foreach (var p in _texCubeParams)
        {
            effect.Value.Parameters[p.Key]?.SetValue(p.Value.Value);
        }
    }
}