using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Mod;
using Verse;

namespace Cosmetics.Util;

public static class CosmeticsUtil
{
    public static string ModTranslate(this string input) => Translator.Translate($"{CosmeticsMod.ID}.{input}");
}
