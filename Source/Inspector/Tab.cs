using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
using Cosmetics.Data;
using Cosmetics.Util;
using RimWorld;
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

    public TSUtil.ScrollPosition ScrollPosition = new(default);

    public override bool IsVisible => SelPawn?.RaceProps.Humanlike == true;

    public ITab_Pawn_Cosmetics()
    {
        labelKey = "Cosmetics.cosmetics".Translate();
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
                title_rect = listing.Label("Cosmetics frfr".ModTranslate());

            title_rect.LeftPart(0.95f).DrawEnumAsButtons<Comp_TSCosmetics.CompState>(
                state => comp.Save.CompState == state,
                state => comp.Save.CompState = state,
                reverse: true
            );

            main_rect = listing.listingRect;
        }

        main_rect.SplitVerticallyPct(0.8f, out var left, out var right);
        using (var list = new TSUtil.Listing_D(right))
        {
            if (list.Listing.ButtonText("Add new set".ModTranslate()))
            {
                comp.NewSet();
            }

            list.Listing.GetRect(400).DrawDraggableList(
                comp.Save.Sets,
                (set, set_rect) =>
                {
                    using (new TSUtil.TextAnchor_D(TextAnchor.MiddleLeft))
                        Widgets.Label(set_rect, set.Name);
                },
                scroll_pos: ScrollPosition
            );
        }
            
            //PawnRoleSelectionWidgetBase
            /*var drnd_group = DragAndDropWidget.NewGroup();
            var m_pos = Event.current.mousePosition;
            foreach (var set in comp.Save.Sets)
            {
                var set_rect = listing.GetRect(30);
                Widgets.DrawOptionBackground(set_rect, false);
                using (new TSUtil.TextAnchor_D(TextAnchor.MiddleLeft))
                    Widgets.Label(set_rect, set.Name);

                DragAndDropWidget.Draggable(drnd_group, set_rect, set, () => comp.EditingSet = set);

                DragAndDropWidget.DropArea(drnd_group, set_rect, drop =>
                {
                    if (drop is not CosmeticSet drop_set)
                        return;
                    comp.Save.Sets.Swap(comp.Save.Sets.IndexOf(set), comp.Save.Sets.IndexOf(drop_set));
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }, set);
                listing.Gap(5);
            */
                //DragAndDropWidget.Drop
                //if (DragAndDropWidget.DraggableAt(drnd_group, m_pos) is CosmeticSet draggin_set)
                //{
                //    if (Event.current.type == EventType.MouseUp)
                //    {

                //    }
                //}
            //}
        //}
    }
}
