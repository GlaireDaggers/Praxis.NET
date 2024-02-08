using System.Xml;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Praxis.Core;

namespace PlatformerSample;

[ExecuteAfter(typeof(BasicForwardRenderer))]
public class HudSystem : PraxisSystem
{
    public override SystemExecutionStage ExecutionStage => SystemExecutionStage.Draw;

    private Canvas _uiCanvas;
    private UIRenderer _uiRenderer;

    private TextWidget _score;

    public HudSystem(WorldContext context) : base(context)
    {
        _uiCanvas = Canvas.Load(Game, Game.Resources.Load<XmlDocument>("content/ui/hud.xml").Value);
        _score = (TextWidget)_uiCanvas.FindById("scoretext")!;

        _uiRenderer = new UIRenderer(Game.GraphicsDevice);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        foreach (var _ in World.GetMessages<PickupMessage>())
        {
            var stats = World.GetSingleton<PlayerStats>();
            _score.Text = $"/esScore: /c[yellow]{stats.score}";
        }

        _uiCanvas.UpdateLayout(_uiRenderer);
        _uiCanvas.DrawUI(_uiRenderer);
    }
}
