namespace Praxis.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GltfModelRoot = SharpGLTF.Schema2.ModelRoot;
using GltfMaterial = SharpGLTF.Schema2.Material;
using GltfMesh = SharpGLTF.Schema2.Mesh;
using GltfTexture = SharpGLTF.Schema2.Texture;
using System.Diagnostics;
using System.Runtime.CompilerServices;

/// <summary>
/// Helper class for loading GLB models
/// </summary>
internal class GLBLoader
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

    private struct Primitive
    {
        public Mesh mesh;
        public BoundingSphere bounds;
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

    public static Model ConvertGlb(PraxisGame game, Stream stream)
    {
        var modelRoot = GltfModelRoot.ReadGLB(stream);
        var model = new Model();

        var fx = new BasicEffect(game.GraphicsDevice);
        fx.LightingEnabled = true;
        fx.DirectionalLight0.Enabled = true;
        fx.DirectionalLight0.Direction = new Vector3(0f, -1f, 0f);
        fx.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 1f);

        var mat = new Material(fx);

        // convert logical meshes into Praxis runtime meshes
        Dictionary<GltfMesh, List<Primitive>> meshMap = new Dictionary<GltfMesh, List<Primitive>>();
        foreach (var m in modelRoot.LogicalMeshes)
        {
            var list = new List<Primitive>();
            LoadGltfMesh(game, m, list);
            meshMap[m] = list;
        }

        // convert GLTF images into textures
        Dictionary<GltfTexture, Texture2D> texmap = new Dictionary<GltfTexture, Texture2D>();
        foreach (var tex in modelRoot.LogicalTextures)
        {
            using var texStream = tex.PrimaryImage.Content.Open();
            texmap.Add(tex, Texture2D.FromStream(game.GraphicsDevice, texStream));
        }

        // convert into flat mesh parts array
        // TODO: convert materials
        foreach (var node in modelRoot.LogicalNodes)
        {
            if (node.Mesh != null)
            {
                var primList = meshMap[node.Mesh];

                foreach (var prim in primList)
                {
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
            }

            vb.SetData(vertices);
            ib.SetData(indices);
            
            outMeshList.Add(new Primitive
            {
                mesh = new Mesh(vb, ib, PrimitiveType.TriangleList, tris.Length),
                bounds = new BoundingSphere(Vector3.Zero, radius),
                material = prim.Material
            });
        }
    }
}
