using System.Collections;
using System.Collections.Generic;

namespace Windows.ApplicationModel.Resources.Core;

/// <summary>
/// Represents a collection of ResourceContext language qualifiers.
/// </summary>
public partial class ResourceContextLanguagesVectorView : IReadOnlyList<string>, IEnumerable<string>
{
	private readonly IReadOnlyList<string> _languages;

	internal ResourceContextLanguagesVectorView(IReadOnlyList<string> languages)
	{
		_languages = languages;
	}

	public string this[int index] => _languages[index];

	public int Count => _languages.Count;

	public uint Size => (uint)Count;

	public IEnumerator<string> GetEnumerator() => _languages.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
