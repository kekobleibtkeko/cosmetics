using System;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace Cosmetics.Mod;

public class CosmeticsSettings : ModSettings
{
	private static readonly Lazy<bool> _HAR = new(() => ModLister.GetActiveModWithIdentifier("erdelf.humanoidalienraces")?.Active == true);
	private static readonly Lazy<bool> _GradientHair = new(() => ModLister.GetActiveModWithIdentifier("automatic.gradienthair")?.Active == true);
	private static readonly Lazy<CosmeticsSettings> _Instance = new(CosmeticsMod.Instance.GetSettings<CosmeticsSettings>);
	
	public static CosmeticsSettings Instance => _Instance.Value;
	public static bool IsHARLoaded => _HAR.Value;
	public static bool GradiendHairLoaded => _GradientHair.Value;

	public const float MaxColdTemp_Default = 5;
	public const float MinHotTemp_Default = 20;
	public float MaxColdTemp;
	public float MinHotTemp;

	private string? Buffer_Cold;
	private string? Buffer_Hot;


	

	public override void ExposeData()
	{
		Scribe_Values.Look(ref MaxColdTemp,	"maxcold", MaxColdTemp_Default);
		Scribe_Values.Look(ref MinHotTemp,	"minhot",  MinHotTemp_Default);
	}

	public void DrawContent(Rect inrect)
	{
		var list = new Listing_Standard();
		list.Begin(inrect);
			list.GapLine();
			Text.Font = GameFont.Small;

			list.SliderLabeledWithValue(ref MaxColdTemp,	"Transmogged.MaxColdTemp".Translate(), -15, 25, ref Buffer_Cold, resetval: MaxColdTemp_Default);
			list.SliderLabeledWithValue(ref MinHotTemp,		"Transmogged.MinHotTemp".Translate(), 0, 40, ref Buffer_Hot, resetval: MinHotTemp_Default);
		list.End();
	}
}

public class CosmeticsMod : Verse.Mod
{
	public const string ID = "Cosmetics";
	public const string ModID = "tsuyao.cosmetics";
	public static CosmeticsMod Instance = default!;

	public CosmeticsMod(ModContentPack content) : base(content)
	{
		Instance = this;

		Log.Message($"[TS] Cosmetics loaded, settings hash: {CosmeticsSettings.Instance.GetHashCode()}"); // getting hash to init settings
		//Log.Message($"Saved sets: {CosmeticsSave.Instance.SavedSets.Count}");
	}

	public override string SettingsCategory() => ID;
	public override void DoSettingsWindowContents(Rect inRect) => CosmeticsSettings.Instance.DrawContent(inRect);
}
