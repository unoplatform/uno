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
using Uno.NUnitTransformTool;

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
				case "rerun-filter":
					return WriteRerunFilter(args[1], int.Parse(args[2], CultureInfo.InvariantCulture), args[3]);
				case "merge-rerun":
					return MergeRerunResults(args[1], args[2], args[3]);
			}

			return 0;
		}

		private static XmlDocument LoadResults(string inputFile)
		{
			var doc = new XmlDocument();
			doc.LoadXml(File.ReadAllText(inputFile));
			return doc;
		}

		/// <summary>
		/// Writes the base64 runtime tests filter of the failed tests to <paramref name="outputFile"/>, only when
		/// there are between 1 and <paramref name="maxFailures"/> of them: more is a real break, not worth re-running.
		/// </summary>
		private static int WriteRerunFilter(string inputFile, int maxFailures, string outputFile)
		{
			File.Delete(outputFile);

			var failedTests = RuntimeTestsRerun.GetFailedTestNames(LoadResults(inputFile));

			if (failedTests.Count == 0)
			{
				Console.WriteLine($"No failed tests in {inputFile}, nothing to re-run.");
			}
			else if (failedTests.Count > maxFailures)
			{
				Console.WriteLine($"{failedTests.Count} failed tests in {inputFile}, more than the {maxFailures} worth re-running.");
			}
			else
			{
				Console.WriteLine($"Re-running {failedTests.Count} failed tests from {inputFile}.");

				var filter = RuntimeTestsRerun.GetFailedTestsFilter(failedTests);
				File.WriteAllText(outputFile, Convert.ToBase64String(Encoding.UTF8.GetBytes(filter)));
			}

			return 0;
		}

		private static int MergeRerunResults(string originalFile, string rerunFile, string outputFile)
		{
			var original = LoadResults(originalFile);
			var result = RuntimeTestsRerun.Merge(original, LoadResults(rerunFile));

			foreach (var test in result.Recovered)
			{
				Console.WriteLine($"##vso[task.logissue type=warning]Flaky test: {test} failed, then passed when re-run in a fresh app process.");
			}

			foreach (var test in result.StillFailing)
			{
				Console.WriteLine($"Still failing after the re-run: {test}");
			}

			Console.WriteLine($"The re-run recovered {result.Recovered.Count} of {result.Recovered.Count + result.StillFailing.Count} failed tests.");

			original.Save(outputFile);

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
			var distinctTests = RuntimeTestsRerun.GetFailedTestNames(LoadResults(inputFile));

			// Reported before the sentinel is appended, so the count is the number of real failures,
			// and named so the retry decision can be made from this log even when the results file
			// never reached PublishTestResults.
			Console.WriteLine($"Found {distinctTests.Count} failed tests in {inputFile}.");

			foreach (var failedTest in distinctTests)
			{
				Console.WriteLine($"  {failedTest}");
			}

			File.WriteAllText(outputFile, RuntimeTestsRerun.GetFailedTestsFilter(distinctTests));

			return 0;
		}
	}
}
