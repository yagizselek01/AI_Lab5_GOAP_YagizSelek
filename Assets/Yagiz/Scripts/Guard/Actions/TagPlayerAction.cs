using System.Collections;
using UnityEngine;

public class TagPlayerAction : GoapActionBase
{
    private void Reset()
    {
        actionName = "Tag Player";
        cost = 1f;
        preMask = GoapBits.Mask(GoapFact.AtPlayer) | GoapBits.Mask(GoapFact.HasWeapon);
        addMask = GoapBits.Mask(GoapFact.PlayerTagged);
        delMask = 0;
    }

    public override GoapStatus Tick(GoapContext ctx)
    {
        Debug.Log("Player Tagged!");
        return GoapStatus.Success;
    }

}
