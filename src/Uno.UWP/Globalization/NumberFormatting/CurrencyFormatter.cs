#nullable enable

using System;
using System.Collections.Generic;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting;

public partial class CurrencyFormatter : INumberParser, INumberFormatter2, INumberFormatter, INumberFormatterOptions, ISignificantDigitsOption, INumberRounderOption, ISignedZeroOption
{
	private const char NoBreakSpaceChar = ' ';

	private readonly FormatterHelper _formatterHelper;
	private readonly NumeralSystemTranslator _translator;
	private readonly CurrencyData _currencyData = CurrencyData.Empty;
	private readonly string _startWithInCurrencyCodeMode;

	public CurrencyFormatter(string currencyCode)
	{
		_formatterHelper = new FormatterHelper();
		_translator = new NumeralSystemTranslator();

		var currencyData = CurrencyData.GetCurrencyData(currencyCode);

		if (currencyData == CurrencyData.Empty)
		{
			ExceptionHelper.ThrowArgumentException(nameof(currencyCode));
		}

		_currencyData = currencyData;
		FractionDigits = currencyData.DefaultFractionDigits;
		_startWithInCurrencyCodeMode = $"{_currencyData.CurrencyCode}{NoBreakSpaceChar}";
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

		bool needParentheses = false;
		var stringBuilder = StringBuildersContainer.Instance.StringBuilder1;

		switch (Mode)
		{
			case CurrencyFormatterMode.UseSymbol:
				stringBuilder.Append(_currencyData.Symbol);
				break;
			case CurrencyFormatterMode.UseCurrencyCode:
				stringBuilder.Append(_currencyData.CurrencyCode);
				stringBuilder.Append(NoBreakSpaceChar);
				break;
		}

		if (value == 0d)
		{
			if (IsZeroSigned && value.IsNegative())
			{
				_formatterHelper.AppendFormatZero(stringBuilder);
				needParentheses = true;
			}
			else
			{
				_formatterHelper.AppendFormatZero(value, stringBuilder);
			}
		}
		else
		{
			if (value.IsNegative())
			{
				value = -value;
				needParentheses = true;
			}

			_formatterHelper.AppendFormatDouble(value, stringBuilder);
		}

		_translator.TranslateNumerals(stringBuilder);

		if (needParentheses)
		{
			stringBuilder.Insert(0, '(');
			stringBuilder.Append(')');
		}

		var formatted = stringBuilder.ToString();

		stringBuilder.Clear();
		return formatted;
	}

	public double? ParseDouble(string text)
	{
		var startWith = "";

		switch (Mode)
		{
			case CurrencyFormatterMode.UseSymbol:
				startWith = _currencyData.Symbol;
				break;
			case CurrencyFormatterMode.UseCurrencyCode:
				startWith = _startWithInCurrencyCodeMode;
				break;
		}

		if (!text.StartsWith(startWith, StringComparison.Ordinal))
		{
			return null;
		}

		var stringBuilder = StringBuildersContainer.Instance.StringBuilder1;

		try
		{
			stringBuilder.Append(text, startWith.Length, text.Length - startWith.Length);
			_translator.TranslateBackNumerals(stringBuilder);
			text = stringBuilder.ToString();
			var result = _formatterHelper.ParseDouble(text);

			if (!result.HasValue)
			{
				return null;
			}

			return result;
		}
		finally
		{
			stringBuilder.Clear();
		}
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
