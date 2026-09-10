#if HAS_UNO
#nullable enable

using System.Xml;
using System.Collections;
using System.Collections.Generic;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

internal class NUnitXmlParser
{
	public record TestResult(string result, string Name, string? errorMessage);

	internal static TestResult[] GetTests(string resultFile)
	{
		var doc = new XmlDocument();
		doc.Load(resultFile);

		List<TestResult> results = new();

		var testCases = doc.SelectNodes("//test-case");

		if (testCases is not null)
		{
			foreach (var testCase in testCases)
			{
				if (testCase is XmlNode node
					&& node.Attributes is not null)
				{
					var result = node.Attributes["result"]?.Value ?? "N/A";

					var name = node.Attributes["name"]?.Value;
					var fullName = node.Attributes["fullname"]?.Value;
					var error = node.SelectSingleNode("failure/message")?.InnerText;

					results.Add(new(result, name ?? "Unknown", error!));
				}
			}
		}

		return results.ToArray();
	}
}
#endif
