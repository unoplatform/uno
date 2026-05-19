#nullable enable

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
/// Pairs nodes by tree position (depth-first, by child index) and reports
/// every field that differs. Does not attempt fuzzy alignment - a single
/// inserted or removed child cascades into a count mismatch for the
/// parent plus per-position mismatches for the rest of the children.
/// That keeps the diff explicit; legit reshuffles look like big diffs
/// and force a deliberate baseline update.
/// </summary>
public static class SnapshotComparer
{
	public static SnapshotDiff Compare(AccessibilityNode expected, AccessibilityNode actual)
	{
		var diff = new SnapshotDiff();
		CompareNode(expected, actual, path: "root", diff);
		return diff;
	}

	private static void CompareNode(AccessibilityNode expected, AccessibilityNode actual, string path, SnapshotDiff diff)
	{
		if (!string.Equals(expected.Role, actual.Role, System.StringComparison.Ordinal))
		{
			diff.Entries.Add(new SnapshotDiffEntry($"{path}.role", "changed", expected.Role, actual.Role));
		}

		if (!string.Equals(expected.Name, actual.Name, System.StringComparison.Ordinal))
		{
			diff.Entries.Add(new SnapshotDiffEntry($"{path}.name", "changed", expected.Name, actual.Name));
		}

		if (!string.Equals(expected.AutomationId, actual.AutomationId, System.StringComparison.Ordinal))
		{
			diff.Entries.Add(new SnapshotDiffEntry($"{path}.automation_id", "changed", expected.AutomationId, actual.AutomationId));
		}

		if (!string.Equals(expected.Value ?? string.Empty, actual.Value ?? string.Empty, System.StringComparison.Ordinal))
		{
			diff.Entries.Add(new SnapshotDiffEntry($"{path}.value", "changed", expected.Value ?? "(null)", actual.Value ?? "(null)"));
		}

		ComparePatterns(expected.Patterns, actual.Patterns, $"{path}.patterns", diff);
		CompareChildren(expected.Children, actual.Children, path, diff);
	}

	private static void ComparePatterns(List<string> expected, List<string> actual, string path, SnapshotDiff diff)
	{
		var expectedSet = new SortedSet<string>(expected, System.StringComparer.Ordinal);
		var actualSet = new SortedSet<string>(actual, System.StringComparer.Ordinal);

		foreach (var lost in expectedSet.Except(actualSet))
		{
			diff.Entries.Add(new SnapshotDiffEntry(path, "pattern-lost", lost, "(absent)"));
		}

		foreach (var added in actualSet.Except(expectedSet))
		{
			diff.Entries.Add(new SnapshotDiffEntry(path, "pattern-added", "(absent)", added));
		}
	}

	private static void CompareChildren(List<AccessibilityNode> expected, List<AccessibilityNode> actual, string path, SnapshotDiff diff)
	{
		if (expected.Count != actual.Count)
		{
			diff.Entries.Add(new SnapshotDiffEntry(
				$"{path}.children.count",
				"changed",
				expected.Count.ToString(),
				actual.Count.ToString()));
		}

		var pairCount = System.Math.Min(expected.Count, actual.Count);
		for (var i = 0; i < pairCount; i++)
		{
			CompareNode(expected[i], actual[i], $"{path}.children[{i}]", diff);
		}

		for (var i = pairCount; i < expected.Count; i++)
		{
			diff.Entries.Add(new SnapshotDiffEntry(
				$"{path}.children[{i}]",
				"removed",
				DescribeNode(expected[i]),
				"(absent)"));
		}

		for (var i = pairCount; i < actual.Count; i++)
		{
			diff.Entries.Add(new SnapshotDiffEntry(
				$"{path}.children[{i}]",
				"added",
				"(absent)",
				DescribeNode(actual[i])));
		}
	}

	private static string DescribeNode(AccessibilityNode node)
	{
		var id = string.IsNullOrEmpty(node.AutomationId) ? string.Empty : $" #{node.AutomationId}";
		var name = string.IsNullOrEmpty(node.Name) ? string.Empty : $" \"{node.Name}\"";
		return $"{node.Role}{id}{name}";
	}
}
