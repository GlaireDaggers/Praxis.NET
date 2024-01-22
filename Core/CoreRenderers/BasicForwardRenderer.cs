namespace Praxis.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonTools.ECS;

[ExecuteAfter(typeof(CalculateTransformSystem))]
public class BasicForwardRenderer : PraxisSystem
{
    private struct RenderMesh
    {
        public Matrix transform;
        public Mesh mesh;
        public Material material;
    }

    private struct RenderPointLight
    {
        public float radius;
        public Vector3 pos;
        public Vector3 color;
    }

    Matrix _cachedView;
    BoundingFrustum _cachedFrustum = new BoundingFrustum(Matrix.Identity);
    List<RenderMesh> _cachedOpaqueMeshes = new List<RenderMesh>();
    List<RenderMesh> _cachedTransparentMeshes = new List<RenderMesh>();
    List<RenderPointLight> _cachedPointLights = new List<RenderPointLight>();

    int _directionalLightCount = 0;
    Vector3[] _directionalLightFwd = new Vector3[4];
    Vector3[] _directionalLightCol = new Vector3[4];

    Filter _cameraFilter;
    Filter _modelFilter;
    Filter _ambientLightFilter;
    Filter _directionalLightFilter;
    Filter _pointLightFilter;

    Comparison<RenderMesh> _frontToBack;
    Comparison<RenderMesh> _backToFront;
    Comparison<RenderPointLight> _sortPointLight;

    Vector3 _ambientLightColor = Vector3.Zero;
    Vector3 _cachedModelPos = Vector3.Zero;

    public BasicForwardRenderer(WorldContext context) : base(context)
    {
        _cameraFilter = World.FilterBuilder
            .Include<TransformComponent>()
            .Include<CachedMatrixComponent>()
            .Include<CameraComponent>()
            .Build();

        _modelFilter = World.FilterBuilder
            .Include<CachedMatrixComponent>()
            .Include<ModelComponent>()
            .Build();

        _ambientLightFilter = World.FilterBuilder
            .Include<AmbientLightComponent>()
            .Build();

        _directionalLightFilter = World.FilterBuilder
            .Include<CachedMatrixComponent>()
            .Include<DirectionalLightComponent>()
            .Build();

        _pointLightFilter = World.FilterBuilder
            .Include<TransformComponent>()
            .Include<PointLightComponent>()
            .Build();

        _frontToBack = (a, b) => {
            Vector3 posA = a.transform.Translation;
            Vector3 posB = b.transform.Translation;
            posA = Vector3.Transform(posA, _cachedView);
            posB = Vector3.Transform(posA, _cachedView);
            return posB.Z.CompareTo(posA.Z);
        };

        _backToFront = (a, b) => {
            Vector3 posA = a.transform.Translation;
            Vector3 posB = b.transform.Translation;
            posA = Vector3.Transform(posA, _cachedView);
            posB = Vector3.Transform(posA, _cachedView);
            return posA.Z.CompareTo(posB.Z);
        };

        _sortPointLight = (a, b) => {
            float distA = Vector3.DistanceSquared(a.pos, _cachedModelPos);
            float distB = Vector3.DistanceSquared(b.pos, _cachedModelPos);
            return distA.CompareTo(distB);
        };
    }

