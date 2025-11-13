using System.Text;

namespace Tower.Net.Session;

public sealed class NetServerConfig
{
 public string? SharedKey { get; init; }
 public long? OutboundBytesPerSecond { get; init; }
 public int? InboundCapBytes { get; init; }
 public int? HeartbeatTimeoutMs { get; init; }
 public (int joinPerSec, int authRespPerSec, int unauthMsgsPerSec)? AuthLimits { get; init; }
 public int? MaxRpcBytes { get; init; }
 public int? InputRateCapPerSec { get; init; }
 public int? RpcRateCapPerSec { get; init; }
 public long? ModBytesPerSecond { get; init; }
 public int? ModMaxChunkBytes { get; init; }
 public float? InterestRadius { get; init; }
 public string? MetricsDumpPath { get; init; }
 public int? MetricsDumpIntervalSeconds { get; init; }
 public int? MetricsLogIntervalSeconds { get; init; }
 public bool? UseDeltas { get; init; }
 public int? CompressThresholdBytes { get; init; }
 public bool? SnapshotBudgetEnabled { get; init; }

 public void ApplyTo(NetServer server)
 {
 if (!string.IsNullOrEmpty(SharedKey)) server.SetAuthKey(Encoding.UTF8.GetBytes(SharedKey!));
 if (OutboundBytesPerSecond.HasValue) server.SetOutboundRateLimit(OutboundBytesPerSecond.Value);
 if (InboundCapBytes.HasValue) server.SetInboundCap(InboundCapBytes.Value);
 if (HeartbeatTimeoutMs.HasValue) server.SetTimeout(HeartbeatTimeoutMs.Value);
 if (AuthLimits.HasValue) server.SetAuthLimits(AuthLimits.Value.joinPerSec, AuthLimits.Value.authRespPerSec, AuthLimits.Value.unauthMsgsPerSec);
 if (MaxRpcBytes.HasValue) server.SetMaxRpcBytes(MaxRpcBytes.Value);
 if (InputRateCapPerSec.HasValue) server.SetInputRateCap(InputRateCapPerSec.Value);
 if (RpcRateCapPerSec.HasValue) server.SetRpcRateCap(RpcRateCapPerSec.Value);
 if (ModBytesPerSecond.HasValue || ModMaxChunkBytes.HasValue) server.SetRateLimit(ModBytesPerSecond ??512*1024, ModMaxChunkBytes ??64*1024);
 if (InterestRadius.HasValue) server.SetInterestRadius(InterestRadius.Value);
 if (UseDeltas.HasValue) server.SetUseDeltas(UseDeltas.Value);
 if (CompressThresholdBytes.HasValue) server.SetCompressThreshold(CompressThresholdBytes.Value);
 if (SnapshotBudgetEnabled.HasValue) server.SetSnapshotBudgetEnabled(SnapshotBudgetEnabled.Value);
 }
}
