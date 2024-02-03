namespace Praxis.Core;

using Praxis.Core.ECS;

using Microsoft.Xna.Framework;
using OWB;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Interface for a handler that can unpack GenericEntityNode data into its target Entity
/// </summary>
public interface IGenericEntityHandler
{
    public void Unpack(GenericEntityNode node, World world, Entity target);
}

/// <summary>
/// Represents a scene which can be unpacked into a World
/// </summary>
public class Scene : IDisposable
{
    // 44 bytes total
    private struct BrushVert
    {
        public Vector4 pos;         // 0
        public Vector4 normal;      // 16
        public Vector2 uv0;         // 32
        public Color color0;        // 40
    }

    private static VertexDeclaration BrushVertDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
        new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.Normal, 0),
        new VertexElement(32, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(40, VertexElementFormat.Color, VertexElementUsage.Color, 0)
    );

    public readonly PraxisGame Game;
    public readonly World World;

    private string _projectPath;
    private Entity _root;

    private List<IDisposable> _resources = new List<IDisposable>();

    private IGenericEntityHandler? _genericEntityHandler = null;

    private List<Vector3> _tmpPolyA = new List<Vector3>();
    private List<Vector3> _tmpPolyB = new List<Vector3>();

    private Dictionary<string, Material> _brushMaterials = new Dictionary<string, Material>();

    /// <summary>
    /// Create a scene from a parsed OWB level and unpack it into the target World
    /// </summary>
    public Scene(PraxisGame game, World world, string projectPath, Level level, IGenericEntityHandler? genericEntityHandler = null)
    {
        Game = game;
        World = world;
        _projectPath = projectPath;
        _genericEntityHandler = genericEntityHandler;
        _root = UnpackBase(level.Root!, null);

        GC.Collect();
    }

    /// <summary>
    /// Clean up all entities & resources created by this scene
    /// </summary>
    public void Dispose()
    {
        World.Send(new DestroyEntity(_root));

        foreach (var handle in _resources)
        {
            handle.Dispose();
        }
        _resources.Clear();

        GC.Collect();
    }

    private Entity UnpackBase(Node node, Entity? parent)
    {
        CalcWorldRecursive(node, Matrix.Identity);

        Entity entity = World.CreateEntity(node.Name!);
        World.Set(entity, new TransformComponent(node.Position, node.Rotation, node.Scale));

        if (parent != null)
        {
            World.Relate(entity, parent.Value, new ChildOf());
        }

        if (node is SceneRootNode)
        {
            Unpack((SceneRootNode)node);
        }
        else if (node is GenericEntityNode)
        {
            Unpack((GenericEntityNode)node, entity);
        }
        else if (node is LightNode)
        {
            Unpack((LightNode)node, entity);
        }
        else if (node is SplineNode)
        {
            Unpack((SplineNode)node, entity);
        }
        else if (node is StaticMeshNode)
        {
            Unpack((StaticMeshNode)node, entity);
        }
        else if (node is BrushNode)
        {
            Unpack((BrushNode)node, entity);
        }
        else if (node is TerrainNode)
        {
            Unpack((TerrainNode)node, entity);
        }
        else
        {
            throw new NotImplementedException();
        }

        if (node.Children != null)
        {
            foreach (var children in node.Children)
            {
                UnpackBase(children, entity);
            }
        }

        return entity;
    }

    private void Unpack(SceneRootNode node)
    {
        Vector3 ambientColor = node.AmbientColor.ToVector3() * (float)node.AmbientIntensity;

        World.SetSingleton(new AmbientLightSingleton
        {
            color = ambientColor
        });
    }

    private void Unpack(GenericEntityNode node, Entity target)
    {
        _genericEntityHandler?.Unpack(node, World, target);
    }

    private void Unpack(LightNode node, Entity target)
    {
        switch(node.LightType)
        {
            case 0:
                World.Set(target, new DirectionalLightComponent
                {
                    color = node.Color.ToVector3() * (float)node.Intensity
                });
                break;
            case 1:
                World.Set(target, new PointLightComponent
                {
                    color = node.Color.ToVector3() * (float)node.Intensity,
                    radius = (float)node.Radius
                });
                break;
            case 2:
                World.Set(target, new SpotLightComponent
                {
                    color = node.Color.ToVector3() * (float)node.Intensity,
                    radius = (float)node.Radius,
                    innerConeAngle = (float)node.InnerConeAngle,
                    outerConeAngle = (float)node.OuterConeAngle
                });
                break;
        }
    }

    private void Unpack(SplineNode node, Entity target)
    {
    }

    private void Unpack(StaticMeshNode node, Entity target)
    {
        // OWB indicates GLTF or GLB model paths, but content pipeline converts these into PMDL
        string meshPath = Path.Combine(_projectPath, Path.ChangeExtension(node.MeshPath!, "pmdl"));

        var model = Game.Resources.Load<Model>(meshPath);

        if (node.Visible)
        {
            World.Set(target, new ModelComponent
            {
                model = model
            });
        }

        if (node.Collision == 1)
        {
            // we can't use ChildOf for colliders, so we construct a brand-new entity to contain this entity's collision
            if (!node.worldTransform.Decompose(out var scale, out var rotation, out var translation))
            {
                throw new Exception("Failed to decompose transform for collision entity");
            }

            var collisionEntity = World.CreateEntity("collision");
            World.Set(collisionEntity, new TransformComponent
            {
                position = translation,
                rotation = rotation
            });
            World.Set(collisionEntity, new ColliderComponent
            {
                collider = new MeshColliderDefinition
                {
                    Mesh = model,
                    Scale = scale
                }
            });
            World.Relate(collisionEntity, target, new BelongsTo());
        }
    }

    private void Unpack(BrushNode node, Entity target)
    {
        Model brushModel = CreateBrush(node);

        World.Set(target, new ModelComponent
        {
            model = brushModel
        });

        if (node.Collision == 1)
        {
            // we can't use ChildOf for colliders, so we construct a brand-new entity to contain this entity's collision
            if (!node.worldTransform.Decompose(out var scale, out var rotation, out var translation))
            {
                throw new Exception("Failed to decompose transform for collision entity");
            }

            var collisionEntity = World.CreateEntity("collision");
            World.Set(collisionEntity, new TransformComponent
            {
                position = translation,
                rotation = rotation
            });
            World.Set(collisionEntity, new ColliderComponent
            {
                collider = new MeshColliderDefinition
                {
                    Mesh = brushModel,
                    Scale = scale
                }
            });
            World.Relate(collisionEntity, target, new BelongsTo());
        }
    }

    private void Unpack(TerrainNode node, Entity target)
    {
    }

    private Model CreateBrush(BrushNode node)
    {
        Model m = new Model();

        float radiusSqr = 0f;

        List<CollisionMesh.Triangle> collisionTriangles = new List<CollisionMesh.Triangle>();

        for (int i = 0; i < node.Planes!.Length; i++)
        {
            var face = node.Planes[i];
            if (!face.Visible) continue;

            List<BrushVert> vertices = new List<BrushVert>();
            List<ushort> indices = new List<ushort>();

            Matrix rot = Matrix.CreateFromQuaternion(face.Rotation);
            Vector3 bx = Vector3.TransformNormal(Vector3.UnitX, rot);
            Vector3 by = Vector3.TransformNormal(Vector3.UnitY, rot);
            Vector3 n = Vector3.TransformNormal(Vector3.UnitZ, rot);

            _tmpPolyA.Clear();
            _tmpPolyB.Clear();
            _tmpPolyA.Add(face.Position + (by * 1000.0f));
            _tmpPolyA.Add(face.Position - (by * 1000.0f) - (bx * 1000.0f));
            _tmpPolyA.Add(face.Position - (by * 1000.0f) + (bx * 1000.0f));

            // clip triangle to each plane
            for (int j = 0; j < node.Planes.Length; j++)
            {
                if (j == i) continue;
                ClipPolygon(_tmpPolyA, _tmpPolyB, node.Planes[j]);
                _tmpPolyA.Clear();
                _tmpPolyA.AddRange(_tmpPolyB);
                _tmpPolyB.Clear();
            }

            // face clipped away, skip
            if (_tmpPolyA.Count < 2) continue;

            // convert polygon into triangle fan
            foreach (var v in _tmpPolyA)
            {
                Vector3 relV = v - face.Position;
                float tex_u = (Vector3.Dot(relV, bx) * face.TextureScale.X) + face.TextureOffset.X;
                float tex_v = (Vector3.Dot(relV, by) * face.TextureScale.Y) + face.TextureOffset.X;

                radiusSqr = MathF.Max(radiusSqr, v.LengthSquared());
                
                vertices.Add(new BrushVert
                {
                    pos = new Vector4(v, 1f),
                    normal = new Vector4(n, 0f),
                    color0 = Color.White,
                    uv0 = new Vector2(tex_u, tex_v),
                });
            }

            for (int j = 1; j < _tmpPolyA.Count - 1; j++)
            {
                indices.Add(0);
                indices.Add((ushort)j);
                indices.Add((ushort)(j + 1));

                collisionTriangles.Add(new CollisionMesh.Triangle
                {
                    a = _tmpPolyA[0],
                    b = _tmpPolyA[j],
                    c = _tmpPolyA[j + 1]
                });
            }

            // construct vertex+index buffers
            VertexBuffer vb = new VertexBuffer(Game.GraphicsDevice, BrushVertDeclaration, vertices.Count, BufferUsage.WriteOnly);
            vb.SetData(vertices.ToArray());

            IndexBuffer ib = new IndexBuffer(Game.GraphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
            ib.SetData(indices.ToArray());

            var faceMesh = new Mesh(vb, ib, PrimitiveType.TriangleList, indices.Count / 3);
            _resources.Add(faceMesh);

            var facePart = new ModelPart(faceMesh, GetBrushMaterial(face.TexturePath!));
            m.parts.Add(facePart);
        }

        m.bounds = new BoundingSphere(Vector3.Zero, MathF.Sqrt(radiusSqr));
        m.collision = new CollisionMesh(collisionTriangles.ToArray());

        return m;
    }

    private Material GetBrushMaterial(string texturePath)
    {
        if (_brushMaterials.ContainsKey(texturePath))
        {
            return _brushMaterials[texturePath];
        }

        var mat = new Material(Game, Game.Resources.Load<Effect>("content/shaders/BasicLit.fxo"))
        {
            technique = "Default"
        };

        if (!string.IsNullOrEmpty(texturePath))
        {
            mat.SetParameter("DiffuseTexture", Game.Resources.Load<Texture2D>(Path.Combine(_projectPath, Path.ChangeExtension(texturePath, "dds"))));
        }
        mat.SetParameter("DiffuseColor", Vector4.One);

        _brushMaterials[texturePath] = mat;

        return mat;
    }

    private void ClipPolygon(List<Vector3> inPolygon, List<Vector3> outPolygon, BrushPlane plane)
    {
        Matrix rot = Matrix.CreateFromQuaternion(plane.Rotation);
        Vector3 planeN = Vector3.TransformNormal(-Vector3.UnitZ, rot);

        // clip each edge against the polygon
        for (int i = 0; i < inPolygon.Count; i++)
        {
            int cur = i;
            int next = (i + 1) % inPolygon.Count;

            Vector3 a = inPolygon[cur];
            Vector3 b = inPolygon[next];

            float ad = Vector3.Dot(a - plane.Position, planeN);
            float bd = Vector3.Dot(b - plane.Position, planeN);

            // both edges on outside of plane, skip
            if (ad < 0f && bd < 0f)
            {
                continue;
            }

            // one vertex on outside, one vertex on inside
            if (ad < 0f || bd < 0f)
            {
                // construct ray pointing from a -> b
                Vector3 rayN = b - a;
                rayN.Normalize();

                float d = Vector3.Dot(rayN, planeN);
                if (MathF.Abs(d) > float.Epsilon)
                {
                    float t = Vector3.Dot(plane.Position - a, planeN) / d;
                    Vector3 intersect = a + (rayN * t);
                    if (ad < 0f)
                    {
                        outPolygon.Add(intersect);
                    }
                    else if (bd < 0f)
                    {
                        outPolygon.Add(a);
                        outPolygon.Add(intersect);
                    }
                }
            }
            // both vertices on inside
            else
            {
                outPolygon.Add(a);
            }
        }
    }

    private void CalcWorldRecursive(Node node, Matrix parent)
    {
        Matrix trs = Matrix.CreateScale(node.Scale)
            * Matrix.CreateFromQuaternion(node.Rotation)
            * Matrix.CreateTranslation(node.Position);

        node.worldTransform = trs * parent;

        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                CalcWorldRecursive(child, trs);
            }
        }
    }
}
