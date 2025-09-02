using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

#nullable enable
namespace Transmogged;

public class WindowUtils
{
	public static bool ShowTransformEditWIthPreview(
		Rect rect,
		Comp_Transmogged comp,
		ref RenderTexture? rt,
		ref Vector2? dragstart,
		ref object? dragvalue,
		ref bool draggable,
		ref EditApparelState editstate,
		ref bool hidehair,
		TRTransform? selectedtransform,
		int activeticks = 0,
		Apparel? selectedapparel = null
	) {
		bool changed = false;
		using (var ll = new TransmoggedUtility.Listing_D(rect))
		{
			var psize = rect.width;
			var previewrect = ll.Listing.GetRect(psize);

			var blinkspertick = 0.03f;
			float brightness = Mathf.Sin(activeticks * blinkspertick) * .15f;

			PortraitsCache.PortraitParams portrait_settings = new(
				new(psize, psize),
				default,
				1,
				selectedtransform?.Rotation ?? Rot4.South,
				overrideApparelColors: selectedapparel is null
					? null
					: new Dictionary<Apparel, Color>() {
					{ selectedapparel, selectedapparel.DrawColor.Darken(brightness) }
				},
				overrideHairColor: hidehair
					? Color.clear
					: null
			);

			comp.ForceEdit(true);
			portrait_settings.RenderPortrait(comp.Pawn, rt ??= new((int)psize, (int)psize, 1));
			comp.ForceEdit(false);
			
			Widgets.DrawShadowAround(previewrect);
			Widgets.DrawWindowBackground(previewrect);
			Widgets.DrawTextureFitted(previewrect, rt, 1);

			if (selectedtransform is not null
				&& Mouse.IsOver(previewrect)
				&& Event.current.type == EventType.MouseDown)
			{
				dragstart ??= Event.current.mousePosition;
				dragvalue ??= editstate switch
				{
					EditApparelState.Move => selectedtransform.Offset,
					EditApparelState.Scale => selectedtransform.Scale,
					EditApparelState.Rotate => selectedtransform.RotationOffset,
					EditApparelState.None or _ => null,
				};	
			}

			draggable = !dragstart.HasValue;

			if (selectedtransform is not null
				&& Mouse.IsOver(previewrect)
				&& dragstart.HasValue
				&& Event.current.type != EventType.MouseUp)
			{
				var absdel = dragstart.Value - Event.current.mousePosition;
				absdel.x = -absdel.x;

				if (Event.current.shift)
				{
                    switch (editstate)
                    {
                        case EditApparelState.Move:
                        case EditApparelState.Scale:
							absdel.x = Mathf.Abs(absdel.x) > Mathf.Abs(absdel.y) ? absdel.x : 0;
							absdel.y = Mathf.Abs(absdel.y) > Mathf.Abs(absdel.x) ? absdel.y : 0;
                            break;
                        case EditApparelState.Rotate:
                            break;
                    }
                }

                switch (editstate)
				{
					case EditApparelState.Move:
						var movstart = (Vector3)dragvalue!;
						selectedtransform.Offset = movstart + (new Vector3(absdel.x, 0, absdel.y) * .008f);
						break;
					case EditApparelState.Scale:
						var scalestart = (Vector2)dragvalue!;
						selectedtransform.Scale = scalestart + (absdel * .008f);
						break;
					case EditApparelState.Rotate:
						var rotatestart = (float)dragvalue!;
						selectedtransform.RotationOffset = rotatestart - (absdel.x * 1f);
						break;
					default:
					case EditApparelState.None:
						break;
				}

				changed = editstate != EditApparelState.None;
            }
            else
			{
				dragstart = null;
				dragvalue = null;
			}

			if (selectedtransform is not null)
			{
				var edittyperect = ll.Listing.GetRect(40);
				var edititemrect = edittyperect
					.LeftPartPixels(edittyperect.height)
					.ExpandedBy(-2);

				foreach (EditApparelState edittype in Enum.GetValues(typeof(EditApparelState)))
				{
					bool active = edittype == EditApparelState.None
						? editstate == edittype
						: editstate.HasFlag(edittype);
					Widgets.DrawOptionBackground(edititemrect, active);
					(edittype switch
					{
						EditApparelState.Move => TransmoggedData.Textures.Move,
						EditApparelState.Scale => TransmoggedData.Textures.Expand,
						EditApparelState.Rotate => TransmoggedData.Textures.Orbit,
						EditApparelState.None or _ => null,
					})?.DrawFitted(edititemrect.ExpandedBy(-2));

					if (Widgets.ButtonInvisible(edititemrect))
					{
						editstate = edittype;
					}

					edititemrect = edititemrect.Move(edittyperect.height);
				}
			}

			ll.Listing.CheckboxLabeled("Transmogged.HideHair".Translate(), ref hidehair, 0);
        }
		return changed;
	}
}

