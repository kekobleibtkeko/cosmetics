using AlienRace.ExtendedGraphics;
using HarmonyLib;
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
