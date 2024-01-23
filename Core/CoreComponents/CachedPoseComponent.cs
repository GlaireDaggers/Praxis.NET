namespace Praxis.Core;

using Microsoft.Xna.Framework;

public struct CachedPoseComponent
{
    public const int MAX_BONES = 128;

    public readonly ObjectHandle<Matrix[]> Pose;

    public CachedPoseComponent()
    {
        Matrix[] pose = new Matrix[MAX_BONES];

        for (int i = 0; i < pose.Length; i++)
        {
            pose[i] = Matrix.Identity;
        }

        Pose = new ObjectHandle<Matrix[]>(pose);
    }
}
