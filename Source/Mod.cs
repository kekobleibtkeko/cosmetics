using System;
using UnityEngine;
using Verse;

namespace Transmogged;
#nullable enable

public class TransmoggedSettings : ModSettings
{
	private static readonly Lazy<bool> _HAR = new(() => ModLister.GetActiveModWithIdentifier("erdelf.humanoidalienraces")?.Active == true);
	private static readonly Lazy<TransmoggedSettings> _Instance = new(TransmoggedMod.Instance.GetSettings<TransmoggedSettings>);
	
	public static TransmoggedSettings Instance => _Instance.Value;
	public static bool IsHARLoaded => _HAR.Value;

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

public class TransmoggedMod : Mod
{
	public const string ID = "Transmogged";
	public static TransmoggedMod Instance = default!;

	public TransmoggedMod(ModContentPack content) : base(content)
	{
		Instance = this;

		foreach (var path in content.textures.GetAllUnderPath(""))
		{
			Log.Message(path);
		}
		// TransmoggedData.Textures.CircleFilled = 
		
		ConverterRegistrator.Register();

		Log.Message($"Transmogged loaded, settings hash: {TransmoggedSettings.Instance.GetHashCode()}"); // getting hash to init settings
		Log.Message($"Saved sets: {TransmoggedSave.Instance.SavedSets.Count}");
	}

	public override string SettingsCategory() => ID;
	public override void DoSettingsWindowContents(Rect inRect) => TransmoggedSettings.Instance.DrawContent(inRect);
}
