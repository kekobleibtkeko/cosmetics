using System;
using System.Collections.Generic;
using System.Linq;
using FuzzySharp;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Transmogged;
#nullable enable

public class ITab_Pawn_Transmogged : ITab
{
	public const float WIDTH = 600f;
	public const float HEIGHT = 500f;
	public const float MARGIN_X = 16f;

	public override bool IsVisible => SelPawn?.RaceProps.Humanlike == true;
	public Vector2 SetScrollPos;
	public Vector2 ApparelScrollPos;

	public TRApparel? SelectedApparel;

	public ITab_Pawn_Transmogged()
	{
		size = new Vector2(WIDTH, HEIGHT);
		labelKey = "Transmogged.Transmogged".Translate();
	}

	public override void FillTab()
	{
		if (SelPawn == null)
		{
			return;
		}
		if (!SelPawn.TryGetComp<Comp_Transmogged>(out var comp))
		{
			return;
		}

		var active = comp.Enabled;

		Rect tabrect	= new(MARGIN_X, 0, WIDTH - (MARGIN_X * 2), HEIGHT);
		Rect postlinerect;

		var centerlist = new Listing_Standard();
		centerlist.Begin(tabrect);
			Text.Font = GameFont.Medium;
			centerlist.Label("Transmogged.Transmogged".Translate());
			Text.Font = GameFont.Tiny;
			centerlist.Label("May the most mogged maggot win");

			Text.Font = GameFont.Small;
			centerlist.CheckboxLabeled("Transmogged.Enabled".Translate(), ref active, 0);
			centerlist.GapLine();
			postlinerect = centerlist.GetRect(0);
		centerlist.End();
		
		var sideoffset = 70;
		tabrect
			.ShrinkTop(postlinerect.y)
			.SplitVerticallyWithMargin(out var left, out var right, 4);

		left	= left.GrowRight(sideoffset);
		right	= right.ShrinkLeft(sideoffset);

		DrawSetListUI(right, comp);
		DrawActiveSetUI(left, comp);

		comp.SetEnabled(active);
	}

	public void DrawSetListUI(Rect rect, Comp_Transmogged comp)
	{
		Text.Font = GameFont.Medium;
		var setlist = new Listing_Standard();
		{
			setlist.Begin(rect);
			var labelrect = setlist.Label("Transmogged.ApparelSets".Translate());
			setlist.GapLine();

			Text.Font = GameFont.Tiny;
			var buttonrow = new WidgetRow(labelrect.x + (labelrect.width * .5f), labelrect.y, growDirection: UIDirection.RightThenDown);
			if (buttonrow.ButtonText("Transmogged.NewSet".Translate()))
			{
				var nset = new TRApparelSet(comp.Pawn){ Name = $"Set {comp.ApparelSets.Count + 1}"};
				comp.ApparelSets.Add(nset);
				comp.EditingSet = nset;
			}
			if (buttonrow.ButtonText("Transmogged.LoadSet".Translate()))
			{
				Find.WindowStack.Add(new SelectSavedApparelSetWindow(savedset => {
					comp.ApparelSets.Add(savedset.CreateCopy().For(comp.Pawn));
					comp.NotifyUpdate();
				}));
			}

			var itemmargin = 4;
			var itemheight = Text.LineHeightOf(GameFont.Small) + (itemmargin * 2);
			var itemsheight = itemheight * comp.ApparelSets.Count;
			var setscrollrect = new Rect(0, 0, rect.width - 16, itemsheight);

			Text.Font = GameFont.Small;
			// setlist.Label($"Set count: {comp.ApparelSets.Count}");
			// setlist.Label($"Height: {itemsheight} ({Text.LineHeightOf(GameFont.Small)})");

			var contentheight = HEIGHT - setlist.curY - 150;
			Rect setcontentrect = setlist.GetRect(contentheight);


			Widgets.BeginScrollView(setcontentrect, ref SetScrollPos, setscrollrect);
				for (int i = 0; i < comp.ApparelSets.Count; i++)
				{
					var y = i * itemheight;
					var setrect = new Rect(
						itemmargin,
						y + itemmargin,
						rect.width - 16 - (itemmargin * 2),
						itemheight - (itemmargin * 2)
					);
					
					setrect.SplitVertically(20, out var left, out var right);
					left = left.RightPart(.9f);
					left = left.LeftPart(.8f);

					var set = comp.ApparelSets[i];
					if (Widgets.ClickedInsideRect(right))
					{
						if (comp.EditingSet != set)
						{
							SoundDefOf.RowTabSelect.PlayOneShotOnCamera();
						}
						comp.EditingSet = set;
					}
					Widgets.DrawOptionBackground(setrect, comp.EditingSet == set);

					var label = new FText(set.Name);
					if (set.State.HasFlag(TRState.Disabled))
						label = label.Clr(Color.gray);
					Widgets.Label(right, label);

					var smallrectsize = 2;
					var statei = 0;
					foreach (TRState state in Enum.GetValues(typeof(TRState)))
					{
						if (state == TRState.Disabled)
							continue;

						bool active = (state & set.State) != 0;
						float darken = active ? 0 : .7f;

						var staterect = new Rect(right.x + (statei * (smallrectsize + 2)), setrect.y + setrect.height - 4, smallrectsize, smallrectsize);

						Widgets.DrawBoxSolid(staterect, state.ToColor().Darken(darken));
						statei++;
					}
					

					if (i > 0 && Widgets.ButtonImage(left.TopHalf(), TexButton.ReorderUp))
					{
						comp.ApparelSets.Swap(i, i - 1);
					}
					if (i < comp.ApparelSets.Count - 1 && Widgets.ButtonImage(left.BottomHalf(), TexButton.ReorderDown))
					{
						comp.ApparelSets.Swap(i, i + 1);
					}

					var actionbuttonrect = right.RightPartPixels(right.height).ExpandedBy(-1);
					if (Widgets.ButtonImage(actionbuttonrect, TexButton.Delete))
					{
						if (set == comp.EditingSet)
						{
							comp.EditingSet = null;
						}
						comp.ApparelSets.Remove(set);
						comp.NotifyUpdate();
					}

					actionbuttonrect = actionbuttonrect.Move(-(right.height + 3));
					if (Widgets.ButtonImage(actionbuttonrect, TexButton.Copy))
					{
						comp.CopySet(set);
					}
				}
			Widgets.EndScrollView();
		}
		setlist.End();
	}

