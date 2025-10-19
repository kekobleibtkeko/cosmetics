using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
using Cosmetics.Util;
using Cosmetics.Windows;
using TS_Lib.Util;
using UnityEngine;
using Verse;
using static TS_Lib.Util.TSUtil;

namespace Cosmetics.Inspector;

public static class SetList
{
	public static bool Draw(Rect rect, Comp_TSCosmetics comp, ScrollPosition scroll)
	{
		var changed = false;
		using var list = new TSUtil.Listing_D(rect);

		if (list.Listing.ButtonText("add new set".ModTranslate()))
		{
			var set = comp.NewSet();

			if (TSUtil.Shift)
			{
				comp.SetState(Comp_TSCosmetics.CompState.Enabled);
				comp.EditingSet = set;
				Find.WindowStack.Add(new Window_ApparelSelection(set.Pawn, set));
			}

			changed = true;
		}

		list.Listing.GetRect(400).DrawDraggableList(
			comp.Save.Sets.ToList(),
			(set, set_rect) =>
			{
				using (new TSUtil.TextAnchor_D(TextAnchor.MiddleLeft))
					Widgets.Label(set_rect, set.Name);
				var rects = set_rect.RectsIn(true).GetEnumerator();
				if (Widgets.ButtonImage(rects.Next(), TexButton.CloseXSmall))
				{
					comp.Save.Sets.Remove(set);
					comp.NotifyUpdate();
				}
			},
			is_active: set => comp.EditingSet == set,
			click_fun: set => comp.EditingSet = set,
			scroll_pos: scroll,
			button_size: set => 0.9f
		);
		return changed;
	}
}
