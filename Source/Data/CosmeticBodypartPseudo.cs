using System;
using Cosmetics.Comp;
using Cosmetics.Mod;
using Cosmetics.Util;
using Cosmetics.Windows;
using TS_Lib.Transforms;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace Cosmetics.Data;

public class CosmeticBodypartPseudo : CosmeticAttachment
{
	public enum BodyPartType
	{
		Head,
		Body,
		Hair,
		Beard,
	}

	public BodyPartType BodyPart;
	public bool AutoTransforms;

	public override string Label => BodyPart.ToString().ModTranslate();
	public override string EditorKey => "body transform";

	[Obsolete("don't use directly, constructer used to deserialize only", true)]
	public CosmeticBodypartPseudo() : base() { }
	public CosmeticBodypartPseudo(Pawn pawn, BodyPartType body_part) : base(pawn)
	{
		BodyPart = body_part;
	}

	private Comp_TSCosmetics CosmeticsComp => Pawn!.GetComp<Comp_TSCosmetics>();

	public override TSTransform4 GetTransform()
	{
		IBodyTransform tr_source = AutoTransforms
			? CosmeticsSave.Instance.AutoBodyTransforms.Ensure(Pawn!.GetAutoBodyKey())
			: CosmeticsComp
		;
		var trs = tr_source.GetBodyTransforms();
		return BodyPart switch
		{
			BodyPartType.Body => trs.BodyTransform,
			BodyPartType.Hair => trs.HairTransform,
			BodyPartType.Beard => trs.BeardTransform,
			BodyPartType.Head or _ => trs.HeadTransform,
		};
	}

	private static bool IsSkin(BodyPartType body_part) => body_part switch
	{
		BodyPartType.Body or BodyPartType.Head => true,
		BodyPartType.Hair or BodyPartType.Beard or _ => false,
	};

	public override void DrawIcon(Rect rect)
	{
		DrawIconFor(rect, Pawn!, BodyPart);
	}

	public static void DrawIconFor(Rect rect, Pawn pawn, BodyPartType part)
	{
		var path = part switch
		{
			BodyPartType.Head => pawn.story.headType?.graphicPath,
			BodyPartType.Body => pawn.story.bodyType?.bodyNakedGraphicPath,
			BodyPartType.Hair => pawn.story.hairDef?.texPath,
			BodyPartType.Beard => pawn.style.beardDef?.texPath,
			_ => string.Empty
		};
		if (path is null)
			return;
		(GraphicDatabase.Get<Graphic_Multi>(
			path,
			ShaderDatabase.Cutout,
			rect.size,
			Color.white
		).MatSouth.mainTexture as Texture2D)?.DrawFitted(
			rect,
			IsSkin(part) ? pawn.story.SkinColor : pawn.story.hairColor,
			1.5f
		);
	}

	public override bool DrawOverallSettings(Window_TransformEditor editor, Listing_Standard listing)
	{
		var changed = false;

		var prev_auto = AutoTransforms;
		listing.CheckboxLabeled("global transforms".ModTranslate(), ref AutoTransforms);
		changed = changed || prev_auto != AutoTransforms;

		// changed = base.DrawOverallSettings(editor, listing) || changed;
		return changed;
	}
}