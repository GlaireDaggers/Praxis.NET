using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Praxis.Core;

/// <summary>
/// A simple splash screen game state which displays a splash image for some amount of time before transitioning to another game state
/// </summary>
public class SplashGameState(PraxisGame game, float duration, Color fadeColor, RuntimeResource<Texture2D> image, GameState? nextState) : GameState(game)
{
    private const float FADE_DURATION = 0.5f;

    private RuntimeResource<Texture2D> _image = image;
    private GameState? _nextState = nextState;
    private SpriteBatch? _sb;

    private float _timer = 0f;
    private float _duration = duration;

    private Color _fadeColor = fadeColor;

    public override void OnEnter()
    {
        base.OnEnter();
        _sb = new SpriteBatch(Game.GraphicsDevice);
    }

    public override void OnExit()
    {
        base.OnExit();
        _sb!.Dispose();
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        _timer += deltaTime;
        if (_timer >= _duration)
        {
            Game.SetState(_nextState);
        }
    }

    public override void Draw()
    {
        base.Draw();

        float fade = 1f;

        if (_timer < FADE_DURATION)
        {
            fade = _timer / FADE_DURATION;
        }
        else if (_timer > (_duration - FADE_DURATION))
        {
            fade = (_duration - _timer) / FADE_DURATION;
        }

        Game.GraphicsDevice.Clear(_fadeColor);
        _sb!.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
        {
            // scale image to fit screen
            var img = _image.Value;
            var img_aspect = (float)img.Width / img.Height;
            var ws = Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
            var hs = Game.GraphicsDevice.PresentationParameters.BackBufferHeight;
            var screen_aspect = (float)ws / hs;

            int new_width, new_height;

            if (screen_aspect > img_aspect)
            {
                new_width = (int)(img.Width * ((float)hs / img.Height));
                new_height = hs;
            }
            else
            {
                new_width = ws;
                new_height = (int)(img.Height * ((float)ws / img.Width));
            }

            // draw centered
            Vector2 centerPos = new Vector2((ws - new_width) / 2, (hs - new_height) / 2);
            _sb.Draw(img, new Rectangle((int)centerPos.X, (int)centerPos.Y, new_width, new_height), new Color(1f, 1f, 1f, fade));
        }
        _sb.End();
    }
}
