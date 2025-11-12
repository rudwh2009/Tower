/* Copyright (c) Tower.
 * Licensed under the MIT License. */

namespace Tower.Core.Scripting.GameApi;

/// <summary>
/// Represents a Lua-style callback receiving an optional payload.
/// </summary>
/// <param name="arg">Optional payload.</param>
public delegate void LuaAction(object? arg);

/// <summary>
/// Represents a Lua system update function receiving delta time seconds.
/// </summary>
/// <param name="dt">Delta time seconds.</param>
public delegate void LuaUpdate(double dt);
