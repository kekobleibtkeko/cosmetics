using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
using Cosmetics.Util;
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
			comp.NewSet();
			changed = true;
		}

		list.Listing.GetRect(400).DrawDraggableList(
			comp.Save.Sets,
			(set, set_rect) =>
			{
				using (new TSUtil.TextAnchor_D(TextAnchor.MiddleLeft))
					Widgets.Label(set_rect, set.Name);
			},
			is_active: set => comp.EditingSet == set,
			click_fun: set => comp.EditingSet = set,
			scroll_pos: scroll
		);
		return changed;
	}
}
