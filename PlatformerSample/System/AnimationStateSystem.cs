using Microsoft.Xna.Framework;
using Praxis.Core;
using Praxis.Core.ECS;

namespace PlatformerSample;

public class AnimationStateSystem : SkinnedAnimationSystem
{
    public AnimationStateSystem(WorldContext context) : base(context)
    {
        SetFilter(new FilterBuilder(World)
            .Include<AnimationStateComponent>()
            .Include<ModelComponent>()
            .Build("AnimationStateSystem.filter")
        );
    }

    protected override void Update(in Entity entity, Matrix[] pose, float deltaTime)
    {
        base.Update(entity, pose, deltaTime);

        var animComp = World.Get<AnimationStateComponent>(entity);
        var modelComp = World.Get<ModelComponent>(entity);

        var model = modelComp.model.Value;

        if (animComp.currentState == -1)
        {
            ClearBoneTransforms(model, pose);
        }
        else
        {
            BlendAnimationResult(animComp.currentState, animComp.currentTime, model, pose, AnimationBlendOp.Replace);

            if (animComp.nextState != null)
            {
                float t = animComp.transition / animComp.transitionTime;
                BlendAnimationResult(animComp.nextState.Value, animComp.nextTime, model, pose, AnimationBlendOp.Mix, AnimationLoopMode.Wrap, t);
            }
        }

        animComp.currentTime += deltaTime;
        animComp.nextTime += deltaTime;

        if (animComp.nextState != null)
        {
            animComp.transition += deltaTime;
            if (animComp.transition >= animComp.transitionTime)
            {
                animComp.currentState = animComp.nextState.Value;
                animComp.currentTime = animComp.nextTime;
                animComp.nextState = null;
            }
        }

        World.Set(entity, animComp);
    }
}
