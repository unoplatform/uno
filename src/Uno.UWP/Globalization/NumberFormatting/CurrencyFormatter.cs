#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Uno;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting;

public partial class CurrencyFormatter : INumberParser, INumberFormatter2, INumberFormatter, INumberFormatterOptions, ISignificantDigitsOption, INumberRounderOption, ISignedZeroOption
{
	private const char NoBreakSpaceChar = '\u00A0';
	private const char OpenPatternSymbol = '(';
	private const char ClosePatternSymbol = ')';
	private const string EscapedOpenPatternSymbol = @"\(";
	private const string EscapedClosePatternSymbol = @"\)";
	private const string NumberPattern = $"(?<{ValueCaptureingGroupName}>[0-9.]*)";
	private const string ValueCaptureingGroupName = "value";
	private readonly FormatterHelper _formatterHelper;
	private readonly NumeralSystemTranslator _translator;
	private readonly CurrencyData _currencyData = CurrencyData.Empty;
	private readonly string _geographicRegion;

	private CurrencyFormatterMode _mode;
	private int _positivePattern = -1;
	private int _negativePattern = -1;
	private string _escapedCurrencySymbol;
	private Regex _positiveRegex;
	private Regex _negativeRegex;

	public CurrencyFormatter(string currencyCode)
		: this(currencyCode, null, null)
	{
	}

	public CurrencyFormatter(string currencyCode, IEnumerable<string>? languages, string? geographicRegion)
	{
		var currencyData = CurrencyData.GetCurrencyData(currencyCode);

		if (currencyData == CurrencyData.Empty)
		{
			ExceptionHelper.ThrowArgumentException(nameof(currencyCode));
		}

		_currencyData = currencyData;

		if (languages != null && languages.Any())
		{
			_translator = new NumeralSystemTranslator(languages);
		}
		else
		{
			_translator = new NumeralSystemTranslator();
		}

		_formatterHelper = new FormatterHelper();
		_geographicRegion = geographicRegion ?? string.Empty;
		FractionDigits = currencyData.DefaultFractionDigits;

		_negativeRegex = CreateNegativeNumberRegex();
		_positiveRegex = CreatePositiveNumberRegex();
		_escapedCurrencySymbol = GetEscapedCurrencySymbol();
	}

	public string Currency => _currencyData.CurrencyCode;

	public string GeographicRegion => _geographicRegion;

	public string ResolvedGeographicRegion => _geographicRegion;

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
			_escapedCurrencySymbol = GetEscapedCurrencySymbol();
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

	public string Format(long value) => FormatInt(value);

	public string Format(ulong value) => FormatUInt(value);

	public string Format(double value) => FormatDouble(value);

	public string FormatInt(long value) => FormatDouble(value);

	public string FormatUInt(ulong value) => FormatDouble(value);

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
			_formatterHelper.AppendFormatZero(stringBuilder);

