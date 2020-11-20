using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
public class Bootstrap : MonoBehaviour
{
    public Mesh mesh;
    public Material mat;

    public Mesh sphere;
    public Material sphereMat;
    public Material sphereMat2;

    public static Mesh staticMesh;
    public static Material staticMat;

    public static Mesh staticSphere;
    public static Material staticSphereMat;
    public static Material staticSphereMat2;

    public static Entity playerEntity;

    private static float3 randomPositionInSphere(float radius, float3 center)
    {
        return new float3(UnityEngine.Random.Range(-radius, radius), UnityEngine.Random.Range(-radius, radius), UnityEngine.Random.Range(-radius, radius));
    }

    private static quaternion randomRotation()
    {
        return quaternion.AxisAngle(math.normalize(randomPositionInSphere(1.0f, float3.zero)), UnityEngine.Random.Range(0, 3.14f));
    }


    public static float3 CalculateSphereInverseInertiaTensor(float radius, float mass)
    {
        float diag = 0.4f * mass * radius * radius;

        float3 localInertiaTensor = new float3(diag, diag, diag);

        float3 inverseInertiaTensorLocal = new float3(localInertiaTensor.x != 0.5f ? 1.0f / localInertiaTensor.x : 0,
                                          localInertiaTensor.y != 0.0f ? 1.0f / localInertiaTensor.y : 0,
                                          localInertiaTensor.z != 0.0f ? 1.0f / localInertiaTensor.z : 0);


        return inverseInertiaTensorLocal;
        //float dSquare = iZ* iZ;
        //float hSquare = iY * iY;
        //float wSquare = iX * iX;
        //float constant = 1.0f / 12.0f;
        //var iAInverse = math.inverse(new float3x3(
        //    new float3(constant * mass * (wSquare * dSquare), 0, 0), 
        //    new float3(0, constant * mass * (dSquare * hSquare), 0), 
        //    new float3(0, 0, constant * mass * (wSquare * hSquare))));
        //return iAInverse;
    }


    public static float3 CalculateBoxInverseInertaTensor(in float iX, in float iY, in float iZ, float mass)
    {
        float factor = (1.0f / 3.0f) * mass;
        float xSquare = iX * iX;
        float ySquare = iY * iY;
        float zSquare = iZ * iZ;
        float3 localInertiaTensor = new float3(factor * (ySquare + zSquare), factor * (xSquare + zSquare), factor * (xSquare + ySquare));
        float3 inverseInertiaTensorLocal = new float3(localInertiaTensor.x != 0.5f ? 1.0f / localInertiaTensor.x : 0,
                                         localInertiaTensor.y != 0.0f ? 1.0f / localInertiaTensor.y : 0,
                                         localInertiaTensor.z != 0.0f ? 1.0f / localInertiaTensor.z : 0);

        return inverseInertiaTensorLocal;
        //var iAInverse = math.inverse(new float3x3(new float3(1.4f * iX * iX, 0, 0), new float3(0, 1.4f * iX * iX, 0), new float3(0, 0, 1.4f * iX * iX)));
        //return iAInverse;
    }

    private void Start()
    {
        staticSphere = sphere;
        staticSphereMat = sphereMat;
        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;

        var player = em.CreateArchetype(typeof(LocalToWorld),
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
            typeof(ShootingComponent),
            typeof(RenderMesh),
            typeof(Player));

        playerEntity = em.CreateEntity(player);
        em.SetComponentData(playerEntity, ColliderHelper.MakeSphereCollider(0.5f, false));
        em.SetComponentData(playerEntity, new AngularVelocity() { value = float3.zero });
        em.SetComponentData(playerEntity, new Rotation() { Value = quaternion.identity });
        em.SetComponentData(playerEntity, new Scale() { Value = 1 });
        em.SetComponentData(playerEntity, new Translation() { Value = new float3(0, 10.5f, 0) });
        em.SetComponentData(playerEntity, new PhysicsBody() { elasticity = 0.9f, inertiaTensorInv = CalculateBoxInverseInertaTensor(1, 1, 1, 1) });
        em.SetComponentData(playerEntity, new Mass() { value = 1.0f });
        em.SetComponentData(playerEntity, new Gravity() { value = new float3(0, -9.81f, 0) });
        em.SetComponentData(playerEntity, new Velocity { value = new float3(0, -4.0f, 0) });
        em.SetComponentData(playerEntity, new Impulse { impulse = float3.zero });
        em.SetComponentData(playerEntity, new ShootingComponent() { Accu = 0, aimDirection = quaternion.identity, Cooldown = 1.0f });

        em.AddBuffer<CollisionResult>(playerEntity);
        em.SetSharedComponentData(playerEntity, new RenderMesh { mesh = mesh, material = sphereMat });
    }
}