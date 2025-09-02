using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmetics.Comp;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace Cosmetics.Util;

[StaticConstructorOnStartup]
public static class ColorHandlerRegister
{
    static ColorHandlerRegister()
    {
        TSUtil.RegisterToColorHandler<Comp_TSCosmetics.CompState>(state => state switch
        {
            Comp_TSCosmetics.CompState.AutoTransforms => TSUtil.ColorFromHTML("#449bc9"),
            Comp_TSCosmetics.CompState.Enabled => TSUtil.ColorFromHTML("#ffb108"),
            Comp_TSCosmetics.CompState.Disabled or _ => TSUtil.ColorFromHTML("#ff2b1c"),
        });
    }
}