	public void DrawActiveSetUI(Rect rect, Comp_Transmogged comp)
	{
		if (comp.EditingSet == null)
			return;
		
		var changed = false;
		Text.Font = GameFont.Small;
		var staterectsize = 27;

		var activelist = new Listing_Standard();
		activelist.Begin(rect);
		{
			float extragap = 0;
			int i = 0;
			Rect staterect;
			foreach (TRState state in Enum.GetValues(typeof(TRState)))
			{
				bool active = state == TRState.None
					? (comp.EditingSet.State == 0)
				 	: ((state & comp.EditingSet.State) != 0);

				float darken = active ? 0 : .6f;
				float saturation = active ? 1 : .5f;

				extragap += state switch
                {
                    TRState.NonDrafted
						or TRState.Indoors
						or TRState.Cold
						or TRState.Sleep
						or TRState.Additive => 5,
					TRState.None
						or TRState.Drafted
						or TRState.Outdoors
						or TRState.Hot
						or TRState.Disabled
						or _ => 0
                };

				staterect = new Rect((i * staterectsize) + extragap, 0, staterectsize, staterectsize);
				var visrect = staterect.ExpandedBy(-2);

				Widgets.DrawBoxSolidWithOutline(
					visrect,
					state.ToColor().Darken(darken + .1f).Saturate(saturation),
					state.ToColor().Darken(darken      ).Saturate(saturation)
				);
				if (Mouse.IsOver(visrect))
				{
					Color prev = GUI.color;
					GUI.color = state.ToColor();
					Widgets.DrawBox(staterect, 2);
					GUI.color = prev;
				}
				if (Widgets.ClickedInsideRect(staterect))
				{
					comp.EditingSet.StateToggled(state);
				}
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(visrect, state.ToString().Substring(0, 1));
				Text.Anchor = TextAnchor.UpperLeft;
				TooltipHandler.TipRegion(staterect, $"Transmogged.{state}_tooltip".Translate());
				i++;
			}

			activelist.Gap(staterectsize + 1);
			var entryrect = activelist.GetRect(Text.LineHeightOf(Text.Font));
			Widgets.Label(entryrect, "Transmogged.SetName".Translate());
			comp.EditingSet.Name = Widgets.TextField(entryrect.RightHalf(), comp.EditingSet.Name);
			activelist.GapLine();

			Text.Font = GameFont.Tiny;
			var row = new WidgetRow(activelist.curX, activelist.curY, growDirection: UIDirection.RightThenDown);
			if (row.ButtonText("Transmogged.CopyFromApparel".Translate()))
			{
				if (!KeyBindingDefOf.ModifierIncrement_10x.IsDownEvent) // ctrl?
				{
					comp.EditingSet.Apparel.Clear();
				}
				foreach (var item in comp.Pawn.apparel.wornApparel)
				{
					comp.EditingSet.AddFromApparel(item);
				}
			}
			if (row.ButtonText("Transmogged.AddItem".Translate()))
			{
				Find.WindowStack.Add(new AddApparelWindow(SelPawn, comp.EditingSet));
			}
			if (row.ButtonIcon(TexButton.Copy))
			{
				TransmoggedUtility.SetClipboard = comp.EditingSet.CreateCopy();
			}
			if (TransmoggedUtility.SetClipboard is not null && row.ButtonIcon(TexButton.Paste))
			{
				foreach (var ap in TransmoggedUtility.SetClipboard.Apparel)
				{
					comp.EditingSet.Apparel.Add(ap.CreateCopy().For(comp.Pawn));
				}
				changed = true;
			}
			if (row.ButtonIcon(TexButton.Save))
			{
				Find.WindowStack.Add(new TextPrompt("Transmogged.SaveSetTitle".Translate(), name => {
					TransmoggedSave.Instance.SaveSet(name, comp.EditingSet.For(null!), true);
				}));
			}
			activelist.Gap(20);
		}
		activelist.End();

		var apparelitemheight = 30;
		Text.Font = GameFont.Small;
		var scrollbounds = rect.ShrinkTop(activelist.curY);
		var scrollcontent = new Rect(0, 0, scrollbounds.width - 16, apparelitemheight * comp.EditingSet.Apparel.Count);

		if (TransmoggedUtility.ApparelClipboard is not null)
		{
			scrollcontent.height += apparelitemheight;
		}

		var contentlist = new Listing_Standard();
		Widgets.BeginScrollView(scrollbounds, ref ApparelScrollPos, scrollcontent);
		contentlist.Begin(scrollcontent);
		{
			for (int i = 0; i < comp.EditingSet.Apparel.Count; i++)
			{
				var item = comp.EditingSet.Apparel[i];
				var itemrect = contentlist.GetRect(apparelitemheight);

				var itemrectinner = itemrect.ExpandedBy(-2);
				Widgets.DrawOptionBackground(itemrectinner, item == SelectedApparel);
				
				var reorderwidth = itemrectinner.height - 2;
				var reorderrect = itemrectinner.LeftPartPixels(reorderwidth);

				if (i > 0 && Widgets.ButtonImage(reorderrect.TopHalf(), TexButton.ReorderUp))
				{
					comp.EditingSet.Apparel.Swap(i, i - 1);
					changed = true;
				}
				if (i < comp.EditingSet.Apparel.Count - 1 && Widgets.ButtonImage(reorderrect.BottomHalf(), TexButton.ReorderDown))
				{
					comp.EditingSet.Apparel.Swap(i, i + 1);
					changed = true;
				}

				var offset = reorderwidth + 3;
				Widgets.DefIcon(
					itemrect.ShrinkLeft(offset).LeftPartPixels(apparelitemheight),
					item.ApparelDef,
					thingStyleDef: item.StyleDef,
					color: item.Color
				);
				Widgets.Label(
					itemrectinner.ShrinkLeft(apparelitemheight + 3 + offset),
					item.ApparelDef.LabelCap
				);

				Rect deleterect;
				if (Widgets.ButtonImage(deleterect = itemrectinner.RightPartPixels(itemrectinner.height), TexButton.Delete))
				{
					comp.EditingSet.RemoveApparel(item);
				}
				else if (Widgets.ButtonImage(deleterect.Move(-(itemrectinner.height + 4)), TexButton.Copy))
				{
					TransmoggedUtility.ApparelClipboard = item.CreateCopy();
				}
				else if (Widgets.ClickedInsideRect(itemrect))
				{
					SelectedApparel = item;
					Find.WindowStack.Add(new EditApparelWindow(comp.Pawn, item, comp.EditingSet));
				}
			}
		}

		if (TransmoggedUtility.ApparelClipboard is not null
			&& contentlist.ButtonImage(TexButton.Paste, apparelitemheight, apparelitemheight))
		{
			comp.EditingSet.Apparel.Add(TransmoggedUtility.ApparelClipboard.CreateCopy(comp.Pawn));
			changed = true;
		}

		if (changed)
		{
			comp.NotifyUpdate();
		}
		
		contentlist.End();
		Widgets.EndScrollView();
	}
}

