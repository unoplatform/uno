#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting;

public partial class CurrencyFormatter : INumberParser, INumberFormatter2, INumberFormatter, INumberFormatterOptions, ISignificantDigitsOption, INumberRounderOption, ISignedZeroOption
{
	private const char NoBreakSpaceChar = ' ';
	private const char OpenPatternSymbol = '(';
	private const char ClosePatternSymbol = ')';
	private const char SpaceSymbol = ' ';
	private readonly FormatterHelper _formatterHelper;
	private readonly NumeralSystemTranslator _translator;
	private readonly CurrencyData _currencyData = CurrencyData.Empty;
	private readonly string _startWithInCurrencyCodeMode;

	private CurrencyFormatterMode _mode;
	private int _positivePattern = -1;
	private int _negativePattern = -1;
	private Regex _positiveRegex;
	private Regex _negativeRegex;

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

		_negativeRegex = CreateNegativeNumberRegex();
		_positiveRegex = CreatePositiveNumberRegex();
	}

	public CurrencyFormatterMode Mode
	{
		get => _mode;
		set
		{
			if (_mode == value)
			{
				return;
			}

			_mode = value;
			_negativeRegex = CreateNegativeNumberRegex();
			_positiveRegex = CreatePositiveNumberRegex();
		}
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

		var isNegative = value.IsNegative();
		var stringBuilder = StringBuilderCache.Acquire();

		if (isNegative)
		{
			value = -value;
		}

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

		if (isNegative)
		{
			switch (Mode)
			{
				case CurrencyFormatterMode.UseSymbol:
					formatted = FormatNegativeWithSymbol(formatted);
					break;
				case CurrencyFormatterMode.UseCurrencyCode:
					formatted = FormatNegativeWithCurrencyCode(formatted);
					break;
				default:
					break;
			}
		}
		else
		{
			switch (Mode)
			{
				case CurrencyFormatterMode.UseSymbol:
					formatted = FormatPositiveWithSymbol(formatted);
					break;
				case CurrencyFormatterMode.UseCurrencyCode:
					formatted = FormatPositiveWithCurrencyCode(formatted);
					break;
				default:
					break;
			}
		}

		return formatted;
	}

	private string FormatNegativeWithCurrencyCode(string text)
	{
		var symbol = _currencyData.CurrencyCode;
		var negativeSign = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyNegativePattern;
		var spaceSymbol = NoBreakSpaceChar;
		var stringBuilder = StringBuilderCache.Acquire();

		switch (pattern)
		{
			case 0:
			case 14:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			case 1:
			case 9:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				break;
			case 2:
			case 12:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				break;
			case 3:
			case 11:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				break;
			case 4:
			case 15:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			case 5:
			case 8:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;
			case 6:
			case 13:
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;
			case 7:
			case 10:
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(negativeSign);
				break;

			default:
				break;
		}

		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	private string FormatNegativeWithSymbol(string text)
	{
		var symbol = _currencyData.Symbol;
		var negativeSign = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyNegativePattern;
		var stringBuilder = StringBuilderCache.Acquire();

		switch (pattern)
		{
			case 0:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(text);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			case 1:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(symbol);
				stringBuilder.Append(text);
				break;
			case 2:
				stringBuilder.Append(symbol);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				break;
			case 3:
				stringBuilder.Append(symbol);
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				break;
			case 4:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(symbol);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			case 5:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				stringBuilder.Append(symbol);
				break;
			case 6:
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(symbol);
				break;
			case 7:
				stringBuilder.Append(text);
				stringBuilder.Append(symbol);
				stringBuilder.Append(negativeSign);
				break;
			case 8:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				stringBuilder.Append(SpaceSymbol);
				stringBuilder.Append(symbol);
				break;
			case 9:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(symbol);
				stringBuilder.Append(SpaceSymbol);
				stringBuilder.Append(text);
				break;
			case 10:
				stringBuilder.Append(text);
				stringBuilder.Append(SpaceSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(negativeSign);
				break;
			case 11:
				stringBuilder.Append(symbol);
				stringBuilder.Append(SpaceSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				break;
			case 12:
				stringBuilder.Append(symbol);
				stringBuilder.Append(SpaceSymbol);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				break;
			case 13:
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(SpaceSymbol);
				stringBuilder.Append(symbol);
				break;
			case 14:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(SpaceSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			case 15:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(SpaceSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			default:
				break;
		}

		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	private string FormatPositiveWithCurrencyCode(string text)
	{
		var symbol = _currencyData.CurrencyCode;
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyPositivePattern;
		var spaceSymbol = NoBreakSpaceChar;
		var stringBuilder = StringBuilderCache.Acquire();

		switch (pattern)
		{
			case 0:
			case 2:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				break;
			case 1:
			case 3:
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;
			default:
				break;
		}

		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	private string FormatPositiveWithSymbol(string text)
	{
		var symbol = _currencyData.Symbol;
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyPositivePattern;
		var spaceSymbol = SpaceSymbol;
		var stringBuilder = StringBuilderCache.Acquire();

		switch (pattern)
		{
			case 0:
				stringBuilder.Append(symbol);
				stringBuilder.Append(text);
				break;
			case 1:
				stringBuilder.Append(text);
				stringBuilder.Append(symbol);
				break;
			case 2:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				break;
			case 3:
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;
			default:
				break;
		}

		return StringBuilderCache.GetStringAndRelease(stringBuilder);
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

		var stringBuilder = StringBuilderCache.Acquire();
		stringBuilder.Append(text, startWith.Length, text.Length - startWith.Length);
		_translator.TranslateBackNumerals(stringBuilder);
		text = StringBuilderCache.GetStringAndRelease(stringBuilder);
		var result = _formatterHelper.ParseDouble(text);

		if (!result.HasValue)
		{
			return null;
		}

		return result;

	}

	private bool TryMatchNegativeNumber(string text, out string value)
	{
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyNegativePattern;

		if (pattern != _negativePattern)
		{
			_negativeRegex = CreateNegativeNumberRegex();
		}

		return false;
	}

	private Regex CreateNegativeNumberRegex()
	{
		var pattern = "";

		return new Regex(pattern);
	}

	private Regex CreatePositiveNumberRegex()
	{
		var pattern = "";

		return new Regex(pattern);
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
