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
    public enum ClothingSlot
    {
        Head,
        TopShell,
        TopMiddle,
        TopSkin,
        Bottom,
        Belt,
    }

    public static List<ClothingSlot> ClothingSlots = [
        ClothingSlot.Head,
        ClothingSlot.TopShell,
        ClothingSlot.TopMiddle,
        ClothingSlot.TopSkin,
        ClothingSlot.Bottom,
        ClothingSlot.Belt,
    ];

    private readonly static Lazy<List<StateDef>> _StateDefs = new(() => [.. DefDatabase<StateDef>.AllDefsListForReading.OrderBy(def => def.order)]);
    public static List<StateDef> StateDefs = _StateDefs.Value;

	private readonly static Lazy<List<ThingDef>> _AllApparel = new(() => [.. DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsApparel)]);
	public static List<ThingDef> AllApparel => _AllApparel.Value;

	private readonly static Lazy<List<BodyTypeDef>> _BodyTypes = new(() => [.. DefDatabase<BodyTypeDef>.AllDefsListForReading]);
	public static List<BodyTypeDef> BodyTypes => _BodyTypes.Value;

	private readonly static Lazy<List<ThingDef>> _RaceDefs = new(() =>
	{
		if (!CosmeticsSettings.IsHARLoaded)
			return [];
		return [.. DefDatabase<AlienRace.ThingDef_AlienRace>.AllDefsListForReading];
	});
	public static List<ThingDef> RaceDefs => _RaceDefs.Value;

    public static string ToTranslated(this ClothingSlot slot) => slot.ToString().ModTranslate();

    public static TaggedString ModTranslate(this string input) => Translator.Translate($"{CosmeticsMod.ID}.{input}");

    public static Apparel? GetWornApparelBySlot(this Pawn pawn, ClothingSlot slot)
    {
        var worn = pawn.apparel.WornApparel;
        return slot switch
        {
            ClothingSlot.Head => worn.FirstOrDefault(x
                => x.def.apparel.CoversBodyPartGroup(BodyPartGroupDefOf.UpperHead)
                || x.def.apparel.CoversBodyPartGroup(BodyPartGroupDefOf.FullHead)
            ),
            ClothingSlot.TopShell => worn.FirstOrDefault(x
                => x.def.apparel.CoversBodyPartGroup(BodyPartGroupDefOf.Torso)
                && x.def.apparel.layers.Contains(ApparelLayerDefOf.Shell)
            ),
            ClothingSlot.TopMiddle => worn.FirstOrDefault(x
                => x.def.apparel.CoversBodyPartGroup(BodyPartGroupDefOf.Torso)
                && x.def.apparel.layers.Contains(ApparelLayerDefOf.Middle)
            ),
            ClothingSlot.TopSkin => worn.FirstOrDefault(x
                => x.def.apparel.CoversBodyPartGroup(BodyPartGroupDefOf.Torso)
                && x.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin)
            ),
            ClothingSlot.Bottom => worn.FirstOrDefault(x
                => x.def.apparel.CoversBodyPartGroup(BodyPartGroupDefOf.Legs)
            ),
            ClothingSlot.Belt => worn.FirstOrDefault(x
                => x.def.apparel.layers.Contains(ApparelLayerDefOf.Belt)
            ),
            _ => null
        };
    }
}
