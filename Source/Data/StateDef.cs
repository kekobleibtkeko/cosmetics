using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Cosmetics.Data;

public class StateDef : Def
{
    [MayTranslate]
    public string shortLabel = "XX";
    public float order;
    public Color color;
    public Type stateWorker = typeof(BaseStateWorker);
    public float fitWeight = 1.0f;
    public float fitWellWeight = 2.0f;
    public float fitHighWeight = 3.0f;
    public float unfitWeight = -20.0f;

    public List<StateDef> incompatibleStates = [];

    private readonly Lazy<BaseStateWorker> _Worker;
    public BaseStateWorker Worker => _Worker.Value;
    
    public StateDef()
    {
        _Worker = new(() => stateWorker.CreateInstance() as BaseStateWorker ?? throw new Exception($"unable to create instance of worker for '{this}'"));
    }

    public override IEnumerable<string> ConfigErrors()
    {
        //DragAndDropWidget
        //PawnPsychicRitualRoleSelectionWidget
        //Dialog_BeginPsychicRitual
        var worker_type = stateWorker;
        if (!typeof(BaseStateWorker).IsAssignableFrom(worker_type))
            yield return $"{nameof(stateWorker)} '{stateWorker.GetType()}' for '{this}' is not assignable from {typeof(BaseStateWorker)}";

        if (incompatibleStates.Contains(this))
            yield return $"{GetType()} '{this}' has itself in its {nameof(incompatibleStates)}";
        yield break;
    }
}
