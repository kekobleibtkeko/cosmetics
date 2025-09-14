using Cosmetics.Comp;
using HarmonyLib;
using Verse;

namespace Cosmetics.Harmony.Prime;

[HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.AdjustParms))]
public static class PawnRenderTree_Adjust_Patch
{
	private const string STACK_VAL = "pawnrendertree_adjust";
	public static void Prefix(ref PawnDrawParms parms, PawnRenderTree __instance, out Comp_TSCosmetics? __state)
	{
		if (!__instance.pawn.TryGetComp(out __state))
			return;

		__state!.PushToStack(STACK_VAL);
	}

	public static void Postfix(ref PawnDrawParms parms, Comp_TSCosmetics? __state)
	{
		if (__state is null)
			return;

		__state.PopFromStack(STACK_VAL);
	}
}

[HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.SetupDynamicNodes))]
public static class PawnRenderTree_SetupNodes_Patch
{
	private const string STACK_VAL = "pawnrendertree_setupnodes";
	public static void Prefix(PawnRenderTree __instance, out Comp_TSCosmetics? __state)
	{
		if (!__instance.pawn.TryGetComp(out __state))
			return;

		__state!.PushToStack(STACK_VAL);
	}

	public static void Postfix(Comp_TSCosmetics? __state)
	{
		if (__state is null)
			return;

		__state!.PopFromStack(STACK_VAL);
	}
}