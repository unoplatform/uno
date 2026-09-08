#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SamplesApp.AppiumTests.Infrastructure;

public sealed record SnapshotDiffEntry(string Path, string Kind, string Expected, string Actual)
{
	public override string ToString() => $"[{Kind}] {Path}: expected '{Expected}' actual '{Actual}'";
}

public sealed class SnapshotDiff
{
	public List<SnapshotDiffEntry> Entries { get; } = new();

	public bool IsMatch => Entries.Count == 0;

	public string Format()
	{
		if (IsMatch)
		{
			return "(no diff)";
		}

		var sb = new StringBuilder();
		sb.AppendLine($"{Entries.Count} difference(s):");
		foreach (var entry in Entries)
		{
			sb.AppendLine("  " + entry);
		}

		return sb.ToString();
	}
}

/// <summary>
/// Compares committed canonical snapshots by element id and reports explicit,
/// actionable semantic diffs.
/// </summary>
/// <remarks>
/// The raw per-platform attributes captured in <see cref="AccessibilityNode.Extras"/> are
/// deliberately kept out of the canonical snapshot and therefore out of every comparison:
/// they exist only to make a failing run debuggable through the diagnostic tree dump, and
/// they differ legitimately between hosts and driver versions. Gating snapshot equality on
/// them would produce failures that say nothing about accessibility semantics.
/// </remarks>
public static class SnapshotComparer
{
	public static SnapshotDiff Compare(AccessibilitySnapshot expected, AccessibilitySnapshot actual)
	{
		var diff = new SnapshotDiff();

		if (!string.Equals(expected.Sample, actual.Sample, System.StringComparison.Ordinal))
		{
			diff.Entries.Add(new SnapshotDiffEntry("sample", "changed", expected.Sample, actual.Sample));
		}

		if (!string.Equals(expected.Flavor, actual.Flavor, System.StringComparison.Ordinal))
		{
			diff.Entries.Add(new SnapshotDiffEntry("flavor", "changed", expected.Flavor, actual.Flavor));
		}

		var expectedById = expected.Elements.ToDictionary(element => element.Id, StringComparer.Ordinal);
		var actualById = actual.Elements.ToDictionary(element => element.Id, StringComparer.Ordinal);

		foreach (var missingId in expectedById.Keys.Except(actualById.Keys, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal))
		{
			diff.Entries.Add(new SnapshotDiffEntry(
				$"elements[{missingId}]",
				"removed",
				DescribeElement(expectedById[missingId]),
				"(absent)"));
		}

		foreach (var addedId in actualById.Keys.Except(expectedById.Keys, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal))
		{
			diff.Entries.Add(new SnapshotDiffEntry(
				$"elements[{addedId}]",
				"added",
				"(absent)",
				DescribeElement(actualById[addedId])));
		}

		foreach (var sharedId in expectedById.Keys.Intersect(actualById.Keys, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal))
		{
			CompareElement(expectedById[sharedId], actualById[sharedId], $"elements[{sharedId}]", diff);
		}

		return diff;
	}

	private static void CompareElement(
		AccessibilityElementSnapshot expected,
		AccessibilityElementSnapshot actual,
		string path,
		SnapshotDiff diff)
	{
		CompareValue(expected.Role, actual.Role, $"{path}.role", diff);
		CompareValue(expected.Name, actual.Name, $"{path}.name", diff);
		CompareValue(expected.AutomationId, actual.AutomationId, $"{path}.automation_id", diff);
		CompareValue(expected.Description, actual.Description, $"{path}.description", diff);
		CompareValue(expected.Value, actual.Value, $"{path}.value", diff);
		ComparePatterns(expected.Patterns, actual.Patterns, $"{path}.patterns", diff);

		CompareValue(expected.State.Enabled, actual.State.Enabled, $"{path}.state.enabled", diff);
		CompareValue(expected.State.KeyboardFocusable, actual.State.KeyboardFocusable, $"{path}.state.keyboard_focusable", diff);
		CompareValue(expected.State.Focused, actual.State.Focused, $"{path}.state.focused", diff);
		CompareValue(expected.State.Offscreen, actual.State.Offscreen, $"{path}.state.offscreen", diff);
		CompareValue(expected.State.ToggleState, actual.State.ToggleState, $"{path}.state.toggle_state", diff);
		CompareValue(expected.State.Selected, actual.State.Selected, $"{path}.state.selected", diff);
		CompareValue(expected.State.Expanded, actual.State.Expanded, $"{path}.state.expanded", diff);
		CompareValue(expected.State.Required, actual.State.Required, $"{path}.state.required", diff);
		CompareValue(expected.State.Level, actual.State.Level, $"{path}.state.level", diff);
		CompareValue(expected.State.Landmark, actual.State.Landmark, $"{path}.state.landmark", diff);
		CompareValue(expected.State.RoleDescription, actual.State.RoleDescription, $"{path}.state.role_description", diff);
		CompareValue(expected.State.LiveSetting, actual.State.LiveSetting, $"{path}.state.live_setting", diff);
	}

	private static void ComparePatterns(List<string> expected, List<string> actual, string path, SnapshotDiff diff)
	{
		var expectedSet = new SortedSet<string>(expected, StringComparer.Ordinal);
		var actualSet = new SortedSet<string>(actual, StringComparer.Ordinal);

		foreach (var lost in expectedSet.Except(actualSet))
		{
			diff.Entries.Add(new SnapshotDiffEntry(path, "pattern-lost", lost, "(absent)"));
		}

		foreach (var added in actualSet.Except(expectedSet))
		{
			diff.Entries.Add(new SnapshotDiffEntry(path, "pattern-added", "(absent)", added));
		}
	}

	private static void CompareValue<T>(T expected, T actual, string path, SnapshotDiff diff)
	{
		if (!EqualityComparer<T>.Default.Equals(expected, actual))
		{
			diff.Entries.Add(new SnapshotDiffEntry(path, "changed", Format(expected), Format(actual)));
		}
	}

	private static string DescribeElement(AccessibilityElementSnapshot snapshot)
	{
		var name = string.IsNullOrEmpty(snapshot.Name) ? string.Empty : $" \"{snapshot.Name}\"";
		return $"{snapshot.Role} #{snapshot.AutomationId}{name}";
	}

	private static string Format<T>(T value)
		=> value?.ToString() ?? "(null)";
}
