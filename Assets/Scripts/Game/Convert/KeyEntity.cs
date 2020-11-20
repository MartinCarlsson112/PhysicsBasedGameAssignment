using Unity.Entities;
using UnityEngine;

public class KeyEntity : MonoBehaviour, IConvertGameObjectToEntity
{
    public int levelId;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<Key>(entity);
        dstManager.SetComponentData(entity, new Key() { level = levelId });

    }
}
