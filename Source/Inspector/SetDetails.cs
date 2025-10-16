using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
using Cosmetics.Data;
using Cosmetics.Util;
using Cosmetics.Windows;
using RimWorld;
using TS_Lib.Util;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Cosmetics.Inspector;

public static class SetDetails
{
	const float ITEM_HEIGHT = 30;
	const float ITEM_GAP = 1;
	const float SPLITTER_SIZE = 6;
	const float PASTE_HEIGHT = 30;

	private static float MeasureList<T>(this IEnumerable<T> list)
		=> list.Count() * (ITEM_HEIGHT + ITEM_GAP);

	public static bool Draw(
		Rect rect,
		Comp_TSCosmetics comp,
		TSUtil.ScrollPosition set_scroll,
		CosmeticApparel? selected
	)
	{
		var changed = false;
		var active_set = comp.EditingSet;
		using var list = new TSUtil.Listing_D(rect);

		if (active_set is null)
		{
			list.Listing.Label("no set selected".ModTranslate());
			return false;
		}

		{   // name edit
			var name_edit_rect = list.Listing.Labled(27, "name edit", CosmeticsUtil.ModTranslate);
			active_set.Name = Widgets.TextField(name_edit_rect, active_set.Name);
		}

		// states
		CosmeticsUtil.StateDefs.DrawAsColoredButtons(
			list.Listing.GetRect(40),
			active_set.States.Contains,
			active_set.ToggleState,
			def => def.shortLabel,
			def => def.description
		);

		DrawSetLists(list.Listing.GetRemaining(), comp, active_set, set_scroll, selected);

		return changed;
	}

