using Sandbox;
using Sandbox.ModelEditor.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollermine;

public partial class Rollermine
{
    /// <summary>
	/// Applies physics forces from damage info.
	/// </summary>
	protected void ApplyDamageForces(DamageInfo info)
    {
        var body = info.Body;
        if (!body.IsValid())
            body = PhysicsBody;

        if (body.IsValid() && !info.HasTag("physics_impact"))
        {
            body.ApplyImpulseAt(info.Position, info.Force * 100);
        }
    }

    public override void TakeDamage(DamageInfo info)
    {
        ApplyDamageForces(info);

        base.TakeDamage(info);
    }

    /// <summary>
	/// Returns model's prop data, or basic defaults if model specifies none.
	/// </summary>
	protected virtual ModelPropData GetModelPropData()
    {
        if (Model != null && !Model.IsError && Model.TryGetData(out ModelPropData propData))
        {
            return propData;
        }

        ModelPropData defaultData = new();
        defaultData.Health = -1;
        defaultData.ImpactDamage = 10;
        if (PhysicsGroup != null)
        {
            defaultData.ImpactDamage = PhysicsGroup.Mass / 10;
        }
        defaultData.MinImpactDamageSpeed = 500;
        return defaultData;
    }

    protected void ApplyCollisionDamage(CollisionEventData eventData)
    {
        var propData = GetModelPropData();
        if (propData == null) return;

        var minImpactSpeed = propData.MinImpactDamageSpeed;
        if (minImpactSpeed <= 0.0f) minImpactSpeed = 500;

        var impactDmg = propData.ImpactDamage;
        if (impactDmg <= 0.0f) impactDmg = 10;

        float speed = eventData.Speed;

        if (speed > minImpactSpeed)
        {
            // I take damage from high speed impacts
            if (Health > 0)
            {
                var damage = speed / minImpactSpeed * impactDmg;
                TakeDamage(DamageInfo.Generic(damage).WithTag("physics_impact"));
            }

            var other = eventData.Other;

            // Whatever I hit takes more damage
            if (other.Entity.IsValid() && other.Entity != this)
            {
                var damage = speed / minImpactSpeed * impactDmg * 1.2f;
                other.Entity.TakeDamage(DamageInfo.Generic(damage)
                    .WithTag("physics_impact")
                    .WithAttacker(this)
                    .WithPosition(eventData.Position)
                    .WithForce(eventData.This.PreVelocity));
            }
        }
    }
}