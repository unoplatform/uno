using static Uno.UI.SourceGenerators.Tests.BoxingAnalyzerTests.BoxingAnalyzerHarness;

namespace Uno.UI.SourceGenerators.Tests.BoxingAnalyzerTests;

[TestClass]
public class Given_BoxingCodeFixProvider
{
	[TestMethod]
	[DataRow("object M() => true;", "object M() => BoolBoxes.True;")]
	[DataRow("object M() => 1;", "object M() => IntBoxes.One;")]
	[DataRow("object M() => 0.0;", "object M() => DoubleBoxes.Zero;")]
	[DataRow("object M(int value) => value;", "object M(int value) => Boxer.Box(value);")]
	[DataRow("object M(byte value) => (int)value;", "object M(byte value) => Boxer.Box((int)value);")]
	[DataRow("object M(byte value) => ((int)value);", "object M(byte value) => (Boxer.Box((int)value));")]
	public async Task When_Implicit_Boxing(string member, string expected)
		=> await AssertFixAsync(member, expected);

	[TestMethod]
	[DataRow("object M() => (object)true;", "object M() => BoolBoxes.True;")]
	[DataRow("object M() => (object)1;", "object M() => IntBoxes.One;")]
	[DataRow("object M() => (object)1.0;", "object M() => DoubleBoxes.One;")]
	[DataRow("object M() => (object)(false);", "object M() => BoolBoxes.False;")]
	[DataRow("object M(int value) => (object)value;", "object M(int value) => Boxer.Box(value);")]
	[DataRow("object M(bool value) => (object)(value);", "object M(bool value) => Boxer.Box(value);")]
	public async Task When_Explicit_Boxing_Cast(string member, string expected)
		=> await AssertFixAsync(member, expected);

	[TestMethod]
	public async Task When_Explicit_Boxing_Cast_Of_Inner_Conversion_Then_Inner_Conversion_Is_Kept()
		=> await AssertFixAsync("object M(byte value) => (object)(int)value;", "object M(byte value) => Boxer.Box((int)value);");

	[TestMethod]
	public async Task When_RoutedEventFlag()
		=> await AssertFixAsync("object M(global::Uno.UI.Xaml.RoutedEventFlag flag) => flag;", "object M(global::Uno.UI.Xaml.RoutedEventFlag flag) => Boxer.Box(flag);");

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
