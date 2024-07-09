using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Transmogged;
#nullable enable

public static class TransmoggedUtility
{
	public static TRApparelSet? SetClipboard;
	public static TRApparel? ApparelClipboard;
	public static TRApparel? OffsetClipboard;

	public static Color ColorFromHTML(string text)
	{
		if (ColorUtility.TryParseHtmlString(text, out var color))
			return color;
		return Color.red;
	}

	public static Color Darken(this Color clr, float t) => Color.LerpUnclamped(clr, Color.black, t);
	public static Color Saturate(this Color color, float t)
	{
		Color.RGBToHSV(color, out float hue, out float saturation, out float brightness);
		return Color.HSVToRGB(hue, saturation * t, brightness);
	}
	
	public static Color ToColor(this TRState state) => state switch
    {
        TRState.NonDrafted	=> ColorFromHTML("#32a8a4"),
        TRState.Drafted		=> ColorFromHTML("#a85d32"),
        
		TRState.Indoors		=> ColorFromHTML("#3261a8"),
        TRState.Outdoors	=> ColorFromHTML("#32a852"),

        TRState.Cold		=> ColorFromHTML("#58d1cb"),
        TRState.Hot			=> ColorFromHTML("#d43737"),

		TRState.Sleep		=> ColorFromHTML("#8c25e6"),

		TRState.Disabled	=> new Color(1, 0, 0),
		TRState.None or _	=> new Color(.5f, .5f, .5f)
    };

	public static Rect ShrinkRight(this Rect rect, float p) => new(rect.x, rect.y, rect.width - p, rect.height);
	public static Rect ShrinkLeft(this Rect rect, float p) => new(rect.x + p, rect.y, rect.width - p, rect.height);
	public static Rect ShrinkTop(this Rect rect, float p) => new(rect.x, rect.y + p, rect.width, rect.height - p);
	public static Rect ShrinkBottom(this Rect rect, float p) => new(rect.x, rect.y, rect.width, rect.height - p);

	public static Rect GrowRight(this Rect rect, float p) => ShrinkRight(rect, -p);
	public static Rect GrowLeft(this Rect rect, float p) => ShrinkLeft(rect, -p);
	public static Rect GrowTop(this Rect rect, float p) => ShrinkTop(rect, -p);
	public static Rect GrowBottom(this Rect rect, float p) => ShrinkBottom(rect, -p);

	public static Rect Move(this Rect rect, float x = 0, float y = 0) => new(rect.x + x, rect.y + y, rect.width, rect.height);

	public static Rect Square(this Rect rect) => new(rect.x, rect.y, Mathf.Min(rect.width, rect.height), Mathf.Min(rect.width, rect.height));

	public static void SetFlag<T>(this ref T flags, T flag, bool state)
		where T : struct, Enum
	{
		flags = (T)(object)(state
			? Convert.ToInt32(flags) | Convert.ToInt32(flag)	// SetFlag
			: Convert.ToInt32(flags) & ~Convert.ToInt32(flag)	// ClearFlag 
		);
	}

	public static void ToggleFlag<T>(this ref T flags, T flag)
		where T : struct, Enum
	{
		SetFlag(ref flags, flag, !flags.HasFlag(flag));
	}

	public static T? DirtyClone<T>(this T obj)
	{
		if (object.Equals(obj, default(T)))
			return default;
		return (T)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(obj, null);
	}

	public static DrawData.RotationalData ApplyTRTransform(this DrawData.RotationalData rotdata, TRTransform transform)
	{
		rotdata.offset = transform.Offset;
		rotdata.rotation = transform.Rotation;
		rotdata.pivot = transform.Pivot;
		rotdata.rotationOffset = transform.RotationOffset;
		return rotdata;
	}

	public static string GetValueLabel<T>(string name, T value)
	{
		return $"{name} ({value})";
	}

