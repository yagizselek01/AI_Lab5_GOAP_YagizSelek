using UnityEngine;

public class PickUpWeaponAction : GoapActionBase
{
    void Reset()
    {
        actionName = "Pick Up Weapon";
        cost = 1f;
        preMask = GoapBits.Mask(GoapFact.AtWeapon);
        addMask = GoapBits.Mask(GoapFact.HasWeapon);
        delMask = GoapBits.Mask(GoapFact.AtWeapon);
    }

    public override bool CheckProcedural(GoapContext ctx)
    {
        return ctx.Weapon != null &&
        ctx.Weapon.gameObject.activeInHierarchy;
    }
    
    public override GoapStatus Tick(GoapContext ctx)
    {
        if (ctx.Weapon == null || !ctx.Weapon.gameObject.activeInHierarchy)
            return GoapStatus.Failure;
        ctx.Weapon.gameObject.SetActive(false);
        return GoapStatus.Success;
    }
}
