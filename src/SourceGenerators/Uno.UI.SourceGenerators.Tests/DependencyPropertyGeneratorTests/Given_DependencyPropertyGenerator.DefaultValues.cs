using static Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests.DependencyPropertyGeneratorHarness;

namespace Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests;

public partial class Given_DependencyPropertyGenerator
{
	[TestMethod]
	[DataRow("string", "\"a\\\"b\\\\c\\n\"", "\"a\\\"b\\\\c\\n\"")]
	[DataRow("char", "'\\''", "'\\''")]
	[DataRow("double", "double.NaN", "double.NaN")]
	[DataRow("double", "double.PositiveInfinity", "double.PositiveInfinity")]
	[DataRow("double", "double.NegativeInfinity", "double.NegativeInfinity")]
	[DataRow("double", "0.1", "0.1d")]
	[DataRow("double", "-0.0", "-0d")]
	[DataRow("double", "21", "21d")]
	[DataRow("double?", "2.5", "2.5d")]
	[DataRow("float", "1.5f", "1.5f")]
	[DataRow("float", "float.NegativeInfinity", "float.NegativeInfinity")]
	[DataRow("long", "-3", "-3L")]
	[DataRow("uint", "7", "7U")]
	[DataRow("short", "-2", "(short)(-2)")]
	[DataRow("byte", "200", "(byte)200")]
	[DataRow("decimal", "3", "3m")]
	[DataRow("bool", "true", "true")]
	[DataRow("int", "int.MaxValue", "2147483647")]
	[DataRow("int", "0", "0")]
	[DataRow("string", "null", "null")]
	[DataRow("int?", "null", "null")]
	[DataRow("object", "5", "5")]
	[DataRow("object", "\"text\"", "\"text\"")]
	[DataRow("global::System.Type", "typeof(string)", "typeof(string)")]
	public async Task When_DefaultValue_Is_Formatted(string propertyType, string defaultValue, string expected)
	{
		var run = await RunAsync(InstanceType($$"""
			[GeneratedDependencyProperty(DefaultValue = {{defaultValue}}, LocalCache = false)]
			public partial {{propertyType}} MyValue { get; set; }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, $"defaultValue: {expected},");
	}

	[TestMethod]
	[DataRow("MyEnum.Second", "global::Mynamespace.MyEnum.Second")]
	[DataRow("MyEnum.Negative", "global::Mynamespace.MyEnum.Negative")]
	[DataRow("(MyEnum)(-5)", "(global::Mynamespace.MyEnum)(-5)")]
	[DataRow("(MyEnum)42", "(global::Mynamespace.MyEnum)(42)")]
	[DataRow("default(MyEnum)", "global::Mynamespace.MyEnum.First")]
	[DataRow("0", "global::Mynamespace.MyEnum.First")]
	public async Task When_DefaultValue_Is_Enum(string defaultValue, string expected)
	{
		var run = await RunAsync(
			$$"""
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace Mynamespace
			{
				public enum MyEnum : short
				{
					Negative = -1,
					First = 0,
					Second = 1,
				}

				public partial class C : TestDependencyObject
				{
					[GeneratedDependencyProperty(DefaultValue = {{defaultValue}})]
					public partial MyEnum MyValue { get; set; }
				}
			}
			""");

		run.ShouldSucceed().ShouldContain(HintName, $"defaultValue: {expected},");
	}

	[TestMethod]
	public async Task When_DefaultValue_Is_Flags_Enum()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(DefaultValue = FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure, Options = FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AutoConvert)]
			public partial FrameworkPropertyMetadataOptions MyValue { get; set; }
			"""));

		run.ShouldSucceed().ShouldContain(
			HintName,
			"defaultValue: global::Microsoft.UI.Xaml.FrameworkPropertyMetadataOptions.Inherits | global::Microsoft.UI.Xaml.FrameworkPropertyMetadataOptions.AffectsMeasure,",
			"options: global::Microsoft.UI.Xaml.FrameworkPropertyMetadataOptions.AutoConvert | global::Microsoft.UI.Xaml.FrameworkPropertyMetadataOptions.AffectsArrange,");
	}

