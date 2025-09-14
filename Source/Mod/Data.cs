using UnityEngine;
using Verse;

namespace Cosmetics.Mod;

public static class CosmeticsData
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

		public static Texture2D GetTexture(string relpath) => ContentFinder<Texture2D>.Get($"{relpath}");
	}
}