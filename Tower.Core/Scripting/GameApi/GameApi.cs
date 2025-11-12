// Copyright (c) Tower.
// Licensed under the MIT License.

using Tower.Core.Engine.Assets;
using Tower.Core.Engine.EventBus;
using Tower.Core.Engine.Systems;
using Tower.Core.Engine.Timing;

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
 }
}
