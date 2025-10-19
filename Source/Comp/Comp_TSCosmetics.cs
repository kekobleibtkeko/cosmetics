using System;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Data;
using Cosmetics.Mod;
using HarmonyLib;
using RimWorld;
using TS_Lib.Util;
using Verse;

namespace Cosmetics.Comp;

public class CompProperties_TSCosmetics : CompProperties
{
    public CompProperties_TSCosmetics()
    {
        compClass = typeof(Comp_TSCosmetics);
    }
}

public class Comp_TSCosmetics : ThingComp, IBodyTransform
{
    public enum CompState
    {
        Disabled,
        AutoTransforms,
        Enabled,
    }

    public enum CompUpdateState
    {
        Needed,
        InProgress,
        UpToDate,
    }

	[Flags]
	public enum CompUpdateNotify
	{
		None			= 0,
		Apparel			= 1 << 0,
		Face			= 1 << 1,
		Body			= 1 << 2,
		ForceInternal	= 1 << 3,
		
		All = Apparel | Face | Body
	}

    // Saved variables
	public Comp.Save Save = new();

	// Non-Saved variables
	public CompUpdateState UpdateState;
    public CosmeticSet? EditingSet;

	public int TicksSinceUpdate = 0;
	public int ApparelCount;
	public HashSet<string> PrimedStack = [];
    public HashSet<string> UnprimedStack = [];
    private bool ForceShowEditing = false;
	public bool IsPrimed => PrimedStack.Any() && !UnprimedStack.Any();
    public Pawn Pawn => parent as Pawn ?? throw new Exception("cosmetics comp attached to non-pawn");
	private readonly Lazy<bool> _IsStatue;
	public bool IsStatue => _IsStatue.Value;

	private ActiveCosmeticSetData? PreviousData;
	private Lazy<ActiveCosmeticSetData> SetData;
	private Lazy<List<Apparel>> ActiveRWApparel;
	private Lazy<List<CosmeticApparel>> ActiveApparel;
	public Func<ActiveCosmeticSetData> ActiveSetDataFactory => () =>
	{
		void _reset_visuals()
		{
			ResetActiveApparelFactory();
			ResetActiveRWApparelFactory();
			ResetPawnApparelInternal();
		}
		if (ForceShowEditing && EditingSet is not null)
		{
			_reset_visuals();
			return new(EditingSet, [], 69420);
		}
		var is_new = ActiveCosmeticSetData.Evaluate(this, PreviousData, out var data);
		PreviousData = data;
		if (is_new)
		{
			// Log.Message("apparel hash changed, notifiying pawn");
			_reset_visuals();
		}
		// Log.Message($"base set: '{data.BaseSet.Name}', additive: '{string.Join(", ", data.AdditiveSets.Select(x => x.Name))}'");
		return data;
	};
	public Func<List<CosmeticApparel>> ActiveApparelFactory => () =>
	{
		List<CosmeticApparel> active_apparel = [.. SetData.Value.ActiveApparel];
		// Log.Message($"Getting active rimworld apparel for {Pawn}");
		// Log.Message(string.Join(", ", active_apparel.Select(x => x.ApparelDef?.LabelCap)));
		return active_apparel;
	};

	public Func<List<Apparel>> ActiveRWApparelFactory => () =>
	{
		List<Apparel> active_rw_apparel = [..ActiveApparel.Value
			.Select(x => x.GetApparel()!)
			.Where(x => x is not null)
		];
		// Log.Message($"Getting active costmetic apparel for {Pawn}");
		// Log.Message(string.Join(", ", active_rw_apparel.Select(x => x.def.LabelCap)));
		return active_rw_apparel;
	};

	public Comp_TSCosmetics()
	{
		_IsStatue = new(() => Pawn.drawer.renderer.StatueColor.HasValue);
		ResetFactories();
		Save ??= new();
	}

	public bool PushToStack(string val, HashSet<string>? stack = null)
	{
		stack ??= PrimedStack;
		var valid = stack.Add(val);
		if (!valid)
		{
			Log.Warning($"tried adding existing value '{val}' to prime/unprime stack, is it not being cleared?");
		}
		return valid;
	}

