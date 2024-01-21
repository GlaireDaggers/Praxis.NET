namespace Praxis.Core;

using Microsoft.Xna.Framework;

public struct TransformComponent
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public TransformComponent(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;
    }
}