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

public class CosmeticsMod : Verse.Mod
{
	public const string ID = "Cosmetics";
	public const string ModID = "tsuyao.cosmetics";
	public static CosmeticsMod Instance = default!;

	public CosmeticsMod(ModContentPack content) : base(content)
	{
		Instance = this;
	}

	public override string SettingsCategory() => ID;
	public override void DoSettingsWindowContents(Rect inRect) => CosmeticsSettings.Instance.DrawContent(inRect);
}

[StaticConstructorOnStartup]
public static class CosmeticsPostSetup
{
	static CosmeticsPostSetup()
	{
		Harmony.Patcher.Patch();
		Log.Message($"[TS] Cosmetics loaded, settings hash: {CosmeticsSettings.Instance.GetHashCode()}"); // getting hash to init settings
		//Log.Message($"Saved sets: {CosmeticsSave.Instance.SavedSets.Count}");
	}
}