# API Surface (Lua)

- Systems: AddSystem(name, order, fn)
- Events: SubscribeEvent(event, fn), EmitEvent(event, payload)
- Timers: ScheduleTimer(delay, fn), Interval(seconds, fn), Cancel(id)
- Assets: RegisterTexture/Sound/Anim/Font/Shader, GetTexture/Sound/Anim
- Prefabs: RegisterPrefab, AddPrefabPostInit, SpawnPrefab (stubs)
- Input/UI: OnAction, CreateHudText (no-op)

Compat: `compat.lua` exposes snake_case aliases which log once.
