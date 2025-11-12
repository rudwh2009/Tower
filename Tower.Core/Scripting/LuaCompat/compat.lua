local warned = {}
local function warn_once(name)
 if warned[name] then return end
 warned[name] = true
 print("[compat] deprecated snake_case: " .. name)
end

local api = api
return {
 add_system = function(name, order, fn) warn_once('add_system'); api:AddSystem(name, order, fn) end,
 subscribe_event = function(ev, fn) warn_once('subscribe_event'); api:SubscribeEvent(ev, fn) end,
 emit_event = function(ev, payload) warn_once('emit_event'); api:EmitEvent(ev, payload) end,
 schedule_timer = function(d, fn) warn_once('schedule_timer'); return api:ScheduleTimer(d, fn) end,
 interval = function(s, fn) warn_once('interval'); return api:Interval(s, fn) end,
 cancel = function(id) warn_once('cancel'); return api:Cancel(id) end,
 register_texture = function(id, path) warn_once('register_texture'); api:RegisterTexture(id, path) end,
}
