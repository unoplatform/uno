using static Uno.UI.SourceGenerators.Tests.BoxingAnalyzerTests.BoxingAnalyzerHarness;

namespace Uno.UI.SourceGenerators.Tests.BoxingAnalyzerTests;

[TestClass]
public class Given_BoxingDiagnosticAnalyzer
{
	[TestMethod]
	[DataRow("void M(DependencyProperty p) => SetValue(p, true);")]
	[DataRow("void M(DependencyProperty p) => SetValue(p, (object)1);")]
	[DataRow("void M(DependencyProperty p, bool value) => SetValue(p, value);")]
	[DataRow("void M(DependencyProperty p, bool flag) => SetValue(p, flag ? 1.0 : 0.0);")]
	[DataRow("void M(DependencyProperty p, bool flag) => SetValue(p, flag ? (object)true : \"x\");")]
	public async Task When_Boxed_Into_DependencyProperty_Value_Api_Then_Reported(string member)
		=> await AssertReportedAsync(Member(member), expected: true);

	[TestMethod]
	[DataRow("object M() => new PropertyMetadata(false);")]
	[DataRow("object M() => new FrameworkPropertyMetadata(defaultValue: 0.0);")]
	public async Task When_Boxed_As_PropertyMetadata_Default_Then_Reported(string member)
		=> await AssertReportedAsync(Member(member), expected: true);

	[TestMethod]
	[DataRow("object M(DependencyProperty p) => true;")]
	[DataRow("object M(DependencyObject d, bool isGet, object valueToSet) => 1;")]
	[DataRow("object M(object baseValue, DependencyPropertyValuePrecedences precedence) { return false; }")]
	[DataRow("CoerceValueCallback M() => (d, baseValue, precedence) => 0.0;")]
	[DataRow("PropMethodCall M() => (instance, isGet, valueToSet) => true;")]
	public async Task When_Returned_From_DependencyProperty_Callback_Then_Reported(string member)
		=> await AssertReportedAsync(Member(member), expected: true);

	[TestMethod]
	[DataRow("object M() => true;")]
	[DataRow("object M(int value) => value;")]
	[DataRow("string M(int value) => string.Format(\"{0}\", value);")]
	[DataRow("void M(System.Text.StringBuilder builder, int value) => builder.AppendFormat(\"{0}\", value);")]
	[DataRow("object[] M() => new object[] { true };")]
	[DataRow("object M(DependencyProperty p) => new object[] { true };")]
	[DataRow("void M(DependencyProperty p) { System.Func<object> f = () => true; }")]
	[DataRow("bool M(DependencyProperty p) => Equals(GetHashCode(), 0);")]
	public async Task When_Boxed_Outside_DependencyProperty_Value_Path_Then_Not_Reported(string member)
		=> await AssertReportedAsync(Member(member), expected: false);

	[TestMethod]
	[DataRow("string M(DependencyProperty p, bool value) => \"x\" + value;")]
	[DataRow("string M(DependencyProperty p, bool value) { var s = \"x\"; s += value; return s; }")]
	public async Task When_String_Concatenation_Then_Not_Reported(string member)
		=> await AssertReportedAsync(Member(member), expected: false);

	[TestMethod]
	[DataRow("void M(DependencyProperty p) => SetValue(p, 0.0);", true)]
	[DataRow("void M(DependencyProperty p) => SetValue(p, 1.0);", true)]
	[DataRow("void M(DependencyProperty p) => SetValue(p, -0.0);", false)]
	[DataRow("void M(DependencyProperty p) => SetValue(p, 2.0);", false)]
	public async Task When_Double_Constant(string member, bool expected)
		=> await AssertReportedAsync(Member(member), expected);

	[TestMethod]
	[DataRow("void M(DependencyProperty p, global::Uno.UI.Xaml.RoutedEventFlag flag) => SetValue(p, flag);", true)]
	[DataRow("void M(DependencyProperty p, RoutedEventFlag flag) => SetValue(p, flag);", false)]
	[DataRow("void M(DependencyProperty p) => SetValue(p, global::Uno.UI.Xaml.RoutedEventFlag.PointerPressed);", true)]
	[DataRow("void M(DependencyProperty p) => SetValue(p, global::Uno.UI.Xaml.RoutedEventFlag.PointerPressed | global::Uno.UI.Xaml.RoutedEventFlag.PointerReleased);", false)]
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

	private static async Task AssertReportedAsync(string source, bool expected)
	{
		var diagnostics = await GetDiagnosticsAsync(CreateDocument(source));

		diagnostics.Any(d => d.Id == BoxingDiagnosticId).Should().Be(expected);
	}

	private static string Member(string member, string siblingType = "") => $$"""
		using Microsoft.UI.Xaml;

		namespace Test
		{
			{{siblingType}}

			public class C : DependencyObject
			{
				public {{member}}
			}
		}
		""";
}
