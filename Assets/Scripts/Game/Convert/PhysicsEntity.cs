using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

public class PhysicsEntity : MonoBehaviour, IConvertGameObjectToEntity
{ 
    public bool dynamic = false;
    public bool useGravity = false;
    public bool trigger = false;

    //Clamp between elipsilon to 10000
    public float mass = 1.0f;
    public float elasticity = 0.5f;
    public Vector3 gravityAcceleration = new Vector3(0, -9.81f, 0);
    public Vector3 initialLinearVelocity = Vector3.zero;
    public Vector3 initialAngularVelocity = Vector3.zero;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<PhysicsBody>(entity);
        if (dynamic)
        {
            dstManager.AddComponent<Dynamic>(entity);
        }
        if(useGravity)
        {
            dstManager.AddComponent<Gravity>(entity);
            dstManager.SetComponentData(entity, new Gravity() { value = gravityAcceleration });
        }
        var sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider)
        {
            dstManager.AddComponent<ColliderComponent>(entity);
            dstManager.SetComponentData(entity, ColliderHelper.MakeSphereCollider(sphereCollider.radius, trigger));
            dstManager.SetComponentData(entity, new PhysicsBody() { elasticity = elasticity, inertiaTensorInv = Bootstrap.CalculateBoxInverseInertaTensor(sphereCollider.radius, sphereCollider.radius, sphereCollider.radius, mass) });
        }

        var boxCollider = GetComponent<BoxCollider>();
        if (boxCollider)
        {
            dstManager.AddComponent<ColliderComponent>(entity);

            var box = new Vector3(transform.localScale.x * boxCollider.size.x * 0.5f, transform.localScale.y * boxCollider.size.y * 0.5f, transform.localScale.z * boxCollider.size.z * 0.5f);

            dstManager.SetComponentData(entity, ColliderHelper.MakeBoxCollider(box, trigger));
            dstManager.SetComponentData(entity, new PhysicsBody() { elasticity = elasticity, inertiaTensorInv = Bootstrap.CalculateBoxInverseInertaTensor(box.x, box.y, box.z, mass) });
        }
        if(!sphereCollider && !boxCollider)
        {
            //todo: Warning
        }

        dstManager.AddComponent<AngularVelocity>(entity); 
        dstManager.SetComponentData(entity, new AngularVelocity() { value = initialAngularVelocity });
        dstManager.AddComponent<Velocity>(entity);
        dstManager.SetComponentData(entity, new Velocity() { value = initialLinearVelocity });

        dstManager.AddComponent<Impulse>(entity);
        dstManager.SetComponentData(entity, new Impulse() { angularImpulse = float3.zero, impulse = float3.zero });

        dstManager.AddComponent<Mass>(entity);
        dstManager.SetComponentData(entity, new Mass() { value = mass });

        dstManager.AddBuffer<CollisionResult>(entity);
   
    }
}
