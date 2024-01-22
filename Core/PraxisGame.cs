namespace Praxis.Core;

using System.Diagnostics;
using Microsoft.Xna.Framework;
using ResourceCache.Core;
using ResourceCache.FNA;

/// <summary>
/// Base class for a game running on the Praxis engine
/// </summary>
public class PraxisGame : Game
{
    /// <summary>
    /// A default world context constructed on game startup
    /// </summary>
    public readonly WorldContext DefaultContext;

    /// <summary>
    /// The resource loader
    /// </summary>
    public readonly ResourceManager Resources;

    private List<WorldContext> _worlds = new List<WorldContext>();

    public PraxisGame(string title, int width = 1280, int height = 720, bool vsync = true) : base()
    {
        Window.Title = title;

        new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = width,
            PreferredBackBufferHeight = height,
            SynchronizeWithVerticalRetrace = vsync
        };

        IsMouseVisible = true;
        IsFixedTimeStep = false;

        DefaultContext = new WorldContext("Default World", this);
        Resources = new ResourceManager();

        RegisterContext(DefaultContext);
    }

    /// <summary>
    /// Register a new world context to be updated & rendered
    /// </summary>
    public void RegisterContext(WorldContext context)
    {
        Debug.Assert(!_worlds.Contains(context));
        _worlds.Add(context);
    }

    /// <summary>
    /// Unregister a previously registered world context
    /// </summary>
    public void UnregisterContext(WorldContext context)
    {
        Debug.Assert(_worlds.Contains(context));
        _worlds.Remove(context);
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        // register default systems
        new CalculateTransformSystem(DefaultContext);
        new BasicForwardRenderer(DefaultContext);
        new CleanupSystem(DefaultContext);

        // register default resource loaders
        Resources.InstallFNAResourceLoaders(this);

        // material loader
        Resources.RegisterFactory((stream) => {
            return Material.Deserialize(this, stream);
        }, false);

        // GLB model loader
        Resources.RegisterFactory((stream) => {
            return GLBLoader.LoadModel(this, stream);
        }, false);

        // let subclasses perform initialization
        Init();
    }

    /// <summary>
    /// Initialize the game. This is where any systems should be created & installed, resource loaders installed, filesystems mounted, etc.
    /// </summary>
    protected virtual void Init()
    {
    }

    /// <summary>
    /// Teardown the game. Any custom world contexts should be disposed here
    /// </summary>
    protected virtual void Teardown()
    {
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach (var context in _worlds)
        {
            context.Update(dt);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        foreach (var context in _worlds)
        {
            context.Draw();
        }
    }

    protected override void UnloadContent()
    {
        base.UnloadContent();
        Teardown();
        DefaultContext.Dispose();
        Resources.UnloadAll();
    }
}
