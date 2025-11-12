# Asset Pipeline

- `assets.json` per mod declares logical IDs and paths
- Allowed extensions: .png .json .ogg .wav .ttf .mgfxo
- Paths sanitized (no `..`, no absolute)
- Anim JSON supports `grid` and `atlas` normalized to `AnimDef`