    public override void Draw()
    {
        base.Draw();

        if (_ambientLightFilter.Count > 0)
        {
            _ambientLightColor = World.GetSingleton<AmbientLightComponent>().color;
        }
        else
        {
            _ambientLightColor = Vector3.Zero;
        }

        _directionalLightCount = 0;
        foreach (var lightEntity in _directionalLightFilter.Entities)
        {
            var cachedMatrix = World.Get<CachedMatrixComponent>(lightEntity);
            var lightComp = World.Get<DirectionalLightComponent>(lightEntity);

            _directionalLightCol[_directionalLightCount] = lightComp.color;
            _directionalLightFwd[_directionalLightCount] = Vector3.TransformNormal(-Vector3.UnitZ, cachedMatrix.transform);

            _directionalLightCount++;

            if (_directionalLightCount == 4) break;
        }

        _cachedPointLights.Clear();
        foreach (var lightEntity in _pointLightFilter.Entities)
        {
            var transform = World.Get<TransformComponent>(lightEntity);
            var lightComp = World.Get<PointLightComponent>(lightEntity);

            _cachedPointLights.Add(new RenderPointLight
            {
                pos = transform.position,
                radius = lightComp.radius,
                color = lightComp.color
            });
        }

        foreach (var cameraEntity in _cameraFilter.Entities)
        {
            var transform = World.Get<TransformComponent>(cameraEntity);
            var cachedMatrix = World.Get<CachedMatrixComponent>(cameraEntity);
            var camera = World.Get<CameraComponent>(cameraEntity);

            RenderTarget2D? renderTarget = camera.renderTarget.Resolve();

            int targetWidth = renderTarget?.Width ?? Game.GraphicsDevice.Viewport.Width;
            int targetHeight = renderTarget?.Height ?? Game.GraphicsDevice.Viewport.Height;

            float aspect = (float)targetWidth / targetHeight;

            Matrix projection;

            if (camera.isOrthographic)
            {
                projection = Matrix.CreateOrthographic(aspect * camera.fieldOfView, camera.fieldOfView, camera.near, camera.far);
            }
            else
            {
                projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(camera.fieldOfView), aspect, camera.near, camera.far);
            }

            _cachedView = Matrix.Invert(cachedMatrix.transform);

            Matrix vp = _cachedView * projection;

            _cachedFrustum.Matrix = vp;

            _cachedOpaqueMeshes.Clear();
            _cachedTransparentMeshes.Clear();

            // gather an array of visible meshes, culled against the frustum
            foreach (var modelEntity in _modelFilter.Entities)
            {
                var modelComponent = World.Get<ModelComponent>(modelEntity);
                var modelHandle = modelComponent.modelHandle.Resolve();
 
                if (modelHandle.State != ResourceCache.Core.ResourceLoadState.Loaded) continue;

                var model = modelHandle.Value;

                var meshCachedMatrix = World.Get<CachedMatrixComponent>(modelEntity).transform;

                BoundingSphere bounds = model.bounds;
                bounds.Center = Vector3.Transform(bounds.Center, meshCachedMatrix);

                if (_cachedFrustum.Intersects(bounds))
                {
                    for (int i = 0; i < model.parts.Count; i++)
                    {
                        var part = model.parts[i];
                        if (part.material.State != ResourceCache.Core.ResourceLoadState.Loaded) continue;

                        var mat = part.material.Value;

                        var renderMesh = new RenderMesh
                        {
                            transform = part.localTransform * meshCachedMatrix,
                            mesh = part.mesh,
                            material = mat
                        };

                        if (mat.type == MaterialType.Opaque)
                        {
                            _cachedOpaqueMeshes.Add(renderMesh);
                        }
                        else
                        {
                            _cachedTransparentMeshes.Add(renderMesh);
                        }
                    }
                }
            }

            _cachedOpaqueMeshes.Sort(_frontToBack);
            _cachedTransparentMeshes.Sort(_backToFront);

            Game.GraphicsDevice.SetRenderTarget(renderTarget);
            Game.GraphicsDevice.Clear(camera.clearColor);

            DrawQueue(vp, _cachedOpaqueMeshes);
            DrawQueue(vp, _cachedTransparentMeshes);
        }

        Game.GraphicsDevice.SetRenderTarget(null);
    }

    private void DrawQueue(Matrix vp, List<RenderMesh> queue)
    {
        for (int i = 0; i < queue.Count; i++)
        {
            var material = queue[i].material;
            var fx = material.effect.Value;

            fx.CurrentTechnique = fx.Techniques[material.technique];

            material.ApplyParameters();

            var mesh = queue[i].mesh;

            // sort point lights
            _cachedModelPos = queue[i].transform.Translation;
            _cachedPointLights.Sort(_sortPointLight);

            fx.Parameters["ViewProjection"].SetValue(vp);
            fx.Parameters["World"].SetValue(queue[i].transform);

            fx.Parameters["AmbientLightColor"].SetValue(_ambientLightColor);

            fx.Parameters["DirectionalLightCount"].SetValue(_directionalLightCount);
            fx.Parameters["DirectionalLightFwd"].SetValue(_directionalLightFwd);
            fx.Parameters["DirectionalLightCol"].SetValue(_directionalLightCol);

            // grab up to 16 closest point lights
            int pointLightCount = _cachedPointLights.Count;
            if (pointLightCount > 16) pointLightCount = 16;

            fx.Parameters["PointLightCount"].SetValue(pointLightCount);
            for (int pt = 0; pt < pointLightCount; pt++)
            {
                fx.Parameters["PointLightPosRadius"].SetValue(new Vector4(_cachedPointLights[pt].pos, _cachedPointLights[pt].radius));
                fx.Parameters["PointLightCol"].SetValue(_cachedPointLights[pt].color);
            }
            
            for (int pass = 0; pass < fx.CurrentTechnique.Passes.Count; pass++)
            {
                fx.CurrentTechnique.Passes[pass].Apply();

                Game.GraphicsDevice.BlendState = material.blendState;
                Game.GraphicsDevice.DepthStencilState = material.dsState;
                Game.GraphicsDevice.RasterizerState = material.rasterState;

                Game.GraphicsDevice.SetVertexBuffer(mesh.vertexBuffer);
                Game.GraphicsDevice.Indices = mesh.indexBuffer;

                Game.GraphicsDevice.DrawIndexedPrimitives(mesh.primitiveType, 0, 0, mesh.vertexBuffer.VertexCount,
                    0, mesh.primitiveCount);
            }
        }
    }
}
