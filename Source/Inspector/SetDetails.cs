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
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace Cosmetics.Inspector;

public static class SetDetails
{
	const float ITEM_HEIGHT = 30;
	const float ITEM_GAP = 1;
	const float SPLITTER_SIZE = 6;

	private static float MeasureList<T>(this IEnumerable<T> list)
		=> list.Count() * (ITEM_HEIGHT + ITEM_GAP);

	public static bool Draw(Rect rect, Comp_TSCosmetics comp, TSUtil.ScrollPosition set_scroll)
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

		DrawSetLists(list.Listing.GetRemaining(), comp, active_set, set_scroll);

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

		}
		void _draw_non_override(Rect rect, CosmeticsUtil.ClothingSlot slot)
		{
			var worn_apparel = comp.Pawn.GetWornApparelBySlot(slot);
			var icon_offset = rect.height + 3;
			Widgets.DefIcon(
				rect.LeftPartPixels(rect.height),
				worn_apparel?.def,
				color: worn_apparel?.DrawColor
			);
			rect = rect.ShrinkLeft(icon_offset);
			Widgets.Label(rect, slot.ToTranslated());
		}

		var height = CosmeticsUtil.ClothingSlots.MeasureList();
		changed = changed || listing.GetRect(height).DrawDraggableList(
			CosmeticsUtil.ClothingSlots,
			(layer, rect) => {
				if (set.OverriddenWorn.FirstOrDefault(x => x.LinkedSlot == layer) is CosmeticApparel ap)
					_draw_existing_override(rect, ap);
				else
					_draw_non_override(rect, layer);
			},
			click_fun: layer =>
			{
				
			},
			color_fun: layer => set.OverriddenWorn.Any(x => x.LinkedSlot == layer)
				? Color.cyan
				: Color.gray
			,
			height: ITEM_HEIGHT,
			gap: ITEM_GAP,
			no_drag: true
		);

		return changed;
	}

	public static void DrawAdditionalApparel(
		Listing listing,
		Comp_TSCosmetics comp,
		CosmeticSet set
	) {
		var apparel_row = listing.GetRect(Text.LineHeight).LabeledRow("apparel", CosmeticsUtil.ModTranslate);
		if (apparel_row.ButtonIcon(TexButton.Add))
		{
			Find.WindowStack.Add(new Window_ApparelSelection(comp.Pawn, set));
		}
		listing.GapLine(SPLITTER_SIZE);
		listing.GetRect(set.Apparel.MeasureList()).DrawDraggableList(
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
					Widgets.Label(rect,ap.ApparelDef?.LabelCap);
			},
			click_fun: ap => Find.WindowStack.Add(new Window_TransformEditor(comp.Pawn, ap, set)),
			height: ITEM_HEIGHT,
			gap: ITEM_GAP
		);
	}

	public static bool DrawSetLists(
		Rect rect,
		Comp_TSCosmetics comp,
		CosmeticSet set,
		TSUtil.ScrollPosition list_scroll
	) { 
		var changed = false;

		float label_height = Text.LineHeightOf(Text.Font);

		float apparel_height = set.Apparel.MeasureList()
			+ label_height + SPLITTER_SIZE
		;

		float worn_height = CosmeticsUtil.ClothingSlots.MeasureList()
			+ label_height + SPLITTER_SIZE
		;

		float total_height = worn_height + apparel_height;
		var content_rect = new Rect(
			0,
			0,
			rect.width - TSUtil.ScrollbarSize,
			total_height
		);

		using var _ = new TSUtil.Scroll_D(rect, content_rect, list_scroll);
		using var list = new TSUtil.Listing_D(content_rect.GrowBottom(1000));

		DrawSlotList(list.Listing, comp, set);
		DrawAdditionalApparel(list.Listing, comp, set);

		return changed;
	}
}
