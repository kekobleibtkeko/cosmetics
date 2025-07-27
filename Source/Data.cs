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


	Additive	= 1 << 30,
	Disabled	= 1 << 31
}

public enum TRTagType
{
	Add,
	Remove
}

public static class TransmoggedData
{
	[StaticConstructorOnStartup]
	public static class Textures
	{
		public static Texture2D CircleLine = GetTexture("UI/circleline");
		public static Texture2D CircleFilled = GetTexture("UI/circlefilled");
		public static Texture2D Arrow = GetTexture("UI/arrow");
		public static Texture2D Expand = GetTexture("UI/expand");
		public static Texture2D Move = GetTexture("UI/move");
		public static Texture2D Orbit = GetTexture("UI/orbit");

		public static Texture2D GetTexture(string relpath) => ContentFinder<Texture2D>.Get($"transmogged/{relpath}");
	}
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

	public void CopyFrom(TRTransform other)
	{
		Rotation = other.Rotation;
		Pivot = other.Pivot;
		Offset = other.Offset;
		Scale = other.Scale;
		RotationOffset = other.RotationOffset;
	}

	public TRTransform Mirror()
	{
		return new(Rotation.Opposite)
		{
			Offset			= new(-Offset.x, Offset.y, Offset.z),
			Scale			= new(Scale.x, Scale.y),
			RotationOffset	= 360 - RotationOffset
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

public class TRTransform4 : IExposable
{
	public TRTransform TransformLeft;
	public TRTransform TransformRight;
	public TRTransform TransformUp;
	public TRTransform TransformDown;

    public TRTransform4()
    {
		TransformLeft	??= new(Rot4.West);
        TransformRight	??= new(Rot4.East);
        TransformUp		??= new(Rot4.North);
        TransformDown	??= new(Rot4.South);
    }

	public void CopyFrom(TRTransform4 other)
	{
		TransformLeft.CopyFrom(other.TransformLeft);
		TransformRight.CopyFrom(other.TransformRight);
		TransformUp.CopyFrom(other.TransformUp);
		TransformDown.CopyFrom(other.TransformDown);
	}

    public TRTransform4 CreateCopy()
	{
		return new()
		{
			TransformLeft	= TransformLeft.CreateCopy(),
			TransformRight	= TransformRight.CreateCopy(),
			TransformUp		= TransformUp.CreateCopy(),
			TransformDown	= TransformDown.CreateCopy()
		};
	}

	public TRTransform4 Mirror()
	{
		return new()
		{
			TransformLeft = TransformLeft.Mirror(),
			TransformRight = TransformRight.Mirror(),
			TransformUp = TransformUp.Mirror(),
			TransformDown = TransformDown.Mirror(),
		};
	}

	public TRTransform ForRot(Rot4 rot)
	{
		return rot.AsInt switch
		{
			Rot4.EastInt		=> TransformRight,
			Rot4.WestInt		=> TransformLeft,
			Rot4.NorthInt		=> TransformUp,
			Rot4.SouthInt or _	=> TransformDown
		};
	}

    public void ExposeData()
    {
        Scribe_Deep.Look(ref TransformLeft, "left");
		Scribe_Deep.Look(ref TransformRight, "right");
		Scribe_Deep.Look(ref TransformUp, "up");
		Scribe_Deep.Look(ref TransformDown, "down");
    }
}

public class TRApparel : IExposable
{
	public ThingDef ApparelDef = default!;
    public ThingStyleDef? StyleDef;
	public BodyTypeDef? BodyDef;
	public ThingDef? RaceDef;
	public Color Color = Color.white;
	public float Scale;

	public TRTransform4 Transform;
		
	public Pawn? Pawn;
	public Lazy<Apparel> InnerApparel;
	public Lazy<DrawData> InnerDrawData;

	public TRApparel()
	{
		InnerApparel	= new(ApparelFactory);
		InnerDrawData	= new(DrawDataFactory);
		Transform	  ??= new();
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
		Transform = copyfrom.Transform.CreateCopy();
	}

	public Func<Apparel> ApparelFactory =>
		() => {
			if (ApparelDef is null)
			{
				throw new Exception("appareldef in TRApparel is null?");
			}
            if (ThingMaker.MakeThing(ApparelDef, GenStuff.DefaultStuffFor(ApparelDef)) is not Apparel apparel)
            {
                Messages.Message("unable to make apparel for transmog cache?", null, MessageTypeDefOf.RejectInput);
				throw new Exception("unable to make apparel for transmog cache?");
            }

			apparel.SetColor(Color, false);
			apparel.StyleDef = StyleDef;
			apparel.holdingOwner = Pawn?.apparel.GetDirectlyHeldThings();
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

			clone.dataEast	= clone.dataEast.Value.ApplyTRTransform(Transform.ForRot(Rot4.East));
			clone.dataWest	= clone.dataWest.Value.ApplyTRTransform(Transform.ForRot(Rot4.West));
			clone.dataNorth = clone.dataNorth.Value.ApplyTRTransform(Transform.ForRot(Rot4.North));
			clone.dataSouth = clone.dataSouth.Value.ApplyTRTransform(Transform.ForRot(Rot4.South));

			clone.scale = Scale;

			return clone;
		};

	public Apparel GetApparel() => (InnerApparel ??= new(ApparelFactory)).Value;
	public DrawData GetDrawData() => (InnerDrawData ??= new(DrawDataFactory)).Value;

	public TRTransform GetTransformFor(Rot4 rot) => Transform.ForRot(rot);

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
			BodyDef = BodyDef,
			Color = Color,
			RaceDef = RaceDef,
            Transform = Transform.CreateCopy(),
        };
        return nap.For(forpawn ?? Pawn);
	}

	public TRApparel For(Pawn? pawn)
	{
		Pawn = pawn;
		return this;
	}

    public void ExposeData()
	{
		Scribe_Defs.Look(ref ApparelDef, "apparelDef");
		Scribe_Defs.Look(ref StyleDef, "styleDef");
		Scribe_Defs.Look(ref BodyDef, "bodyDef");
		Scribe_Defs.Look(ref RaceDef, "race");
		Scribe_Values.Look(ref Color, "color", defaultValue: Color.white);
		Scribe_Values.Look(ref Scale, "scale");

		Scribe_Deep.Look(ref Transform, "trs");

		Scribe_References.Look(ref Pawn, "pawn");
	}
}

public struct TRTag : IExposable
{
	public TRTagType TagType;
	public string Name = string.Empty;

