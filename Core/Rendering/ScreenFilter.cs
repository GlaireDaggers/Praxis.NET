namespace Praxis.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ResourceCache.Core;

/// <summary>
/// Parent class for filters which can apply post-processing shaders to the screen
/// </summary>
public class ScreenFilter : IDisposable
{
    public readonly PraxisGame Game;

    private SpriteBatch _sb;

    public ScreenFilter(PraxisGame game)
    {
        Game = game;
        _sb = new SpriteBatch(game.GraphicsDevice);
    }

    /// <summary>
    /// Called to render the filter, copying from source to destination
    /// </summary>
    /// <param name="source">The input render target</param>
    /// <param name="dest">The output render target (null if outputting directly to screen)</param>
    public virtual void OnRender(RenderTarget2D source, RenderTarget2D? dest)
    {
        Blit(source, dest);
    }

    protected void Blit(RenderTarget2D source, RenderTarget2D? dest, Material? material = null, string? technique = null)
    {
        Effect? effect = null;

        if (material != null)
        {
            effect = material.effect.Value;
            technique = technique ?? material.technique;
            if (technique != null)
            {
                effect.CurrentTechnique = effect.Techniques[technique];
            }
            material.ApplyParameters();
        }

        int targetWidth = dest?.Width ?? Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int targetHeight = dest?.Height ?? Game.GraphicsDevice.PresentationParameters.BackBufferHeight;

        effect?.Parameters["TargetSize"]?.SetValue(new Vector4(
            targetWidth,
            targetHeight,
            1f / targetWidth,
            1f / targetHeight
        ));

        Game.GraphicsDevice.SetRenderTarget(dest);
        _sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, effect);
        _sb.Draw(source, new Rectangle(0, 0, targetWidth, targetHeight), Color.White);
        _sb.End();
    }

    public virtual void Dispose()
    {
        _sb.Dispose();
    }
}

/// <summary>
/// A container which wraps around an underlying set of filters to be applied in order
/// </summary>
public class ScreenFilterStack : ScreenFilter
{
    public readonly List<ScreenFilter> filters = new List<ScreenFilter>();

    private RenderTarget2D? _temp;
    private RenderTarget2D? _temp2;

    public ScreenFilterStack(PraxisGame game) : base(game)
    {
    }

    internal RenderTarget2D GetTarget(RenderTarget2D? dest)
    {
        int targetWidth = dest?.Width ?? Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int targetHeight = dest?.Height ?? Game.GraphicsDevice.PresentationParameters.BackBufferHeight;
        SurfaceFormat targetFormat = dest?.Format ?? Game.GraphicsDevice.PresentationParameters.BackBufferFormat;
        DepthFormat targetDepth = dest?.DepthStencilFormat ?? Game.GraphicsDevice.PresentationParameters.DepthStencilFormat;

        if (_temp2 == null || _temp2.Width != targetWidth || _temp2.Height != targetHeight ||
            _temp2.Format != targetFormat)
        {
            _temp2?.Dispose();
            _temp2 = new RenderTarget2D(Game.GraphicsDevice, targetWidth, targetHeight, false, targetFormat, targetDepth, 1, RenderTargetUsage.PreserveContents);
        }

        return _temp2;
    }

    public override void OnRender(RenderTarget2D source, RenderTarget2D? dest)
    {
        int targetWidth = dest?.Width ?? Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int targetHeight = dest?.Height ?? Game.GraphicsDevice.PresentationParameters.BackBufferHeight;
        SurfaceFormat targetFormat = dest?.Format ?? Game.GraphicsDevice.PresentationParameters.BackBufferFormat;

        if (_temp == null || _temp.Width != targetWidth || _temp.Height != targetHeight ||
            _temp.Format != targetFormat)
        {
            _temp?.Dispose();
            _temp = new RenderTarget2D(Game.GraphicsDevice, targetWidth, targetHeight, false, targetFormat, DepthFormat.None, 1, RenderTargetUsage.PreserveContents);
        }

        if (filters.Count == 0)
        {
            Blit(source, dest);
        }
        else
        {
            RenderTarget2D a = source;
            RenderTarget2D b = _temp;

            for (int i = 0; i < filters.Count - 1; i++)
            {
                filters[i].OnRender(a, b);
                RenderTarget2D c = a;
                a = b;
                b = c;
            }

            // last filter renders to target
            filters[filters.Count - 1].OnRender(a, dest);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _temp?.Dispose();
        _temp2?.Dispose();
    }
}

/// <summary>
/// Built in filter to perform simple bloom pass
/// </summary>
public class BloomFilter : ScreenFilter
{
    private ResourceHandle<Effect> _effect;
    private Material _mat;

    private RenderTarget2D? _temp0;
    private RenderTarget2D[] _temp;

    public BloomFilter(PraxisGame game,
        int iterations = 3,
        float filterRadius = 2f,
        float thresholdMin = 0.5f,
        float thresholdMax = 0.75f,
        float brightness = 0.5f) : base(game)
    {
        _effect = game.Resources.Load<Effect>("content/shaders/Bloom.fxo");
        _mat = new Material(_effect);
        _temp = new RenderTarget2D[iterations];

        _mat.SetParameter("FilterRadius", filterRadius);
        _mat.SetParameter("ThresholdMin", thresholdMin);
        _mat.SetParameter("ThresholdMax", thresholdMax);
        _mat.SetParameter("Brightness", brightness);
    }

    public override void OnRender(RenderTarget2D source, RenderTarget2D? dest)
    {
        int targetWidth = dest?.Width ?? Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int targetHeight = dest?.Height ?? Game.GraphicsDevice.PresentationParameters.BackBufferHeight;

        RenderTarget2D prev = source;

        if (_temp0 == null || _temp0.Width != targetWidth || _temp0.Height != targetHeight)
        {
            _temp0?.Dispose();
            _temp0 = new RenderTarget2D(Game.GraphicsDevice, targetWidth, targetHeight, false, SurfaceFormat.Color, DepthFormat.None);
        }

        Blit(prev, _temp0, _mat, "Threshold");
        prev = _temp0;

        // iterative downscale
        for (int i = 0; i < _temp.Length; i++)
        {
            int w = targetWidth / (i + 2);
            int h = targetHeight / (i + 2);

            if (_temp[i] == null || _temp[i].Width != w || _temp[i].Height != h)
            {
                _temp[i]?.Dispose();
                _temp[i] = new RenderTarget2D(Game.GraphicsDevice, w, h, false, SurfaceFormat.Color, DepthFormat.None);
            }

            Blit(prev, _temp[i], _mat, "Downsample");
            prev = _temp[i];
        }

        // iterative upscale & blur
        for (int i = _temp.Length - 1; i >= 1; i--)
        {
            Blit(_temp[i], _temp[i - 1], _mat, "Upsample");
        }

        // composite
        _mat.SetParameter("BloomTexture", _temp[0]);
        Blit(source, dest, _mat, "Composite");
    }
}