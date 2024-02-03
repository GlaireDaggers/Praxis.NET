using Microsoft.Xna.Framework;
using OWB;
using PlatformerSample;
using Praxis.Core;
using Praxis.Core.ECS;

namespace Platformer;

/// <summary>
/// Default game state for platformer game
/// </summary>
public class DefaultGameState : GameState, IGenericEntityHandler
{
    private readonly WorldContext _context;

    private Scene? _scene;
    private Filter _spawnFilter;

    public DefaultGameState(PraxisGame game) : base(game)
    {
        _context = new WorldContext("Platformer World", Game);

        _spawnFilter = new FilterBuilder(_context.World)
            .Include<TransformComponent>()
            .Include<PlayerSpawnComponent>()
            .Build();
    }

    public override void OnEnter()
    {
        base.OnEnter();

        Game.RegisterContext(_context);
        Game.InstallDefaultSystems(_context);

        _scene = Game.LoadScene("content/levels/TestLevel.owblevel", _context.World, this);

        var playerTemplate = Game.Resources.Load<EntityTemplate>("content/entities/Player.json");

        var spawnPos = _context.World.Get<TransformComponent>(_spawnFilter.FirstEntity);

        var cam = _context.World.CreateEntity("camera");
        _context.World.Set(cam, new TransformComponent(Vector3.Zero, Quaternion.Identity, Vector3.One));
        _context.World.Set(cam, new CameraComponent
        {
            fieldOfView = 60f,
            near = 0.1f,
            far = 1000.0f,
            clearColor = Color.CornflowerBlue,
        });

        var player = playerTemplate.Value.Unpack(_context.World, null);
        _context.World.Set(player, spawnPos);

        if (_context.World.FindTaggedEntityInChildren("Mesh", player) is Entity playerMesh)
        {
            var anim = _context.World.Get<SimpleAnimationComponent>(playerMesh);
            var model = _context.World.Get<ModelComponent>(playerMesh);
            anim.SetAnimation(model.model.Value.GetAnimationId("idle"));
            _context.World.Set(playerMesh, anim);
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        _scene?.Dispose();
        Game.UnregisterContext(_context);
    }

    public override void Draw()
    {
        base.Draw();
        Game.GraphicsDevice.Clear(Color.CornflowerBlue);
    }

    public void Unpack(GenericEntityNode node, World world, Entity target)
    {
        var transform = world.Get<TransformComponent>(target);
        Console.WriteLine("Found spawn at " + transform.position);
        world.Set(target, new PlayerSpawnComponent());
    }
}
