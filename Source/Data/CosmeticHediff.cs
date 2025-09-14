using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Windows;
using UnityEngine;
using Verse;

namespace Cosmetics.Data;

public class CosmeticHediff : CosmeticAttachment
{
	public HediffDef Hediff;
	public override string Label => throw new NotImplementedException();
	public override string EditorKey => throw new NotImplementedException();

	[Obsolete("don't use directly, constructer used to deserialize only", true)]
	public CosmeticHediff() : base() { Hediff ??= default!; }
	public CosmeticHediff(Pawn pawn, HediffDef def) : base(pawn)
	{
		Hediff = def;
	}

	public override void DrawIcon(Rect rect)
	{
		throw new NotImplementedException();
	}

	public CosmeticHediff CreateCopy()
	{
		throw new NotImplementedException();
	}

	public CosmeticHediff For(Pawn? pawn) => (SetPawn(pawn) as CosmeticHediff)!;

	public override bool DrawOverallSettings(Window_TransformEditor editor, Listing_Standard listing)
	{
		return base.DrawOverallSettings(editor, listing);
	}

	public override void ExposeData()
	{
		base.ExposeData();
	}
}
