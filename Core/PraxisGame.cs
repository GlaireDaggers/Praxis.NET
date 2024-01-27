namespace Praxis.Core;

using Praxis.Core.ECS;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OWB;
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

    /// <summary>
    /// A blank white placeholder texture
    /// </summary>
    public Texture2D? DummyWhite { get; private set; }

    /// <summary>
    /// A blank black placeholder texture
    /// </summary>
    public Texture2D? DummyBlack { get; private set; }

    /// <summary>
    /// A flat normal map placeholder texture
    /// </summary>
    public Texture2D? DummyNormal { get; private set; }

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

    /// <summary>
    /// Load a new scene into the given world and return a reference to it
    /// </summary>
    public Scene LoadScene(string path, World world, IGenericEntityHandler? entityHandler = null)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters = {
                new JsonVector2Converter(),
                new JsonVector3Converter(),
                new JsonVector4Converter(),
                new JsonQuaternionConverter(),
                new JsonColorConverter(),
            }
        };
        using var stream = Resources.Open(path);
        Level level = JsonSerializer.Deserialize<Level>(stream, options)!;

        return new Scene(this, world, "content", level, entityHandler);
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        DummyWhite = new Texture2D(GraphicsDevice, 2, 2, false, SurfaceFormat.Color);
        DummyWhite.SetData(new [] {
            Color.White, Color.White,
            Color.White, Color.White
        });

        DummyBlack = new Texture2D(GraphicsDevice, 2, 2, false, SurfaceFormat.Color);
        DummyBlack.SetData(new [] {
            Color.Black, Color.Black,
            Color.Black, Color.Black
        });

        Color flatNormal = new Color(128, 128, 255);
        DummyNormal = new Texture2D(GraphicsDevice, 2, 2, false, SurfaceFormat.Color);
        DummyNormal.SetData(new [] {
            flatNormal, flatNormal,
            flatNormal, flatNormal
        });

        // register default systems
        new SimpleAnimationSystem(DefaultContext);
        new CalculateTransformSystem(DefaultContext);
        new BasicForwardRenderer(DefaultContext);
        new CleanupSystem(DefaultContext);

        // register default resource loaders
        Resources.InstallFNAResourceLoaders(this);

        // material loader
        Resources.RegisterFactory((stream) => {
            return Material.Deserialize(this, stream);
        }, false);

        // model loader
        Resources.RegisterFactory((stream) => {
            return ModelLoader.Load(this, stream);
        }, false);

        // let subclasses perform initialization
        Init();

        GC.Collect();
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
        Resources.UnloadAll();
    }
}
