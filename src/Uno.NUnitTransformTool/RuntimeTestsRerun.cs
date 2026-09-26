#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Uno.NUnitTransformTool;

/// <summary>
/// Supports re-running the failed runtime tests once, in a fresh app process, within the same CI job.
/// </summary>
public static partial class RuntimeTestsRerun
{
	/// <summary>
	/// Marker appended to every failed-tests list, see <see cref="GetFailedTestsFilter"/>.
	/// </summary>
	public const string RetrySentinel = "invalid-test-for-retry";

	/// <summary>
	/// Gets the distinct names of the failed tests, with the test case arguments removed.
	/// </summary>
	public static List<string> GetFailedTestNames(XmlDocument results)
		=> GetFailedCases(results)
			// The runtime tests runner matches the filter against the method name, so the
			// arguments are stripped and the parameterized cases collapse onto one entry.
			.Select(testCase => SimpleNameRegex().Replace(testCase.GetAttribute("fullname"), ""))
			.Distinct()
			.ToList();

	/// <summary>
	/// Gets the filter consumed by the runtime tests runner through UITEST_RUNTIME_TESTS_FILTER.
	/// </summary>
	/// <remarks>
	/// The sentinel is a dummy entry used to rerun the tests in case they get canceled. This happens when
	/// running nunit-console and the retry attribute, which marks runners as cancelled and fails any
	/// subsequent test.
	/// </remarks>
	public static string GetFailedTestsFilter(IEnumerable<string> failedTests)
		=> string.Join(" | ", failedTests.Append(RetrySentinel));

	/// <summary>
	/// Replaces each failed test case of <paramref name="original"/> that passed in <paramref name="rerun"/>
	/// and updates the counters of the enclosing suites.
	/// </summary>
	public static RerunMergeResult Merge(XmlDocument original, XmlDocument rerun)
	{
		var rerunCasesByName = rerun
			.SelectNodes("//test-case")!
			.OfType<XmlElement>()
			.GroupBy(testCase => testCase.GetAttribute("fullname"))
			.ToDictionary(group => group.Key, group => new Queue<XmlElement>(group));

		var recovered = new List<string>();
		var stillFailing = new List<string>();

		// Materialized first, the loop replaces nodes of the document it walks.
		foreach (var failedCase in GetFailedCases(original).ToList())
		{
			var fullName = failedCase.GetAttribute("fullname");

			if (!rerunCasesByName.TryGetValue(fullName, out var rerunCases)
				|| !rerunCases.TryDequeue(out var rerunCase)
				|| rerunCase.GetAttribute("result") != "Passed")
			{
				// The first failure stays: its message is the one worth investigating.
				stillFailing.Add(fullName);
				continue;
			}

			var mergedCase = (XmlElement)original.ImportNode(rerunCase, deep: true);
			AddFirstAttemptFailure(mergedCase, failedCase);
			failedCase.ParentNode!.ReplaceChild(mergedCase, failedCase);

			for (var ancestor = mergedCase.ParentNode as XmlElement; ancestor is not null; ancestor = ancestor.ParentNode as XmlElement)
			{
				AddToCounter(ancestor, "failed", -1);
				AddToCounter(ancestor, "passed", 1);
			}

			recovered.Add(fullName);
		}

		if (recovered.Count > 0)
		{
			foreach (var suite in original.SelectNodes("//test-suite | //test-run")!.OfType<XmlElement>())
			{
				if (suite.GetAttribute("result") == "Failed"
					&& int.TryParse(suite.GetAttribute("failed"), out var failed)
					&& failed == 0)
				{
					suite.SetAttribute("result", "Passed");
				}
			}
		}

		return new(recovered, stillFailing);
	}

	private static IEnumerable<XmlElement> GetFailedCases(XmlDocument results)
		=> results.SelectNodes("//test-case[@result='Failed']")!.OfType<XmlElement>();

	private static void AddFirstAttemptFailure(XmlElement mergedCase, XmlElement failedCase)
	{
		var firstMessage = failedCase.SelectSingleNode("failure/message")?.InnerText;
		var note = new StringBuilder("Failed on the first attempt, passed when re-run in a fresh app process.");
		if (!string.IsNullOrWhiteSpace(firstMessage))
		{
			note.AppendLine().Append("First attempt failure: ").Append(firstMessage);
		}

		var output = mergedCase.SelectSingleNode("output");
		if (output is null)
		{
			output = mergedCase.OwnerDocument.CreateElement("output");
			mergedCase.AppendChild(output);
		}
		else
		{
			note.AppendLine().AppendLine().Append(output.InnerText);
		}

		output.InnerText = note.ToString();
	}

	private static void AddToCounter(XmlElement suite, string counter, int delta)
	{
		if (int.TryParse(suite.GetAttribute(counter), out var value))
		{
			suite.SetAttribute(counter, (value + delta).ToString(CultureInfo.InvariantCulture));
		}
	}

	[GeneratedRegex(@"\(([^)]*)\)")]
	private static partial Regex SimpleNameRegex();
}

public record RerunMergeResult(IReadOnlyList<string> Recovered, IReadOnlyList<string> StillFailing);