public class SelectSavedApparelSetWindow : Window
{
	public Action<TRApparelSet> SelectedAction;
    public Vector2 ScrollPosition;

	public SelectSavedApparelSetWindow(Action<TRApparelSet> selectedAction)
    {
        SelectedAction = selectedAction;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
		var list = new Listing_Standard();

		var itemheight = 30;
		var saved = TransmoggedSave.Instance.SavedSets;
		var itemsrect = new Rect(0, 0, inRect.width - 50, itemheight * saved.Count);

		list.Begin(inRect);
		{
			list.Label("Transmogged.LoadSetTitle".Translate());
			var scrollboundsrect = list.GetRect(200);
			var scrolllist = new Listing_Standard();
			Widgets.BeginScrollView(scrollboundsrect, ref ScrollPosition, itemsrect);
			scrolllist.Begin(itemsrect.Move(0, scrollboundsrect.y));
			{
				foreach (var (name, set) in saved)
				{
					var itemrect = scrolllist.GetRect(itemheight);
					var visrect = itemrect.ExpandedBy(-2);
					Widgets.DrawOptionBackground(visrect, false);
					Widgets.Label(visrect.Move(3), name);
					var iconrect = visrect.RightPartPixels(visrect.height);
					foreach (var ap in set.Apparel)
					{
						Widgets.ThingIcon(iconrect, ap.GetApparel());
						iconrect = iconrect.Move(-(visrect.height + 3));
					}

					if (Widgets.ClickedInsideRect(itemrect))
					{
						Select(set);
					}
				}
			}
			scrolllist.End();
			Widgets.EndScrollView();
		}
		list.End();
    }

