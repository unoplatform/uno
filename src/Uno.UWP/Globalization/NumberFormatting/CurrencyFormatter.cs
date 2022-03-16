using System;
using System.Collections.Generic;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting;

public partial class CurrencyFormatter : INumberParser, INumberFormatter2, INumberFormatter, INumberFormatterOptions, ISignificantDigitsOption, INumberRounderOption, ISignedZeroOption
{
	private const char NoBreakSpaceChar = ' ';

	private readonly FormatterHelper _formatterHelper;
	private readonly NumeralSystemTranslator _translator;
	private readonly CurrencyData _currencyData;

	public CurrencyFormatter(string currencyCode)
	{
		_formatterHelper = new FormatterHelper();
		_translator = new NumeralSystemTranslator();

		var currencyData = CurrencyData.GetCurrencyData(currencyCode);

		if (currencyData is null)
		{
			ExceptionHelper.ThrowArgumentException(nameof(currencyCode));
		}

		_currencyData = currencyData;
		FractionDigits = currencyData.DefaultFractionDigits;
	}

	public CurrencyFormatterMode Mode { get; set; }

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
		bool needParentheses = false;

		if (value == 0d)
		{
			if (IsZeroSigned && value.IsNegative())
			{
				formatted = _formatterHelper.FormatZeroCore();
				needParentheses = true;
			}
			else
			{
				formatted = _formatterHelper.FormatZero(value);
			}
		}
		else
		{
			if (value.IsNegative())
			{
				value = -value;
				needParentheses = true;
			}

			formatted = _formatterHelper.FormatDoubleCore(value);
		}

		switch (Mode)
		{
			case CurrencyFormatterMode.UseSymbol:
				formatted = $"{_currencyData.Symbol}{formatted}";
				break;
			case CurrencyFormatterMode.UseCurrencyCode:
				formatted = $"{_currencyData.CurrencyCode}{NoBreakSpaceChar}{formatted}";
				break;
		}

		formatted = _translator.TranslateNumerals(formatted);

		if (needParentheses)
		{
			formatted = $"({formatted})";
		}

		return formatted;
	}

	public double? ParseDouble(string text)
	{
		text = _translator.TranslateBackNumerals(text);

		var startWith = "";

		switch (Mode)
		{
			case CurrencyFormatterMode.UseSymbol:
				startWith = _currencyData.Symbol;
				break;
			case CurrencyFormatterMode.UseCurrencyCode:
				startWith = $"{_currencyData.CurrencyCode}{NoBreakSpaceChar}";
				break;
		}

		if (!text.StartsWith(startWith, StringComparison.Ordinal))
		{
			return null;
		}

		text = text.Substring(startWith.Length);

		var result = _formatterHelper.ParseDoubleCore(text);

		if (!result.HasValue)
		{
			return null;
		}

		return result;
	}

	public void ApplyRoundingForCurrency(RoundingAlgorithm roundingAlgorithm)
	{
		NumberRounder = new IncrementNumberRounder
		{
			RoundingAlgorithm = roundingAlgorithm,
			Increment = Math.Pow(10, -_currencyData.DefaultFractionDigits)
		};
	}
}
