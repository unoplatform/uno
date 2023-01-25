using System.Collections;
using System.Collections.Generic;

namespace Windows.ApplicationModel.Resources.Core;

/// <summary>
/// An unchangeable view into a vector of ResourceQualifier objects.
/// </summary>
public partial class ResourceQualifierVectorView : IReadOnlyList<ResourceQualifier>, IEnumerable<ResourceQualifier>
{
	private readonly IReadOnlyList<ResourceQualifier> _qualifiers;

	internal ResourceQualifierVectorView(IReadOnlyList<ResourceQualifier> qualifiers)
	{
		_qualifiers = qualifiers;
	}

	public ResourceQualifier this[int index] => _qualifiers[index];

	public int Count => _qualifiers.Count;

	public uint Size => (uint)Count;

	public IEnumerator<ResourceQualifier> GetEnumerator() => _qualifiers.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
