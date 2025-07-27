using System.Collections.Generic;
using System.Reflection.Emit;
using AlienRace;
using AlienRace.ApparelGraphics;
using AlienRace.ExtendedGraphics;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Transmogged;
#nullable enable

[HarmonyPatch(typeof(ExtendedGraphicsPawnWrapper), nameof(ExtendedGraphicsPawnWrapper.GetWornApparelProps))]
public static class Alien_ExtendedGraphicsPawnWrapper_Prime_Patch
{
	public static void Prefix(ExtendedGraphicsPawnWrapper __instance, out Comp_Transmogged? __state)
	{
		if (!__instance.WrappedPawn.TryGetComp(out __state))
			return;

		__state!.PrimedStack++;
	}
	public static void Postfix(Comp_Transmogged? __state)
	{
		if (__state is null)
			return;

		__state.PrimedStack--;
	}
}


[HarmonyPatch]
public static class Various_TR_HAR_Patches
{
	public static ThingDef? GetModifiedDef(Apparel? apparel)
	{
		var pawn = apparel?.Wearer;
		if (apparel is null || pawn is null)
		{
			return null;
		}

		if (!pawn.TryGetComp<Comp_Transmogged>(out var comp)
			|| comp.GetData().State !=  TRCompState.Enabled
			|| !comp.TryGetCurrentTransmog(out var set)
			|| !set.TryGetTRApparel(apparel, out var trap))
		{
			return pawn.def;
		}

		return trap?.RaceDef ?? pawn.def;
	}

	[HarmonyPatch(typeof(ApparelGraphicUtility), nameof(ApparelGraphicUtility.GetPath))]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> HAR_ChangeRace_Trans(IEnumerable<CodeInstruction> insts, ILGenerator generator)
	{
		var matcher = new CodeMatcher(insts, generator);

		matcher.MatchStartForward(
			new CodeMatch(OpCodes.Stloc_2)
		)
			.ThrowIfInvalid("unable to find stloc2 ???")
			.Advance(1)
			.InsertAndAdvance(
				new CodeInstruction(OpCodes.Ldarg_1),
				CodeMatch.Call(() => GetModifiedDef(default!)),
				new CodeInstruction(OpCodes.Stloc_1)
			);

		return matcher.Instructions();
	}
}