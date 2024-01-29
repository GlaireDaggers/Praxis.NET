namespace Praxis.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class DebugGizmoSystem : PraxisSystem
{
    private const int GIZMO_BUFFER_SIZE = 4096;

    private static VertexDeclaration GridVertexDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
        new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
    );

    private struct GridVert
    {
        public Vector4 pos;
        public Color col;
    }

    public override SystemExecutionStage ExecutionStage => SystemExecutionStage.Draw;

    private bool _isDebug = false;

    private Matrix _cachedView = Matrix.Identity;
    private Matrix _cachedProjection = Matrix.Identity;

    private RasterizerState _gizmoRS;
    private BasicEffect _gizmoEffect;
    private VertexBuffer _gizmoVB;
    private IndexBuffer _gizmoIB;

    private ushort[] _gizmoIndices = new ushort[GIZMO_BUFFER_SIZE];
    private GridVert[] _gizmoVerts = new GridVert[GIZMO_BUFFER_SIZE];
    private int _gizmoVertsCount = 0;
    private int _gizmoIndicesCount = 0;

    public DebugGizmoSystem(WorldContext context) : base(context)
    {
        _gizmoEffect = new BasicEffect(Game.GraphicsDevice)
        {
            LightingEnabled = false,
            TextureEnabled = false,
            VertexColorEnabled = true
        };
        _gizmoIB = new IndexBuffer(Game.GraphicsDevice, IndexElementSize.SixteenBits, GIZMO_BUFFER_SIZE, BufferUsage.WriteOnly);
        _gizmoVB = new VertexBuffer(Game.GraphicsDevice, GridVertexDeclaration, GIZMO_BUFFER_SIZE, BufferUsage.WriteOnly);
        
        _gizmoRS = new RasterizerState
        {
            CullMode = CullMode.None,
            FillMode = FillMode.WireFrame
        };
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        foreach (var msg in World.GetMessages<DebugModeMessage>())
        {
            _isDebug = msg.enableDebug;
        }

        foreach (var msg in World.GetMessages<SetDebugCameraParams>())
        {
            _cachedView = msg.view;
            _cachedProjection = msg.projection;
        }

        if (_isDebug)
        {
            DrawGizmos();
            FlushGizmoBuffer();
        }
    }

    protected virtual void DrawGizmos()
    {
    }

    public void DrawWireframeMeshGizmo(Matrix transform, IndexBuffer ib, VertexBuffer vb, Color color)
    {
        // draw gizmo buffer
        _gizmoEffect.View = _cachedView;
        _gizmoEffect.Projection = _cachedProjection;
        _gizmoEffect.World = transform;
        _gizmoEffect.DiffuseColor = color.ToVector3();

        _gizmoEffect.CurrentTechnique.Passes[0].Apply();
        Game.GraphicsDevice.RasterizerState = _gizmoRS;
        Game.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        Game.GraphicsDevice.SetVertexBuffer(vb);
        Game.GraphicsDevice.Indices = ib;
        Game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vb.VertexCount, 0, ib.IndexCount / 3);
    }

    public void DrawLineGizmo(Vector3 start, Vector3 end, Color color1, Color color2)
    {
        ushort idx = (ushort)_gizmoVertsCount;

        _gizmoIndices[_gizmoIndicesCount++] = idx++;
        _gizmoIndices[_gizmoIndicesCount++] = idx;

        _gizmoVerts[_gizmoVertsCount++] = new GridVert()
        {
            pos = new Vector4(start, 1f),
            col = color1,
        };

        _gizmoVerts[_gizmoVertsCount++] = new GridVert()
        {
            pos = new Vector4(end, 1f),
            col = color2,
        };

        if (_gizmoIndicesCount == _gizmoIB.IndexCount)
        {
            FlushGizmoBuffer();
        }
    }

    public void DrawBoxGizmo(Matrix transform, Color color)
    {
        Vector3 min = -Vector3.One * 0.5f;
        Vector3 max = Vector3.One * 0.5f;

        Vector3 p0 = Vector3.Transform(new Vector3(min.X, min.Y, min.Z), transform);
        Vector3 p1 = Vector3.Transform(new Vector3(max.X, min.Y, min.Z), transform);
        Vector3 p2 = Vector3.Transform(new Vector3(max.X, min.Y, max.Z), transform);
        Vector3 p3 = Vector3.Transform(new Vector3(min.X, min.Y, max.Z), transform);

        Vector3 p4 = Vector3.Transform(new Vector3(min.X, max.Y, min.Z), transform);
        Vector3 p5 = Vector3.Transform(new Vector3(max.X, max.Y, min.Z), transform);
        Vector3 p6 = Vector3.Transform(new Vector3(max.X, max.Y, max.Z), transform);
        Vector3 p7 = Vector3.Transform(new Vector3(min.X, max.Y, max.Z), transform);

        DrawLineGizmo(p0, p1, color, color);
        DrawLineGizmo(p1, p2, color, color);
        DrawLineGizmo(p2, p3, color, color);
        DrawLineGizmo(p3, p0, color, color);

        DrawLineGizmo(p4, p5, color, color);
        DrawLineGizmo(p5, p6, color, color);
        DrawLineGizmo(p6, p7, color, color);
        DrawLineGizmo(p7, p4, color, color);

        DrawLineGizmo(p0, p4, color, color);
        DrawLineGizmo(p1, p5, color, color);
        DrawLineGizmo(p2, p6, color, color);
        DrawLineGizmo(p3, p7, color, color);
    }

    public void DrawCircleGizmo(Vector3 center, float radius, Quaternion rotation, Color color)
    {
        for (int i = 0; i < 64; i++)
        {
            int i2 = (i + 1) % 64;
            float angle1 = (i / 64f) * 360f;
            float angle2 = (i2 / 64f) * 360f;

            Quaternion r1 = rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(angle1));
            Quaternion r2 = rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(angle2));

            Matrix m1 = Matrix.CreateFromQuaternion(r1);
            Matrix m2 = Matrix.CreateFromQuaternion(r2);

            Vector3 v1 = Vector3.TransformNormal(Vector3.UnitX, m1) * radius;
            Vector3 v2 = Vector3.TransformNormal(Vector3.UnitX, m2) * radius;

            DrawLineGizmo(v1 + center, v2 + center, color, color);
        }
    }

    public void DrawSphereGizmo(Vector3 center, float radius, Color color)
    {
        DrawCircleGizmo(center, radius, Quaternion.Identity, color);
        DrawCircleGizmo(center, radius, Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90f)), color);
        DrawCircleGizmo(center, radius, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(90f)), color);
    }

    public void DrawCylinderGizmo(Vector3 center, float height, float radius, Quaternion rotation, Color color)
    {
        Matrix m = Matrix.CreateFromQuaternion(rotation);
        Vector3 right = Vector3.TransformNormal(Vector3.UnitX, m);
        Vector3 up = Vector3.TransformNormal(Vector3.UnitY, m);
        Vector3 fwd = Vector3.TransformNormal(Vector3.UnitZ, m);
        Vector3 top = center + (up * height);
        Vector3 bottom = center - (up * height);

        DrawCircleGizmo(top, radius, rotation, color);
        DrawCircleGizmo(bottom, radius, rotation, color);

        DrawLineGizmo(bottom + (up * radius), top + (up * radius), color, color);
        DrawLineGizmo(bottom + (right * radius), top + (right * radius), color, color);
        DrawLineGizmo(bottom - (up * radius), top - (up * radius), color, color);
        DrawLineGizmo(bottom - (right * radius), top - (right * radius), color, color);
    }

    public void DrawCapsuleGizmo(Vector3 center, float height, float radius, Quaternion rotation, Color color)
    {
        Matrix m = Matrix.CreateFromQuaternion(rotation);
        Vector3 right = Vector3.TransformNormal(Vector3.UnitX, m);
        Vector3 up = Vector3.TransformNormal(Vector3.UnitY, m);
        Vector3 fwd = Vector3.TransformNormal(Vector3.UnitZ, m);
        Vector3 top = center + (up * height);
        Vector3 bottom = center - (up * height);

        DrawSphereGizmo(top, radius, color);
        DrawSphereGizmo(bottom, radius, color);

        DrawCircleGizmo(top, radius, rotation, color);
        DrawCircleGizmo(bottom, radius, rotation, color);

        DrawLineGizmo(bottom + (up * radius), top + (up * radius), color, color);
        DrawLineGizmo(bottom + (right * radius), top + (right * radius), color, color);
        DrawLineGizmo(bottom - (up * radius), top - (up * radius), color, color);
        DrawLineGizmo(bottom - (right * radius), top - (right * radius), color, color);
    }

    private void FlushGizmoBuffer()
    {
        if (_gizmoIndicesCount > 0)
        {
            _gizmoVB.SetData(_gizmoVerts, 0, _gizmoVertsCount);
            _gizmoIB.SetData(_gizmoIndices, 0, _gizmoIndicesCount);

            // draw gizmo buffer
            _gizmoEffect.View = _cachedView;
            _gizmoEffect.Projection = _cachedProjection;
            _gizmoEffect.World = Matrix.Identity;
            _gizmoEffect.DiffuseColor = Vector3.One;

            _gizmoEffect.CurrentTechnique.Passes[0].Apply();
            Game.GraphicsDevice.RasterizerState = _gizmoRS;
            Game.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            Game.GraphicsDevice.SetVertexBuffer(_gizmoVB);
            Game.GraphicsDevice.Indices = _gizmoIB;
            Game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, _gizmoVertsCount, 0, _gizmoIndicesCount / 2);
            
            _gizmoVertsCount = 0;
            _gizmoIndicesCount = 0;
        }
    }
}