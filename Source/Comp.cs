using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Transmogged;
#nullable enable

public class CompProperties_Transmogged : CompProperties
{
	public CompProperties_Transmogged()
	{
		compClass = typeof(Comp_Transmogged);
	}
}

public enum TRCompState
{
	Disabled,
	Transforms,
	Enabled
}

public class TransmoggedCompSave : IExposable
{
	public TRCompState State;
	public List<TRApparelSet> ApparelSets = [];
	public TRTransform4 BodyTransform;
	public TRTransform4 HeadTransform;
	public TRTransform4 HairTransform;
	public TRTransform4 BeardTransform;
	public HairDef? BaseHair;
	public BeardDef? BaseBeard;
    
	public TransmoggedCompSave()
	{
		ApparelSets ??= [];
		BodyTransform ??= new();
		HeadTransform ??= new();
		HairTransform ??= new();
		BeardTransform ??= new();
	}

	public void ExposeData()
    {
		Scribe_Values.Look(ref State, "state");
		Scribe_Collections.Look(ref ApparelSets, "sets");
		
		Scribe_Deep.Look(ref BodyTransform, "bodytr");
		Scribe_Deep.Look(ref HeadTransform, "headtr");
		Scribe_Deep.Look(ref HairTransform, "hairtr");
		Scribe_Deep.Look(ref BeardTransform, "beardtr");
		
		Scribe_Defs.Look(ref BaseHair, "basehair");
		Scribe_Defs.Look(ref BaseBeard, "basebeard");
    }
}

public class Comp_Transmogged : ThingComp, IBodyTransform
{
	public const float FLAG_UNFIT = -999;
	public const float FLAG_FIT = 1;
	public const float FLAG_FIT_WELL = 2;
	public const float FLAG_FIT_HIGH = 3;

	private TransmoggedCompSave SavedData = new();
	public int PrimedStack = 0;
	public int UnprimedStack = 0;

	public TickTimer Timer;

	public TRApparelSet? EditingSet;
	public bool ForceEditingVisible;
	public Lazy<List<TRApparel>> ActiveSet = new();
	public Lazy<List<Apparel>> ActiveApparel = new();

	private PawnRenderSubWorker? HairWorker;
	private PawnRenderSubWorker? HeadWorker;
	private PawnRenderSubWorker? BodyWorker;
	private PawnRenderSubWorker? BeardWorker;

	public Pawn Pawn => (parent as Pawn) ?? throw new Exception("pawn was null in transmogged comp");

	public Func<List<TRApparel>> ActiveSetFactory => () => {
		if (ForceEditingVisible && EditingSet is not null)
		{
			return EditingSet.Apparel;
		}

		List<TRApparel> res = [];
		float maxval = 0;

		IEnumerable<TRApparel>? baseset = null;
		bool outside;
		try
		{
			outside = Pawn.IsOutside(); // throws here in some circumstances, unsure which
		}
		catch (System.Exception e)
		{
			Debug.LogError($"CAUGHT: {e}{e.StackTrace}");
			outside = true;
		}

		float temp = Pawn.AmbientTemperature;
		var nonadditive = (SavedData.ApparelSets ??= []).Where(x => !x.State.HasFlag(TRState.Additive));
		var additive = SavedData.ApparelSets.Except(nonadditive);

		foreach (var apset in nonadditive)
		{
			float curval = apset.GetSetPoints(Pawn, outside, temp);
			if (curval < 0 || curval < maxval)
				continue;

			maxval = curval;
			baseset = apset?.Apparel;
		}

		if (baseset is not null)
			res.AddRange(baseset);

		foreach (var apset in additive)
		{
			float curval = apset.GetSetPoints(Pawn, outside, temp);
			if (curval < 0)
				continue;

			if (apset is not null && apset.Apparel is not null)
				res.AddRange(apset.Apparel);
		}
		return res;
	};

	public Func<List<Apparel>> ActiveApparelFactory => () => {
		return ActiveSet.Value.Select(x => x.GetApparel()).ToList();
	};

	public bool IsPrimed()
	{
		return PrimedStack > 0 && UnprimedStack == 0;
	}

    public Comp_Transmogged()
    {
		ResetFactories();
		SavedData ??= new();
        Timer = new();
    }

	public TransmoggedCompSave GetData()
	{
		return SavedData ??= new();
	}

	public void ResetFactories()
	{
		ActiveSet = new(ActiveSetFactory);
		ActiveApparel = new(ActiveApparelFactory);
	}

	public void ForceEdit(bool force)
	{
		ForceEditingVisible = force;
		NotifyUpdate();
	}

	public void NotifyUpdate()
	{
		ResetFactories();
		Pawn.apparel.Notify_ApparelChanged();
	}

    public void SetState(TRCompState state)
	{
		if (SavedData.State != state)
			NotifyUpdate();
		SavedData.State = state;
	}

	public void CopySet(TRApparelSet set)
	{
		var nset = set.CreateCopy();
		SavedData.ApparelSets.Add(nset);
		EditingSet = nset;
	}

	public TRState GetCurrentPawnState()
	{
		if (SavedData.State != TRCompState.Enabled)
			return TRState.None;
		
		return Pawn.Drafted
			? TRState.Drafted
			: TRState.NonDrafted;
	}

    public override void CompTick()
    {
		int tickstime = 60 + UnityEngine.Random.Range(0, 5); // TODO: make configurable
		if ((Timer ??= new()).Finished)
		{
			Timer.Start(GenTicks.TicksGame, tickstime, NotifyUpdate);
		}
		Timer.TickIntervalDelta();
    }

	public List<Apparel> GetActiveApparel() => ActiveApparel.Value;

	public bool TryGetCurrentTransmog(out IEnumerable<TRApparel> set)
	{
		set = ActiveSet.Value;
		return set.Any();
	}

	public override void PostExposeData()
	{
		Scribe_Deep.Look(ref SavedData, "TR_data");
	}

	public PawnRenderSubWorker? GetWorkerFor(PawnRenderNode node) => node switch
	{
		PawnRenderNode_Hair => node.parent is PawnRenderNode_Hair ? null : (HairWorker ??= new TRPawnRenderSubWorker(GetData().HairTransform)),
		PawnRenderNode_Head => HeadWorker ??= new TRPawnRenderSubWorker(GetData().HeadTransform),
		PawnRenderNode_Body => BodyWorker ??= new TRPawnRenderSubWorker(GetData().BodyTransform),
		PawnRenderNode_Beard => BeardWorker ??= new TRPawnRenderSubWorker(GetData().BeardTransform),
		_ => null,
	};

    public TRTransform4 GetTransformFor(TransformModType type) => type switch
    {
        TransformModType.Head => GetData().HeadTransform,
        TransformModType.Hair => GetData().HairTransform,
        TransformModType.Body => GetData().BodyTransform,
        TransformModType.Beard => GetData().BeardTransform,
		_ => throw new NotImplementedException(),
    };
}