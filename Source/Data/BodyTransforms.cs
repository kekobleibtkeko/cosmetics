using System;
using TS_Lib.Transforms;
using Verse;

namespace Cosmetics.Data;

public interface IBodyTransform
{
	public BodyTransforms GetBodyTransforms();
}

public class BodyTransforms : IExposable, IBodyTransform
{
	public TSTransform4 BodyTransform = new();
	public TSTransform4 HeadTransform = new();
	public TSTransform4 HairTransform = new();
	public TSTransform4 BeardTransform = new();

	public TSTransform4? GetTransformFor(PawnRenderNode node) => node switch
	{
		PawnRenderNode_Hair => node.parent is PawnRenderNode_Hair ? null : (HairTransform ??= new()),
		PawnRenderNode_Head => HeadTransform ??= new(),
		PawnRenderNode_Body => BodyTransform ??= new(),
		PawnRenderNode_Beard => BeardTransform ??= new(),
		_ => null,
	};

	public BodyTransforms CreateCopy()
	{
		return new()
		{
			BodyTransform = BodyTransform.CreateCopy(),
			HeadTransform = HeadTransform.CreateCopy(),
			HairTransform = HairTransform.CreateCopy(),
			BeardTransform = BeardTransform.CreateCopy(),
		};
	}

	public void ExposeData()
	{
		Scribe_Deep.Look(ref BodyTransform, "body");
		Scribe_Deep.Look(ref HeadTransform, "head");
		Scribe_Deep.Look(ref HairTransform, "hair");
		Scribe_Deep.Look(ref BeardTransform, "beard");
	}

	public TSTransform4 GetTransformFor(CosmeticBodypartPseudo.BodyPartType type) => type switch
	{
		CosmeticBodypartPseudo.BodyPartType.Head => HeadTransform,
		CosmeticBodypartPseudo.BodyPartType.Hair => HairTransform,
		CosmeticBodypartPseudo.BodyPartType.Body => BodyTransform,
		CosmeticBodypartPseudo.BodyPartType.Beard => BeardTransform,
		_ => throw new NotImplementedException(),
	};

	public BodyTransforms GetBodyTransforms() => this;
}