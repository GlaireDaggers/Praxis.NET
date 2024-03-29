﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Praxis.Core;
using ResourceCache.Core.FS;
using SDL2;

namespace Platformer;

public class StarterGame : PraxisGame
{
    public StarterGame() : base("Starter Game", 1280, 720)
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

        #if DEBUG
        Resources.Mount("content", new FolderFS("content/bin"), true);
        #else
        Resources.Mount("content", new FolderFS("content/bin"), false);
        #endif

        SetState(new SplashGameState(this, 3f, Color.Black, Resources.Load<Texture2D>("content/image/splash.dds"), new DefaultGameState(this)));
    }
}
