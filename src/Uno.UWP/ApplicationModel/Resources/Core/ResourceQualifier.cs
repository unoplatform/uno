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
	private static readonly object _languageTagsLock = new();
	private static HashSet<string> LanguageTags
	{
		get
		{
			lock (_languageTagsLock)
			{
				if (_languageTags == null)
				{
					var cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
					var ietfLanguageTags = cultures.Select(c => c.IetfLanguageTag);
					var ietfLanguageParentTags = cultures.Select(c => c.Parent.IetfLanguageTag);
					var twoLetterLanguageTags = cultures.Select(c => c.TwoLetterISOLanguageName);

					var allCulture = Enumerable.Concat(
						ietfLanguageTags,
						twoLetterLanguageTags.Concat(ietfLanguageParentTags));

					_languageTags = new HashSet<string>(allCulture.Distinct(), StringComparer.InvariantCultureIgnoreCase);
				}

				return _languageTags;
			}
		}
	}

	private static bool IsLanguageTag(string str)
	{
		if (LanguageTags.Contains(str))
		{
			return true;
		}

		// Fallback: CultureInfo.GetCultures may not enumerate all valid cultures
		// on all platforms (e.g., macOS/Linux ICU may use different IETF tags like
		// zh-Hans-CN instead of zh-CN, or may omit cultures like kok-IN, quz-PE).
		// Try to create the culture directly as a fallback.
		try
		{
			_ = CultureInfo.GetCultureInfo(str);

			lock (_languageTagsLock)
			{
				LanguageTags.Add(str);
			}

			return true;
		}
		catch (CultureNotFoundException)
		{
			return false;
		}
	}

	#endregion
}
