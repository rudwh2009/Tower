# Architecture

- C# engine only: loop, assets, ECS wrappers, events/timers, networking, Lua host
- Lua for gameplay: same sandbox for base content and mods
- Logical IDs: <modId>/<type>/<name>, last-writer-wins by load order
- Soft-fail everywhere; errors log via Serilog, engine continues
- Tests cover core services and integration stubs
