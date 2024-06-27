using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Transmogged
{
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

	[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WornApparel), MethodType.Getter)]
	public static class Pawn_ApparelTracker_WornApparel_Patch
	{
		public static void Postfix(ref List<Apparel> __result, Pawn_ApparelTracker __instance)
		{
			if (!__instance.pawn.TryGetComp<Comp_Transmogged>(out var comp))
				return;

			if (!comp.Enabled)
				return;

			__result = comp.GetApparel().ToList();
		}
	}

	// [HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.SetupApparelNodes))]
	// public static class PawnRenderTree_Setup_Patch
	// {
	// 	public static List<Apparel>.Enumerator GetTransmoggedApparel(Pawn pawn)
	// 	{
	// 		// Messages.Message($"app pawn: {Pawn}", Pawn, MessageTypeDefOf.RejectInput, historical: false);
	// 		if (!pawn.TryGetComp<Comp_Transmogged>(out var comp))
	// 		{
	// 			return pawn.apparel.WornApparel.GetEnumerator();
	// 		}
	// 		return comp.GetApparel().ToList().GetEnumerator();
	// 	}

	// 	// [HarmonyTranspiler]
	// 	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	// 	{
	// 		bool patched = false;
	// 		var insts = instructions.ToArray();
	// 		for (int i = 0; i < insts.Length; i++)
	// 		{
	// 			if (!patched
	// 			 &&				insts[i    ].opcode		== OpCodes.Ldarg_0
	// 			 &&				insts[i + 1].opcode		== OpCodes.Ldfld
	// 			 && (FieldInfo)	insts[i + 1].operand	== AccessTools.Field(typeof(PawnRenderTree), nameof(PawnRenderTree.pawn))
	// 			 &&				insts[i + 2].opcode		== OpCodes.Ldfld
	// 			 && (FieldInfo)	insts[i + 2].operand	== AccessTools.Field(typeof(Pawn), nameof(Pawn.apparel))
	// 			 &&				insts[i + 3].opcode		== OpCodes.Callvirt
	// 			 && (MethodInfo)insts[i + 3].operand	== AccessTools.PropertyGetter(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WornApparel)))
	// 			 // 				  i + 4 get enumerator
	// 			 //					  i + 5 stloc.s
	// 			{
	// 				yield return new CodeInstruction(OpCodes.Ldarg_0);
	// 				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderTree), nameof(PawnRenderTree.pawn)));
	// 				yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PawnRenderTree_Setup_Patch), nameof(GetTransmoggedApparel)));
	// 				i += 5;
	// 				patched = true;
	// 			}
	// 			yield return insts[i];
	// 		}
	// 	}
	// }

	[HarmonyPatch(typeof(Translator), nameof(Translator.PseudoTranslated))]
	public static class Translator_Unfuck
	{
		public static bool Prefix(string original, ref string __result)
		{
			__result = original;
			return false;
		}
	}
}