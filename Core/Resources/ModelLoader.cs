using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ResourceCache.Core;

namespace Praxis.Core;

/// <summary>
/// Utility class responsible for loading Praxis PMDL model files
/// </summary>
internal static class ModelLoader
{
    // 80 bytes total
    private struct MeshVert
    {
        public Vector4 pos;         // 0
        public Vector4 normal;      // 16
        public Vector4 tangent;     // 32
        public Vector2 uv0;         // 48
        public Vector2 uv1;         // 56
        public Color color0;        // 64
        public Color color1;        // 68
        public Color boneJoints;    // 72
        public Color boneWeights;   // 76
    }

    private static VertexDeclaration MeshVertDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
        new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.Normal, 0),
        new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.Tangent, 0),
        new VertexElement(48, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(56, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(64, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(68, VertexElementFormat.Color, VertexElementUsage.Color, 1),
        new VertexElement(72, VertexElementFormat.Byte4, VertexElementUsage.TextureCoordinate, 2),
        new VertexElement(76, VertexElementFormat.Color, VertexElementUsage.TextureCoordinate, 3)
    );

    /// <summary>
    /// Load a model from the input stream
    /// </summary>
    public static Model Load(PraxisGame game, Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8);

        ulong magic = reader.ReadUInt64();
        Debug.Assert(magic == 0x4853454D53585250UL);

        uint version = reader.ReadUInt32();
        Debug.Assert(version == 100);

        uint numModelParts = reader.ReadUInt32();
        uint numMaterials = reader.ReadUInt32();
        uint numAnimations = reader.ReadUInt32();
        bool hasSkeleton = reader.ReadBoolean();

        float boundsX = reader.ReadSingle();
        float boundsY = reader.ReadSingle();
        float boundsZ = reader.ReadSingle();
        float boundsR = reader.ReadSingle();

        BoundingSphere bounds = new BoundingSphere(new Vector3(boundsX, boundsY, boundsZ), boundsR);

        // load materials
        ResourceHandle<Material>[] materials = new ResourceHandle<Material>[numMaterials];
        for (int i = 0; i < numMaterials; i++)
        {
            string materialPath = reader.ReadString();
            materials[i] = game.Resources.Load<Material>(materialPath);
        }

        // load model parts
        ModelPart[] parts = new ModelPart[numModelParts];
        for (int i = 0; i < numModelParts; i++)
        {
            parts[i] = ReadModelPart(game, reader, materials, bounds);
        }

        // load skeleton
        Skeleton? skeleton = null;
        SkeletonAnimation[]? animations = null;
        if (hasSkeleton)
        {
            uint numSkeletonNodes = reader.ReadUInt32();

            Dictionary<Skeleton.SkeletonNode, uint[]> childrenMap = new Dictionary<Skeleton.SkeletonNode, uint[]>();
            Skeleton.SkeletonNode[] nodes = new Skeleton.SkeletonNode[numSkeletonNodes];

            for (int i = 0; i < numSkeletonNodes; i++)
            {
                string name = reader.ReadString();
                int jointIndex = reader.ReadInt32();

                float posX = reader.ReadSingle();
                float posY = reader.ReadSingle();
                float posZ = reader.ReadSingle();

                float rotX = reader.ReadSingle();
                float rotY = reader.ReadSingle();
                float rotZ = reader.ReadSingle();
                float rotW = reader.ReadSingle();

                float scaleX = reader.ReadSingle();
                float scaleY = reader.ReadSingle();
                float scaleZ = reader.ReadSingle();

                uint childCount = reader.ReadUInt32();
                uint[] children = new uint[childCount];

                for (int j = 0; j < childCount; j++)
                {
                    children[j] = reader.ReadUInt32();
                }

                Skeleton.SkeletonNode node = new Skeleton.SkeletonNode(name)
                {
                    BoneIndex = jointIndex,
                    LocalRestPosition = new Vector3(posX, posY, posZ),
                    LocalRestRotation = new Quaternion(rotX, rotY, rotZ, rotW),
                    LocalRestScale = new Vector3(scaleX, scaleY, scaleZ)
                };

                node.LocalBindPose = Matrix.CreateScale(node.LocalRestScale)
                    * Matrix.CreateFromQuaternion(node.LocalRestRotation)
                    * Matrix.CreateTranslation(node.LocalRestPosition);
                node.BindPose = node.LocalBindPose;
                node.InverseBindPose = Matrix.Invert(node.BindPose);

                nodes[i] = node;
                childrenMap.Add(node, children);
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                SetHierarchy(nodes[i], nodes, childrenMap);
            }

            // first node is always the root
            skeleton = new Skeleton(nodes[0]);

            // load animations
            animations = new SkeletonAnimation[numAnimations];
            for (int i = 0; i < numAnimations; i++)
            {
                animations[i] = ReadAnimation(nodes, reader);
            }
        }

        Model model = new Model
        {
            bounds = bounds,
            skeleton = skeleton
        };

        model.parts.AddRange(parts);

        if (animations != null)
        {
            model.animations.AddRange(animations);
        }

        return model;
    }

    private static SkeletonAnimation ReadAnimation(Skeleton.SkeletonNode[] nodes, BinaryReader reader)
    {
        string name = reader.ReadString();
        float duration = reader.ReadSingle();
        uint channelCount = reader.ReadUInt32();

        SkeletonAnimation animation = new SkeletonAnimation(name, duration);

        for (int i = 0; i < channelCount; i++)
        {
            int targetId = reader.ReadInt32();

            var pos = ReadVector3Curve(reader);
            var rot = ReadQuaternionCurve(reader);
            var scale = ReadVector3Curve(reader);

            if (targetId != -1)
            {
                Skeleton.SkeletonNode target = nodes[targetId];
                animation.AnimationChannels[target] = new SkeletonAnimation.SkeletonAnimationChannel
                {
                    translationCurve = pos,
                    rotationCurve = rot,
                    scaleCurve = scale
                };
            }
        }

        return animation;
    }

    private static Vector3AnimationCurve? ReadVector3Curve(BinaryReader reader)
    {
        CurveInterpolationMode interpolationMode = (CurveInterpolationMode)reader.ReadByte();
        uint numKeys = reader.ReadUInt32();

        if (numKeys == 0) return null;

        Vector3AnimationCurve.CurvePoint[] points = new Vector3AnimationCurve.CurvePoint[numKeys];

        for (int i = 0; i < numKeys; i++)
        {
            points[i].time = reader.ReadSingle();

            if (interpolationMode == CurveInterpolationMode.Cubic)
            {
                points[i].tangentIn.X = reader.ReadSingle();
                points[i].tangentIn.Y = reader.ReadSingle();
                points[i].tangentIn.Z = reader.ReadSingle();

                points[i].value.X = reader.ReadSingle();
                points[i].value.Y = reader.ReadSingle();
                points[i].value.Z = reader.ReadSingle();

                points[i].tangentOut.X = reader.ReadSingle();
                points[i].tangentOut.Y = reader.ReadSingle();
                points[i].tangentOut.Z = reader.ReadSingle();
            }
            else
            {
                points[i].value.X = reader.ReadSingle();
                points[i].value.Y = reader.ReadSingle();
                points[i].value.Z = reader.ReadSingle();
            }
        }

        return new Vector3AnimationCurve(interpolationMode, points);
    }

    private static QuaternionAnimationCurve? ReadQuaternionCurve(BinaryReader reader)
    {
        CurveInterpolationMode interpolationMode = (CurveInterpolationMode)reader.ReadByte();
        uint numKeys = reader.ReadUInt32();

        if (numKeys == 0) return null;

        QuaternionAnimationCurve.CurvePoint[] points = new QuaternionAnimationCurve.CurvePoint[numKeys];

        for (int i = 0; i < numKeys; i++)
        {
            points[i].time = reader.ReadSingle();

            if (interpolationMode == CurveInterpolationMode.Cubic)
            {
                points[i].tangentIn.X = reader.ReadSingle();
                points[i].tangentIn.Y = reader.ReadSingle();
                points[i].tangentIn.Z = reader.ReadSingle();
                points[i].tangentIn.W = reader.ReadSingle();

                points[i].value.X = reader.ReadSingle();
                points[i].value.Y = reader.ReadSingle();
                points[i].value.Z = reader.ReadSingle();
                points[i].value.W = reader.ReadSingle();

                points[i].tangentOut.X = reader.ReadSingle();
                points[i].tangentOut.Y = reader.ReadSingle();
                points[i].tangentOut.Z = reader.ReadSingle();
                points[i].tangentOut.W = reader.ReadSingle();
            }
            else
            {
                points[i].value.X = reader.ReadSingle();
                points[i].value.Y = reader.ReadSingle();
                points[i].value.Z = reader.ReadSingle();
                points[i].value.W = reader.ReadSingle();
            }
        }

        return new QuaternionAnimationCurve(interpolationMode, points);
    }

    private static void SetHierarchy(Skeleton.SkeletonNode currentNode, Skeleton.SkeletonNode[] nodes, Dictionary<Skeleton.SkeletonNode, uint[]> childMap)
    {
        uint[] children = childMap[currentNode];

        for(int i = 0; i < children.Length; i++)
        {
            var child = nodes[children[i]];
            child.Parent = currentNode;
            child.BindPose = child.LocalBindPose * currentNode.BindPose;
            child.InverseBindPose = Matrix.Invert(child.BindPose);

            currentNode.Children.Add(child);
        }
    }

    private static ModelPart ReadModelPart(PraxisGame game, BinaryReader reader, ResourceHandle<Material>[] materials, BoundingSphere bounds)
    {
        uint matId = reader.ReadUInt32();
        uint numVertices = reader.ReadUInt32();
        uint numIndices = reader.ReadUInt32();

        MeshVert[] vertices = new MeshVert[numVertices];
        ushort[] indices = new ushort[numIndices];

        for (int i = 0; i < numVertices; i++)
        {
            vertices[i].pos.X = reader.ReadSingle();
            vertices[i].pos.Y = reader.ReadSingle();
            vertices[i].pos.Z = reader.ReadSingle();
            vertices[i].pos.W = 1f;

            vertices[i].normal.X = reader.ReadSingle();
            vertices[i].normal.Y = reader.ReadSingle();
            vertices[i].normal.Z = reader.ReadSingle();
            vertices[i].normal.W = 0f;

            vertices[i].tangent.X = reader.ReadSingle();
            vertices[i].tangent.Y = reader.ReadSingle();
            vertices[i].tangent.Z = reader.ReadSingle();
            vertices[i].tangent.W = reader.ReadSingle();
            
            vertices[i].uv0.X = reader.ReadSingle();
            vertices[i].uv0.Y = reader.ReadSingle();

            vertices[i].uv1.X = reader.ReadSingle();
            vertices[i].uv1.Y = reader.ReadSingle();

            vertices[i].color0.R = reader.ReadByte();
            vertices[i].color0.G = reader.ReadByte();
            vertices[i].color0.B = reader.ReadByte();
            vertices[i].color0.A = reader.ReadByte();

            vertices[i].color1.R = reader.ReadByte();
            vertices[i].color1.G = reader.ReadByte();
            vertices[i].color1.B = reader.ReadByte();
            vertices[i].color1.A = reader.ReadByte();

            vertices[i].boneJoints.R = reader.ReadByte();
            vertices[i].boneJoints.G = reader.ReadByte();
            vertices[i].boneJoints.B = reader.ReadByte();
            vertices[i].boneJoints.A = reader.ReadByte();

            vertices[i].boneWeights.R = reader.ReadByte();
            vertices[i].boneWeights.G = reader.ReadByte();
            vertices[i].boneWeights.B = reader.ReadByte();
            vertices[i].boneWeights.A = reader.ReadByte();
        }

        for (int i = 0; i < numIndices; i++)
        {
            indices[i] = reader.ReadUInt16();
        }

        VertexBuffer vb = new VertexBuffer(game.GraphicsDevice, MeshVertDeclaration, (int)numVertices, BufferUsage.WriteOnly);
        IndexBuffer ib = new IndexBuffer(game.GraphicsDevice, IndexElementSize.SixteenBits, (int)numIndices, BufferUsage.WriteOnly);

        vb.SetData(vertices);
        ib.SetData(indices);

        Mesh mesh = new(vb, ib, PrimitiveType.TriangleList, indices.Length / 3);
        return new ModelPart(mesh, materials[matId]);
    }
}
