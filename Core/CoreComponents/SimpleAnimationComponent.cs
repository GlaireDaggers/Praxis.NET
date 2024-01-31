using Praxis.Core.ECS;

namespace Praxis.Core;

/// <summary>
/// Simple example animation component
/// </summary>
public struct SimpleAnimationComponent
{
    public int animationId;

    public float time;

    public SimpleAnimationComponent()
    {
        animationId = -1;
        time = 0f;
    }

    /// <summary>
    /// Play new animation
    /// </summary>
    /// <param name="animId">The animation ID</param>
    public void PlayAnimation(int animId)
    {
        animationId = animId;
        time = 0f;
    }

    /// <summary>
    /// Play a new animation, if it isn't already playing
    /// </summary>
    /// <param name="anim">The animation ID</param>
    public void SetAnimation(int animId)
    {
        if (animationId != animId)
        {
            PlayAnimation(animId);
        }
    }
}

[SerializedComponent(nameof(SimpleAnimationComponent))]
public class SimpleAnimationComponentData : IComponentData
{
    private SimpleAnimationComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new SimpleAnimationComponent();
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}