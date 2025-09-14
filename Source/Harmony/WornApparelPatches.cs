using System.Collections.Generic;
using System.Linq;
using Cosmetics.Comp;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmetics.Harmony;

[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WornApparel), MethodType.Getter)]
public static class Pawn_ApparelTracker_WornApparel_Patch
{
	public static void Postfix(ref List<Apparel> __result, Pawn_ApparelTracker __instance)
	{	
		if (__instance?.pawn?.TryGetComp<Comp_TSCosmetics>(out var comp) != true)
			return;

		if (comp.Save.CompState != Comp_TSCosmetics.CompState.Enabled)
			return;
		
		if (!comp.IsPrimed)
			return;

		__result = comp.GetActiveApparel();
	}
}