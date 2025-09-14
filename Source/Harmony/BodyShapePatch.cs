using Cosmetics.Comp;
using Cosmetics.Util;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmetics.Harmony;

[HarmonyPatch(typeof(ApparelGraphicRecordGetter), nameof(ApparelGraphicRecordGetter.TryGetGraphicApparel))]
public static class ApparelGraphicRecordGetter_BodyType_Patch
{
	public static void Prefix(Apparel apparel, ref BodyTypeDef bodyType, out ApparelGraphicRecord rec)
	{
		rec = default;
		if (apparel is null || apparel.Wearer is null)
			return;

		if (!apparel.Wearer.TryGetComp<Comp_TSCosmetics>(out var comp)
			|| comp.Save.CompState != Comp_TSCosmetics.CompState.Enabled
			|| !comp.TryGetCurrentCosmeticApparel(out var set)
			|| !set.TryGetCosmeticApparelFor(apparel, out var trap)
			|| trap!.BodyDef is null)
			return;

		bodyType = trap.BodyDef;
	}
}