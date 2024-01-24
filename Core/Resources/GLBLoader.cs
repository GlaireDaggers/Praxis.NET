﻿namespace Praxis.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GltfModelRoot = SharpGLTF.Schema2.ModelRoot;
using GltfMaterial = SharpGLTF.Schema2.Material;
using GltfMesh = SharpGLTF.Schema2.Mesh;
using GltfTexture = SharpGLTF.Schema2.Texture;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpGLTF.Schema2;

using PrimitiveType = Microsoft.Xna.Framework.Graphics.PrimitiveType;

/// <summary>
/// Helper class for loading GLB models
/// </summary>
internal class GLBLoader
{
    public class ModelData
    {
        [JsonPropertyName("mesh")]
        public string? Mesh { get; set; }
        
        [JsonPropertyName("materials")]
        public Dictionary<string, string>? Materials { get; set; }
    }

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

    private struct Primitive
    {
        public Mesh mesh;
        public GltfMaterial material;
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

    public static Model LoadModel(PraxisGame game, Stream stream)
    {
        ModelData modelData = JsonSerializer.Deserialize<ModelData>(stream)!;
        var modelStream = game.Resources.Open(modelData.Mesh!);

        var modelRoot = GltfModelRoot.ReadGLB(modelStream);
        var model = new Model();

        var defaultShader = game.Resources.Load<Effect>("content/shaders/BasicLit.fxo");

        // convert logical meshes into Praxis runtime meshes
        Dictionary<GltfMesh, List<Primitive>> meshMap = new Dictionary<GltfMesh, List<Primitive>>();
        foreach (var m in modelRoot.LogicalMeshes)
        {
            var list = new List<Primitive>();
            LoadGltfMesh(game, m, list);
            meshMap[m] = list;
        }

        // convert skeleton hierarchy
        if (modelRoot.LogicalSkins.Count > 0)
        {
            var skin = modelRoot.LogicalSkins[0];
            Dictionary<Node, Skeleton.SkeletonNode> nodemap = new Dictionary<Node, Skeleton.SkeletonNode>();
            Dictionary<Node, int> jointmap = new Dictionary<Node, int>();

            for (int i = 0; i < skin.JointsCount; i++)
            {
                var jointNode = skin.GetJoint(i).Joint;
                jointmap[jointNode] = i;
            }

            var skeletonRoot = new Skeleton.SkeletonNode(null, skin.Skeleton, jointmap, nodemap);
            model.skeleton = new Skeleton(skeletonRoot);

            foreach (var anim in modelRoot.LogicalAnimations)
            {
                var dstAnimation = new SkeletonAnimation(anim.Name, anim.Duration);

                foreach (var channel in anim.Channels)
                {
                    if (nodemap.TryGetValue(channel.TargetNode, out var targetNode))
                    {
                        var translation = channel.GetTranslationSampler();
                        var rotation = channel.GetRotationSampler();
                        var scale = channel.GetScaleSampler();

                        dstAnimation.AnimationChannels[targetNode] = new SkeletonAnimation.SkeletonAnimationChannel()
                        {
                            translationCurve = Convert(anim.Duration, translation),
                            rotationCurve = Convert(anim.Duration, rotation),
                            scaleCurve = Convert(anim.Duration, scale),
                        };
                    }
                }

                model.animations.Add(dstAnimation);
            }
        }

        Dictionary<GltfTexture, Texture2D> texmap = new Dictionary<GltfTexture, Texture2D>();

        // convert GLTF materials & load any textures as necessary
        Dictionary<GltfMaterial, RuntimeResource<Material>> matmap = new Dictionary<GltfMaterial, RuntimeResource<Material>>();
        foreach (var mat in modelRoot.LogicalMaterials)
        {
            // if the model data has an override, load that
            // otherwise, construct a default material
            if (modelData.Materials!.ContainsKey(mat.Name))
            {
                matmap.Add(mat, game.Resources.Load<Material>(modelData.Materials[mat.Name]));
            }
            else
            {
                Material defaultMat = new Material(defaultShader);
                defaultMat.SetParameter("AlphaCutoff", mat.AlphaCutoff);

                if (mat.Alpha == AlphaMode.BLEND)
                {
                    defaultMat.type = MaterialType.Transparent;
                    defaultMat.blendState = BlendState.NonPremultiplied;
                    defaultMat.dsState = DepthStencilState.DepthRead;
                }

                if (mat.FindChannel("BaseColor") is MaterialChannel baseColor)
                {
                    defaultMat.SetParameter("DiffuseColor", new Vector4(baseColor.Color.X, baseColor.Color.Y, baseColor.Color.Z, baseColor.Color.W));
                    if (baseColor.Texture != null)
                    {
                        Texture2D tex;

                        if (!texmap.ContainsKey(baseColor.Texture))
                        {
                            // load texture
                            using var texStream = baseColor.Texture.PrimaryImage.Content.Open();
                            tex = Texture2D.FromStream(game.GraphicsDevice, texStream);
                            texmap.Add(baseColor.Texture, tex);
                        }
                        else
                        {
                            tex = texmap[baseColor.Texture];
                        }

                        defaultMat.SetParameter("DiffuseTexture", tex);
                    }
                    else
                    {
                        defaultMat.SetParameter("DiffuseTexture", game.DummyWhite!);
                    }
                }
                else
                {
                    defaultMat.SetParameter("DiffuseColor", Vector4.One);
                    defaultMat.SetParameter("DiffuseTexture", game.DummyWhite!);
                }

                if (mat.Alpha == AlphaMode.MASK)
                {
                    defaultMat.technique = "Default_Mask";
                    defaultMat.techniqueSkinned = "Skinned_Mask";
                }
                else
                {
                    defaultMat.technique = "Default";
                    defaultMat.techniqueSkinned = "Skinned";
                }

                matmap.Add(mat, defaultMat);
            }
        }

        // convert into flat mesh parts array
        foreach (var node in modelRoot.LogicalNodes)
        {
            if (node.Mesh != null)
            {
                var primList = meshMap[node.Mesh];

                foreach (var prim in primList)
                {
                    var mat = matmap[prim.material];

                    model.parts.Add(new ModelPart(prim.mesh, mat)
                    {
                        localTransform = ToFNA(node.WorldMatrix)
                    });
                }
            }
        }

        model.RecalcBounds();

        return model;
    }

