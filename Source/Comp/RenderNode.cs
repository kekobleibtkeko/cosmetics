using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Cosmetics.Comp;

public class PawnRenderNode_TSCosmetics : PawnRenderNode
{
    public Pawn Pawn;
    public Comp_TSCosmetics CosmeticsComp;
    public PawnRenderNode_TSCosmetics(Comp_TSCosmetics comp)
        : base(
            comp.Pawn,
            new()
            {
                workerClass = typeof(PawnRenderNodeWorker_TSCosmetics),
            },
            comp.Pawn.drawer.renderer.renderTree
        )
    {
        Pawn = comp.Pawn;
        CosmeticsComp = comp;
    }

    public override string TexPathFor(Pawn pawn) => "Things/Empty";
}

public class PawnRenderNodeWorker_TSCosmetics : PawnRenderNodeWorker
{
    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        //Log.Message($"rendering node: {node.GetType()}");
        return base.OffsetFor(node, parms, out pivot);
    }
}


