using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlatformEntity : MonoBehaviour,  IConvertGameObjectToEntity
{
    public int level;
    public float3 targetPosition;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<UnlockablePlatform>(entity);
        dstManager.SetComponentData<UnlockablePlatform>(entity, new UnlockablePlatform() { level = level, targetPosition = targetPosition });



    }
}
