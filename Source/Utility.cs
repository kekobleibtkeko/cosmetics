using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Transmogged;
#nullable enable

public static class TransmoggedUtility
{
	public static TRApparelSet? SetClipboard;
	public static TRApparel? ApparelClipboard;
	public static TRApparel? OffsetClipboard;

	public static TRTransform? TransformClipboard;
	public static TRTransform4? Transform4Clipboard;

	public static IDictionary<(Texture2D, float), Texture2D> TextureRotateCache = new Dictionary<(Texture2D, float), Texture2D>();

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
		TRState.Additive	=> ColorFromHTML("#eaf04a"),

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

	public static V Ensure<K, V>(this IDictionary<K, V> dict, K key)
		where V : new()
	{
		if (!dict.TryGetValue(key, out var val))
		{
			val = new();
			dict.Add(key, val);
		}
		return val;
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

	public static string GetAutoBodyKey(this Pawn pawn)
	{
		string race = "Human";
		if (TransmoggedSettings.IsHARLoaded && pawn.def is AlienRace.ThingDef_AlienRace alrace)
		{
			race = alrace.defName;
		}
		return $"{race}.{pawn.story.bodyType.defName}";
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
		float margin = 5;
		var prevfont = Text.Font;
		// Text.Font = GameFont.Small;

		float orig = value;
		var rect = list.GetRect(50);
		Widgets.DrawWindowBackground(rect);
		Widgets.Label(rect.Move(margin, margin), name);

		var valrect = rect.LeftPart(.9f).RightHalf();
		string? prevstr = editbuffer;

		editbuffer ??= value.ToString();
		editbuffer = Widgets.TextField(valrect.TopHalf().ShrinkTop(margin), editbuffer);

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
			valrect.BottomHalf().ShrinkTop(margin * 1.5f),
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

	public static bool TryGetTRApparel(this IEnumerable<TRApparel> vset, Apparel apparel, out TRApparel? tr)
	{
		tr = null;
		foreach (var ap in vset)
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

	public static DrawData GetDrawDataFor(this IEnumerable<TRApparel> vset, Apparel apparel)
	{
		foreach (var ap in vset)
		{
			var inner = ap.GetApparel();
			if (apparel == inner)
				return ap.GetDrawData();
		}
		return apparel.def.apparel.drawData;
	}

	public static void DrawFitted(this Texture2D tex, Rect rect, Color? color = null, float scale = 1f, float rotation = 0f)
	{
		if (rotation != 0)
			tex = tex.GetRotatedCached(rotation);

		using (new GUIColor_D(color ?? GUI.color))
			Widgets.DrawTextureFitted(rect, tex, scale, new(1, 1), new(0, 0, 1, 1));
	}
#region rotation
	/// <summary>
	/// from https://stackoverflow.com/questions/58873582/what-is-arraypool-in-netcore-c-sharp
	/// </summary>
	/// <param name="tex"></param>
	/// <param name="angleDegrees"></param>
	public static void RotateImage(Texture2D tex, float angleDegrees)
	{
		int width = tex.width;
		int height = tex.height;
		float halfHeight = height * 0.5f;
		float halfWidth = width * 0.5f;

		var texels = tex.GetRawTextureData<Color32>();        
		// var copy = System.Buffers.ArrayPool<Color32>.Shared.Rent(texels.Length);
		var copy = new Color32[texels.Length];
		Unity.Collections.NativeArray<Color32>.Copy(texels, copy, texels.Length);

		float phi = Mathf.Deg2Rad * angleDegrees;
		float cosPhi = Mathf.Cos(phi);
		float sinPhi = Mathf.Sin(phi);

		int address = 0;
		for (int newY = 0; newY < height; newY++)
		{
			for (int newX = 0; newX < width; newX++)
			{
				float cX = newX - halfWidth;
				float cY = newY - halfHeight;
				int oldX = Mathf.RoundToInt(cosPhi * cX + sinPhi * cY + halfWidth);
				int oldY = Mathf.RoundToInt(-sinPhi * cX + cosPhi * cY + halfHeight);
				bool InsideImageBounds = (oldX > -1) & (oldX < width)
									& (oldY > -1) & (oldY < height);
				
				texels[address++] = InsideImageBounds ? copy[oldY * width + oldX] : default;
			}
		}

		// No need to reinitialize or SetPixels - data is already in-place.
		tex.Apply(true);
	}
	public static Texture2D Rotated(this Texture2D tex_gpu, float angle)
	{
		var rt = new RenderTexture(tex_gpu.width, tex_gpu.height, 1, UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
		var tex_cpu = new Texture2D(tex_gpu.width, tex_gpu.height);

		Graphics.Blit(tex_gpu, rt);

		using (new ActiveRT_D(rt))
			tex_cpu.ReadPixels(new(0, 0, tex_gpu.width, tex_gpu.height), 0, 0);

		RotateImage(tex_cpu, -angle);
		return tex_cpu;
	}

	public static Color GetPixelColor(this Texture2D tex, float x, float y)
	{
		Color pix;
		int x1 = (int) Mathf.Floor(x);
		int y1 = (int) Mathf.Floor(y);

		if(x1 > tex.width || x1 < 0 ||
		y1 > tex.height || y1 < 0) {
			pix = Color.clear;
		} else {
			pix = tex.GetPixel(x1,y1);
		}
	
		return pix;
	}

	public static float RotX(float angle, float x, float y) {
		float cos = Mathf.Cos(angle/180.0f*Mathf.PI);
		float sin = Mathf.Sin(angle/180.0f*Mathf.PI);
		return (x * cos + y * (-sin));
	}
	public static float RotY(float angle, float x, float y) {
		float cos = Mathf.Cos(angle/180.0f*Mathf.PI);
		float sin = Mathf.Sin(angle/180.0f*Mathf.PI);
		return (x * sin + y * cos);
	}
#endregion

	public static Rect WithTT(this Rect rect, string key)
	{
		TooltipHandler.TipRegionByKey(rect, key);
		return rect;
	}

	public static Texture2D GetRotatedCached(this Texture2D tex, float angle)
	{
		var key = (tex, angle);
		if (!TextureRotateCache.TryGetValue(key, out var res))
		{
			res = TextureRotateCache[key] = tex.Rotated(angle);
		}
		return res;
	}

	public abstract class UIDisposableHelper<T> : IDisposable
	{
		public T OldValue { get; }
		public abstract void SetValue(T value);
		public abstract T GetValue();

		public UIDisposableHelper(T value)
		{
			OldValue = GetValue();
			SetValue(value);
		}

		public virtual void Dispose()
		{
			SetValue(OldValue);
		}
	}

	public class TextSize_D(GameFont value) : UIDisposableHelper<GameFont>(value)
	{
        public override GameFont GetValue() => Text.Font;
		public override void SetValue(GameFont value) => Text.Font = value;
	}

	public class GUIColor_D(Color value) : UIDisposableHelper<Color>(value)
	{
        public override Color GetValue() => GUI.color;
		public override void SetValue(Color value) => GUI.color = value;
	}

    public class ActiveRT_D(RenderTexture value) : UIDisposableHelper<RenderTexture>(value)
    {
        public override RenderTexture GetValue() => RenderTexture.active;
        public override void SetValue(RenderTexture value) => RenderTexture.active = value;
    }

    public class Listing_D(Rect value) : UIDisposableHelper<Rect>(value)
    {
        public readonly Listing_Standard Listing = new();
		
        public override Rect GetValue() => Listing.listingRect;
        public override void SetValue(Rect value) => Listing.Begin(value);

        public override void Dispose()
        {
			Listing.End();
        }
    }

    public class TextAnchor_D(TextAnchor value) : UIDisposableHelper<TextAnchor>(value)
    {
        public override TextAnchor GetValue() => Text.Anchor;
        public override void SetValue(TextAnchor value) => Text.Anchor = value;
    }
}
