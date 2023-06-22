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
    public float BaseForce { get; set; } = 10000000;

    /// <summary>
    /// The amount of torque to use while correcting for horizontal velocity.
    /// Lower values may cause the rollermine to orbit its target, and higher
    /// values will make it beeline directly at it.
    /// </summary>
    [Property(Title = "Correction Force")]
    public float CorrectionForce { get; set; } = 20000;

    [Property(Title = "Max Range")]
    public float MaxRange { get; set; } = 1024;


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
    [GameEvent.Tick.Server]
    public virtual void TickAI()
    {
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


        //var targetSegment = Path.Segments[CurrentPathSegment];

        //if (Position.Distance(targetSegment.Position) <= PATH_ERROR_ALLOWANCE)
        //{
        //    CurrentPathSegment++;
        //}

        //Vector3 targetLocation;

        //if (CurrentPathSegment >= Path.Count)
        //{
        //    targetLocation = target.Position;
        //}
        //else
        //{
        //    targetLocation = Path.Segments[CurrentPathSegment].Position;
        //}

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