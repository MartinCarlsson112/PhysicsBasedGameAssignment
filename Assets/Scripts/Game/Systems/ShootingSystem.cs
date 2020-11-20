using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

public class ShootingSystem : SystemBase
{
    EntityArchetype bulletArchetype;
    EntityQuery query;
    protected override void OnUpdate()
    {
        var playerArray = query.ToComponentDataArray<Player>(Unity.Collections.Allocator.Temp);
        var playerPos = query.ToComponentDataArray<Translation>(Unity.Collections.Allocator.Temp);
        var playerCount = query.CalculateEntityCount();
        var playerRot = query.ToComponentDataArray<Rotation>(Unity.Collections.Allocator.Temp);
        var playerShootingComponent = query.ToComponentDataArray<ShootingComponent>(Unity.Collections.Allocator.Temp);
        Dependency = Entities.WithStoreEntityQueryInField(ref query).ForEach((ref ShootingComponent shootingComp, in Player player, in Translation pos, in Rotation rotation) =>
            {

            }).Schedule(Dependency);
        Dependency.Complete();


        if (Input.GetKeyDown(KeyCode.Mouse0) && playerCount > 0)
        {
            var entity = EntityManager.CreateEntity(bulletArchetype);
            var direction = math.normalize(math.mul(playerShootingComponent[playerCount - 1].aimDirection, new float3(0, 0, 1)));
            var pos = playerPos[playerCount - 1].Value + direction * 3.0f;

            EntityManager.SetComponentData(entity, new Translation() { Value = pos });
            EntityManager.SetComponentData(entity, new Velocity() { value = direction * 25.0f });
            EntityManager.SetComponentData(entity, new Rotation() { Value = quaternion.identity });
            EntityManager.SetSharedComponentData(entity, new RenderMesh() { mesh = Bootstrap.staticSphere, material = Bootstrap.staticSphereMat });
            EntityManager.SetComponentData(entity, ColliderHelper.MakeSphereCollider(0.5f, false));
            EntityManager.SetComponentData(entity, new PhysicsBody() { elasticity = 0.9f, inertiaTensorInv = Bootstrap.CalculateSphereInverseInertiaTensor(0.5f, 1) });
            EntityManager.SetComponentData(entity, new AngularVelocity() { value = float3.zero });
            EntityManager.SetComponentData(entity, new Scale() { Value = 1 });

            EntityManager.AddBuffer<CollisionResult>(entity);
            EntityManager.SetComponentData(entity, new Gravity() { value = new float3(0, -9.81f, 0) });
            EntityManager.SetComponentData(entity, new Impulse { impulse = float3.zero, angularImpulse = float3.zero });
            EntityManager.SetComponentData(entity, new Mass() { value = 1 });
        }
        

        playerShootingComponent.Dispose();
        playerArray.Dispose();
        playerPos.Dispose();
        playerRot.Dispose();
    }

    protected override void OnCreate()
    {
        bulletArchetype = EntityManager.CreateArchetype(
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(Rotation),
            typeof(Scale),
            typeof(Velocity),
            typeof(AngularVelocity),
            typeof(Impulse),
            typeof(Gravity),
            typeof(Dynamic),
            typeof(Mass),
            typeof(PhysicsBody),
            typeof(ColliderComponent),
            typeof(RenderBounds),
            typeof(RenderMesh),
            typeof(BulletComponent));
    }
}
