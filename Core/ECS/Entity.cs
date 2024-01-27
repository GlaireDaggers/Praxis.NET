namespace Praxis.Core.ECS;

public struct Entity
{
    public readonly uint ID;
    public readonly string? Tag;

    internal Entity(uint id, string? tag = null)
    {
        ID = id;
        Tag = tag;
    }
}