using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public struct PhysicsBody : IComponentData
{
    public float elasticity;
    public float3 inertiaTensorInv;
}
