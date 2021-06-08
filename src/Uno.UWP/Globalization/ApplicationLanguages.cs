using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Uno.UI;

namespace Windows.Globalization
{
	public static partial class ApplicationLanguages
	{
		private static string _primaryLanguageOverride;

		static ApplicationLanguages()
		{
			ApplyLanguages();
		}

		public static string PrimaryLanguageOverride
		{
			get => _primaryLanguageOverride;
			set
			{
				_primaryLanguageOverride = value;
				ApplyLanguages();
			}
		}

		public static global::System.Collections.Generic.IReadOnlyList<string> Languages
		{
			get;
			private set;
		}

#if !(__IOS__)
		public static global::System.Collections.Generic.IReadOnlyList<string> ManifestLanguages
		{
			get;
		} = GetManifestLanguages();


		private static string[] GetManifestLanguages()
		{
			var languages = new[]
			{
#if __ANDROID__
				ContextHelper.Current?.Resources?.Configuration?.Locales?.Get(0)?.ToLanguageTag(),
#endif
				CultureInfo.InstalledUICulture?.Name,
				CultureInfo.CurrentUICulture?.Name,
				CultureInfo.CurrentCulture?.Name
			};

			return languages
				.Where(l => !string.IsNullOrWhiteSpace(l))
				.Distinct()
				.ToArray();
		}
#endif

		private static void ApplyLanguages()
		{
			var overridenLanguage = PrimaryLanguageOverride;

			if (string.IsNullOrWhiteSpace(overridenLanguage))
			{
				Languages = ManifestLanguages;
			}
			else
			{
				var manifestLanguages = ManifestLanguages.ToArray();
				var languages = new string[ManifestLanguages.Count + 1];
				languages[0] = overridenLanguage;
				if (manifestLanguages.Length > 0)
				{
					Array.Copy(manifestLanguages, 0, languages, 1, manifestLanguages.Length);
				}

				Languages = languages.Distinct().ToArray();
			}

			var primaryLanguage = Languages.First();
			var primaryCulture = new CultureInfo(primaryLanguage);

			CultureInfo.CurrentCulture = primaryCulture;
			CultureInfo.CurrentUICulture = primaryCulture;
		}
	}
}
