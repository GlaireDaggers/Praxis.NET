using Microsoft.Xna.Framework;

namespace Praxis.Core;

public struct PhysicsConfigSingleton
{
    public float timestep;
    public float maxTimestep;
    public float sleepThreshold;
    public Vector3 gravity;
}
