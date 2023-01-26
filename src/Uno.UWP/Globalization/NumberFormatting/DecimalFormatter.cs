using System.Collections.Generic;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting
{
	public partial class DecimalFormatter : INumberFormatterOptions, INumberFormatter, INumberFormatter2, INumberParser, ISignificantDigitsOption, INumberRounderOption, ISignedZeroOption
	{
		private readonly FormatterHelper _formatterHelper;
		private readonly NumeralSystemTranslator _translator;

		public DecimalFormatter()
		{
			_formatterHelper = new FormatterHelper();
			_translator = new NumeralSystemTranslator();
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

		public int FractionDigits { get => _formatterHelper.FractionDigits; set => _formatterHelper.FractionDigits = value; }

		public INumberRounder NumberRounder { get; set; }

		public bool IsZeroSigned { get => _formatterHelper.IsZeroSigned; set => _formatterHelper.IsZeroSigned = value; }

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

			string formatted = string.Empty;

			if (value == 0d)
			{
				formatted = _formatterHelper.FormatZero(value);
			}
			else
			{
				formatted = _formatterHelper.FormatDoubleCore(value);
			}

			formatted = _translator.TranslateNumerals(formatted);
			return formatted;
		}

		public double? ParseDouble(string text)
		{
			text = _translator.TranslateBackNumerals(text);
			return _formatterHelper.ParseDoubleCore(text);
		}

	}
}
