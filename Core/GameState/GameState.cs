namespace Praxis.Core;

/// <summary>
/// Base class for global game state logic
/// </summary>
public class GameState
{
    public readonly PraxisGame Game;

    public GameState(PraxisGame game)
    {
        Game = game;
    }

    public virtual void OnEnter()
    {
    }

    public virtual void OnExit()
    {
    }

    public virtual void Update(float deltaTime)
    {
    }

    public virtual void Draw()
    {
    }
}
