using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Data;
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

public class Comp_TSCosmetics : ThingComp
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
		None		= 0,
		Apparel		= 1 << 0,
		Face		= 1 << 1,
		Body		= 1 << 2,
		
		All			= Apparel | Face | Body
	}

    // Saved variables
	public Comp.Save Save = new();

	// Non-Saved variables
	public CompUpdateState UpdateState;
    public CosmeticSet? EditingSet;

    public int PrimedStack = 0;
    public int UnprimedStack = 0;
    public bool ForcePrime = false;

    public Pawn Pawn => parent as Pawn ?? throw new Exception("cosmetics comp attached to non-pawn");

    public void NewSet()
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
    }

	public void NotifyUpdate(CompUpdateNotify notify = CompUpdateNotify.All)
	{
		if (notify == CompUpdateNotify.None)
			return;

		if (notify.HasFlag(CompUpdateNotify.Apparel))
		{
			// reset faactories
			Pawn.apparel.Notify_ApparelChanged();
		}
	}

    public override List<PawnRenderNode> CompRenderNodes()
	{
		return [new PawnRenderNode_TSCosmetics(this)];
	}

    public override void PostExposeData()
    {
        Scribe_Deep.Look(ref Save, "data");
    }
}
