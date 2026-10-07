using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Windows.Globalization
{
	public partial class Language
	{
		public Language(string languageTag)
		{
			var cultureInfo = new CultureInfo(languageTag, false);

			LanguageTag = languageTag;
			DisplayName = cultureInfo.DisplayName;
			NativeName = cultureInfo.NativeName;
		}

		public string DisplayName { get; private set; }

		public string LanguageTag { get; private set; }

		public string NativeName { get; private set; }

		/// <summary>
		/// Determines whether a BCP-47 language tag is well-formed (RFC 5646 syntax), without checking that its subtags are registered.
		/// </summary>
		public static bool IsWellFormed(string languageTag) =>
			!string.IsNullOrEmpty(languageTag)
			&& (_irregularGrandfatheredTags.Contains(languageTag) || WellFormedLanguageTagRegex().IsMatch(languageTag));

		// RFC 5646 section 2.2.8. The regular grandfathered tags already match the langtag production.
		private static readonly HashSet<string> _irregularGrandfatheredTags = new(StringComparer.OrdinalIgnoreCase)
		{
			"en-GB-oed", "i-ami", "i-bnn", "i-default", "i-enochian", "i-hak", "i-klingon", "i-lux", "i-mingo",
			"i-navajo", "i-pwn", "i-tao", "i-tay", "i-tsu", "sgn-BE-FR", "sgn-BE-NL", "sgn-CH-DE",
		};

		// RFC 5646 section 2.1: langtag / privateuse.
		// Explicit letter ranges instead of IgnoreCase, which would also accept U+212A KELVIN SIGN for "k".
		[GeneratedRegex(
			@"\A(?:" +
				@"(?:[a-zA-Z]{2,3}(?:-[a-zA-Z]{3}){0,3}|[a-zA-Z]{4}|[a-zA-Z]{5,8})" +
				@"(?:-[a-zA-Z]{4})?" +
				@"(?:-(?:[a-zA-Z]{2}|[0-9]{3}))?" +
				@"(?:-(?:[a-zA-Z0-9]{5,8}|[0-9][a-zA-Z0-9]{3}))*" +
				@"(?:-[0-9a-wyzA-WYZ](?:-[a-zA-Z0-9]{2,8})+)*" +
				@"(?:-[xX](?:-[a-zA-Z0-9]{1,8})+)?" +
				@"|[xX](?:-[a-zA-Z0-9]{1,8})+" +
			@")\z",
			RegexOptions.CultureInvariant)]
		private static partial Regex WellFormedLanguageTagRegex();
	}
}
