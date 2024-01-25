namespace Praxis.Core;

using Microsoft.Xna.Framework;
using MoonTools.ECS;
using OWB;

/// <summary>
/// Represents a scene which can be unpacked into a World
/// </summary>
public class Scene
{
    public readonly PraxisGame Game;
    public readonly World World;

    private string _projectPath;
    private Entity _root;

    private List<IDisposable> _resources = new List<IDisposable>();

    /// <summary>
    /// Create a scene from a parsed OWB level and unpack it into the target World
    /// </summary>
    public Scene(PraxisGame game, World world, string projectPath, Level level)
    {
        Game = game;
        World = world;
        _projectPath = projectPath;
        _root = UnpackBase(level.Root!, null);
    }

    /// <summary>
    /// Clean up all entities created by this scene
    /// </summary>
    public void Unload()
    {
        World.Send(new DestroyEntity(_root));

        foreach (var handle in _resources)
        {
            handle.Dispose();
        }
        _resources.Clear();
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
            Unpack((SceneRootNode)node, entity);
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

        return entity;
    }

    private void Unpack(SceneRootNode node, Entity target)
    {
        Vector3 ambientColor = node.AmbientColor.ToVector3() * (float)node.AmbientIntensity;

        Entity ambientLightNode = World.CreateEntity("ambientLight");
        World.Set(ambientLightNode, new AmbientLightComponent
        {
            color = ambientColor
        });

        World.Relate(ambientLightNode, target, new ChildOf());
    }

    private void Unpack(GenericEntityNode node, Entity target)
    {
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
        var modelHandle = new ObjectHandle<RuntimeResource<Model>>(model);

        _resources.Add(modelHandle);

        World.Set(target, new ModelComponent
        {
            modelHandle = modelHandle
        });
    }

    private void Unpack(BrushNode node, Entity target)
    {
    }

    private void Unpack(TerrainNode node, Entity target)
    {
    }
}
