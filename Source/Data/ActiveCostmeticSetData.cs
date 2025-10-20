using System;
using System.Collections.Generic;
using System.Linq;
using Cosmetics.Comp;
using Cosmetics.Util;
using RimWorld;
using Verse;

namespace Cosmetics.Data;

public class ActiveCosmeticSetData
{
	public int Hash;
	public CosmeticSet BaseSet;
	public List<CosmeticSet> AdditiveSets;

	private readonly Lazy<List<CosmeticApparel>> _ActiveApparel;
	public List<CosmeticApparel> ActiveApparel => _ActiveApparel.Value;
	private Func<List<CosmeticApparel>> ApparelFactory => () => [.. GetActiveApparel()];

	public ActiveCosmeticSetData(CosmeticSet base_set, List<CosmeticSet> additive_sets, int hash)
	{
		BaseSet = base_set;
		AdditiveSets = additive_sets;
		Hash = hash;
		_ActiveApparel = new(ApparelFactory);
	}

	private IEnumerable<CosmeticApparel> GetActiveApparel()
	{
		var pawn = BaseSet.Pawn;
		var set_order = AdditiveSets.Prepend(BaseSet);
		var disabled_apparel = new HashSet<Apparel>();
		foreach (var slot in CosmeticsUtil.ClothingSlots)
		{
			var aps_for_slot = set_order.Select(set => set.GetApparelForSlot(slot));
			if (aps_for_slot.All(x => x is null)
				|| aps_for_slot.Any(x =>
				{
					var ap = x?.LinkedSlot?.GetApparelFor(pawn);
					if (ap is not null && disabled_apparel.Contains(ap))
						return true;
					if (x?.LinkedSlot?.State == CosmeticApparel.LinkedSlotData.StateType.Disable)
					{
						if (ap is not null)
							disabled_apparel.Add(ap);
						return true;
					}
					return false;
				}))
			{
				continue;
			}

			var first = aps_for_slot.FirstOrDefault(x => x is not null);
			var combined = first?.CreateCopy() ?? new(pawn);
			combined.LinkedSlot = new(slot);
			foreach (var ovap in aps_for_slot)
			{
				if (ovap is null)
					continue;

				if (ovap.OverrideApparelDef is not null)
				{
					combined.Transform = ovap.Transform.CreateCopy();
					combined.OverrideApparelDef = ovap.OverrideApparelDef;
					combined.OverallScale = ovap.OverallScale;
					combined.RaceDef = ovap.RaceDef;
					combined.BodyDef = ovap.BodyDef;
				}
				combined.Color = ovap.Color ?? combined.Color;
				combined.StyleDef = ovap.StyleDef ?? combined.StyleDef;
			}
			yield return combined;
		}

		foreach (var ap in BaseSet.Apparel)
		{
			yield return ap;
		}

		foreach (var ap in AdditiveSets.SelectMany(x => x.Apparel))
		{
			yield return ap;
		}
	}

	public static CosmeticSet EmptySet => new(default!) { Name = "Empty Set" };

	/// <summary>
	/// Evaluate the sets, comparing the hash to the currently active set data if exists.
	/// </summary>
	/// <param name="comp"></param>
	/// <param name="current"></param>
	/// <param name="new_data"></param>
	/// <returns>Whether new_data is new or not</returns>
	public static bool Evaluate(Comp_TSCosmetics comp, ActiveCosmeticSetData? current, out ActiveCosmeticSetData new_data)
	{
		// Log.Message($"evalutating set for {comp.Pawn}");
		int hash = 0;
		var all_sets = (comp.Save.Sets ??= [])
			.Where(x => !x.States.Contains(StateDefOf.Disabled))
		;
		var pawn = comp.Pawn;
		Dictionary<CosmeticSet, float> set_fits = [];

		foreach (var set in all_sets)
		{
			var fit = set.GetSetPoints(pawn, comp);
			set_fits[set] = fit;
			// Log.Message($"set '{set.Name}' fit: {fit}");
		}

		set_fits = set_fits
			.Where(kv => kv.Value >= 0)
			.ToDictionary(kv => kv.Key, kv => kv.Value)
		;
		// Log.Message($"sets: {string.Join(", ", set_fits.Select(kv => $"'{kv.Key.Name}': {kv.Value}"))}");

		CosmeticSet base_set = set_fits
			.Where(kv => !kv.Key.States.Contains(StateDefOf.Additive))
			.OrderBy(kv => kv.Value)
			.Select(kv => kv.Key)
			.FirstOrDefault()
			?? EmptySet.For(pawn)
		;
		unchecked
		{
			hash += base_set.GetHashCode();
		}
		List<CosmeticSet> active_additive = [.. set_fits
			.Where(kv => kv.Key.States.Contains(StateDefOf.Additive))
			.Select(kv => kv.Key)
		];
		unchecked
		{
			hash += active_additive.Sum(set => set.GetHashCode());
		}

		if (current is not null && current.Hash == hash)
		{
			new_data = current;
			return false;
		}

		new_data = new(base_set, active_additive, hash);
		return true;
	}
}