	public void Select(TRApparelSet set)
	{
		SelectedAction(set);
		Close(true);
	}
}

public class EditApparelWindow : Window
{
	public Pawn Pawn { get; }
    public TRApparel Apparel { get; }
    public TRApparelSet Set { get; }

	public bool DraggingHSVWheel;

    public TRTransform? SelectedTransform;

	private string? Buffer_ApScale;
	private string? Buffer_SideRotation;
	private string? Buffer_SideScaleX;
	private string? Buffer_SideScaleY;
	private string? Buffer_SideOffsetX;
	private string? Buffer_SideOffsetY;
	private string? Buffer_LayerOffset;

	public static Lazy<IEnumerable<BodyTypeDef?>> BodyTypes = new(() => DefDatabase<BodyTypeDef>.AllDefsListForReading);

	public override Vector2 InitialSize => new(750, 850);

	public EditApparelWindow(Pawn pawn, TRApparel apparel, TRApparelSet set)
	{
        Pawn = pawn;
        Apparel = apparel;
        Set = set;

        preventCameraMotion = false;
        draggable = true;
        doCloseX = true;
    }


    public override void DoWindowContents(Rect inRect)
    {
		var siderectheight = 100;
		var buttonrectinner = 90;
		var colorrectsize = 75;
		var leftbarsize = 150;

		bool changed = false;

        var list = new Listing_Standard();
		var mainrect = inRect.ShrinkLeft(leftbarsize);
		list.Begin(mainrect);
		{
			Text.Font = GameFont.Medium;
			var labrect = list.Label("Transmogged.ApparelSettings".Translate());

			var btnrow = new WidgetRow(labrect.RightHalf().x, labrect.y, UIDirection.RightThenDown);
			if (btnrow.ButtonIcon(TexButton.Copy))
			{
				TransmoggedUtility.OffsetClipboard = Apparel.CreateCopy();
			}
			if (TransmoggedUtility.OffsetClipboard is not null
				&& btnrow.ButtonIcon(TexButton.Paste))
			{
				Apparel.CopyTransforms(TransmoggedUtility.OffsetClipboard);
				changed = true;
			}

			list.GapLine();
			Text.Font = GameFont.Small;
			list.Label("Transmogged.OverallApparelSettings".Translate());
			list.GapLine();

			changed = changed || list.SliderLabeledWithValue(ref Apparel.Scale, "Transmogged.ApparelScale".Translate(), 0, 2, ref Buffer_ApScale, resetval: 1);

			
			var lrect = list.Label("Transmogged.BodyType".Translate());
			TooltipHandler.TipRegionByKey(lrect, "Transmogged.BodyTypeTT");
			var bodyrectsize = 45;
			var bodyrect = list.GetRect(bodyrectsize).LeftPartPixels(bodyrectsize);
			
			foreach (var btd in BodyTypes.Value.Prepend(null))
			{
				var bodyvisrect = bodyrect.ExpandedBy(-4);
				Widgets.DrawOptionBackground(bodyvisrect, Apparel.BodyDef == btd);
				if (Widgets.ButtonInvisible(bodyrect))
				{
					changed = true;
					Apparel.BodyDef = btd;
				}
				TooltipHandler.TipRegion(bodyrect, btd?.defName ?? "Default");

				if (btd is not null)
				{
					try
					{
						var rot = Rot4.South;
						Graphic_Multi bodygraph = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(btd.bodyNakedGraphicPath, ShaderDatabase.CutoutSkin, bodyvisrect.size, Color.gray);
						Widgets.DrawTextureFitted(bodyvisrect, bodygraph.MatAt(rot).mainTexture, 1.2f);
					}
					catch (System.Exception)
					{
						// no idea what fuckyness modded bodies may cause
					}
				}

				bodyrect = bodyrect.Move(bodyrectsize);
			}

			{
				var colorrect = list.GetRect(colorrectsize);
				Widgets.DrawWindowBackground(colorrect);
				colorrect = colorrect.ExpandedBy(-3);
				Text.Font = GameFont.Small;
				Color prevclr = Apparel.Color;
				var colorlabel = "Transmogged.Color".Translate();

				using (new TransmoggedUtility.TextAnchor_D(TextAnchor.MiddleLeft))
					Widgets.Label(colorrect, colorlabel);

				var wheelrect = colorrect
					.LeftHalf()
					.RightHalf()
					.Square();

				Widgets.HSVColorWheel(wheelrect, ref Apparel.Color, ref DraggingHSVWheel);
				

				Color.RGBToHSV(Apparel.Color, out var h, out var s, out var v);
				Widgets.HorizontalSlider(colorrect.RightHalf(), ref v, new(0, 1), "Transmogged.Brightness".Translate());
				Apparel.Color = Color.HSVToRGB(h, s, v);
				
				if (Apparel.Color != prevclr)
				{
					Apparel.SetApparelDirty();
					Set.NotifyUpdate();
				}
			}

			list.GapLine();
			Text.Font = GameFont.Small;
			list.Label("Transmogged.SideApparelSettings".Translate());
			list.GapLine();
		}
		list.End();

		var srbig = 60;
		var srsmall = 30;

		var siderect = new Rect(inRect.x, list.curY, inRect.width, 1000);
		siderect.SplitVertically(siderectheight + leftbarsize, out var left, out var rightbtnsrect);
		var leftbtnsrect = left
			.TopPartPixels(siderectheight)
			.ExpandedBy(buttonrectinner - siderectheight)
			.Move(leftbarsize);
		rightbtnsrect = rightbtnsrect.ShrinkLeft(5);
		
		TransmoggedData.Textures.CircleLine
			.DrawFitted(leftbtnsrect.ExpandedBy(3), Widgets.WindowBGBorderColor);

		foreach ((Rect btnrect, Rot4 btnrot) in new[]{
			(leftbtnsrect.ShrinkBottom(srbig).ShrinkLeft(srsmall).ShrinkRight(srsmall), Rot4.North),
			(leftbtnsrect.ShrinkTop(srbig).ShrinkLeft(srsmall).ShrinkRight(srsmall), Rot4.South),
			(leftbtnsrect.ShrinkLeft(srbig).ShrinkTop(srsmall).ShrinkBottom(srsmall), Rot4.East),
			(leftbtnsrect.ShrinkRight(srbig).ShrinkTop(srsmall).ShrinkBottom(srsmall), Rot4.West),
		}) {
			var tr = Apparel.GetTransformFor(btnrot);
			Widgets.DrawOptionBackground(btnrect, SelectedTransform == tr);
			if (Widgets.ButtonInvisible(btnrect) || Widgets.ClickedInsideRect(btnrect))
			{
				SelectedTransform = tr;
			}
			TransmoggedData.Textures.Arrow
				.DrawFitted(btnrect, color: Color.white, rotation: btnrot.AsAngle);
		}

		using (var ll = new TransmoggedUtility.Listing_D(left))
		{
			ll.Listing.Gap(siderectheight);
			ll.Listing.GapLine();
			ll.Listing.ButtonText("test");
		}

		// new PortraitsCache.PortraitParams().RenderPortrait()
		// 		PortraitsCache.Get()

		var sliderlist = new Listing_Standard();
		sliderlist.Begin(rightbtnsrect);
		{
			if (SelectedTransform is not null)
			{
				Text.Font = GameFont.Tiny;
				if (sliderlist.ButtonText("Transmogged.SideMirror".Translate()))
				{
					var optr = Apparel.GetTransformFor(SelectedTransform.Rotation.Opposite);
					optr.RotationOffset = 360 - SelectedTransform.RotationOffset;
					optr.Scale.y = SelectedTransform.Scale.y;
					optr.Scale.x = SelectedTransform.Scale.x;
					optr.Offset.x = -SelectedTransform.Offset.x;
					optr.Offset.z = SelectedTransform.Offset.z;
					optr.Offset.y = SelectedTransform.Offset.y;
					changed = true;
				}
				Text.Font = GameFont.Small;
				changed = changed || sliderlist.SliderLabeledWithValue(ref SelectedTransform.RotationOffset,	"Transmogged.SideRotation".Translate(), 0, 360,		ref Buffer_SideRotation,	resetval: 0);
				sliderlist.Label("Transmogged.Scale".Translate());
				changed = changed || sliderlist.SliderLabeledWithValue(ref SelectedTransform.Scale.x,			"Transmogged.SideScaleX".Translate(), 0, 2,			ref Buffer_SideScaleX,  	resetval: 1);
				changed = changed || sliderlist.SliderLabeledWithValue(ref SelectedTransform.Scale.y,			"Transmogged.SideScaleY".Translate(), 0, 2,			ref Buffer_SideScaleY,  	resetval: 1);
				sliderlist.Label("Transmogged.Position".Translate());
				changed = changed || sliderlist.SliderLabeledWithValue(ref SelectedTransform.Offset.x,			"Transmogged.SideOffsetX".Translate(), -1, 1,		ref Buffer_SideOffsetX, 	resetval: 0);
				changed = changed || sliderlist.SliderLabeledWithValue(ref SelectedTransform.Offset.z,			"Transmogged.SideOffsetY".Translate(), -1, 1,		ref Buffer_SideOffsetY, 	resetval: 0);
				changed = changed || sliderlist.SliderLabeledWithValue(ref SelectedTransform.Offset.y,			"Transmogged.LayerOffset".Translate(), -.06f,.06f,	ref Buffer_LayerOffset, 	resetval: 0, accuracy: 0.0001f);
			}
			
		}
        sliderlist.End();



		if (changed)
		{
			Apparel.SetDrawDataDirty();
			Set.NotifyUpdate();
		}
    }
}

