using Cosmetics.Comp;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmetics.Harmony;

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