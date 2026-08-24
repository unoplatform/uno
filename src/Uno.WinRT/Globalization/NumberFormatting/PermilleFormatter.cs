#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Uno;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting;

public partial class PermilleFormatter : INumberFormatterOptions, INumberFormatter, INumberFormatter2, INumberParser, ISignificantDigitsOption, INumberRounderOption, ISignedZeroOption
{
	private const int Pow10 = 3;
	private const double ParseCoefficient = 0.001;

	private readonly FormatterHelper _formatterHelper;
	private readonly NumeralSystemTranslator _translator;
	private readonly string _symbol = CultureInfo.InvariantCulture.NumberFormat.PerMilleSymbol;

	public PermilleFormatter()
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

	public INumberRounder? NumberRounder { get; set; }

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

		var stringBuilder = StringBuilderCache.Acquire();

		if (value == 0d)
		{
			_formatterHelper.AppendFormatZero(value, stringBuilder);
		}
		else
		{
			// due to accuracy precision "MultiplyByPow10" is used for multiplication
			value = value.MultiplyByPow10(Pow10);
			_formatterHelper.AppendFormatDouble(value, stringBuilder);
		}

		stringBuilder.Append(_symbol);
		_translator.TranslateNumerals(stringBuilder);

		var formatted = StringBuilderCache.GetStringAndRelease(stringBuilder);
		return formatted;
	}

	public double? ParseDouble(string text)
	{
		text = _translator.TranslateBackNumerals(text);

		if (!text.EndsWith(_symbol, StringComparison.Ordinal))
		{
			return null;
		}

		var stringBuilder = StringBuilderCache.Acquire();
		stringBuilder.Append(text, 0, text.Length - _symbol.Length);
		text = StringBuilderCache.GetStringAndRelease(stringBuilder);
		var result = _formatterHelper.ParseDouble(text);

		if (!result.HasValue)
		{
			return null;
		}

		return result * ParseCoefficient;
	}
}
