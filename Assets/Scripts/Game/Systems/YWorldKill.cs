using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public class ZWorldKill : SystemBase
{
    EntityCommandBufferSystem ecbs;

    protected override void OnUpdate()
    {
        float worldYKill = -25.0f;
        var ecb = ecbs.CreateCommandBuffer();

        var ecbp = ecb.ToConcurrent();
        EntityManager em = EntityManager;

       Dependency = Entities.ForEach((Entity e, int entityInQueryIndex, ref Translation t) => { 
        
            if(t.Value.y <= worldYKill)
            {

                if(em.HasComponent<Player>(e))
                {
                    t.Value = new float3(0, 10, 0);
                   ecbp.SetComponent<Velocity>(entityInQueryIndex, e, new Velocity() { value = float3.zero });
                }
                else
                {
                    ecbp.DestroyEntity(entityInQueryIndex, e);
                }


            }
        
        }).Schedule(Dependency);
        Dependency.Complete();
        ecbs.AddJobHandleForProducer(Dependency);
    }

    protected override void OnCreate()
    {
        ecbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
}
