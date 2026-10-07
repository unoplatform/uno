#nullable enable

using System.Collections.Generic;
using Uno;
using Uno.Globalization.NumberFormatting;
using Windows.System.UserProfile;

namespace Windows.Globalization.NumberFormatting
{
	public partial class DecimalFormatter : INumberFormatterOptions, INumberFormatter, INumberFormatter2, INumberParser, ISignificantDigitsOption, INumberRounderOption, ISignedZeroOption
	{
		private readonly FormatterHelper _formatterHelper;
		private readonly NumeralSystemTranslator _translator;
		private string? _geographicRegion;

		public DecimalFormatter()
		{
			_formatterHelper = new FormatterHelper();
			_translator = new NumeralSystemTranslator();
		}

		public DecimalFormatter(IEnumerable<string> languages, string geographicRegion)
		{
			_formatterHelper = new FormatterHelper();
			_translator = new NumeralSystemTranslator(languages);

			ValidateGeographicRegion(geographicRegion);
			_geographicRegion = geographicRegion;
		}

		public bool IsDecimalPointAlwaysDisplayed { get => _formatterHelper.IsDecimalPointAlwaysDisplayed; set => _formatterHelper.IsDecimalPointAlwaysDisplayed = value; }

		public int IntegerDigits { get => _formatterHelper.IntegerDigits; set => _formatterHelper.IntegerDigits = value; }

		public bool IsGrouped { get => _formatterHelper.IsGrouped; set => _formatterHelper.IsGrouped = value; }

		public string NumeralSystem
		{
			get => _translator.NumeralSystem;
			set => _translator.NumeralSystem = value;
		}

		public IReadOnlyList<string> Languages => _translator.Languages;

		public string ResolvedLanguage => _translator.ResolvedLanguage;

		public string GeographicRegion => _geographicRegion ??= GlobalizationPreferences.HomeGeographicRegion;

		public string ResolvedGeographicRegion => GeographicRegion.ToUpperInvariant();

		public int FractionDigits { get => _formatterHelper.FractionDigits; set => _formatterHelper.FractionDigits = value; }

		public INumberRounder? NumberRounder { get; set; }

		public bool IsZeroSigned { get => _formatterHelper.IsZeroSigned; set => _formatterHelper.IsZeroSigned = value; }

		public int SignificantDigits { get => _formatterHelper.SignificantDigits; set => _formatterHelper.SignificantDigits = value; }

		public string Format(long value) => FormatInt(value);

		public string Format(ulong value) => FormatUInt(value);

		public string Format(double value) => FormatDouble(value);

		public string FormatInt(long value)
		{
			if (NumberRounder != null)
			{
				value = NumberRounder.RoundInt64(value);
			}

			var magnitude = value < 0 ? (ulong)(-(value + 1)) + 1UL : (ulong)value;

			return FormatInteger(magnitude, value < 0);
		}

		public string FormatUInt(ulong value)
		{
			if (NumberRounder != null)
			{
				value = NumberRounder.RoundUInt64(value);
			}

			return FormatInteger(value, isNegative: false);
		}

		private string FormatInteger(ulong magnitude, bool isNegative)
		{
			var stringBuilder = StringBuilderCache.Acquire();

			if (magnitude == 0)
			{
				_formatterHelper.AppendFormatZero(stringBuilder);
			}
			else
			{
				_formatterHelper.AppendFormatInteger(magnitude, isNegative, stringBuilder);
			}

			_translator.TranslateNumerals(stringBuilder);
			return StringBuilderCache.GetStringAndRelease(stringBuilder);
		}

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
			text = _translator.TranslateBackNumerals(text);
			return _formatterHelper.ParseDouble(text);
		}

		// ISO 3166-1 alpha-2/alpha-3 or UN M.49 numeric code; checked syntactically so it does not depend on the ICU region data.
		private static void ValidateGeographicRegion(string geographicRegion)
		{
			var isValid = geographicRegion?.Length switch
			{
				2 => char.IsAsciiLetter(geographicRegion[0]) && char.IsAsciiLetter(geographicRegion[1]),
				3 => (char.IsAsciiLetter(geographicRegion[0]) && char.IsAsciiLetter(geographicRegion[1]) && char.IsAsciiLetter(geographicRegion[2]))
					|| (char.IsAsciiDigit(geographicRegion[0]) && char.IsAsciiDigit(geographicRegion[1]) && char.IsAsciiDigit(geographicRegion[2])),
				_ => false,
			};

			if (!isValid)
			{
				ExceptionHelper.ThrowArgumentException(nameof(geographicRegion));
			}
		}
	}
}
