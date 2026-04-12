using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Resources.Core;

public partial class ResourceQualifier
{
	private static readonly char[] _dashArray = new[] { '-' };

	// MRT resource qualifier names (and their aliases). Used to short-circuit
	// the BCP-47 fallback in IsLanguageTag so non-language qualifier segments
	// like "contrast-high" or "scale-200" don't hit the CultureInfo constructor
	// and throw CultureNotFoundException during resource scanning.
	private static readonly HashSet<string> _knownQualifierNames =
		new(StringComparer.OrdinalIgnoreCase)
		{
			"alternateform", "altform",
			"configuration", "config",
			"contrast",
			"custom",
			"dxfeaturelevel",
			"homeregion",
			"language", "lang",
			"layoutdirection",
			"scale",
			"targetsize",
			"theme",
		};

	internal ResourceQualifier(string name, string value)
	{
		QualifierName = name;
		QualifierValue = value;
	}

	public string QualifierName { get; }

	public string QualifierValue { get; }

	internal static ResourceQualifier Parse(string str)
	{
		if (IsLanguageTag(str))
		{
			str = $"language-{str}";
		}

		if (str.Contains("-"))
		{
			var qualifierParts = str.Split(_dashArray, 2);
			var name = qualifierParts[0].ToLowerInvariant();
			var value = qualifierParts[1];

			if (name == "lang")
			{
				name = "language";
			}

			if (name == "scale" || name == "language" || name == "theme" || name == "custom")
			{
				return new ResourceQualifier(name, value);
			}
		}

		return null;
	}

	#region Language helpers

	private static HashSet<string> _languageTags;
	private static HashSet<string> LanguageTags
	{
		get
		{
			if (_languageTags == null)
			{
				var cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
				var ietfLanguageTags = cultures.Select(c => c.IetfLanguageTag);
				var ietfLanguageParentTags = cultures.Select(c => c.Parent.IetfLanguageTag);
				var twoLetterLanguageTags = cultures.Select(c => c.TwoLetterISOLanguageName);
				var cultureNames = cultures.Select(c => c.Name);
				var parentCultureNames = cultures.Select(c => c.Parent.Name);

				var allTags = ietfLanguageTags
					.Concat(ietfLanguageParentTags)
					.Concat(twoLetterLanguageTags)
					.Concat(cultureNames)
					.Concat(parentCultureNames);

				_languageTags = new HashSet<string>(allTags.Distinct(), StringComparer.InvariantCultureIgnoreCase);
			}

			return _languageTags;
		}
	}

	private static bool IsLanguageTag(string str)
	{
		if (LanguageTags.Contains(str))
		{
			return true;
		}

		var dashIndex = str.IndexOf('-');
		if (dashIndex < 0)
		{
			// Avoid false positives on file extensions like "png" (ISO 639-3 Pangwa).
			return false;
		}

		// Known qualifier prefixes (including "language" / "lang" aliases): skip
		// the CultureInfo fallback to avoid CultureNotFoundException during
		// resource/asset scanning. Parse's second block produces the language
		// qualifier from an explicitly-prefixed string like "language-ca-Es-VALENCIA",
		// so returning false here for prefixed strings is both correct and cheaper.
		if (_knownQualifierNames.Contains(str.Substring(0, dashIndex)))
		{
			return false;
		}

		// Fallback for language tags with region/script subtags (e.g., quz-PE,
		// ca-Es-VALENCIA) that may not be enumerated by GetCultures() on all
		// platforms.
		try
		{
			var culture = new CultureInfo(str);
			return !string.IsNullOrEmpty(culture.Name);
		}
		catch (CultureNotFoundException)
		{
			return false;
		}
	}

	#endregion
}
