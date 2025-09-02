using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Data;
using Verse;

namespace Cosmetics.Comp;

public class Save : IExposable
{
    public Comp_TSCosmetics.CompState CompState = Comp_TSCosmetics.CompState.AutoTransforms;

    public List<CosmeticSet> Sets = [];

    public void ExposeData()
    {
        Scribe_Values.Look(ref CompState, "state");
        Scribe_Collections.Look(ref Sets, "sets");
    }
}
