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

    public static readonly float BASE_FORCE = 10000000;
    public static readonly float ADJUSTMENT_FACTOR = .3f;

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
        var axis = normal.RotateAround(new Vector3(0, 0, 0), Rotation.FromYaw(90));

        Vector3 currentDirection = PhysicsBody.Velocity.Normal;

        // A value from 1 - 0 denoting the deviation from the desired direction of the ball's velocity
        // We increase the max torque depending on how much change is needed.
        float dot = PhysicsBody.Velocity.Dot(normal);
        /*Log.Info(dot);*/
        float factor = MapRange(-300, 380, -4, 2, dot).Clamp(-1.4f, -0) * -1;
        Log.Info(factor);

        float torque = BASE_FORCE * factor;

        PhysicsBody.ApplyTorque(axis * torque);

        SpikesOpen = this.Position.Distance(Target.Position) < 192;
    }

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
}