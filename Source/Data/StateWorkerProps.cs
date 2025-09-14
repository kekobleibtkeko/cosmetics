using System;
using Cosmetics.Mod;
using Cosmetics.Util;
using TS_Lib.Util;
using Verse;

namespace Cosmetics.Data;

public interface IStateWorkerProps
{
	bool HasSettings { get; }
	void DrawExtraOptions(CosmeticsSettings settings, Listing_Standard listing, StateDef def);
	void ExposeSettingsData(StateDef def);
}

public abstract class BaseStateWorkerProps : IStateWorkerProps
{
	public virtual bool HasSettings => false;
	public abstract Type WorkerType { get; }

	public virtual void ExposeSettingsData(StateDef def)
	{

	}

	public virtual void DrawExtraOptions(CosmeticsSettings settings, Listing_Standard listing, StateDef def)
	{

	}
}