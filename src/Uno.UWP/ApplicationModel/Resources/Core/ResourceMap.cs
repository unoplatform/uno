using System;
using System.Collections;
using System.Collections.Generic;

namespace Windows.ApplicationModel.Resources.Core;

public partial class ResourceMap : IReadOnlyDictionary<string, NamedResource>, IEnumerable<KeyValuePair<string, NamedResource>>
{
	private readonly Dictionary<string, NamedResource> _namedResources;

	internal ResourceMap(Dictionary<string, NamedResource> namedResources)
	{
		_namedResources = namedResources;
	}

	public Uri Uri
	{
		get
		{
			throw new global::System.NotImplementedException("The member Uri ResourceMap.Uri is not implemented in Uno.");
		}
	}

	public int Count => _namedResources;

	public uint Size => (uint)Count;

	public  ResourceCandidate GetValue( string resource)
	{
		throw new global::System.NotImplementedException("The member ResourceCandidate ResourceMap.GetValue(string resource) is not implemented in Uno.");
	}

	public  ResourceCandidate GetValue( string resource,  ResourceContext context)
	{
		throw new global::System.NotImplementedException("The member ResourceCandidate ResourceMap.GetValue(string resource, ResourceContext context) is not implemented in Uno.");
	}

	public  ResourceMap GetSubtree( string reference)
	{
		throw new global::System.NotImplementedException("The member ResourceMap ResourceMap.GetSubtree(string reference) is not implemented in Uno.");
	}

	public bool ContainsKey(string key) => _namedResources.ContainsKey(key);

	public bool TryGetValue(string key, out NamedResource value) => _namedResources.TryGetValue(key, out value);

	public NamedResource this[string key] => _namedResources[key];

	public IEnumerable<string> Keys => _namedResources.Keys;

	public IEnumerable<NamedResource> Values => _namedResources.Values;

	public IEnumerator<KeyValuePair<string, NamedResource>> GetEnumerator() => _namedResources.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