	public TRTag(TRTagType tagType, string name) : this(name)
    {
        TagType = tagType;
    }

    public TRTag(string name) : this()
    {
        Name = name;
    }

    public TRTag()
    {
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref TagType, "type");
		Scribe_Values.Look(ref Name!, "name");
    }
}

public class TRApparelSet : IExposable
{
	public Pawn Pawn = default!;
    public string Name = "New Set";
	public TRState State;
	public List<TRApparel> Apparel = [];
	public List<TRTag> Tags = [];
	public HairDef? SetHair;
	public BeardDef? SetBeard;

	[Obsolete("do not use directly")] public TRApparelSet() { }
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
			
			case TRState.Additive:
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
			State = State,
			Tags = new(Tags),
			Name = $"{Name} Copy"
		};
		foreach (var ap in Apparel)
		{
			nset.Apparel.Add(ap.CreateCopy());
		}
		return nset;
	}

	public TRApparelSet For(Pawn pawn)
	{
		Pawn = pawn;
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

	public void ExposeData()
	{
		Scribe_References.Look(ref Pawn, "pawn");
		Scribe_Values.Look(ref Name!, "name");
		Scribe_Values.Look(ref State, "state");
		Scribe_Collections.Look(ref Apparel, "apparel");
		Scribe_Collections.Look(ref Tags, "tags");
		
		Scribe_Defs.Look(ref SetHair, "hair");
		Scribe_Defs.Look(ref SetBeard, "beard");
	}

	public float GetSetPoints(Pawn pawn, bool? outside = null, float? temp = null)
	{
		if (State.HasFlag(TRState.Disabled))
			return Comp_Transmogged.FLAG_UNFIT;

		pawn ??= Pawn ?? throw new NullReferenceException("attempted to get set on null pawn");

		if (!outside.HasValue)
		{
			try
			{
				outside = pawn.IsOutside();
			}
			catch (System.Exception e)
			{
				Debug.LogError($"CAUGHT: {e}{e.StackTrace}");
				outside = false;
			}
		}

		temp ??= pawn.AmbientTemperature;
		float curval = 0;
			
		if ((State & (TRState.NonDrafted | TRState.Drafted)) > 0)
		{
			if (pawn.Drafted)
			{
				curval += State.HasFlag(TRState.Drafted)
					? Comp_Transmogged.FLAG_FIT_HIGH
					: Comp_Transmogged.FLAG_UNFIT;
			}
			else
			{
				curval += State.HasFlag(TRState.NonDrafted)
					? Comp_Transmogged.FLAG_FIT_HIGH
					: Comp_Transmogged.FLAG_UNFIT;
			}
		}

		if ((State & (TRState.Indoors | TRState.Outdoors)) > 0)
		{
			if (outside ?? true)
			{
				curval += State.HasFlag(TRState.Outdoors)
					? Comp_Transmogged.FLAG_FIT_WELL
					: Comp_Transmogged.FLAG_UNFIT;
			}
			else
			{
				curval += State.HasFlag(TRState.Indoors)
					? Comp_Transmogged.FLAG_FIT_WELL
					: Comp_Transmogged.FLAG_UNFIT;
			}
		}

		if ((State & (TRState.Cold | TRState.Hot)) > 0)
		{
			if (temp <= TransmoggedSettings.Instance.MaxColdTemp)
			{
				curval += State.HasFlag(TRState.Cold)
					? Comp_Transmogged.FLAG_FIT
					: Comp_Transmogged.FLAG_UNFIT;
			}
			else if (temp >= TransmoggedSettings.Instance.MinHotTemp)
			{
				curval += State.HasFlag(TRState.Hot)
					? Comp_Transmogged.FLAG_FIT
					: Comp_Transmogged.FLAG_UNFIT;
			}
			else
			{
				curval -= Comp_Transmogged.FLAG_FIT_WELL;
			}
		}

		if (State.HasFlag(TRState.Sleep))
		{
			curval += (pawn?.health?.Dead ?? true) || pawn.Awake()
				? Comp_Transmogged.FLAG_UNFIT
				: Comp_Transmogged.FLAG_FIT_HIGH;
		}
		
		return curval;
	}
}
