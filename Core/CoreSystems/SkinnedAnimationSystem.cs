﻿using Microsoft.Xna.Framework;
using MoonTools.ECS;

namespace Praxis.Core;

/// <summary>
/// Base class for a system which performs skeletal animation
/// </summary>
public class SkinnedAnimationSystem : PraxisSystem
{
    /// <summary>
    /// Enumeration of ways bone animation can be blend into the current pose
    /// </summary>
    public enum AnimationBlendOp
    {
        Mix,
        Additive,
        Replace,
    }

    private Dictionary<Skeleton.SkeletonNode, Matrix> _nodeTransformCache = new Dictionary<Skeleton.SkeletonNode, Matrix>();

    private Filter? _animFilter = null;

    public SkinnedAnimationSystem(WorldContext context) : base(context)
    {
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        if (_animFilter != null)
        {
            foreach (var entity in _animFilter.Entities)
            {
                CachedPoseComponent pose;
                if (World.Has<CachedPoseComponent>(entity))
                {
                    pose = World.Get<CachedPoseComponent>(entity);
                }
                else
                {
                    pose = new CachedPoseComponent();
                }

                Update(entity, pose.Pose.Value!, deltaTime);
                World.Set(entity, pose);
            }
        }
    }

    /// <summary>
    /// Called on each animated entity to update its pose
    /// </summary>
    protected virtual void Update(in Entity entity, Matrix[] pose, float deltaTime)
    {
    }

    /// <summary>
    /// Set the filter used to iterate over animated entities
    /// </summary>
    protected void SetFilter(Filter filter)
    {
        _animFilter = filter;
    }

    /// <summary>
    /// Reset bone transforms to their rest pose
    /// </summary>
    /// <param name="model">The model to get the rest pose from</param>
    /// <param name="outBoneTransforms">The bone transforms array to write to</param>
    protected void ClearBoneTransforms(Model model, Matrix[] outBoneTransforms)
    {
        if (model.skeleton == null)
        {
            return;
        }

        // compute skeleton hierarchy positions
        _nodeTransformCache.Clear();
        UpdateAnimationNode(0f, null, model.skeleton.Root, outBoneTransforms, AnimationBlendOp.Replace);
    }

    /// <summary>
    /// Compute animation pose and blend the results into the bone transforms
    /// </summary>
    /// <param name="animationId">The animation ID</param>
    /// <param name="time">Animation time to sample</param>
    /// <param name="model">The model to animate</param>
    /// <param name="outBoneTransforms">The bone transforms array to write to</param>
    /// <param name="blendOp">How to blend the pose into th bone transforms array</param>
    /// <param name="blendA">Alpha value to use for pose blending</param>
    protected void BlendAnimationResult(string animationId, float time, Model model, Matrix[] outBoneTransforms,
        AnimationBlendOp blendOp = AnimationBlendOp.Mix, float blendA = 1f)
    {
        if (model.skeleton == null)
        {
            return;
        }

        model.animations.TryGetValue(animationId, out var animation);

        // compute skeleton hierarchy positions
        _nodeTransformCache.Clear();
        UpdateAnimationNode(time, animation, model.skeleton.Root, outBoneTransforms, blendOp, blendA);
    }

    private void UpdateAnimationNode(float time, SkeletonAnimation? animation, Skeleton.SkeletonNode node, Matrix[] outBoneTransforms,
        AnimationBlendOp blendOp = AnimationBlendOp.Mix, float blendA = 1f)
    {
        if (animation != null && animation.AnimationChannels.TryGetValue(node, out var nodeAnimation))
        {
            var transform = nodeAnimation.Sample(time % animation.Length);
            Matrix trs = Matrix.CreateScale(transform.Item3 ?? node.LocalRestScale)
                * Matrix.CreateFromQuaternion(transform.Item2 ?? node.LocalRestRotation)
                * Matrix.CreateTranslation(transform.Item1 ?? node.LocalRestPosition);

            if (node.Parent != null)
            {
                trs *= _nodeTransformCache[node.Parent];
            }

            _nodeTransformCache[node] = trs;
        }
        else
        {
            Matrix trs = node.LocalRestPose;

            if (node.Parent != null)
            {
                trs *= _nodeTransformCache[node.Parent];
            }

            _nodeTransformCache[node] = trs;
        }

        if (node.BoneIndex != -1)
        {
            switch (blendOp)
            {
                case AnimationBlendOp.Additive:
                    Matrix additiveMat = LerpBoneTransform(Matrix.Identity, node.InverseBindPose * _nodeTransformCache[node], blendA);
                    outBoneTransforms[node.BoneIndex] = outBoneTransforms[node.BoneIndex] * additiveMat;
                    break;
                case AnimationBlendOp.Mix:
                    outBoneTransforms[node.BoneIndex] = LerpBoneTransform(outBoneTransforms[node.BoneIndex], node.InverseBindPose * _nodeTransformCache[node], blendA);
                    break;
                case AnimationBlendOp.Replace:
                    outBoneTransforms[node.BoneIndex] = node.InverseBindPose * _nodeTransformCache[node];
                    break;
            }
        }

        foreach (var child in node.Children)
        {
            UpdateAnimationNode(time, animation, child, outBoneTransforms, blendOp, blendA);
        }
    }

    private Matrix LerpBoneTransform(Matrix lhs, Matrix rhs, float t)
    {
        // decompose into translation, rotation, scale and lerp separately
        lhs.Decompose(out var scaleLhs, out var rotLhs, out var posLhs);
        rhs.Decompose(out var scaleRhs, out var rotRhs, out var posRhs);

        return Matrix.CreateScale(Vector3.Lerp(scaleLhs, scaleRhs, t))
            * Matrix.CreateFromQuaternion(Quaternion.Slerp(rotLhs, rotRhs, t))
            * Matrix.CreateTranslation(Vector3.Lerp(posLhs, posRhs, t));
    }
}

/// <summary>
/// Example simple skinned animation system
/// </summary>
public class SimpleAnimationSystem : SkinnedAnimationSystem
{
    public SimpleAnimationSystem(WorldContext context) : base(context)
    {
        SetFilter(World.FilterBuilder
            .Include<SimpleAnimationComponent>()
            .Include<ModelComponent>()
            .Build()
        );
    }

    protected override void Update(in Entity entity, Matrix[] pose, float deltaTime)
    {
        base.Update(entity, pose, deltaTime);
        
        var animComp = World.Get<SimpleAnimationComponent>(entity);
        var modelComp = World.Get<ModelComponent>(entity);

        var model = modelComp.modelHandle.Value;
        string? anim = animComp.Animation.Value;

        if (anim == null)
        {
            ClearBoneTransforms(model.Value, pose);
        }
        else
        {
            BlendAnimationResult(anim, animComp.time, model.Value, pose, AnimationBlendOp.Replace);
        }

        animComp.time += deltaTime;
        World.Set(entity, animComp);
    }
}