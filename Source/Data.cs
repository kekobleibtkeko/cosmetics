using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Transmogged;

public static class TransmoggedUtility
{
	public static Color ColorFromHTML(string text)
	{
		if (ColorUtility.TryParseHtmlString(text, out var color))
			return color;
		return Color.red;
	}

	public static Color Darken(this Color clr, float t) => Color.LerpUnclamped(clr, Color.black, t);
	
	public static Color ToColor(this TRState state) => state switch
	{
		TRState.NonDrafted	=> ColorFromHTML("#19d2f7"),
		TRState.Drafted		=> ColorFromHTML("#f76719"),
		TRState.Indoors		=> ColorFromHTML("#0932ba"),
		TRState.Outdoors	=> ColorFromHTML("#6eba09"),
		TRState.None or _	=> new Color(.5f, .5f, .5f)
	};
}

[Flags]
public enum TRState
{
	None = 0,
	NonDrafted = 1 << 0,
	Drafted = 1 << 1,
	Indoors = 1 << 2,
	Outdoors = 1 << 3,
}

public class TRTransform : IExposable
{
	public Rot4 Rotation;
	public Vector2 Pivot;
	public Vector3 Offset;

	public void ExposeData()
	{
		Scribe_Values.Look(ref Rotation, "rotation");
		Scribe_Values.Look(ref Pivot, "pivot");
		Scribe_Values.Look(ref Offset, "offset");
	}
}

public class TRApparel : IExposable
{
	public ThingDef ApparelDef;
	public ThingStyleDef StyleDef;
	public Color Color;
	public float Scale;

	public TRTransform TransformLeft;
	public TRTransform TransformRight;
	public TRTransform TransformUp;
	public TRTransform TransformDown;
		
	public Pawn Pawn;
	public Apparel CachedApparel;


	public void ExposeData()
	{
		Scribe_Defs.Look(ref ApparelDef, "apparelDef");
		Scribe_Defs.Look(ref StyleDef, "styleDef");
		Scribe_Values.Look(ref Color, "color");
		Scribe_Values.Look(ref Scale, "scale");

		Scribe_Deep.Look(ref TransformLeft, "trLeft");
		Scribe_Deep.Look(ref TransformRight, "trRight");
		Scribe_Deep.Look(ref TransformUp, "trUp");
		Scribe_Deep.Look(ref TransformDown, "trDown");

		Scribe_References.Look(ref Pawn, "pawn");
	}
}

public class TRApparelSet : IExposable
{
	public string Name = "New Set";
	public TRState State;
	public List<TRApparel> Apparel = new();

	public void ExposeData()
	{
		Scribe_Values.Look(ref Name, "name");
		Scribe_Values.Look(ref State, "state");
		Scribe_Collections.Look(ref Apparel, "apparel");
	}
}

public class CompProperties_Transmogged : CompProperties
{
	public CompProperties_Transmogged()
	{
		compClass = typeof(Comp_Transmogged);
	}
}

public class Comp_Transmogged : ThingComp
{
	public bool Enabled;
	public List<TRApparelSet> ApparelSets = new();

	public TRState CurrentState;
	public TRApparelSet ActiveSet;

	public Pawn Pawn => parent as Pawn;

	public void NotifyUpdate()
	{
		Pawn.apparel.Notify_ApparelChanged();
	}

	public void StateToggled(TRState state)
	{
		
	}

	public void SetEnabled(bool active)
	{
		if (Enabled != active)
			NotifyUpdate();
		Enabled = active;
	}

	public void MakeNewSet()
	{
		ActiveSet = new TRApparelSet();
	}

	public TRState GetCurrentPawnState()
	{
		if (!Enabled)
			return TRState.None;
		
		return Pawn.Drafted
			? TRState.Drafted
			: TRState.NonDrafted;
	}

	public bool TryGetCurrentTransmog(out TRApparelSet set)
	{
		set = null;

		return set != null;
	}

	public IEnumerable<Apparel> GetApparel()
	{
		if (Enabled)
		{
			if (TryGetCurrentTransmog(out var set))
				return set.Apparel.Select(x => x.CachedApparel);
			else
				return Enumerable.Empty<Apparel>();
		}
		else
		{
			return Pawn.apparel.WornApparel;
		}
	}

	public override void PostExposeData()
	{
		Scribe_Values.Look(ref Enabled, "active");
		Scribe_Collections.Look(ref ApparelSets, "sets");
	}
}