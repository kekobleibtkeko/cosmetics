using System.Linq;
using AlienRace;
using Cosmetics.Util;
using TS_Lib.Util;
using Verse;

namespace Cosmetics.Windows;

public static class HARInspectorHelper
{
	public const float DROPDOWN_HEIGHT = 40;
	public static void DrawRaceSelection(Listing_Standard listing, ref ThingDef? def, Pawn pawn)
	{
		listing.Label("as race".ModTranslate());
		CosmeticsUtil.RaceDefs
			.Except(pawn.def)
			.Prepend(null)
			.ValueDropdown(
				listing.GetRect(DROPDOWN_HEIGHT),
				ref def,
				pawn!.GetHashCode(),
				def => def?.LabelCap ?? "default race".ModTranslate()
			)
		;
	}
}