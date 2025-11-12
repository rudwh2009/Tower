using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Modding;
using Tower.Core.Scripting;
using Tower.Core.Scripting.GameApi;
using Tower.Core.Engine.UI;
using Tower.Core.Engine.Input;
using Tower.Client.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Serilog;

namespace Tower.Client;

public sealed class GameClient : Game
{
 private sealed class UiSink : IUiSink
 {
 public void ShowHudText(string text) => Log.Information("HUD: {Text}", text);
 public void Clear() => Log.Information("UI.Clear");
 public void AddText(string id, string text, float x, float y, string? fontId = null) => Log.Information("UI.AddText {Id} '{Text}' @({X},{Y}) {Font}", id, text, x, y, fontId);
 public void SetText(string id, string text) => Log.Information("UI.SetText {Id} '{Text}'", id, text);
 public void Remove(string id) => Log.Information("UI.Remove {Id}", id);
 public void AddButton(string id, string text, float x, float y, Action onClick) => Log.Information("UI.AddButton {Id} '{Text}' @({X},{Y})", id, text, x, y);
 }

 private sealed class InputSink : IInputSink
 {
 private readonly Dictionary<string, Keys> _bindings = new(StringComparer.OrdinalIgnoreCase);
 private readonly Dictionary<string, Action> _subs = new(StringComparer.OrdinalIgnoreCase);
 public void Bind(string action, string key)
 {
 if (Enum.TryParse<Keys>(key, true, out var k)) _bindings[action] = k;
 }
 public void Subscribe(string action, Action onPress) { _subs[action] = onPress; }
 public void Poll()
 {
 var state = Keyboard.GetState();
 foreach (var kv in _bindings)
 {
 if (state.IsKeyDown(kv.Value) && _subs.TryGetValue(kv.Key, out var fn)) fn();
 }
 }
 }

 private readonly GraphicsDeviceManager _gdm;
 private readonly SystemRegistry _systems = new();
 private readonly EventBus _bus = new();
 private readonly AssetService _assets = new();
 private readonly TimerService _timers;
 private readonly LuaRuntime _lua;
 private readonly GameApi _api;
 private readonly ModBootstrapper _mods;
 private readonly bool _smoke;
 private readonly SoundManager _sound;
 private readonly InputSink _input = new();

 public GameClient(bool smoke)
 {
 _smoke = smoke;
 _gdm = new GraphicsDeviceManager(this);
 _timers = new TimerService(_bus);
 _api = new GameApi(_assets, _bus, _systems, _timers);
 _api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(false)); // client
 _lua = new LuaRuntime(_api);
 _mods = new ModBootstrapper(_assets, _lua, _api);
 _api.SetUiSink(new UiSink());
 _api.SetInputSink(_input);
 _sound = new SoundManager(_assets);
 _api.SetSoundSink(_sound);
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
 _input.Poll();
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