	[TestMethod]
	[DataRow("bool", "true", "global::Uno.UI.Helpers.Boxes.BoolBoxes.True")]
	[DataRow("bool", "false", "global::Uno.UI.Helpers.Boxes.BoolBoxes.False")]
	[DataRow("bool?", "true", "global::Uno.UI.Helpers.Boxes.BoolBoxes.True")]
	[DataRow("int", "-1", "global::Uno.UI.Helpers.Boxes.IntBoxes.NegativeOne")]
	[DataRow("int", "0", "global::Uno.UI.Helpers.Boxes.IntBoxes.Zero")]
	[DataRow("int", "1", "global::Uno.UI.Helpers.Boxes.IntBoxes.One")]
	[DataRow("int", "2", "2")]
	[DataRow("double", "0", "global::Uno.UI.Helpers.Boxes.DoubleBoxes.Zero")]
	[DataRow("double", "1.0", "global::Uno.UI.Helpers.Boxes.DoubleBoxes.One")]
	[DataRow("double", "-0.0", "-0d")]
	[DataRow("double?", "-0.0", "-0d")]
	[DataRow("double", "0.5", "0.5d")]
	[DataRow("object", "0", "global::Uno.UI.Helpers.Boxes.IntBoxes.Zero")]
	[DataRow("object", "-0.0", "-0d")]
	public async Task When_DefaultValue_Has_A_Cached_Box(string propertyType, string defaultValue, string expected)
	{
		var run = await RunWithBoxesAsync(InstanceType($$"""
			[GeneratedDependencyProperty(DefaultValue = {{defaultValue}})]
			public partial {{propertyType}} MyValue { get; set; }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, $"defaultValue: {expected},");
		(await run.GetGeneratedCodeAnalyzerDiagnosticsAsync("Uno.UI.SourceGenerators.Internal.BoxingDiagnosticAnalyzer")).Should().BeEmpty();
	}

	[TestMethod]
	[DataRow("double", false)]
	[DataRow("double", true)]
	[DataRow("double?", true)]
	[DataRow("object", true)]
	public async Task When_DefaultValue_Is_Negative_Zero_It_Keeps_Its_Sign(string propertyType, bool withBoxes)
	{
		var source = InstanceType($$"""
			[GeneratedDependencyProperty(DefaultValue = -0.0d)]
			public partial {{propertyType}} MyValue { get; set; }
			""");
		var run = withBoxes ? await RunWithBoxesAsync(source) : await RunAsync(source);

		run.ShouldSucceed().ShouldNotContain(HintName, "DoubleBoxes.Zero");

		// A bare "-0" would be integer negation, registering positive zero.
		var tree = run.OutputCompilation.SyntaxTrees.Single(t => t.FilePath.EndsWith(HintName, StringComparison.Ordinal));
		var argument = (await tree.GetRootAsync()).DescendantNodes()
			.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentSyntax>()
			.Single(a => a.NameColon?.Name.Identifier.ValueText == "defaultValue");
		var constant = run.OutputCompilation.GetSemanticModel(tree).GetConstantValue(argument.Expression);

		constant.HasValue.Should().BeTrue(argument.ToString());
		constant.Value.Should().BeOfType<double>();
		BitConverter.DoubleToInt64Bits((double)constant.Value!).Should().Be(BitConverter.DoubleToInt64Bits(-0.0d));

		if (withBoxes)
		{
			(await run.GetGeneratedCodeAnalyzerDiagnosticsAsync("Uno.UI.SourceGenerators.Internal.BoxingDiagnosticAnalyzer")).Should().BeEmpty();
		}
	}

	[TestMethod]
	[DataRow("int", "0")]
	[DataRow("double", "0d")]
	[DataRow("bool", "false")]
	[DataRow("string", "null")]
	[DataRow("int?", "null")]
	[DataRow("global::Mynamespace.MyStruct", "default(global::Mynamespace.MyStruct)")]
	public async Task When_No_DefaultValue(string propertyType, string expected)
	{
		var run = await RunAsync(InstanceType($$"""
			[GeneratedDependencyProperty]
			public partial {{propertyType}} MyValue { get; set; }
			}

			public struct MyStruct
			{
			"""));

		run.ShouldSucceed().ShouldContain(HintName, $"defaultValue: {expected},");
	}

	[TestMethod]
	[DataRow("int", "global::Uno.UI.Helpers.Boxes.IntBoxes.Zero")]
	[DataRow("double", "global::Uno.UI.Helpers.Boxes.DoubleBoxes.Zero")]
	[DataRow("bool", "global::Uno.UI.Helpers.Boxes.BoolBoxes.False")]
	public async Task When_No_DefaultValue_With_Boxes(string propertyType, string expected)
	{
		var run = await RunWithBoxesAsync(InstanceType($$"""
			[GeneratedDependencyProperty]
			public partial {{propertyType}} MyValue { get; set; }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, $"defaultValue: {expected},");
	}

	[TestMethod]
	public async Task When_DefaultValue_Method()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public partial int MyValue { get; set; }

			private static int GetMyValueDefaultValue() => 42;
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "defaultValue: GetMyValueDefaultValue(),");
	}

	[TestMethod]
	public async Task When_DefaultValue_Method_Returns_A_Boxable_Type()
	{
		var run = await RunWithBoxesAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public partial int MyValue { get; set; }

			private static int GetMyValueDefaultValue() => 42;
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "defaultValue: global::Uno.UI.Helpers.Boxes.Boxer.Box(GetMyValueDefaultValue()),");
		(await run.GetGeneratedCodeAnalyzerDiagnosticsAsync("Uno.UI.SourceGenerators.Internal.BoxingDiagnosticAnalyzer")).Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_DefaultValue_Method_Returns_Object()
	{
		var run = await RunWithBoxesAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public partial Windows.Foundation.Point MyValue { get; set; }

			private static object GetMyValueDefaultValue() => default(Windows.Foundation.Point);
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "defaultValue: GetMyValueDefaultValue(),");
	}
}
