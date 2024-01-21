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

    Matrix _cachedView;
    BoundingFrustum _cachedFrustum = new BoundingFrustum(Matrix.Identity);
    List<RenderMesh> _cachedOpaqueMeshes = new List<RenderMesh>();
    List<RenderMesh> _cachedTransparentMeshes = new List<RenderMesh>();

    Vector3[] _directionalLightFwd = new Vector3[4];
    Vector3[] _directionalLightCol = new Vector3[4];

    Filter _cameraFilter;
    Filter _modelFilter;

    Comparison<RenderMesh> _frontToBack;
    Comparison<RenderMesh> _backToFront;

    public BasicForwardRenderer(WorldContext context) : base(context)
    {
        _cameraFilter = World.FilterBuilder
            .Include<TransformComponent>()
            .Include<CachedMatrixComponent>()
            .Include<CameraComponent>()
            .Build();

        _modelFilter = World.FilterBuilder
            .Include<TransformComponent>()
            .Include<CachedMatrixComponent>()
            .Include<ModelComponent>()
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
    }

    public override void Draw()
    {
        base.Draw();

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

            _directionalLightFwd[0] = new Vector3(0f, -1f, 0f);
            _directionalLightCol[0] = new Vector3(1f, 1f, 1f);

            fx.Parameters["ViewProjection"].SetValue(vp);
            fx.Parameters["World"].SetValue(queue[i].transform);

            fx.Parameters["DirectionalLightCount"].SetValue(1);
            fx.Parameters["DirectionalLightFwd"].SetValue(_directionalLightFwd);
            fx.Parameters["DirectionalLightCol"].SetValue(_directionalLightCol);

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
