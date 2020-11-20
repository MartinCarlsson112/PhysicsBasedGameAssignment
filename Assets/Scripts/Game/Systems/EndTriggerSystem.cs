using Unity.Entities;
using Unity.Collections;
[UpdateAfter(typeof(CollisionSystem))]
public class EndTriggerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;

        NativeList<int> endGame = new NativeList<int>(Allocator.TempJob);
        var endGameParallel = endGame.AsParallelWriter();
        Dependency = Entities.ForEach((ref DynamicBuffer<CollisionResult> collisions, in EndTrigger trigger) =>
            {
                for(int i =0; i < collisions.Length; i++)
                {
                    if (em.HasComponent<Player>(collisions[i].other))
                    {
                        endGame.Add(1);
                    }
                }
            }).Schedule(Dependency);

        Dependency.Complete();

        if (endGame.Length > 0)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("YouWon");
        }
        endGame.Dispose();
    }
}
