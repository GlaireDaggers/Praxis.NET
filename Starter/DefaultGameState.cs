using Microsoft.Xna.Framework;
using Praxis.Core;
using Praxis.Core.ECS;

namespace Starter;

/// <summary>
/// Default game state for starter game
/// </summary>
public class DefaultGameState : GameState
{
    private readonly WorldContext _context;

    public DefaultGameState(PraxisGame game) : base(game)
    {
        _context = new WorldContext("Starter World", Game);
    }

    public override void OnEnter()
    {
        base.OnEnter();

        Game.RegisterContext(_context);
        Game.InstallDefaultSystems(_context);
    }

    public override void OnExit()
    {
        base.OnExit();
        Game.UnregisterContext(_context);
    }

    public override void Draw()
    {
        base.Draw();
        Game.GraphicsDevice.Clear(Color.CornflowerBlue);
    }
}
