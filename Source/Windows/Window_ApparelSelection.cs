using System;
using System.Collections.Generic;
using System.Linq;
using Cosmetics.Data;
using Cosmetics.Util;
using HarmonyLib;
using RimWorld;
using TS_Lib.Util;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Cosmetics.Windows;

public class Window_ApparelSelection : Window
{
	public const int FUZZY_RATIO = 50;
	public const float ITEM_HEIGHT = 30;

	public delegate void SelectDelegate(ThingDef? def, ThingStyleDef? style);

	public Lazy<List<ThingDef>> WearableApparel;
	public List<ThingDef?>? FilteredList;

	public Pawn Pawn { get; }
	public CosmeticSet Set { get; }
	public bool AllowNull { get; }

	public bool ShowOnlyWearable;
	public string SearchTerm = string.Empty;
	public TSUtil.ScrollPosition ScrollPosition = new(default);

	public SelectDelegate SelectAction;

	public Window_ApparelSelection(Pawn pawn, CosmeticSet set, Func<Window_ApparelSelection, SelectDelegate>? on_select = null, bool allow_null = false)
	{
		Pawn = pawn;
		Set = set;
		AllowNull = allow_null;
		WearableApparel = new(() => [.. CosmeticsUtil.AllApparel.Where(x => x.apparel.PawnCanWear(Pawn))]);
		SelectAction = on_select?.Invoke(this) ?? GetAddFunc();

		// Window-internal stuff
		preventCameraMotion = false;
		draggable = true;
		doCloseX = true;
	}

	public void Select(ThingDef? def, ThingStyleDef? style = null)
	{
		SelectAction(def, style);
		SoundDefOf.Designate_PlanAdd.PlayOneShotOnCamera();
		// holding ctrl allows adding multiple apparel without closing the window
		if (!TSUtil.Ctrl)
			Close(false);
	}

	public SelectDelegate GetAddFunc() => (def, style) =>
	{
		if (def is null)
		{
			return;
		}
		var added = Set.AddNewApparel(def);
		var change = GetChangeFunc(added);
		change(def, style);
	};

	public SelectDelegate GetChangeFunc(CosmeticApparel apparel) => (def, style) =>
	{
		apparel.OverrideApparelDef = def;
		apparel.StyleDef = style;
		apparel.SetDirty();
		Set.NotifyUpdate();
	};

	public override void DoWindowContents(Rect inRect)
	{
		bool search_dirty = false;
		using var list = new TSUtil.Listing_D(inRect);

		list.Listing.Gap();
		Rect header_rect;
		using (new TSUtil.TextSize_D(GameFont.Medium))
			header_rect = list.Listing.Label("select apparel".ModTranslate());

		var prev_only_wearable = ShowOnlyWearable;
		Widgets.CheckboxLabeled(
			header_rect.RightHalf(),
			"show only wearable".ModTranslate(),
			ref ShowOnlyWearable
		);
		search_dirty = prev_only_wearable != ShowOnlyWearable;

		list.Listing.GapLine();
		var prev_search = SearchTerm;
		SearchTerm = list.Listing.TextEntry(SearchTerm);
		search_dirty = search_dirty || (!string.IsNullOrEmpty(prev_search) && SearchTerm != prev_search);
		list.Listing.GapLine();

		List<ThingDef> items = ShowOnlyWearable
			? WearableApparel.Value
			: CosmeticsUtil.AllApparel
		;

		List<ThingDef?> full_items = [
			.. items
		];
		if (AllowNull)
			full_items.Insert(0, null);

		if (!string.IsNullOrEmpty(SearchTerm))
		{
			if (search_dirty)
			{
				FilteredList = [.. full_items
					.Select(def => (TSUtil.FuzzyRatio(SearchTerm, def?.label ?? "null"), def))
					.Where(((int w, ThingDef? def) x) => x.w >= FUZZY_RATIO)
					.OrderByDescending(((int w, ThingDef? _) x) => x.w)
					.Select(((int _, ThingDef? def) x) => x.def)
				];
			}
			full_items = FilteredList ?? full_items;
		}

		var content_rect = new Rect(
			0, 0,
			inRect.width,
			full_items.Count() * ITEM_HEIGHT
		);

		list.Listing.GetRemaining().DrawDraggableList(
			full_items,
			(item, rect) =>
			{
				rect.SplitVerticallyPct(0.6f, out var left, out var right, 5);
				var icon_size = rect.height;
				Widgets.DrawOptionBackground(left, false);
				if (item is not null)
				{
					Widgets.ThingIcon(left.LeftPartPixels(icon_size), item);
					left = left.ShrinkLeft(icon_size);
				}
				Widgets.Label(left, item?.LabelCap ?? "nothing".ModTranslate());
				if (Widgets.ButtonInvisible(left))
					Select(item, null);

				if (item?.CanBeStyled() == true)
				{
					DefDatabase<StyleCategoryDef>.AllDefsListForReading
						.Select(x => x.GetStyleForThingDef(item))
						.Where(x => x is not null)
						.SplitIntoSquaresGap(right)
						.Do((style, rect) =>
						{
							Widgets.DrawOptionBackground(rect, false);
							if (Widgets.ButtonInvisible(rect))
								Select(item, style);
							Widgets.ThingIcon(rect, item, thingStyleDef: style);
							TooltipHandler.TipRegion(rect, $"{style.Category?.label} ({style.defName})");
						})
					;
				}
			},
			color_fun: item => Color.clear,

			height: ITEM_HEIGHT,
			scroll_pos: ScrollPosition,
			no_drag: true
		);
	}
}