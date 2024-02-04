namespace Praxis.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Represents a hierarchy for skeletal animation
/// </summary>
public class Skeleton
{
    /// <summary>
    /// Represents a single node in the hierarchy
    /// </summary>
    public class SkeletonNode
    {
        /// <summary>
        /// The name of this node
        /// </summary>
        public string Name;

        /// <summary>
        /// The index into the skinning matrix palette this bone corresponds to, if any
        /// </summary>
        public int BoneIndex;

        /// <summary>
        /// The default rest position of this node
        /// </summary>
        public Vector3 LocalRestPosition;

        /// <summary>
        /// The default rest rotation of this node
        /// </summary>
        public Quaternion LocalRestRotation;

        /// <summary>
        /// The default rest scale of this node
        /// </summary>
        public Vector3 LocalRestScale;

        /// <summary>
        /// This node's default rest pose (in local space)
        /// </summary>
        public Matrix LocalRestPose;

        /// <summary>
        /// This node's default rest pose (relative to the model root)
        /// </summary>
        public Matrix RestPose;

        /// <summary>
        /// The inverse of this node's bind pose
        /// </summary>
        public Matrix InverseBindPose;

        /// <summary>
        /// This node's parent, if any
        /// </summary>
        public SkeletonNode? Parent = null;

        /// <summary>
        /// This node's children
        /// </summary>
        public List<SkeletonNode> Children = new List<SkeletonNode>();

        public SkeletonNode(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"(Skeleton Node) \"{Name}\"";
        }
    }

    public readonly SkeletonNode Root;

    public Skeleton(SkeletonNode root)
    {
        Root = root;
    }
}

/// <summary>
/// An animation which operates on a particular skeleton's hierarchy
/// </summary>
public class SkeletonAnimation
{
    public struct SkeletonAnimationChannel
    {
        public Vector3AnimationCurve? translationCurve;
        public QuaternionAnimationCurve? rotationCurve;
        public Vector3AnimationCurve? scaleCurve;

        public (Vector3?, Quaternion?, Vector3?) Sample(float time)
        {
            return (translationCurve?.Sample(time), rotationCurve?.Sample(time), scaleCurve?.Sample(time));
        }
    }

    public readonly string Name;
    public readonly Dictionary<Skeleton.SkeletonNode, SkeletonAnimationChannel> AnimationChannels = new Dictionary<Skeleton.SkeletonNode, SkeletonAnimationChannel>();
    public readonly float Length;

    public SkeletonAnimation(string name, float length)
    {
        Name = name;
        Length = length;
    }
}

/// <summary>
/// A model in Praxis, acting as a container of ModelParts, an optional skeleton, and an optional set of named animations
/// </summary>
public class Model
{
    public List<ModelPart> parts = new List<ModelPart>();
    public CollisionMesh? collision = null;
    public BoundingSphere bounds;
    public Skeleton? skeleton = null;
    public List<SkeletonAnimation> animations = new List<SkeletonAnimation>();

    /// <summary>
    /// Find the index of an animation by name, or -1 if the animation does not exist
    /// </summary>
    public int GetAnimationId(string name)
    {
        for (int i = 0; i < animations.Count; i++)
        {
            if (animations[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>
/// A model part, referencing a mesh & a material
/// </summary>
public class ModelPart
{
    public Mesh mesh;
    public RuntimeResource<Material> material;

    public ModelPart(Mesh mesh, RuntimeResource<Material> material)
    {
        this.mesh = mesh;
        this.material = material;
    }
}

/// <summary>
/// Contains information which can be used to create a mesh collision shape
/// </summary>
public class CollisionMesh
{
    public struct Triangle
    {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
    }

    public Triangle[] triangles;

    public CollisionMesh(Triangle[] triangles)
    {
        this.triangles = triangles;
    }
}

/// <summary>
/// A mesh, acting as a wrapper around an underlying vertex & index buffer
/// </summary>
public class Mesh : IDisposable
{
    public VertexBuffer vertexBuffer;
    public IndexBuffer indexBuffer;
    public PrimitiveType primitiveType;
    public int startIndex;
    public int primitiveCount;

    public Mesh(VertexBuffer vertexBuffer, IndexBuffer indexBuffer, PrimitiveType primitiveType, int primitiveCount)
    {
        this.vertexBuffer = vertexBuffer;
        this.indexBuffer = indexBuffer;
        this.primitiveType = primitiveType;
        this.startIndex = 0;
        this.primitiveCount = primitiveCount;
    }

    public void Dispose()
    {
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
    }
}