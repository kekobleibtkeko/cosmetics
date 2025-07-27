using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

#nullable enable
namespace Transmogged;

public interface IBodyTransform
{
	PawnRenderSubWorker? GetWorkerFor(PawnRenderNode node);
	TRTransform4 GetTransformFor(TransformModType type);
}

public class TRAutoBodyTransform : IExposable, IBodyTransform
{
	public TRTransform4 BodyTransform = new();
	public TRTransform4 HeadTransform = new();
	public TRTransform4 HairTransform = new();
	public TRTransform4 BeardTransform = new();

	private PawnRenderSubWorker? HairWorker;
	private PawnRenderSubWorker? HeadWorker;
	private PawnRenderSubWorker? BodyWorker;
	private PawnRenderSubWorker? BeardWorker;

    public PawnRenderSubWorker? GetWorkerFor(PawnRenderNode node) => node switch
    {
        PawnRenderNode_Hair => node.parent is PawnRenderNode_Hair ? null : (HairWorker ??= new TRPawnRenderSubWorker(HairTransform)),
        PawnRenderNode_Head => HeadWorker ??= new TRPawnRenderSubWorker(HeadTransform),
        PawnRenderNode_Body => BodyWorker ??= new TRPawnRenderSubWorker(BodyTransform),
        PawnRenderNode_Beard => BeardWorker ??= new TRPawnRenderSubWorker(BeardTransform),
        _ => null,
    };

    public void CopyFrom(Comp_Transmogged comp)
	{
		BodyTransform.CopyFrom(comp.GetData().BodyTransform);
		HeadTransform.CopyFrom(comp.GetData().HeadTransform);
		HairTransform.CopyFrom(comp.GetData().HairTransform);
		BeardTransform.CopyFrom(comp.GetData().BeardTransform);
	}

    public void ExposeData()
    {
		Scribe_Deep.Look(ref BodyTransform, "body");
		Scribe_Deep.Look(ref HeadTransform, "head");
		Scribe_Deep.Look(ref HairTransform, "hair");
		Scribe_Deep.Look(ref BeardTransform, "beard");
    }

	public TRTransform4 GetTransformFor(TransformModType type) => type switch
	{
		TransformModType.Head => HeadTransform,
		TransformModType.Hair => HairTransform,
		TransformModType.Body => BodyTransform,
		TransformModType.Beard => BeardTransform,
		_ => throw new NotImplementedException(),
	};
}

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

	public Dictionary<string, TRApparelSet> SavedSets = [];
	public Dictionary<string, TRAutoBodyTransform> AutoBodyTransforms = [];
	public Dictionary<string, TRTransform4> AutoApparelTransforms = [];

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

		SavedSets[name] = set.For(default!);
		Save();
		return true;
	}

    public void ExposeData()
    {
        TransmoggedSaveUtility.LookDict(ref SavedSets, nameof(SavedSets));
		TransmoggedSaveUtility.LookDict(ref AutoBodyTransforms, nameof(AutoBodyTransforms));
		TransmoggedSaveUtility.LookDict(ref AutoApparelTransforms, nameof(AutoApparelTransforms));
    }
}
