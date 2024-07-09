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
		// var racedefs = DefDatabase<ThingDef>
		// 	.AllDefsListForReading
		// 	.Where(x => x.race != null && x.race.Humanlike);
		// foreach (var race in racedefs)
		// {
		// 	race.comps.Add(new CompProperties_Transmogged());
		// 	// race.inspectorTabs
		// }

		var harmony = new Harmony("tsuyao.transmogged");
		harmony.PatchAll(typeof(TransmoggedMod).Assembly);
	}
}

[HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.AdjustParms))]
public static class PawnRenderTree_Adjust_Patch
{
	public static void Prefix(ref PawnDrawParms parms, PawnRenderTree __instance, out Comp_Transmogged __state)
	{
		if (!__instance.pawn.TryGetComp(out __state))
			return;

		__state.PrimedStack++;
	}

	public static void Postfix(ref PawnDrawParms parms, Comp_Transmogged __state)
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

		if (!comp.Enabled)
			return;
		
		if (comp.PrimedStack <= 0 || comp.UnprimedStack >= 1)
			return;

		__result = comp.GetApparel().ToList();
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
			|| !comp.Enabled
			|| !comp.TryGetCurrentTransmog(out var set)
			|| !set!.TryGetTRApparel(node.apparel, out var tr))
			return;

		var rtr = tr!.GetTransformFor(parms.facing);
		__result = Vector3.Scale(__result, new Vector3(rtr.Scale.x, 1, rtr.Scale.y));
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

	// [HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		bool patched = false;
		var insts = instructions.ToArray();
		for (int i = 0; i < insts.Length; i++)
		{
			if (!patched
			 &&				insts[i    ].opcode		== OpCodes.Ldarg_1
			 &&				insts[i + 1].opcode		== OpCodes.Ldfld
			 && (FieldInfo)	insts[i + 1].operand	== AccessTools.Field(typeof(Thing), nameof(Thing.def))
			 &&				insts[i + 2].opcode		== OpCodes.Ldfld
			 && (FieldInfo)	insts[i + 2].operand	== AccessTools.Field(typeof(ThingDef), nameof(ThingDef.apparel))
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
