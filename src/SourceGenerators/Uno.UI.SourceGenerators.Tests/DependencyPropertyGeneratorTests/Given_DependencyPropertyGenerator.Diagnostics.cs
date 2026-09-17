using static Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests.DependencyPropertyGeneratorHarness;

namespace Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests;

public partial class Given_DependencyPropertyGenerator
{
	[TestMethod]
	public async Task When_Property_Is_Not_Partial()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public int MyValue { get; set; }
			"""));

		var diagnostic = run.ShouldReportSingle("UnoInternal0010");
		diagnostic.GetMessage().Should().Be("'C.MyValue' must be a partial definition without an implementation part to use [GeneratedDependencyProperty]");

		var span = diagnostic.Location.GetLineSpan();
		span.Path.Should().Be("/0/Test2.cs");
		run.InputCompilation.SyntaxTrees.Single(t => t.FilePath == "/0/Test2.cs").GetText().ToString(diagnostic.Location.SourceSpan).Should().Be("MyValue");
		run.HintNames.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_Partial_Property_Already_Has_An_Implementation()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public partial int MyValue { get; set; }

			public partial int MyValue { get => 0; set { } }
			"""));

		run.ShouldReportSingle("UnoInternal0010");
	}

	[TestMethod]
	public async Task When_Attached_Getter_Is_Not_Partial()
	{
		var run = await RunAsync(StaticType("""
			[GeneratedDependencyProperty]
			public static int GetMyValue(DependencyObject target) => 0;
			"""));

		run.ShouldReportSingle("UnoInternal0010");
	}

	[TestMethod]
	public async Task When_Legacy_Identifier_Pattern()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public static DependencyProperty MyValueProperty { get; } = null!;
			"""));

		run.ShouldReportSingle("UnoInternal0011").GetMessage().Should().Contain("'MyValueProperty'").And.Contain("'MyValue'");
	}

	[TestMethod]
	[DataRow("public partial int MyValue { get; init; }", "init accessors are not supported")]
	[DataRow("public static partial int MyValue { get; set; }", "static properties are not supported")]
	[DataRow("public partial int MyValue { set; }", "a get accessor is required")]
	[DataRow("public partial int this[int index] { get; set; }", "indexers are not supported")]
	public async Task When_Property_Shape_Is_Unsupported(string declaration, string reason)
	{
		var run = await RunAsync(InstanceType($$"""
			[GeneratedDependencyProperty]
			{{declaration}}
			"""));

		run.ShouldReportSingle("UnoInternal0012").GetMessage().Should().Contain(reason);
	}

	[TestMethod]
	public async Task When_Containing_Type_Is_Not_A_DependencyObject()
	{
		var run = await RunAsync(
			"""
			using Uno.UI.Xaml;

			namespace Mynamespace
			{
				public partial class C
				{
					[GeneratedDependencyProperty]
					public partial int MyValue { get; set; }
				}
			}
			""");

		run.ShouldReportSingle("UnoInternal0013").GetMessage().Should().Contain("deriving from DependencyObject");
	}

	[TestMethod]
	public async Task When_Outer_Containing_Type_Is_Not_Partial()
	{
		var run = await RunAsync(
			"""
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace Mynamespace
			{
				public class Outer
				{
					public partial class C : TestDependencyObject
					{
						[GeneratedDependencyProperty]
						public partial int MyValue { get; set; }
					}
				}
			}
			""");

		run.ShouldReportSingle("UnoInternal0013").GetMessage().Should().Contain("'Outer' must be partial");
	}

	[TestMethod]
	public async Task When_Attached_Getter_Signature_Is_Invalid()
	{
		var run = await RunAsync(StaticType("""
			[GeneratedDependencyProperty]
			public static partial int GetMyValue(int target);
			"""));

		run.ShouldReportSingle("UnoInternal0014");
	}

	[TestMethod]
	public async Task When_Attached_Setter_Signature_Is_Invalid()
	{
		var run = await RunAsync(StaticType("""
			[GeneratedDependencyProperty]
			public static partial int GetMyValue(DependencyObject target);

			public static partial void SetMyValue(DependencyObject target, string value);
			"""));

		run.ShouldReportSingle("UnoInternal0014").GetMessage().Should().Contain("'C.SetMyValue'");
	}

	[TestMethod]
	public async Task When_Identifier_Is_Already_Declared()
	{
		var run = await RunAsync(InstanceType("""
			public static DependencyProperty MyValueProperty { get; } = null!;

			[GeneratedDependencyProperty]
			public partial int MyValue { get; set; }
			"""));

		run.ShouldReportSingle("UnoInternal0015");
	}

	[TestMethod]
	public async Task When_Explicit_Identifier_Declaration_Is_Invalid()
	{
		var run = await RunAsync(InstanceType("""
			public partial DependencyProperty MyValueProperty { get; }

			[GeneratedDependencyProperty]
			public partial int MyValue { get; set; }
			"""));

		run.ShouldReportSingle("UnoInternal0016");
	}

	[TestMethod]
	public async Task When_DefaultValue_And_DefaultValue_Method()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(DefaultValue = 1)]
			public partial int MyValue { get; set; }

			private static int GetMyValueDefaultValue() => 2;
			"""));

		run.ShouldReportSingle("UnoInternal0017");
	}

	[TestMethod]
	public async Task When_DefaultValue_Method_Is_Invalid()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public partial int MyValue { get; set; }

			private int GetMyValueDefaultValue() => 2;
			"""));

		run.ShouldReportSingle("UnoInternal0018");
	}

	[TestMethod]
	[DataRow("int", "\"text\"")]
	[DataRow("int", "null")]
	[DataRow("int", "1.5")]
	[DataRow("byte", "300")]
	[DataRow("string", "1")]
	[DataRow("global::Microsoft.UI.Xaml.Visibility", "FrameworkPropertyMetadataOptions.Inherits")]
	public async Task When_DefaultValue_Is_Incompatible(string propertyType, string defaultValue)
	{
		var run = await RunAsync(InstanceType($$"""
			[GeneratedDependencyProperty(DefaultValue = {{defaultValue}})]
			public partial {{propertyType}} MyValue { get; set; }
			"""));

		var diagnostic = run.ShouldReportSingle("UnoInternal0019");
		diagnostic.GetMessage().Should().Contain($"DefaultValue '{defaultValue}'");
		run.InputCompilation.SyntaxTrees.Single(t => t.FilePath == diagnostic.Location.GetLineSpan().Path).GetText().ToString(diagnostic.Location.SourceSpan)
			.Should().Be($"DefaultValue = {defaultValue}");
	}

	[TestMethod]
	public async Task When_Requested_ChangedCallback_Is_Missing()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(ChangedCallback = true)]
			public partial int MyValue { get; set; }
			"""));

		run.ShouldReportSingle("UnoInternal0020");
	}

	[TestMethod]
	public async Task When_Conventional_ChangedCallback_Is_Invalid()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public partial int MyValue { get; set; }

			private void OnMyValueChanged(string unrelated) { }
			"""));

		run.ShouldReportSingle("UnoInternal0020").GetMessage().Should().Contain("'OnMyValueChanged'");
	}

	[TestMethod]
	public async Task When_Attached_ChangedCallback_Is_Instance_Method()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public static partial int GetMyValue(DependencyObject target);

			private void OnMyValueChanged(DependencyPropertyChangedEventArgs args) { }
			"""));

		run.ShouldReportSingle("UnoInternal0020");
	}

	[TestMethod]
	public async Task When_Coerce_Callback_Is_Invalid()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(CoerceCallback = true)]
			public partial int MyValue { get; set; }

			private int CoerceMyValue(int baseValue) => baseValue;
			"""));

		run.ShouldReportSingle("UnoInternal0021");
	}

	private const string UnrelatedTypesSource = """
		using TestHelpers;

		namespace Mynamespace
		{
			public partial class Unrelated : TestDependencyObject
			{
			}

			public partial class Target : TestDependencyObject
			{
			}
		}
		""";

	[TestMethod]
	[DataRow("[GeneratedDependencyProperty(ChangedCallback = true)]", "private static void OnMyValueChanged(Unrelated sender, DependencyPropertyChangedEventArgs args) { }", "UnoInternal0020")]
	[DataRow("[GeneratedDependencyProperty]", "private static void OnMyValueChanged(Unrelated sender, DependencyPropertyChangedEventArgs args) { }", "UnoInternal0020")]
	[DataRow("[GeneratedDependencyProperty(CoerceCallback = true)]", "private static object CoerceMyValue(Unrelated sender, object baseValue) => baseValue;", "UnoInternal0021")]
	[DataRow("[GeneratedDependencyProperty]", "private static object CoerceMyValue(Unrelated sender, object baseValue) => baseValue;", "UnoInternal0021")]
	public async Task When_Static_Callback_Sender_Is_Unrelated_To_Containing_Type(string attribute, string callback, string id)
	{
		var run = await RunAsync(
			InstanceType($$"""
				{{attribute}}
				public partial int MyValue { get; set; }

				{{callback}}
				"""),
			UnrelatedTypesSource);

		run.ShouldReportSingle(id);
	}

	[TestMethod]
	[DataRow("private static void OnMyValueChanged(Unrelated target, DependencyPropertyChangedEventArgs args) { }", "UnoInternal0020")]
	[DataRow("private static object CoerceMyValue(Unrelated target, object baseValue) => baseValue;", "UnoInternal0021")]
	public async Task When_Attached_Callback_Sender_Is_Unrelated_To_Target_Type(string callback, string id)
	{
		var run = await RunAsync(
			StaticType($$"""
				[GeneratedDependencyProperty]
				public static partial int GetMyValue(Target target);

				{{callback}}
				"""),
			UnrelatedTypesSource);

		run.ShouldReportSingle(id);
	}

	[TestMethod]
	[DataRow("private static void OnMyValueChanged(object target, DependencyPropertyChangedEventArgs args) { }", "OnMyValueChanged(instance, args)")]
	[DataRow("private static void OnMyValueChanged(TestDependencyObject target, DependencyPropertyChangedEventArgs args) { }", "OnMyValueChanged((global::TestHelpers.TestDependencyObject)instance, args)")]
	[DataRow("private static object CoerceMyValue(Target target, object baseValue) => baseValue;", "CoerceMyValue((global::Mynamespace.Target)instance, baseValue)")]
	public async Task When_Attached_Callback_Sender_Is_Target_Or_Base_Type(string callback, string invocation)
	{
		var run = await RunAsync(
			StaticType($$"""
				[GeneratedDependencyProperty]
				public static partial int GetMyValue(Target target);

				{{callback}}
				"""),
			UnrelatedTypesSource);

		run.ShouldSucceed().ShouldContain(HintName, invocation);
	}

	[TestMethod]
	public async Task When_Attached_LocalCache_Without_Owner()
	{
		var run = await RunAsync(StaticType("""
			[GeneratedDependencyProperty(LocalCache = true)]
			public static partial int GetMyValue(DependencyObject target);
			"""));

		run.ShouldReportSingle("UnoInternal0022");
	}

	[TestMethod]
	public async Task When_Backing_Field_Owner_Is_Not_Partial()
	{
		var run = await RunAsync(StaticType("""
			[GeneratedDependencyProperty(AttachedBackingFieldOwner = typeof(TestDependencyObject))]
			public static partial int GetMyValue(DependencyObject target);
			"""));

		run.ShouldReportSingle("UnoInternal0023");
	}

	[TestMethod]
	[DataRow("typeof(int[])", "DependencyObject")]
	[DataRow("typeof(Unrelated)", "Target")]
	public async Task When_Backing_Field_Owner_Cannot_Hold_The_Target(string owner, string targetType)
	{
		var run = await RunAsync(
			StaticType($$"""
				[GeneratedDependencyProperty(AttachedBackingFieldOwner = {{owner}})]
				public static partial int GetMyValue({{targetType}} target);
				"""),
			UnrelatedTypesSource);

		var diagnostic = run.ShouldReportSingle("UnoInternal0023");
		run.InputCompilation.SyntaxTrees.Single(t => t.FilePath == diagnostic.Location.GetLineSpan().Path).GetText().ToString(diagnostic.Location.SourceSpan)
			.Should().Be($"AttachedBackingFieldOwner = {owner}");
	}

	[TestMethod]
	public async Task When_WeakStorage_With_LocalCache()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.WeakStorage)]
			public partial object MyValue { get; set; }
			"""));

		run.ShouldReportSingle("UnoInternal0024");
	}

	[TestMethod]
	public async Task When_WeakStorage_Without_LocalCache()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.WeakStorage, LocalCache = false)]
			public partial object MyValue { get; set; }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "options: global::Microsoft.UI.Xaml.FrameworkPropertyMetadataOptions.WeakStorage,");
	}
}
