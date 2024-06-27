using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Transmogged;

public class ITab_Pawn_Transmogged : ITab
{
	public const float WIDTH = 500f;
	public const float HEIGHT = 400f;
	public const float MARGIN_X = 16f;

	public override bool IsVisible => SelPawn?.RaceProps.Humanlike == true;
	public Vector2 ScrollPos;

	public ITab_Pawn_Transmogged()
	{
		size = new Vector2(WIDTH, HEIGHT);
		labelKey = "TabLabel".Translate();
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

		Rect tabrect	= new Rect(MARGIN_X, 0, WIDTH - (MARGIN_X * 2), HEIGHT);
		Rect postlinerect;

		var centerlist = new Listing_Standard();
		centerlist.Begin(tabrect);
			Text.Font = GameFont.Medium;
			centerlist.Label("Transmogged".Translate());
			Text.Font = GameFont.Tiny;
			centerlist.Label("Get mogged moron modders".Translate());

			Text.Font = GameFont.Small;
			centerlist.CheckboxLabeled("Enabled".Translate(), ref active, 0);
			centerlist.GapLine();
			postlinerect = centerlist.GetRect(0);
		centerlist.End();
		
		Rect rectleft	= new Rect(tabrect.x, postlinerect.y, tabrect.width / 2, tabrect.height - postlinerect.y);
		Rect rectright	= new Rect(tabrect.x + tabrect.width / 2, postlinerect.y, tabrect.width / 2, tabrect.height - postlinerect.y);

		DrawSetListUI(rectright, comp);
		DrawActiveSetUI(rectleft, comp);

		comp.SetEnabled(active);
	}

	public void DrawSetListUI(Rect rect, Comp_Transmogged comp)
	{
		var setlist = new Listing_Standard();
		setlist.Begin(rect);
		
			if (setlist.ButtonText("NewSet".Translate()))
			{
				comp.ApparelSets.Add(new TRApparelSet(){ Name = $"Set {comp.ApparelSets.Count + 1}"});
			}
			setlist.GapLine();

			var itemmargin = 4;
			var itemheight = Text.LineHeightOf(GameFont.Small) + (itemmargin * 2);
			var itemsheight = itemheight * comp.ApparelSets.Count;
			var setscrollrect = new Rect(0, 0, rect.width - 16, itemsheight);

			setlist.Label($"Apparel count: {comp.ApparelSets.Count}");
			setlist.Label($"Height: {itemsheight} ({Text.LineHeightOf(GameFont.Small)})");

			var contentheight = HEIGHT - setlist.curY - 150;
			Rect setcontentrect = setlist.GetRect(contentheight);


			Widgets.BeginScrollView(setcontentrect, ref ScrollPos, setscrollrect);
				for (int i = 0; i < comp.ApparelSets.Count; i++)
				{
					var y = i * itemheight;
					var setrect = new Rect(
						itemmargin,
						y + itemmargin,
						rect.width - 16 - (itemmargin * 2),
						itemheight - (itemmargin * 2)
					);
					var set = comp.ApparelSets[i];
					if (Widgets.ClickedInsideRect(setrect))
					{
						if (comp.ActiveSet != set)
						{
							SoundDefOf.ThingSelected.PlayOneShotOnCamera();
						}
						comp.ActiveSet = set;
					}
					Widgets.DrawOptionBackground(setrect, comp.ActiveSet == set);

					var labely = y + itemmargin;
					Widgets.Label(itemmargin * 2, ref labely, 100, set.Name);
				}
			Widgets.EndScrollView();
		setlist.End();
	}

	public void DrawActiveSetUI(Rect rect, Comp_Transmogged comp)
	{
		if (comp.ActiveSet == null)
			return;
		
		var rectsize = 26;

		var activelist = new Listing_Standard();
		activelist.Begin(rect);
			int i = 0;
			Rect staterect;
			foreach (TRState state in Enum.GetValues(typeof(TRState)))
			{
				bool active = state == TRState.None
					? (comp.ActiveSet.State > 0)
				 	: ((state & comp.ActiveSet.State) != 0);

				float darken = active ? 0 : .5f;

				staterect = new Rect(i * rectsize, 0, rectsize, rectsize);
				var visrect = staterect.ExpandedBy(-2);

				Widgets.DrawBoxSolidWithOutline(visrect, state.ToColor().Darken(darken + .1f), state.ToColor().Darken(darken));
				if (Mouse.IsOver(visrect))
				{
					Color prev = GUI.color;
					GUI.color = state.ToColor();
					Widgets.DrawBox(staterect, 2);
					GUI.color = prev;
				}
				if (Widgets.ClickedInsideRect(staterect))
				{
					comp.StateToggled(state);
				}
				i++;
			}


		activelist.End();
	}
}
