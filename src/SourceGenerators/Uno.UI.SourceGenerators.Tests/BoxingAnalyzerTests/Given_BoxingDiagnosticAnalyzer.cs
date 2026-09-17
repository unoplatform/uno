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

	[TestMethod]
	public async Task When_Conditional_Attribute_Is_Not_The_Compiler_One_Then_Reported()
		=> await AssertReportedAsync(
			"""
			namespace Test
			{
				public sealed class ConditionalAttribute : System.Attribute
				{
					public ConditionalAttribute(string condition)
					{
					}
				}

				public class C
				{
					[Conditional("OFF")]
					private static void Trace(object value)
					{
					}

					public void M() => Trace(true);
				}
			}
			""",
			expected: true);

	[TestMethod]
	[DataRow("object M() => 0.0;", true)]
	[DataRow("object M() => 1.0;", true)]
	[DataRow("object M() => -0.0;", false)]
	[DataRow("object M() => 2.0;", false)]
	public async Task When_Double_Constant(string member, bool expected)
		=> await AssertReportedAsync(Member(member), expected);

	[TestMethod]
	[DataRow("string M(bool value) => \"x\" + value;", false)]
	[DataRow("string M(bool value) { var s = \"x\"; s += value; return s; }", false)]
	[DataRow("string M(bool value) => \"x\" + (object)value;", true)]
	public async Task When_String_Concatenation(string member, bool expected)
		=> await AssertReportedAsync(Member(member), expected);

	[TestMethod]
	[DataRow("object M(global::Uno.UI.Xaml.RoutedEventFlag flag) => flag;", true)]
	[DataRow("object M(RoutedEventFlag flag) => flag;", false)]
	[DataRow("object M() => global::Uno.UI.Xaml.RoutedEventFlag.PointerPressed;", true)]
	[DataRow("object M() => global::Uno.UI.Xaml.RoutedEventFlag.PointerPressed | global::Uno.UI.Xaml.RoutedEventFlag.PointerReleased;", false)]
	public async Task When_RoutedEventFlag(string member, bool expected)
		=> await AssertReportedAsync(Member(member, "public enum RoutedEventFlag { None }"), expected);

	[TestMethod]
	[DataRow("void SetValue(global::Microsoft.UI.Xaml.DependencyProperty property, double value) { }", "SetValue(null, 1.0f)", true)]
	[DataRow("void SetValue(global::Microsoft.UI.Xaml.DependencyProperty property, double value) { }", "SetValue(null, 1.0)", false)]
	[DataRow("void SetValue(string key, double value) { }", "SetValue(\"key\", 1.0f)", false)]
	[DataRow("void SetValue(global::Microsoft.UI.Xaml.DependencyProperty property, double value) { }", "SetValue(value: 1.0f, property: default(global::Microsoft.UI.Xaml.DependencyProperty))", true)]
	[DataRow("void SetValue(global::Microsoft.UI.Xaml.DependencyProperty property, double value) { }", "SetValue(value: 1.0, property: default(global::Microsoft.UI.Xaml.DependencyProperty))", false)]
	public async Task When_Typed_SetValue_Receives_Converted_Argument(string setValue, string call, bool expected)
	{
		var diagnostics = await GetDiagnosticsAsync(CreateDocument(Member($"{setValue}\r\n\t\tpublic void M() => {call};")));

		var reported = diagnostics.Where(d => d.Id == "UnoInternal0003").ToArray();
		reported.Any().Should().Be(expected);
		foreach (var diagnostic in reported)
		{
			diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan).Should().Contain("1.0f", "the diagnostic belongs on the value argument");
		}
	}

	private static async Task AssertReportedAsync(string source, bool expected, params string[] preprocessorSymbols)
	{
		var diagnostics = await GetDiagnosticsAsync(CreateDocument(source, preprocessorSymbols));

		diagnostics.Any(d => d.Id == BoxingDiagnosticId).Should().Be(expected);
	}

	private static string Member(string member, string siblingType = "") => $$"""
		namespace Test
		{
			{{siblingType}}

			public class C
			{
				public {{member}}
			}
		}
		""";

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
