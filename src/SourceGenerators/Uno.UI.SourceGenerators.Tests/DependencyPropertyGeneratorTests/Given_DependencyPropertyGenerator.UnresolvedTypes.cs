using static Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests.DependencyPropertyGeneratorHarness;

namespace Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests;

/// <summary>
/// The compiler already reports unresolved types, so the generator reports nothing for them and still declares the
/// identifier. The WinAppSDK sync tool compiles without the Generated folders and would otherwise stub it again.
/// </summary>
public partial class Given_DependencyPropertyGenerator
{
	[TestMethod]
	public async Task When_Property_Type_And_DefaultValue_Are_Unresolved()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnFontStretchChanged), DefaultValue = FontStretch.Normal, Options = FrameworkPropertyMetadataOptions.Inherits)]
			public partial FontStretch FontStretch { get; set; }

			private protected virtual void OnFontStretchChanged(FontStretch oldValue, FontStretch newValue) { }
			"""));

		run.ShouldTolerateUnresolvedTypes("Mynamespace.C", "FontStretchProperty");
	}

	[TestMethod]
	[DataRow("""
		[GeneratedDependencyProperty(DefaultValue = Missing.Value)]
		public partial int MyValue { get; set; }
		""")]
	[DataRow("""
		[GeneratedDependencyProperty(DefaultValue = 1)]
		public partial Missing MyValue { get; set; }
		""")]
	[DataRow("""
		[GeneratedDependencyProperty(DefaultValue = null)]
		public partial Missing MyValue { get; set; }
		""")]
	[DataRow("""
		[GeneratedDependencyProperty(ChangedCallback = true)]
		public partial int MyValue { get; set; }

		private static void OnMyValueChanged(Missing sender, DependencyPropertyChangedEventArgs args) { }
		""")]
	[DataRow("""
		[GeneratedDependencyProperty(ChangedCallback = true)]
		public partial int MyValue { get; set; }

		private void OnMyValueChanged(MissingArgs args) { }
		""")]
	[DataRow("""
		[GeneratedDependencyProperty(CoerceCallback = true)]
		public partial int MyValue { get; set; }

		private static object CoerceMyValue(Missing sender, object baseValue) => baseValue;
		""")]
	[DataRow("""
		[GeneratedDependencyProperty(CoerceCallback = true)]
		public partial int MyValue { get; set; }

		private object CoerceMyValue(object baseValue, MissingPrecedences precedence) => baseValue;
		""")]
	public async Task When_Instance_Property_Involves_Unresolved_Types(string members)
	{
		var run = await RunAsync(InstanceType(members));

		run.ShouldTolerateUnresolvedTypes("Mynamespace.C", "MyValueProperty");
	}

	[TestMethod]
	[DataRow("""
		[GeneratedDependencyProperty]
		public static partial int GetMyValue(Missing target);

		public static partial void SetMyValue(Missing target, int value);
		""")]
	[DataRow("""
		[GeneratedDependencyProperty(AttachedBackingFieldOwner = typeof(Missing))]
		public static partial int GetMyValue(DependencyObject target);
		""")]
	[DataRow("""
		[GeneratedDependencyProperty(AttachedBackingFieldOwner = typeof(Missing), LocalCache = true)]
		public static partial int GetMyValue(DependencyObject target);
		""")]
	[DataRow("""
		[GeneratedDependencyProperty(ChangedCallback = true)]
		public static partial int GetMyValue(DependencyObject target);

		private static void OnMyValueChanged(Missing target, DependencyPropertyChangedEventArgs args) { }
		""")]
	public async Task When_Attached_Property_Involves_Unresolved_Types(string members)
	{
		var run = await RunAsync(StaticType(members));

		run.ShouldTolerateUnresolvedTypes("Mynamespace.C", "MyValueProperty");
	}

	[TestMethod]
	public async Task When_Base_Type_Is_Unresolved()
	{
		var run = await RunAsync(
			"""
			using Uno.UI.Xaml;

			namespace Mynamespace
			{
				public partial class C : MissingBase
				{
					[GeneratedDependencyProperty]
					public partial int MyValue { get; set; }
				}
			}
			""");

		run.ShouldTolerateUnresolvedTypes("Mynamespace.C", "MyValueProperty");
	}
}
