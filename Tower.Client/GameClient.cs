using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Modding;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Microsoft.Xna.Framework;
using Serilog;

namespace Tower.Client;

public sealed class GameClient : Game
{
 private readonly GraphicsDeviceManager _gdm;
 private readonly SystemRegistry _systems = new();
 private readonly EventBus _bus = new();
 private readonly AssetService _assets = new();
 private readonly TimerService _timers;
 private readonly LuaRuntime _lua;
 private readonly GameApi _api;
 private readonly ModBootstrapper _mods;
 private readonly bool _smoke;

 public GameClient(bool smoke)
 {
 _smoke = smoke;
 _gdm = new GraphicsDeviceManager(this);
 _timers = new TimerService(_bus);
 _api = new GameApi(_assets, _bus, _systems, _timers);
 _lua = new LuaRuntime(_api);
 _mods = new ModBootstrapper(_assets, _lua, _api);
 IsMouseVisible = true;
 }

 protected override void Initialize()
 {
 base.Initialize();
 _mods.LoadAll(Path.Combine(AppContext.BaseDirectory, "Content"));
 Log.Information("Core OK / Mods OK");
 if (_smoke) Exit();
 }

 protected override void Update(GameTime gameTime)
 {
 var dt = gameTime.ElapsedGameTime.TotalSeconds;
 _timers.Update(dt);
 _systems.Update(dt);
 base.Update(gameTime);
 }

 protected override void Draw(GameTime gameTime)
 {
 GraphicsDevice.Clear(Color.CornflowerBlue);
 base.Draw(gameTime);
 }
}
