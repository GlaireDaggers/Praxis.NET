namespace Praxis.Core;

using Microsoft.Xna.Framework;

public struct CachedPoseComponent
{
    public const int MAX_BONES = 128;

    public readonly Matrix[] Pose;

    public CachedPoseComponent()
    {
        Pose = new Matrix[MAX_BONES];

        for (int i = 0; i < Pose.Length; i++)
        {
            Pose[i] = Matrix.Identity;
        }
    }
}
