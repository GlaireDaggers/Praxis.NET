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
