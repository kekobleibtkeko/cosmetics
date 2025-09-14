
using System;
using System.Collections.Generic;
using System.Linq;
using Cosmetics.Data;
using Cosmetics.Util;
using HarmonyLib;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace Cosmetics.Mod;

public class CosmeticsSettings : ModSettings
{
	private static readonly Lazy<bool> _HAR = new(() => ModLister.GetActiveModWithIdentifier("erdelf.humanoidalienraces")?.Active == true);
	private static readonly Lazy<bool> _GradientHair = new(() => ModLister.GetActiveModWithIdentifier("automatic.gradienthair")?.Active == true);
	private static readonly Lazy<CosmeticsSettings> _Instance = new(CosmeticsMod.Instance.GetSettings<CosmeticsSettings>);
	private static readonly Lazy<Tree<CosmeticSet, string>> _SavedApparelTree = new(() =>
	{
		return TSUtil.BuildTree(
			CosmeticsSave.Instance.SavedSets,
			(set, rect) =>
			{
				Widgets.Label(rect, set.Item.Name);
				return false;
			}
		);
	});
	
	public static CosmeticsSettings Instance => _Instance.Value;
	public static bool IsHARLoaded => _HAR.Value;
	public static bool GradiendHairLoaded => _GradientHair.Value;

	public const int COMP_UPDATE_INTERVAL = 40;

	public float CompUpdateInterval = COMP_UPDATE_INTERVAL;
	public Dictionary<string, string> EditBuffers = [];
	

	public override void ExposeData()
	{
		Scribe_Values.Look(ref CompUpdateInterval, "updateinterval", COMP_UPDATE_INTERVAL);
		CosmeticsUtil.StateDefs
			.Where(def => def.Worker.GetWorkerProps().HasSettings)
			.Do(def => def.Worker.GetWorkerProps().ExposeSettingsData(def));
	}

	public void DrawContent(Rect inrect)
	{
		using var list = new TSUtil.Listing_D(inrect);
		list.Listing.GapLine();

		list.Listing.SliderLabeledWithValue(
			ref CompUpdateInterval,
			"comp update interval".ModTranslate(),
			1, 120,
			EditBuffers,
			"comp update interval explanation".ModTranslate(),
			COMP_UPDATE_INTERVAL
		);

		using (new TSUtil.TextSize_D(GameFont.Medium))
			list.Listing.Label("state settings".ModTranslate());
		CosmeticsUtil.StateDefs
			.Where(def => def.Worker.GetWorkerProps().HasSettings)
			.Do(def =>
			{
				list.Listing.GapLine();
				list.Listing.Label(def.LabelCap);
				def.Worker.GetWorkerProps().DrawExtraOptions(this, list.Listing, def);
			})
		;
		list.Listing.GapLine();

		_SavedApparelTree.Value.Draw(list.Listing);
	}
}
