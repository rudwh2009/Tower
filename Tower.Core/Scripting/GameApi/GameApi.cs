// Copyright (c) Tower.
// Licensed under the MIT License.

using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;
using Tower.Core.Engine.Entities;
using Tower.Core.Engine.Prefabs;

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Canonical engine API surface exposed to Lua scripts and mods. Partial by feature area.
/// </summary>
public sealed partial class GameApi
{
 private readonly IAssetService assets;
 private readonly IEventBus bus;
 private readonly SystemRegistry systems;
 private readonly ITimerService timers;

 // Prefab services (defaulted, can be overridden by host)
 private IPrefabRegistry prefabRegistry = new PrefabRegistry();
 private IHookBus hookBus = new HookBus();
 private IEntitySpawner spawner = new EntitySpawner();
 private ISideGate sideGate = new SideGate(true); // default server-mode for singleplayer

 /// <summary>Allows host to override the side gate (server/client).</summary>
 public void SetSideGate(ISideGate gate) => sideGate = gate ?? throw new ArgumentNullException(nameof(gate));
 /// <summary>Allows host to override prefab services.</summary>
 public void SetPrefabServices(IPrefabRegistry reg, IHookBus hooks, IEntitySpawner spawn)
 {
 prefabRegistry = reg ?? throw new ArgumentNullException(nameof(reg));
 hookBus = hooks ?? throw new ArgumentNullException(nameof(hooks));
 spawner = spawn ?? throw new ArgumentNullException(nameof(spawn));
 }

 /// <summary>
 /// Initializes a new instance of the <see cref="GameApi"/> class.
 /// </summary>
 /// <param name="assets">Asset service.</param>
 /// <param name="bus">Event bus.</param>
 /// <param name="systems">System registry.</param>
 /// <param name="timers">Timer service.</param>
 public GameApi(IAssetService assets, IEventBus bus, SystemRegistry systems, ITimerService timers)
 {
 this.assets = assets;
 this.bus = bus;
 this.systems = systems;
 this.timers = timers;
 OnConstructed(); // ensure partial initialization (e.g., VFX)
 }

 // Partial extension point defined in VFX file
 partial void OnConstructed();
}
