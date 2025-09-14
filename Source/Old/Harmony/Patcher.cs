using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cosmetics;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Transmogged;
#nullable enable

//#define HAR

[StaticConstructorOnStartup]
public static class TransmoggedPatches
{
	static TransmoggedPatches()
	{
		var harmony = new Harmony(CosmeticsMod.ModID);

		List<Type> patchclasses = [
			typeof(PawnRenderTree_Adjust_Patch),
			typeof(PawnRenderTree_SetupNodes_Patch),
			typeof(StatWorker_Value_Unprime),
			typeof(Pawn_ApparelTracker_WornApparel_Patch),
			typeof(Draft_Patch),
			typeof(Translator_Unfuck),
			typeof(PawnRenderNodeTransform_Patch),
			typeof(ApparelGraphicRecordGetter_BodyType_Patch),
			typeof(PawnRenderNode_TR),
		];
		
		if (TransmoggedSettings.IsHARLoaded)
		{
#if HAR
			patchclasses.Add(typeof(Alien_ExtendedGraphicsPawnWrapper_Prime_Patch));
			patchclasses.Add(typeof(Various_TR_HAR_Patches));
#endif
		}

		patchclasses
			.Select(x => harmony.CreateClassProcessor(x))
			.Do(x => x.Patch());
	}
}

[HarmonyPatch(typeof(CompStatue), nameof(CompStatue.CreateSnapshotOfPawn))]
public static class CompStatue_Prime_Patch
{
	public static void Prefix(Pawn p, out Comp_TSCosmetics? __state)
	{
        if (!p.TryGetComp(out __state))
            return;

        __state!.PrimedStack++;
    }

	public static void Postfix(Pawn p, Comp_TSCosmetics? __state)
	{
        if (__state is null)
            return;

        __state.PrimedStack--;
    }
}

[HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.AdjustParms))]
public static class PawnRenderTree_Adjust_Patch
{
	public static void Prefix(ref PawnDrawParms parms, PawnRenderTree __instance, out Comp_TSCosmetics? __state)
	{
		if (!__instance.pawn.TryGetComp(out __state))
			return;

		__state!.PrimedStack++;
	}

	public static void Postfix(ref PawnDrawParms parms, Comp_TSCosmetics? __state)
	{
		if (__state is null)
			return;

		__state.PrimedStack--;
	}
}

[HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.SetupDynamicNodes))]
public static class PawnRenderTree_SetupNodes_Patch
{
	public static void Prefix(PawnRenderTree __instance, out Comp_TSCosmetics __state)
	{
		if (!__instance.pawn.TryGetComp(out __state))
			return;

		__state.PrimedStack++;
	}

	public static void Postfix(Comp_TSCosmetics __state)
	{
		if (__state is null)
			return;

		__state.PrimedStack--;
	}
}

[HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
public static class StatWorker_Value_Unprime
{
	public static void Prefix(out Comp_TSCosmetics? __state, StatRequest req, bool applyPostProcess = true)
	{
		if (req.Pawn is null)
		{
			__state = null;
			return;
		}

		if (!req.Pawn.TryGetComp(out __state))
		{
			return;
		}

		__state!.UnprimedStack++;
	}

	public static void Postfix(Comp_TSCosmetics? __state)
	{
		if (__state is null)
			return;

		__state.UnprimedStack--;
	}
}

[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WornApparel), MethodType.Getter)]
public static class Pawn_ApparelTracker_WornApparel_Patch
{
	public static void Postfix(ref List<Apparel> __result, Pawn_ApparelTracker __instance)
	{	
		if (!__instance.pawn.TryGetComp<Comp_TSCosmetics>(out var comp))
			return;

		if (comp.GetData().State !=  TRCompState.Enabled)
			return;
		
		if (!comp.IsPrimed())
			return;

		__result = comp.GetActiveApparel();
	}
}

[HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
public static class Draft_Patch
{
	public static void Postfix(Pawn_DraftController __instance)
	{
		if (!__instance.pawn.TryGetComp<Comp_TSCosmetics>(out var comp))
			return;

		comp.NotifyUpdate();
	}
}

[HarmonyPatch(typeof(Translator), nameof(Translator.PseudoTranslated))]
public static class Translator_Unfuck
{
	public static bool Prefix(string original, ref string __result)
	{
		__result = original;
		return false;
	}
}

[HarmonyPatch]
public static class PawnRenderNodeTransform_Patch
{
	static bool TryGetTRApparel(PawnRenderNode node, PawnDrawParms parms, out TRApparel? ap)
	{
		ap = default;
        if (parms.pawn is null
			|| !parms.pawn.TryGetComp<Comp_TSCosmetics>(out var comp)
			|| comp.GetData().State != TRCompState.Enabled
			|| !comp.TryGetCurrentTransmog(out var set)
			|| !set.TryGetTRApparel(node.apparel, out ap))
				return false;
		return true;
    }

	[HarmonyPostfix]
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.ScaleFor))]
    public static void ScaleFor_Postfix(PawnRenderNode node, PawnDrawParms parms, PawnRenderNodeWorker __instance, ref Vector3 __result)
    {
		if (!TryGetTRApparel(node, parms, out var tr))
			return;

        var rtr = tr!.GetTransformFor(parms.facing);
        __result = Vector3.Scale(__result, new Vector3(rtr.Scale.x, 1, rtr.Scale.y));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.OffsetFor))]
    public static void OffsetFor_Postfix(PawnRenderNode node, PawnDrawParms parms, PawnRenderNodeWorker __instance, ref Vector3 __result)
    {
        if (!TryGetTRApparel(node, parms, out var tr))
            return;

        var rtr = tr!.GetTransformFor(parms.facing);
        __result = __result + rtr.Offset;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.RotationFor))]
    public static void RotationFor_Postfix(PawnRenderNode node, PawnDrawParms parms, PawnRenderNodeWorker __instance, ref Quaternion __result)
    {
        if (!TryGetTRApparel(node, parms, out var tr))
            return;

        var rtr = tr!.GetTransformFor(parms.facing);
        __result = __result * Quaternion.AngleAxis(rtr.RotationOffset, Vector3.up);
    }
}


