namespace Praxis.Core.ECS;

public readonly struct Entity : IEquatable<Entity>
{
    public readonly uint ID;
    public readonly string? Tag;

    internal Entity(uint id, string? tag = null)
    {
        ID = id;
        Tag = tag;
    }

    public readonly bool Equals(Entity other)
    {
        return ID == other.ID;
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity e && Equals(e);
    }

    public static bool operator ==(Entity left, Entity right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Entity left, Entity right)
    {
        return !(left == right);
    }

    public override readonly int GetHashCode()
    {
        return (int)ID;
    }

    public override string ToString()
    {
        return Tag ?? $"<entity {ID}>";
    }
}