    private static Matrix ToFNA(System.Numerics.Matrix4x4 m)
    {
        return Unsafe.As<System.Numerics.Matrix4x4, Matrix>(ref m);
    }

    private static void LoadGltfMesh(PraxisGame game, GltfMesh mesh, List<Primitive> outMeshList)
    {
        // convert vertices
        var primitives = mesh.Primitives;

        foreach (var prim in primitives)
        {
            var vpos = prim.GetVertexAccessor("POSITION")?.AsVector3Array()!;
            var vnorm = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
            var vtan = prim.GetVertexAccessor("TANGENT")?.AsVector4Array();
            var vcolor0 = prim.GetVertexAccessor("COLOR_0")?.AsVector4Array();
            var vcolor1 = prim.GetVertexAccessor("COLOR_1")?.AsVector4Array();
            var vtex0 = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
            var vtex1 = prim.GetVertexAccessor("TEXCOORD_1")?.AsVector2Array();
            var vjoints = prim.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
            var vweights = prim.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();
            var tris = prim.GetTriangleIndices().ToArray();

            // since we only use 16-bit indices, ensure that model won't overflow max index
            Debug.Assert(vpos.Count <= 65536);

            VertexBuffer vb = new VertexBuffer(game.GraphicsDevice, MeshVertDeclaration, vpos.Count, BufferUsage.WriteOnly);
            IndexBuffer ib = new IndexBuffer(game.GraphicsDevice, IndexElementSize.SixteenBits, tris.Length * 3, BufferUsage.WriteOnly);

            MeshVert[] vertices = new MeshVert[vpos.Count];
            ushort[] indices = new ushort[tris.Length * 3];

            float radius = 0f;

            for (int i = 0; i < vpos.Count; i++)
            {
                MeshVert v = new MeshVert
                {
                    pos = new Vector4(vpos[i].X, vpos[i].Y, vpos[i].Z, 1f),
                    color0 = Color.White
                };

                radius = MathF.Max(radius, v.pos.Length());

                if (vnorm != null) v.normal = new Vector4(vnorm[i].X, vnorm[i].Y, vnorm[i].Z, 0f);
                if (vtan != null) v.tangent = new Vector4(vtan[i].X, vtan[i].Y, vtan[i].Z, vtan[i].W);
                if (vcolor0 != null) v.color0 = new Color(vcolor0[i].X, vcolor0[i].Y, vcolor0[i].Z, vcolor0[i].W);
                if (vcolor1 != null) v.color1 = new Color(vcolor1[i].X, vcolor1[i].Y, vcolor1[i].Z, vcolor1[i].W);
                if (vtex0 != null) v.uv0 = new Vector2(vtex0[i].X, vtex0[i].Y);
                if (vtex1 != null) v.uv1 = new Vector2(vtex1[i].X, vtex1[i].Y);
                if (vjoints != null) v.boneJoints = new Color((byte)vjoints[i].X, (byte)vjoints[i].Y, (byte)vjoints[i].Z, (byte)vjoints[i].W);
                if (vweights != null) v.boneWeights = new Color(vweights[i].X, vweights[i].Y, vweights[i].Z, vweights[i].W);

                vertices[i] = v;
            }

            for (int i = 0; i < tris.Length; i++)
            {
                indices[i * 3] = (ushort)tris[i].A;
                indices[(i * 3) + 1] = (ushort)tris[i].B;
                indices[(i * 3) + 2] = (ushort)tris[i].C;

                if (vnorm == null)
                {
                    // oh... model doesn't have normals. I guess just generate something.
                    var a = vpos[tris[i].A];
                    var b = vpos[tris[i].B];
                    var c = vpos[tris[i].C];

                    var n = System.Numerics.Vector3.Cross(b - a, c - b);
                    Vector4 normal = new Vector4(n.X, n.Y, n.Z, 0f);

                    vertices[tris[i].A].normal = normal;
                    vertices[tris[i].B].normal = normal;
                    vertices[tris[i].C].normal = normal;
                }
            }

            vb.SetData(vertices);
            ib.SetData(indices);
            
            outMeshList.Add(new Primitive
            {
                mesh = new Mesh(vb, ib, PrimitiveType.TriangleList, tris.Length)
                {
                    bounds = new BoundingSphere(Vector3.Zero, radius)
                },
                material = prim.Material
            });
        }
    }

