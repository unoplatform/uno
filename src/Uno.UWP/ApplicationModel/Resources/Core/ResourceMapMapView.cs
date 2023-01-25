using System.Collections;
using System.Collections.Generic;

namespace Windows.ApplicationModel.Resources.Core;

/// <summary>
/// An unchangeable view into a map of ResourceMap objects.
/// </summary>
public partial class ResourceMapMapView : IReadOnlyDictionary<string, ResourceMap>, IEnumerable<KeyValuePair<string, ResourceMap>>
{
	private readonly IReadOnlyDictionary<string, ResourceMap> _resourceMapMap;

	internal ResourceMapMapView(IReadOnlyDictionary<string, ResourceMap> resourceMapMap)
	{
		_resourceMapMap = resourceMapMap;
	}

	public ResourceMap this[string key] => _resourceMapMap[key];

	public int Count => _resourceMapMap.Count;

	public uint Size => (uint)Count;

	public bool ContainsKey(string key) => _resourceMapMap.ContainsKey(key);

	public bool TryGetValue(string key, out ResourceMap value) => _resourceMapMap.TryGetValue(key, out value);

	public IEnumerable<string> Keys => _resourceMapMap.Keys;

	public IEnumerable<ResourceMap> Values => _resourceMapMap.Values;

	public IEnumerator<KeyValuePair<string, ResourceMap>> GetEnumerator() => _resourceMapMap.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
