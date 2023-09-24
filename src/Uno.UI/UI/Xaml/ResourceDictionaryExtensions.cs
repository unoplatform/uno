#nullable enable

using System.ComponentModel;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml;

/// <summary>
/// Helper for XAML code generation. Not intended to be used in apps outside of XAML generator.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResourceDictionaryExtensions
{
	public static ResourceDictionary AddMergedDictionaries(this ResourceDictionary dictionary, params ResourceDictionary[] mergedDictionaries)
	{
		foreach (var mergedDictionary in mergedDictionaries)
		{
			dictionary.MergedDictionaries.Add(mergedDictionary);
		}

		return dictionary;
	}
}