	public static bool SliderLabeledWithValue(
		this Listing_Standard list,
		ref float value,
		string name,
		float min,
		float max,
		ref string? editbuffer,
		string? tt = null,
		float? resetval = null,
		float? accuracy = null
	) {
		var prevfont = Text.Font;
		Text.Font = GameFont.Small;

		float orig = value;
		var rect = list.GetRect(50);
		Widgets.DrawWindowBackground(rect);
		Widgets.Label(rect, name);

		var valrect = rect.LeftPart(.9f).RightHalf();
		string? prevstr = editbuffer;

		editbuffer ??= value.ToString();
		editbuffer = Widgets.TextField(valrect.TopHalf(), editbuffer);

		if (!string.Equals(editbuffer, prevstr)
			&& !string.IsNullOrEmpty(editbuffer)
			&& editbuffer.IsFullyTypedNumber<float>()
			&& editbuffer != "-")
		{
			value = float.Parse(editbuffer);
		}
		else if(!string.IsNullOrEmpty(editbuffer)
			&& !editbuffer.EndsWith(".")
			&& editbuffer != "-")
		{
			editbuffer = value.ToString();
		}

		value = Widgets.HorizontalSlider(
			valrect.BottomHalf(),
			value,
			min, max,
			roundTo: accuracy ?? .01f
		);

		var resetrect = rect
				.RightPart(.1f)
				.Square()
				.ExpandedBy(-3);

		if (resetval.HasValue && Widgets.ButtonImage(resetrect, TexButton.Delete))
		{
			value = resetval.Value;
		}

		if (tt is not null)
			TooltipHandler.TipRegion(rect, tt);

		Text.Font = prevfont;
		return orig != value;
	}
}


[Flags]
public enum TRState
{
	None 		= 0,
	NonDrafted 	= 1 << 0,
	Drafted 	= 1 << 1,
	Indoors 	= 1 << 2,
	Outdoors 	= 1 << 3,
	Cold		= 1 << 4,
	Hot			= 1 << 5,
	Sleep		= 1 << 6,

	Disabled	= 1 << 31
}

public class TRTransform : IExposable
{
	public Rot4 Rotation;
	public Vector2 Pivot = DrawData.PivotCenter;
	public Vector3 Offset;
	public Vector2 Scale = Vector2.one;
	public float RotationOffset;

	[Obsolete("do not use")] public TRTransform() { }
    public TRTransform(Rot4 rotation)
    {
        Rotation = rotation;
    }

	public TRTransform CreateCopy()
	{
		return new(Rotation)
		{
			Pivot = Pivot,
			Offset = Offset,
			Scale = Scale,
			RotationOffset = RotationOffset
		};
	}

    public void ExposeData()
	{
		Scribe_Values.Look(ref Rotation, "rotation");
		Scribe_Values.Look(ref RotationOffset, "rotoffset");
		Scribe_Values.Look(ref Pivot, "pivot", DrawData.PivotCenter);
		TransmoggedSaveUtility.LookAccurate(ref Offset, "offset", default);
		TransmoggedSaveUtility.LookAccurate(ref Scale, "scale", Vector2.one);
	}
}

public class TRApparel : IExposable
{
	public ThingDef ApparelDef = default!;
    public ThingStyleDef? StyleDef;
	public Color Color = Color.white;
	public float Scale;

	public TRTransform TransformLeft;
	public TRTransform TransformRight;
	public TRTransform TransformUp;
	public TRTransform TransformDown;
		
	public Pawn Pawn = default!;
	public Lazy<Apparel> InnerApparel;
	public Lazy<DrawData> InnerDrawData;

	public TRApparel()
	{
		InnerApparel	= new(ApparelFactory);
		InnerDrawData	= new(DrawDataFactory);
		TransformLeft	??= new(Rot4.West);
        TransformRight	??= new(Rot4.East);
        TransformUp		??= new(Rot4.North);
        TransformDown	??= new(Rot4.South);
	}
    public TRApparel(ThingDef apparelDef, Pawn pawn) : this()
    {
        ApparelDef = apparelDef;
		Pawn = pawn;
		Scale = 1;
    }

