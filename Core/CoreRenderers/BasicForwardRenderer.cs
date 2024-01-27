namespace Praxis.Core;

using Praxis.Core.ECS;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

[ExecuteAfter(typeof(CalculateTransformSystem))]
public class BasicForwardRenderer : PraxisSystem
{
    private struct RenderMesh
    {
        public Matrix transform;
        public Mesh mesh;
        public Material material;
        public Matrix[]? pose;
    }

    private struct RenderPointLight
    {
        public float radius;
        public Vector3 pos;
        public Vector3 color;
    }

    private struct RenderSpotLight
    {
        public float radius;
        public float innerConeAngle;
        public float outerConeAngle;
        public Vector3 pos;
        public Vector3 fwd;
        public Vector3 color;
    }

    Matrix _cachedView;
    BoundingFrustum _cachedFrustum = new BoundingFrustum(Matrix.Identity);
    List<RenderMesh> _cachedOpaqueMeshes = new List<RenderMesh>();
    List<RenderMesh> _cachedTransparentMeshes = new List<RenderMesh>();
    List<RenderPointLight> _cachedPointLights = new List<RenderPointLight>();
    List<RenderSpotLight> _cachedSpotLights = new List<RenderSpotLight>();

    int _directionalLightCount = 0;
    Vector3[] _directionalLightFwd = new Vector3[4];
    Vector3[] _directionalLightCol = new Vector3[4];
    
    Vector4[] _pointLightPosRadius = new Vector4[16];
    Vector3[] _pointLightColor = new Vector3[16];

    Vector4[] _spotLightPosRadius = new Vector4[8];
    Vector4[] _spotLightFwdAngle1 = new Vector4[8];
    Vector4[] _spotLightColAngle2 = new Vector4[8];

    Filter _cameraFilter;
    Filter _modelFilter;
    Filter _directionalLightFilter;
    Filter _pointLightFilter;
    Filter _spotLightFilter;

    Comparison<RenderMesh> _frontToBack;
    Comparison<RenderMesh> _backToFront;
    Comparison<RenderPointLight> _sortPointLight;
    Comparison<RenderSpotLight> _sortSpotLight;

    Vector3 _ambientLightColor = Vector3.Zero;
    Vector3 _cachedModelPos = Vector3.Zero;

    public BasicForwardRenderer(WorldContext context) : base(context)
    {
        _cameraFilter = new FilterBuilder(World)
            .Include<CachedMatrixComponent>()
            .Include<CameraComponent>()
            .Build();

        _modelFilter = new FilterBuilder(World)
            .Include<CachedMatrixComponent>()
            .Include<ModelComponent>()
            .Build();

        _directionalLightFilter = new FilterBuilder(World)
            .Include<CachedMatrixComponent>()
            .Include<DirectionalLightComponent>()
            .Build();

        _pointLightFilter = new FilterBuilder(World)
            .Include<CachedMatrixComponent>()
            .Include<PointLightComponent>()
            .Build();

        _spotLightFilter = new FilterBuilder(World)
            .Include<CachedMatrixComponent>()
            .Include<SpotLightComponent>()
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

        _sortSpotLight = (a, b) => {
            float distA = Vector3.DistanceSquared(a.pos, _cachedModelPos);
            float distB = Vector3.DistanceSquared(b.pos, _cachedModelPos);
            return distA.CompareTo(distB);
        };
    }

    public override void Draw()
    {
        base.Draw();

        if (World.HasSingleton<AmbientLightComponent>())
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
            _directionalLightFwd[_directionalLightCount] = Vector3.TransformNormal(Vector3.UnitZ, cachedMatrix.transform);

            _directionalLightCount++;

            if (_directionalLightCount == 4) break;
        }

        _cachedPointLights.Clear();
        foreach (var lightEntity in _pointLightFilter.Entities)
        {
            var cachedMatrix = World.Get<CachedMatrixComponent>(lightEntity);
            var lightComp = World.Get<PointLightComponent>(lightEntity);

            _cachedPointLights.Add(new RenderPointLight
            {
                pos = cachedMatrix.transform.Translation,
                radius = lightComp.radius,
                color = lightComp.color
            });
        }

        _cachedSpotLights.Clear();
        foreach (var lightEntity in _spotLightFilter.Entities)
        {
            var cachedMatrix = World.Get<CachedMatrixComponent>(lightEntity);
            var lightComp = World.Get<SpotLightComponent>(lightEntity);

            _cachedSpotLights.Add(new RenderSpotLight
            {
                pos = cachedMatrix.transform.Translation,
                fwd = Vector3.TransformNormal(Vector3.UnitZ, cachedMatrix.transform),
                innerConeAngle = lightComp.innerConeAngle,
                outerConeAngle = lightComp.outerConeAngle,
                radius = lightComp.radius,
                color = lightComp.color
            });
        }

