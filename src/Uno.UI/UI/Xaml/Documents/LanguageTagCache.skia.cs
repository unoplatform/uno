#nullable enable

using System;
using System.Collections.Generic;
using Windows.Globalization;

namespace Windows.UI.Xaml.Documents;

internal static class LanguageTagCache
{
	[ThreadStatic] private static readonly Dictionary<string, string[]> _languageTagsCache = new();

	public static string[] GetLanguageTag(TextElement element)
	{
		var key = element.Language
			?? ApplicationLanguages.PrimaryLanguageOverride
			?? string.Empty;
		if (!_languageTagsCache.TryGetValue(key, out var tag))
		{
			var cultureInfo = key.Length == 0
				? global::System.Globalization.CultureInfo.CurrentUICulture
				: global::System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag(key);
			tag = new[] { cultureInfo.TwoLetterISOLanguageName };
			_languageTagsCache[key] = tag;
		}
		return tag;
	}
}
