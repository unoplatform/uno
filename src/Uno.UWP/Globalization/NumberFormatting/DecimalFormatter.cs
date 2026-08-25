#nullable enable

using System.Collections.Generic;
using Uno;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting
{
	public partial class DecimalFormatter : INumberFormatterOptions, INumberFormatter, INumberFormatter2, INumberParser, ISignificantDigitsOption, INumberRounderOption, ISignedZeroOption
	{
		// Default language/region used by the parameterless constructor; matches NumeralSystemTranslator's
		// own default (en-US) so default-constructed formatting output is unchanged by locale awareness.
		private const string DefaultGeographicRegion = "US";

		// Real WinRT reports "ZZ" (ISO 3166 "Unknown or Invalid Territory") for ResolvedGeographicRegion
		// when the formatter was default-constructed, i.e. no geographic region was actually resolved.
		// Captured from a live WinRT debugger session watch dump (see the commented block in
		// Given_DecimalFormatter.When_Initialize) - not derived/guessed.
		private const string UnresolvedGeographicRegion = "ZZ";

		private readonly FormatterHelper _formatterHelper;
		private readonly NumeralSystemTranslator _translator;
		private readonly string _numberFormatGeographicRegion;

		public DecimalFormatter()
		{
			_translator = new NumeralSystemTranslator();
			_formatterHelper = new FormatterHelper();
			GeographicRegion = DefaultGeographicRegion;
			ResolvedGeographicRegion = UnresolvedGeographicRegion;
			_numberFormatGeographicRegion = DefaultGeographicRegion;
		}

		/// <summary>
		/// Creates a DecimalFormatter for a given list of languages and a geographic region.
		/// </summary>
		/// <param name="languages">
		/// The list of BCP-47 language tags, in priority order, that represent the language preferences of the user.
		/// </param>
		/// <param name="geographicRegion">
		/// The two-letter ISO 3166 region code (or a region/culture name from which one can be inferred) that represents
		/// the user's home geographic region.
		/// </param>
		public DecimalFormatter(IEnumerable<string> languages, string geographicRegion)
		{
			// NumeralSystemTranslator validates languages is non-null, non-empty and that every
			// language tag maps to a known numeral system; it also resolves ResolvedLanguage/NumeralSystem.
			_translator = new NumeralSystemTranslator(languages);

			GeographicRegionHelper.ValidateGeographicRegion(geographicRegion);

			GeographicRegion = geographicRegion;
			ResolvedGeographicRegion = UnresolvedGeographicRegion;
			_numberFormatGeographicRegion = GeographicRegionHelper.ResolveGeographicRegion(geographicRegion);

			_formatterHelper = new FormatterHelper
			{
				NumberFormat = GeographicRegionHelper.ResolveNumberFormat(_translator.ResolvedLanguage, _numberFormatGeographicRegion, _translator.NumeralSystem)
			};
		}

		public bool IsDecimalPointAlwaysDisplayed { get => _formatterHelper.IsDecimalPointAlwaysDisplayed; set => _formatterHelper.IsDecimalPointAlwaysDisplayed = value; }

		public int FractionDigits { get => _formatterHelper.FractionDigits; set => _formatterHelper.FractionDigits = value; }

		/// <summary>
		/// Gets the region used to determine the numeral system and grouping/separator conventions,
		/// as originally provided to the constructor.
		/// </summary>
		public string GeographicRegion { get; }

		public int IntegerDigits { get => _formatterHelper.IntegerDigits; set => _formatterHelper.IntegerDigits = value; }

		public bool IsGrouped { get => _formatterHelper.IsGrouped; set => _formatterHelper.IsGrouped = value; }

		public bool IsZeroSigned { get => _formatterHelper.IsZeroSigned; set => _formatterHelper.IsZeroSigned = value; }

		public IReadOnlyList<string> Languages => _translator.Languages;

		public INumberRounder? NumberRounder { get; set; }

		public string NumeralSystem
		{
			get => _translator.NumeralSystem;
			set
			{
				_translator.NumeralSystem = value;

				// Re-resolve the punctuation source: switching to/from an Arabic-Indic numeral system
				// changes whether NumeralSystemTranslator localizes the decimal/group separators itself
				// (see GeographicRegionHelper.ResolveNumberFormat), so the previously-resolved
				// NumberFormatInfo may no longer be the correct one to avoid double localization.
				_formatterHelper.NumberFormat = GeographicRegionHelper.ResolveNumberFormat(_translator.ResolvedLanguage, _numberFormatGeographicRegion, _translator.NumeralSystem);
			}
		}

		/// <summary>
		/// Gets the geographic region that was most recently used to format or parse decimal values.
		/// </summary>
		public string ResolvedGeographicRegion { get; }

		public string ResolvedLanguage => _translator.ResolvedLanguage;

		public int SignificantDigits { get => _formatterHelper.SignificantDigits; set => _formatterHelper.SignificantDigits = value; }

		public string Format(double value) => FormatDouble(value);

		public string FormatDouble(double value)
		{
			if (!_formatterHelper.TryValidate(value, out string text))
			{
				return text;
			}

			if (NumberRounder != null)
			{
				value = NumberRounder.RoundDouble(value);
			}


			var stringBuilder = StringBuilderCache.Acquire();

			if (value == 0d)
			{
				_formatterHelper.AppendFormatZero(value, stringBuilder);
			}
			else
			{
				_formatterHelper.AppendFormatDouble(value, stringBuilder);
			}

			_translator.TranslateNumerals(stringBuilder);
			var formatted = StringBuilderCache.GetStringAndRelease(stringBuilder);
			return formatted;
		}

		public double? ParseDouble(string text)
		{
			if (FormatterHelper.TryParseSpecialValue(text, out var specialValue))
			{
				return specialValue;
			}

			text = _translator.TranslateBackNumerals(text);
			return _formatterHelper.ParseDouble(text);
		}
	}
}
