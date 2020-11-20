using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

public class ProgressionSystem : SystemBase
{
    EntityQuery levelPlatformQuery;
    protected override void OnUpdate()
    {

        int nLevels = levelPlatformQuery.CalculateEntityCount();
        NativeArray<int> levelKeysCounter = new NativeArray<int>(nLevels, Allocator.TempJob);

        Dependency = Entities.WithStoreEntityQueryInField(ref levelPlatformQuery).ForEach((in Translation pos, in UnlockablePlatform platForm) =>
        {
        }).Schedule(Dependency);
        Dependency.Complete();


        Dependency = Entities.ForEach((in Key keys) => {
            levelKeysCounter[keys.level]++;
        }).Schedule(Dependency);
        Dependency.Complete();


        Dependency = Entities.ForEach((ref Translation pos, in UnlockablePlatform platForm) =>
        {
            if (levelKeysCounter[platForm.level] == 0)
            {
                pos.Value = platForm.targetPosition;
            }
        }).Schedule(Dependency);
        Dependency.Complete();
        levelKeysCounter.Dispose();
    }
}
