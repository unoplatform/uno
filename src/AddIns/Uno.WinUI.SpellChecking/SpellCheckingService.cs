using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Documents;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Logging;
using Uno.UI;
using Uno.WinUI.SpellChecking;
using WeCantSpell.Hunspell;

[assembly: ApiExtension(
	typeof(ISpellCheckingService),
	typeof(SpellCheckingService))]

namespace Uno.WinUI.SpellChecking;

public class SpellCheckingService : ISpellCheckingService
{
	private readonly List<WordList> _wordLists = GetWordLists();

	public SpellCheckingService(object owner)
	{
	}

	public List<(int correctionStart, int correctionEnd)?> SpellCheck(List<int> wordBoundaries, string text)
	{
		var ret = new List<(int correctionStart, int correctionEnd)?>();
		var start = 0;
		foreach (var end in wordBoundaries)
		{
			var word = text.Substring(start, end - start);
			var startTrimmedWord = word.TrimStart();
			var trimmedWord = startTrimmedWord.TrimEnd();

			if (trimmedWord.Length > 0 && !trimmedWord.Any(c => char.IsPunctuation(c) || char.IsNumber(c) || char.IsSeparator(c) || char.IsWhiteSpace(c) || char.IsSymbol(c)))
			{
				if (_wordLists.Any(wordList => wordList.Check(trimmedWord)))
				{
					ret.Add(null);
				}
				else
				{
					var correctionStart = word.Length - startTrimmedWord.Length;
					var correctionEnd = word.Length - startTrimmedWord.Length + trimmedWord.Length;
					ret.Add((correctionStart, correctionEnd));
				}
			}
			else
			{
				ret.Add(null);
			}

			start = end;
		}

		return ret;
	}

	private static List<WordList> GetWordLists()
	{
		var wordLists = new List<WordList>();
		var assembly = Assembly.GetAssembly(typeof(SpellCheckingService))!;
		var enAff = assembly.GetManifestResourceNames().First(r => r.Contains("en_US.aff"));
		var enDic = assembly.GetManifestResourceNames().First(r => r.Contains("en_US.dic"));
		wordLists.Add(WordList.CreateFromStreams(assembly.GetManifestResourceStream(enDic), assembly.GetManifestResourceStream(enAff)));

		if (FeatureConfiguration.TextBox.CustomSpellCheckDictionaries is { } dictionaries)
		{
			foreach (var (dic, aff) in dictionaries)
			{
				if (aff != null)
				{
					try
					{
						wordLists.Add(WordList.CreateFromStreams(dic, aff));
					}
					catch (Exception e)
					{
						if (typeof(SpellCheckingService).Log().IsEnabled(LogLevel.Error))
						{
							typeof(SpellCheckingService).Log().Error($"Failed to load dictionary", e);
						}
					}
				}
			}
		}

		return wordLists;
	}

	public (int replaceIndexStart, int replaceIndexEnd, List<string> suggestions)? GetSpellCheckSuggestions(string text, List<int> wordBoundaries, int correctionStart, int correctionEnd)
	{
		var index = wordBoundaries.BinarySearch(correctionStart);
		var i = index >= 0 ? index + 1 : ~index;

		if (i < wordBoundaries.Count)
		{
			var boundary = wordBoundaries[i];
			var start = i == 0 ? 0 : wordBoundaries[i - 1];

			if (start <= correctionStart && boundary >= correctionEnd)
			{
				var word = text.Substring(start, boundary - start);
				var startTrimmedWord = word.TrimStart();
				var trimmedWord = startTrimmedWord.TrimEnd();
				var suggestions = _wordLists.SelectMany(wordList => wordList.Suggest(trimmedWord)).Distinct().OrderBy(w => LevenshteinDistance(w, trimmedWord)).ToList();
				return (start + word.Length - startTrimmedWord.Length, start + word.Length - startTrimmedWord.Length + trimmedWord.Length, suggestions);
			}
		}
		return null;
	}

	private static int LevenshteinDistance(string s, string t)
	{
		if (string.IsNullOrEmpty(s))
		{
			return string.IsNullOrEmpty(t) ? 0 : t.Length;
		}

		if (string.IsNullOrEmpty(t))
		{
			return s.Length;
		}

		var n = s.Length;
		var m = t.Length;

		var prev = new int[m + 1];
		var curr = new int[m + 1];

		// Initialize first row
		for (var j = 0; j <= m; j++)
		{
			prev[j] = j;
		}

		for (var i = 1; i <= n; i++)
		{
			curr[0] = i;
			for (var j = 1; j <= m; j++)
			{
				var cost = s[i - 1] == t[j - 1] ? 0 : 1;

				curr[j] = Math.Min(
					Math.Min(
						prev[j] + 1,        // deletion
						curr[j - 1] + 1     // insertion
					),
					prev[j - 1] + cost      // substitution
				);
			}

			// Swap rows
			(prev, curr) = (curr, prev);
		}

		return prev[m];
	}
}
