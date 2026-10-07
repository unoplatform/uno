#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SampleControl.Entities;

namespace SampleControl.Presentation;

/// <summary>
/// Ranks samples for the shell search: name prefix, then name word matches (word-start abbreviations
/// such as "tb" for TextBox or "btev" for Button_Events, and substrings), then category substring,
/// then type name substring. Name tiers ignore separators in both the term and the name. Name word
/// matches are scored by how compact they are (see <see cref="GetWordStartCost"/>), then by how early
/// they start; remaining ties go by name.
/// </summary>
internal static class SampleSearch
{
	internal const int MaxGroupedResults = 200;

	private enum Tier
	{
		NameStartsWith,
		NameWords,
		CategoryContains,
		TypeNameContains,
	}

	public static List<SampleChooserContent> Rank(string? term, IEnumerable<SampleChooserContent> items, CancellationToken ct = default)
	{
		var trimmed = term?.Trim();
		if (string.IsNullOrEmpty(trimmed))
		{
			return [];
		}

		var compactTerm = Compact(trimmed);
		HashSet<SampleChooserContent> seen = new();
		List<(Tier Tier, int Score, int FirstWord, SampleChooserContent Content)> matches = new();

		foreach (var item in items)
		{
			ct.ThrowIfCancellationRequested();

			if (item?.ControlName is null || !seen.Add(item))
			{
				continue;
			}

			if (GetTier(trimmed, compactTerm, item) is { } match)
			{
				matches.Add((match.Tier, match.Score, match.FirstWord, item));
			}
		}

		return matches
			.OrderBy(m => m.Tier)
			.ThenBy(m => m.Score)
			.ThenBy(m => m.FirstWord)
			.ThenBy(m => m.Content.ControlName, StringComparer.OrdinalIgnoreCase)
			.ThenBy(m => m.Content.ControlName, StringComparer.Ordinal)
			.Select(m => m.Content)
			.ToList();
	}

	/// <summary>
	/// Groups ranked results by their first category, keeping the rank order of groups
	/// (by their best result) and of items inside each group.
	/// </summary>
	public static List<SampleSearchGroup> Group(IEnumerable<SampleChooserContent> ranked, int max = MaxGroupedResults)
	{
		List<SampleSearchGroup> groups = new();
		Dictionary<string, SampleSearchGroup> byKey = new(StringComparer.Ordinal);

		foreach (var item in ranked.Take(max))
		{
			var key = item.Categories?.FirstOrDefault() ?? "";
			if (!byKey.TryGetValue(key, out var group))
			{
				group = new SampleSearchGroup(key);
				byKey.Add(key, group);
				groups.Add(group);
			}

			group.Add(item);
		}

		return groups;
	}

	internal static bool IsWordStartMatch(string term, string name) => GetWordStartCost(term, name) > 0;

	/// <summary>
	/// Matches <paramref name="term"/> as pieces that each start a word and may continue inside it
	/// ("btev" is B·t + Ev in Button_Events). Returns the lowest cost, or 0 when it does not match:
	/// 2 per word spanned from the first to the last matched word, plus 3 per in-word skip.
	/// </summary>
	internal static int GetWordStartCost(string term, string name) => GetWordStartMatch(term, name)?.Cost ?? 0;

	private static (int Cost, int FirstWord)? GetWordStartMatch(string term, string name)
	{
		if (term.Length == 0 || term.Length > name.Length)
		{
			return null;
		}

		var wordIds = GetWordIds(name);

		// best[i, p]: lowest cost of term[i..] after placing term[i - 1] at p - 1 (stored +2; 0 = unknown, 1 = no match).
		var best = new int[term.Length, name.Length + 1];
		var firstWord = -1;
		var result = Match(0, 0);
		return result < 0 ? null : (result, firstWord);

		int Match(int termIndex, int position)
		{
			if (termIndex == term.Length)
			{
				return 0;
			}

			if (best[termIndex, position] != 0)
			{
				return best[termIndex, position] - 2;
			}

			var currentWord = termIndex > 0 ? wordIds[position - 1] : -1;
			var lowest = -1;
			for (var candidate = position; candidate < name.Length; candidate++)
			{
				var word = wordIds[candidate];
				if (word < 0 || char.ToUpperInvariant(name[candidate]) != char.ToUpperInvariant(term[termIndex]))
				{
					continue;
				}

				int step;
				if (termIndex > 0 && word == currentWord)
				{
					step = candidate == position ? 0 : 3;
				}
				else if (candidate == 0 || wordIds[candidate - 1] != word)
				{
					step = termIndex == 0 ? 2 : 2 * (word - currentWord);
				}
				else
				{
					continue;
				}

				var rest = Match(termIndex + 1, candidate + 1);
				if (rest >= 0 && (lowest < 0 || step + rest < lowest))
				{
					lowest = step + rest;
					if (termIndex == 0)
					{
						firstWord = word;
					}
				}
			}

			best[termIndex, position] = lowest + 2;
			return lowest;
		}
	}

