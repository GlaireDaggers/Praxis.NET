namespace Praxis.Core;

/// <summary>
/// Simple example animation component
/// </summary>
public struct SimpleAnimationComponent
{
    public ObjectHandle<string> Animation { get; private set; }

    public float time;

    public SimpleAnimationComponent()
    {
        Animation = ObjectHandle<string>.NULL;
        time = 0f;
    }

    /// <summary>
    /// Play new animation
    /// </summary>
    /// <param name="anim">The animation ID</param>
    public void PlayAnimation(string? anim)
    {
        Animation.Dispose();
        Animation = new ObjectHandle<string>(anim);
        time = 0f;
    }

    /// <summary>
    /// Play a new animation, if it isn't already playing
    /// </summary>
    /// <param name="anim">The animation ID</param>
    public void SetAnimation(string? anim)
    {
        if (Animation.Value != anim)
        {
            PlayAnimation(anim);
        }
    }
}
