namespace Praxis.Core;

using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpGLTF.Schema2;

using PrimitiveType = Microsoft.Xna.Framework.Graphics.PrimitiveType;

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
        public readonly string Name;

        /// <summary>
        /// The index into the skinning matrix palette this bone corresponds to, if any
        /// </summary>
        public readonly int BoneIndex;

        /// <summary>
        /// The default rest position of this node
        /// </summary>
        public readonly Vector3 LocalRestPosition;

        /// <summary>
        /// The default rest rotation of this node
        /// </summary>
        public readonly Quaternion LocalRestRotation;

        /// <summary>
        /// The default rest scale of this node
        /// </summary>
        public readonly Vector3 LocalRestScale;

        /// <summary>
        /// This node's default rest transform
        /// </summary>
        public readonly Matrix LocalRestPose;

        /// <summary>
        /// The inverse of this node's default bind pose, relative to the root node
        /// </summary>
        public readonly Matrix InverseBindPose;

        /// <summary>
        /// This node's parent, if any
        /// </summary>
        public readonly SkeletonNode? Parent = null;

        /// <summary>
        /// This node's children
        /// </summary>
        public readonly List<SkeletonNode> Children = new List<SkeletonNode>();

        internal SkeletonNode(SkeletonNode? parent, Node srcNode, Dictionary<Node, int> jointmap, Dictionary<Node, SkeletonNode> nodemap)
        {
            Name = srcNode.Name;
            Parent = parent;
            LocalRestPosition = new Vector3(srcNode.LocalTransform.Translation.X, srcNode.LocalTransform.Translation.Y, srcNode.LocalTransform.Translation.Z);
            LocalRestRotation = new Quaternion(srcNode.LocalTransform.Rotation.X, srcNode.LocalTransform.Rotation.Y, srcNode.LocalTransform.Rotation.Z, srcNode.LocalTransform.Rotation.W);
            LocalRestScale = new Vector3(srcNode.LocalTransform.Scale.X, srcNode.LocalTransform.Scale.Y, srcNode.LocalTransform.Scale.Z);
            LocalRestPose = Matrix.CreateScale(LocalRestScale) * Matrix.CreateFromQuaternion(LocalRestRotation) * Matrix.CreateTranslation(LocalRestPosition);
            InverseBindPose = Matrix.Invert(ToFNA(srcNode.WorldMatrix));

            if (!jointmap.TryGetValue(srcNode, out BoneIndex))
            {
                BoneIndex = -1;
            }

            foreach (var child in srcNode.VisualChildren)
            {
                var childNode = new SkeletonNode(this, child, jointmap, nodemap);
                Children.Add(childNode);
            }

            nodemap.Add(srcNode, this);
        }

        private static Matrix ToFNA(System.Numerics.Matrix4x4 m)
        {
            return Unsafe.As<System.Numerics.Matrix4x4, Matrix>(ref m);
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

    public readonly Dictionary<Skeleton.SkeletonNode, SkeletonAnimationChannel> AnimationChannels = new Dictionary<Skeleton.SkeletonNode, SkeletonAnimationChannel>();
    public readonly float Length;

    public SkeletonAnimation(float length)
    {
        Length = length;
    }
}

/// <summary>
/// A model in Praxis, acting as a container of ModelParts, an optional skeleton, and an optional set of named animations
/// </summary>
public class Model
{
    public List<ModelPart> parts = new List<ModelPart>();
    public BoundingSphere bounds;
    public Skeleton? skeleton = null;
    public Dictionary<string, SkeletonAnimation> animations = new Dictionary<string, SkeletonAnimation>();

    public void RecalcBounds()
    {
        if (parts.Count > 0)
        {
            bounds = parts[0].Bounds;
            
            for (int i = 1; i < parts.Count; i++)
            {
                BoundingSphere b = parts[i].Bounds;
                BoundingSphere.CreateMerged(ref bounds, ref b, out bounds);
            }
        }
        else
        {
            bounds = new BoundingSphere(Vector3.Zero, 0f);
        }
    }
}

/// <summary>
/// A model part, referencing a mesh, a material, and a local transform relative to its parent Model container
/// </summary>
public class ModelPart
{
    public BoundingSphere Bounds
    {
        get
        {
            return mesh.bounds.Transform(localTransform);
        }
    }

    public Matrix localTransform = Matrix.Identity;
    public Mesh mesh;
    public RuntimeResource<Material> material;

    public ModelPart(Mesh mesh, RuntimeResource<Material> material)
    {
        this.mesh = mesh;
        this.material = material;
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
    public BoundingSphere bounds;

    public Mesh(VertexBuffer vertexBuffer, IndexBuffer indexBuffer, PrimitiveType primitiveType, int primitiveCount)
    {
        this.bounds = new BoundingSphere(Vector3.Zero, 0f);
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