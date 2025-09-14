using Cosmetics.Comp;
using Cosmetics.Data;
using Cosmetics.Util;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Cosmetics.Harmony.Transform;

[HarmonyPatch]
public static class ApparelTransformPatches
{
	static bool TryGetCosmeticApparel(PawnRenderNode node, PawnDrawParms parms, out CosmeticApparel? ap)
	{
		ap = default;
		if (node is null || node.apparel is null)
			return false;
        if (parms.pawn is null
			|| !parms.pawn.TryGetComp<Comp_TSCosmetics>(out var comp)
			|| comp.Save.CompState != Comp_TSCosmetics.CompState.Enabled
			|| !comp.TryGetCurrentCosmeticApparel(out var set)
			|| !set.TryGetCosmeticApparelFor(node.apparel, out ap))
			return false;
		return true;
    }

	[HarmonyPostfix]
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.ScaleFor))]
    public static void ScaleFor_Postfix(PawnRenderNode node, PawnDrawParms parms, PawnRenderNodeWorker __instance, ref Vector3 __result)
    {
		if (!TryGetCosmeticApparel(node, parms, out var tr))
			return;

        var rtr = tr!.GetTransformFor(parms.facing);
        __result = Vector3.Scale(__result, new Vector3(rtr.Scale.x, 1, rtr.Scale.y) * tr.OverallScale);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.OffsetFor))]
    public static void OffsetFor_Postfix(PawnRenderNode node, PawnDrawParms parms, PawnRenderNodeWorker __instance, ref Vector3 __result)
    {
        if (!TryGetCosmeticApparel(node, parms, out var tr))
            return;

        var rtr = tr!.GetTransformFor(parms.facing);
        __result += rtr.Offset;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.RotationFor))]
    public static void RotationFor_Postfix(PawnRenderNode node, PawnDrawParms parms, PawnRenderNodeWorker __instance, ref Quaternion __result)
    {
        if (!TryGetCosmeticApparel(node, parms, out var tr))
            return;

        var rtr = tr!.GetTransformFor(parms.facing);
        __result *= Quaternion.AngleAxis(rtr.RotationOffset, Vector3.up);
    }
}