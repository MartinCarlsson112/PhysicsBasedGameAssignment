using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
public struct PlayerInput
{
    public float2 axis;
}
public class PlayerMovementSystem : SystemBase
{

    protected override void OnUpdate()
    {
        float3 maxPlayerSpeed = new float3(10, 0, 10);
        float dt = Time.DeltaTime;
        bool jump = false;
        PlayerInput pi = new PlayerInput();
        if (Input.GetKey(KeyCode.W))
        {
            pi.axis.y += 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            pi.axis.y += -1;
        }

        if (Input.GetKey(KeyCode.A))
        {
            pi.axis.x += -1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            pi.axis.x += 1;
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            jump = true;
            UnityEngine.Debug.Log("pressed jump!");
        }

        Dependency = Entities.ForEach((ref Velocity vel,ref Dynamic dynamic, in Player player, in Rotation rot, in Mass mass ) => {

            vel.value.x = math.clamp(vel.value.x, -maxPlayerSpeed.x, maxPlayerSpeed.x);
            vel.value.z = math.clamp(vel.value.z, -maxPlayerSpeed.z, maxPlayerSpeed.z);


            float3 force = new float3();
            if (pi.axis.x != 0)
            {
                force += math.mul(rot.Value, new float3(pi.axis.x, 0, 0));
            }
            if(pi.axis.y != 0)
            {
                force += math.mul(rot.Value, new float3(0, 0, pi.axis.y));
            }
            if (!math.all(force == float3.zero))
            {
                DynamicsSystem.AddForce(ref vel, force * 100.0f, mass.value, dt);
            }

            DynamicsSystem.AddForce(ref vel, -new float3(vel.value.x, 0, vel.value.z) * 10.0f, mass.value, dt);


            if (jump && dynamic.grounded)
            {
                dynamic.jumped = true;
            }
        }).Schedule(Dependency);
        Dependency.Complete();
    }
}
