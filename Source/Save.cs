using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace Transmogged;

public class TransmoggedSave : IExposable
{
	public const string SaveName = "Transmogged";
	public const string DataName = "Data";
	public static string SavePath = Path.Combine(GenFilePaths.ConfigFolderPath, $"{SaveName}.xml");
	private static readonly Lazy<TransmoggedSave> _Instance = new(delegate {
		TransmoggedSave save = new();
		
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
			.Where(x => x.Value.Apparel.All(x => x.ApparelDef is not null))
			.ToDictionary(x => x.Key, y => y.Value);
		
		if (save.SavedSets.Count < count)
		{
			Log.Warning($"{count - save.SavedSets.Count} sets were unable to load and were discarded");
		}

		return save;
	});
	public static TransmoggedSave Instance => _Instance.Value;

	public Dictionary<string, TRApparelSet> SavedSets = new();

	public void Save()
	{
		SafeSaver.Save(SavePath, $"{nameof(TransmoggedSave)}.{DataName}", delegate {
			ScribeMetaHeaderUtility.WriteMetaHeader();
			var save = this;
			Scribe_Deep.Look(ref save, DataName);
		});
	}

	public bool SaveSet(string name, TRApparelSet set, bool force)
	{
		if (!force && SavedSets.ContainsKey(name))
			return false;

		SavedSets[name] = set.For(null!);
		Save();
		return true;
	}

    public void ExposeData()
    {
        Scribe_Collections.Look(ref SavedSets, nameof(SavedSets));
    }
}
