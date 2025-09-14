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
	public GeneDef Gene;

	[Obsolete("don't use directly, constructer used to deserialize only", true)]
	public CosmeticGene() : base() { Gene ??= default!; }
	public CosmeticGene(Pawn pawn, GeneDef def) : base(pawn)
	{
		Gene = def;
	}

	public override string Label => throw new NotImplementedException();
	public override string EditorKey => throw new NotImplementedException();

	public override void DrawIcon(Rect rect)
	{
		throw new NotImplementedException();
	}

	public CosmeticGene CreateCopy()
	{
		throw new NotImplementedException();
	}

	public CosmeticGene For(Pawn? pawn) => (this.SetPawn(pawn) as CosmeticGene)!;

	public override bool DrawOverallSettings(Window_TransformEditor editor, Listing_Standard listing)
	{
		return base.DrawOverallSettings(editor, listing);
	}

	public override void ExposeData()
	{
		base.ExposeData();
	}
}