        foreach (var cameraEntity in _cameraFilter.Entities)
        {
            var cachedMatrix = World.Get<CachedMatrixComponent>(cameraEntity);
            var camera = World.Get<CameraComponent>(cameraEntity);

            RenderTarget2D? renderTarget = camera.renderTarget;
            ScreenFilterStack? screenFilter = camera.filterStack;

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
                var modelHandle = modelComponent.model.Value;
 
                if (modelHandle.State != ResourceCache.Core.ResourceLoadState.Loaded) continue;

                var model = modelHandle.Value;

                var meshCachedMatrix = World.Get<CachedMatrixComponent>(modelEntity).transform;

                BoundingSphere bounds = model.bounds.Transform(meshCachedMatrix);

                Matrix[]? pose = null;

                if (World.Has<CachedPoseComponent>(modelEntity))
                {
                    pose = World.Get<CachedPoseComponent>(modelEntity).Pose;
                }

                if (_cachedFrustum.Intersects(bounds))
                {
                    for (int i = 0; i < model.parts.Count; i++)
                    {
                        var part = model.parts[i];
                        if (part.material.State != ResourceCache.Core.ResourceLoadState.Loaded) continue;

                        var mat = part.material.Value;

                        var renderMesh = new RenderMesh
                        {
                            transform = meshCachedMatrix,
                            mesh = part.mesh,
                            material = mat,
                            pose = pose
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

            RenderTarget2D? tempTarget = screenFilter?.GetTarget(renderTarget) ?? renderTarget;

            Game.GraphicsDevice.SetRenderTarget(tempTarget);
            Game.GraphicsDevice.Clear(camera.clearColor);

            DrawQueue(vp, _cachedOpaqueMeshes);
            DrawQueue(vp, _cachedTransparentMeshes);

            screenFilter?.OnRender(tempTarget!, renderTarget);
        }

        Game.GraphicsDevice.SetRenderTarget(null);
    }

    private void DrawQueue(Matrix vp, List<RenderMesh> queue)
    {
        for (int i = 0; i < queue.Count; i++)
        {
            var material = queue[i].material;
            var fx = material.effect.Value;

            string? technique = material.technique;

            if (queue[i].pose != null)
            {
                technique = material.techniqueSkinned ?? technique;
            }

            if (technique != null)
            {
                fx.CurrentTechnique = fx.Techniques[technique];
            }

            material.ApplyParameters();

            var mesh = queue[i].mesh;

            // sort point & spot lights
            _cachedModelPos = queue[i].transform.Translation;
            _cachedPointLights.Sort(_sortPointLight);
            _cachedSpotLights.Sort(_sortSpotLight);

            fx.Parameters["ViewProjection"]?.SetValue(vp);
            fx.Parameters["World"]?.SetValue(queue[i].transform);

            fx.Parameters["AmbientLightColor"]?.SetValue(_ambientLightColor);

            fx.Parameters["DirectionalLightCount"]?.SetValue(_directionalLightCount);
            fx.Parameters["DirectionalLightFwd"]?.SetValue(_directionalLightFwd);
            fx.Parameters["DirectionalLightCol"]?.SetValue(_directionalLightCol);

            // grab up to 16 closest point lights
            int pointLightCount = _cachedPointLights.Count;
            if (pointLightCount > 16) pointLightCount = 16;

            fx.Parameters["PointLightCount"]?.SetValue(pointLightCount);
            
            for (int pt = 0; pt < pointLightCount; pt++)
            {
                _pointLightPosRadius[pt] = new Vector4(_cachedPointLights[pt].pos, _cachedPointLights[pt].radius);
                _pointLightColor[pt] = _cachedPointLights[pt].color;
            }

            fx.Parameters["PointLightPosRadius"]?.SetValue(_pointLightPosRadius);
            fx.Parameters["PointLightCol"]?.SetValue(_pointLightColor);

            // grab up to 8 closest spot lights
            int spotLightCount = _cachedSpotLights.Count;
            if (spotLightCount > 8) spotLightCount = 8;

            fx.Parameters["SpotLightCount"]?.SetValue(spotLightCount);
            
            for (int pt = 0; pt < spotLightCount; pt++)
            {
                float angle1 = MathF.Cos(MathHelper.ToRadians(_cachedSpotLights[pt].innerConeAngle));
                float angle2 = MathF.Cos(MathHelper.ToRadians(_cachedSpotLights[pt].outerConeAngle));

                _spotLightPosRadius[pt] = new Vector4(_cachedSpotLights[pt].pos, _cachedSpotLights[pt].radius);
                _spotLightFwdAngle1[pt] = new Vector4(_cachedSpotLights[pt].fwd, angle1);
                _spotLightColAngle2[pt] = new Vector4(_cachedSpotLights[pt].color, angle2);
            }

            fx.Parameters["SpotLightPosRadius"]?.SetValue(_spotLightPosRadius);
            fx.Parameters["SpotLightFwdAngle1"]?.SetValue(_spotLightFwdAngle1);
            fx.Parameters["SpotLightColAngle2"]?.SetValue(_spotLightColAngle2);

            // set bone transforms if applicable
            if (queue[i].pose is Matrix[] pose)
            {
                fx.Parameters["BoneTransforms"]?.SetValue(pose);
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
