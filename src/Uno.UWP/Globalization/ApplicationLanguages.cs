using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Storage;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno;
using System.Diagnostics.CodeAnalysis;

namespace Windows.Globalization
{
	public static partial class ApplicationLanguages
	{
		private static string _primaryLanguageOverride = string.Empty;

#if !IS_UNIT_TESTS
		private const string PrimaryLanguageOverrideSettingKey = "__Uno.PrimaryLanguageOverride";
#endif

		static ApplicationLanguages()
		{
#if !IS_UNIT_TESTS
			if (ApplicationData.Current.LocalSettings.Values.TryGetValue(PrimaryLanguageOverrideSettingKey, out var savedValue)
				&& savedValue is string stringSavedValue)
			{
				_primaryLanguageOverride = stringSavedValue;
			}
#endif

			ApplyLanguages();
		}

		internal static void ApplyCulture()
		{
			var primaryLanguageOverride = PrimaryLanguageOverride;
			if (primaryLanguageOverride.Length > 0)
			{
				if (typeof(ApplicationLanguages).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(ApplicationLanguages).Log().Debug($"Using {primaryLanguageOverride} (from PrimaryLanguageOverride) as primary language");
				}

				setCulture(primaryLanguageOverride);
			}
			else if (Languages.Count > 0)
			{
				var language = Languages[0];
				if (typeof(ApplicationLanguages).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(ApplicationLanguages).Log().Debug($"Using {language} (from Languages) as primary language");
				}

				setCulture(language);
			}
			else
			{
				if (typeof(ApplicationLanguages).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(ApplicationLanguages).Log().Warn($"Unable to determine the default culture, using invariant culture");
				}
			}

			static void setCulture(string cultureId)
			{
				var culture = CreateCulture(cultureId);
				CultureInfo.CurrentCulture = culture;
				CultureInfo.DefaultThreadCurrentCulture = culture;
				CultureInfo.CurrentUICulture = culture;
				CultureInfo.DefaultThreadCurrentUICulture = culture;
			}
		}

		public static string PrimaryLanguageOverride
		{
			get
			{
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
				if (WinRTFeatureConfiguration.ApplicationLanguages.UseLegacyPrimaryLanguageOverride)
				{
					ApplyCulture();
				}

#if !IS_UNIT_TESTS
				ApplicationData.Current.LocalSettings.Values[PrimaryLanguageOverrideSettingKey] = _primaryLanguageOverride;
#endif
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
				.ToArray()!;
		}
#endif

		[MemberNotNull(nameof(Languages))]
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

		private static Regex? _cultureFormatRegex;

		private static CultureInfo CreateCulture(string cultureId)
		{
			try
			{
				return new CultureInfo(cultureId);
			}
			catch (CultureNotFoundException)
			{
				_cultureFormatRegex ??= CultureRegex();

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

		[GeneratedRegex(@"(?<lang>[a-z]{2,8})(?:(?:\-(?<script>[a-zA-Z]+))?\-(?<reg>[A-Z]+))?", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		private static partial Regex CultureRegex();
	}
}