[HarmonyPatch(typeof(ApparelGraphicRecordGetter), nameof(ApparelGraphicRecordGetter.TryGetGraphicApparel))]
public static class ApparelGraphicRecordGetter_BodyType_Patch
{
	public static void Prefix(Apparel apparel, ref BodyTypeDef bodyType, out ApparelGraphicRecord rec)
	{
		rec = default;
		if (apparel is null || apparel.Wearer is null)
			return;

		if (!apparel.Wearer.TryGetComp<Comp_TSCosmetics>(out var comp)
			|| comp.GetData().State !=  TRCompState.Enabled
			|| !comp.TryGetCurrentTransmog(out var set)
			|| !set.TryGetTRApparel(apparel, out var trap)
			|| trap!.BodyDef is null)
			return;

		bodyType = trap.BodyDef;
	}
}

[HarmonyPatch]
public static class PawnRenderNode_TR
{
	static List<PawnRenderSubWorker> InsertHandlerIfNeeded(PawnRenderNode node)
	{
		var pawn = node.tree.pawn;
		var workers = node.Props.SubWorkers;

		if (pawn is null || !pawn.TryGetComp<Comp_TSCosmetics>(out var comp))
			return workers;

		IEnumerable<PawnRenderSubWorker> workere = [..workers];
		if (CosmeticsSave.Instance.AutoBodyTransforms.TryGetValue(pawn.GetAutoBodyKey(), out var trs)
			&& trs.GetWorkerFor(node) is PawnRenderSubWorker autoworker)
		{
			workere = [..workere, autoworker];
		}

		if (comp.GetData().State != TRCompState.Disabled
			&& comp.GetWorkerFor(node) is PawnRenderSubWorker compworker)
		{
			workere = [..workere, compworker];
		}
		//nameof(Map.Biome)
		return [..workere];
    }

    [HarmonyPatch(typeof(PawnRenderNode), nameof(PawnRenderNode.GetTransform))]
    public static void Postfix(PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot, ref Quaternion rotation, ref Vector3 scale, PawnRenderNode __instance)
	{
        var pawn = __instance.tree.pawn;

        if (pawn is null || !pawn.TryGetComp<Comp_TSCosmetics>(out var comp))
            return;

        if (CosmeticsSave.Instance.AutoBodyTransforms.TryGetValue(pawn.GetAutoBodyKey(), out var trs)
            && trs.GetWorkerFor(__instance) is PawnRenderSubWorker autoworker)
        {
            autoworker.TransformOffset(__instance, parms, ref offset, ref pivot);
            autoworker.TransformRotation(__instance, parms, ref rotation);
            autoworker.TransformScale(__instance, parms, ref scale);
        }

        if (comp.GetData().State != TRCompState.Disabled
            && comp.GetWorkerFor(__instance) is PawnRenderSubWorker compworker)
        {
            compworker.TransformOffset(__instance, parms, ref offset, ref pivot);
            compworker.TransformRotation(__instance, parms, ref rotation);
            compworker.TransformScale(__instance, parms, ref scale);
        }
    }

 //   [HarmonyPatch(typeof(PawnRenderNode), nameof(PawnRenderNode.GetTransform))]
	//[HarmonyTranspiler]
	//public static IEnumerable<CodeInstruction> Hair_GetTransform_Trans(IEnumerable<CodeInstruction> insts, ILGenerator generator)
	//{
	//	var matcher = new CodeMatcher(insts, generator);

	//	matcher.MatchStartForward(
	//		new CodeMatch(OpCodes.Call, AccessTools.DeclaredPropertyGetter(typeof(PawnRenderNode), nameof(PawnRenderNode.Props)))
	//	)
	//		.ThrowIfInvalid("unable to find call to find subworkers")
	//		.RemoveInstruction()
	//		.RemoveInstruction()
	//		// .RemoveInstructionsWithOffsets(-1, 0)
	//		.InsertAndAdvance(
	//			CodeMatch.Call(() => InsertHandlerIfNeeded(default!))
	//		);

	//	return matcher.Instructions();
	//}
}