public class AddApparelWindow : Window
{
	public static Lazy<List<ThingDef>> AllApparel = new (() => DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsApparel).ToList());
	public Lazy<List<ThingDef>> WearableApparel;
	public IEnumerable<ThingDef>? FilteredList;
	public Vector2 ScrollPosition;

	public Pawn Pawn { get; }
	public TRApparelSet Set { get; }
	public bool ShowOnlyWearable;
	public ThingDef? Selected;

    public string SearchTerm = string.Empty;

    public AddApparelWindow(Pawn pawn, TRApparelSet set)
    {
        Pawn = pawn;
		Set = set;
        WearableApparel = new(() => AllApparel.Value.Where(x => x.apparel.PawnCanWear(Pawn)).ToList());
        preventCameraMotion = false;
        draggable = true;
        doCloseX = true;
    }

	public void AddApparel(ThingDef apparel, ThingStyleDef? style = null)
	{
		var nap = Set.AddNew(apparel);
		nap.StyleDef = style;
	}

    public void AddCurrent()
	{
		if (Selected is null)
			return;
		AddApparel(Selected);
	}

    public override void DoWindowContents(Rect inRect)
    {
        var windowlist = new Listing_Standard();
		Text.Font = GameFont.Medium;
		bool searchdirty = false;
		windowlist.Begin(inRect);
		{
			windowlist.Gap();
			var labelrect = windowlist.Label("Transmogged.AddApparel".Translate());

			var prevonlywear = ShowOnlyWearable;
			Widgets.CheckboxLabeled(labelrect.RightHalf(), "Transmogged.ShowOnlyWearable".Translate(), ref ShowOnlyWearable);
			searchdirty = prevonlywear != ShowOnlyWearable;

			windowlist.GapLine();

			var prevterm = SearchTerm;
			SearchTerm = windowlist.TextEntry(SearchTerm);
			searchdirty = searchdirty || (!string.IsNullOrEmpty(prevterm) && SearchTerm != prevterm);

			windowlist.GapLine();
		}
		windowlist.End();

		var itemheight = 30;
		var fuzzyratio = 50; // TODO: make configurable

		IEnumerable<ThingDef> items = ShowOnlyWearable
			? WearableApparel.Value.AsEnumerable()
			: AllApparel.Value.AsEnumerable();
		
		if (!string.IsNullOrEmpty(SearchTerm))
		{
			var ratio = (Func<string, string, int>)Fuzz.WeightedRatio;
			if (searchdirty)
			{
				FilteredList = items
					.Select(def => (ratio(SearchTerm.ToLowerInvariant(), def.LabelCap.Resolve().ToLowerInvariant()), def))
					.Where(((int w, ThingDef def) x) => x.w > fuzzyratio)
					.OrderByDescending(((int w, ThingDef def) x) => x.w)
					.Select(((int w, ThingDef def) x) => x.def)
					.ToArray();
			}
			items = FilteredList ?? items;
		}
		
		var itemboundsrect = inRect.BottomPartPixels(inRect.height - windowlist.curY);
		var itemsrect = itemboundsrect.TopPartPixels(items.Count() * itemheight).ShrinkRight(16);

		var itemlist = new Listing_Standard(itemboundsrect, () => ScrollPosition);
		Widgets.BeginScrollView(itemboundsrect, ref ScrollPosition, itemsrect);
		itemlist.Begin(itemsrect);
		{
			for (int i = 0; i < items.Count(); i++)
			{
				var thing = items.ElementAt(i);
				var fullitemrect = itemlist.GetRect(itemheight, .4f);
				var itemrect = fullitemrect.ExpandedBy(-2);
				var height = itemrect.height;

				// Widgets.DrawOptionBackground(itemrect, Selected == thing);
				Widgets.DrawWindowBackground(itemrect);
				// if (Widgets.ClickedInsideRect(fullitemrect))
				// {
				// 	Selected = thing;
				// }

				Widgets.ThingIcon(itemrect.LeftPartPixels(height), thing);
				var label = thing.LabelCap.Resolve();
				if (!string.IsNullOrEmpty(SearchTerm))
				{
					// label += $" ({Fuzz.Ratio(SearchTerm, label)} ({Fuzz.WeightedRatio(SearchTerm, label)}))";
				}
				Widgets.Label(itemrect.RightPartPixels(itemrect.width - height - 4), label);

				Rect btnrect;
				if (Widgets.ButtonImage(btnrect = itemrect.RightPartPixels(height).Move(height + 4), TexButton.Add))
				{
					AddApparel(thing);
				}

				if (thing.CanBeStyled())
				{
					foreach (ThingStyleDef sd in DefDatabase<StyleCategoryDef>.AllDefsListForReading
						.Select(y => y.GetStyleForThingDef(thing))
						.Where(x => x?.graphicData is not null))
					{
						btnrect = btnrect.Move(height + 4);
						Widgets.DrawOptionBackground(btnrect, false);
						if (Widgets.ClickedInsideRect(btnrect))
						{
							AddApparel(thing, sd);
						}

						Widgets.ThingIcon(btnrect, thing, thingStyleDef: sd);
						TooltipHandler.TipRegion(btnrect, $"{sd.label} ({sd.defName})");
					}
				}
			}
		}
		itemlist.End();
		Widgets.EndScrollView();
    }

    public override void OnAcceptKeyPressed() => AddCurrent();
}