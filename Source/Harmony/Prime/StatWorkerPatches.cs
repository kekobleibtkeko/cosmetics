using Cosmetics.Comp;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmetics.Harmony.Prime;

[HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
public static class StatWorker_Value_Unprime
{
	private const string STACK_VAL = "statworker_value";
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

		__state!.PushToStack(STACK_VAL, __state!.UnprimedStack);
	}

	public static void Postfix(Comp_TSCosmetics? __state)
	{
		if (__state is null)
			return;

		__state!.PopFromStack(STACK_VAL, __state!.UnprimedStack);
	}
}