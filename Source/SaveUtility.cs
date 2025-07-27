using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Transmogged;
#nullable enable

public interface IAccurateXMLConverter<T>
{
	string GetAccurateXMLString(T value);
	T GetObjectFromXML(string xmlstring);
}

public static class TransmoggedSaveUtility
{
	public static readonly IDictionary<Type, Func<object, string>> ObjectToXMLConverters = new Dictionary<Type, Func<object, string>>();
	public static readonly IDictionary<Type, Func<string, object>> XMLToObjectConverters = new Dictionary<Type, Func<string, object>>();

	public static void LookAccurate<T>(ref T? value, string name, T? defval = default)
	{
		switch (Scribe.mode)
		{
			case LoadSaveMode.Saving:
				{
					if (value is null
						|| Equals(value, default)
						|| (!Equals(defval, default) && Equals(value, defval)))
						return;

					if (!ObjectToXMLConverters.TryGetValue(typeof(T), out var f))
						throw new ArgumentException($"Invalid type '{typeof(T)}' given to LookAccurate", nameof(value));
					
					Scribe.saver.WriteElement(name, f(value));
				}
				break;
			case LoadSaveMode.LoadingVars:
				{
					if (!XMLToObjectConverters.TryGetValue(typeof(T), out var f))
						throw new ArgumentException($"Invalid type '{typeof(T)}' given to LookAccurate", nameof(value));

					var node = Scribe.loader.curXmlParent[name];
					value = node is null
						? defval ?? default
						: (T)f(node.InnerText);
				}
				break;
		}
	}

	public static void LookDict<K, V>(ref Dictionary<K, V> dict, string name)
	{
		if (Scribe.mode == LoadSaveMode.Saving)
			dict ??= [];
		Scribe_Collections.Look(ref dict, name);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
			dict ??= [];
	}

	public static void AddConverters<T>(Func<T, string> toxml, Func<string, T> fromxml)
	{
		Debug.LogError($"added converter for type {typeof(T)}");
		var type = typeof(T);
		ObjectToXMLConverters[type] = obj => toxml((T)obj);
		XMLToObjectConverters[type] = xml => fromxml(xml)!;
	}

	public static void Register<T>(this IAccurateXMLConverter<T> conv) => AddConverters<T>(conv.GetAccurateXMLString, conv.GetObjectFromXML);
}

public struct Vector3XmlConverter : IAccurateXMLConverter<Vector3>
{
	public string GetAccurateXMLString(Vector3 value)
	{
		return $"{value.x:0.00000},{value.y:0.00000},{value.z:0.00000}";
	}

	public Vector3 GetObjectFromXML(string xmlstring)
	{
		if (xmlstring is null)
		{
			return default;
		}
		Vector3 res = default;
		var strs = xmlstring.Split(new[]{'(',')',',','|'}, 3, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < strs.Length; i++)
		{
			if (float.TryParse(strs[i] ?? string.Empty, out var fres))
			{
				res[i] = fres;
			}
			else
			{
				Debug.LogError($"unable to parse string to element of vector 3 at i='{i}', string to parse: '{strs[i]}'");
			}
		}
		return res;
	}
}

public struct Vector2XmlConverter : IAccurateXMLConverter<Vector2>
{
	public string GetAccurateXMLString(Vector2 value)
	{
		return $"{value.x:0.00000},{value.y:0.00000}";
	}

	public Vector2 GetObjectFromXML(string xmlstring)
	{
		if (xmlstring is null)
		{
			return default;
		}
		Vector2 res = default;
		var strs = xmlstring.Split(new[]{'(',')',',','|'}, 2, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < strs.Length; i++)
		{
			if (float.TryParse(strs[i] ?? string.Empty, out var fres))
			{
				res[i] = fres;
			}
			else
			{
				Debug.LogError($"unable to parse string to element of vector 2 at i='{i}', string to parse: '{strs[i]}'");
			}
		}
		return res;
	}
}


public static class ConverterRegistrator
{
	public static void Register()
	{
		new Vector3XmlConverter().Register();
		new Vector2XmlConverter().Register();
	}
}