using static Uno.UI.SourceGenerators.Tests.BoxingAnalyzerTests.BoxingAnalyzerHarness;

namespace Uno.UI.SourceGenerators.Tests.BoxingAnalyzerTests;

[TestClass]
public class Given_BoxingCodeFixProvider
{
	[TestMethod]
	[DataRow("object M() => true;", "object M() => Boxes.BoolBoxes.BoxedTrue;")]
	[DataRow("object M() => 1;", "object M() => Boxes.IntegerBoxes.One;")]
	[DataRow("object M() => 0.0;", "object M() => Boxes.DoubleBoxes.Zero;")]
	[DataRow("object M(int value) => value;", "object M(int value) => Boxes.Box(value);")]
	public async Task When_Implicit_Boxing(string member, string expected)
		=> await AssertFixAsync(member, expected);

	[TestMethod]
	[DataRow("object M() => (object)true;", "object M() => Boxes.BoolBoxes.BoxedTrue;")]
	[DataRow("object M() => (object)1;", "object M() => Boxes.IntegerBoxes.One;")]
	[DataRow("object M() => (object)1.0;", "object M() => Boxes.DoubleBoxes.One;")]
	[DataRow("object M() => (object)(false);", "object M() => Boxes.BoolBoxes.BoxedFalse;")]
	[DataRow("object M(int value) => (object)value;", "object M(int value) => Boxes.Box(value);")]
	[DataRow("object M(bool value) => (object)(value);", "object M(bool value) => Boxes.Box(value);")]
	public async Task When_Explicit_Boxing_Cast(string member, string expected)
		=> await AssertFixAsync(member, expected);

	[TestMethod]
	public async Task When_Explicit_Boxing_Cast_Of_Inner_Conversion_Then_Inner_Conversion_Is_Kept()
		=> await AssertFixAsync("object M(byte value) => (object)(int)value;", "object M(byte value) => Boxes.Box((int)value);");

	[TestMethod]
	public async Task When_RoutedEventFlag()
		=> await AssertFixAsync("object M(global::Uno.UI.Xaml.RoutedEventFlag flag) => flag;", "object M(global::Uno.UI.Xaml.RoutedEventFlag flag) => Boxes.Box(flag);");

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
		namespace Test
		{
			public class C
			{
				public {{member}}
			}
		}
		""";
}
