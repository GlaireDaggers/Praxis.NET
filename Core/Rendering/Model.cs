namespace Praxis.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// A model in Praxis, acting as a container of ModelParts
/// </summary>
public class Model
{
    public List<ModelPart> parts = new List<ModelPart>();
    public BoundingSphere bounds;

    public void RecalcBounds()
    {
        if (parts.Count > 1)
        {
            bounds = parts[0].Bounds;
            
            for (int i = 1; i < parts.Count; i++)
            {
                BoundingSphere b = parts[i].Bounds;
                BoundingSphere.CreateMerged(ref bounds, ref b, out bounds);
            }
        }
        else
        {
            bounds = new BoundingSphere(Vector3.Zero, 0f);
        }
    }
}

/// <summary>
/// A model part, referencing a mesh, a material, and a local transform relative to its parent Model container
/// </summary>
public class ModelPart
{
    public BoundingSphere Bounds
    {
        get
        {
            var b = mesh.bounds;
            b.Center = Vector3.Transform(b.Center, localTransform);

            return b;
        }
    }

    public Matrix localTransform = Matrix.Identity;
    public Mesh mesh;
    public Material material;

    public ModelPart(Mesh mesh, Material material)
    {
        this.mesh = mesh;
        this.material = material;
    }
}

/// <summary>
/// A mesh, acting as a wrapper around an underlying vertex & index buffer
/// </summary>
public class Mesh : IDisposable
{
    public VertexBuffer vertexBuffer;
    public IndexBuffer indexBuffer;
    public PrimitiveType primitiveType;
    public int startIndex;
    public int primitiveCount;
    public BoundingSphere bounds;

    public Mesh(VertexBuffer vertexBuffer, IndexBuffer indexBuffer, PrimitiveType primitiveType, int primitiveCount)
    {
        this.bounds = new BoundingSphere(Vector3.Zero, 0f);
        this.vertexBuffer = vertexBuffer;
        this.indexBuffer = indexBuffer;
        this.primitiveType = primitiveType;
        this.startIndex = 0;
        this.primitiveCount = primitiveCount;
    }

    public void Dispose()
    {
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
    }
}