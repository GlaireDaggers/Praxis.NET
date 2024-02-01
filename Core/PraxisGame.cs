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
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework.Audio;

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
    private bool _debugPause = false;
    private bool _debugStep = false;

    private List<JsonConverter> _converters = [
        new JsonVector2Converter(),
        new JsonVector3Converter(),
        new JsonVector4Converter(),
        new JsonQuaternionConverter(),
        new JsonColorConverter()
    ];

    public PraxisGame(string title, int width = 1280, int height = 720, bool vsync = true) : base()
    {
        Window.Title = title;

        new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = width,
            PreferredBackBufferHeight = height,
            SynchronizeWithVerticalRetrace = vsync,
            PreferMultiSampling = true
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
    /// Construct serializer options which is aware of Praxis types & resources
    /// </summary>
    public JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();
        foreach (var converter in _converters)
        {
            options.Converters.Add(converter);
        }

        return options;
    }

    /// <summary>
    /// Load a new scene into the given world and return a reference to it
    /// </summary>
    public Scene LoadScene(string path, World world, IGenericEntityHandler? entityHandler = null)
    {
        JsonSerializerOptions options = CreateJsonOptions();
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

    public void RegisterResourceType<T>()
    {
        _converters.Add(new JsonResourceHandleConverter<T>(this));
        _converters.Add(new JsonRuntimeResourceConverter<T>(this));
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
        new ParticleEmitterSystem(DefaultContext);
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

        // entity template loader
        Resources.RegisterFactory((stream) => {
            return EntityTemplate.Deserialize(this, stream);
        }, false);

        RegisterResourceType<Effect>();
        RegisterResourceType<Texture2D>();
        RegisterResourceType<TextureCube>();
        RegisterResourceType<SoundEffect>();
        RegisterResourceType<Material>();
        RegisterResourceType<Model>();
        RegisterResourceType<EntityTemplate>();

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
        if (GetKeyPressed(Keys.F10))
        {
            // step one frame
            _debugPause = false;
            _debugStep = true;
        }

        if (GetKeyPressed(Keys.F11))
        {
            // toggle pause
            _debugPause = !_debugPause;
        }

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

        if (!_debugPause)
        {
            foreach (var context in _worlds)
            {
                context.Update(dt);
            }
        }

        if (_debugStep)
        {
            _debugPause = true;
            _debugStep = false;
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
                DebugFilters();

                EntityPreview();
            }
            _imGuiRenderer.AfterLayout();
        }
        #endif
    }

    private string _entityPreviewPath = "";
    private Entity? _previewEntity;
    private void EntityPreview()
    {
        if (ImGui.Begin("Entity Preview"))
        {
            ImGui.InputText("Entity Template Path", ref _entityPreviewPath, 1024);
            if (ImGui.Button("Load"))
            {
                if (_previewEntity != null)
                {
                    DefaultContext.World.Send(new DestroyEntity
                    {
                        entity = _previewEntity.Value
                    });
                    _previewEntity = null;
                }

                try
                {
                    var entityDef = Resources.Load<EntityTemplate>(_entityPreviewPath);
                    _previewEntity = entityDef.Value.Unpack(DefaultContext.World, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed spawning entity: " + e.Message);
                }
            }
            if (ImGui.Button("Delete"))
            {
                if (_previewEntity != null)
                {
                    DefaultContext.World.Send(new DestroyEntity
                    {
                        entity = _previewEntity.Value
                    });
                    _previewEntity = null;
                }
            }
            ImGui.End();
        }
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
                    if (ImGui.TreeNodeEx("Update", flags))
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
                    if (ImGui.TreeNodeEx("PostUpdate", flags))
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
                    if (ImGui.TreeNodeEx("Draw", flags))
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

    private void DebugFilters()
    {
        if (ImGui.Begin("Filters"))
        {
            for (int i = 0; i < _worlds.Count; i++)
            {
                var world = _worlds[i];

                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.DefaultOpen;
                if (ImGui.TreeNodeEx((world.Tag ?? $"<World {i}>") + $"##{i}", flags))
                {
                    int unnamedFilterCount = 0;
                    foreach (var filter in world.World.AllFilters)
                    {
                        if (filter.tag != null)
                        {
                            bool included = _selectedEntity != null && _selectedWorld == world.World && filter.Contains(_selectedEntity.Value);

                            if (!included)
                            {
                                ImGui.BeginDisabled();
                            }

                            if (ImGui.TreeNodeEx(filter.tag, ImGuiTreeNodeFlags.Leaf))
                            {
                                ImGui.TreePop();
                            }

                            if (!included)
                            {
                                ImGui.EndDisabled();
                            }
                        }
                        else
                        {
                            unnamedFilterCount++;
                        }
                    }

                    if (unnamedFilterCount > 0)
                    {
                        ImGui.BeginDisabled();
                        if (ImGui.TreeNodeEx($"{unnamedFilterCount} untagged filter(s)##{i}", ImGuiTreeNodeFlags.Leaf))
                        {
                            ImGui.TreePop();
                        }
                        ImGui.EndDisabled();
                    }

                    ImGui.TreePop();
                }
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
