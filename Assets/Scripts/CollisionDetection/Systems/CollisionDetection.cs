using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Burst;



[BurstCompile]
struct CollisionPair
{
    public CollisionPair(Entity a, SupportData aSupport, Entity b, SupportData bSupport)
    {
        this.a = a;
        this.b = b;
        this.aSupport = aSupport;
        this.bSupport = bSupport;
    }

    public Entity a, b;
    public SupportData aSupport, bSupport;
}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
class CollisionSystem : SystemBase
{
    private EntityQuery query;

    protected override void OnUpdate()
    {

        Dependency = Entities.WithBurst().ForEach((ref DynamicBuffer<CollisionResult> collisionRes) =>
        {
            collisionRes.Clear();
        }).Schedule(Dependency);

        Dependency = Entities.WithBurst().WithChangeFilter<Translation>().WithChangeFilter<Rotation>().ForEach((ref ColliderComponent collider, in Translation pos, in Rotation rot) =>
        {
            collider.min = pos.Value - (collider.halfSize * 10.0f);
            collider.max = pos.Value + (collider.halfSize * 10.0f);
            collider.localToWorld = float4x4.TRS(pos.Value, rot.Value, new float3(1));
            collider.localToWorldInverse = math.inverse(collider.localToWorld);
        }).Schedule(Dependency);

        Dependency.Complete();

        int count = query.CalculateEntityCount();
        NativeArray<ColliderComponent> colliders = query.ToComponentDataArray<ColliderComponent>(Allocator.TempJob);
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
        NativeArray<Translation> positions = query.ToComponentDataArray<Translation>(Allocator.TempJob);
        var ECB = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
        var EntityCommandBuffer = ECB.ToConcurrent();

 
        Dependency = Entities
            .WithStoreEntityQueryInField(ref query)
            .WithReadOnly(colliders)
            .WithReadOnly(entities)
            .WithReadOnly(positions)
            .WithBurst()
            .ForEach((Entity entity, int entityInQueryIndex, in ColliderComponent collider, in Translation position) => {
                for (int i = entityInQueryIndex + 1; i < count; i++)
                {
                    if (AABBIntersect(collider.min, collider.max, colliders[i].min, colliders[i].max))
                    {
                        SupportData aSupport = new SupportData
                        {
                            collider = collider,
                            pos = position.Value
                        };

                        SupportData bSupport = new SupportData
                        {
                            pos = positions[i].Value,
                            collider = colliders[i]
                        };
                        ContactData contact = new ContactData() ;

                        if (GJK.intersect(ref aSupport, ref bSupport, ref contact))
                        {
                            bool isTrigger = false;
                            if(collider.isTrigger || colliders[i].isTrigger)
                            {
                                isTrigger = true;
                            }
                            EntityCommandBuffer.AppendToBuffer(entityInQueryIndex, entity, new CollisionResult() { other = entities[i], contactData = contact, isTrigger = isTrigger });
                            contact.normal = -contact.normal;
                            var tempContact = contact.worldSpacePointA;
                            contact.worldSpacePointA = contact.worldSpacePointB;
                            
                            contact.worldSpacePointA = tempContact;

                            EntityCommandBuffer.AppendToBuffer(entityInQueryIndex, entities[i], new CollisionResult() { other = entity, contactData = contact, isTrigger = isTrigger });

                        }
                    }
                }
        }).ScheduleParallel(Dependency);
        Dependency.Complete();
        ECB.Playback(World.EntityManager);
        ECB.Dispose();
        colliders.Dispose();
        entities.Dispose();
        positions.Dispose();
    }

    static bool AABBIntersect(float3 aMin, float3 aMax, float3 bMin, float3 bMax)
    {
        return (aMin.x <= bMax.x && aMax.x >= bMin.x) &&
                   (aMin.y <= bMax.y && aMax.y >= bMin.y) &&
                  (aMin.z <= bMax.z && aMax.z >= bMin.z);
    }
}




