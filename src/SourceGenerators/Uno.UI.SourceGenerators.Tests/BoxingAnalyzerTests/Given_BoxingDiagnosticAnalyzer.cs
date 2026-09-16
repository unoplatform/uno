using static Uno.UI.SourceGenerators.Tests.BoxingAnalyzerTests.BoxingAnalyzerHarness;

namespace Uno.UI.SourceGenerators.Tests.BoxingAnalyzerTests;

[TestClass]
public class Given_BoxingDiagnosticAnalyzer
{
	[TestMethod]
	public async Task When_Conditional_Symbol_Is_Undefined_Then_Call_Is_Omitted_And_Not_Reported()
		=> await AssertReportedAsync(ConditionalCall(attributes: """[Conditional("ON")]"""), expected: false);

	[TestMethod]
	public async Task When_Conditional_Symbol_Is_Defined_Then_Reported()
		=> await AssertReportedAsync(ConditionalCall(attributes: """[Conditional("ON")]"""), expected: true, "ON");

	[TestMethod]
	[DataRow("ON")]
	[DataRow("OFF")]
	public async Task When_Any_Of_Multiple_Conditional_Symbols_Is_Defined_Then_Reported(string definedSymbol)
		=> await AssertReportedAsync(ConditionalCall(attributes: """[Conditional("ON"), Conditional("OFF")]"""), expected: true, definedSymbol);

	[TestMethod]
	public async Task When_None_Of_Multiple_Conditional_Symbols_Is_Defined_Then_Not_Reported()
		=> await AssertReportedAsync(ConditionalCall(attributes: """[Conditional("ON"), Conditional("OFF")]"""), expected: false);

	[TestMethod]
	public async Task When_Conditional_Symbol_Is_Defined_In_File_Then_Reported()
		=> await AssertReportedAsync(ConditionalCall(attributes: """[Conditional("ON")]""", directives: "#define ON"), expected: true);

	[TestMethod]
	public async Task When_Conditional_Symbol_Is_Undefined_In_File_Then_Not_Reported()
		=> await AssertReportedAsync(ConditionalCall(attributes: """[Conditional("ON")]""", directives: "#undef ON"), expected: false, "ON");

	[TestMethod]
	public async Task When_Conditional_Symbol_Is_Defined_In_Inactive_Region_Then_Not_Reported()
		=> await AssertReportedAsync(ConditionalCall(attributes: """[Conditional("ON")]""", directives: "#if NEVER\r\n#define ON\r\n#endif"), expected: false);

	private static async Task AssertReportedAsync(string source, bool expected, params string[] preprocessorSymbols)
	{
		var diagnostics = await GetDiagnosticsAsync(CreateDocument(source, preprocessorSymbols));

		diagnostics.Any(d => d.Id == BoxingDiagnosticId).Should().Be(expected);
	}

	private static string ConditionalCall(string attributes, string directives = "") => $$"""
		{{directives}}
		using System.Diagnostics;

		namespace Test
		{
			public class C
			{
				{{attributes}}
				private static void Trace(object value)
				{
				}

				public void M() => Trace(true);
			}
		}
		""";
}
