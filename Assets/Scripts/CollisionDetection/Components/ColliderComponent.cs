using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
public enum ColliderType : short
{
    Box,
    Sphere,
    Capsule,
    Plane,
    Other
}

public static class ColliderHelper
{
    public static ColliderComponent MakeBoxCollider(float3 halfSize, bool trigger)
    {
        ColliderComponent collider = new ColliderComponent
        {
            type = ColliderType.Box,
            halfSize = halfSize,
            isTrigger = trigger,
        };
        return collider;
    }

    public static ColliderComponent MakeCapsuleCollider(float height, float radius, bool trigger)
    {
        ColliderComponent collider = new ColliderComponent
        {
            yBase = -height * 0.5f,
            yCap = height * 0.5f,
            radius = radius,
            isTrigger = trigger,
            halfSize = new float3(radius, height * 0.5f, radius),
            type = ColliderType.Capsule
        };
        return collider;
    }

    public static ColliderComponent MakeSphereCollider(float radius, bool trigger)
    {
        ColliderComponent collider = new ColliderComponent
        {
            type = ColliderType.Sphere,
            radius = radius,
            isTrigger = trigger,
            halfSize = new float3(radius, radius, radius)
        };
        return collider;
    }

    public static ColliderComponent MakePlaneCollider(float3 normal, float3 size, bool trigger)
    {
        ColliderComponent collider = new ColliderComponent
        {
            type = ColliderType.Plane
        };

        return collider;
    }
}

public struct CollisionResult : IBufferElementData
{
    public Entity other;
    public ContactData contactData;
    public bool isTrigger;
}


public struct ColliderComponent : IComponentData
{
    public bool isTrigger;
    public ColliderType type;
    public float4x4 localToWorld;
    public float4x4 localToWorldInverse;
    public float3 min;
    public float3 max;
    public float3 halfSize;
    public float radius;
    public float yBase;
    public float yCap;
}

public struct Tag
{
    public int tag;
}

public struct Static : IComponentData
{
    public int flags;
}

public struct Dynamic : IComponentData
{
    public int flags;
    public bool grounded;
    public bool jumped;
}