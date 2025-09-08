using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Windows;
using TS_Lib.Transforms;
using UnityEngine;
using Verse;

namespace Cosmetics.Data;

public abstract class CosmeticAttachment : IExposable
{
    public TSTransform4 Transform;
    public Pawn? Pawn;
	public float OverallScale = 1;

    public CosmeticAttachment()
	{
		Transform ??= new();
	}
	public CosmeticAttachment(Pawn pawn) : this()
	{
		Pawn = pawn;
	}


	public abstract void DrawIcon(Rect rect);
	public virtual string EditorKey => "attachment";

	public abstract void DrawTransformSettings(Window_TransformEditor editor, Listing_Standard listing);

	public virtual void ExposeData()
	{
		Scribe_Deep.Look(ref Transform, "tf");
		Scribe_References.Look(ref Pawn, "pawn");
		Scribe_Values.Look(ref OverallScale, "scale");
	}
}
