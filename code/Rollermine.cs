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
public partial class Rollermine : BasePhysics
{
    private Entity? _target;
    public TimeSince LastTargetUpdate { private set; get; }

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
        Log.Info("Spawning Melon");
        base.Spawn();
        Model = Cloud.Model("garry.beachball");
        /*SetModel("models/sbox_props/watermelon/watermelon.vmdl");*/
    }

    [GameEvent.Tick.Server]
    public virtual void ComputeAI()
    {
        if (ShouldUpdateTarget) UpdateTarget();

        if (Target == null) return;

        PhysicsBody.ApplyForce((Target.Position - this.Position).Normal * 80000);
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

}