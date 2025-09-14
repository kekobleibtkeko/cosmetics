using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
using Cosmetics.Mod;
using Cosmetics.Util;
using RimWorld;
using TS_Lib.Util;
using Verse;

namespace Cosmetics.Data;

public enum StateFit
{
	Neutral,
	Fit,
	FitWell,
	FitHigh,
	Unfit,
}

public interface IStateWorker
{
	StateFit GetFitValue(Pawn pawn, Comp_TSCosmetics comp);
	IStateWorkerProps GetWorkerProps();
}

public class BaseStateWorker<T>(T props) : IStateWorker
	where
		T : BaseStateWorkerProps
{
	public T WorkerProps = props;

	public virtual StateFit GetFitValue(Pawn pawn, Comp_TSCosmetics comp) => StateFit.Neutral;

	public IStateWorkerProps GetWorkerProps() => WorkerProps;
}

public class TemperatureStateProps : BaseStateWorkerProps
{
	public class TemperatureStateWorker(TemperatureStateProps props) : BaseStateWorker<TemperatureStateProps>(props)
	{
		public override StateFit GetFitValue(Pawn pawn, Comp_TSCosmetics comp)
		{
			var min = WorkerProps.ActiveRange.min;
			var max = WorkerProps.ActiveRange.max;
			switch (WorkerProps.uncap)
			{
				case UncapType.None:
					break;
				case UncapType.Upper:
					max = int.MaxValue;
					break;
				case UncapType.Lower:
					min = int.MinValue;
					break;
			}

			var temp = pawn.AmbientTemperature;
			Log.Message($"TEMP: {temp} ALLOWED: {min}~{max}");
			return (temp <= max && temp >= min)
				? StateFit.Fit
				: StateFit.Unfit
			;
		}
	}
	public override Type WorkerType => typeof(TemperatureStateWorker);
	public override bool HasSettings => true;
	public enum UncapType
	{
		None,
		Upper,
		Lower,
	}
	public IntRange activeRange;
	public IntRange editRange;
	public UncapType uncap;

	public IntRange? SavedRange;
	public IntRange ActiveRange => SavedRange ?? activeRange;

	public override void ExposeSettingsData(StateDef def)
	{
		Scribe_Values.Look(ref SavedRange, $"{def.defName}.range");
	}

	public override void DrawExtraOptions(CosmeticsSettings settings, Listing_Standard listing, StateDef def)
	{
		var range_rect = listing
			.GetRect(30)
			.Labled("temperature range", CosmeticsUtil.ModTranslate)
		;
		var range = ActiveRange;
		Widgets.IntRange(
			range_rect,
			(int)listing.curY,
			ref range,
			editRange.min,
			editRange.max
		);
		if (range != activeRange)
			SavedRange = range;
	}
}

public class OutsideStateProps : BaseStateWorkerProps
{
	public class OutsideStateWorker(OutsideStateProps props) : BaseStateWorker<OutsideStateProps>(props)
	{
		public override StateFit GetFitValue(Pawn pawn, Comp_TSCosmetics comp)
		{
			try
			{
				var outside = pawn.IsOutside();
				Log.Message($"OUTSIDE: {outside} NEEDED: {WorkerProps.outside}");
				return outside == WorkerProps.outside
					? StateFit.Fit
					: StateFit.Unfit
				;
			}
			catch
			{
				return StateFit.Neutral;
			}
		}
	}

	public override Type WorkerType => typeof(OutsideStateWorker);
	public bool outside = true;
}

public class DraftedStateProps : BaseStateWorkerProps
{
	public class DraftedStateWorker(DraftedStateProps props) : BaseStateWorker<DraftedStateProps>(props)
	{
		public override StateFit GetFitValue(Pawn pawn, Comp_TSCosmetics comp)
		{
			return (pawn.Drafted == WorkerProps.drafted)
				? StateFit.FitWell
				: StateFit.Unfit
			;
		}
	}
	public override Type WorkerType => typeof(DraftedStateWorker);
	public bool drafted = false;
}

public class SleepStateProps : BaseStateWorkerProps
{
	public class SleepStateWorker(SleepStateProps props) : BaseStateWorker<SleepStateProps>(props)
	{
		public override StateFit GetFitValue(Pawn pawn, Comp_TSCosmetics comp)
		{
			return (pawn.Awake() != WorkerProps.sleeping)
				? StateFit.FitWell
				: StateFit.Unfit
			;
		}
	}
	public override Type WorkerType => typeof(SleepStateWorker);
	public bool sleeping = true;
}

