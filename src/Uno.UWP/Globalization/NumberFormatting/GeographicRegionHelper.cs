#nullable enable

using System;
using System.Collections.Concurrent;
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
	private static readonly ConcurrentDictionary<string, NumberFormatInfo> _signNormalizedNumberFormats = new(StringComparer.Ordinal);

	// Native WinRT uses the Windows NLS geographic data exposed by GetGeoInfoEx/EnumSystemGeoNames.
	// Keep this complete, sorted list aligned with that data; WinRT also accepts 900-999.
	private static readonly int[] _supportedNumericM49Regions =
	[
		0, 1, 2, 4, 5, 8, 9, 10, 11, 12, 13, 14,
		15, 16, 17, 18, 19, 20, 21, 24, 28, 29, 30, 31,
		32, 34, 35, 36, 39, 40, 44, 48, 50, 51, 52, 53,
		54, 56, 57, 60, 61, 64, 68, 70, 72, 74, 76, 84,
		86, 90, 92, 96, 100, 104, 108, 112, 116, 120, 124, 132,
		136, 140, 142, 143, 144, 145, 148, 150, 151, 152, 154, 155,
		156, 158, 162, 166, 170, 174, 175, 178, 180, 184, 188, 191,
		192, 196, 203, 204, 208, 212, 214, 218, 222, 226, 231, 232,
		233, 234, 238, 239, 242, 246, 248, 250, 254, 258, 260, 262,
		266, 268, 270, 275, 276, 288, 292, 296, 300, 304, 308, 312,
		316, 320, 324, 328, 332, 334, 336, 340, 344, 348, 352, 356,
		360, 364, 368, 372, 376, 380, 384, 388, 392, 398, 400, 404,
		408, 410, 414, 417, 418, 419, 422, 426, 428, 430, 434, 438,
		440, 442, 446, 450, 454, 458, 462, 466, 470, 474, 478, 480,
		484, 492, 496, 498, 499, 500, 504, 508, 512, 516, 520, 524,
		528, 530, 531, 533, 534, 535, 540, 548, 554, 558, 562, 566,
		570, 574, 578, 580, 581, 583, 584, 585, 586, 591, 598, 600,
		604, 608, 612, 616, 620, 624, 626, 630, 634, 638, 642, 643,
		646, 652, 654, 659, 660, 662, 663, 666, 670, 674, 678, 682,
		686, 688, 690, 694, 702, 703, 704, 705, 706, 710, 716, 724,
		728, 736, 740, 744, 748, 752, 756, 760, 762, 764, 768, 772,
		776, 780, 784, 788, 792, 795, 796, 798, 800, 804, 807, 818,
		826, 830, 831, 832, 833, 834, 840, 850, 854, 858, 860, 862,
		876, 882, 887, 894,
	];

	// ISO 3166-1 codes that the same NLS data reports as assigned, excluding the user-assigned
	// ranges recognized by IsUserAssignedRegion. Sorted, fixed-width records.
	private const string _assignedAlpha2Regions =
		"ADAEAFAGAIALAMANAOAQARASATAUAWAXAZBABBBDBEBFBGBHBIBJ" +
		"BLBMBNBOBQBRBSBTBVBWBYBZCACCCDCFCGCHCICKCLCMCNCOCRCU" +
		"CVCWCXCYCZDEDJDKDMDODZECEEEGERESETFIFJFKFMFOFRGAGBGD" +
		"GEGFGGGHGIGLGMGNGPGQGRGSGTGUGWGYHKHMHNHRHTHUIDIEILIM" +
		"INIOIQIRISITJEJMJOJPKEKGKHKIKMKNKPKRKWKYKZLALBLCLILK" +
		"LRLSLTLULVLYMAMCMDMEMFMGMHMKMLMMMNMOMPMQMRMSMTMUMVMW" +
		"MXMYMZNANCNENFNGNINLNONPNRNUNZOMOOPAPEPFPGPHPKPLPMPN" +
		"PRPSPTPWPYQARERORSRURWSASBSCSDSESGSHSISJSKSLSMSNSOSR" +
		"SSSTSVSXSYSZTCTDTFTGTHTJTKTLTMTNTOTRTTTVTWTZUAUGUMUS" +
		"UYUZVAVCVEVGVIVNVUWFWSYEYTZAZMZW";

	private const string _assignedAlpha3Regions =
		"ABWAFGAGOAIAALAALBANDANTAREARGARMASMATAATFATGAUSAUTAZEBDIBEL" +
		"BENBESBFABGDBGRBHRBHSBIHBLMBLRBLZBMUBOLBRABRBBRNBTNBVTBWACAF" +
		"CANCCKCHECHLCHNCIVCMRCODCOGCOKCOLCOMCPVCRICUBCUWCXRCYMCYPCZE" +
		"DEUDJIDMADNKDOMDZAECUEGYERIESPESTETHFINFJIFLKFRAFROFSMGABGBR" +
		"GEOGGYGHAGIBGINGLPGMBGNBGNQGRCGRDGRLGTMGUFGUMGUYHKGHMDHNDHRV" +
		"HTIHUNIDNIMNINDIOTIRLIRNIRQISLISRITAJAMJEYJORJPNKAZKENKGZKHM" +
		"KIRKNAKORKWTLAOLBNLBRLBYLCALIELKALSOLTULUXLVAMACMAFMARMCOMDA" +
		"MDGMDVMEXMHLMKDMLIMLTMMRMNEMNGMNPMOZMRTMSRMTQMUSMWIMYSMYTNAM" +
		"NCLNERNFKNGANICNIUNLDNORNPLNRUNZLOMNOOOPAKPANPCNPERPHLPLWPNG" +
		"POLPRIPRKPRTPRYPSEPYFQATREUROURUSRWASAUSDNSENSGPSGSSHNSJMSLB" +
		"SLESLVSMRSOMSPMSRBSSDSTPSURSVKSVNSWESWZSXMSYCSYRTCATCDTGOTHA" +
		"TJKTKLTKMTLSTONTTOTUNTURTUVTWNTZAUGAUKRUMIURYUSAUZBVATVCTVEN" +
		"VGBVIRVNMVUTWLFWSMYEMZAFZMBZWE";

	/// <summary>
	/// Validates <paramref name="geographicRegion"/>, throwing the same exceptions real WinRT throws
	/// for a null/empty/unrecognized region (mirrors <see cref="NumeralSystemTranslator"/>'s validation
	/// conventions for <c>languages</c>).
	/// </summary>
	public static void ValidateGeographicRegion(string? geographicRegion)
	{
		if (geographicRegion is null ||
			!IsSupportedRegion(geographicRegion))
		{
			ExceptionHelper.ThrowArgumentException(nameof(geographicRegion));
		}
	}

	private static bool IsSupportedRegion(string region) => region.Length switch
	{
		2 => IsUppercaseAlpha(region) &&
			(IsUserAssignedRegion(region) || ContainsRegion(_assignedAlpha2Regions, region)),
		3 => IsNumericM49(region) ||
			IsUppercaseAlpha(region) &&
			(IsUserAssignedRegion(region) || ContainsRegion(_assignedAlpha3Regions, region)),
		_ => false,
	};

	private static bool IsUppercaseAlpha(string region)
	{
		for (var i = 0; i < region.Length; i++)
		{
			if (region[i] is < 'A' or > 'Z')
			{
				return false;
			}
		}

		return true;
	}

	// ISO 3166-1 user-assigned ranges, which WinRT accepts in full: AA/QM-QZ/XA-XZ/ZZ for alpha-2
	// and the matching AAA-AAZ/QMA-QZZ/XAA-XZZ/ZZA-ZZZ blocks for alpha-3.
	private static bool IsUserAssignedRegion(string region) =>
		region[0] switch
		{
			'A' => region[1] == 'A',
			'Q' => region[1] >= 'M',
			'X' => true,
			'Z' => region[1] == 'Z',
			_ => false,
		};

	private static bool ContainsRegion(string table, string region)
	{
		var width = region.Length;
		var low = 0;
		var high = table.Length / width - 1;

		while (low <= high)
		{
			var middle = low + (high - low) / 2;
			var comparison = string.CompareOrdinal(table, middle * width, region, 0, width);

			if (comparison == 0)
			{
				return true;
			}

			if (comparison < 0)
			{
				low = middle + 1;
			}
			else
			{
				high = middle - 1;
			}
		}

		return false;
	}

	private static bool IsNumericM49(string region)
	{
		if (region.Length != 3 ||
			region[0] is < '0' or > '9' ||
			region[1] is < '0' or > '9' ||
			region[2] is < '0' or > '9')
		{
			return false;
		}

		var numericRegion = (region[0] - '0') * 100 + (region[1] - '0') * 10 + region[2] - '0';
		return numericRegion >= 900 ||
			Array.BinarySearch(_supportedNumericM49Regions, numericRegion) >= 0;
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

		var culture = TryResolveCulture(resolvedLanguage, resolvedGeographicRegion);
		return culture is null ? CultureInfo.InvariantCulture.NumberFormat : GetSignNormalizedNumberFormat(culture);
	}

	private static NumberFormatInfo GetSignNormalizedNumberFormat(CultureInfo culture) =>
		_signNormalizedNumberFormats.GetOrAdd(
			culture.Name,
			static (_, source) =>
			{
				// WinRT reads the signs from the Windows NLS locale data, which uses ASCII "-"/"+" for every
				// locale, whereas ICU (and therefore .NET) reports U+2212 MINUS SIGN for sv/fi/lt/et/nb and
				// bidi-marked signs for ar/fa/he. Normalizing keeps both formatting and parsing WinRT-compatible.
				var numberFormat = (NumberFormatInfo)source.NumberFormat.Clone();
				numberFormat.NegativeSign = "-";
				numberFormat.PositiveSign = "+";
				return numberFormat;
			},
			culture);

	private static bool IsArabicNumeralSystem(string numeralSystem) =>
		numeralSystem.Equals("Arab", StringComparison.Ordinal) ||
		numeralSystem.Equals("ArabExt", StringComparison.Ordinal);

	private static CultureInfo? TryResolveCulture(string resolvedLanguage, string resolvedGeographicRegion)
	{
		if (TryGetCultureInfo(resolvedLanguage, out var culture))
		{
			return culture;
		}

		var primaryLanguageSubtag = GetPrimaryLanguageSubtag(resolvedLanguage);

		if (TryGetCultureInfo($"{primaryLanguageSubtag}-{resolvedGeographicRegion}", out culture))
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
