using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cosmetics.Data;

public class BaseStateWorker
{
    public enum StateFit
    {
        Neutral,
        Fit,
        FitWell,
        FitHigh,
        Unfit,
    }

    public virtual StateFit GetFitValue() => StateFit.Neutral;
}