public enum TransformModType
{
	Head,
	Hair,
	Body,
	Beard,
}

public class TransformEditWindow : Window
{
	public Pawn Pawn { get; }

	public override Vector2 InitialSize => new(750, 850);

	private RenderTexture? RT;
	private Rot4 Direction = Rot4.South;
	private Vector2? DragStart;
	private object? DragObject;
	private EditApparelState State;
	private TransformModType ModType = TransformModType.Head;
	private bool HideHair;

	private string? Buffer_ApScale;
	private string? Buffer_SideRotation;
	private string? Buffer_SideScaleX;
	private string? Buffer_SideScaleY;
	private string? Buffer_SideOffsetX;
	private string? Buffer_SideOffsetY;
	private string? Buffer_LayerOffset;

	public TransformEditWindow(Pawn pawn)
    {
        Pawn = pawn;

		preventCameraMotion = false;
		doCloseX = true;
    }

    public override void Close(bool doCloseSound = true)
    {
        base.Close(doCloseSound);
		CosmeticsSave.Instance.Save();
    }

    public override void DoWindowContents(Rect inRect)
    {
		if (!Pawn.TryGetComp<Comp_Transmogged>(out var comp))
		{
			Close();
			return;
		}

		IBodyTransform trsource = comp.GetData().State switch
		{
			TRCompState.Enabled => comp,
			_					=> CosmeticsSave.Instance.AutoBodyTransforms.Ensure(Pawn.GetAutoBodyKey())
		};

		bool changed = false;
		var tr = trsource.GetTransformFor(ModType);
		var rottr = tr.ForRot(Direction);

		Rect imagerect;

        using (var mainlist = new TransmoggedUtility.Listing_D(inRect))
		{
			Rect labrect;
			using (new TransmoggedUtility.TextSize_D(GameFont.Medium))
				labrect = mainlist.Listing.Label("transform settings");

			var btnrow = new WidgetRow(labrect.RightHalf().x, labrect.y, UIDirection.RightThenDown);
			if (btnrow.ButtonIcon(TexButton.Copy))
			{
				TransmoggedUtility.Transform4Clipboard = tr.CreateCopy();
			}
			if (TransmoggedUtility.Transform4Clipboard is not null
				&& btnrow.ButtonIcon(TexButton.Paste))
			{
				tr.CopyFrom(TransmoggedUtility.Transform4Clipboard);
				changed = true;
			}
			btnrow.Gap(40);
			// if (btnrow.ButtonIcon(TexButton.IconBook, "set as auto-scale"))
			// {
			// 	var autotr = TransmoggedSave.Instance.AutoBodyTransforms.Ensure(Pawn.GetAutoBodyKey());
			// 	autotr.CopyFrom(comp);
			// }

			var siderectheight = 100;
			var buttonrectinner = 90;
			var srbig = 60;
			var srsmall = 30;

			var dirrect = mainlist.Listing.GetRect(100);
			var leftbtnsrect = dirrect
				.Square()
				.ExpandedBy(buttonrectinner - siderectheight);

			var row = new WidgetRow(leftbtnsrect.x + leftbtnsrect.width + 5, leftbtnsrect.y, UIDirection.RightThenDown);
			foreach (TransformModType trt in Enum.GetValues(typeof(TransformModType)))
			{
				if (row.ButtonText(trt.ToString(), drawBackground: ModType != trt))
				{
					ModType = trt;
				}
			}
			
			TransmoggedData.Textures.CircleLine
				.DrawFitted(leftbtnsrect.ExpandedBy(3), Widgets.WindowBGBorderColor);

			foreach ((Rect btnrect, Rot4 btnrot) in new[]{
				(leftbtnsrect.ShrinkBottom(srbig).ShrinkLeft(srsmall).ShrinkRight(srsmall),	Rot4.North),
				(leftbtnsrect.ShrinkTop(srbig).ShrinkLeft(srsmall).ShrinkRight(srsmall),	Rot4.South),
				(leftbtnsrect.ShrinkLeft(srbig).ShrinkTop(srsmall).ShrinkBottom(srsmall),	Rot4.East),
				(leftbtnsrect.ShrinkRight(srbig).ShrinkTop(srsmall).ShrinkBottom(srsmall),	Rot4.West),
			}) {
				Widgets.DrawOptionBackground(btnrect, btnrot == Direction);
				if (Widgets.ButtonInvisible(btnrect) || Widgets.ClickedInsideRect(btnrect))
				{
					Direction = btnrot;
				}
				TransmoggedData.Textures.Arrow
					.DrawFitted(btnrect, color: Color.white, rotation: btnrot.AsAngle);
			}

			imagerect = mainlist.Listing.GetRect(200);
			changed = WindowUtils.ShowTransformEditWIthPreview(
				imagerect.Square().GrowBottom(100),
				comp,
				ref RT,
				ref DragStart,
				ref DragObject,
				ref draggable,
				ref State,
				ref HideHair,
				rottr
			);
		}

		using (var sublist = new TransmoggedUtility.Listing_D(inRect.ShrinkLeft(220).ShrinkTop(imagerect.y)))
		{
			var row = new WidgetRow(sublist.Listing.curX, sublist.Listing.curY, UIDirection.RightThenDown);
			if (row.ButtonIcon(TexButton.Copy))
			{
				TransmoggedUtility.TransformClipboard = rottr.CreateCopy();
			}
			if (TransmoggedUtility.TransformClipboard is not null
				&& row.ButtonIcon(TexButton.Paste))
			{
				rottr.CopyFrom(TransmoggedUtility.TransformClipboard);
			}
			using (new TransmoggedUtility.TextSize_D(GameFont.Tiny))
			if (row.ButtonText("Mirror"))
			{
				tr.ForRot(rottr.Rotation.Opposite).CopyFrom(rottr.Mirror());
			}
			sublist.Listing.Gap(20);

			Text.Font = GameFont.Small;
			var sliderlist = sublist.Listing;
			changed = sliderlist.SliderLabeledWithValue(ref rottr.RotationOffset,	"Transmogged.SideRotation".Translate(), -360, 360,	ref Buffer_SideRotation,	resetval: 0) || changed;
			sliderlist.Label("Transmogged.Scale".Translate());
			changed = sliderlist.SliderLabeledWithValue(ref rottr.Scale.x,			"Transmogged.SideScaleX".Translate(), 0, 2,			ref Buffer_SideScaleX,  	resetval: 1) || changed;
			changed = sliderlist.SliderLabeledWithValue(ref rottr.Scale.y,			"Transmogged.SideScaleY".Translate(), 0, 2,			ref Buffer_SideScaleY,  	resetval: 1) || changed;
			sliderlist.Label("Transmogged.Position".Translate());
			changed = sliderlist.SliderLabeledWithValue(ref rottr.Offset.x,			"Transmogged.SideOffsetX".Translate(), -1, 1,		ref Buffer_SideOffsetX, 	resetval: 0) || changed;
			changed = sliderlist.SliderLabeledWithValue(ref rottr.Offset.z,			"Transmogged.SideOffsetY".Translate(), -1, 1,		ref Buffer_SideOffsetY, 	resetval: 0) || changed;
			changed = sliderlist.SliderLabeledWithValue(ref rottr.Offset.y,			"Transmogged.LayerOffset".Translate(), -.06f,.06f,	ref Buffer_LayerOffset, 	resetval: 0, accuracy: 0.0001f) || changed;
		}
    }
}

public class TRPawnRenderSubWorker : PawnRenderSubWorker
{
	private TRTransform4 Transform;

    public TRPawnRenderSubWorker(TRTransform4 transform)
    {
        Transform = transform;
    }

    public override void TransformRotation(PawnRenderNode node, PawnDrawParms parms, ref Quaternion rotation)
    {
        var tr = Transform.ForRot(parms.facing);
		rotation *= Quaternion.AngleAxis(tr.RotationOffset, Vector3.up);
    }

    public override void TransformOffset(PawnRenderNode node, PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot)
    {
        var tr = Transform.ForRot(parms.facing);
		// Log.Message($"{parms.facing}, {offset}, {tr.Offset}");
		offset += tr.Offset;
    }

    public override void TransformScale(PawnRenderNode node, PawnDrawParms parms, ref Vector3 scale)
    {
        var tr = Transform.ForRot(parms.facing);
		scale = Vector3.Scale(scale, new Vector3(tr.Scale.x, 1, tr.Scale.y));
    }
}