namespace Praxis.Core;

using Praxis.Core.ECS;

using Microsoft.Xna.Framework;
using OWB;

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
    public readonly PraxisGame Game;
    public readonly World World;

    private string _projectPath;
    private Entity _root;

    private List<IDisposable> _resources = new List<IDisposable>();

    private IGenericEntityHandler? _genericEntityHandler = null;

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

        World.Set(target, new ModelComponent
        {
            model = model
        });
    }

    private void Unpack(BrushNode node, Entity target)
    {
    }

    private void Unpack(TerrainNode node, Entity target)
    {
    }
}
