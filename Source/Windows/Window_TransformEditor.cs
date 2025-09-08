using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cosmetics.Comp;
using Cosmetics.Data;
using Cosmetics.Util;
using RimWorld;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace Cosmetics.Windows;

public class Window_TransformEditor : Window
{
	[Flags]
	public enum EditSettingType
	{
		None = 0,
		HideHair = 1 << 0,
	}

	[Flags]
	public enum TransformStateType
	{
		None = 0,
		Move = 1 << 0,
		Scale = 1 << 1,
		Rotate = 1 << 2,
	}

	public Pawn Pawn { get; }
	public Comp_TSCosmetics CosmeticsComp { get; }
	public CosmeticSet Set { get; }
	public CosmeticAttachment Attachment;
	public Dictionary<string, string> EditBuffers = [];

	public TransformStateType TransformState;
	public EditSettingType EditSettings;

	public override Vector2 InitialSize => new(750, 1250);

	public int ActiveTicks;

	[MemberNotNull(nameof(Attachment))]
	public void AttachmentChanged(CosmeticAttachment attachment)
	{
		Attachment = attachment;
		// TODO: change transform
		// TODO: change color
	}

	public Window_TransformEditor(Pawn pawn, CosmeticAttachment attachment, CosmeticSet set)
	{
		Pawn = pawn;
		Set = set;
		CosmeticsComp = Pawn.GetComp<Comp_TSCosmetics>() ?? throw new Exception("invalid pawn trying to open transform editor");
		AttachmentChanged(attachment);

		// window settings
		preventCameraMotion = false;
		doCloseX = true;
		// draggable = true;
	}

	private const float SIDE_PANEL_SIZE = 400;
	private const float BODY_TYPE_HEIGHT = 70;

	public override void DoWindowContents(Rect inRect)
	{
		if (!Set.AllAttachments.Contains(Attachment))
		{
			Close();
			return;
		}
		ActiveTicks++;
		Comp_TSCosmetics.CompUpdateNotify changed = Comp_TSCosmetics.CompUpdateNotify.None;

		inRect.SplitVerticallyWithMargin(
			out var side_rect,
			out var main_rect,
			out _,
			leftWidth: SIDE_PANEL_SIZE
		);

		DrawMain(main_rect);

		CosmeticsComp.NotifyUpdate(changed);
	}


	public bool DrawMain(Rect main_rect)
	{
		var changed = false;
		using var main_list = new TSUtil.Listing_D(main_rect);

		using (new TSUtil.TextSize_D(GameFont.Medium))
			main_list.Listing.Label(Attachment.EditorKey.ModTranslate());

		main_list.Listing.GapLine();
		using (new TSUtil.TextSize_D(GameFont.Small))
			main_list.Listing.Label("overall settings".ModTranslate());
		main_list.Listing.GapLine();

		changed = main_list.Listing.SliderLabeledWithValue(
			ref Attachment.OverallScale,
			"overall scale".ModTranslate(),
			0, 2,
			EditBuffers,
			resetval: 1
		) || changed;

		Attachment.DrawTransformSettings(this, main_list.Listing);

		return changed;
	}

	public bool DrawBodySelection(Listing listing, ref BodyTypeDef? body)
	{
		var body_rect = listing.GetRect(BODY_TYPE_HEIGHT);
		Widgets.DrawWindowBackground(body_rect);
		body_rect = body_rect.ContractedBy(3);
		var body_ref = body;
		CosmeticsUtil.BodyTypes.Prepend(null).SplitIntoSquaresGap(
			body_rect
		).Do((list_body, rect) => {
			var vis_rect = rect.ContractedBy(4);
			var color = Color.cyan;
			var clicked = TSUtil.DrawColoredBox(
				vis_rect,
				color,
				list_body == body_ref,
				is_button: true
			);
			if (clicked)
				body_ref = list_body;
			TooltipHandler.TipRegion(rect, list_body?.defName);

			if (list_body is null)
				return;

			try
			{
				var body_graphic = GraphicDatabase.Get<Graphic_Multi>(
					list_body.bodyNakedGraphicPath,
					ShaderDatabase.CutoutSkin,
					vis_rect.size,
					Color.gray
				);
				Widgets.DrawTextureFitted(vis_rect, body_graphic.MatAt(Rot4.South).mainTexture, 1.2f);
			}
			catch (System.Exception)
			{ /* modded bodies could mess stuff up perhaps */ }
		});

		var changed = body != body_ref;
		body = body_ref;
		return changed;
	}
}