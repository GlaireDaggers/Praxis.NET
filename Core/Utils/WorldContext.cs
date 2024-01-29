using System.Diagnostics;
using System.Reflection;
using Praxis.Core.ECS;

namespace Praxis.Core;

/// <summary>
/// Wrapper around an ECS world and all of its associated systems
/// </summary>
public class WorldContext
{
    public readonly string Tag;
    public readonly PraxisGame Game;
    public readonly World World;

    public List<PraxisSystem> UpdateSystems => _updateSystems;
    public List<PraxisSystem> PostUpdateSystems => _postUpdateSystems;
    public List<PraxisSystem> DrawSystems => _drawSystems;

    private List<PraxisSystem> _updateSystems = new List<PraxisSystem>();
    private List<PraxisSystem> _postUpdateSystems = new List<PraxisSystem>();
    private List<PraxisSystem> _drawSystems = new List<PraxisSystem>();
    private Dictionary<Type, PraxisSystem> _systemTypeMap = new Dictionary<Type, PraxisSystem>();
    private DependencyResolver<PraxisSystem> _sysGraph = new DependencyResolver<PraxisSystem>();
    private Dictionary<Type, DependencyResolver<PraxisSystem>.Node> _sysCache = new Dictionary<Type, DependencyResolver<PraxisSystem>.Node>();
    private bool _systemsDirty = false;

    private float _cachedDt = 0f;

    public WorldContext(string tag, PraxisGame game)
    {
        Tag = tag;
        Game = game;
        World = new World();
        _systemsDirty = false;
    }

    public void Update(float dt)
    {
        _cachedDt = dt;

        if (_systemsDirty)
        {
            InstallSystems();
        }

        for (int i = 0; i < _updateSystems.Count; i++)
        {
            _updateSystems[i].Update(dt);
        }

        for (int i = 0; i < _postUpdateSystems.Count; i++)
        {
            _postUpdateSystems[i].Update(dt);
        }
    }

    public void Draw()
    {
        if (_systemsDirty)
        {
            InstallSystems();
        }

        for (int i = 0; i < _drawSystems.Count; i++)
        {
            _drawSystems[i].Update(_cachedDt);
        }
    }

    public void EndFrame()
    {
        World.PostUpdate();
    }

    /// <summary>
    /// Get a system of the given type, or null if no system of the given type has been registered
    /// </summary>
    public T? GetSystem<T>() where T : PraxisSystem
    {
        if (_systemTypeMap.ContainsKey(typeof(T)))
        {
            return _systemTypeMap[typeof(T)] as T;
        }

        return null;
    }

    // Installs pending systems that have been registered with this world context
    private void InstallSystems()
    {
        // somewhat goofy hack: just re-register any systems that were previously registered
        // this way, even if we've called InstallSystems, we can later still register new systems & call InstallSystems again

        foreach (var sys in _updateSystems)
        {
            RegisterSystem(sys);
        }
        foreach (var sys in _postUpdateSystems)
        {
            RegisterSystem(sys);
        }
        foreach (var sys in _drawSystems)
        {
            RegisterSystem(sys);
        }
        _updateSystems.Clear();
        _postUpdateSystems.Clear();
        _drawSystems.Clear();

        // generate links
        foreach (var kvp in _sysCache)
        {
            var execBefore = kvp.Key.GetCustomAttributes<ExecuteBeforeAttribute>();
            var execAfter = kvp.Key.GetCustomAttributes<ExecuteAfterAttribute>();

            foreach (var dep in execBefore)
            {
                // note: if the system this exec-before dependency specifies is not present, then we just ignore the dependency
                if (_sysCache.ContainsKey(dep.systemType))
                {
                    _sysCache[dep.systemType].AddLink(kvp.Value);
                }
            }

            foreach (var dep in execAfter)
            {
                // note: if the system this exec-after dependency specifies is not present, throw an error b/c the dependency cannot be satisfied
                try
                {
                    kvp.Value.AddLink(_sysCache[dep.systemType]);
                }
                catch(KeyNotFoundException)
                {
                    throw new DependencyException<PraxisSystem>(kvp.Value.payload);
                }
            }
        }

        // resolve dependencies & add to system execution list
        var sysList = _sysGraph.Resolve();

        foreach (var sys in sysList)
        {
            switch (sys.ExecutionStage)
            {
                case SystemExecutionStage.Update:
                    _updateSystems.Add(sys);
                    break;
                case SystemExecutionStage.PostUpdate:
                    _postUpdateSystems.Add(sys);
                    break;
                case SystemExecutionStage.Draw:
                    _drawSystems.Add(sys);
                    break;
            }
        }

        _systemsDirty = false;
    }

    // register a system to be installed
    internal void RegisterSystem(PraxisSystem system)
    {
        Debug.Assert(system.World == World);

        var node = _sysGraph.AddNode(system);
        _sysCache.Add(system.GetType(), node);

        _systemTypeMap.Add(system.GetType(), system);

        _systemsDirty = true;
    }
}
