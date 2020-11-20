using Unity.Entities;
using Unity.Mathematics;
public struct ShootingComponent : IComponentData
{
    public float Cooldown;
    public float Accu;
    public quaternion aimDirection;
}
