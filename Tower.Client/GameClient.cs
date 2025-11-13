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
using Tower.Net.Transport;
using Tower.Net.Session;

namespace Tower.Client;

public sealed class GameClient : Game
{
 private sealed class UiSink : IUiSink
 {
 private readonly Dictionary<string,(string text, Rectangle rect, Action onClick)> _buttons = new(StringComparer.Ordinal);
 public void ShowHudText(string text) => Log.Information("HUD: {Text}", text);
 public void Clear() { _buttons.Clear(); Log.Information("UI.Clear"); }
 public void AddText(string id, string text, float x, float y, string? fontId = null) => Log.Information("UI.AddText {Id} '{Text}' @({X},{Y}) {Font}", id, text, x, y, fontId);
 public void SetText(string id, string text) => Log.Information("UI.SetText {Id} '{Text}'", id, text);
 public void Remove(string id) { _buttons.Remove(id); Log.Information("UI.Remove {Id}", id); }
 public void AddButton(string id, string text, float x, float y, Action onClick)
 {
 // Use a default size for hitbox (no font metrics available here)
 var rect = new Rectangle((int)x, (int)y,120,30);
 _buttons[id] = (text, rect, onClick);
 Log.Information("UI.AddButton {Id} '{Text}' @({X},{Y})", id, text, x, y);
 }
 private ButtonState _prev = ButtonState.Released;
 public void UpdateMouse(MouseState ms)
 {
 var clicked = ms.LeftButton == ButtonState.Pressed && _prev == ButtonState.Released;
 if (clicked)
 {
 foreach (var kv in _buttons)
 {
 if (kv.Value.rect.Contains(ms.X, ms.Y))
 {
 try { kv.Value.onClick(); }
 catch (Exception ex) { Log.Error(ex, "UI button callback failed: {Id}", kv.Key); }
 break;
 }
 }
 }
 _prev = ms.LeftButton;
 }
 }

 private sealed class InputSink : IInputSink
 {
 private readonly Dictionary<string, Keys> _bindings = new(StringComparer.OrdinalIgnoreCase);
 private readonly Dictionary<string, Action> _subs = new(StringComparer.OrdinalIgnoreCase);
 private KeyboardState _prev;
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
 var nowDown = state.IsKeyDown(kv.Value);
 var wasDown = _prev.IsKeyDown(kv.Value);
 if (nowDown && !wasDown && _subs.TryGetValue(kv.Key, out var fn)) fn();
 }
 _prev = state;
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
 private readonly UiSink _ui = new();
 private ListenSync? _listenSync;
 private Tower.Net.Session.NetClient? _netClient;
 private bool _toastCached;

 public GameClient(bool smoke)
 {
 _smoke = smoke;
 _gdm = new GraphicsDeviceManager(this);
 _timers = new TimerService(_bus);
 _api = new GameApi(_assets, _bus, _systems, _timers);
 _api.SetSideGate(new Tower.Core.Engine.Prefabs.SideGate(false)); // client
 _lua = new LuaRuntime(_api);
 _systems.SetOnTickStart(() => _lua.SetInstructionBudget(200000));
 _mods = new ModBootstrapper(_assets, _lua, _api);
 _api.SetUiSink(_ui);
 _api.SetInputSink(_input);
 _sound = new SoundManager(_assets);
 _api.SetSoundSink(_sound);
 IsMouseVisible = true;
 }

 protected override void Initialize()
 {
 base.Initialize();
 var content = Path.Combine(AppContext.BaseDirectory, "Content");
 // In listen modes, a server thread has already been started and a duplex is in use by Program.
 // Here we still run offline fallback: load client UI directly if no sync was setup.
 if (_listenSync is null)
 {
 _mods.LoadAll(content, executeScripts: true, clientMode: true);
 Log.Information("Core OK / Mods Client UI Loaded");
 }
 if (_smoke) Exit();
 }

 public void AttachListenClient(Tower.Net.Session.NetClient client, ListenSync sync)
 {
 _netClient = client; _listenSync = sync;
 }

 protected override void Update(GameTime gameTime)
 {
 var dt = gameTime.ElapsedGameTime.TotalSeconds;
 _input.Poll();
 _ui.UpdateMouse(Mouse.GetState());
 _netClient?.Poll();
 if (_listenSync?.Started == true)
 {
 if (!_toastCached && _netClient is not null && _netClient.LoadingStarted)
 {
 if (_netClient.LastProcessedInputTick ==0) { _ui.ShowHudText("Using cached packs. Starting..."); _toastCached = true; }
 }
 _mods.LoadAll(Path.Combine(AppContext.BaseDirectory, "Content"), executeScripts: true, clientMode: true);
 _listenSync = null;
 }
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
