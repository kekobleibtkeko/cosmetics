using System;
using System.Collections.Generic;
using System.Linq;
using Cosmetics.Comp;
using RimWorld;
using TS_Lib.Util;
using Verse;

namespace Cosmetics.Data;

public class ClothingSlotWorkerBase
{
	private const string STACK_VAL = "clothingworker_equipped";
	public virtual Apparel? GetEquippedItem(Pawn pawn, ClothingSlotDef def)
	{
		// comp.PushToStack(STACK_VAL, comp.UnprimedStack);
		var worn = pawn.apparel.wornApparel.InnerListForReading;
		// comp.PopFromStack(STACK_VAL, comp.UnprimedStack);

		return worn.FirstOrDefault(apparel =>
		{
			var layer_func = def.apparelLayerInclusion.GetFuncFor(def.apparelLayers);
			if (!layer_func(apparel.def.apparel.layers.Contains))
				return false;
			var body_part_func = def.bodyPartInclusion.GetFuncFor(def.bodyParts);
			if (!body_part_func(apparel.def.apparel.CoversBodyPartGroup))
				return false;
			return true;
		});
	}
}