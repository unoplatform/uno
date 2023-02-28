using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Storage;
using Uno.UI;

namespace Windows.Globalization
{
	public static partial class ApplicationLanguages
	{
		private static string _primaryLanguageOverride = string.Empty;
		private const string PrimaryLanguageOverrideSettingKey = "__Uno.PrimaryLanguageOverride";

		static ApplicationLanguages()
		{
			ApplyLanguages();
		}

		internal static void Initialize()
		{
			var primaryLanguageOverride = ApplicationLanguages.PrimaryLanguageOverride;
			if (primaryLanguageOverride.Length > 0)
			{
				var culture = CreateCulture(primaryLanguageOverride);
				CultureInfo.CurrentCulture = culture;
				CultureInfo.CurrentUICulture = culture;
			}
		}

		public static string PrimaryLanguageOverride
		{
			get
			{
				if (_primaryLanguageOverride.Length == 0 &&
					ApplicationData.Current.LocalSettings.Values.TryGetValue(PrimaryLanguageOverrideSettingKey, out var savedValue) &&
					savedValue is string stringSavedValue)
				{
					_primaryLanguageOverride = stringSavedValue;
				}

				return _primaryLanguageOverride;
			}
			set
			{
				if (value is null)
				{
					throw new ArgumentNullException(nameof(value), "Value cannot be null.");
				}

				_primaryLanguageOverride = value;
				ApplyLanguages();

				ApplicationData.Current.LocalSettings.Values[PrimaryLanguageOverrideSettingKey] = _primaryLanguageOverride;
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
