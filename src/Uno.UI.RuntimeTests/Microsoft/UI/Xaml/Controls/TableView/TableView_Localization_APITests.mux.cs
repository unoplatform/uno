// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Localization_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;


using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewLocalizationTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Most automation strings fall back to hard-coded English, so a peer cannot reveal a failed lookup
// (#12070); these tests read the resource map directly. The header sort-state help text has no
// fallback, so it is the one readback that exercises the control's own lookup.
[TestClass]
public class TableViewLocalizationTests : TableViewApiTestBase
{
	#region 17.1 The resource map resolves every string the control asks for

	[TestMethod]
	[TestProperty("Ignore", "True")] // Fails on VM: TableViewCellBlankName unresolved / second Resources subtree; tracked follow-up
	[Ignore("WinUI ignores this test: Fails on VM: TableViewCellBlankName unresolved / second Resources subtree; tracked follow-up")]
	[TestProperty("Description", "Verifies every SR_TableView* resource the control references is present in the resource map and resolves to a non-empty string.")]
	public void VerifyEveryTableViewResourceKeyResolves()
	{
		RunOnUIThread.Execute(() =>
		{
			var map = RequireTableViewResourceMap();
			var unresolved = new List<string>();

			foreach (var key in AllResourceKeys)
			{
				var value = TryResolve(map, key);

				if (string.IsNullOrWhiteSpace(value))
				{
					unresolved.Add(key);
				}
				else
				{
					Log.Comment($"{key} = '{value}'");
				}
			}

			Verify.AreEqual(0, unresolved.Count,
				$"Every TableView resource must resolve. Unresolved: {string.Join(", ", unresolved)}.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies no TableView resource resolves to its own key name, which is how a missing string surfaces as a plausible-looking value.")]
	[Ignore("Uno does not implement ResourceManager.MainResourceMap, which these tests enumerate.")]
	public void VerifyNoResourceResolvesToItsOwnKeyName()
	{
		RunOnUIThread.Execute(() =>
		{
			var map = RequireTableViewResourceMap();

			foreach (var key in AllResourceKeys)
			{
				var value = TryResolve(map, key);

				Verify.AreNotEqual(key, value,
					$"{key} resolved to its own key name, which means the lookup failed rather than succeeded.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Ignore", "True")] // Fails on VM: TableViewCellBlankName unresolved / second Resources subtree; tracked follow-up
	[Ignore("WinUI ignores this test: Fails on VM: TableViewCellBlankName unresolved / second Resources subtree; tracked follow-up")]
	[TestProperty("Description", "Verifies the control's resources live under the single resource subtree the control reads rather than being split across maps.")]
	public void VerifyAllResourcesShareOneSubtree()
	{
		RunOnUIThread.Execute(() =>
		{
			var subtrees = AllResourceKeys
				.Select(key => SubtreeOf(FindResourcePath(key)))
				.Where(path => path != null)
				.Distinct()
				.ToList();

			Log.Comment($"Subtrees in use: {string.Join(", ", subtrees)}");

			Verify.AreEqual(1, subtrees.Count,
				$"All TableView resources must share one subtree; found {subtrees.Count}: {string.Join(", ", subtrees)}.");
			Verify.IsTrue(string.Equals(ProductResourceSubtree, subtrees[0], StringComparison.OrdinalIgnoreCase),
				$"TableView resources must live in the subtree the control reads, '{ProductResourceSubtree}'; found '{subtrees[0]}'.");
		});
	}

	[TestMethod]
	[TestProperty("Ignore", "True")] // Fails on VM: TableViewCellBlankName unresolved / second Resources subtree; tracked follow-up
	[Ignore("WinUI ignores this test: Fails on VM: TableViewCellBlankName unresolved / second Resources subtree; tracked follow-up")]
	[TestProperty("Description", "Verifies AllResourceKeys lists exactly the TableView* resources in the control's resource subtree, so a newly added string cannot go uncovered.")]
	public void VerifyAllResourceKeysMatchesTheResourceMap()
	{
		RunOnUIThread.Execute(() =>
		{
			RequireTableViewResourceMap();
			var inMap = EnumerateTableViewResourceKeys();
			Log.Comment($"TableView* keys in the resource map: {string.Join(", ", inMap)}");

			var unlisted = inMap.Except(AllResourceKeys, StringComparer.Ordinal).ToList();
			var absent = AllResourceKeys.Except(inMap, StringComparer.Ordinal).ToList();

			Verify.AreEqual(0, unlisted.Count,
				$"Resources missing from AllResourceKeys (add them, and to FormatResources if they take arguments): {string.Join(", ", unlisted)}.");
			Verify.AreEqual(0, absent.Count,
				$"AllResourceKeys entries with no resource in the map: {string.Join(", ", absent)}.");
		});
	}

	#endregion

	#region 17.2 Localized values exist for the locales the control ships

	[TestMethod]
	[TestProperty("Description", "Verifies every TableView resource resolves in each shipped locale rather than silently falling back to English.")]
	[Ignore("Uno does not implement ResourceManager.MainResourceMap, which these tests enumerate.")]
	public void VerifyEveryResourceResolvesInEveryShippedLocale()
	{
		RunOnUIThread.Execute(() =>
		{
			var map = RequireTableViewResourceMap();
			var gaps = new List<string>();
			var resolvedSomewhere = 0;

			foreach (var language in RepresentativeLocales)
			{
				foreach (var key in AllResourceKeys)
				{
					if (string.IsNullOrWhiteSpace(TryResolveForLanguage(map, key, language)))
					{
						gaps.Add($"{language}/{key}");
					}
					else
					{
						resolvedSomewhere++;
					}
				}
			}

			foreach (var gap in gaps)
			{
				Log.Comment($"Unlocalized: {gap}");
			}

			// A payload with no localized resources at all is reported as such, not as N missing strings.
			Verify.IsGreaterThan(resolvedSomewhere, 0,
				"No TableView resource resolved in any non-default language. The test payload appears to carry no localized resources at all, so per-locale coverage cannot be judged.");

			// Known gap: six automation strings exist only in en-US. Every gap must be on the allow-list, and
			// allow-list entries that now resolve are reported so the list can be tightened.
			var unexpected = gaps.Where(gap => !KnownUnlocalized.Contains(gap)).ToList();

			Verify.AreEqual(0, unexpected.Count,
				$"TableView strings unlocalized beyond the known gaps: {string.Join(", ", unexpected)}.");

			foreach (var translated in KnownUnlocalized.Where(entry => !gaps.Contains(entry, StringComparer.OrdinalIgnoreCase)))
			{
				Log.Comment($"{translated} now resolves; remove it from KnownUnlocalizedKeysByLocale / KnownEmptyLocales.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies no shipped locale resolves zero TableView resources, which is how an entirely untranslated locale file goes unnoticed.")]
	[Ignore("Uno does not implement ResourceManager.MainResourceMap, which these tests enumerate.")]
	public void VerifyNoShippedLocaleIsCompletelyUnlocalized()
	{
		RunOnUIThread.Execute(() =>
		{
			var map = RequireTableViewResourceMap();
			var empty = new List<string>();
			var anyResolved = false;

			foreach (var language in RepresentativeLocales)
			{
				var resolved = AllResourceKeys.Count(key => !string.IsNullOrWhiteSpace(TryResolveForLanguage(map, key, language)));
				Log.Comment($"{language}: {resolved}/{AllResourceKeys.Length} resources resolved.");

				anyResolved |= resolved > 0;

				if (resolved == 0)
				{
					empty.Add(language);
				}
			}

			Verify.IsTrue(anyResolved,
				"No TableView resource resolved in any non-default language, so an empty locale cannot be distinguished from an unlocalized payload.");

			var unexpected = empty.Except(KnownEmptyLocales, StringComparer.OrdinalIgnoreCase).ToList();

			Verify.AreEqual(0, unexpected.Count,
				$"No shipped locale may be entirely unlocalized. Unexpectedly empty: {string.Join(", ", unexpected)}.");

			foreach (var fixedUp in KnownEmptyLocales.Except(empty, StringComparer.OrdinalIgnoreCase))
			{
				Log.Comment($"{fixedUp} is no longer empty; remove it from KnownEmptyLocales.");
			}
		});
	}

	#endregion

	#region 17.3 Format strings survive translation

	[TestMethod]
	[TestProperty("Description", "Verifies every FormatString-style resource keeps exactly the placeholder indices the product substitutes into it.")]
	[Ignore("Uno does not implement ResourceManager.MainResourceMap, which these tests enumerate.")]
	public void VerifyFormatStringPlaceholderArityIsPreserved()
	{
		RunOnUIThread.Execute(() =>
		{
			var map = RequireTableViewResourceMap();

			foreach (var format in FormatResources)
			{
				var value = TryResolve(map, format.Key);
				Verify.IsFalse(string.IsNullOrWhiteSpace(value), $"{format.Key} must resolve before its arity can be checked.");

				var actual = FormatIndices(PlaceholderIndices(value, format.Style));
				var expected = FormatIndices(format.ExpectedIndices);
				Log.Comment($"{format.Key} = '{value}' -> placeholders {actual}, expected {expected}.");

				Verify.AreEqual(expected, actual,
					$"{format.Key} must keep exactly the placeholders {expected}; resolved value was '{value}'.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies translated format strings keep exactly their placeholder indices in every shipped locale.")]
	[Ignore("Uno does not implement ResourceManager.MainResourceMap, which these tests enumerate.")]
	public void VerifyFormatStringPlaceholderArityIsPreservedAcrossLocales()
	{
		RunOnUIThread.Execute(() =>
		{
			var map = RequireTableViewResourceMap();

			foreach (var language in RepresentativeLocales)
			{
				foreach (var format in FormatResources)
				{
					var value = TryResolveForLanguage(map, format.Key, language);

					if (string.IsNullOrWhiteSpace(value))
					{
						continue;
					}

					var actual = FormatIndices(PlaceholderIndices(value, format.Style));
					var expected = FormatIndices(format.ExpectedIndices);

					Verify.AreEqual(expected, actual,
						$"{language}/{format.Key} must keep exactly the placeholders {expected}; resolved value was '{value}'.");
				}
			}
		});
	}

	#endregion

	#region 17.4 What the control announces comes from the resource, not the fallback

	[TestMethod]
	[TestProperty("Description", "Verifies a cell peer builds its name from the resolved CellNameFormat resource rather than from the hard-coded English fallback.")]
	[Ignore("Uno does not implement ResourceManager.MainResourceMap, which these tests enumerate.")]
	public async Task VerifyCellPeerNameIsBuiltFromTheResolvedFormatResource()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeItems();
			tableView = CreateTableView(items, headers: new[] { "Name", "Role" });
			// Only a cell whose column header is hidden composes its name with CellNameFormat.
			tableView.HeadersVisibility = TableViewHeadersVisibility.None;
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var map = RequireTableViewResourceMap();
			var format = TryResolve(map, "TableViewCellNameFormat");
			Verify.IsFalse(string.IsNullOrWhiteSpace(format), "CellNameFormat must resolve for this test to mean anything.");

			var peer = GetCellPeer(tableView, items[0], 0);
			Verify.IsNotNull(peer, "The fixture must produce a cell peer for the first cell.");

			var name = peer.GetName();
			Log.Comment($"Cell automation name: '{name}'; format resource: '{format}'.");

			Verify.IsFalse(string.IsNullOrWhiteSpace(name), "A realized cell must expose an automation name.");

			// Checking the resolved separator, not just a non-empty name, is what tells the resource from the fallback.
			var separator = SeparatorOf(format);
			if (!string.IsNullOrEmpty(separator))
			{
				Verify.IsTrue(name.Contains(separator),
					$"The cell name '{name}' must be composed with the separator '{separator}' carried by the resolved CellNameFormat.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a group header peer builds its name from the resolved GroupHeaderNameFormat resource rather than from the hard-coded English fallback.")]
	[Ignore("Uno does not implement ResourceManager.MainResourceMap, which these tests enumerate.")]
	public async Task VerifyGroupHeaderNameIsBuiltFromTheResolvedFormatResource()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = TableViewGroupingTestHelpers.CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var map = RequireTableViewResourceMap();
			var nameFormat = TryResolve(map, "TableViewGroupHeaderNameFormat");
			var countFormat = TryResolve(map, "TableViewGroupHeaderCountFormat");

			Verify.IsFalse(string.IsNullOrWhiteSpace(nameFormat), "GroupHeaderNameFormat must resolve.");
			Verify.IsFalse(string.IsNullOrWhiteSpace(countFormat), "GroupHeaderCountFormat must resolve.");

			var peer = GetGroupHeaderPeer(tableView, 0);
			Verify.IsNotNull(peer, "The grouped fixture must produce a group header peer.");

			var name = peer.GetName();
			Log.Comment($"Group header automation name: '{name}'; name format: '{nameFormat}'; count format: '{countFormat}'.");

			Verify.IsFalse(string.IsNullOrWhiteSpace(name), "A realized group header must expose an automation name.");

			var framing = LiteralFramingOf(countFormat);
			if (!string.IsNullOrEmpty(framing))
			{
				Verify.IsTrue(name.Contains(framing),
					$"The group header name '{name}' must carry the framing '{framing}' from the resolved GroupHeaderCountFormat.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the header sort-state help text the control reads through its own resource lookup equals the resource in the control's subtree; a lookup in the wrong subtree yields an empty help text.")]
	[Ignore("Uno does not implement ResourceManager.MainResourceMap, which these tests enumerate.")]
	public async Task VerifyColumnHeaderSortHelpTextComesFromTheControlsOwnLookup()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(MakeItems(), headers: new[] { "Name", "Role" });
			tableView.CanUserSortColumns = true;
			tableView.Columns[0].CanSort = true;
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var map = RequireTableViewResourceMap();
			var column = tableView.Columns[0];

			foreach (var (direction, key) in new[]
			{
				(SortDirection.None, "TableViewSortNoneHelpText"),
				(SortDirection.Ascending, "TableViewSortAscendingHelpText"),
				(SortDirection.Descending, "TableViewSortDescendingHelpText"),
			})
			{
				var expected = TryResolve(map, key);
				Verify.IsFalse(string.IsNullOrWhiteSpace(expected), $"{key} must resolve for this test to mean anything.");

				tableView.SortByColumn(column, direction);
				tableView.UpdateLayout();

				var actual = new TableViewColumnHeaderAutomationPeer(tableView, column).GetHelpText();
				Log.Comment($"SortDirection.{direction}: help text '{actual}', {key} '{expected}'.");

				Verify.AreEqual(expected, actual,
					$"The header help text for SortDirection.{direction} must be the control's own lookup of {key}.");
			}
		});
	}

	#endregion
}

internal static class TableViewLocalizationTestHelpers
{
	// The subtree the control reads its strings from: ResourceAccessor::c_resourceLoc, built from
	// MUXTABULARROOT_NAMESPACE_STR in controls/dev/inc/BuildMacros.h. Keep the two in sync.
	internal const string ProductResourceSubtree = "Microsoft.UI.Xaml.Controls.Tabular/Resources";

	internal static readonly string[] AllResourceKeys =
	{
		"TableViewBooleanFalse",
		"TableViewBooleanTrue",
		"TableViewCellBlankName",
		"TableViewCellLocalizedControlType",
		"TableViewCellNameFormat",
		"TableViewCellTextSeparator",
		"TableViewColumnHeaderHelpTextFormat",
		"TableViewColumnWidthChanged",
		"TableViewGroupHeaderCountFormat",
		"TableViewGroupHeaderFallback",
		"TableViewGroupHeaderNameFormat",
		"TableViewGroupHeaderNull",
		"TableViewSortAscendingHelpText",
		"TableViewSortCleared",
		"TableViewSortClearedAll",
		"TableViewSortDescendingHelpText",
		"TableViewSortNoneHelpText",
		"TableViewSortedAscending",
		"TableViewSortedDescending",
	};

	internal static readonly string[] RepresentativeLocales =
	{
		"fr-FR",
		"de-DE",
		"ja-JP",
		"ar-SA",
		"he-IL",
		"ru-RU",
		"zh-CN",
		"mi-NZ",
	};

	// Per locale, the keys allowed to fall back to en-US. Remove entries as translations land.
	private static readonly string[] EnglishOnlyKeys =
	{
		"TableViewBooleanFalse",
		"TableViewBooleanTrue",
		"TableViewCellBlankName",
		"TableViewCellLocalizedControlType",
		"TableViewCellNameFormat",
		"TableViewCellTextSeparator",
		"TableViewGroupHeaderNameFormat",
	};

	internal static readonly Dictionary<string, string[]> KnownUnlocalizedKeysByLocale = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
	{
		["fr-FR"] = EnglishOnlyKeys,
		["de-DE"] = EnglishOnlyKeys,
		["ja-JP"] = EnglishOnlyKeys,
		["ar-SA"] = EnglishOnlyKeys,
		["he-IL"] = EnglishOnlyKeys,
		["ru-RU"] = EnglishOnlyKeys,
		["zh-CN"] = EnglishOnlyKeys,
		["mi-NZ"] = EnglishOnlyKeys,
	};

	// Locales allowed to resolve no TableView string at all.
	internal static readonly string[] KnownEmptyLocales = { };

	// "locale/key" pairs allowed to be unlocalized.
	internal static readonly HashSet<string> KnownUnlocalized = new HashSet<string>(
		KnownUnlocalizedKeysByLocale.SelectMany(entry => entry.Value.Select(key => $"{entry.Key}/{key}"))
			.Concat(KnownEmptyLocales.SelectMany(locale => AllResourceKeys.Select(key => $"{locale}/{key}"))),
		StringComparer.OrdinalIgnoreCase);

	internal enum PlaceholderStyle
	{
		// %1!s!, numbered from 1.
		FormatMessage,

		// {0}, numbered from 0.
		DotNet,
	}

	internal struct FormatResource
	{
		public string Key;
		public int[] ExpectedIndices;
		public PlaceholderStyle Style;
	}

	internal static readonly FormatResource[] FormatResources =
	{
		new FormatResource { Key = "TableViewCellNameFormat", ExpectedIndices = new[] { 1, 2 }, Style = PlaceholderStyle.FormatMessage },
		new FormatResource { Key = "TableViewGroupHeaderNameFormat", ExpectedIndices = new[] { 1, 2 }, Style = PlaceholderStyle.FormatMessage },
		new FormatResource { Key = "TableViewColumnHeaderHelpTextFormat", ExpectedIndices = new[] { 1, 2 }, Style = PlaceholderStyle.FormatMessage },
		new FormatResource { Key = "TableViewColumnWidthChanged", ExpectedIndices = new[] { 1, 2 }, Style = PlaceholderStyle.FormatMessage },
		new FormatResource { Key = "TableViewSortedAscending", ExpectedIndices = new[] { 1 }, Style = PlaceholderStyle.FormatMessage },
		new FormatResource { Key = "TableViewSortedDescending", ExpectedIndices = new[] { 1 }, Style = PlaceholderStyle.FormatMessage },
		new FormatResource { Key = "TableViewSortCleared", ExpectedIndices = new[] { 1 }, Style = PlaceholderStyle.FormatMessage },
		new FormatResource { Key = "TableViewGroupHeaderCountFormat", ExpectedIndices = new[] { 0 }, Style = PlaceholderStyle.DotNet },
	};

	private static Microsoft.Windows.ApplicationModel.Resources.ResourceManager s_manager;
	private static Dictionary<string, string> s_resourcePathsByKey;

	private static Microsoft.Windows.ApplicationModel.Resources.ResourceManager Manager
		=> s_manager ?? (s_manager = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager());

	// Scanned by leaf name so a string placed under the wrong subtree is still found and reported.
	private static Dictionary<string, string> ResourcePathsByKey()
	{
		if (s_resourcePathsByKey != null)
		{
			return s_resourcePathsByKey;
		}

		var paths = new Dictionary<string, string>();
		var main = Manager.MainResourceMap;
		var wanted = new HashSet<string>(AllResourceKeys, StringComparer.Ordinal);

		for (uint i = 0; i < main.ResourceCount; i++)
		{
			var entry = main.GetValueByIndex(i);
			var path = entry.Key;

			if (string.IsNullOrEmpty(path))
			{
				continue;
			}

			var leaf = LeafOf(path);

			if (wanted.Contains(leaf) && !paths.ContainsKey(leaf))
			{
				paths[leaf] = path;
			}
		}

		s_resourcePathsByKey = paths;
		return paths;
	}

	internal static string FindResourcePath(string key)
		=> ResourcePathsByKey().TryGetValue(key, out var path) ? path : null;

	// Every TableView* leaf under the subtree the control reads.
	internal static List<string> EnumerateTableViewResourceKeys()
	{
		var keys = new SortedSet<string>(StringComparer.Ordinal);
		var main = Manager.MainResourceMap;

		for (uint i = 0; i < main.ResourceCount; i++)
		{
			var path = main.GetValueByIndex(i).Key;

			if (!string.IsNullOrEmpty(path) && string.Equals(SubtreeOf(path), ProductResourceSubtree, StringComparison.OrdinalIgnoreCase))
			{
				var leaf = LeafOf(path);

				if (leaf.StartsWith("TableView", StringComparison.Ordinal))
				{
					keys.Add(leaf);
				}
			}
		}

		return keys.ToList();
	}

	internal static Microsoft.Windows.ApplicationModel.Resources.ResourceMap RequireTableViewResourceMap()
	{
		var main = Manager.MainResourceMap;
		var found = ResourcePathsByKey();

		Verify.IsGreaterThan(found.Count, 0,
			$"No TableView resource was found anywhere in the resource map ({main.ResourceCount} resources scanned). " +
			"This is the #12070 failure mode: the control's resource subtree is not resolvable at runtime.");

		// Resolve the way the control does, so strings found under any other subtree do not count.
		try
		{
			return main.GetSubtree(ProductResourceSubtree);
		}
		catch (Exception e)
		{
			Verify.Fail($"The control's resource subtree '{ProductResourceSubtree}' is not in the resource map: {e.Message}");
			return null;
		}
	}

	internal static string TryResolve(Microsoft.Windows.ApplicationModel.Resources.ResourceMap map, string key)
	{
		try
		{
			return map.GetValue(key).ValueAsString;
		}
		catch (Exception e)
		{
			Log.Comment($"Resolving {key} threw: {e.Message}");
			return null;
		}
	}

	internal static string TryResolveForLanguage(
		Microsoft.Windows.ApplicationModel.Resources.ResourceMap map,
		string key,
		string language)
	{
		try
		{
			var context = Manager.CreateResourceContext();
			context.QualifierValues["Language"] = language;

			var candidate = map.GetValue(key, context);

			if (!ResolvedForLanguage(candidate, language))
			{
				return null;
			}

			return candidate.ValueAsString;
		}
		catch (Exception e)
		{
			Log.Comment($"Resolving {key} for {language} threw: {e.Message}");
			return null;
		}
	}

	private static bool ResolvedForLanguage(
		Microsoft.Windows.ApplicationModel.Resources.ResourceCandidate candidate,
		string language)
	{
		if (candidate == null)
		{
			return false;
		}

		try
		{
			var qualifiers = candidate.QualifierValues;

			if (qualifiers == null || !qualifiers.TryGetValue("Language", out var resolved) || string.IsNullOrEmpty(resolved))
			{
				// A language-neutral candidate is the default-language fallback; treat it as untranslated.
				return false;
			}

			return SameLanguage(language, resolved);
		}
		catch
		{
			return false;
		}
	}

	// Compares BCP-47 tags by language, script and region rather than by prefix, so "zh-CN" matches a "zh-Hans"
	// candidate but not "zh-TW", and "ar-SA" does not match "arn". A region on only one side matches any region.
	internal static bool SameLanguage(string requested, string resolved)
	{
		ParseTag(requested, out var requestedLanguage, out var requestedScript, out var requestedRegion);
		ParseTag(resolved, out var resolvedLanguage, out var resolvedScript, out var resolvedRegion);

		return string.Equals(requestedLanguage, resolvedLanguage, StringComparison.OrdinalIgnoreCase)
			&& (requestedScript == null || resolvedScript == null || string.Equals(requestedScript, resolvedScript, StringComparison.OrdinalIgnoreCase))
			&& (requestedRegion == null || resolvedRegion == null || string.Equals(requestedRegion, resolvedRegion, StringComparison.OrdinalIgnoreCase));
	}

	private static void ParseTag(string tag, out string language, out string script, out string region)
	{
		var parts = tag.Split('-', '_');
		language = parts[0];
		script = null;
		region = null;

		foreach (var part in parts.Skip(1))
		{
			if (script == null && region == null && part.Length == 4 && part.All(char.IsLetter))
			{
				script = part;
			}
			else if (region == null && ((part.Length == 2 && part.All(char.IsLetter)) || (part.Length == 3 && part.All(char.IsDigit))))
			{
				region = part;
			}
		}

		// Chinese tags often omit the script; infer it from the region so zh-CN and zh-Hans compare equal.
		if (script == null && region != null && string.Equals(language, "zh", StringComparison.OrdinalIgnoreCase))
		{
			script = (region.Equals("TW", StringComparison.OrdinalIgnoreCase)
				   || region.Equals("HK", StringComparison.OrdinalIgnoreCase)
				   || region.Equals("MO", StringComparison.OrdinalIgnoreCase)) ? "Hant" : "Hans";
		}
	}

	internal static string LeafOf(string resourcePath)
	{
		var slash = resourcePath.LastIndexOf('/');
		return slash < 0 ? resourcePath : resourcePath.Substring(slash + 1);
	}

	internal static string SubtreeOf(string resourcePath)
	{
		if (resourcePath == null)
		{
			return null;
		}

		var slash = resourcePath.LastIndexOf('/');
		return slash < 0 ? string.Empty : resourcePath.Substring(0, slash);
	}

	// The distinct placeholder indices in value, multi-digit included; escaped "%%" / "{{" are skipped.
	internal static SortedSet<int> PlaceholderIndices(string value, PlaceholderStyle style)
	{
		var indices = new SortedSet<int>();

		if (string.IsNullOrEmpty(value))
		{
			return indices;
		}

		var open = style == PlaceholderStyle.FormatMessage ? '%' : '{';

		for (var i = 0; i < value.Length; i++)
		{
			if (value[i] != open)
			{
				continue;
			}

			if (i + 1 < value.Length && value[i + 1] == open)
			{
				i++;
				continue;
			}

			var end = i + 1;
			var index = 0;

			while (end < value.Length && value[end] >= '0' && value[end] <= '9')
			{
				index = (index * 10) + (value[end] - '0');
				end++;
			}

			if (end == i + 1)
			{
				continue;
			}

			// A .NET item is "{index}", "{index,alignment}" or "{index:format}".
			if (style == PlaceholderStyle.DotNet && (end >= value.Length || (value[end] != '}' && value[end] != ',' && value[end] != ':')))
			{
				continue;
			}

			indices.Add(index);
			i = end - 1;
		}

		return indices;
	}

	internal static string FormatIndices(IEnumerable<int> indices)
		=> "{" + string.Join(",", indices.OrderBy(index => index)) + "}";

	internal static string SeparatorOf(string format)
	{
		if (string.IsNullOrEmpty(format))
		{
			return null;
		}

		var first = format.IndexOf("%1", StringComparison.Ordinal);
		var second = format.IndexOf("%2", StringComparison.Ordinal);

		if (first < 0 || second < 0 || second <= first)
		{
			return null;
		}

		var afterFirst = format.IndexOf('!', first);
		afterFirst = afterFirst < 0 ? first + 2 : format.IndexOf('!', afterFirst + 1) + 1;

		return afterFirst <= 0 || afterFirst >= second
			? null
			: format.Substring(afterFirst, second - afterFirst);
	}

	internal static string LiteralFramingOf(string format)
	{
		if (string.IsNullOrEmpty(format))
		{
			return null;
		}

		var brace = format.IndexOf('{');
		return brace <= 0 ? null : format.Substring(0, brace);
	}
}
