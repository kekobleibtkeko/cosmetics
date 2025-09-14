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
using Cosmetics.Comp;

namespace Cosmetics.Data;

public class CosmeticApparel : CosmeticAttachment
{
	public class LinkedSlotData(ClothingSlotDef def) : IExposable
	{
		public enum StateType
		{
			Modify,
			Disable,
		}

		public ClothingSlotDef Def = def;
		public StateType State;

		public Apparel? GetApparelFor(Pawn? pawn)
		{
			if (pawn is null)
				return null;
			return Def.GetEquippedItemFor(pawn);
		}

		public LinkedSlotData CreateCopy()
		{
			return new(Def) { State = State };
		}

		public void ExposeData()
		{
			Scribe_Defs.Look(ref Def, "def");
			Scribe_Values.Look(ref State, "state");
		}
	}

	public ThingDef? OverrideApparelDef;
	public ThingStyleDef? StyleDef;
	public BodyTypeDef? BodyDef;
	public ThingDef? RaceDef;
	public LinkedSlotData? LinkedSlot;
	public Color? Color;

	// unsaved vars
	private Lazy<Apparel?> InnerApparel;

	public override string Label => OverrideApparelDef?.LabelCap ?? "Unknown Apparel";
	public override string EditorKey => "apparel transform";
	public override Comp_TSCosmetics.CompUpdateNotify UpdateFlags => Comp_TSCosmetics.CompUpdateNotify.Apparel;

	[Obsolete("don't use directly, constructer used to deserialize only", true)]
	public CosmeticApparel() : base()
	{
		Init();
	}
	public CosmeticApparel(Pawn? pawn) : base(pawn)
	{
		Init();
	}
	public CosmeticApparel(ThingDef def, Pawn pawn) : base(pawn)
	{
		OverrideApparelDef = def;
		Init();
	}

	public CosmeticApparel(Pawn? pawn, ClothingSlotDef slot_def) : this(pawn)
	{
		LinkedSlot = new(slot_def);
	}

	public CosmeticApparel For(Pawn? pawn) => (SetPawn(pawn) as CosmeticApparel)!;

	public ThingDef? GetApparelDef()
	{
		if (LinkedSlot is not null)
		{
			return LinkedSlot.State switch
			{
				LinkedSlotData.StateType.Modify => OverrideApparelDef ?? LinkedSlot.GetApparelFor(Pawn)?.def,
				LinkedSlotData.StateType.Disable or _ => null,
			};
		}
		else
		{
			return OverrideApparelDef;
		}
	}
	public Color? GetColor()
	{
		if (LinkedSlot is not null)
		{
			return LinkedSlot.State switch
			{
				LinkedSlotData.StateType.Modify => Color ?? LinkedSlot.GetApparelFor(Pawn)?.DrawColor,
				LinkedSlotData.StateType.Disable or _ => null,
			};
		}
		else
		{
			return Color;
		}
	}
	public ThingStyleDef? GetStyleDef()
	{
		if (LinkedSlot is not null)
		{
			return LinkedSlot.State switch
			{
				LinkedSlotData.StateType.Modify => StyleDef ?? LinkedSlot.GetApparelFor(Pawn)?.StyleDef,
				LinkedSlotData.StateType.Disable or _ => null,
			};
		}
		else
		{
			return StyleDef;
		}
	}

	public CosmeticApparel CreateCopy(Pawn? for_pawn = null)
	{
		return new CosmeticApparel(for_pawn ?? Pawn)
		{
			Transform = Transform.CreateCopy(),
			OverallScale = OverallScale,
			OverrideApparelDef = OverrideApparelDef,
			StyleDef = StyleDef,
			BodyDef = BodyDef,
			RaceDef = RaceDef,
			LinkedSlot = LinkedSlot?.CreateCopy(),
			Color = Color,
		};
	}

	[MemberNotNull(nameof(InnerApparel))]
	private void Init()
	{
		SetApparelDirty();
	}

	public Func<Apparel?> ApparelFactory =>
		() =>
		{
			if (LinkedSlot is not null && LinkedSlot.GetApparelFor(Pawn) is null)
				return null;
			var def = GetApparelDef();
			if (def is null)
				return null;
			if (ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def)) is not Apparel apparel)
			{
				Messages.Message("unable to make apparel for cosmetics cache?", null, MessageTypeDefOf.RejectInput);
				throw new Exception("unable to make apparel for cosmetics cache?");
			}

			var color = GetColor();
			if (color.HasValue)
				apparel.SetColor(color.Value, reportFailure: false);
			apparel.StyleDef = GetStyleDef();
			apparel.holdingOwner = Pawn?.apparel.GetDirectlyHeldThings();
			return apparel;
		};

	public Apparel? GetApparel() => (InnerApparel ??= new(ApparelFactory)).Value;

	[MemberNotNull(nameof(InnerApparel))]
	public void SetApparelDirty() => InnerApparel = (InnerApparel ??= new(ApparelFactory)).IsValueCreated
		? new(ApparelFactory)
		: InnerApparel
	;

	public override void SetDirty()
	{
		SetApparelDirty();
	}

	public void SetSlotState(LinkedSlotData.StateType state)
	{
		if (LinkedSlot is null)
			return;

		LinkedSlot.State = state;
		SetDirty();
		Pawn?.GetComp<Comp_TSCosmetics>()?.NotifyUpdate();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref OverrideApparelDef, "apdef");
		Scribe_Defs.Look(ref StyleDef, "stdef");
		Scribe_Defs.Look(ref BodyDef, "bddef");
		Scribe_Defs.Look(ref RaceDef, "race");
		Scribe_Deep.Look(ref LinkedSlot, "slotlink");
		Scribe_Values.Look(ref Color, "clr");
	}

	public override void DrawIcon(Rect rect)
	{
		Widgets.ThingIcon(rect, GetApparel());
	}

	public override Color? GetDefaultColor()
	{
		if (GetApparelDef() is ThingDef def)
			return GenStuff.DefaultStuffFor(def).stuffProps.color;
		return null;
	}

	public override bool DrawHeaderOptions(WidgetRow row, Window_TransformEditor editor)
	{
		var changed = false;
		if (row.ButtonText("change apparel".ModTranslate()))
		{
			Find.WindowStack.Add(new Window_ApparelSelection(
				editor.Pawn,
				editor.Set!,
				window => window.GetChangeFunc(this),
				LinkedSlot is not null
			));
		}
		return base.DrawHeaderOptions(row, editor) || changed;
	}

	public override bool DrawOverallSettings(Window_TransformEditor editor, Listing_Standard listing)
	{
		var changed = base.DrawOverallSettings(editor, listing);
		changed = editor.DrawBodySelection(listing, ref BodyDef) || changed;
		if (CosmeticsSettings.IsHARLoaded)
			changed = HARInspectorHelper.DrawRaceSelection(
				listing,
				ref RaceDef, Pawn!
			) || changed;

		changed = DrawColorEditor(listing, editor, ref Color) || changed;

		return changed;
	}
}
