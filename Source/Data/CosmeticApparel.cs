using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TS_Lib.Transforms;
using UnityEngine;
using Verse;

namespace Cosmetics.Data;

public class CosmeticApparel : CosmeticAttachment
{
    public ThingDef? ApparelDef;
    public ThingStyleDef? StyleDef;
    public BodyTypeDef? BodyDef;
    public ThingDef? RaceDef;
    public ApparelLayerDef? LinkedLayer;
    public Color? Color;
    public float OverallScale = 1;

    // unsaved vars
    public Lazy<Apparel> InnerApparel;

    public CosmeticApparel() : base()
    {
        
    }

    public override void ExposeData()
    {
        base.ExposeData();
    }
}
