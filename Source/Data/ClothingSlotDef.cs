using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TS_Lib.Util;
using Verse;

namespace Cosmetics.Data;

public class ClothingSlotDef : Def
{
	public int order;
	public Type slotWorker = typeof(ClothingSlotWorkerBase);

	public List<BodyPartGroupDef> bodyParts = [];
	public TSUtil.ListInclusionType bodyPartInclusion;
	public List<ApparelLayerDef> apparelLayers = [];
	public TSUtil.ListInclusionType apparelLayerInclusion;


	[Unsaved(false)]
	private readonly Lazy<ClothingSlotWorkerBase> _Worker;
	public ClothingSlotWorkerBase Worker => _Worker.Value;

	public ClothingSlotDef()
	{
		_Worker = new(() => slotWorker.CreateInstance() as ClothingSlotWorkerBase ?? throw new Exception($"unable to create instance of worker for '{this}'"));
	}

	public Apparel? GetEquippedItemFor(Pawn pawn) => Worker.GetEquippedItem(pawn, this);

	public override IEnumerable<string> ConfigErrors()
	{
		if (!typeof(ClothingSlotWorkerBase).IsAssignableFrom(slotWorker))
			yield return $"{nameof(slotWorker)} '{slotWorker}' for '{this}' is not assignable from {typeof(BaseStateWorker<BaseStateWorkerProps>)}";

		foreach (var er in base.ConfigErrors())
			yield return er;
	}
}