using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmetics.Data;

public class CosmeticSet : IExposable
{
    public string Name = "New set";
    public Pawn Pawn = default!; // is not null, will be set on deserialize

    public HairDef? HairOverride;
    public BeardDef? BeardOverride;

    public List<CosmeticApparel> OverriddenWorn = [];
    public List<CosmeticApparel> Apparel = [];
    public List<CosmeticHediff> Hediffs = [];
    public List<CosmeticGene> Genes = [];
    public List<StateDef> States = [];

	public IEnumerable<CosmeticAttachment> AllAttachments => Enumerable.Empty<CosmeticAttachment>()
		.Concat(OverriddenWorn)
		.Concat(Apparel)
		.Concat(Hediffs)
		.Concat(Genes)
	;

    [Obsolete("don't use directly, constructer used to deserialize only", true)]
    public CosmeticSet() { }
    public CosmeticSet(Pawn pawn)
    {
        Pawn = pawn;
    }

	public CosmeticSet CreateCopy(Pawn? for_pawn)
	{
		var new_set = new CosmeticSet(Pawn)
		{
			Name = $"{Name} Copy",
			HairOverride = HairOverride,
			BeardOverride = BeardOverride,
			States = [.. States],
			OverriddenWorn = [.. OverriddenWorn.Select(x => x.CreateCopy())],
			Apparel = [.. Apparel.Select(x => x.CreateCopy())],
			Genes = [.. Genes.Select(x => x.CreateCopy())],
			Hediffs = [.. Hediffs.Select(x => x.CreateCopy())],
		};
		return new_set.For(for_pawn ?? Pawn);
	}

	public CosmeticSet For(Pawn? pawn)
	{
		Pawn = pawn!; // will only be null when saving set
		AllAttachments.Do(ap => ap.SetPawn(pawn));
		return this;
	}

	public CosmeticApparel AddNewApparel(ThingDef def)
	{
		CosmeticApparel res = new(def, Pawn);
		Apparel.Add(res);
		NotifyUpdate();
		return res;
	}

	public CosmeticApparel? GetApparelForSlot(ClothingSlotDef def)
	{
		return OverriddenWorn.FirstOrDefault(x => x.LinkedSlot?.Def == def);
	}

	public void ToggleState(StateDef state)
	{
		if (States.Contains(state))
		{
			States.Remove(state);
		}
		else
		{
			States.Add(state);
			States.RemoveAll(state.incompatibleStates.Contains);
			States.RemoveDuplicates();
		}
		NotifyUpdate();
	}

	public float GetSetPoints(Pawn pawn, Comp_TSCosmetics comp)
	{
		float res = 0;
		foreach (var state in States)
		{
			res += state.GetFit(pawn, comp);
		}
		return res;
	}

	public void NotifyUpdate(
		Comp_TSCosmetics.CompUpdateNotify notify
			= Comp_TSCosmetics.CompUpdateNotify.All | Comp_TSCosmetics.CompUpdateNotify.ForceInternal
	)
		=> Pawn?.GetComp<Comp_TSCosmetics>()?.NotifyUpdate(notify);

	public override int GetHashCode()
	{
		return ((States, AllAttachments, Name).GetHashCode() / 2) + base.GetHashCode() / 2;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref Name!, "name");
		Scribe_References.Look(ref Pawn, "pawn");

		Scribe_Defs.Look(ref HairOverride, "hair");
		Scribe_Defs.Look(ref BeardOverride, "beard");

		Scribe_Collections.Look(ref OverriddenWorn, "overr");
		Scribe_Collections.Look(ref Apparel, "apparel");
		Scribe_Collections.Look(ref Hediffs, "hediffs");
		Scribe_Collections.Look(ref Genes, "genes");
		Scribe_Collections.Look(ref States, "states");
	}
}
