using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Data;
using Cosmetics.Mod;
using RimWorld;
using Verse;

namespace Cosmetics.Util;

public static class CosmeticsUtil
{
	private readonly static Lazy<List<StateDef>> _StateDefs = new(() => [.. DefDatabase<StateDef>.AllDefsListForReading.OrderBy(def => def.order)]);
	public static List<StateDef> StateDefs = _StateDefs.Value;

	private readonly static Lazy<List<ThingDef>> _AllApparel = new(() => [.. DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsApparel)]);
	public static List<ThingDef> AllApparel => _AllApparel.Value;

	private readonly static Lazy<List<BodyTypeDef>> _BodyTypes = new(() => [.. DefDatabase<BodyTypeDef>.AllDefsListForReading]);
	public static List<BodyTypeDef> BodyTypes => _BodyTypes.Value;

	private readonly static Lazy<List<ClothingSlotDef>> _ClothingSlots = new(() => [.. DefDatabase<ClothingSlotDef>.AllDefsListForReading.OrderBy(x => x.order)]);
	public static List<ClothingSlotDef> ClothingSlots => _ClothingSlots.Value;

	private readonly static Lazy<List<ThingDef>> _Materials = new(() => [.. DefDatabase<ThingDef>.AllDefsListForReading.Where(t => t.IsStuff)]);
	public static List<ThingDef> Materials => _Materials.Value;

	private readonly static Lazy<List<ThingDef>> _RaceDefs = new(() =>
	{
		if (!CosmeticsSettings.IsHARLoaded)
			return [];
		return [.. DefDatabase<AlienRace.ThingDef_AlienRace>.AllDefsListForReading];
	});

	public static List<ThingDef> RaceDefs => _RaceDefs.Value;

	public static TaggedString ModTranslate(this string input) => Translator.Translate($"{CosmeticsMod.ID}.{input.Replace(' ', '_')}");

	public static bool TryGetCosmeticApparelFor(this IEnumerable<CosmeticApparel> vset, Apparel apparel, out CosmeticApparel? tr)
	{
		tr = null;
		foreach (var ap in vset)
		{
			var inner = ap.GetApparel();
			if (apparel == inner)
			{
				tr = ap;
				return true;
			}
		}
		return false;
	}
	
	public static string GetAutoBodyKey(this Pawn pawn)
	{
		string race = "Human";
		if (CosmeticsSettings.IsHARLoaded)
		{
			var alienRaceType = Type.GetType("AlienRace.ThingDef_AlienRace");
			if (alienRaceType != null && alienRaceType.IsInstanceOfType(pawn.def))
			{
				var defNameProp = alienRaceType.GetProperty("defName");
				if (defNameProp?.GetValue(pawn.def) is string defNameValue)
					race = defNameValue;
			}
		}
		return $"{race}.{pawn.story.bodyType.defName}";
	}
}
