using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
using Cosmetics.Data;
using Cosmetics.Util;
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
        labelKey = "cosmetics".ModTranslate();
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

            title_rect.LeftPart(0.95f).DrawEnumAsButtons<Comp_TSCosmetics.CompState>(
                state => comp.Save.CompState == state,
                state => comp.Save.CompState = state,
                reverse: true
            );

            main_rect = listing.GetRemaining();
        }

        main_rect.SplitVerticallyPct(0.8f, out var left, out var right);
        SetList.Draw(right, comp, SetListScrollPosition);
        SetDetails.Draw(left, comp, SetDetailsScrollPosition);
    }

}
