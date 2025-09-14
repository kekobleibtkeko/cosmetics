using System;
using System.Collections.Generic;
using System.Linq;
using Cosmetics.Mod;
using HarmonyLib;

namespace Cosmetics.Harmony;

public static class Patcher
{
	public static void Patch()
	{
		var harmony = new HarmonyLib.Harmony(CosmeticsMod.ModID);

		List<Type> patch_classes = [
			// prime/unprime
			typeof(Prime.PawnRenderTree_Adjust_Patch),
			typeof(Prime.PawnRenderTree_SetupNodes_Patch),
			typeof(Prime.CompStatue_Prime_Patch),
			typeof(Prime.StatWorker_Value_Unprime),

			// transform
			typeof(Transform.ApparelTransformPatches),
			typeof(Transform.BodyTransformPatches),

			// other
			typeof(Pawn_ApparelTracker_WornApparel_Patch),
			typeof(ApparelGraphicRecordGetter_BodyType_Patch),
			typeof(Draft_Patch),
		];

		if (CosmeticsSettings.IsHARLoaded)
		{
			// add har patches
		}

		patch_classes
			.Select(t => harmony.CreateClassProcessor(t))
			.Do(p => p.Patch())
		;
	}
}