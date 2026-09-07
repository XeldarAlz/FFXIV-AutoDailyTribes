using System.Numerics;

namespace AutoDailyTribes.Core.Tribes;

public readonly record struct TribeIssuer(uint BaseId, ulong InstanceId, Vector3 Location);
