
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
public class GoapAgent : MonoBehaviour
{
    [Header("Scene refs")]
    public GuardSensors sensors;
    public Transform player;
    public Transform weaponPickup;
    public Transform[] patrolWaypoints;
    [Header("Debug")]
    public bool logPlans = true;
    [Header("Planning")]
    [Tooltip("Minimum seconds between replans (prevents spam when facts flicker).")]
    public float minSecondsBetweenReplans = 0.20f;
    private float _nextAllowedReplanTime = 0f;
    private NavMeshAgent _agent;
    private GoapContext _ctx;
    private List<GoapActionBase> _allActions;
    private Queue<GoapActionBase> _plan;
    private GoapActionBase _currentAction;
    // “Owned” facts: memory/execution facts (e.g., HasWeapon, AtWeapon, AtPlayer, PatrolStepDone, PlayerTagged)
    // Sensor/world facts (SeesPlayer, WeaponExists) are refreshed each tick.
    private ulong _ownedFactsBits = 0;
    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _ctx = new GoapContext
        {
            Agent = _agent,
            Player = player,
            Weapon = weaponPickup,
            PatrolWaypoints = patrolWaypoints,
            Sensors = sensors,
            PatrolIndex = 0
        };
        _allActions = new
        List<GoapActionBase>(GetComponents<GoapActionBase>());
    }
    void Update()
    {
        _ownedFactsBits &= ~GoapBits.Mask(GoapFact.PatrolStepDone);

        GoapState current = BuildCurrentState();
        ulong goalMask = SelectGoalMask(current);
        // If we have no plan, request one (throttled).
        if ((_plan == null || _plan.Count == 0) && Time.time >=
        _nextAllowedReplanTime)
        {
            MakePlan(current, goalMask);
        }
        if (_plan == null || _plan.Count == 0) return;
        // Start next action if needed
        if (_currentAction == null)
        {
            _currentAction = _plan.Dequeue();
            // Procedural check at runtime (not planner-visible)
            if (!_currentAction.CheckProcedural(_ctx))
            {
                InvalidatePlan(throttle: true);
                return;
            }
            _currentAction.OnEnter(_ctx);
        }
        var status = _currentAction.Tick(_ctx);
        if (status == GoapStatus.Running) return;
        if (status == GoapStatus.Success)
        {
            // Apply effects only on success
            ApplyActionEffectsToOwnedFacts(_currentAction);
            _currentAction.OnExit(_ctx);
            _currentAction = null;
            return;
        }
        // Failure: action did not complete; invalidate and replan (throttled)
        _currentAction.OnExit(_ctx);
        _currentAction = null;
        InvalidatePlan(throttle: true);
    }
    private GoapState BuildCurrentState()
    {
        ulong bits = _ownedFactsBits;
        // Determine owned HasWeapon first (used to interpret WeaponExists as "pickup available to THIS agent")
        bool hasWeapon = (bits & GoapBits.Mask(GoapFact.HasWeapon)) != 0;
        // Sensor-driven fact (fresh each tick)
        if (sensors != null && sensors.SeesPlayer) bits |=
        GoapBits.Mask(GoapFact.SeesPlayer);
        else bits &= ~GoapBits.Mask(GoapFact.SeesPlayer);
        // World-driven fact (fresh each tick)
        // Interpret WeaponExists as "pickup available to pick up".
        // If the agent already has a weapon, treat WeaponExists as false for planning purposes.
        bool pickupActive = weaponPickup != null &&
        weaponPickup.gameObject.activeInHierarchy;
        bool weaponAvailable = pickupActive && !hasWeapon;
        if (weaponAvailable) bits |= GoapBits.Mask(GoapFact.WeaponExists);
        else bits &= ~GoapBits.Mask(GoapFact.WeaponExists);
        return new GoapState(bits);
    }
    private ulong SelectGoalMask(GoapState current)
    {
        // Simple rule set:
        // - If player seen: goal is to tag the player
        // - Else: goal is to complete one patrol step
        if (current.Has(GoapFact.SeesPlayer))
            return GoapBits.Mask(GoapFact.PlayerTagged);
        return GoapBits.Mask(GoapFact.PatrolStepDone);
    }
    private void MakePlan(GoapState current, ulong goalMask)
    {
        var res = GoapPlanner.Plan(current, goalMask, _allActions);
        if (res == null)
        {
            if (logPlans) Debug.LogWarning("GOAP: No plan found.");
            _plan = null;
            return;
        }
        _plan = new Queue<GoapActionBase>(res.Actions);
        if (logPlans)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"GOAP Plan (cost {res.TotalCost:0.0}):");
            foreach (var a in res.Actions) sb.AppendLine($"-{ a.actionName} (cost { a.cost:0.0})");
        Debug.Log(sb.ToString());
        }
    }
    private void InvalidatePlan(bool throttle)
    {
        _plan = null;
        _currentAction = null;
        if (throttle)
            _nextAllowedReplanTime = Time.time +
            minSecondsBetweenReplans;
    }
    private void ApplyActionEffectsToOwnedFacts(GoapActionBase a)
    {
        _ownedFactsBits &= ~a.delMask;
        _ownedFactsBits |= a.addMask;
    }

public string GetDebugString()
    {
        var s = BuildCurrentState();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Goal: {GoalMaskToString(SelectGoalMask(s))}");
        sb.AppendLine($"Current Action: {(_currentAction != null ?_currentAction.actionName : "(none)")}");
        sb.AppendLine("Facts:");
        foreach (GoapFact f in System.Enum.GetValues(typeof(GoapFact)))
            sb.AppendLine($"- {f}: {(s.Has(f) ? "true" : "false")}");
        sb.AppendLine("Plan:");
        if (_plan == null || _plan.Count == 0)
        {
            sb.AppendLine("- (none)");
        }
        else
        {
            foreach (var a in _plan)
                sb.AppendLine($"- {a.actionName}");
        }
        return sb.ToString();
    }
    private string GoalMaskToString(ulong goalMask)
    {
        var sb = new System.Text.StringBuilder();
        bool first = true;
        foreach (GoapFact f in System.Enum.GetValues(typeof(GoapFact)))
        {
            ulong bit = 1UL << (int)f;
            if ((goalMask & bit) != 0)
            {
                if (!first) sb.Append(", ");
                sb.Append(f);
                first = false;
            }
        }
        return first ? "(none)" : sb.ToString();
    }
}
