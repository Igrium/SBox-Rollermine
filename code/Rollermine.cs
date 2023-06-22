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
    public float BaseForce { get; set; } = 15000000;

    /// <summary>
    /// The amount of torque to use while correcting for horizontal velocity.
    /// Lower values may cause the rollermine to orbit its target, and higher
    /// values will make it beeline directly at it.
    /// </summary>
    [Property(Title = "Correction Force")]
    public float CorrectionForce { get; set; } = 30000;

    [Property(Title = "Max Range")]
    public float MaxRange { get; set; } = 1024;

    /// <summary>
    /// The amount of damage to inflict on targets.
    /// </summary>
    [Property(Title = "Damage Amount")]
    public float DamageAmount { get; set; } = 10;

    public static readonly float MAX_TORQUE_FACTOR = 5;

    public float SelfKnockbackForce { get; set; } = 80000;

    /// <summary>
    /// The amount of time to stun ourselves after an attack.
    /// </summary>
    [Property(Title = "Attack Stun Duration")]
    public float AttackStunDuration { get; set; } = 3;

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

    public static readonly float PATH_UPDATE_INTERVAL = 1f;
    public static readonly int PATH_ERROR_ALLOWANCE = 64;
    protected TimeSince TimeSinceGeneratedPath = 0;
    protected int CurrentPathSegment;
    protected NavPath? Path;

    public override void Spawn()
    {
        base.Spawn();
        SetModel("models/rollermine.vmdl");

        PhysicsEnabled = true;
        UsePhysicsCollision = true;
    }

    /// <summary>
    /// The time at which the rollermine will be revived if stunned.
    /// </summary>
    protected float ReviveTime;

    public bool IsStunned { get => Time.Now <= ReviveTime; private set { } }

    /// <summary>
    /// Stun this rollermine
    /// </summary>
    /// <param name="time">The amount of time to stun for, in seconds.</param>
    [Input]
    public void Stun(float time)
    {
        ReviveTime = Time.Now + time;
    }

    /// <summary>
    /// Force revive this rollermine
    /// </summary>
    [Input]
    public void Revive()
    {
        ReviveTime = 0;
    }

    [GameEvent.Tick.Server]
    public virtual void TickAI()
    {
        if (IsStunned)
        {
            SpikesOpen = false;
            return;
        }

        if (ShouldUpdateTarget) UpdateTarget();

        if (Target == null)
        {
            Path = null;
            return;
        };

        TickMovement(Target);

        SpikesOpen = this.Position.Distance(Target.Position) < 128;

    }

    public void TickMovement(Entity target)
    {
        if (Path == null || TimeSinceGeneratedPath >= PATH_UPDATE_INTERVAL)
        {
            GeneratePath(target);
        }

        if (Path == null)
        {
            return;
        }

        if (Path.Count <= 0) return;

        Vector3 targetLocation = target.Position;
        if (CurrentPathSegment < Path.Count)
        {
            var pathSegment = Path.Segments[CurrentPathSegment];
            targetLocation = pathSegment.Position;

            if (Position.Distance(targetLocation) <= PATH_ERROR_ALLOWANCE)
            {
                CurrentPathSegment++;
            }
        }

        MoveTowards(targetLocation);
    }

    protected void GeneratePath(Entity target)
    {
        Path = NavMesh.PathBuilder(Position)
            .WithMaxClimbDistance(4)
            .WithStepHeight(4)
            .WithMaxDistance(MaxRange * 2)
            .WithPartialPaths()
            .Build(target.Position);

        CurrentPathSegment = 0;
        TimeSinceGeneratedPath = 0;
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
        foreach (var ent in Entity.All.Where(CanTarget))
        {
            if (ent == null) continue;
            if (closest == null)
            {
                closest = ent;
                continue;
            }

            if (ent.Position.Distance(this.Position) < closest.Position.Distance(this.Position))
            {
                closest = ent;
            }
        }

        Target = closest;

        Target = closest;
    }

    protected virtual bool CanTarget(Entity? target)
    {
        return target != null && target.LifeState == LifeState.Alive && target.Tags.Has("player") && Position.Distance(target.Position) <= MaxRange;
    }

    protected virtual bool ShouldUpdateTarget => LastTargetUpdate > TargetInterval || !CanTarget(Target);

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

    protected override void OnPhysicsCollision(CollisionEventData eventData)
    {
        base.OnPhysicsCollision(eventData);
        ApplyCollisionDamage(eventData);
        if (IsStunned) return;

        var ent = eventData.Other.Entity;

        if (CanTarget(ent))
        {
            ent.TakeDamage(DamageInfo.Generic(DamageAmount).WithAttacker(this));

            Vector3 knockback = eventData.Normal.WithZ(1) * SelfKnockbackForce;
            PhysicsBody.ApplyImpulse(knockback);
            Stun(AttackStunDuration);
        } else
        {
            // Still do damage to physics props.
            if (SpikesOpen && ent is ModelEntity prop && prop.PhysicsEnabled)
            {
                prop.TakeDamage(DamageInfo.Generic(DamageAmount).WithAttacker(this));
            }
        }

    }
}