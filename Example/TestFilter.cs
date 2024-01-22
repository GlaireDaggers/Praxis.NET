namespace Example;

using Microsoft.Xna.Framework.Graphics;
using Praxis.Core;
using ResourceCache.Core;

public class TestFilter : ScreenFilter
{
    private ResourceHandle<Effect> _effect;
    private Material _mat;

    public TestFilter(PraxisGame game) : base(game)
    {
        _effect = game.Resources.Load<Effect>("content/shaders/TestPostEffect.fxo");
        _mat = new Material(_effect);
    }

    public override void OnRender(RenderTarget2D source, RenderTarget2D? dest)
    {
        Blit(source, dest, _mat);
    }
}