	public bool PopFromStack(string val, HashSet<string>? stack = null)
	{
		stack ??= PrimedStack;
		var valid = stack.Remove(val);
		if (!valid)
		{
			Log.Warning($"tried removing non-existing value '{val}' from prime/unprime stack, is it not being added?");
		}
		return valid;
	}

	[MemberNotNull(nameof(SetData))]
	public void ResetSetDataFactory()
	{
		TicksSinceUpdate = 0;
		if (SetData?.IsValueCreated ?? false)
		{
			PreviousData = SetData.Value;
		}
		SetData = new(ActiveSetDataFactory);
	}
	[MemberNotNull(nameof(ActiveRWApparel))]
	public void ResetActiveRWApparelFactory()
	{
		ActiveRWApparel = new(ActiveRWApparelFactory);
	}
	[MemberNotNull(nameof(ActiveApparel))]
	public void ResetActiveApparelFactory()
	{
		ActiveApparel = new(ActiveApparelFactory);
	}

	[MemberNotNull(nameof(SetData), nameof(ActiveApparel), nameof(ActiveRWApparel))]
	public void ResetFactories()
	{
		ResetSetDataFactory();
		ResetActiveApparelFactory();
		ResetActiveRWApparelFactory();
	}

	public CosmeticSet NewSet()
	{
		bool added = false;
		int i = 0;
		var set = new CosmeticSet(Pawn);
		var name = set.Name;
		while (!added)
		{
			if (i != 0)
				name = $"{set.Name} ({i})";
			i++;
			if (Save.Sets.Any(x => x.Name == name))
				continue;

			set.Name = name;
			Save.Sets.Add(set);
			added = true;
		}
		return set;
	}

	public List<CosmeticApparel> GetActiveCosmeticApparel() => ActiveApparel.Value;
	public List<Apparel> GetActiveApparel() => ActiveRWApparel.Value;

	public ActiveCosmeticSetData Reevaluate() => SetData.Value;

	public bool TryGetCurrentCosmeticApparel([NotNullWhen(true)] out IEnumerable<CosmeticApparel>? set)
	{
		set = GetActiveCosmeticApparel();
		return set.Any();
	}

	public void SetState(CompState state)
	{
		if (Save.CompState != state)
		{
			NotifyUpdate(CompUpdateNotify.All | CompUpdateNotify.ForceInternal);
		}
		Save.CompState = state;
	}

	public void ResetPawnApparelInternal()
	{
		Pawn.apparel.Notify_ApparelChanged();
	}

	public void NotifyUpdate(CompUpdateNotify notify = CompUpdateNotify.All)
	{
		if (notify == CompUpdateNotify.None)
			return;

		ResetSetDataFactory();
		if (notify.HasFlag(CompUpdateNotify.ForceInternal))
		{
			ResetPawnApparelInternal();
			ResetActiveRWApparelFactory();
		}
		Reevaluate();
	}

	public override void CompTickInterval(int delta)
	{
		var new_count = Pawn.apparel.WornApparelCount;
		if (ApparelCount != new_count)
		{
			ApparelCount = new_count;
			foreach (var set in Save.Sets)
			{
				ResetActiveRWApparelFactory();
				GetActiveCosmeticApparel()
					.Where(x => x.LinkedSlot is not null)
					.Do(x => x.SetDirty())
				;
			}
		}

		if (Save.CompState == CompState.Disabled)
				return;
		TicksSinceUpdate += delta;
		if (TicksSinceUpdate > CosmeticsSettings.Instance.CompUpdateInterval)
		{
			ResetSetDataFactory();
			Reevaluate();
		}
	}

	public void ForceEditingSet(bool force)
	{
		ForceShowEditing = force;
		NotifyUpdate(CompUpdateNotify.All | CompUpdateNotify.ForceInternal);
	}

    public override List<PawnRenderNode> CompRenderNodes()
	{
		return [new PawnRenderNode_TSCosmetics(this)];
	}

	public override void PostExposeData()
	{
		Scribe_Deep.Look(ref Save, "data");

		foreach (var set in Save.Sets)
			set.Pawn = Pawn;
    }

	public BodyTransforms GetBodyTransforms() => Save.BodyTransforms;
}
