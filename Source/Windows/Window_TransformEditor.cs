using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cosmetics.Comp;
using Cosmetics.Data;
using Cosmetics.Inspector;
using Cosmetics.Mod;
using Cosmetics.Util;
using RimWorld;
using TS_Lib.Transforms;
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

	public enum SideListDrawType
	{
		Set,
		BodyTransforms,
		Face,
	}

	public Pawn Pawn { get; }
	public Comp_TSCosmetics CosmeticsComp { get; }
	public CosmeticSet? Set { get; }
	public CosmeticAttachment Attachment;
	public TSUtil.ScrollPosition SideListScroll = new();
	public Rot4? SelectedRotation;
	public Dictionary<string, string> EditBuffers = [];
	public RenderTexture? PortraitRT;

	public SideListDrawType ListDrawType;

	public TSTransform? SelectedTransform => SelectedRotation.HasValue
		? Attachment.GetTransformFor(SelectedRotation.Value)
		: null
	;

	// preview transform vars
	public Vector2? DragStart;
	public object? DragValue;
	public EditSettingType EditState;

	public TransformStateType TransformState;
	public EditSettingType EditSettings;

	public override Vector2 InitialSize => new(1250, 1150);

	public int ActiveTicks;

	[MemberNotNull(nameof(Attachment))]
	public void AttachmentChanged(CosmeticAttachment attachment)
	{
		Attachment = attachment;
		// TODO: change transform
		// TODO: change color
	}

	public Window_TransformEditor(Pawn pawn, CosmeticAttachment attachment, CosmeticSet? set)
	{
		Pawn = pawn;
		Set = set;
		CosmeticsComp = Pawn.GetComp<Comp_TSCosmetics>() ?? throw new Exception("invalid pawn trying to open transform editor");
		ListDrawType = attachment switch
		{
			CosmeticBodypartPseudo pseudo => SideListDrawType.BodyTransforms,
			_ => SideListDrawType.Set,
		};
		AttachmentChanged(attachment);

		// window settings
		preventCameraMotion = false;
		doCloseX = true;
		// draggable = true;
	}

	private const float SIDE_PANEL_SIZE = 400;
	private const float BODY_TYPE_HEIGHT = 70;
	private const float SIDE_LIST_HEIGHT = 500;
	private const float DIRECTION_WHEEL_HEIGHT = 100;
	private const float BLINKS_PER_TICK = 0.01f;
	private const float BLINK_DEPTH = 0.2f;

	private const float MOVE_MULT = 0.008f;
	private const float SCALE_MULT = 0.008f;
	private const float ROTATION_MULT = 1f;
	private const float EDIT_BUTTONS_HEIGHT = 40;

	public override void DoWindowContents(Rect inRect)
	{
		switch (Attachment)
		{
			case CosmeticBodypartPseudo pseudo:
				break;
			case CosmeticApparel app:
				if (Set is null || !Set.AllAttachments.Contains(Attachment))
				{
					Close();
					return;
				}
				break;
			case null:
				Close();
				return;
		}
		ActiveTicks++;
		bool changed = false;

		inRect.SplitVerticallyWithMargin(
			out var side_rect,
			out var main_rect,
			out _,
			leftWidth: SIDE_PANEL_SIZE
		);

		changed = DrawSidePanel(side_rect) || changed;
		changed = DrawMain(main_rect) || changed;

		if (changed)
		{
			CosmeticsComp.NotifyUpdate(Attachment.UpdateFlags | Comp_TSCosmetics.CompUpdateNotify.ForceInternal);
			Attachment.SetDirty();
		}
	}

	public void DrawDirectionWheel(Rect rect, ref Rot4? rotation)
	{
		if (rect.Square() != rect)
		{
			Log.WarningOnce("direction wheel attempted to drawn in non-square", rect.GetHashCode());
			return;
		}

		var srbig = rect.width * 0.75f;
		var srsmall = rect.width * 0.35f;

		CosmeticsData.Textures.CircleLine.DrawFitted(
			rect.ExpandedBy(3),
			Widgets.WindowBGBorderColor
		);

		foreach ((Rect btnrect, Rot4 btnrot) in new[]{
			(rect.ShrinkBottom(srbig).ShrinkLeft(srsmall).ShrinkRight(srsmall), Rot4.North),
			(rect.ShrinkTop(srbig).ShrinkLeft(srsmall).ShrinkRight(srsmall), Rot4.South),
			(rect.ShrinkLeft(srbig).ShrinkTop(srsmall).ShrinkBottom(srsmall), Rot4.East),
			(rect.ShrinkRight(srbig).ShrinkTop(srsmall).ShrinkBottom(srsmall), Rot4.West),
		})
		{
			var functional_rect = btnrect.ExpandedBy(7);
			Widgets.DrawOptionBackground(btnrect, rotation == btnrot);
			if (Widgets.ButtonInvisible(functional_rect))
			{
				rotation = btnrot;
			}
			CosmeticsData.Textures.Arrow.DrawFitted(
				btnrect,
				color: Color.white,
				rotation: btnrot.AsAngle
			);
		}
	}

	public bool DrawSidePanel(Rect side_rect)
	{
		bool changed = false;
		using var list = new TSUtil.Listing_D(side_rect);
		var side_list_rect = list.GetRect(SIDE_LIST_HEIGHT);
		switch (ListDrawType)
		{
			case SideListDrawType.Set:
				changed = SetDetails.DrawSetLists(
					side_list_rect,
					CosmeticsComp,
					Set!,
					SideListScroll,
					Attachment as CosmeticApparel
				) || changed;
				break;
			case SideListDrawType.BodyTransforms:
				changed = DrawBodyTransformList(side_list_rect) || changed;
				break;
			case SideListDrawType.Face:
				break;
		}

		var direction_rect = list.GetRect(DIRECTION_WHEEL_HEIGHT);
		DrawDirectionWheel(
			direction_rect.RightPartPixels(direction_rect.height).Move(-7),
			ref SelectedRotation
		);
		list.Listing.GapLine();
		changed = DrawPreview(
			list.Listing,
			(Attachment as CosmeticApparel)?.GetApparel(),
			false, // TODO: make hair color change when selected
			null,
			false
		) || changed;

		return changed;
	}

	public bool DrawBodyTransformList(
		Rect rect
	)
	{
		if (Attachment is not CosmeticBodypartPseudo pseudo)
			return false;
		using var list = new TSUtil.Listing_D(rect);
		var changed = false;
		changed = list.Listing.DrawDraggableList(
			TSUtil.GetEnumValues<CosmeticBodypartPseudo.BodyPartType>(),
			draw_fun: (en, rect) =>
			{
				var icon_offset = rect.height + 3;
				CosmeticBodypartPseudo.DrawIconFor(rect.LeftPartPixels(rect.height), Pawn, en);
				rect = rect.ShrinkLeft(icon_offset);
				Widgets.Label(rect, en.ToString().ModTranslate());
			},
			is_active: en => en == pseudo.BodyPart,
			click_fun: en =>
			{
				pseudo.BodyPart = en;
				changed = true;
			},
			no_drag: true
		) || changed;

		return changed;
	}

	public bool HandleMouseTransforms(
		Rect preview_rect
	)
	{

		var selected_transform = SelectedTransform;
		var changed = false;
		// handle mouse down
		if (selected_transform is not null
			&& Mouse.IsOver(preview_rect)
			&& Event.current.type == EventType.MouseDown)
		{
			DragStart ??= Event.current.mousePosition;
			DragValue ??= TransformState switch
			{
				TransformStateType.Move => selected_transform.Offset,
				TransformStateType.Scale => selected_transform.Scale,
				TransformStateType.Rotate => selected_transform.RotationOffset,
				TransformStateType.None or _ => null,
			};
		}

		// handle dragging
		if (selected_transform is not null
			&& Mouse.IsOver(preview_rect)
			&& DragStart.HasValue
			&& Event.current.type != EventType.MouseUp)
		{
			var absolute_movement = DragStart.Value - Event.current.mousePosition;
			absolute_movement.x *= -1.0f;

			if (Event.current.shift)
			{
				// only make the largest axis apply when shift is held
				// (straight movement when moving)
				switch (TransformState)
				{
					case TransformStateType.Move:
					case TransformStateType.Scale:
						absolute_movement.x = Mathf.Abs(absolute_movement.x) > Mathf.Abs(absolute_movement.y)
							? absolute_movement.x
							: 0
						;
						absolute_movement.y = Mathf.Abs(absolute_movement.y) > Mathf.Abs(absolute_movement.x)
							? absolute_movement.y
							: 0
						;
						break;
				}
			}

			switch (TransformState)
			{
				case TransformStateType.Move:
					var move_start = (Vector3)DragValue!;
					selected_transform.Offset = move_start
						+ (new Vector3(absolute_movement.x, 0, absolute_movement.y) * MOVE_MULT)
					;
					break;
				case TransformStateType.Scale:
					var scale_start = (Vector2)DragValue!;
					selected_transform.Scale = scale_start + (absolute_movement * SCALE_MULT);
					break;
				case TransformStateType.Rotate:
					var rotation_start = (float)DragValue!;
					selected_transform.RotationOffset = rotation_start - (absolute_movement.x * ROTATION_MULT);
					break;
			}

			changed = TransformState != TransformStateType.None;
		}
		else
		{
			DragStart = null;
			DragValue = null;
		}
		return changed;
	}

	public bool DrawPreview(
		Listing_Standard listing,
		Apparel? selected_apparel,
		bool hair_selected,
		Color? hair_color_override,
		bool no_edit
	)
	{
		var hide_hair = EditState.HasFlag(EditSettingType.HideHair);
		var preview_size = listing.ColumnWidth;
		var preview_rect = listing.GetRect(preview_size);

		var item_brightness = Mathf.Sin(ActiveTicks * BLINKS_PER_TICK) * BLINK_DEPTH;

		PortraitsCache.PortraitParams portrait_params = new(
			new(preview_size, preview_size),
			default,
			1,
			SelectedRotation ?? Rot4.South,
			overrideApparelColors: selected_apparel is null
				? null
				: new Dictionary<Apparel, Color>() {
					{ selected_apparel, selected_apparel.DrawColor.Darken(item_brightness) }
				},
			overrideHairColor: hide_hair
				? Color.clear
				: hair_color_override.HasValue
					? hair_color_override.Value.Darken(hair_selected ? item_brightness : 0)
					: null
		);

		CosmeticsComp.ForceEditingSet(true);
		portrait_params.RenderPortrait(
			Pawn,
			PortraitRT ??= new((int)preview_size, (int)preview_size, 1)
		);
		CosmeticsComp.ForceEditingSet(false);

		Widgets.DrawShadowAround(preview_rect);
		Widgets.DrawWindowBackground(preview_rect);
		Widgets.DrawTextureFitted(preview_rect, PortraitRT, 1);

		if (no_edit)
			return false;

		listing.CheckboxLabeled("hide hair".ModTranslate(), ref hide_hair, 0);
		EditState.SetFlag(EditSettingType.HideHair, hide_hair);

		if (SelectedTransform is null)
			return false;

		var edit_rect = listing.GetRect(EDIT_BUTTONS_HEIGHT);
		edit_rect.ShrinkLeft(4).DrawEnumAsButtons<TransformStateType>(
			state => TransformState == state,
			state => TransformState = state,
			draw_fun: (state, rect) => (state switch
			{
				TransformStateType.Move => CosmeticsData.Textures.Move,
				TransformStateType.Scale => CosmeticsData.Textures.Expand,
				TransformStateType.Rotate => CosmeticsData.Textures.Orbit,
				TransformStateType.None or _ => null,
			}).DrawFitted(rect.ContractedBy(2))
		);

		return HandleMouseTransforms(preview_rect);
	}

	public bool DrawMain(Rect main_rect)
	{
		var changed = false;
		using var main_list = new TSUtil.Listing_D(main_rect);

		using (new TSUtil.TextSize_D(GameFont.Medium))
		{
			var row = main_list.Listing
				.GetRect(Text.CalcHeight(string.Empty, 0))
				.LabeledRow(Attachment.EditorKey.ModTranslate(), split: 0.3f)
			;
			changed = Attachment.DrawHeaderOptions(row, this) || changed;
		}

		main_list.Listing.GapLine();

		using (new TSUtil.TextSize_D(GameFont.Small))
			main_list.Listing.Label("overall settings".ModTranslate());
		main_list.Listing.GapLine();

		changed = Attachment.DrawOverallSettings(
			this,
			main_list.Listing
		) || changed;

		main_list.Listing.Gap();

		WidgetRow side_row;
		using (new TSUtil.TextSize_D(GameFont.Small))
			side_row = main_list.Listing.GetRect(Text.LineHeight).LabeledRow("sidespecific settings".ModTranslate());
		main_list.Listing.GapLine();
		if (SelectedRotation.HasValue)
		{
			changed = Attachment.DrawSideSpecificOptions(side_row, this, SelectedRotation.Value) || changed;
			changed = Attachment.DrawTransformSettings(
				this,
				main_list.Listing,
				SelectedRotation.Value
			) || changed;
		}

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
					Pawn.story.SkinColor
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