#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting;

public partial class NumeralSystemTranslator
{
	private const string NumeralSystemParameterName = "numeralSystem";

	private readonly static string[] _defaultLanguages = { "en-US" };
	private string _numeralSystem = "Latn";

	public NumeralSystemTranslator() : this(_defaultLanguages)
	{

	}

	public NumeralSystemTranslator(IEnumerable<string> languages)
	{
		ValidateLanguages(languages);

		Languages = languages.ToList();
		ResolvedLanguage = NumeralSystemTranslatorHelper.GetResolvedLanguage(Languages[0]);
		NumeralSystem = NumeralSystemTranslatorHelper.GetNumeralSystem(Languages[0]);
	}

	public string NumeralSystem
	{
		get => _numeralSystem;
		set
		{
			if (value is null)
			{
				throw new ArgumentNullException();
			}

			_numeralSystem = NumeralSystemTranslatorHelper.ToPascalCase(value);

			if (string.IsNullOrEmpty(_numeralSystem))
			{
				ExceptionHelper.ThrowArgumentException(NumeralSystemParameterName);
			}
		}
	}

	public IReadOnlyList<string> Languages { get; }

	public string ResolvedLanguage { get; }

	private void ValidateLanguages(IEnumerable<string> languages)
	{
		if (languages is null)
		{
			ExceptionHelper.ThrowNullReferenceException(nameof(languages));
		}

		if (!languages!.Any())
		{
			ExceptionHelper.ThrowArgumentException(nameof(languages));
		}

		foreach (var language in languages!)
		{
			if (string.IsNullOrEmpty(NumeralSystemTranslatorHelper.GetNumeralSystem(language)))
			{
				ExceptionHelper.ThrowArgumentException(nameof(languages));
			}
		}
	}

	public string TranslateNumerals(string value)
	{
		var stringBuilder = StringBuilderCache.Acquire();
		stringBuilder.Append(value);

		TranslateNumerals(stringBuilder);
		var translated = StringBuilderCache.GetStringAndRelease(stringBuilder);
		return translated;
	}

	internal void TranslateNumerals(StringBuilder stringBuilder)
	{
		var digitsSource = NumeralSystemTranslatorHelper.GetDigitsSource(NumeralSystem);

		if (digitsSource == NumeralSystemTranslatorHelper.EmptyDigits)
		{
			ExceptionHelper.ThrowArgumentException(NumeralSystemParameterName);
		}

		if (NumeralSystem.Equals("Arab", StringComparison.Ordinal) ||
			NumeralSystem.Equals("ArabExt", StringComparison.Ordinal))
		{
			TranslateArab(stringBuilder, digitsSource);
		}
		else
		{
			Translate(stringBuilder, digitsSource);
		}
	}

	private static void TranslateArab(StringBuilder stringBuilder, char[] digitsSource)
	{
		for (int i = 0; i < stringBuilder.Length; i++)
		{
			var c = stringBuilder[i];

			switch (c)
			{
				case '.':
					if (IsImmediatelyBeforeALatinDigit(i, stringBuilder))
					{
						stringBuilder[i] = '\u066b';
					}
					break;
				case ',':
					if (IsImmediatelyBeforeALatinDigit(i, stringBuilder))
					{
						stringBuilder[i] = '\u066c';
					}
					break;
				case '%':
					if (IsAdjacentToALatinDigit(i, stringBuilder))
					{
						stringBuilder[i] = '\u066a';
					}
					break;
				case '\u2030': //Per Mille Symbol
					if (IsAdjacentToALatinDigit(i, stringBuilder))
					{
						stringBuilder[i] = '\u0609';
					}
					break;
				default:
					stringBuilder[i] = Translate(c, digitsSource);
					break;
			}
		}
	}

	private static bool IsImmediatelyBeforeALatinDigit(int index, StringBuilder input)
	{
		if (index + 1 >= input.Length)
		{
			return false;
		}

		return char.IsDigit(input[index + 1]);
	}

	private static bool IsAdjacentToALatinDigit(int index, StringBuilder input)
	{
		if (index + 1 < input.Length &&
			char.IsDigit(input[index + 1]))
		{
			return true;
		}

		if (index - 1 >= 0 &&
		   char.IsDigit(input[index - 1]))
		{
			return true;
		}

		return false;
	}

	private static void Translate(StringBuilder stringBuilder, char[] digitsSource)
	{
		for (int i = 0; i < stringBuilder.Length; i++)
		{
			stringBuilder[i] = Translate(stringBuilder[i], digitsSource);
		}
	}

	private static char Translate(char c, char[] digitsSource)
	{
		var d = c - '0';
		var t = c;

		if (d >= 0 && d <= 9)
		{
			t = digitsSource[d];
		}

		return t;
	}

	public string TranslateBackNumerals(string value)
	{
		var stringBuilder = StringBuilderCache.Acquire();
		stringBuilder.Append(value);
		TranslateBackNumerals(stringBuilder);

		var translated = StringBuilderCache.GetStringAndRelease(stringBuilder);
		return translated;
	}

	internal void TranslateBackNumerals(StringBuilder stringBuilder)
	{
		if (NumeralSystem.Equals("Arab", StringComparison.Ordinal) ||
			NumeralSystem.Equals("ArabExt", StringComparison.Ordinal))
		{
			TranslateBackArab(stringBuilder, NumeralSystemTranslatorHelper.GetDigitsSource(NumeralSystem));
		}
		else
		{
			TranslateBack(stringBuilder, NumeralSystemTranslatorHelper.GetDigitsSource(NumeralSystem));
		}
	}

	private static void TranslateBackArab(StringBuilder stringBuilder, char[] digitsSource)
	{
		for (int i = 0; i < stringBuilder.Length; i++)
		{
			var c = stringBuilder[i];

			switch (c)
			{
				case '\u066b':
					stringBuilder[i] = '.';
					break;
				case '\u066c':
					stringBuilder[i] = ',';
					break;
				case '\u066a':
					stringBuilder[i] = '%';
					break;
				case '\u0609': //Per Mille Symbol
					stringBuilder[i] = '\u2030';
					break;
				default:
					stringBuilder[i] = TranslateBack(c, digitsSource);
					break;
			}
		}
	}

	private static void TranslateBack(StringBuilder stringBuilder, char[] digitsSource)
	{
		for (int i = 0; i < stringBuilder.Length; i++)
		{
			stringBuilder[i] = TranslateBack(stringBuilder[i], digitsSource);
		}
	}

	private static char TranslateBack(char c, char[] digitsSource)
	{
		var d = c - digitsSource[0];
		var t = c;

		if (d >= 0 && d <= 9)
		{
			t = (char)(d + '0');
		}

		return t;
	}
}
