using Unity.Entities;
using Unity.Mathematics;

public struct UnlockablePlatform : IComponentData
{
    public int level;
    public float3 targetPosition;
}