	/// <summary>
	/// Assigns a word index to each character; separators get -1. Words split on separators,
	/// lower-to-upper case changes, acronym ends ("XAMLReader" is XAML + Reader) and letter/digit changes.
	/// </summary>
	private static int[] GetWordIds(string name)
	{
		var ids = new int[name.Length];
		var word = -1;
		for (var i = 0; i < name.Length; i++)
		{
			var c = name[i];
			if (!char.IsLetterOrDigit(c))
			{
				ids[i] = -1;
				continue;
			}

			var previous = i > 0 ? name[i - 1] : '\0';
			var next = i + 1 < name.Length ? name[i + 1] : '\0';
			var isBoundary = i == 0
				|| !char.IsLetterOrDigit(previous)
				|| (char.IsUpper(c) && char.IsLower(previous))
				|| (char.IsUpper(c) && char.IsUpper(previous) && char.IsLower(next))
				|| (char.IsDigit(c) != char.IsDigit(previous));

			if (isBoundary)
			{
				word++;
			}

			ids[i] = word;
		}

		return ids;
	}

	private static string Compact(string value) => new(value.Where(char.IsLetterOrDigit).ToArray());

	/// <summary>
	/// Finds the best occurrence of <paramref name="compactTerm"/> in the separator-free name like
	/// <see cref="GetWordStartCost"/>, plus a mid-word start penalty that is heavier for 1-2 characters,
	/// which are more likely abbreviations ("tb") than word fragments ("list" in Playlist).
	/// Returns null when there is none.
	/// </summary>
	private static (int Cost, int FirstWord)? GetContainsMatch(string compactTerm, string name)
	{
		var wordIds = GetWordIds(name);
		List<int> positions = new(name.Length);
		for (var i = 0; i < name.Length; i++)
		{
			if (wordIds[i] >= 0)
			{
				positions.Add(i);
			}
		}

		var compactName = Compact(name);
		var midWordPenalty = compactTerm.Length <= 2 ? 3 : 1;
		(int Cost, int FirstWord)? bestMatch = null;
		for (var index = compactName.IndexOf(compactTerm, StringComparison.OrdinalIgnoreCase);
			index >= 0;
			index = compactName.IndexOf(compactTerm, index + 1, StringComparison.OrdinalIgnoreCase))
		{
			var first = positions[index];
			var last = positions[index + compactTerm.Length - 1];
			var startsWord = first == 0 || wordIds[first - 1] != wordIds[first];
			var score = (wordIds[last] - wordIds[first] + 1) * 2 + (startsWord ? 0 : midWordPenalty);
			if (bestMatch is null || score < bestMatch.Value.Cost)
			{
				bestMatch = (score, wordIds[first]);
			}
		}

		return bestMatch;
	}

	private static (Tier Tier, int Score, int FirstWord)? GetTier(string term, string compactTerm, SampleChooserContent item)
	{
		var name = item.ControlName;

		if (compactTerm.Length > 0)
		{
			if (Compact(name).StartsWith(compactTerm, StringComparison.OrdinalIgnoreCase))
			{
				return (Tier.NameStartsWith, 0, 0);
			}

			if (Best(GetContainsMatch(compactTerm, name), GetWordStartMatch(compactTerm, name)) is { } nameMatch)
			{
				return (Tier.NameWords, nameMatch.Cost, nameMatch.FirstWord);
			}
		}

		if (item.Categories?.Any(c => c?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) == true)
		{
			return (Tier.CategoryContains, 0, 0);
		}

		if (item.ControlType?.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)
		{
			return (Tier.TypeNameContains, 0, 0);
		}

		return null;
	}

	private static (int Cost, int FirstWord)? Best((int Cost, int FirstWord)? a, (int Cost, int FirstWord)? b)
		=> a is null ? b : b is null || a.Value.CompareTo(b.Value) <= 0 ? a : b;
}

/// <summary>
/// A category group of search results, usable as a grouped <c>CollectionViewSource</c> source.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class SampleSearchGroup : List<SampleChooserContent>, IGrouping<string, SampleChooserContent>
{
	public SampleSearchGroup(string key) => Key = key;

	public string Key { get; }
}
