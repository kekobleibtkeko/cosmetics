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

public class Comp_Transmogged : ThingComp
{
	public const float FLAG_UNFIT = -999;
	public const float FLAG_FIT = 1;
	public const float FLAG_FIT_WELL = 2;
	public const float FLAG_FIT_HIGH = 3;

	public bool Enabled;
	public int PrimedStack = 0;
	public int UnprimedStack = 0;

	public TickTimer Timer;
	public List<TRApparelSet> ApparelSets = new();

	public TRApparelSet? EditingSet;
	public Lazy<List<TRApparel>> ActiveSet = new();
	public Lazy<List<Apparel>> ActiveApparel = new();

	public Func<List<TRApparel>> ActiveSetFactory => () => {
		List<TRApparel> res = new();
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
		var nonadditive = (ApparelSets ??= new()).Where(x => !x.State.HasFlag(TRState.Additive));
		var additive = ApparelSets.Except(nonadditive);

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

    public Comp_Transmogged()
    {
		ResetFactories();
        Timer = new();
    }

	public void ResetFactories()
	{
		ActiveSet = new(ActiveSetFactory);
		ActiveApparel = new(ActiveApparelFactory);
	}

    public Pawn Pawn => (parent as Pawn) ?? throw new Exception("pawn was null in transmogged comp");

	public void NotifyUpdate()
	{
		ResetFactories();
		Pawn.apparel.Notify_ApparelChanged();
	}

    public void SetEnabled(bool active)
	{
		if (Enabled != active)
			NotifyUpdate();
		Enabled = active;
	}

	public void CopySet(TRApparelSet set)
	{
		var nset = set.CreateCopy();
		ApparelSets.Add(nset);
		EditingSet = nset;
	}

	public TRState GetCurrentPawnState()
	{
		if (!Enabled)
			return TRState.None;
		
		return Pawn.Drafted
			? TRState.Drafted
			: TRState.NonDrafted;
	}

    public override void CompTick()
    {
		int tickstime = 60; // TODO: make configurable
		if ((Timer ??= new()).Finished)
		{
			Timer.Start(GenTicks.TicksGame, tickstime, NotifyUpdate);
		}
		Timer.TickInterval();
    }

	public List<Apparel> GetActiveApparel() => ActiveApparel.Value;

	public bool TryGetCurrentTransmog(out IEnumerable<TRApparel> set)
	{
		set = ActiveSet.Value;
		return set.Any();
	}

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref Enabled, "TR_active");
		Scribe_Collections.Look(ref ApparelSets, "TR_sets");
		Scribe_Deep.Look(ref Timer, "TR_timer");
	}
}