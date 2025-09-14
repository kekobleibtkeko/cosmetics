using System.Collections.Generic;
using Cosmetics.Comp;
using Cosmetics.Mod;
using Cosmetics.Util;
using HarmonyLib;
using TS_Lib.Transforms;
using UnityEngine;
using Verse;

namespace Cosmetics.Harmony.Transform;


[HarmonyPatch]
public static class BodyTransformPatches
{
    [HarmonyPatch(typeof(PawnRenderNode), nameof(PawnRenderNode.GetTransform))]
    public static void Postfix(PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot, ref Quaternion rotation, ref Vector3 scale, PawnRenderNode __instance)
	{
        var pawn = __instance.tree.pawn;

        if (pawn is null || !pawn.TryGetComp<Comp_TSCosmetics>(out var comp))
            return;

        if (comp.Save.CompState != Comp_TSCosmetics.CompState.Disabled
			&& CosmeticsSave.Instance.AutoBodyTransforms.TryGetValue(pawn.GetAutoBodyKey(), out var trs)
            && trs.GetTransformFor(__instance) is TSTransform4 auto_trs)
        {
			var tr = auto_trs.ForRot(parms.facing);
            tr.TransformOffset(ref offset);
            tr.TransformRotation(ref rotation);
            tr.TransformScale(ref scale);
        }

		if (comp.Save.CompState == Comp_TSCosmetics.CompState.Enabled
			&& comp.Save.BodyTransforms.GetTransformFor(__instance) is TSTransform4 comp_trs)
		{
			var tr = comp_trs.ForRot(parms.facing);
			tr.TransformOffset(ref offset);
			tr.TransformRotation(ref rotation);
			tr.TransformScale(ref scale);
        }
    }
}