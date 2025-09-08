using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Windows;
using UnityEngine;
using Verse;

namespace Cosmetics.Data;

public class CosmeticGene : CosmeticAttachment
{
    public CosmeticGene() : base()
    {
        
    }

	public CosmeticGene(Pawn pawn) : base(pawn)
	{
		
	}

	public override void DrawIcon(Rect rect)
	{
		throw new NotImplementedException();
	}

	public override void DrawTransformSettings(Window_TransformEditor editor, Listing_Standard listing)
	{
		throw new NotImplementedException();
	}

	public override void ExposeData()
    {
        base.ExposeData();
    }
}
