using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Rendering;


public struct Grounded : IComponentData
{
    public bool value;
}


[BurstCompile]
public struct Impulse : IComponentData
{
    public float3 impulse;
    public float3 angularImpulse;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CollisionSystem))]
public class DynamicsSystem : SystemBase
{
    private const double minDotForGround = 0.7;

    public static void AddForce(ref Velocity vel, float3 force, float mass, float dt)
    {
        float3 a = force / mass;
        vel.value += a * dt;
    }

    public static void Jump(ref Velocity vel, float3 force, float mass, float dt)
    {
        vel.value = new float3(vel.value.x, 0, vel.value.z);
        AddForce(ref vel, force, mass, dt);

    }

    public static quaternion Mul1f(quaternion q, float v )
    {
        return q.value *= v;
    }

    public static quaternion Add(quaternion q0, quaternion q1)
    {
        return q0.value + q1.value;
    }

    public static quaternion IntegrateRotation(quaternion q0, float3 w, float dt)
    {
        float halfDt = 0.5f * dt;

        var q0HalfDt = Mul1f(q0, halfDt);
        var newAngVel = new quaternion(w.x, w.y, w.z, 0);
        var mulWq0HalfDt = math.mul(q0HalfDt, newAngVel);
        return math.normalize(Add(q0, mulWq0HalfDt));
    }

    protected override void OnUpdate()
    {
        float dt = Time.DeltaTime;
        float e = 0.2f;
        const int jumpForce = 1250;


        Dependency = Entities.ForEach((ref Impulse impulse, ref Dynamic dyn, in Velocity vel) => {
            impulse.impulse = float3.zero;
            impulse.angularImpulse = float3.zero;
            
            if(vel.value.y > 0.1f || vel.value.y < -0.1f)
            {
                dyn.grounded = false;
            }
        }).Schedule(Dependency);

        Dependency.Complete();

        Dependency = Entities.WithBurst().ForEach((ref Velocity vel, in Gravity gravity, in Mass mass) =>
        {
            AddForce(ref vel, mass.value * gravity.value, mass.value, dt);
        }).Schedule(Dependency);

        Dependency.Complete();


        //Impulse Collision Response
        Dependency = Entities.ForEach((ref DynamicBuffer<CollisionResult> collisions, ref Impulse impulse, ref Dynamic dyn,
            in Velocity vA, in AngularVelocity vAng, in Translation pos, in Mass mA, in PhysicsBody bA) =>
        {
            for (int i = 0; i < collisions.Length; i++)
            {
                if(collisions[i].isTrigger)
                {
                    continue;
                }
                //get information about collision partner
                var vB = float3.zero;
                var mB = GetComponent<Mass>(collisions[i].other);
                var bB = GetComponent<PhysicsBody>(collisions[i].other);
                var bPos = GetComponent<Translation>(collisions[i].other);
                if (HasComponent<Velocity>(collisions[i].other))
                {
                    vB = GetComponent<Velocity>(collisions[i].other).value;
                }
                float3 vAngB = float3.zero;
                if (HasComponent<AngularVelocity>(collisions[i].other))
                {
                    vAngB = GetComponent<AngularVelocity>(collisions[i].other).value;
                }



                float penDepth = collisions[i].contactData.depth;
                float3 n = collisions[i].contactData.normal;
                float3 rA = collisions[i].contactData.worldSpacePointA - pos.Value;
                float3 rB = collisions[i].contactData.worldSpacePointB - bPos.Value;
                if (math.dot(n, new float3(0, 1, 0)) > minDotForGround)
                {
                    dyn.grounded = true;
                }

                float3 v2 = vB;
                float3 w2 = vAngB;
                float3 v1 = vA.value;
                float3 w1 = vAng.value;

                float3 deltaV = v2 + math.cross(w2, rB) - (v1 + math.cross(w1, rA));
                float deltaVDotN = math.dot(deltaV, n);

                float jV = deltaVDotN;
                float SLOP = 0.01f;
                float beta = 0.02f;

                float biasPenetrationDepth = 0.0f;
                if (penDepth > SLOP)
                {
                    biasPenetrationDepth = -(beta / dt) *
                       math.max(0.0f, penDepth - SLOP);
                }
                float b = biasPenetrationDepth;
                float deltaLambda = -(jV + b) * 1.0f;


                float3 linearImpulse = n * deltaLambda;
                impulse.impulse -= (1 + e) * (1.0f / mA.value) * linearImpulse;

                var iAMulRACrossN = bA.inertiaTensorInv * math.cross(n, rA);
               // impulse.angularImpulse += iAMulRACrossN * deltaLambda;
            }

        }).Schedule(Dependency);
        Dependency.Complete();

        //Correct position of the object by moving them by the normal * the depth of the collision
        Dependency = Entities.WithBurst().ForEach((ref DynamicBuffer<CollisionResult> collisions, ref Translation pos, in Mass mass, in Dynamic dyn) =>
        {
            float3 correctionAccu = float3.zero;
            for (int i = 0; i < collisions.Length; i++)
            {
                if (collisions[i].isTrigger)
                {
                    continue;
                }
                correctionAccu += collisions[i].contactData.normal * collisions[i].contactData.depth;
            }
            pos.Value += correctionAccu;
        }).Schedule(Dependency);

        //custom jumping code... 
        Dependency = Entities.WithBurst().ForEach((ref Velocity vel, ref Dynamic dyn, in Mass mass) =>
        {
            if(dyn.jumped)
            {
                Jump(ref vel, new float3(0, jumpForce, 0), mass.value, dt);
                dyn.jumped = false;
            }
            
        }).Schedule(Dependency);


        //integrate impulse and angular impulse.
        Dependency = Entities.WithBurst().ForEach((ref Velocity vel, ref AngularVelocity angVel, in Impulse impulse, in Mass mass, in PhysicsBody body) =>
        {
            vel.value += impulse.impulse;
            angVel.value += impulse.angularImpulse;
        }).Schedule(Dependency);

        //Integrate velocities.
        Dependency = Entities.WithBurst().ForEach((ref Translation pos, ref Rotation rot, in Velocity vel, in AngularVelocity angVel) =>
        {
            pos.Value += vel.value * dt;
            rot.Value = IntegrateRotation(rot.Value, angVel.value, dt);
        }).Schedule(Dependency);
    }
}
