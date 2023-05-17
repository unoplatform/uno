using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.ApplicationModel;
using Windows.Storage;
using Uno;
using Uno.Foundation.Logging;
using Uno.UI;

namespace Windows.Globalization
{
	public static partial class ApplicationLanguages
	{
		private static string _primaryLanguageOverride = string.Empty;

#if !NET461
		private const string PrimaryLanguageOverrideSettingKey = "__Uno.PrimaryLanguageOverride";
#endif

		static ApplicationLanguages()
		{
#if !NET461
			var appSpecificKey = GetAppSpecificSettingKey();
			var hasLegacySetting = TryGetSettingFromKey(PrimaryLanguageOverrideSettingKey, out var legacySavedValue);
			var hasAppSpecificSetting = TryGetSettingFromKey(appSpecificKey, out var appSpecificSavedValue);

			// We have four cases:
			// 1. hasAppSpecificSetting && hasLegacySetting
			// 2. hasAppSpecificSetting && !hasLegacySetting
			// 3. !hasAppSpecificSetting && hasLegacySetting
			// 4. !hasAppSpecificSetting && !hasLegacySetting

			// For 1 and 2, we will ignore the legacy setting and use the app specific setting.
			// For 3, we will copy the setting from legacy to app specific
			// For 4, we will do nothing.

			// History:
			// When persistent was implemented, it relied on saving the PrimaryLanguageOverride value to
			// "__Uno.PrimaryLanguageOverride" in LocalSettings of ApplicationData.
			// After that, we found that in Skia, the LocalSettings of ApplicationData is shared across all applications (which is wrong).
			// The fix for having app-specific local settings on Skia is being done for Uno 5 (see https://github.com/unoplatform/uno/pull/12314)
			// However, since the above is breaking change, we won't take it for Uno 4.x, but we still want PrimaryLanguageOverride to behave properly in Uno 4.x.
			// So, we modify the key to have an app-specific part.
			// We try to copy the value we read from the "legacy" key to the app-specific key.
			// In a future major release, we may consider removing the legacy setting key.

			// Note: There is a Wasm concern that is mentioned in https://github.com/unoplatform/uno/issues/12320
			// So, we decided to implement this for all platforms.

			if (hasAppSpecificSetting)
			{
				_primaryLanguageOverride = appSpecificSavedValue;
			}
			else if (hasLegacySetting)
			{
				ApplicationData.Current.LocalSettings.Values[appSpecificKey] = legacySavedValue;
				_primaryLanguageOverride = legacySavedValue;
			}
#endif

			ApplyLanguages();
		}

		private static bool TryGetSettingFromKey(string key, out string value)
		{
			if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var savedValue))
			{
				value = savedValue as string;
				return value is not null;
			}

			value = null;
			return false;
		}

		private static string GetAppSpecificSettingKey()
		{
			if (Package.EntryAssembly is not { } entryAssembly)
			{
				throw new InvalidOperationException("ApplicationLanguages is being accessed too early before an instance of Application was created.");
			}

			return $"PrimaryLanguageOverrideSettingKey.{entryAssembly.GetName().Name}";
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

#if !NET461
				ApplicationData.Current.LocalSettings.Values[GetAppSpecificSettingKey()] = _primaryLanguageOverride;
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
