using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
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

	public CosmeticApparel AddNewApparel(ThingDef def)
	{
		CosmeticApparel res = new(def, Pawn);
		Apparel.Add(res);
		NotifyUpdate();
		return res;
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
	}

	public void NotifyUpdate() => Pawn.GetComp<Comp_TSCosmetics>()?.NotifyUpdate(Comp_TSCosmetics.CompUpdateNotify.All);

    public void ExposeData()
	{
		Scribe_Values.Look(ref Name!, "name");
		Scribe_References.Look(ref Pawn, "pawn");

		Scribe_Defs.Look(ref HairOverride, "hair");
		Scribe_Defs.Look(ref BeardOverride, "beard");

		Scribe_Collections.Look(ref Apparel, "apparel");
		Scribe_Collections.Look(ref Hediffs, "hediffs");
		Scribe_Collections.Look(ref Genes, "genes");
		Scribe_Collections.Look(ref States, "states");
	}
}
