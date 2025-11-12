namespace Tower.Core.Engine.Assets.Anim;

public sealed record AnimFrame(int X, int Y, int W, int H, int DurationMs);

public sealed record AnimDef(string Type, int Frames, int FrameWidth, int FrameHeight, IReadOnlyList<AnimFrame> FramesMeta);
