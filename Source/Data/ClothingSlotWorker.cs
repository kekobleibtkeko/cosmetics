using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TS_Lib.Util;
using Verse;

namespace Cosmetics.Data;

public class ClothingSlotWorkerBase
{
	public virtual Apparel? GetEquippedItem(Pawn pawn, ClothingSlotDef def)
	{
		var worn = pawn.apparel.WornApparel;

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