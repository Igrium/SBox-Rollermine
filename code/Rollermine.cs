#nullable enable

using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollermine;

/*[Spawnable]*/
[Library("npc_rollermine")]
public partial class Rollermine : AnimatedEntity
{
    private Entity? _target;
    public TimeSince LastTargetUpdate { private set; get; }

    public static readonly float BASE_FORCE = 9000000;
    public static readonly float CORRECTION_FORCE = 20000;

    public static readonly float MAX_TORQUE_FACTOR = 5;

    private float ForwardSpeed = -1200000;

    // When it was last charged
    private float ChargeTime = Time.Now;

    public bool SpikesOpen
    {
        get => GetAnimParameterBool("b_open");
        set => SetAnimParameter("b_open", value);
    }

    public Entity? Target
    {
        get { return _target; }
        set
        {
            _target = value;
            LastTargetUpdate = 0;
        }
    }

    public float TargetInterval { get; set; } = 5;

    public override void Spawn()
    {
        base.Spawn();
        SetModel("models/rollermine.vmdl");

        PhysicsEnabled = true;
        UsePhysicsCollision = true;
    }
    [GameEvent.Tick.Server]
    public virtual void ComputeAI()
    {
        if (ShouldUpdateTarget) UpdateTarget();

        if (Target == null) return;

        Vector3 targetPos = Target.Position.WithZ(this.Position.z);
        /*Rotation targetRot = new Rotation((targetPos - this.Position).Normal, 180);*/

        Vector3 normal = (targetPos - this.Position).Normal;
        Vector2 normal2D = new Vector2(normal.x, normal.y);

        //Log.Info(correctionMagnitude);

        //Log.Info(currentVelocityLocal.y);

        var axis = normal.RotateAround(new Vector3(0, 0, 0), Rotation.FromYaw(90));

        Vector3 currentDirection = PhysicsBody.Velocity.Normal;

        // A value from 1 - 0 denoting the deviation from the desired direction of the ball's velocity
        // We increase the max torque depending on how much change is needed.
        float dot = PhysicsBody.Velocity.Dot(normal);

        float factor = MapRange(-300, 380, -4, 2, dot).Clamp(-1.2f, -0) * -1;
        //float torque = BASE_FORCE * factor;
        float torque = BASE_FORCE * .6f;

        PhysicsBody.ApplyTorque(axis * torque);

        // determine angle to account for existing velocity
        Vector2 velocity2D = new Vector2(PhysicsBody.Velocity.x, PhysicsBody.Velocity.y);
        float angle = MeasureAngle(velocity2D, normal2D);

        float correctionMagnitude = velocity2D.Length * MathF.Sin(angle);

        PhysicsBody.ApplyTorque(normal * correctionMagnitude * CORRECTION_FORCE);



        SpikesOpen = this.Position.Distance(Target.Position) < 128;
    }

    ////[GameEvent.Tick.Server]
    //public virtual void ComputeAI()
    //{
    //    if (ShouldUpdateTarget) UpdateTarget();
    //    if (Target == null) return;

    //    Vector3 targetPosition = Target.Position;

    //    Vector3 vecToTarget = targetPosition - this.Position;

    //    float yaw = Util.VecToYaw(vecToTarget);
    //    var vecRight = vecToTarget.Normal.RotateAround(new Vector3(0, 0, 0), Rotation.FromYaw(90));

    //    Vector3 vecVelocity = PhysicsBody.Velocity.Normal;
    //    vecToTarget = vecToTarget.Normal;

    //    var flDot = vecVelocity.Dot(vecToTarget);
    //    var flTorqueFactor = 1 + (Time.Now - ChargeTime) * 2;
    //    flTorqueFactor = 1 * 2;

    //    if ( flTorqueFactor < 1 )
    //    {
    //        flTorqueFactor = 1;
    //    }
    //    else if ( flTorqueFactor > MAX_TORQUE_FACTOR )
    //    {
    //        flTorqueFactor = MAX_TORQUE_FACTOR;
    //    }

    //    Vector3 vecCompensate = new Vector3
    //    (
    //        vecVelocity.y,
    //        -vecVelocity.x,
    //        0
    //    ).Normal;

    //    Log.Info(vecCompensate);

    //    PhysicsBody.ApplyTorque((vecRight + vecCompensate) * ForwardSpeed * flTorqueFactor);
        
    //}

    private void UpdateTarget()
    {
        Log.Trace("Updating rollermine target");

        Entity? closest = null;
        foreach (var ent in Game.Clients.Where(c => c.Pawn is Entity).Select(c => c.Pawn as Entity))
        {
            if (ent == null) continue;
            if (closest == null)
            {
                closest = ent;
                continue;
            }

            if (ent.LifeState == LifeState.Alive && ent.Position.DistanceSquared(this.Position) < closest.Position.DistanceSquared(this.Position))
            {
                closest = ent;
            }

        }

        Target = closest;
    }

    protected virtual bool ShouldUpdateTarget => LastTargetUpdate > TargetInterval || Target?.LifeState != LifeState.Alive;

    private float MapRange(float min1, float max1, float min2, float max2, float value) => (value - min1) * (max2 - min2) / (max1 - min1) + min2;
    
    /// <summary>
    /// Rotate a 2D vector around the origin
    /// </summary>
    /// <param name="src">Source vector</param>
    /// <param name="angle">Amount to rotate it, in radians</param>
    /// <returns>Rotated vector</returns>
    private Vector2 Rotate2D(Vector2 src, float angle)
    {
        float x2 = src.x * MathF.Cos(angle) - src.y * MathF.Sin(angle);
        float y2 = src.y * MathF.Cos(angle) + src.x * MathF.Sin(angle);
        return new Vector2(x2, y2);
    }

    private float MeasureAngle(Vector2 a, Vector2 b)
    {
        float angleA = MathF.Atan2(a.y, a.x);
        float angleB = MathF.Atan2(b.y, b.x);
        return angleA - angleB;
    }
}