using System.Text.Json;
using System.Text.Json.Serialization;
using Praxis.Core.ECS;

namespace Praxis.Core;

/// <summary>
/// Attribute to mark a serializable IComponentData (and which type name it should use)
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SerializedComponentAttribute : Attribute
{
    public readonly string TypeName;

    public SerializedComponentAttribute(string typeName) : base()
    {
        TypeName = typeName;
    }
}

/// <summary>
/// Indicates which kind of relation a child should have to its parent once it is deserialized
/// </summary>
public enum ChildRelationType
{
    ChildOf,
    BelongsTo
}

/// <summary>
/// Interface for serializable data of a particular component type
/// </summary>
public interface IComponentData
{
    /// <summary>
    /// Called after the entity template has been deserialized
    /// </summary>
    void OnDeserialize(PraxisGame game);

    /// <summary>
    /// Called to add the component to the given entity
    /// </summary>
    void Unpack(in Entity root, in Entity entity, World world);
}

/// <summary>
/// Container which can construct a new Entity hierarchy in a world
/// </summary>
public class EntityTemplate
{
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    [JsonPropertyName("relation")]
    public ChildRelationType Relation { get; set; } = ChildRelationType.ChildOf;

    [JsonPropertyName("components")]
    public IComponentData[]? Components { get; set; }
    
    [JsonPropertyName("children")]
    public EntityTemplate[]? Children { get; set; }

    public static EntityTemplate Deserialize(PraxisGame game, Stream stream)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters = {
                new JsonVector2Converter(),
                new JsonVector3Converter(),
                new JsonVector4Converter(),
                new JsonEulerConverter(),
                new JsonColorConverter(),
                new JsonComponentDataConverter()
            }
        };

        EntityTemplate entityTemplate = JsonSerializer.Deserialize<EntityTemplate>(stream, options)!;
        entityTemplate.OnDeserialize(game);

        return entityTemplate;
    }

    internal void OnDeserialize(PraxisGame game)
    {
        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.OnDeserialize(game);
            }
        }

        if (Components != null)
        {
            foreach (var component in Components)
            {
                component.OnDeserialize(game);
            }
        }
    }

    /// <summary>
    /// Unpack the entity into the world and return the entity handle
    /// </summary>
    public Entity Unpack(World world, in Entity? parent)
    {
        Dictionary<EntityTemplate, Entity> childMap = new Dictionary<EntityTemplate, Entity>();

        Entity root = UnpackHierarchy(world, parent, childMap);
        UnpackComponents(world, root, childMap);

        return root;
    }

    private Entity UnpackHierarchy(World world, in Entity? parent, Dictionary<EntityTemplate, Entity> childMap)
    {
        Entity entity = world.CreateEntity(Tag);
        childMap.Add(this, entity);

        if (parent != null)
        {
            switch (Relation)
            {
                case ChildRelationType.ChildOf:
                    world.Relate(entity, parent.Value, new ChildOf());
                    break;
                case ChildRelationType.BelongsTo:
                    world.Relate(entity, parent.Value, new BelongsTo());
                    break;
            }
        }

        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.UnpackHierarchy(world, entity, childMap);
            }
        }

        return entity;
    }

    private void UnpackComponents(World world, in Entity root, Dictionary<EntityTemplate, Entity> childMap)
    {
        if (Components != null)
        {
            foreach (var comp in Components)
            {
                comp.Unpack(root, childMap[this], world);
            }
        }

        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.UnpackComponents(world, root, childMap);
            }
        }
    }
}