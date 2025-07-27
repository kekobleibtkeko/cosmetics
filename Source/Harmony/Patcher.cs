using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Transmogged;
#nullable enable

[StaticConstructorOnStartup]
public static class TransmoggedPatches
{
	static TransmoggedPatches()
	{
		var harmony = new Harmony("tsuyao.transmogged");

		List<Type> patchclasses = [
			typeof(PawnRenderTree_Adjust_Patch),
			typeof(PawnRenderTree_SetupNodes_Patch),
			typeof(StatWorker_Value_Unprime),
			typeof(Pawn_ApparelTracker_WornApparel_Patch),
			typeof(Draft_Patch),
			typeof(Translator_Unfuck),
			typeof(PawnRenderNodeWorker_ScaleFor_Patch),
			typeof(PawnRenderTree_ProcessApparel_Patch),
			typeof(ApparelGraphicRecordGetter_BodyType_Patch),
			typeof(PawnRenderNode_TR),
		];
		
		if (TransmoggedSettings.IsHARLoaded)
		{
			patchclasses.Add(typeof(Alien_ExtendedGraphicsPawnWrapper_Prime_Patch));
			patchclasses.Add(typeof(Various_TR_HAR_Patches));
		}

		patchclasses
			.Select(x => harmony.CreateClassProcessor(x))
			.Do(x => x.Patch());
	}
}

[HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.AdjustParms))]
public static class PawnRenderTree_Adjust_Patch
{
	public static void Prefix(ref PawnDrawParms parms, PawnRenderTree __instance, out Comp_Transmogged? __state)
	{
		if (!__instance.pawn.TryGetComp(out __state))
			return;

		__state!.PrimedStack++;
	}

	public static void Postfix(ref PawnDrawParms parms, Comp_Transmogged? __state)
	{
		if (__state is null)
			return;

		__state.PrimedStack--;
	}
}

[HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.SetupDynamicNodes))]
public static class PawnRenderTree_SetupNodes_Patch
{
	public static void Prefix(PawnRenderTree __instance, out Comp_Transmogged __state)
	{
		if (!__instance.pawn.TryGetComp(out __state))
			return;

		__state.PrimedStack++;
	}

	public static void Postfix(Comp_Transmogged __state)
	{
		if (__state is null)
			return;

		__state.PrimedStack--;
	}
}

[HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
public static class StatWorker_Value_Unprime
{
	public static void Prefix(out Comp_Transmogged? __state, StatRequest req, bool applyPostProcess = true)
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

	public static void Postfix(Comp_Transmogged? __state)
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
		if (!__instance.pawn.TryGetComp<Comp_Transmogged>(out var comp))
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
		if (!__instance.pawn.TryGetComp<Comp_Transmogged>(out var comp))
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

[HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.ScaleFor))]
public static class PawnRenderNodeWorker_ScaleFor_Patch
{
	public static void Postfix(PawnRenderNode node, PawnDrawParms parms, PawnRenderNodeWorker __instance, ref Vector3 __result)
	{
		if (parms.pawn is null
			|| !parms.pawn.TryGetComp<Comp_Transmogged>(out var comp)
			|| comp.GetData().State !=  TRCompState.Enabled
			|| !comp.TryGetCurrentTransmog(out var set)
			|| !set.TryGetTRApparel(node.apparel, out var tr))
			return;

		var rtr = tr!.GetTransformFor(parms.facing);
		__result = Vector3.Scale(__result, new Vector3(rtr.Scale.x, 1, rtr.Scale.y));
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

		if (!apparel.Wearer.TryGetComp<Comp_Transmogged>(out var comp)
			|| comp.GetData().State !=  TRCompState.Enabled
			|| !comp.TryGetCurrentTransmog(out var set)
			|| !set.TryGetTRApparel(apparel, out var trap)
			|| trap!.BodyDef is null)
			return;

		bodyType = trap.BodyDef;
	}
}

//ProcessApparel(Apparel ap, PawnRenderNode headApparelNode, PawnRenderNode bodyApparelNode)
[HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.ProcessApparel))]
public static class PawnRenderTree_ProcessApparel_Patch
{
	public static DrawData GetTransmoggedDrawData(Apparel apparel, Pawn pawn)
	{
		// Messages.Message($"app pawn: {Pawn}", Pawn, MessageTypeDefOf.RejectInput, historical: false);
		if (!pawn.TryGetComp<Comp_Transmogged>(out var comp) || ! comp.TryGetCurrentTransmog(out var set))
		{
			return apparel.def.apparel.drawData;
		}
		return set!.GetDrawDataFor(apparel);
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		bool patched = false;
		var insts = instructions.ToArray();

		var field_thing_def = AccessTools.Field(typeof(Thing), nameof(Thing.def));
		var field_thingdef_apparel = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.apparel));

		for (int i = 0; i < insts.Length; i++)
		{
			if (!patched
			 &&				insts[i    ].opcode		== OpCodes.Ldarg_1
			 &&				insts[i + 1].opcode		== OpCodes.Ldfld
			 && (FieldInfo)	insts[i + 1].operand	== field_thing_def
			 &&				insts[i + 2].opcode		== OpCodes.Ldfld
			 && (FieldInfo)	insts[i + 2].operand	== field_thingdef_apparel
			 &&				insts[i + 3].opcode		== OpCodes.Ldfld
			 && (FieldInfo)	insts[i + 3].operand	== AccessTools.Field(typeof(ApparelProperties), nameof(ApparelProperties.drawData)))
			 //					  i + 4 stloc.s
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderTree), nameof(PawnRenderTree.pawn)));
				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PawnRenderTree_ProcessApparel_Patch), nameof(GetTransmoggedDrawData)));
				i += 4;
				patched = true;
			}
			yield return insts[i];
		}
	}
}

[HarmonyPatch]
public static class PawnRenderNode_TR
{
	static List<PawnRenderSubWorker> InsertHandlerIfNeeded(PawnRenderNode node)
	{
		var pawn = node.tree.pawn;
		var workers = node.Props.SubWorkers;

		if (pawn is null || !pawn.TryGetComp<Comp_Transmogged>(out var comp))
			return workers;

		IEnumerable<PawnRenderSubWorker> workere = [..workers];
		if (TransmoggedSave.Instance.AutoBodyTransforms.TryGetValue(pawn.GetAutoBodyKey(), out var trs)
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
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Hair_GetTransform_Trans(IEnumerable<CodeInstruction> insts, ILGenerator generator)
	{
		var matcher = new CodeMatcher(insts, generator);

		matcher.MatchStartForward(
			new CodeMatch(OpCodes.Call, AccessTools.DeclaredPropertyGetter(typeof(PawnRenderNode), nameof(PawnRenderNode.Props)))
		)
			.ThrowIfInvalid("unable to find call to find subworkers")
			.RemoveInstruction()
			.RemoveInstruction()
			// .RemoveInstructionsWithOffsets(-1, 0)
			.InsertAndAdvance(
				CodeMatch.Call(() => InsertHandlerIfNeeded(default!))
			);

		return matcher.Instructions();
	}
}