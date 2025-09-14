using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cosmetics;
using Cosmetics.Data;
using TS_Lib.Save;
using TS_Lib.Transforms;
using UnityEngine;
using Verse;

namespace Cosmetics.Mod;

public class CosmeticsSave : IExposable
{
	public const string SaveName = CosmeticsMod.ID;
	public const string DataName = "Data";
	public static string SavePath = Path.Combine(GenFilePaths.ConfigFolderPath, $"{SaveName}.xml");
	private static readonly Lazy<CosmeticsSave> _Instance = new(delegate {
		CosmeticsSave save = new();
		
		if (!File.Exists(SavePath))
			return save;
		
		try
		{
			Scribe.loader.InitLoading(SavePath);
			ScribeMetaHeaderUtility.LoadGameDataHeader(ScribeMetaHeaderUtility.ScribeHeaderMode.None, true);
			Scribe_Deep.Look(ref save, DataName);
			Scribe.loader.FinalizeLoading();
		}
		catch (System.Exception e)
		{
			Log.Error($"Error loading saved apparel sets: '{e}:{e.StackTrace}'");
			save = new();
		}
		var count = save.SavedSets.Count;
		save.SavedSets = save.SavedSets
			.Where(x => x.Value.Apparel.All(x => x.OverrideApparelDef is not null))
			.ToDictionary(x => x.Key, y => y.Value);
		
		if (save.SavedSets.Count < count)
		{
			Log.Warning($"{count - save.SavedSets.Count} sets were unable to load and were discarded");
		}

		return save;
	});
	public static CosmeticsSave Instance => _Instance.Value;

	public Dictionary<string, CosmeticSet> SavedSets = [];
	public Dictionary<string, BodyTransforms> AutoBodyTransforms = [];
	public Dictionary<string, TSTransform4> AutoApparelTransforms = [];

	public void Save()
	{
		SafeSaver.Save(SavePath, $"{nameof(CosmeticsSave)}.{DataName}", delegate {
			ScribeMetaHeaderUtility.WriteMetaHeader();
			var save = this;
			Scribe_Deep.Look(ref save, DataName);
		});
	}

	public bool SaveSet(string name, CosmeticSet set, bool force)
	{
		if (!force && SavedSets.ContainsKey(name))
			return false;

		SavedSets[name] = set.For(default);
		Save();
		return true;
	}

	public void ExposeData()
	{
		TSSaveUtility.LookDict(ref SavedSets, nameof(SavedSets));
		TSSaveUtility.LookDict(ref AutoBodyTransforms, nameof(AutoBodyTransforms));
		TSSaveUtility.LookDict(ref AutoApparelTransforms, nameof(AutoApparelTransforms));
	}
}
