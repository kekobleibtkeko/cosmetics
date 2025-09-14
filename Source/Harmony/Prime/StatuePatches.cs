using Cosmetics.Comp;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmetics.Harmony.Prime;

[HarmonyPatch(typeof(CompStatue), nameof(CompStatue.CreateSnapshotOfPawn))]
public static class CompStatue_Prime_Patch
{
	private const string STACK_VAL = "compstatue";
	public static void Prefix(Pawn p, out Comp_TSCosmetics? __state)
	{
		if (!p.TryGetComp(out __state))
			return;

		__state!.PushToStack(STACK_VAL);
	}

	public static void Postfix(Pawn p, Comp_TSCosmetics? __state)
	{
		if (__state is null)
			return;

		__state.PopFromStack(STACK_VAL);
	}
}