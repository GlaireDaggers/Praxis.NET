using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Praxis.Core;
using ResourceCache.Core.FS;
using SDL2;

namespace Example;

public class ExampleGame : PraxisGame
{
    public ExampleGame() : base("Example Game", 1280, 720)
    {
        var icon = SDL.SDL_LoadBMP("icon_32.bmp");
        SDL.SDL_SetWindowIcon(Window.Handle, icon);
    }

    protected override void Init()
    {
        base.Init();

        GraphicsDevice.PresentationParameters.MultiSampleCount = 4;

        // set up input
        Input.BindInput("Move X", new CompositeAxisSource
        {
            sources = [
                new GamepadAxisSource(PlayerIndex.One, GamepadAxis.LeftStickX),
                new DualButtonAxisSource(new KeyboardButtonSource(Keys.D), new KeyboardButtonSource(Keys.A))
            ]
        });

        Input.BindInput("Move Y", new CompositeAxisSource
        {
            sources = [
                new GamepadAxisSource(PlayerIndex.One, GamepadAxis.LeftStickY),
                new DualButtonAxisSource(new KeyboardButtonSource(Keys.W), new KeyboardButtonSource(Keys.S))
            ]
        });

        Input.BindInput("Camera X", new CompositeAxisSource
        {
            sources = [
                new GamepadAxisSource(PlayerIndex.One, GamepadAxis.RightStickX),
                new DualButtonAxisSource(new KeyboardButtonSource(Keys.Right), new KeyboardButtonSource(Keys.Left))
            ]
        });

        Input.BindInput("Camera Y", new CompositeAxisSource
        {
            sources = [
                new GamepadAxisSource(PlayerIndex.One, GamepadAxis.RightStickY),
                new DualButtonAxisSource(new KeyboardButtonSource(Keys.Up), new KeyboardButtonSource(Keys.Down))
            ]
        });

        #if DEBUG
        Resources.Mount("content", new FolderFS("content/bin"), true);
        #else
        Resources.Mount("content", new FolderFS("content/bin"), false);
        #endif

        //SetState(new DefaultGameState(this));
        SetState(new SplashGameState(this, 3f, Color.Black, Resources.Load<Texture2D>("content/image/splash.dds"), new DefaultGameState(this)));
    }
}
