#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Uno.ReferenceImplComparer
{
	partial class Program
	{
		static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.Error.WriteLine("Usage: NUnitTransformTool <command> [args...]");
				Console.Error.WriteLine("Commands: list-failed, fail-empty, count-failed, merge-results, create-hung-result");
				return 1;
			}

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
				case "create-hung-result":
					// create-hung-result <testName> <outputFile> [hangSeconds]
					return CreateHungResult(args[1], args[2], args.Length > 3 ? int.Parse(args[3], CultureInfo.InvariantCulture) : 0);
				default:
					Console.Error.WriteLine($"Unknown command: {args[0]}");
					Console.Error.WriteLine("Commands: list-failed, fail-empty, count-failed, merge-results, create-hung-result");
					return 1;
			}
		}

		private static int FailOnEmptyResults(string inputFile)
		{
			var doc = new XmlDocument();
			doc.Load(inputFile);

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
			doc.Load(inputFile);

			var failedNodes = doc.SelectNodes("//test-case[@result='Failed']")!;

			var failedTests = new List<string>();
			foreach (var failedNode in failedNodes.OfType<XmlElement>())
			{
				var name = failedNode.GetAttribute("fullname");

				// Remove test parameters from the name — nunit-console uses bare names in filters.
				var simpleName = SimpleNameRegex().Replace(name, "");

				failedTests.Add(simpleName);
			}

			// Sentinel: ensures the filter expression is never empty even if the app cancelled
			// some tests. Without it, an empty filter would run the full suite on retry.
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
			doc.Load(inputFile);

			var count = doc.SelectNodes("//test-case[@result='Failed']")?.Count ?? 0;

			Console.WriteLine(count.ToString(CultureInfo.InvariantCulture));
			return 0;
		}

		/// <summary>
		/// Merges a rerun XML (subset of tests) into the original full XML.
		/// For each test in the rerun, its result in the original is replaced by
		/// the rerun outcome.  If the rerun made a previously-failed test pass,
		/// the failure details are removed and the test is marked retried="true".
		/// Summary statistics on the root test-run and all test-suite elements are recalculated.
		/// Parameter-suffixed names are normalised the same way list-failed normalises them so
		/// that rerun results are matched even when NUnit includes full parameter strings.
		/// </summary>
		private static int MergeResults(string originalFile, string rerunFile, string outputFile)
		{
			var originalDoc = new XmlDocument();
			originalDoc.Load(originalFile);

			var rerunDoc = new XmlDocument();
			rerunDoc.Load(rerunFile);

			// Build lookup using the same parameter-stripped key as ListFailedTests.
			var originalCases = new Dictionary<string, XmlElement>();
			foreach (XmlElement node in originalDoc.SelectNodes("//test-case")!)
			{
				var name = node.GetAttribute("fullname");
				if (string.IsNullOrEmpty(name))
					continue;

				var key = SimpleNameRegex().Replace(name, "");
				if (!originalCases.TryAdd(key, node))
					Console.Error.WriteLine($"##[warning]Duplicate test-case key '{key}' in original results — only first occurrence will be updated.");
			}

			var updatedCount = 0;

			foreach (XmlElement rerunCase in rerunDoc.SelectNodes("//test-case")!)
			{
				var fullName = rerunCase.GetAttribute("fullname");
				if (string.IsNullOrEmpty(fullName))
					continue;

				var key = SimpleNameRegex().Replace(fullName, "");
				if (!originalCases.TryGetValue(key, out var originalCase))
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

			// Write atomically so a partial write never corrupts the original.
			var tmpFile = outputFile + ".tmp";
			originalDoc.Save(tmpFile);
			File.Move(tmpFile, outputFile, overwrite: true);

			Console.WriteLine($"Merged {updatedCount} test result(s) from rerun. Output: {outputFile}");
			return 0;
		}

		private static void RecalculateTestRunTotals(XmlDocument doc)
		{
			// Update all intermediate test-suite nodes so nested suite counts are correct.
			foreach (XmlElement suite in doc.SelectNodes("//test-suite")!.OfType<XmlElement>())
				RecalculateSuiteOrRunTotals(suite);

			// Update the root test-run element last.
			if (doc.SelectSingleNode("//test-run") is XmlElement testRun)
				RecalculateSuiteOrRunTotals(testRun);
		}

		private static void RecalculateSuiteOrRunTotals(XmlElement node)
		{
			var passed = 0;
			var failed = 0;
			var skipped = 0;
			var inconclusive = 0;
			var total = 0;

			foreach (XmlElement tc in node.SelectNodes(".//test-case")!.OfType<XmlElement>())
			{
				total++;
				switch (tc.GetAttribute("result"))
				{
					case "Passed": passed++; break;
					case "Failed": failed++; break;
					case "Skipped": skipped++; break;
					case "Inconclusive": inconclusive++; break;
				}
			}

			node.SetAttribute("total", total.ToString(CultureInfo.InvariantCulture));
			node.SetAttribute("passed", passed.ToString(CultureInfo.InvariantCulture));
			node.SetAttribute("failed", failed.ToString(CultureInfo.InvariantCulture));
			node.SetAttribute("skipped", skipped.ToString(CultureInfo.InvariantCulture));
			node.SetAttribute("inconclusive", inconclusive.ToString(CultureInfo.InvariantCulture));
			node.SetAttribute("result", failed > 0 ? "Failed" : "Passed");
		}

		/// <summary>
		/// Produces a minimal NUnit XML result file that marks <paramref name="testName"/> as Failed
		/// with a watchdog-timeout message.  The file can be merged with a later re-run result.
		/// </summary>
		private static int CreateHungResult(string testName, string outputFile, int hangSeconds)
		{
			var duration = hangSeconds > 0 ? hangSeconds.ToString(CultureInfo.InvariantCulture) : "300";
			var message = hangSeconds > 0
				? $"Test hung for {hangSeconds}s (no heartbeat). The CI watchdog terminated the test runner."
				: "Test hung (no heartbeat). The CI watchdog terminated the test runner.";

			// Sanitise for XML attribute/text content
			testName = testName.Trim();
			var safeName = SecurityElement.Escape(testName) ?? testName;
			var safeMessage = SecurityElement.Escape(message) ?? message;

			var xml = $"""
				<?xml version="1.0" encoding="utf-8" standalone="no"?>
				<test-run id="2" name="WatchdogSynthetic" fullname="WatchdogSynthetic" testcasecount="1"
				          result="Failed" total="1" passed="0" failed="1" inconclusive="0" skipped="0">
				  <test-suite type="Assembly" id="0-1000" name="WatchdogSynthetic" fullname="WatchdogSynthetic"
				              testcasecount="1" result="Failed" total="1" passed="0" failed="1" inconclusive="0" skipped="0">
				    <test-suite type="TestFixture" id="0-1001" name="WatchdogSynthetic" fullname="WatchdogSynthetic"
				                testcasecount="1" result="Failed" total="1" passed="0" failed="1" inconclusive="0" skipped="0">
				      <test-case id="0-1002" name="{safeName}" fullname="WatchdogSynthetic.{safeName}"
				                 result="Failed" duration="{duration}">
				        <failure>
				          <message>{safeMessage}</message>
				        </failure>
				      </test-case>
				    </test-suite>
				  </test-suite>
				</test-run>
				""";

			var tmpFile = outputFile + ".tmp";
			File.WriteAllText(tmpFile, xml, Encoding.UTF8);
			File.Move(tmpFile, outputFile, overwrite: true);

			Console.WriteLine($"Created synthetic hung-test result: {testName} → {outputFile}");
			return 0;
		}

		[GeneratedRegex(@"\(([^)]*)\)")]
		private static partial Regex SimpleNameRegex();
	}
}