	public void CopyTransforms(TRApparel copyfrom)
	{
		Scale = copyfrom.Scale;
		TransformLeft = copyfrom.TransformLeft.CreateCopy();
		TransformRight = copyfrom.TransformRight.CreateCopy();
		TransformUp = copyfrom.TransformUp.CreateCopy();
		TransformDown = copyfrom.TransformDown.CreateCopy();
	}

	public Func<Apparel> ApparelFactory =>
		() => {
            if (ThingMaker.MakeThing(ApparelDef, GenStuff.DefaultStuffFor(ApparelDef)) is not Apparel apparel)
            {
                Messages.Message("unable to make apparel for transmog cache?", null, MessageTypeDefOf.RejectInput);
				throw new Exception("unable to make apparel for transmog cache?");
            }

			apparel.SetColor(Color, false);
			apparel.StyleDef = StyleDef;
            return apparel;
		};

	public Func<DrawData> DrawDataFactory =>
		() => {
			var ap = GetApparel();
			var orig = ap.def.apparel.drawData;
			var clone = orig?.DirtyClone() ?? new();

			clone.defaultData = new();
			clone.dataEast	= clone.dataEast?.DirtyClone()	?? new(Rot4.East, 0)	{ pivot = DrawData.PivotCenter, layer = null };
			clone.dataWest	= clone.dataWest?.DirtyClone()	?? new(Rot4.West, 0)	{ pivot = DrawData.PivotCenter, layer = null };
			clone.dataNorth = clone.dataNorth?.DirtyClone()	?? new(Rot4.North, 0)	{ pivot = DrawData.PivotCenter, layer = null };
			clone.dataSouth = clone.dataSouth?.DirtyClone()	?? new(Rot4.South, 0)	{ pivot = DrawData.PivotCenter, layer = null };

			clone.dataEast	= clone.dataEast.Value.ApplyTRTransform(TransformRight);
			clone.dataWest	= clone.dataWest.Value.ApplyTRTransform(TransformLeft);
			clone.dataNorth = clone.dataNorth.Value.ApplyTRTransform(TransformUp);
			clone.dataSouth = clone.dataSouth.Value.ApplyTRTransform(TransformDown);

			clone.scale = Scale;

			return clone;
		};

	public Apparel GetApparel() => (InnerApparel ??= new(ApparelFactory)).Value;
	public DrawData GetDrawData() => (InnerDrawData ??= new(DrawDataFactory)).Value;

	public TRTransform GetTransformFor(Rot4 rot)
	{
		return rot.AsInt switch
		{
			Rot4.EastInt		=> TransformRight,
			Rot4.WestInt		=> TransformLeft,
			Rot4.NorthInt		=> TransformUp,
			Rot4.SouthInt or _	=> TransformDown
		};
	}

	public void SetApparelDirty() => InnerApparel = (InnerApparel ??= new(ApparelFactory)).IsValueCreated
			? new(ApparelFactory)
			: InnerApparel;

	public void SetDrawDataDirty() => InnerDrawData = (InnerDrawData ??= new(DrawDataFactory)).IsValueCreated
			? new(DrawDataFactory)
			: InnerDrawData;

	public TRApparel CreateCopy(Pawn? forpawn = null)
	{
        TRApparel nap = new()
        {
            Scale = Scale,
			ApparelDef = ApparelDef,
			StyleDef = StyleDef,
			Color = Color,
            TransformLeft = TransformLeft.CreateCopy(),
            TransformRight = TransformRight.CreateCopy(),
            TransformUp = TransformUp.CreateCopy(),
            TransformDown = TransformDown.CreateCopy()
        };
        return nap.For(forpawn ?? Pawn);
	}

