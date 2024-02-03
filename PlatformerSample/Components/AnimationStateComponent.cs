using Praxis.Core;
using Praxis.Core.ECS;

namespace PlatformerSample;

public struct AnimationStateComponent
{
    public int currentState;
    public int? nextState;
    public float transition;
    public float transitionTime;
    public float currentTime;
    public float nextTime;

    public AnimationStateComponent()
    {
        currentState = -1;
        nextState = null;
    }

    public void PlayAnimation(int animId, float transitionTime = 0.2f)
    {
        if (transitionTime == 0f)
        {
            currentState = animId;
            currentTime = 0f;
            nextState = null;
        }
        else
        {
            if (nextState is int i)
            {
                currentState = i;
                currentTime = nextTime;
            }

            nextState = animId;
            nextTime = 0f;
            transition = 0f;
            this.transitionTime = transitionTime;
        }
    }

    public void SetAnimation(int animId, float transitionTime = 0.2f)
    {
        if (currentState != animId && nextState != animId)
        {
            PlayAnimation(animId, transitionTime);   
        }
    }
}

[SerializedComponent(nameof(AnimationStateComponent))]
public class SimpleAnimationComponentData : IComponentData
{
    private AnimationStateComponent _comp;

    public void OnDeserialize(PraxisGame game)
    {
        _comp = new AnimationStateComponent();
    }

    public void Unpack(in Entity root, in Entity entity, World world)
    {
        world.Set(entity, _comp);
    }
}