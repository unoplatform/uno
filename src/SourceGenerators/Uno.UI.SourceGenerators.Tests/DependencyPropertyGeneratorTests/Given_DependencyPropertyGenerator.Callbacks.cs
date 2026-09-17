using static Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests.DependencyPropertyGeneratorHarness;

namespace Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests;

public partial class Given_DependencyPropertyGenerator
{
	private static string InstanceType(string members) => $$"""
		using Microsoft.UI.Xaml;
		using TestHelpers;
		using Uno.UI.Xaml;

		namespace Mynamespace
		{
			public partial class C : TestDependencyObject
			{
				{{members}}
			}
		}
		""";

	private static string StaticType(string members) => $$"""
		using Microsoft.UI.Xaml;
		using TestHelpers;
		using Uno.UI.Xaml;

		namespace Mynamespace
		{
			public static partial class C
			{
				{{members}}
			}
		}
		""";

	[TestMethod]
	public async Task When_ChangedCallback_Takes_Sender_And_Args()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(ChangedCallback = true)]
			public partial int MyValue { get; set; }

			private void OnMyValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) { }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "propertyChangedCallback: static (instance, args) => ((global::Mynamespace.C)instance).OnMyValueChanged(instance, args),");
	}

	[TestMethod]
	public async Task When_ChangedCallback_Is_Static()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(ChangedCallback = true)]
			public partial int MyValue { get; set; }

			private static void OnMyValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) { }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "propertyChangedCallback: static (instance, args) => global::Mynamespace.C.OnMyValueChanged(instance, args),");
	}

	[TestMethod]
	public async Task When_ChangedCallback_Sender_Is_Derived_Type()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(ChangedCallback = true)]
			public partial int MyValue { get; set; }

			private static void OnMyValueChanged(C sender, DependencyPropertyChangedEventArgs args) { }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "global::Mynamespace.C.OnMyValueChanged((global::Mynamespace.C)instance, args)");
	}

	[TestMethod]
	public async Task When_ChangedCallback_Is_Partial_Void()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(ChangedCallback = true)]
			public partial int MyValue { get; set; }

			partial void OnMyValueChanged(DependencyPropertyChangedEventArgs args);
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "propertyChangedCallback: static (instance, args) => ((global::Mynamespace.C)instance).OnMyValueChanged(args),");
	}

	[TestMethod]
	public async Task When_ChangedCallback_Is_Virtual_With_Old_And_New_Values()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(ChangedCallback = true)]
			public partial string MyValue { get; set; }

			protected virtual void OnMyValueChanged(string? oldValue, string? newValue) { }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "((global::Mynamespace.C)instance).OnMyValueChanged((string)args.OldValue, (string)args.NewValue)");
	}

	[TestMethod]
	public async Task When_ChangedCallbackName_Is_Set()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnAnyPropertyChanged))]
			public partial int MyValue { get; set; }

			private void OnAnyPropertyChanged() { }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "propertyChangedCallback: static (instance, args) => ((global::Mynamespace.C)instance).OnAnyPropertyChanged(),");
	}

	[TestMethod]
	public async Task When_ChangedCallback_Is_Found_By_Convention()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public partial int MyValue { get; set; }

			private void OnMyValueChanged() { }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "propertyChangedCallback: static (instance, args) => ((global::Mynamespace.C)instance).OnMyValueChanged(),");
	}

	[TestMethod]
	public async Task When_No_ChangedCallback()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public partial int MyValue { get; set; }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "propertyChangedCallback: null,");
	}

	[TestMethod]
	public async Task When_ChangedCallback_Has_An_Unsupported_Overload()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(ChangedCallback = true)]
			public partial bool IsOn { get; set; }

			private void OnIsOnChanged(string unrelated) { }

			private void OnIsOnChanged() { }

			private void OnIsOnChanged(bool oldValue, bool newValue) { }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "((global::Mynamespace.C)instance).OnIsOnChanged((bool)args.OldValue, (bool)args.NewValue)");
	}

	[TestMethod]
	public async Task When_ChangedCallback_Overloads_Prefer_Args()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public partial bool IsOn { get; set; }

			private void OnIsOnChanged(bool oldValue, bool newValue) { }

			private void OnIsOnChanged(DependencyPropertyChangedEventArgs args) { }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "((global::Mynamespace.C)instance).OnIsOnChanged(args)");
	}

	[TestMethod]
	public async Task When_Attached_ChangedCallback_Takes_Target_Type()
	{
		var run = await RunAsync(StaticType("""
			[GeneratedDependencyProperty]
			public static partial int GetMyValue(TestDependencyObject target);

			private static void OnMyValueChanged(TestDependencyObject target, DependencyPropertyChangedEventArgs args) { }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "global::Mynamespace.C.OnMyValueChanged((global::TestHelpers.TestDependencyObject)instance, args)");
	}

	[TestMethod]
	public async Task When_Coerce_Takes_Base_Value()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(CoerceCallback = true)]
			public partial int MyValue { get; set; }

			private object CoerceMyValue(object baseValue) => baseValue;
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "coerceValueCallback: static (instance, baseValue, precedence) => ((global::Mynamespace.C)instance).CoerceMyValue(baseValue),");
	}

	[TestMethod]
	public async Task When_Coerce_Takes_Precedence()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public partial int MyValue { get; set; }

			private protected virtual object CoerceMyValue(object baseValue, DependencyPropertyValuePrecedences precedence) => baseValue;
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "coerceValueCallback: static (instance, baseValue, precedence) => ((global::Mynamespace.C)instance).CoerceMyValue(baseValue, precedence),");
	}

	[TestMethod]
	public async Task When_Coerce_Is_Static()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(CoerceCallback = true)]
			public partial int MyValue { get; set; }

			private static object CoerceMyValue(DependencyObject instance, object baseValue) => baseValue;
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "coerceValueCallback: static (instance, baseValue, precedence) => global::Mynamespace.C.CoerceMyValue(instance, baseValue),");
	}

	[TestMethod]
	public async Task When_Attached_Coerce()
	{
		var run = await RunAsync(StaticType("""
			[GeneratedDependencyProperty(CoerceCallback = true)]
			public static partial int GetMyValue(DependencyObject target);

			private static object CoerceMyValue(DependencyObject target, int baseValue, DependencyPropertyValuePrecedences precedence) => baseValue + 1;
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "coerceValueCallback: static (instance, baseValue, precedence) => global::Mynamespace.C.CoerceMyValue(instance, (int)baseValue, precedence),");
	}

	[TestMethod]
	public async Task When_Attached_Coerce_Takes_Base_Value_Only()
	{
		var run = await RunAsync(StaticType("""
			[GeneratedDependencyProperty]
			public static partial int GetMyValue(DependencyObject target);

			private static object CoerceMyValue(DependencyObject target, object baseValue) => baseValue;
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "coerceValueCallback: static (instance, baseValue, precedence) => global::Mynamespace.C.CoerceMyValue(instance, baseValue),");
	}
}
