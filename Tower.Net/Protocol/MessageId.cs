namespace Tower.Net.Protocol;

public enum MessageId : byte
{
 JoinRequest =1,
 JoinAccepted =2,
 RpcEvent =3,
 InputCmd =4,
 Snapshot =5,
 ModSetAdvertise =6,
 ModSetAck =7,
 ModChunk =8,
 ModDone =9,
 StartLoading =10,
 Heartbeat =11,
 SnapshotSet =12,
 AuthChallenge =13,
 AuthResponse =14,
 SnapshotBaseline =15,
 SnapshotDelta =16,
 Compressed =17,
 CompBaseline =18,
 CompDelta =19,
 Reliable =20,
 ReliableAck =21,
 SnapshotAck =22,
}
