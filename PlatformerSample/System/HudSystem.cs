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
        _uiCanvas = new Canvas();
        
        _score = new TextWidget
        {
            left = Unit.Pixels(8),
            top = Unit.Pixels(8),
            font = Game.Resources.Load<Font>("content/font/RussoOne-Regular.ttf"),
            text = "Score: 0",
            size = 32,
        };
        _uiCanvas.AddWidget(_score);
        _uiRenderer = new UIRenderer(Game.GraphicsDevice);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        foreach (var _ in World.GetMessages<PickupMessage>())
        {
            var stats = World.GetSingleton<PlayerStats>();
            _score.text = $"Score: {stats.score}";
        }

        _uiCanvas.UpdateLayout(_uiRenderer);
        _uiCanvas.DrawUI(_uiRenderer);
    }
}
