using System.Text;
using System.Xml;
using AwesomeAssertions;

namespace Uno.NUnitTransformTool.Tests;

[TestClass]
public class Given_RuntimeTestsRerun
{
	[TestMethod]
	public void When_FailedTestPassesOnRerun_Then_ItIsReportedPassed()
	{
		var original = Results(
			Case("NS.Given_A.When_Stable()", "Passed"),
			Case("NS.Given_A.When_Flaky()", "Failed", "first failure"));
		var rerun = Results(Case("NS.Given_A.When_Flaky()", "Passed"));

		var result = RuntimeTestsRerun.Merge(original, rerun);

		result.Recovered.Should().Equal("NS.Given_A.When_Flaky()");
		result.StillFailing.Should().BeEmpty();
		ResultOf(original, "NS.Given_A.When_Flaky()").Should().Be("Passed");
		original.SelectNodes("//test-case")!.Count.Should().Be(2);
	}

	[TestMethod]
	public void When_AllFailuresRecover_Then_SuitesArePassedWithUpdatedCounters()
	{
		var original = Results(
			Case("NS.Given_A.When_Stable()", "Passed"),
			Case("NS.Given_A.When_Flaky()", "Failed", "first failure"));
		var rerun = Results(Case("NS.Given_A.When_Flaky()", "Passed"));

		RuntimeTestsRerun.Merge(original, rerun);

		foreach (var suite in original.SelectNodes("//test-run | //test-suite[@type='TestFixture']")!.OfType<XmlElement>())
		{
			suite.GetAttribute("result").Should().Be("Passed");
			suite.GetAttribute("passed").Should().Be("2");
			suite.GetAttribute("failed").Should().Be("0");
		}
	}

	[TestMethod]
	public void When_SomeFailuresRemain_Then_SuitesStayFailed()
	{
		var original = Results(
			Case("NS.Given_A.When_Flaky()", "Failed", "first failure"),
			Case("NS.Given_A.When_Broken()", "Failed", "broken"));
		var rerun = Results(
			Case("NS.Given_A.When_Flaky()", "Passed"),
			Case("NS.Given_A.When_Broken()", "Failed", "broken again"));

		var result = RuntimeTestsRerun.Merge(original, rerun);

		result.StillFailing.Should().Equal("NS.Given_A.When_Broken()");
		original.DocumentElement!.GetAttribute("result").Should().Be("Failed");
		original.DocumentElement.GetAttribute("failed").Should().Be("1");
		original.DocumentElement.GetAttribute("passed").Should().Be("1");
	}

	[TestMethod]
	public void When_FailureStaysOnRerun_Then_TheFirstFailureIsKept()
	{
		var original = Results(Case("NS.Given_A.When_Broken()", "Failed", "first failure"));
		var rerun = Results(Case("NS.Given_A.When_Broken()", "Failed", "second failure"));

		RuntimeTestsRerun.Merge(original, rerun);

		original.SelectSingleNode("//failure/message")!.InnerText.Should().Be("first failure");
	}

	[TestMethod]
	public void When_TestIsMissingFromRerun_Then_ItStaysFailed()
	{
		var original = Results(Case("NS.Given_A.When_Crashing()", "Failed", "crash"));
		var rerun = Results(Case("NS.Given_A.When_Other()", "Passed"));

		var result = RuntimeTestsRerun.Merge(original, rerun);

		result.StillFailing.Should().Equal("NS.Given_A.When_Crashing()");
		ResultOf(original, "NS.Given_A.When_Crashing()").Should().Be("Failed");
	}

	[TestMethod]
	public void When_ParameterizedCaseRecovers_Then_OnlyThatCaseIsReplaced()
	{
		// The re-run filter drops the arguments, so every case of the method runs again.
		var original = Results(
			Case("NS.Given_A.When_Level(1)", "Passed"),
			Case("NS.Given_A.When_Level(2)", "Failed", "level 2"),
			Case("NS.Given_A.When_Level(3)", "Passed"));
		var rerun = Results(
			Case("NS.Given_A.When_Level(1)", "Failed", "failed on rerun only"),
			Case("NS.Given_A.When_Level(2)", "Passed"),
			Case("NS.Given_A.When_Level(3)", "Passed"));

		var result = RuntimeTestsRerun.Merge(original, rerun);

		result.Recovered.Should().Equal("NS.Given_A.When_Level(2)");
		ResultOf(original, "NS.Given_A.When_Level(1)").Should().Be("Passed");
		ResultOf(original, "NS.Given_A.When_Level(2)").Should().Be("Passed");
		original.DocumentElement!.GetAttribute("failed").Should().Be("0");
	}

	[TestMethod]
	public void When_FailedTestRecovers_Then_TheFirstFailureIsKeptInTheOutput()
	{
		var original = Results(Case("NS.Given_A.When_Flaky()", "Failed", "first failure"));
		var rerun = Results(Case("NS.Given_A.When_Flaky()", "Passed", output: "rerun output"));

		RuntimeTestsRerun.Merge(original, rerun);

		var output = original.SelectSingleNode("//test-case/output")!.InnerText;
		output.Should().Contain("first failure").And.Contain("rerun output");
	}

	[TestMethod]
	public void When_GettingTheFilter_Then_ArgumentsAreStrippedAndTheSentinelAppended()
	{
		var results = Results(
			Case("NS.Given_A.When_Level(1)", "Failed", "a"),
			Case("NS.Given_A.When_Level(2)", "Failed", "b"),
			Case("NS.Given_A.When_Other()", "Failed", "c"),
			Case("NS.Given_A.When_Fine()", "Passed"));

		var names = RuntimeTestsRerun.GetFailedTestNames(results);

		RuntimeTestsRerun.GetFailedTestsFilter(names)
			.Should().Be("NS.Given_A.When_Level | NS.Given_A.When_Other | invalid-test-for-retry");
	}

	private static string ResultOf(XmlDocument results, string fullName)
		=> ((XmlElement)results.SelectSingleNode($"//test-case[@fullname='{fullName}']")!).GetAttribute("result");

	private static string Case(string fullName, string result, string? message = null, string? output = null)
	{
		var builder = new StringBuilder($"<test-case name=\"{fullName.Split('.')[^1]}\" fullname=\"{fullName}\" result=\"{result}\">");
		if (message is not null)
		{
			builder.Append($"<failure><message>{message}</message></failure>");
		}
		if (output is not null)
		{
			builder.Append($"<output>{output}</output>");
		}
		return builder.Append("</test-case>").ToString();
	}

	// Mirrors the layout the runtime tests runner writes: one fixture holding every case.
	private static XmlDocument Results(params string[] cases)
	{
		var passed = cases.Count(c => c.Contains("result=\"Passed\""));
		var failed = cases.Count(c => c.Contains("result=\"Failed\""));
		var counters = $"testcasecount=\"{cases.Length}\" result=\"{(failed == 0 ? "Passed" : "Failed")}\" total=\"{cases.Length}\" passed=\"{passed}\" failed=\"{failed}\" inconclusive=\"0\" skipped=\"0\"";

		var doc = new XmlDocument();
		doc.LoadXml($"""
			<test-run id="r" name="Runtime Tests" {counters}>
				<test-suite type="Assembly" name="SamplesApp">
					<test-suite type="TestFixture" name="r" executed="true" {counters}>
						{string.Concat(cases)}
					</test-suite>
				</test-suite>
			</test-run>
			""");
		return doc;
	}
}
