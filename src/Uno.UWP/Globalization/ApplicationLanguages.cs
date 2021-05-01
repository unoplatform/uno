using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
#pragma warning disable CS0618 // Type or member is obsolete
				ContextHelper.Current.Resources.Configuration.Locale.ToLanguageTag(),
#pragma warning restore CS0618 // Type or member is obsolete
#endif
				CultureInfo.InstalledUICulture.Name,
				CultureInfo.CurrentUICulture.Name,
				CultureInfo.CurrentCulture.Name
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
			var primaryCulture = CreateCulture(primaryLanguage);

			CultureInfo.CurrentCulture = primaryCulture;
			CultureInfo.CurrentUICulture = primaryCulture;
		}

		private static Regex _cultureFormatRegex;

		private static CultureInfo CreateCulture(string cultureId)
		{
			try
			{
				return new CultureInfo(cultureId);
			}
			catch (CultureNotFoundException)
			{
				_cultureFormatRegex ??= new Regex(
					@"(?<lang>[a-z]{2,8})(?:(?:\-(?<script>[a-zA-Z]+))?\-(?<reg>[A-Z]+))?",
					RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

				var match = _cultureFormatRegex.Match(cultureId);
				try
				{
					// If the script subtag is specified, we'll try to just remove it.
					// Mono is not supporting it.
					if (match.Groups["script"].Success && match.Groups["reg"].Success)
					{
						cultureId = $"{match.Groups["lang"].Value}-{match.Groups["reg"].Value}";
						return new CultureInfo(cultureId);
					}
				}
				catch (CultureNotFoundException)
				{
				}

				// If the runtime is not able to match the language + region, we'll fallback to just the language.
				if (match.Groups["lang"].Success)
				{
					return new CultureInfo(match.Groups["lang"].Value);
				}

				// It seems not possible to resolve this culture.
				throw;
			}
		}
	}
}