public class PollutionStateProps : BaseStateWorkerProps
{
	public enum PollutionCheckType
	{
		Range,
		Percent,
	}
	public class PollutionStateWorker(PollutionStateProps props) : BaseStateWorker<PollutionStateProps>(props)
	{
		public override StateFit GetFitValue(Pawn pawn, Comp_TSCosmetics comp)
		{
			bool polluted = false;
			try
			{
				if (WorkerProps.ActiveValue == 0)
				{
					if (WorkerProps.ActiveCheck != PollutionCheckType.Percent)
						polluted = pawn.Map.pollutionGrid.IsPolluted(pawn.positionInt);
				}
				else if (WorkerProps.ActiveValue > 0)
				{
					switch (WorkerProps.ActiveCheck)
					{
						case PollutionCheckType.Range:
							if (pawn.Map.pollutionGrid.TotalPollutionPercent <= 0.0001)
								break;
							var range_count = GenRadial.NumCellsInRadius(WorkerProps.ActiveValue);
							var center = pawn.positionInt;
							polluted = Enumerable
								.Range(0, range_count)
								.Any(i => pawn.Map.pollutionGrid.IsPolluted(center + GenRadial.RadialPattern[i]))
							;
							break;
						case PollutionCheckType.Percent:
							polluted = pawn.Map.pollutionGrid.TotalPollutionPercent > WorkerProps.ActiveValue;
							break;
					}
				}
			}
			catch (Exception e)
			{
				Log.ErrorOnce($"Unable to determine map pollution for PollutionStateWorker: '{e}'", GetHashCode());
			}

			return (polluted == WorkerProps.polluted)
				? StateFit.FitHigh
				: StateFit.Unfit
			;
		}
	}

	public override Type WorkerType => typeof(PollutionStateWorker);
	public override bool HasSettings => true;

	public bool polluted = true;

	public PollutionCheckType check;
	public PollutionCheckType? SavedCheck;
	public PollutionCheckType ActiveCheck => SavedCheck ?? check;

	public int value = 0;
	public int? SavedValue;
	public int ActiveValue => SavedValue ?? value;

	static PollutionStateProps()
	{
		TSUtil.RegisterToColorHandler<PollutionCheckType>(val => val switch
		{
			PollutionCheckType.Percent => TSUtil.ColorFromHTML("#3FDC31"),
			PollutionCheckType.Range or _ => TSUtil.ColorFromHTML("#3940FF"),
		});
	}

	public override void ExposeSettingsData(StateDef def)
	{
		Scribe_Values.Look(ref SavedValue, $"{def.defName}.value");
		Scribe_Values.Look(ref SavedCheck, $"{def.defName}.check");
	}

	public override void DrawExtraOptions(CosmeticsSettings settings, Listing_Standard listing, StateDef def)
	{
		var type_rect = listing.Labled(30, "pollution type", CosmeticsUtil.ModTranslate);
		var chk = ActiveCheck;
		type_rect.DrawEnumAsButtons(ref chk, size_ratio: 2);
		if (chk != check)
		{
			SavedCheck = chk;
		}

		var value_rect = listing.Labled(30, "pollution value", CosmeticsUtil.ModTranslate);
		var range = ActiveValue;
		using (var buf = new TSUtil.EditBuffer_D<PollutionStateProps, string>(this, () => range.ToString()))
			Widgets.IntEntry(value_rect, ref range, ref buf.Ref);

		if (value != range)
		{
			SavedValue = range;
		}
	}
}

public class SpaceStateProps : BaseStateWorkerProps
{
	public class SpaceStateWorker(SpaceStateProps props) : BaseStateWorker<SpaceStateProps>(props)
	{
		public override StateFit GetFitValue(Pawn pawn, Comp_TSCosmetics comp)
		{
			var in_space = false;
			try
			{
				in_space = pawn.Map.Biomes.Contains(BiomeDefOf.Space)
					|| pawn.Map.Biomes.Contains(BiomeDefOf.Orbit)
				;
			}
			catch (System.Exception)
			{ }

			return (in_space == WorkerProps.space)
				? StateFit.FitWell
				: StateFit.Unfit
			;
		}
	}

	public override Type WorkerType => typeof(SpaceStateWorker);
	public bool space = true;
}

public class VacuumStateProps : BaseStateWorkerProps
{
	public class VacuumStateWorker(VacuumStateProps props) : BaseStateWorker<VacuumStateProps>(props)
	{
		public override StateFit GetFitValue(Pawn pawn, Comp_TSCosmetics comp)
		{
			float vacuum = 0.0f;
			try
			{
				vacuum = pawn.Position.GetVacuum(pawn.Map);
			}
			catch (System.Exception)
			{ }

			var fitting = vacuum > WorkerProps.threshold == WorkerProps.vacuum;
			return fitting
				? StateFit.FitHigh
				: StateFit.Unfit
			;
		}
	}
	public override Type WorkerType => typeof(VacuumStateWorker);
	public bool vacuum = true;
	public float threshold = 0.5f;
}

public class NoneStateProps : BaseStateWorkerProps
{
	public class NoneStateWorker(NoneStateProps props) : BaseStateWorker<NoneStateProps>(props)
	{
	}
	public override Type WorkerType => typeof(NoneStateWorker);
}