	public static bool DrawSlotList(
		Listing listing,
		Comp_TSCosmetics comp,
		CosmeticSet set
	) {
		bool changed = false;

		var head_row = listing.GetRect(Text.LineHeight).LabeledRow("actual worn", CosmeticsUtil.ModTranslate);
		listing.GapLine(SPLITTER_SIZE);

		void _draw_existing_override(Rect rect, CosmeticApparel ap)
		{
			if (ap.LinkedSlot is null)
			{
				Widgets.Label(rect, "ERROR: Linked slot missing");
				return;
			}

			var right = rect.RightHalf();

			var icon_offset = rect.height + 3;
			if ((ap.OverrideApparelDef ?? ap.LinkedSlot.GetApparelFor(set.Pawn)?.def ?? ap.GetApparel()?.def) is ThingDef def)
				Widgets.DefIcon(
					rect.LeftPartPixels(rect.height),
					def,
					color: ap.GetColor()
				);
			rect = rect.ShrinkLeft(icon_offset);
			var slot_text = ap.LinkedSlot.Def.LabelCap;
			var text_size = Text.CalcSize(slot_text);
			Widgets.Label(rect, slot_text);
			rect = rect.ShrinkLeft(text_size.x);

			if (ap.OverrideApparelDef is not null)
			{
				slot_text = ap.OverrideApparelDef.LabelCap;
				text_size = Text.CalcSize(slot_text);
				Widgets.Label(rect, $" -> {slot_text}");
				rect = rect.ShrinkLeft(text_size.x);
			}

			rect = rect.ShrinkLeft(10);

			right.DrawEnumAsButtons<CosmeticApparel.LinkedSlotData.StateType>(
				state => state == ap.LinkedSlot.State,
				ap.SetSlotState,
				state => $"{state}.label".ModTranslate(),
				state => $"{state}.desc".ModTranslate(),
				size_ratio: 1.5f
			);

			if (Widgets.ButtonImage(
				rect.RightPartPixels(rect.height),
				TexButton.CloseXSmall
			))
			{
				set.OverriddenWorn.Remove(ap);
				set.NotifyUpdate();
			}
		}
		void _draw_non_override(Rect rect, ClothingSlotDef slot)
		{
			var worn_apparel = slot.GetEquippedItemFor(comp.Pawn);
			var icon_offset = rect.height + 3;
			Widgets.DefIcon(
				rect.LeftPartPixels(rect.height),
				worn_apparel?.def,
				color: worn_apparel?.DrawColor
			);
			rect = rect.ShrinkLeft(icon_offset);
			var w_row = rect.LabeledRow(slot.LabelCap);
			if (w_row.ButtonIcon(TexButton.Add))
			{
				set.OverriddenWorn.Add(new(set.Pawn, slot));
				set.NotifyUpdate();
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
		}

		var height = CosmeticsUtil.ClothingSlots.MeasureList();
		changed = changed || listing.GetRect(height).DrawDraggableList(
			CosmeticsUtil.ClothingSlots,
			draw_fun: (layer, rect) => {
				if (set.OverriddenWorn.FirstOrDefault(x => x.LinkedSlot?.Def == layer) is CosmeticApparel ap)
					_draw_existing_override(rect, ap);
				else
					_draw_non_override(rect, layer);
			},
			click_fun: layer =>
			{
				if (set.OverriddenWorn.FirstOrDefault(x => x.LinkedSlot?.Def == layer) is CosmeticApparel ap)
					Find.WindowStack.Add(new Window_TransformEditor(comp.Pawn, ap, set));
			},
			color_fun: layer => set.OverriddenWorn.Any(x => x.LinkedSlot?.Def == layer)
				? Color.cyan
				: Color.gray
			,
			button_size: layer => set.OverriddenWorn.Any(x => x.LinkedSlot?.Def == layer)
				? 0.5f
				: 0
			,
			height: ITEM_HEIGHT,
			gap: ITEM_GAP,
			no_drag: true
		);

		return changed;
	}

	public static bool DrawAdditionalApparel(
		Listing listing,
		Comp_TSCosmetics comp,
		CosmeticSet set,
		CosmeticApparel? selected
	)
	{
		var changed = false;
		var apparel_row = listing.GetRect(Text.LineHeight).LabeledRow("apparel", CosmeticsUtil.ModTranslate);
		if (apparel_row.ButtonIcon(TexButton.Add))
		{
			Find.WindowStack.Add(new Window_ApparelSelection(comp.Pawn, set));
		}
		listing.GapLine(SPLITTER_SIZE);
		changed = listing.GetRect(set.Apparel.MeasureList()).DrawDraggableList(
			set.Apparel,
			(ap, rect) =>
			{
				var icon_width = rect.height;
				Widgets.ThingIcon(
					rect.LeftPartPixels(icon_width),
					ap.GetApparel()
				);
				rect = rect.ShrinkLeft(icon_width);
				using (new TSUtil.TextAnchor_D(TextAnchor.MiddleLeft))
					Widgets.Label(rect, ap.OverrideApparelDef?.LabelCap);

				var button_rects = rect.RectsIn(reverse: true).GetEnumerator();
				if (button_rects.Next().ButtonImage(TexButton.CloseXSmall))
				{
					set.Apparel.Remove(ap);
					set.NotifyUpdate();
				}
				if (button_rects.Next().ButtonImage(TexButton.Copy))
				{
					Clipboard<CosmeticApparel>.SetValue(ap.CreateCopy());
				}
			},
			is_active: ap => ap == selected,
			click_fun: ap => Find.WindowStack.Add(new Window_TransformEditor(comp.Pawn, ap, set)),
			height: ITEM_HEIGHT,
			gap: ITEM_GAP,
			button_size: _ => 0.9f
		) || changed;

		var bottom_row = listing.Row(PASTE_HEIGHT);
		if (Clipboard<CosmeticApparel>.TryGetValue(out var clip)
			&& bottom_row.ButtonIcon(TexButton.Paste))
		{
			set.Apparel.Add(clip.CreateCopy(set.Pawn));
			changed = true;
		}

		return changed;
	}

	public static bool DrawSetLists(
		Rect rect,
		Comp_TSCosmetics comp,
		CosmeticSet set,
		TSUtil.ScrollPosition list_scroll,
		CosmeticApparel? selected
	)
	{
		var changed = false;

		float label_height = Text.LineHeightOf(Text.Font);

		float apparel_height = set.Apparel.MeasureList()
			+ label_height + SPLITTER_SIZE
		;

		float worn_height = CosmeticsUtil.ClothingSlots.MeasureList()
			+ label_height + SPLITTER_SIZE
		;

		float total_height = worn_height + apparel_height;
		if (Clipboard<CosmeticApparel>.HasValue)
			total_height += PASTE_HEIGHT;

		var content_rect = new Rect(
			0,
			0,
			rect.width - TSUtil.ScrollbarSize,
			total_height
		);

		using var _ = new TSUtil.Scroll_D(rect, content_rect, list_scroll);
		using var list = new TSUtil.Listing_D(content_rect.GrowBottom(1000));

		changed = DrawSlotList(list.Listing, comp, set) || changed;
		changed = DrawAdditionalApparel(list.Listing, comp, set, selected) || changed;

		return changed;
	}
}
