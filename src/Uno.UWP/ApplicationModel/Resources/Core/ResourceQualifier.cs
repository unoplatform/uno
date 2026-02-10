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

		// Fallback for language tags with region/script subtags (e.g., quz-PE,
		// ca-Es-VALENCIA) that may not be enumerated by GetCultures() on all
		// platforms. Only attempt this for strings containing a dash, to avoid
		// false positives on file extensions like "png" (ISO 639-3 Pangwa).
		if (str.IndexOf('-') >= 0)
		{
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

		return false;
	}

	#endregion
}
