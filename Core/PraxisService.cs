namespace Praxis.Core;

/// <summary>
/// Base class for a global service on a Praxis game
/// </summary>
public class PraxisService
{
    public readonly PraxisGame Game;

    public PraxisService(PraxisGame game)
    {
        Game = game;
    }

    public virtual void Update(float deltaTime)
    {
    }
}
