#nullable enable

using System;
using System.Globalization;
using Uno;

namespace Uno.Globalization.NumberFormatting;

/// <summary>
/// Shared geographic-region validation/resolution and culture-aware <see cref="NumberFormatInfo"/>
/// resolution logic for the locale-aware constructors of <see cref="Windows.Globalization.NumberFormatting.DecimalFormatter"/>
/// and (in future ports) sibling formatters such as CurrencyFormatter that accept the same
/// <c>(languages, geographicRegion)</c> shape.
/// </summary>
internal static class GeographicRegionHelper
{
	/// <summary>
	/// Validates <paramref name="geographicRegion"/>, throwing the same exceptions real WinRT throws
	/// for a null/empty/unrecognized region (mirrors <see cref="NumeralSystemTranslator"/>'s validation
	/// conventions for <c>languages</c>).
	/// </summary>
	public static void ValidateGeographicRegion(string geographicRegion)
	{
		if (geographicRegion is null ||
			geographicRegion.Length != 2 ||
			geographicRegion[0] is < 'A' or > 'Z' ||
			geographicRegion[1] is < 'A' or > 'Z' ||
			!TryResolveRegion(geographicRegion, out _))
		{
			ExceptionHelper.ThrowArgumentException(nameof(geographicRegion));
		}
	}

	/// <summary>
	/// Resolves <paramref name="geographicRegion"/> to its canonical two-letter ISO 3166 form.
	/// </summary>
	/// <remarks>
	/// Expected to be called only after <see cref="ValidateGeographicRegion"/> already succeeded.
	/// </remarks>
	public static string ResolveGeographicRegion(string geographicRegion) =>
		TryResolveRegion(geographicRegion, out var regionInfo) ? regionInfo!.TwoLetterISORegionName : geographicRegion;

	private static bool TryResolveRegion(string geographicRegion, out RegionInfo? regionInfo)
	{
		try
		{
			regionInfo = new RegionInfo(geographicRegion);
			return true;
		}
		catch (ArgumentException)
		{
			regionInfo = null;
			return false;
		}
	}

	/// <summary>
	/// Resolves the format used for punctuation, grouping, and signs.
	/// </summary>
	/// <remarks>
	/// Arab and ArabExt use invariant punctuation because the translator localizes separators.
	/// Other numeral systems use the resolved locale. InvariantGlobalization may silently return
	/// invariant-equivalent data for unknown cultures.
	/// </remarks>
	public static NumberFormatInfo ResolveNumberFormat(string resolvedLanguage, string resolvedGeographicRegion, string numeralSystem)
	{
		if (IsArabicNumeralSystem(numeralSystem))
		{
			return CultureInfo.InvariantCulture.NumberFormat;
		}

		return TryResolveCulture(resolvedLanguage, resolvedGeographicRegion)?.NumberFormat ?? CultureInfo.InvariantCulture.NumberFormat;
	}

	private static bool IsArabicNumeralSystem(string numeralSystem) =>
		numeralSystem.Equals("Arab", StringComparison.Ordinal) ||
		numeralSystem.Equals("ArabExt", StringComparison.Ordinal);

	// Prefer combining the primary language with the caller-supplied region (e.g. "fr"+"CA" => "fr-CA") so an
	// explicit geographicRegion - the whole point of accepting one - can influence punctuation even when the
	// language tag already carries its own region, then fall back to the language's own resolved (default)
	// region, then to the bare language.
	private static CultureInfo? TryResolveCulture(string resolvedLanguage, string resolvedGeographicRegion)
	{
		var primaryLanguageSubtag = GetPrimaryLanguageSubtag(resolvedLanguage);

		if (TryGetCultureInfo($"{primaryLanguageSubtag}-{resolvedGeographicRegion}", out var culture))
		{
			return culture;
		}

		if (TryGetCultureInfo(resolvedLanguage, out culture))
		{
			return culture;
		}

		if (TryGetCultureInfo(primaryLanguageSubtag, out culture))
		{
			return culture;
		}

		return null;
	}

	private static string GetPrimaryLanguageSubtag(string languageTag)
	{
		var dashIndex = languageTag.IndexOf('-');
		return dashIndex < 0 ? languageTag : languageTag.Substring(0, dashIndex);
	}

	private static bool TryGetCultureInfo(string name, out CultureInfo? culture)
	{
		try
		{
			culture = CultureInfo.GetCultureInfo(name);
			return true;
		}
		catch (CultureNotFoundException)
		{
			culture = null;
			return false;
		}
	}
}
