namespace Praxis.Core;

/// <summary>
/// Exception thrown if dependencies could not be satisfied
/// </summary>
public class DependencyException<T> : Exception
{
    public readonly T payload;

    public DependencyException(T payload) : base()
    {
        this.payload = payload;
    }
}

/// <summary>
/// Helper to resolve dependencies between a list of nodes
/// </summary>
internal class DependencyResolver<T>
{
    public class Node
    {
        public List<Node> dependsOn = new List<Node>();
        public T payload;

        public Node(T payload)
        {
            this.payload = payload;
        }

        public void AddLink(Node node)
        {
            if (!dependsOn.Contains(node))
            {
                dependsOn.Add(node);
            }
        }
    }

    private Queue<Node> items = new Queue<Node>();
    private List<Node> resolved = new List<Node>();
    private HashSet<Node> seen = new HashSet<Node>();

    /// <summary>
    /// Enqueue an item to be resolved
    /// </summary>
    /// <param name="payload">The item payload</param>
    /// <returns>The new node</returns>
    public Node AddNode(T payload)
    {
        Node node = new Node(payload);
        items.Enqueue(node);

        return node;
    }

    /// <summary>
    /// Resolve all dependencies
    /// </summary>
    /// <returns>An array of payload data ordered to satisfy dependencies</returns>
    public T[] Resolve()
    {
        resolved.Clear();
        seen.Clear();

        // keep resolving until there are no more nodes in the queue
        while (items.Count > 0)
        {
            ResolveDep(items.Dequeue());
        }

        T[] order = new T[resolved.Count];

        for (int i = 0; i < resolved.Count; i++)
        {
            order[i] = resolved[i].payload;
        }

        return order;
    }

    private void ResolveDep(Node node)
    {
        if (resolved.Contains(node)) return;

        seen.Add(node);

        foreach (var dep in node.dependsOn)
        {
            if (!resolved.Contains(dep))
            {
                if (seen.Contains(dep))
                {
                    throw new DependencyException<T>(dep.payload);
                }

                ResolveDep(dep);
            }
        }

        resolved.Add(node);
    }
}
