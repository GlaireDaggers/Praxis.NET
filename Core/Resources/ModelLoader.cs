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

        List<CollisionMesh.Triangle> collisionTriangles = new List<CollisionMesh.Triangle>();

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
            parts[i] = ReadModelPart(game, reader, materials, collisionTriangles);
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

                Matrix invBindPose = ReadMatrix(reader);

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

                node.LocalRestPose = Matrix.CreateScale(node.LocalRestScale)
                    * Matrix.CreateFromQuaternion(node.LocalRestRotation)
                    * Matrix.CreateTranslation(node.LocalRestPosition);
                node.RestPose = node.LocalRestPose;
                node.InverseBindPose = invBindPose;

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
            skeleton = skeleton,
            collision = new CollisionMesh(collisionTriangles.ToArray())
        };

        model.parts.AddRange(parts);

        if (animations != null)
        {
            model.animations.AddRange(animations);
        }

        return model;
    }

    private static Matrix ReadMatrix(BinaryReader reader)
    {
        Matrix m = new Matrix();
        
        m.M11 = reader.ReadSingle();
        m.M12 = reader.ReadSingle();
        m.M13 = reader.ReadSingle();
        m.M14 = reader.ReadSingle();
        m.M21 = reader.ReadSingle();
        m.M22 = reader.ReadSingle();
        m.M23 = reader.ReadSingle();
        m.M24 = reader.ReadSingle();
        m.M31 = reader.ReadSingle();
        m.M32 = reader.ReadSingle();
        m.M33 = reader.ReadSingle();
        m.M34 = reader.ReadSingle();
        m.M41 = reader.ReadSingle();
        m.M42 = reader.ReadSingle();
        m.M43 = reader.ReadSingle();
        m.M44 = reader.ReadSingle();

        return m;
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
            points[i].Time = reader.ReadSingle();

            if (interpolationMode == CurveInterpolationMode.Cubic)
            {
                points[i].TangentIn = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );

                points[i].Value = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );

                points[i].TangentOut = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );
            }
            else
            {
                points[i].Value = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );
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
            points[i].Time = reader.ReadSingle();

            if (interpolationMode == CurveInterpolationMode.Cubic)
            {
                points[i].TangentIn = new Quaternion(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );

                points[i].Value = new Quaternion(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );

                points[i].TangentOut = new Quaternion(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );
            }
            else
            {
                points[i].Value = new Quaternion(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );
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
            child.RestPose = child.LocalRestPose * currentNode.RestPose;
            child.InverseBindPose = Matrix.Invert(child.RestPose);

            currentNode.Children.Add(child);
        }
    }

    private static ModelPart ReadModelPart(PraxisGame game, BinaryReader reader, ResourceHandle<Material>[] materials, List<CollisionMesh.Triangle> triangles)
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

        for (int i = 0; i < indices.Length; i += 3)
        {
            ref var vtxA = ref vertices[indices[i]];
            ref var vtxB = ref vertices[indices[i + 1]];
            ref var vtxC = ref vertices[indices[i + 2]];
            Vector3 a = new Vector3(vtxA.pos.X, vtxA.pos.Y, vtxA.pos.Z);
            Vector3 b = new Vector3(vtxB.pos.X, vtxB.pos.Y, vtxB.pos.Z);
            Vector3 c = new Vector3(vtxC.pos.X, vtxC.pos.Y, vtxC.pos.Z);
            triangles.Add(new CollisionMesh.Triangle
            {
                a = a,
                b = b,
                c = c
            });
        }

        VertexBuffer vb = new VertexBuffer(game.GraphicsDevice, MeshVertDeclaration, (int)numVertices, BufferUsage.WriteOnly);
        IndexBuffer ib = new IndexBuffer(game.GraphicsDevice, IndexElementSize.SixteenBits, (int)numIndices, BufferUsage.WriteOnly);

        vb.SetData(vertices);
        ib.SetData(indices);

        Mesh mesh = new(vb, ib, PrimitiveType.TriangleList, indices.Length / 3);
        return new ModelPart(mesh, materials[matId]);
    }
}
