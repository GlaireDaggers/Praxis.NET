namespace Praxis.Core;

using Praxis.Core.ECS;
using Praxis.Core.DebugGui;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OWB;
using ResourceCache.Core;
using ResourceCache.FNA;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;

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

    public KeyboardState CurrentKeyboardState => _curKbState;
    public KeyboardState PreviousKeyboardState => _prevKbState;

    public MouseState CurrentMouseState => _curMouseState;
    public MouseState PreviousMouseState => _prevMouseState;

    private List<WorldContext> _worlds = new List<WorldContext>();

    private ImGuiRenderer? _imGuiRenderer;

    private KeyboardState _prevKbState;
    private KeyboardState _curKbState;

    private MouseState _prevMouseState;
    private MouseState _curMouseState;

    private bool _debugMode = false;

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

    public bool GetKeyPressed(Keys key)
    {
        return _curKbState.IsKeyDown(key) && !_prevKbState.IsKeyDown(key);
    }

    public bool GetKeyReleased(Keys key)
    {
        return _curKbState.IsKeyUp(key) && !_prevKbState.IsKeyUp(key);
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        _imGuiRenderer = new ImGuiRenderer(this);
        _imGuiRenderer.RebuildFontAtlas();

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
        new PhysicsSystem(DefaultContext);
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

        _prevKbState = _curKbState;
        _curKbState = Keyboard.GetState();

        _prevMouseState = _curMouseState;
        _curMouseState = Mouse.GetState();

        #if DEBUG
        if (GetKeyPressed(Keys.F12))
        {
            // toggle debug mode
            _debugMode = !_debugMode;

            // post message to allow systems to respond to enter/exit debug
            foreach (var world in _worlds)
            {
                world.World.Send(new DebugModeMessage
                {
                    enableDebug = _debugMode
                });
            }
        }
        #endif

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
            context.EndFrame();
        }

        #if DEBUG
        if (_debugMode)
        {
            _imGuiRenderer!.BeforeLayout(gameTime);
            {
                DebugEntities();
                DebugSystems();
                DebugComponents();
            }
            _imGuiRenderer.AfterLayout();
        }
        #endif
    }

    private void DebugEntities()
    {
        if (ImGui.Begin("Entities"))
        {
            for (int i = 0; i < _worlds.Count; i++)
            {
                var world = _worlds[i];

                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.DefaultOpen;
                if (ImGui.TreeNodeEx((world.Tag ?? $"<World {i}>") + $"##{i}", flags))
                {
                    foreach (var entity in world.World.AllEntities)
                    {
                        if (!world.World.HasOutRelations<ChildOf>(entity) && !world.World.HasOutRelations<BelongsTo>(entity))
                        {
                            DebugEntity(world.World, entity);
                        }
                    }
                    ImGui.TreePop();
                }
            }

            ImGui.End();
        }
    }

    private void DebugSystems()
    {
        if (ImGui.Begin("Systems"))
        {
            for (int i = 0; i < _worlds.Count; i++)
            {
                var world = _worlds[i];

                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.DefaultOpen;
                if (ImGui.TreeNodeEx((world.Tag ?? $"<World {i}>") + $"##{i}", flags))
                {
                    if (ImGui.TreeNode("Update"))
                    {
                        foreach (var sys in world.UpdateSystems)
                        {
                            if (ImGui.TreeNodeEx(sys.GetType().Name, ImGuiTreeNodeFlags.Leaf))
                            {
                                ImGui.TreePop();
                            }
                        }
                        ImGui.TreePop();
                    }
                    if (ImGui.TreeNode("PostUpdate"))
                    {
                        foreach (var sys in world.PostUpdateSystems)
                        {
                            if (ImGui.TreeNodeEx(sys.GetType().Name, ImGuiTreeNodeFlags.Leaf))
                            {
                                ImGui.TreePop();
                            }
                        }
                        ImGui.TreePop();
                    }
                    if (ImGui.TreeNode("Draw"))
                    {
                        foreach (var sys in world.DrawSystems)
                        {
                            if (ImGui.TreeNodeEx(sys.GetType().Name, ImGuiTreeNodeFlags.Leaf))
                            {
                                ImGui.TreePop();
                            }
                        }
                        ImGui.TreePop();
                    }
                    ImGui.TreePop();
                }
            }

            ImGui.End();
        }
    }

    private Entity? _selectedEntity;
    private World? _selectedWorld;
    private void DebugEntity(World world, in Entity entity)
    {
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.DefaultOpen;

        if (!world.HasInRelations<ChildOf>(entity) && !world.HasInRelations<BelongsTo>(entity))
        {
            flags |= ImGuiTreeNodeFlags.Leaf;
        }

        if (_selectedEntity is Entity e && entity.ID == e.ID && _selectedWorld == world)
        {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        bool isOpen = ImGui.TreeNodeEx((entity.Tag ?? $"<Entity {entity.ID}>") + $"##{entity.ID}", flags);

        if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            _selectedWorld = world;
            _selectedEntity = entity;
        }

        if (isOpen)
        {
            if (world.HasInRelations<ChildOf>(entity))
            {
                foreach (var child in world.GetInRelations<ChildOf>(entity))
                {
                    DebugEntity(world, child);
                }
            }

            if (world.HasInRelations<BelongsTo>(entity))
            {
                foreach (var child in world.GetInRelations<BelongsTo>(entity))
                {
                    DebugEntity(world, child);
                }
            }

            ImGui.TreePop();
        }
    }

    IndexableSet<Type> _cachedComponentTypes = new IndexableSet<Type>();
    private void DebugComponents()
    {
        if (ImGui.Begin("Components"))
        {
            if (_selectedWorld != null && _selectedEntity != null)
            {
                _cachedComponentTypes.Clear();
                _selectedWorld.GetComponentTypes(_selectedEntity.Value, _cachedComponentTypes);

                foreach (var component in _cachedComponentTypes.AsSpan)
                {
                    ImGui.Text(component.Name);
                }
            }
            else
            {
                ImGui.Text("Nothing selected");
            }
            
            ImGui.End();
        }
    }

    protected override void UnloadContent()
    {
        base.UnloadContent();
        Teardown();
        Resources.UnloadAll();
    }
}
