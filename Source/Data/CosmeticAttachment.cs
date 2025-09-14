using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
using Cosmetics.Util;
using Cosmetics.Windows;
using RimWorld;
using TS_Lib.Transforms;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace Cosmetics.Data;

public abstract class CosmeticAttachment : IExposable
{
	public const float COLOR_RECT_SIZE = 75;

    public TSTransform4 Transform;
    public Pawn? Pawn;
	public float OverallScale = 1;

	public CosmeticAttachment()
	{
		Transform ??= new();
	}
	public CosmeticAttachment(Pawn? pawn) : this()
	{
		Pawn = pawn;
	}

	public CosmeticAttachment SetPawn(Pawn? pawn)
	{
		Pawn = pawn;
		return this;
	}

	public virtual TSTransform4 GetTransform() => Transform;

	public abstract void DrawIcon(Rect rect);
	public abstract string Label { get; }
	public abstract string EditorKey { get; }
	public virtual Comp_TSCosmetics.CompUpdateNotify UpdateFlags => Comp_TSCosmetics.CompUpdateNotify.None;

	public TSTransform GetTransformFor(Rot4 rot) => GetTransform().ForRot(rot);

	public virtual bool DrawOverallSettings(
		Window_TransformEditor editor,
		Listing_Standard listing
	)
	{
		return listing.SliderLabeledWithValue(
			ref OverallScale,
			"overall scale".ModTranslate(),
			0, 2,
			editor.EditBuffers,
			resetval: 1
		);
	}

	public virtual void SetDirty() { }

	public virtual bool DrawTransformSettings(
		Window_TransformEditor editor,
		Listing_Standard listing,
		Rot4 rotation
	)
	{
		var rottr = GetTransform().ForRot(rotation);
		var changed = false;
		changed = listing.SliderLabeledWithValue(ref rottr.RotationOffset, "side rotation".ModTranslate(), -360, 360, editor.EditBuffers, resetval: 0) || changed;
		listing.Label("scale".ModTranslate());
		changed = listing.SliderLabeledWithValue(ref rottr.Scale.x, "side scale x".ModTranslate(), 0, 2, editor.EditBuffers, resetval: 1) || changed;
		changed = listing.SliderLabeledWithValue(ref rottr.Scale.y, "side scale y".ModTranslate(), 0, 2, editor.EditBuffers, resetval: 1) || changed;
		listing.Label("position".ModTranslate());
		changed = listing.SliderLabeledWithValue(ref rottr.Offset.x, "side offset x".ModTranslate(), -1, 1, editor.EditBuffers, resetval: 0) || changed;
		changed = listing.SliderLabeledWithValue(ref rottr.Offset.z, "side offset y".ModTranslate(), -1, 1, editor.EditBuffers, resetval: 0) || changed;
		changed = listing.SliderLabeledWithValue(ref rottr.Offset.y, "side offset layer".ModTranslate(), -.06f, .06f, editor.EditBuffers, resetval: 0, accuracy: 0.0001f) || changed;
		return changed;
	}

	public virtual bool DrawHeaderOptions(WidgetRow row, Window_TransformEditor editor)
	{
		bool changed = false;
		if (row.ButtonText("copy transforms".ModTranslate()))
		{
			Clipboard<TSTransform4>.SetValue(Transform);
			changed = true;
		}
		if (Clipboard<TSTransform4>.TryGetValue(out var trs)
			&& row.ButtonText("paste transforms".ModTranslate()))
		{
			Transform.CopyFrom(trs);
			changed = true;
		}
		return changed;
	}

	public virtual bool DrawSideSpecificOptions(WidgetRow row, Window_TransformEditor editor, Rot4 side)
	{
		bool changed = false;
		if (row.ButtonText("mirror".ModTranslate()))
		{
			GetTransformFor(side.Opposite).CopyFrom(GetTransformFor(side).Mirror());
			changed = true;
		}
		if (row.ButtonText("copy transforms".ModTranslate()))
		{
			Clipboard<TSTransform>.SetValue(GetTransformFor(side));
			changed = true;
		}
		if (Clipboard<TSTransform>.TryGetValue(out var tr)
			&& row.ButtonText("paste transforms".ModTranslate()))
		{
			GetTransformFor(side).CopyFrom(tr);
			changed = true;
		}
		return changed;
	}

	public virtual Color? GetDefaultColor() => Color.grey;

	public bool DrawColorEditor(Listing_Standard listing, Window_TransformEditor editor, ref Color? color_ref)
	{
		var changed = false;
		var has_value = color_ref.HasValue;
		listing.CheckboxLabeled(
			"override color".ModTranslate(),
			ref has_value
		);
		if (color_ref.HasValue != has_value)
		{
			changed = true;
			color_ref = has_value ? (GetDefaultColor() ?? Color.grey) : null;
		}
		if (!color_ref.HasValue)
			return changed;

		var color = color_ref.Value;
		var prev_clr = color;

		var row = listing.Row(Text.LineHeight);
		if (row.ButtonText("default color".ModTranslate()))
		{
			color = GetDefaultColor() ?? Color.grey;
		}
		if (row.ButtonText("material color".ModTranslate()))
		{
			TSUtil.Menu(
				CosmeticsUtil.Materials,
				mat => ChangeNotifyVal<CosmeticAttachment, Color?>.Notify(this, mat.stuffProps.color),
				mat => mat.LabelCap,
				mat => mat
			);
		}
		if (ChangeNotifyVal<CosmeticAttachment, Color?>.TryConsume(this, out var clr))
		{
			color = clr.Value;
		}

		var colorrect = listing.GetRect(COLOR_RECT_SIZE);
		Widgets.DrawWindowBackground(colorrect);
		colorrect = colorrect.ExpandedBy(-3);

		using (new TSUtil.TextAnchor_D(TextAnchor.MiddleLeft))
			Widgets.Label(colorrect, "color".ModTranslate());

		var wheelrect = colorrect
			.LeftHalf()
			.RightHalf()
			.Square();

		using (var drag = new TSUtil.EditBuffer_D<CosmeticAttachment, bool>(this, () => false))
			Widgets.HSVColorWheel(wheelrect.Move(wheelrect.width / 2), ref color, ref drag.Ref);

		Color.RGBToHSV(color, out var h, out var s, out var v);
		Widgets.HorizontalSlider(colorrect.RightHalf(), ref v, new(0, 1), "brightness".ModTranslate());
		color = Color.HSVToRGB(h, s, v);
		using (var buf = new TSUtil.EditBuffer_D<CosmeticAttachment, string>(this, () => string.Empty))
		{
			buf.Ref = Widgets.TextField(wheelrect.Move(-(wheelrect.width / 2)), buf.Ref);
			if (ColorUtility.TryParseHtmlString($"#{buf.Ref}", out var bclr)
				&& bclr != prev_clr)
				color = bclr;

			changed = prev_clr != color || changed;

			if (changed)
			{
				buf.Ref = ColorUtility.ToHtmlStringRGB(color);
			}
		}
		color_ref = color;
		return changed;
	}

	public virtual void ExposeData()
	{
		Scribe_Deep.Look(ref Transform, "tf");
		Scribe_References.Look(ref Pawn, "pawn");
		Scribe_Values.Look(ref OverallScale, "scale");
	}
}
