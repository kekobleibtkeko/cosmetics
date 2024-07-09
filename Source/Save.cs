using System;
using System.Collections.Generic;
using System.IO;
using Verse;

namespace Transmogged;

public class TransmoggedSave : IExposable
{
	public const string SaveName = "Transmogged";
	public const string DataName = "Data";
	public static string SavePath = Path.Combine(GenFilePaths.ConfigFolderPath, $"{SaveName}.xml");
	private static Lazy<TransmoggedSave> _Instance = new(delegate {
		if (!File.Exists(SavePath))
			return new();

		TransmoggedSave save = new();
		
		Scribe.loader.InitLoading(SavePath);
		ScribeMetaHeaderUtility.LoadGameDataHeader(ScribeMetaHeaderUtility.ScribeHeaderMode.None, true);
		Scribe_Deep.Look(ref save, DataName);
		Scribe.loader.FinalizeLoading();

		return save;
	});
	public static TransmoggedSave Instance = _Instance.Value;

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