			if (!IsZeroSigned)
			{
				isNegative = false;
			}
		}
		else
		{
			_formatterHelper.AppendFormatDouble(value, stringBuilder);
		}

		_translator.TranslateNumerals(stringBuilder);

		var formatted = StringBuilderCache.GetStringAndRelease(stringBuilder);

		if (isNegative)
		{
			if (_currencyData.AlwaysUseCurrencyCode ||
				Mode == CurrencyFormatterMode.UseCurrencyCode)
			{
				formatted = FormatCurrencyCodeModeNegativeNumber(formatted);
			}
			else
			{
				formatted = FormatSymbolModeNegativeNumber(formatted);
			}
		}
		else
		{
			if (_currencyData.AlwaysUseCurrencyCode ||
				Mode == CurrencyFormatterMode.UseCurrencyCode)
			{
				formatted = FormatCurrencyCodeModePositiveNumber(formatted);
			}
			else
			{
				formatted = FormatSymbolModePositiveNumber(formatted);
			}
		}

		return formatted;
	}

	private string FormatCurrencyCodeModeNegativeNumber(string text)
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
			case 7:
			case 10:
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;

			default:
				break;
		}

		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	private string FormatSymbolModeNegativeNumber(string text)
	{
		var symbol = _currencyData.Symbol;
		var negativeSign = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyNegativePattern;
		var stringBuilder = StringBuilderCache.Acquire();
		var spaceSymbol = NoBreakSpaceChar;

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
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;
			case 9:
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				break;
			case 10:
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(negativeSign);
				break;
			case 11:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				break;
			case 12:
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(text);
				break;
			case 13:
				stringBuilder.Append(text);
				stringBuilder.Append(negativeSign);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				break;
			case 14:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			case 15:
				stringBuilder.Append(OpenPatternSymbol);
				stringBuilder.Append(text);
				stringBuilder.Append(spaceSymbol);
				stringBuilder.Append(symbol);
				stringBuilder.Append(ClosePatternSymbol);
				break;
			default:
				break;
		}

		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	private string FormatCurrencyCodeModePositiveNumber(string text)
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

	private string FormatSymbolModePositiveNumber(string text)
	{
		var symbol = _currencyData.Symbol;
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyPositivePattern;
		var spaceSymbol = NoBreakSpaceChar;
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

	public long? ParseInt(string text)
	{
		var result = ParseDouble(text);
		if (result.HasValue)
		{
			var truncated = Math.Truncate(result.Value);
			if (truncated >= long.MinValue && truncated <= long.MaxValue)
			{
				return (long)truncated;
			}
		}
		return null;
	}

	public ulong? ParseUInt(string text)
	{
		var result = ParseDouble(text);
		if (result.HasValue)
		{
			var truncated = Math.Truncate(result.Value);
			if (truncated >= 0 && truncated <= ulong.MaxValue)
			{
				return (ulong)truncated;
			}
		}
		return null;
	}

	public double? ParseDouble(string text)
	{
		text = _translator.TranslateBackNumerals(text);

		var isNegative = false;

		if (TryMatchPositiveNumber(text, out string positiveNumber))
		{
			text = positiveNumber;
		}
		else if (TryMatchNegativeNumber(text, out string negativeNumber))
		{
			text = negativeNumber;
			isNegative = true;
		}
		else
		{
			return null;
		}

		var result = _formatterHelper.ParseDouble(text);

		if (!result.HasValue)
		{
			return null;
		}

		if (isNegative)
		{
			result = -result;
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

		var match = _negativeRegex.Match(text);

		if (match.Success)
		{
			value = match.Groups[ValueCaptureingGroupName].Value;
		}
		else
		{
			value = string.Empty;
		}

		return match.Success;
	}

	private bool TryMatchPositiveNumber(string text, out string value)
	{
		var pattern = CultureInfo.CurrentCulture.NumberFormat.CurrencyPositivePattern;

		if (pattern != _positivePattern)
		{
			_positiveRegex = CreatePositiveNumberRegex();
		}

		var match = _positiveRegex.Match(text);

		if (match.Success)
		{
			value = match.Groups[ValueCaptureingGroupName].Value;
		}
		else
		{
			value = string.Empty;
		}

		return match.Success;
	}

	private Regex CreateNegativeNumberRegex()
	{
		string pattern;

		if (_currencyData.AlwaysUseCurrencyCode ||
			Mode == CurrencyFormatterMode.UseCurrencyCode)
		{
			pattern = GetCurrencyCodeModeNegativeNumberPattern();
		}
		else
		{
			pattern = GetSymbolModeNegativeNumberPattern();
		}

		return new Regex(pattern);
	}

	private string GetCurrencyCodeModeNegativeNumberPattern()
	{
		var patternNumber = CultureInfo.CurrentCulture.NumberFormat.CurrencyNegativePattern;
		var negativeSign = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
		var spacePattern = $"[\\s{NoBreakSpaceChar}]";

		switch (patternNumber)
		{
			case 0:
			case 14:
				return $"{EscapedOpenPatternSymbol}{_escapedCurrencySymbol}{spacePattern}{NumberPattern}{EscapedClosePatternSymbol}";
			case 1:
			case 9:
			case 2:
			case 12:
				return $"{_escapedCurrencySymbol}{spacePattern}{negativeSign}{NumberPattern}";
			case 3:
			case 11:
				return $"{_escapedCurrencySymbol}{spacePattern}{NumberPattern}{negativeSign}";
			case 4:
			case 15:
				return $"{EscapedOpenPatternSymbol}{NumberPattern}{spacePattern}{_escapedCurrencySymbol}{EscapedClosePatternSymbol}";
			case 5:
			case 8:
				return $"{negativeSign}{NumberPattern}{spacePattern}{_escapedCurrencySymbol}";
			case 6:
			case 13:
			case 7:
			case 10:
				return $"{NumberPattern}{negativeSign}{spacePattern}{_escapedCurrencySymbol}";
			default:
				return string.Empty;
		}
	}

	private string GetSymbolModeNegativeNumberPattern()
	{
		var patternNumber = CultureInfo.CurrentCulture.NumberFormat.CurrencyNegativePattern;
		var negativeSign = CultureInfo.CurrentCulture.NumberFormat.NegativeSign;
		var spacePattern = $"[\\s{NoBreakSpaceChar}]";

		switch (patternNumber)
		{
			case 0:
				return $"{EscapedOpenPatternSymbol}{_escapedCurrencySymbol}{spacePattern}{NumberPattern}{EscapedClosePatternSymbol}";
			case 1:
				return $"{negativeSign}{_escapedCurrencySymbol}{spacePattern}{NumberPattern}";
			case 2:
				return $"{_escapedCurrencySymbol}{spacePattern}{negativeSign}{NumberPattern}";
			case 3:
				return $"{_escapedCurrencySymbol}{spacePattern}{NumberPattern}{negativeSign}";
			case 4:
				return $"{EscapedOpenPatternSymbol}{NumberPattern}{spacePattern}{_escapedCurrencySymbol}{EscapedClosePatternSymbol}";
			case 5:
				return $"{negativeSign}{NumberPattern}{spacePattern}{_escapedCurrencySymbol}";
			case 6:
				return $"{NumberPattern}{negativeSign}{spacePattern}{_escapedCurrencySymbol}";
			case 7:
				return $"{NumberPattern}{spacePattern}{_escapedCurrencySymbol}{negativeSign}";
			case 8:
				return $"{negativeSign}{NumberPattern}{_escapedCurrencySymbol}";
			case 9:
				return $"{negativeSign}{_escapedCurrencySymbol}{NumberPattern}";
			case 10:
				return $"{NumberPattern}{_escapedCurrencySymbol}{negativeSign}";
			case 11:
				return $"{_escapedCurrencySymbol}{NumberPattern}{negativeSign}";
			case 12:
				return $"{_escapedCurrencySymbol}{spacePattern}{NumberPattern}";
			case 13:
				return $"{NumberPattern}{negativeSign}{_escapedCurrencySymbol}";
			case 14:
				return $"{EscapedOpenPatternSymbol}{_escapedCurrencySymbol}{NumberPattern}{EscapedClosePatternSymbol}";
			case 15:
				return $"{EscapedOpenPatternSymbol}{NumberPattern}{_escapedCurrencySymbol}{EscapedClosePatternSymbol}";
			default:
				return string.Empty;
		}
	}

	private Regex CreatePositiveNumberRegex()
	{
		string pattern;

		if (_currencyData.AlwaysUseCurrencyCode ||
			Mode == CurrencyFormatterMode.UseCurrencyCode)
		{
			pattern = GetCurrencyCodeModePositiveNumberPattern();
		}
		else
		{
			pattern = GetSymbolModePositiveNumberPattern();
		}

		return new Regex(pattern);
	}

	private string GetCurrencyCodeModePositiveNumberPattern()
	{
		var patternNumber = CultureInfo.CurrentCulture.NumberFormat.CurrencyPositivePattern;
		var spacePattern = $"[\\s{NoBreakSpaceChar}]";

		switch (patternNumber)
		{

			case 0:
			case 2:
				return $"{_escapedCurrencySymbol}{spacePattern}{NumberPattern}";
			case 1:
			case 3:
				return $"{NumberPattern}{spacePattern}{_escapedCurrencySymbol}";
			default:
				return string.Empty;
		}
	}

	private string GetSymbolModePositiveNumberPattern()
	{
		var patternNumber = CultureInfo.CurrentCulture.NumberFormat.CurrencyPositivePattern;
		var spacePattern = $"[\\s{NoBreakSpaceChar}]";

		switch (patternNumber)
		{
			case 0:
				return $"{_escapedCurrencySymbol}{NumberPattern}";
			case 1:
				return $"{NumberPattern}{_escapedCurrencySymbol}";
			case 2:
				return $"{_escapedCurrencySymbol}{spacePattern}{NumberPattern}";
			case 3:
				return $"{NumberPattern}{spacePattern}{_escapedCurrencySymbol}";
			default:
				return string.Empty;
		}
	}

	private string GetEscapedCurrencySymbol()
	{
		switch (Mode)
		{
			case CurrencyFormatterMode.UseSymbol:
				return Regex.Escape(_currencyData.Symbol);
			case CurrencyFormatterMode.UseCurrencyCode:
				return Regex.Escape(_currencyData.CurrencyCode);
			default:
				return string.Empty;
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
