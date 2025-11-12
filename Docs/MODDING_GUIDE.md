# Modding Guide

Structure:
- Content/BaseGame
- Content/Mods/<ModId>

Files:
- modinfo.json: id, version, api_version, dependencies, entry
- assets.json: assets array with id/path
- modmain.lua: uses `api` to register systems, timers, assets
