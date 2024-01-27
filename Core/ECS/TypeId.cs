namespace Praxis.Core.ECS;

internal class TypeIdAssigner
{
    protected static uint _counter;
}

internal class TypeIdAssigner<T> : TypeIdAssigner
{
    public static readonly uint ID;

    static TypeIdAssigner()
    {
        ID = _counter++;
    }
}

internal static class TypeId
{
    public static uint GetTypeId<T>()
    {
        return TypeIdAssigner<T>.ID;
    }
}