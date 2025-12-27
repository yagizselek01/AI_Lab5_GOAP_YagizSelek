using UnityEngine;
using UnityEngine.UIElements;

public class PatrolAction : GoapActionBase
{

    public float arriveDistance = 0.5f;
    void Reset()
    {
        actionName = "Patrol (One Step)";
        cost = 2f;

        // No planner preconditions; goal selection handles “patrol only when not chasing”.
        preMask = 0;

        // This is intentionally a "one-step completion" fact.
        addMask = GoapBits.Mask(GoapFact.PatrolStepDone);
        delMask = 0;
    }

    public override void OnEnter(GoapContext ctx)
    {
        if (ctx.PatrolWaypoints == null ||
        ctx.PatrolWaypoints.Length == 0) return;
        ctx.Agent.SetDestination(ctx.PatrolWaypoints[ctx.PatrolIndex].position);
    }
    public override GoapStatus Tick(GoapContext ctx)
    {
        // If the player appears, we want to stop patrolling andreplan for the chase goal.
        if (ctx.Sensors != null && ctx.Sensors.SeesPlayer)
                return GoapStatus.Failure;
        if (ctx.PatrolWaypoints == null || ctx.PatrolWaypoints.Length == 0)
            return GoapStatus.Failure;
        if (ctx.Agent.pathPending) return GoapStatus.Running;
        if (ctx.Agent.remainingDistance <= arriveDistance)
        {
            ctx.PatrolIndex = (ctx.PatrolIndex + 1 ) % ctx.PatrolWaypoints.Length;
            return GoapStatus.Success;
        }
        return GoapStatus.Running;
    }
}
