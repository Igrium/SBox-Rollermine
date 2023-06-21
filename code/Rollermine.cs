#nullable enable

using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollermine;

[Spawnable]
[HammerEntity]
[Library("npc_rollermine", Title = "Rollermine", Group = "NPC")]
public partial class Rollermine : AnimatedEntity
{
    private Entity? _target;
    public TimeSince LastTargetUpdate { private set; get; }

    /// <summary>
    /// The base amount of torque to apply when moving the rollermine.
    /// </summary>
    [Property(Title = "Base Force")]
    public float BaseForce { get; set; } = 9000000;

    /// <summary>
    /// The amount of torque to use while correcting for horizontal velocity.
    /// Lower values may cause the rollermine to orbit its target, and higher
    /// values will make it beeline directly at it.
    /// </summary>
    [Property(Title = "Correction Force")]
    public float CorrectionForce { get; set; } = 20000;


    public static readonly float MAX_TORQUE_FACTOR = 5;

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
    public virtual void TickAI()
    {
        if (ShouldUpdateTarget) UpdateTarget();

        if (Target == null) return;

        MoveTowards(Target.Position);

        SpikesOpen = this.Position.Distance(Target.Position) < 128;

    }

    public void TickMovement(Entity target)
    {

    }

    /// <summary>
    /// Move the rollermine towards a specific position, usually the attack target.
    /// </summary>
    /// <param name="targetPos">The target position.</param>
    public void MoveTowards(Vector3 targetPos)
    {
        targetPos = targetPos.WithZ(this.Position.z);

        Vector3 normal = (targetPos - this.Position).Normal;
        Vector2 normal2D = new Vector2(normal.x, normal.y);

        var axis = normal.RotateAround(new Vector3(0, 0, 0), Rotation.FromYaw(90));

        float torque = BaseForce * .6f;

        PhysicsBody.ApplyTorque(axis * torque);

        // determine angle to account for existing velocity
        Vector2 velocity2D = new Vector2(PhysicsBody.Velocity.x, PhysicsBody.Velocity.y);
        float angle = MeasureAngle(velocity2D, normal2D);

        float correctionMagnitude = velocity2D.Length * MathF.Sin(angle);
        PhysicsBody.ApplyTorque(normal * correctionMagnitude * CorrectionForce);

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

    /// <summary>
    /// Return the angle created by two vectors, given they start at the same point.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private float MeasureAngle(Vector2 a, Vector2 b)
    {
        float angleA = MathF.Atan2(a.y, a.x);
        float angleB = MathF.Atan2(b.y, b.x);
        return angleA - angleB;
    }
}