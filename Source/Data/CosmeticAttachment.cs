using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS_Lib.Transforms;
using Verse;

namespace Cosmetics.Data;

public abstract class CosmeticAttachment : IExposable
{
    public TSTransform4 Transform;

    public Pawn? Pawn;

    public CosmeticAttachment()
    {
        Transform ??= new();
    }

    public virtual void ExposeData()
    {
        Scribe_Deep.Look(ref Transform, "tf");
    }
}
