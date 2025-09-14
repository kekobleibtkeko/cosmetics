using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
using HarmonyLib;
using RimWorld;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace Cosmetics.Data;

[DefOf]
public static class StateDefOf
{
    public static StateDef Disabled = default!;
	public static StateDef Additive = default!;

    static StateDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(StateDefOf));
	}
}

public class StateDef : Def, TSUtil.IToColor
{
    [MayTranslate]
    public string shortLabel = "XX";
    public float order;
    public Color color;
    public BaseStateWorkerProps props = new NoneStateProps();
    public float fitWeight = 1.0f;
    public float fitWellWeight = 2.0f;
    public float fitHighWeight = 3.0f;
    public float unfitWeight = -20.0f;

    public List<StateDef> incompatibleStates = [];

	[Unsaved(false)]
	private readonly Lazy<IStateWorker> _Worker;
    public IStateWorker Worker => _Worker.Value;

	public StateDef()
	{
		_Worker = new(() =>
		{
			var worker = Activator.CreateInstance(
				props.WorkerType,
				props
			);
			// Log.Message($"generated worker: {worker}({worker.GetType()})");

			return worker as IStateWorker ?? throw new Exception($"unable to create instance of worker for '{this}'");
		});
	}

	public Color ToColor() => color;

	public float GetFit(Pawn pawn, Comp_TSCosmetics comp)
	{
		var fit = Worker.GetFitValue(pawn, comp);
		var ret = fit switch
		{
			StateFit.Neutral => 0,
			StateFit.Fit => fitWeight,
			StateFit.FitWell => fitWellWeight,
			StateFit.FitHigh => fitHighWeight,
			StateFit.Unfit or _ => unfitWeight,
		};
		// Log.Message($"FIT: {fit} -> {ret}");
		return ret;
	}

	public override void PostLoad()
	{
		// Log.Message($"worker for: {this}: {Worker}, props: {Worker.Props}");
	}

	public override IEnumerable<string> ConfigErrors()
	{
		var worker_props = props;
		if (!typeof(BaseStateWorkerProps).IsAssignableFrom(props.GetType()))
			yield return $"{nameof(props)} '{props}' for '{this}' is not assignable from {typeof(BaseStateWorkerProps)}";

		if (incompatibleStates.Contains(this))
			yield return $"{GetType()} '{this}' has itself in its {nameof(incompatibleStates)}";

		foreach (var er in base.ConfigErrors())
			yield return er;
    }
}
