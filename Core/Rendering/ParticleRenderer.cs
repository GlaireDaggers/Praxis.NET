namespace Praxis.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal class ParticleSpriteRenderer : IDisposable
{
    private struct ParticleVertex
    {
        public Vector4 position;
        public Vector2 texcoord;
        public Color color;
    }

    private static VertexDeclaration ParticleVertexDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
        new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0)  
    );

    public readonly PraxisGame Game;

    private ParticleVertex[] _vertices = new ParticleVertex[ParticleEmitterSystem.MAXPARTICLES * 4];
    private ushort[] _indices = new ushort[ParticleEmitterSystem.MAXPARTICLES * 6];

    private int _vtxCount = 0;
    private int _idxCount = 0;

    private Stack<Mesh> _meshPool = new Stack<Mesh>();
    
    public ParticleSpriteRenderer(PraxisGame game)
    {
        Game = game;
    }


    public void Dispose()
    {
        while (_meshPool.Count > 0)
        {
            _meshPool.Pop().Dispose();
        }
    }

    public void Reset()
    {
        _vtxCount = 0;
        _idxCount = 0;
    }

    public void AppendQuad(in Vector3 up, in Vector3 right, in Vector3 pos, in Color tint)
    {
        int vtxBase = _vtxCount;

        _vertices[_vtxCount++] = new ParticleVertex
        {
            position = new Vector4(pos - right + up, 1f),
            texcoord = new Vector2(0f, 0f),
            color = tint
        };

        _vertices[_vtxCount++] = new ParticleVertex
        {
            position = new Vector4(pos + right + up, 1f),
            texcoord = new Vector2(1f, 0f),
            color = tint
        };

        _vertices[_vtxCount++] = new ParticleVertex
        {
            position = new Vector4(pos - right - up, 1f),
            texcoord = new Vector2(0f, 1f),
            color = tint
        };

        _vertices[_vtxCount++] = new ParticleVertex
        {
            position = new Vector4(pos + right - up, 1f),
            texcoord = new Vector2(1f, 1f),
            color = tint
        };

        _indices[_idxCount++] = (ushort)vtxBase;
        _indices[_idxCount++] = (ushort)(vtxBase + 2);
        _indices[_idxCount++] = (ushort)(vtxBase + 1);

        _indices[_idxCount++] = (ushort)(vtxBase + 1);
        _indices[_idxCount++] = (ushort)(vtxBase + 2);
        _indices[_idxCount++] = (ushort)(vtxBase + 3);
    }

    public Mesh Allocate()
    {
        Mesh bufferMesh;

        if (_meshPool.Count > 0)
        {
            bufferMesh = _meshPool.Pop();
        }
        else
        {
            bufferMesh = new Mesh(
                new VertexBuffer(Game.GraphicsDevice, ParticleVertexDeclaration, _vertices.Length, BufferUsage.WriteOnly),
                new IndexBuffer(Game.GraphicsDevice, IndexElementSize.SixteenBits, _indices.Length, BufferUsage.WriteOnly),
                PrimitiveType.TriangleList,
                0
            );
        }

        bufferMesh.vertexBuffer.SetData(_vertices, 0, _vtxCount);
        bufferMesh.indexBuffer.SetData(_indices, 0, _idxCount);
        bufferMesh.primitiveCount = _idxCount / 3;

        Reset();

        return bufferMesh;
    }

    public void Return(Mesh mesh)
    {
        _meshPool.Push(mesh);
    }
}
