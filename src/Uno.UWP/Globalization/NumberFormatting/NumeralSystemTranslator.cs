#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uno.Globalization.NumberFormatting;

namespace Windows.Globalization.NumberFormatting
{
	public partial class NumeralSystemTranslator
	{
		private readonly static string[] _defaultLanguages = { "en-US" };
		private string _numeralSystem;

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
					throw new ArgumentNullException("Value cannot be null.");
				}

				_numeralSystem = NumeralSystemTranslatorHelper.ToPascalCase(value);
			}
		}

		public IReadOnlyList<string> Languages { get; }

		public string ResolvedLanguage { get; }

		private void ValidateLanguages(IEnumerable<string> languages)
		{
			if (languages is null)
			{
				throw new NullReferenceException("Invalid pointer");
			}

			if (!languages.Any())
			{
				throw new ArgumentException("The parameter is incorrect.");
			}

			foreach (var language in languages)
			{
				NumeralSystemTranslatorHelper.GetNumeralSystem(language);
			}
		}

		public string TranslateNumerals(string value)
		{
			if (NumeralSystem.Equals("Arab", StringComparison.Ordinal) ||
				NumeralSystem.Equals("ArabExt", StringComparison.Ordinal))
			{
				return TranslateArab(value, NumeralSystemTranslatorHelper.GetDigitsSource(NumeralSystem));
			}

			return Translate(value, NumeralSystemTranslatorHelper.GetDigitsSource(NumeralSystem));
		}

		private static string TranslateArab(string value, char[] digitsSource)
		{
			var chars = value.ToCharArray();

			for (int i = 0; i < chars.Length; i++)
			{
				var c = chars[i];

				switch (c)
				{
					case '.':
						if (IsImmediatelyBeforeALatinDigit(i, chars))
						{
							chars[i] = '\u066b';
						}
						break;
					case ',':
						if (IsImmediatelyBeforeALatinDigit(i, chars))
						{
							chars[i] = '\u066c';
						}
						break;
					case '%':
						if (IsAdjacentToALatinDigit(i, chars))
						{
							chars[i] = '\u066a';
						}
						break;
					case '\u2030': //Per Mille Symbol
						if (IsAdjacentToALatinDigit(i, chars))
						{
							chars[i] = '\u0609';
						}
						break;
					default:
						chars[i] = Translate(c, digitsSource);
						break;
				}
			}

			return new string(chars);
		}

		private static bool IsImmediatelyBeforeALatinDigit(int index, char[] input)
		{
			if (index + 1 >= input.Length)
			{
				return false;
			}

			return char.IsDigit(input[index + 1]);
		}

		private static bool IsAdjacentToALatinDigit(int index, char[] input)
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

		private static string Translate(string value, char[] digitsSource)
		{
			var chars = value.ToCharArray();

			for (int i = 0; i < chars.Length; i++)
			{
				chars[i] = Translate(chars[i], digitsSource);
			}

			return new string(chars);
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
			if (NumeralSystem.Equals("Arab", StringComparison.Ordinal) ||
				NumeralSystem.Equals("ArabExt", StringComparison.Ordinal))
			{
				return TranslateBackArab(value, NumeralSystemTranslatorHelper.GetDigitsSource(NumeralSystem));
			}

			return TranslateBack(value, NumeralSystemTranslatorHelper.GetDigitsSource(NumeralSystem));
		}

		private static string TranslateBackArab(string value, char[] digitsSource)
		{
			var chars = value.ToCharArray();

			for (int i = 0; i < chars.Length; i++)
			{
				var c = chars[i];

				switch (c)
				{
					case '\u066b':
						chars[i] = '.';
						break;
					case '\u066c':
						chars[i] = ',';
						break;
					case '\u066a':
						chars[i] = '%';
						break;
					case '\u0609': //Per Mille Symbol
						chars[i] = '\u2030';
						break;
					default:
						chars[i] = TranslateBack(c, digitsSource);
						break;
				}
			}

			return new string(chars);
		}

		private static string TranslateBack(string value, char[] digitsSource)
		{
			var chars = value.ToCharArray();

			for (int i = 0; i < chars.Length; i++)
			{
				chars[i] = TranslateBack(chars[i], digitsSource);
			}

			return new string(chars);
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
}