	public TRApparel For(Pawn pawn)
	{
		Pawn = pawn;
		return this;
	}

    public void ExposeData()
	{
		Scribe_Defs.Look(ref ApparelDef, "apparelDef");
		Scribe_Defs.Look(ref StyleDef, "styleDef");
		Scribe_Values.Look(ref Color, "color", defaultValue: Color.white);
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
	public Pawn Pawn = default!;
    public string Name = "New Set";
	public TRState State;
	public List<TRApparel> Apparel = new();

	[Obsolete("do not use")] public TRApparelSet() { }
	public TRApparelSet(Pawn pawn)
    {
        Pawn = pawn;
    }

	public void NotifyUpdate() => Pawn.GetComp<Comp_Transmogged>()?.NotifyUpdate();

	public void StateToggled(TRState state)
	{
		State.ToggleFlag(state);
        switch (state)
        {
            case TRState.None:
                State = TRState.None;
                break;
            case TRState.NonDrafted:
                State.SetFlag(TRState.Drafted, false);
                break;
            case TRState.Drafted:
                State.SetFlag(TRState.NonDrafted, false);
                break;
            case TRState.Indoors:
                State.SetFlag(TRState.Outdoors, false);
                break;
            case TRState.Outdoors:
                State.SetFlag(TRState.Indoors, false);
                break;
            case TRState.Cold:
				State.SetFlag(TRState.Hot, false);
                break;
            case TRState.Hot:
				State.SetFlag(TRState.Cold, false);
                break;
			
			case TRState.Disabled:
                break;
        }
        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
		NotifyUpdate();
    }

	public TRApparelSet CreateCopy()
	{
		var nset = new TRApparelSet(Pawn)
		{
			State = State
		};
		foreach (var ap in Apparel)
		{
			nset.Apparel.Add(ap.CreateCopy());
		}
		return nset;
	}

	public TRApparelSet For(Pawn pawn)
	{
		foreach (var ap in Apparel)
			ap.For(pawn);
		return this;
	}

    public TRApparel AddNew(ThingDef def)
	{
		TRApparel res = new(def, Pawn);
		Apparel.Add(res);
		NotifyUpdate();
		return res;
	}

	public TRApparel AddFromApparel(Apparel apparel)
	{
		var res = AddNew(apparel.def);
		res.Color = apparel.DrawColor;
		
		res.StyleDef = apparel.StyleDef;
		NotifyUpdate();
		return res;
	}

	public bool RemoveApparel(TRApparel apparel)
	{
		var succ = Apparel.Remove(apparel);
		NotifyUpdate();
		return succ;
	}

	public DrawData GetDrawDataFor(Apparel apparel)
	{
		foreach (var ap in Apparel)
		{
			var inner = ap.GetApparel();
			if (apparel == inner)
				return ap.GetDrawData();
		}
		return apparel.def.apparel.drawData;
	}

	public bool TryGetTRApparel(Apparel apparel, out TRApparel? tr)
	{
		tr = null;
		foreach (var ap in Apparel)
		{
			var inner = ap.GetApparel();
			if (apparel == inner)
			{
				tr = ap;
				return true;
			}
		}
		return false;
	}

	public void ExposeData()
	{
		Scribe_References.Look(ref Pawn, "pawn");
		Scribe_Values.Look(ref Name!, "name");
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
	public const float FLAG_UNFIT = -999;
	public const float FLAG_FIT = 1;
	public const float FLAG_FIT_WELL = 2;
	public const float FLAG_FIT_HIGH = 3;

	public bool Enabled;
	public int PrimedStack = 0;
	public int UnprimedStack = 0;

	public TickTimer Timer;
	public List<TRApparelSet> ApparelSets = new();

	public TRApparelSet? ActiveSet;

    public Comp_Transmogged()
    {
        Timer = new();
    }

    public Pawn Pawn => (parent as Pawn) ?? throw new Exception("pawn was null in transmogged comp");

	public void NotifyUpdate()
	{
		Pawn.apparel.Notify_ApparelChanged();
	}

    public void SetEnabled(bool active)
	{
		if (Enabled != active)
			NotifyUpdate();
		Enabled = active;
	}

	public void MakeNewSet()
	{
		ActiveSet = new TRApparelSet(Pawn);
	}

	public void CopySet(TRApparelSet set)
	{
		var nset = set.CreateCopy();
		ApparelSets.Add(nset);
		ActiveSet = nset;
	}

	public TRState GetCurrentPawnState()
	{
		if (!Enabled)
			return TRState.None;
		
		return Pawn.Drafted
			? TRState.Drafted
			: TRState.NonDrafted;
	}

    public override void CompTick()
    {
		int tickstime = 60; // TODO: make configurable
		if ((Timer ??= new()).Finished)
		{
			Timer.Start(GenTicks.TicksGame, tickstime, NotifyUpdate);
		}
		Timer.TickInterval();
    }

	public bool TryGetCurrentTransmog(out TRApparelSet? set)
	{
		set = null;

		float maxval = 0;

		bool outside;
		try
		{
			outside = Pawn.IsOutside(); // throws here in some circumstances, unsure which
		}
		
		catch (System.Exception e)
		{
			Debug.LogError($"CAUGHT: {e}{e.StackTrace}");
			outside = true;
		}

		float temp = Pawn.AmbientTemperature;

		foreach (var apset in ApparelSets ??= new())
		{
			float curval = 0;
			if (apset.State.HasFlag(TRState.Disabled))
				continue;
			
			if ((apset.State & (TRState.NonDrafted | TRState.Drafted)) > 0)
			{
				if (Pawn.Drafted)
				{
					curval += apset.State.HasFlag(TRState.Drafted)
						? FLAG_FIT_HIGH
						: FLAG_UNFIT;
				}
				else
				{
					curval += apset.State.HasFlag(TRState.NonDrafted)
						? FLAG_FIT_HIGH
						: FLAG_UNFIT;
				}
			}

			if ((apset.State & (TRState.Indoors | TRState.Outdoors)) > 0)
			{
				if (outside)
				{
					curval += apset.State.HasFlag(TRState.Outdoors)
						? FLAG_FIT_WELL
						: FLAG_UNFIT;
				}
				else
				{
					curval += apset.State.HasFlag(TRState.Indoors)
						? FLAG_FIT_WELL
						: FLAG_UNFIT;
				}
			}

			if ((apset.State & (TRState.Cold | TRState.Hot)) > 0)
			{
				if (temp <= TransmoggedSettings.Instance.MaxColdTemp)
				{
					curval += apset.State.HasFlag(TRState.Cold)
						? FLAG_FIT
						: FLAG_UNFIT;
				}
				else if (temp >= TransmoggedSettings.Instance.MinHotTemp)
				{
					curval += apset.State.HasFlag(TRState.Hot)
						? FLAG_FIT
						: FLAG_UNFIT;
				}
				else
				{
					curval -= FLAG_FIT_WELL;
				}
			}

			if (apset.State.HasFlag(TRState.Sleep))
			{
				curval += Pawn.Awake()
					? FLAG_UNFIT
					: FLAG_FIT_HIGH;
			}

			if (curval < maxval)
				continue;

			maxval = curval;
			set = apset;
		}

		return set != null;
	}

	public IEnumerable<Apparel> GetApparel()
	{
		if (Enabled)
		{
			if (TryGetCurrentTransmog(out var set))
				return set!.Apparel.Select(x => x.InnerApparel.Value);
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
		Scribe_Values.Look(ref Enabled, "TR_active");
		Scribe_Collections.Look(ref ApparelSets, "TR_sets");
		Scribe_Deep.Look(ref Timer, "TR_timer");
	}
}