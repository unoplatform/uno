using System.Collections;
using System.Collections.Generic;

namespace Windows.ApplicationModel.Resources.Core;

/// <summary>
/// An unchangeable view into a map of ResourceQualifier objects.
/// </summary>
public partial class ResourceQualifierMapView : IReadOnlyDictionary<string, string>, IEnumerable<KeyValuePair<string, string>>
{
	private readonly IReadOnlyDictionary<string, string> _qualifierMap;

	internal ResourceQualifierMapView(IReadOnlyDictionary<string, string> qualifierMap)
	{
		_qualifierMap = qualifierMap;
	}

	public string this[string key] => _qualifierMap[key];

	public int Count => _qualifierMap.Count;

	public uint Size => (uint)Count;

	public bool ContainsKey(string key) => _qualifierMap.ContainsKey(key);

	public bool TryGetValue(string key, out string value) => _qualifierMap.TryGetValue(key, out value);

	public IEnumerable<string> Keys => _qualifierMap.Keys;

	public IEnumerable<string> Values => _qualifierMap.Values;

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _qualifierMap.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
