using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
using Cosmetics.Data;
using Cosmetics.Util;
using Cosmetics.Windows;
using RimWorld;
using RimWorld.Planet;
using TS_Lib.Util;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Cosmetics.Inspector;

public class ITab_Pawn_Cosmetics : ITab
{
    public const float WIDTH = 1000f;
    public const float HEIGHT = 600f;
    public const float MARGIN_X = 5;
    public const float MARGIN_Y = 4;

    public TSUtil.ScrollPosition SetListScrollPosition = new(default);
    public TSUtil.ScrollPosition SetDetailsScrollPosition = new(default);

    public override bool IsVisible => SelPawn?.RaceProps.Humanlike == true;

	public ITab_Pawn_Cosmetics()
	{
		labelKey = "Cosmetics.cosmetics";
    }

    public override void UpdateSize()
    {
        size = new(WIDTH, HEIGHT);
    }

	public override void FillTab()
	{
		if (SelPawn is null)
			return;
		if (!SelPawn.TryGetComp(out Comp_TSCosmetics comp))
			return;

		var tab_rect = new Rect(MARGIN_X, MARGIN_Y, size.x - MARGIN_X * 2, size.y - MARGIN_Y);

		Rect main_rect;
		using (var list = new TSUtil.Listing_D(tab_rect))
		{
			var listing = list.Listing;
			Rect title_rect;
			using (new TSUtil.TextSize_D(GameFont.Medium))
				title_rect = listing.Label("cosmetics".ModTranslate());
			var row = title_rect.LabeledRow(string.Empty, split: 0.2f);
			if (row.ButtonText("transforms".ModTranslate()))
			{
				Find.WindowStack.Add(new Window_TransformEditor(
					comp.Pawn,
					new CosmeticBodypartPseudo(comp.Pawn, CosmeticBodypartPseudo.BodyPartType.Head),
					null
				));
			}

			title_rect.LeftPart(0.95f).DrawEnumAsButtons<Comp_TSCosmetics.CompState>(
				state => comp.Save.CompState == state,
				comp.SetState,
				reverse: true
			);

			main_rect = listing.GetRemaining();
		}

		bool changed = false;
		main_rect.SplitVerticallyPct(0.8f, out var left, out var right);
		changed = SetList.Draw(right, comp, SetListScrollPosition) || changed;
		changed = SetDetails.Draw(left, comp, SetDetailsScrollPosition, null) || changed;

		if (changed)
			comp.NotifyUpdate();
    }

}