    private static Vector3AnimationCurve? Convert(float duration, IAnimationSampler<System.Numerics.Vector3>? v)
    {
        if (v == null)
        {
            return null;
        }

        if (v.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE)
        {
            var keys = v.GetCubicKeys().ToArray();

            Vector3AnimationCurve.CurvePoint[] points = new Vector3AnimationCurve.CurvePoint[keys.Length];

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new AnimationCurve<Vector3>.CurvePoint
                {
                    time = keys[i].Key,
                    tangentIn = ToFNA(keys[i].Value.TangentIn),
                    value = ToFNA(keys[i].Value.Value),
                    tangentOut = ToFNA(keys[i].Value.TangentOut),
                };
            }

            return new Vector3AnimationCurve(CurveInterpolationMode.Cubic, points);
        }
        else
        {
            var keys = v.GetLinearKeys().ToArray();

            Vector3AnimationCurve.CurvePoint[] points = new Vector3AnimationCurve.CurvePoint[keys.Length];

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector3AnimationCurve.CurvePoint
                {
                    time = keys[i].Key,
                    value = ToFNA(keys[i].Value),
                };
            }

            if (v.InterpolationMode == AnimationInterpolationMode.STEP)
            {
                return new Vector3AnimationCurve(CurveInterpolationMode.Step, points);
            }

            return new Vector3AnimationCurve(CurveInterpolationMode.Linear, points);
        }
    }

    private static QuaternionAnimationCurve? Convert(float duration, IAnimationSampler<System.Numerics.Quaternion>? v)
    {
        if (v == null)
        {
            return null;
        }

        if (v.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE)
        {
            var keys = v.GetCubicKeys().ToArray();

            QuaternionAnimationCurve.CurvePoint[] points = new QuaternionAnimationCurve.CurvePoint[keys.Length];

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new AnimationCurve<Quaternion>.CurvePoint
                {
                    time = keys[i].Key,
                    tangentIn = ToFNA(keys[i].Value.TangentIn),
                    value = ToFNA(keys[i].Value.Value),
                    tangentOut = ToFNA(keys[i].Value.TangentOut),
                };
            }

            return new QuaternionAnimationCurve(CurveInterpolationMode.Cubic, points);
        }
        else
        {
            var keys = v.GetLinearKeys().ToArray();

            QuaternionAnimationCurve.CurvePoint[] points = new QuaternionAnimationCurve.CurvePoint[keys.Length];

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new QuaternionAnimationCurve.CurvePoint
                {
                    time = keys[i].Key,
                    value = ToFNA(keys[i].Value),
                };
            }

            if (v.InterpolationMode == AnimationInterpolationMode.STEP)
            {
                return new QuaternionAnimationCurve(CurveInterpolationMode.Step, points);
            }

            return new QuaternionAnimationCurve(CurveInterpolationMode.Linear, points);
        }
    }

    private static Vector3 ToFNA(System.Numerics.Vector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    private static Quaternion ToFNA(System.Numerics.Quaternion v)
    {
        return new Quaternion(v.X, v.Y, v.Z, v.W);
    }
}
