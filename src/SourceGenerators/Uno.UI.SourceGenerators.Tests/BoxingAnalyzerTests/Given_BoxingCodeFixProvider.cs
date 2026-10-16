using static Uno.UI.SourceGenerators.Tests.BoxingAnalyzerTests.BoxingAnalyzerHarness;

namespace Uno.UI.SourceGenerators.Tests.BoxingAnalyzerTests;

[TestClass]
public class Given_BoxingCodeFixProvider
{
	[TestMethod]
	[DataRow("object M(DependencyProperty p) => true;", "object M(DependencyProperty p) => BoolBoxes.True;")]
	[DataRow("object M(DependencyProperty p) => 1;", "object M(DependencyProperty p) => IntBoxes.One;")]
	[DataRow("object M(DependencyProperty p) => 0.0;", "object M(DependencyProperty p) => DoubleBoxes.Zero;")]
	[DataRow("object M(DependencyProperty p, int value) => value;", "object M(DependencyProperty p, int value) => Boxer.Box(value);")]
	[DataRow("object M(DependencyProperty p, byte value) => (int)value;", "object M(DependencyProperty p, byte value) => Boxer.Box((int)value);")]
	[DataRow("object M(DependencyProperty p, byte value) => ((int)value);", "object M(DependencyProperty p, byte value) => (Boxer.Box((int)value));")]
	public async Task When_Implicit_Boxing(string member, string expected)
		=> await AssertFixAsync(member, expected);

	[TestMethod]
	[DataRow("object M(DependencyProperty p) => (object)true;", "object M(DependencyProperty p) => BoolBoxes.True;")]
	[DataRow("object M(DependencyProperty p) => (object)1;", "object M(DependencyProperty p) => IntBoxes.One;")]
	[DataRow("object M(DependencyProperty p) => (object)1.0;", "object M(DependencyProperty p) => DoubleBoxes.One;")]
	[DataRow("object M(DependencyProperty p) => (object)(false);", "object M(DependencyProperty p) => BoolBoxes.False;")]
	[DataRow("object M(DependencyProperty p, int value) => (object)value;", "object M(DependencyProperty p, int value) => Boxer.Box(value);")]
	[DataRow("object M(DependencyProperty p, bool value) => (object)(value);", "object M(DependencyProperty p, bool value) => Boxer.Box(value);")]
	public async Task When_Explicit_Boxing_Cast(string member, string expected)
		=> await AssertFixAsync(member, expected);

	[TestMethod]
	public async Task When_Explicit_Boxing_Cast_Of_Inner_Conversion_Then_Inner_Conversion_Is_Kept()
		=> await AssertFixAsync("object M(DependencyProperty p, byte value) => (object)(int)value;", "object M(DependencyProperty p, byte value) => Boxer.Box((int)value);");

	[TestMethod]
	public async Task When_RoutedEventFlag()
		=> await AssertFixAsync("object M(DependencyProperty p, global::Uno.UI.Xaml.RoutedEventFlag flag) => flag;", "object M(DependencyProperty p, global::Uno.UI.Xaml.RoutedEventFlag flag) => Boxer.Box(flag);");

	[TestMethod]
	public async Task When_Unrelated_Enum_Named_RoutedEventFlag_Then_Not_Rewritten()
	{
		var source = """
			namespace Test
			{
				public enum RoutedEventFlag
				{
					None,
				}

				public class C
				{
					public object M(RoutedEventFlag flag) => flag;
				}
			}
			""";

		var fixedDocument = await ApplyBoxingFixAtAsync(CreateDocument(source), "flag");

		(await fixedDocument.GetTextAsync()).ToString().Should().Be(source);
	}

	private static async Task AssertFixAsync(string member, string expected)
	{
		var document = CreateDocument(Wrap(member));

		var fixedDocument = await ApplyBoxingFixAsync(document);

		var fixedText = (await fixedDocument.GetTextAsync()).ToString();
		fixedText.Should().Contain(expected);
		(await GetDiagnosticsAsync(fixedDocument)).Should().NotContain(d => d.Id == BoxingDiagnosticId);
	}

	private static string Wrap(string member) => $$"""
		using Microsoft.UI.Xaml;

		namespace Test
		{
			public class C
			{
				public {{member}}
			}
		}
		""";
}
