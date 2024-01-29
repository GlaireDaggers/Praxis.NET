namespace Praxis.Core;

using Praxis.Core.ECS;

/// <summary>
/// Attribute to indicate that this system must execute after another system
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExecuteBeforeAttribute : Attribute
{
    public readonly Type systemType;

    public ExecuteBeforeAttribute(Type systemType)
    {
        this.systemType = systemType;
    }
}

/// <summary>
/// Attribute to indicate that this system must execute after another system
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExecuteAfterAttribute : Attribute
{
    public readonly Type systemType;

    public ExecuteAfterAttribute(Type systemType)
    {
        this.systemType = systemType;
    }
}

public enum SystemExecutionStage
{
    Update,
    PostUpdate,
    Draw,
}

/// <summary>
/// Base class for Praxis ECS systems
/// </summary>
public class PraxisSystem
{
    /// <summary>
    /// Indicates the phase during the frame at which this system executes
    /// </summary>
    public virtual SystemExecutionStage ExecutionStage => SystemExecutionStage.Update;

    public readonly PraxisGame Game;
    public readonly World World;

    /// <summary>
    /// Construct an instance of the system and install it into the given world context
    /// </summary>
    public PraxisSystem(WorldContext context)
    {
        Game = context.Game;
        World = context.World;
        context.RegisterSystem(this);
    }

    public virtual void Update(float deltaTime)
    {
    }
}