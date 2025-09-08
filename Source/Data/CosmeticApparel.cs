using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Util;
using RimWorld;
using System.Diagnostics.CodeAnalysis;
using TS_Lib.Transforms;
using UnityEngine;
using Verse;
using Cosmetics.Windows;
using Cosmetics.Mod;

namespace Cosmetics.Data;

public class CosmeticApparel : CosmeticAttachment
{
	public ThingDef? ApparelDef;
	public ThingStyleDef? StyleDef;
	public BodyTypeDef? BodyDef;
	public ThingDef? RaceDef;
	public CosmeticsUtil.ClothingSlot? LinkedSlot;
	public Color? Color;

	// unsaved vars
	private Lazy<Apparel?> InnerApparel;

	public CosmeticApparel() : base()
	{
		Init();
	}
	public CosmeticApparel(ThingDef def, Pawn pawn) : base(pawn)
	{
		ApparelDef = def;
		Init();
	}

	[MemberNotNull(nameof(InnerApparel))]
	private void Init()
	{
		SetApparelDirty();
	}

	public Func<Apparel?> GetApparelFactory() =>
		() =>
		{
			if (ApparelDef is null)
				return null;
			if (ThingMaker.MakeThing(ApparelDef, GenStuff.DefaultStuffFor(ApparelDef)) is not Apparel apparel)
			{
				Messages.Message("unable to make apparel for cosmetics cache?", null, MessageTypeDefOf.RejectInput);
				throw new Exception("unable to make apparel for cosmetics cache?");
			}

			if (Color.HasValue)
				apparel.SetColor(Color.Value, reportFailure: false);
			apparel.StyleDef = StyleDef;
			apparel.holdingOwner = Pawn?.apparel.GetDirectlyHeldThings();
			return apparel;
		};

	public Apparel? GetApparel() => (InnerApparel ??= new(GetApparelFactory())).Value;

	[MemberNotNull(nameof(InnerApparel))]
	public void SetApparelDirty() => InnerApparel = (InnerApparel ??= new(GetApparelFactory())).IsValueCreated
		? new(GetApparelFactory())
		: InnerApparel
	;

	public override void ExposeData()
	{
		base.ExposeData();
	}

	public override void DrawIcon(Rect rect)
	{
		Widgets.ThingIcon(rect, GetApparel());
	}

	public override void DrawTransformSettings(Window_TransformEditor editor, Listing_Standard listing)
	{
		editor.DrawBodySelection(listing, ref BodyDef);
		if (CosmeticsSettings.IsHARLoaded)
			HARInspectorHelper.DrawRaceSelection(listing, ref RaceDef, Pawn!);
	}
}
