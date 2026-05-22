#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Xml;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Uno.ReferenceImplComparer
{
	partial class Program
	{
		static int Main(string[] args)
		{
			switch (args[0])
			{
				case "list-failed":
					return ListFailedTests(args[1], args[2]);
				case "fail-empty":
					return FailOnEmptyResults(args[1]);
				case "count-failed":
					return CountFailedTests(args[1]);
				case "merge-results":
					return MergeResults(args[1], args[2], args[3]);
			}

			return 0;
		}

		private static int FailOnEmptyResults(string inputFile)
		{
			var doc = new XmlDocument();
			doc.LoadXml(File.ReadAllText(inputFile));

			var allNodes = doc.SelectNodes("//test-case");

			var isEmpty = allNodes?.Count == 0;

			if (isEmpty)
			{
				Console.WriteLine($"The test results file {inputFile} does not contain any results");
			}
			else
			{
				Console.WriteLine($"The test results file {inputFile} contains {allNodes?.Count} results");
			}

			return isEmpty ? 1 : 0;
		}

		private static int ListFailedTests(string inputFile, string outputFile)
		{
			var doc = new XmlDocument();
			doc.LoadXml(File.ReadAllText(inputFile));

			var failedNodes = doc.SelectNodes("//test-case[@result='Failed']")!;

			var failedTests = new List<string>();
			foreach (var failedNode in failedNodes.OfType<XmlElement>())
			{
				var name = failedNode.GetAttribute("fullname");

				// This is used to remove the test parameters from the test name, which are not used by the nunit-console runner.
				var simpleName = SimpleNameRegex().Replace(name, "");

				failedTests.Add(simpleName);
			}

			// Add a dummy line to be used to rerun the test running in case
			// tests get canceled. This condition happens when running nunit-console
			// and the retry attribute which markes runners as cancelled and fails any
			// subsequent test.
			failedTests.Add("invalid-test-for-retry");

			File.WriteAllText(outputFile, string.Join(" | ", failedTests));

			Console.WriteLine($"Found {failedTests.Count} failed tests in {inputFile}.");

			return 0;
		}

		/// <summary>
		/// Prints the number of failed test-cases to stdout and exits with 0.
		/// Callers capture stdout: FAILED=$(dotnet run count-failed results.xml)
		/// </summary>
		private static int CountFailedTests(string inputFile)
		{
			var doc = new XmlDocument();
			doc.LoadXml(File.ReadAllText(inputFile));

			var count = doc.SelectNodes("//test-case[@result='Failed']")?.Count ?? 0;

			Console.WriteLine(count.ToString(CultureInfo.InvariantCulture));
			return 0;
		}

		/// <summary>
		/// Merges a rerun XML (subset of tests) into the original full XML.
		/// For each test in the rerun, its result in the original is replaced by
		/// the rerun outcome.  If the rerun made a previously-failed test pass,
		/// the failure details are removed and the test is marked retried="true".
		/// Summary statistics on the root test-run element are recalculated.
		/// </summary>
		private static int MergeResults(string originalFile, string rerunFile, string outputFile)
		{
			var originalDoc = new XmlDocument();
			originalDoc.Load(originalFile);

			var rerunDoc = new XmlDocument();
			rerunDoc.Load(rerunFile);

			var originalCases = new Dictionary<string, XmlElement>();
			foreach (XmlElement node in originalDoc.SelectNodes("//test-case")!)
			{
				var name = node.GetAttribute("fullname");
				if (!string.IsNullOrEmpty(name))
					originalCases.TryAdd(name, node);
			}

			var updatedCount = 0;

			foreach (XmlElement rerunCase in rerunDoc.SelectNodes("//test-case")!)
			{
				var fullName = rerunCase.GetAttribute("fullname");

				if (string.IsNullOrEmpty(fullName) || !originalCases.TryGetValue(fullName, out var originalCase))
					continue;

				var originalResult = originalCase.GetAttribute("result");
				var rerunResult = rerunCase.GetAttribute("result");

				originalCase.SetAttribute("result", rerunResult);
				originalCase.SetAttribute("retried", "true");
				originalCase.SetAttribute("original-result", originalResult);

				if (rerunCase.HasAttribute("duration"))
					originalCase.SetAttribute("duration", rerunCase.GetAttribute("duration"));

				if (rerunResult == "Passed")
				{
					// Remove failure details so Azure DevOps does not report the test as failed
					var failureNode = originalCase.SelectSingleNode("failure");
					if (failureNode != null)
						originalCase.RemoveChild(failureNode);
				}

				updatedCount++;
			}

			RecalculateTestRunTotals(originalDoc);

			originalDoc.Save(outputFile);
			Console.WriteLine($"Merged {updatedCount} test result(s) from rerun. Output: {outputFile}");
			return 0;
		}

		private static void RecalculateTestRunTotals(XmlDocument doc)
		{
			var testRun = doc.SelectSingleNode("//test-run") as XmlElement;
			if (testRun is null)
				return;

			var passed = 0;
			var failed = 0;
			var skipped = 0;
			var inconclusive = 0;
			var total = 0;
			foreach (XmlElement node in doc.SelectNodes("//test-case")!)
			{
				total++;
				switch (node.GetAttribute("result"))
				{
					case "Passed": passed++; break;
					case "Failed": failed++; break;
					case "Skipped": skipped++; break;
					case "Inconclusive": inconclusive++; break;
				}
			}

			testRun.SetAttribute("total", total.ToString(CultureInfo.InvariantCulture));
			testRun.SetAttribute("passed", passed.ToString(CultureInfo.InvariantCulture));
			testRun.SetAttribute("failed", failed.ToString(CultureInfo.InvariantCulture));
			testRun.SetAttribute("skipped", skipped.ToString(CultureInfo.InvariantCulture));
			testRun.SetAttribute("inconclusive", inconclusive.ToString(CultureInfo.InvariantCulture));
			testRun.SetAttribute("result", failed > 0 ? "Failed" : "Passed");
		}

		[GeneratedRegex(@"\(([^)]*)\)")]
		private static partial Regex SimpleNameRegex();
	}
}
