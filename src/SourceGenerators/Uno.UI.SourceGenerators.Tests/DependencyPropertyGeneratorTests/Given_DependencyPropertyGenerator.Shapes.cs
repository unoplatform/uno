using static Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests.DependencyPropertyGeneratorHarness;

namespace Uno.UI.SourceGenerators.Tests.DependencyPropertyGeneratorTests;

public partial class Given_DependencyPropertyGenerator
{
	[TestMethod]
	public async Task When_Property_Is_Get_Only()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(LocalCache = false)]
			public partial int MyValue { get; }
			"""));

		run.ShouldSucceed()
			.ShouldContain(HintName, "public partial int MyValue { get => (int)GetValue(MyValueProperty); }")
			.ShouldNotContain(HintName, "set =>");
	}

	[TestMethod]
	public async Task When_Property_Has_Accessor_Modifiers()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			internal partial int First { get; private set; }

			[GeneratedDependencyProperty]
			protected internal partial int Second { get; protected set; }

			[GeneratedDependencyProperty]
			partial int Third { get; set; }
			"""));

		run.ShouldSucceed().ShouldContain(
			HintName,
			"internal static global::Microsoft.UI.Xaml.DependencyProperty FirstProperty { get; }",
			"internal partial int First {",
			"private set => SetValue(FirstProperty, value);",
			"protected internal static global::Microsoft.UI.Xaml.DependencyProperty SecondProperty { get; }",
			"protected internal partial int Second {",
			"protected set => SetValue(SecondProperty, value);",
			"} static global::Microsoft.UI.Xaml.DependencyProperty ThirdProperty { get; }",
			"partial int Third {");
	}

	[TestMethod]
	public async Task When_Property_Is_Virtual()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty]
			public virtual partial int MyValue { get; set; }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "public virtual partial int MyValue");
	}

	[TestMethod]
	public async Task When_New_Property_Hides_Base_Identifier()
	{
		var run = await RunAsync(
			"""
			using Microsoft.UI.Xaml;
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace Mynamespace
			{
				public class Base : TestDependencyObject
				{
					public static DependencyProperty MyValueProperty { get; } = DependencyProperty.Register("MyValue", typeof(int), typeof(Base), new PropertyMetadata(0));

					public int MyValue { get; set; }
				}

				public partial class C : Base
				{
					[GeneratedDependencyProperty]
					public new partial int MyValue { get; set; }
				}
			}
			""");

		run.ShouldSucceed().ShouldContain(
			HintName,
			"public new static global::Microsoft.UI.Xaml.DependencyProperty MyValueProperty { get; } = CreateMyValueProperty();",
			"public new partial int MyValue");
	}

	[TestMethod]
	public async Task When_Base_Identifier_Is_Generated()
	{
		var run = await RunAsync(
			"""
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace Mynamespace
			{
				public partial class Base : TestDependencyObject
				{
					[GeneratedDependencyProperty]
					protected partial int MyValue { get; set; }
				}

				public partial class C : Base
				{
					[GeneratedDependencyProperty]
					public new partial int MyValue { get; set; }
				}
			}
			""");

		run.ShouldSucceed()
			.ShouldContain(HintName, "public new static global::Microsoft.UI.Xaml.DependencyProperty MyValueProperty { get; }")
			.ShouldContain("Mynamespace.Base.g.cs", "protected static global::Microsoft.UI.Xaml.DependencyProperty MyValueProperty { get; }");
	}

	[TestMethod]
	public async Task When_Base_Identifier_Is_Private()
	{
		var run = await RunAsync(
			"""
			using Microsoft.UI.Xaml;
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace Mynamespace
			{
				public class Base : TestDependencyObject
				{
					private static DependencyProperty MyValueProperty { get; } = null!;
				}

				public partial class C : Base
				{
					[GeneratedDependencyProperty]
					public partial int MyValue { get; set; }
				}
			}
			""");

		run.ShouldSucceed().ShouldContain(HintName, "public static global::Microsoft.UI.Xaml.DependencyProperty MyValueProperty { get; }");
	}

	[TestMethod]
	public async Task When_Explicit_Identifier_Is_New()
	{
		var run = await RunAsync(
			"""
			using Microsoft.UI.Xaml;
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace Mynamespace
			{
				public class Base : TestDependencyObject
				{
					public static DependencyProperty MyValueProperty { get; } = null!;
				}

				public partial class C : Base
				{
					public new static partial DependencyProperty MyValueProperty { get; }

					[GeneratedDependencyProperty]
					internal partial int MyValue { get; set; }
				}
			}
			""");

		run.ShouldSucceed().ShouldContain(
			HintName,
			"private static readonly global::Microsoft.UI.Xaml.DependencyProperty __MyValueProperty = CreateMyValueProperty();",
			"public new static partial global::Microsoft.UI.Xaml.DependencyProperty MyValueProperty { get => __MyValueProperty; }");
	}

	[TestMethod]
	public async Task When_LocalCache_Is_Disabled()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(LocalCache = false)]
			public partial bool IsOn { get; set; }
			"""));

		run.ShouldSucceed()
			.ShouldContain(HintName, "get => (bool)GetValue(IsOnProperty);", "backingFieldUpdateCallback: null));")
			.ShouldNotContain(HintName, "__generatedDependencyPropertyFlags", "BackingFieldUpdate(");
	}

	[TestMethod]
	public async Task When_More_Than_32_Flags()
	{
		var members = string.Join(
			"\n",
			Enumerable.Range(0, 20).Select(i => $"[GeneratedDependencyProperty] public partial bool Flag{i} {{ get; set; }}"));

		var run = await RunAsync(InstanceType(members));

		run.ShouldSucceed().ShouldContain(
			HintName,
			"private uint __generatedDependencyPropertyFlags0; private uint __generatedDependencyPropertyFlags1;",
			// Flag15 uses bits 30 and 31, Flag16 starts the second field.
			"if ((__generatedDependencyPropertyFlags0 & (1u << 30)) == 0)",
			"__generatedDependencyPropertyFlags0 |= (1u << 31);",
			"if ((__generatedDependencyPropertyFlags1 & (1u << 0)) == 0)",
			"return (__generatedDependencyPropertyFlags1 & (1u << 7)) != 0;");

		run.ShouldNotContain(HintName, "__generatedDependencyPropertyFlags2", "1u << 32");
	}

	[TestMethod]
	public async Task When_Bool_Cache_Bits_Straddle_Two_Fields()
	{
		var members = string.Join(
			"\n",
			Enumerable.Range(0, 31).Select(i => $"[GeneratedDependencyProperty] public partial int Value{i} {{ get; set; }}")
				.Append("[GeneratedDependencyProperty] public partial bool IsOn { get; set; }"));

		var run = await RunAsync(InstanceType(members));

		run.ShouldSucceed().ShouldContain(
			HintName,
			"if ((__generatedDependencyPropertyFlags0 & (1u << 31)) == 0) { if ((bool)GetValue(IsOnProperty)) { __generatedDependencyPropertyFlags1 |= (1u << 0); }",
			"return (__generatedDependencyPropertyFlags1 & (1u << 0)) != 0;");
	}

	[TestMethod]
	public async Task When_Setter_Uses_Discovered_Box_Overload()
	{
		var run = await RunAsync(
			"""
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace Uno.UI.Helpers.Boxes
			{
				internal static class Boxer
				{
					public static object Box(global::Mynamespace.MyEnum value) => value;
				}
			}

			namespace Mynamespace
			{
				public enum MyEnum { First, Second }

				public partial class C : TestDependencyObject
				{
					[GeneratedDependencyProperty(DefaultValue = MyEnum.Second)]
					public partial MyEnum MyValue { get; set; }

					[GeneratedDependencyProperty]
					public partial int Other { get; set; }
				}
			}
			""");

		run.ShouldSucceed()
			.ShouldContain(
				HintName,
				"set => SetValue(MyValueProperty, global::Uno.UI.Helpers.Boxes.Boxer.Box(value));",
				"defaultValue: global::Uno.UI.Helpers.Boxes.Boxer.Box(global::Mynamespace.MyEnum.Second),",
				"set => SetValue(OtherProperty, value);",
				"defaultValue: 0,");
	}

	[TestMethod]
	public async Task When_Default_Value_Uses_Discovered_Cached_Box()
	{
		var run = await RunAsync(
			"""
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace Uno.UI.Helpers.Boxes
			{
				internal static class IntBoxes
				{
					public static readonly object Zero = 0;
				}

				internal static class Boxer
				{
				}
			}

			namespace Mynamespace
			{
				public partial class C : TestDependencyObject
				{
					[GeneratedDependencyProperty]
					public partial int MyValue { get; set; }

					[GeneratedDependencyProperty(DefaultValue = true)]
					public partial bool Other { get; set; }
				}
			}
			""");

		run.ShouldSucceed()
			.ShouldContain(
				HintName,
				"defaultValue: global::Uno.UI.Helpers.Boxes.IntBoxes.Zero,",
				"set => SetValue(MyValueProperty, value);",
				"defaultValue: true,");
	}

	[TestMethod]
	public async Task When_Attached_Property_Has_Hand_Written_Setter()
	{
		var run = await RunAsync(StaticType("""
			[GeneratedDependencyProperty(DefaultValue = 1)]
			public static partial int GetSpan(DependencyObject view);

			public static void SetSpan(DependencyObject view, int span)
			{
				if (span <= 0)
				{
					throw new global::System.ArgumentException("The value must be above zero", nameof(span));
				}

				SetSpanValue(view, span);
			}
			"""));

		run.ShouldSucceed()
			.ShouldContain(
				HintName,
				"public static partial int GetSpan(global::Microsoft.UI.Xaml.DependencyObject view) => GetSpanValue(view);",
				"private static int GetSpanValue(global::Microsoft.UI.Xaml.DependencyObject instance) => (int)instance.GetValue(SpanProperty);",
				"private static void SetSpanValue(global::Microsoft.UI.Xaml.DependencyObject instance, int value) => instance.SetValue(SpanProperty, value);",
				"backingFieldUpdateCallback: null));")
			.ShouldNotContain(HintName, "void SetSpan(");
	}

	[TestMethod]
	public async Task When_Attached_Property_Has_No_Setter()
	{
		var run = await RunAsync(StaticType("""
			[GeneratedDependencyProperty]
			internal static partial string GetLabel(DependencyObject target);
			"""));

		run.ShouldSucceed().ShouldContain(
			HintName,
			"internal static global::Microsoft.UI.Xaml.DependencyProperty LabelProperty { get; } = CreateLabelProperty();",
			"internal static partial string GetLabel(global::Microsoft.UI.Xaml.DependencyObject target) => GetLabelValue(target);",
			"private static void SetLabelValue(global::Microsoft.UI.Xaml.DependencyObject instance, string value)");
	}

	[TestMethod]
	public async Task When_Attached_Property_Is_Extension()
	{
		var run = await RunAsync(StaticType("""
			[GeneratedDependencyProperty]
			public static partial int GetMyValue(this DependencyObject target);

			public static partial void SetMyValue(this DependencyObject target, int value);
			"""));

		run.ShouldSucceed().ShouldContain(
			HintName,
			"public static partial int GetMyValue(this global::Microsoft.UI.Xaml.DependencyObject target) => GetMyValueValue(target);",
			"public static partial void SetMyValue(this global::Microsoft.UI.Xaml.DependencyObject target, int value) => SetMyValueValue(target, value);");
	}

	[TestMethod]
	public async Task When_Attached_Property_Is_In_DependencyObject()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(AttachedBackingFieldOwner = typeof(C))]
			public static partial bool GetIsOn(C target);

			public static partial void SetIsOn(C target, bool value);
			"""));

		run.ShouldSucceed().ShouldContain(
			HintName,
			"if (instance is global::Mynamespace.C backingFieldOwner)",
			"internal bool __Mynamespace_C_IsOnPropertyBackingField; internal bool __Mynamespace_C_IsOnPropertyBackingFieldSet;");
	}

	[TestMethod]
	public async Task When_Attached_Backing_Field_Owner_Is_Nested()
	{
		var run = await RunAsync(
			"""
			using Microsoft.UI.Xaml;
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace Owners
			{
				public partial class Outer
				{
					public partial class Owner : TestDependencyObject
					{
					}
				}
			}

			namespace Mynamespace
			{
				public static partial class C
				{
					[GeneratedDependencyProperty(AttachedBackingFieldOwner = typeof(Owners.Outer.Owner))]
					public static partial int GetMyValue(DependencyObject target);
				}
			}
			""");

		run.ShouldSucceed().ShouldContain(
			HintName,
			"namespace Owners { partial class Outer { partial class Owner { internal int __Mynamespace_C_MyValuePropertyBackingField;",
			"if (instance is global::Owners.Outer.Owner backingFieldOwner)");
	}

	[TestMethod]
	public async Task When_Containing_Type_Is_Nested_And_Generic()
	{
		var run = await RunAsync(
			"""
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace Mynamespace
			{
				public partial record Outer
				{
					public partial class Inner<T> : TestDependencyObject
					{
						[GeneratedDependencyProperty(ChangedCallback = true)]
						public partial T? MyValue { get; set; }

						private void OnMyValueChanged(T? oldValue, T? newValue) { }
					}
				}
			}
			""");

		run.ShouldSucceed().ShouldContain(
			"Mynamespace.Outer.Inner-1.g.cs",
			"namespace Mynamespace { partial record Outer { partial class Inner<T> {",
			"public partial T MyValue",
			"typeof(global::Mynamespace.Outer.Inner<T>),",
			"((global::Mynamespace.Outer.Inner<T>)instance).OnMyValueChanged((T)args.OldValue, (T)args.NewValue)",
			"defaultValue: default(T),");
	}

	[TestMethod]
	public async Task When_Containing_Type_Is_In_Global_Namespace()
	{
		var run = await RunAsync(
			"""
			using TestHelpers;
			using Uno.UI.Xaml;

			public partial class GlobalType : TestDependencyObject
			{
				[GeneratedDependencyProperty]
				public partial int MyValue { get; set; }
			}
			""");

		run.ShouldSucceed();
		run.HintNames.Should().Equal("GlobalType.g.cs");
		run.Source("GlobalType.g.cs").Should().NotContain("namespace");
		run.ShouldContain("GlobalType.g.cs", "#nullable disable partial class GlobalType {", "typeof(global::GlobalType)");
	}

	[TestMethod]
	public async Task When_Namespace_And_Type_Names_Are_Keywords()
	{
		var run = await RunAsync(
			"""
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace N.@event
			{
				public partial class @class : TestDependencyObject
				{
					[GeneratedDependencyProperty]
					public partial int X { get; set; }
				}

				public partial class Other : TestDependencyObject
				{
					[GeneratedDependencyProperty]
					public partial int Y { get; set; }
				}
			}
			""");

		run.ShouldSucceed();
		run.HintNames.Should().BeEquivalentTo("N.event.class.g.cs", "N.event.Other.g.cs");
		run.ShouldContain("N.event.class.g.cs", "namespace N.@event { partial class @class {", "typeof(global::N.@event.@class)");
	}

	[TestMethod]
	public async Task When_Type_Names_Differ_Only_By_Case()
	{
		const string Upper = """
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace N
			{
				public partial class Foo : TestDependencyObject
				{
					[GeneratedDependencyProperty]
					public partial int A { get; set; }
				}
			}
			""";

		const string Lower = """
			using TestHelpers;
			using Uno.UI.Xaml;

			namespace N
			{
				public partial class foo : TestDependencyObject
				{
					[GeneratedDependencyProperty]
					public partial int A { get; set; }
				}

				public partial class Bar : TestDependencyObject
				{
					[GeneratedDependencyProperty]
					public partial int B { get; set; }
				}
			}
			""";

		var run = await RunAsync(Upper, Lower);
		var reversedRun = await RunAsync(Lower, Upper);

		run.ShouldSucceed();
		reversedRun.ShouldSucceed();

		var hintNames = run.HintNames.ToArray();
		hintNames.Should().HaveCount(3).And.OnlyHaveUniqueItems(name => name.ToUpperInvariant());
		hintNames.Should().Contain("N.Bar.g.cs");
		reversedRun.HintNames.Should().BeEquivalentTo(hintNames, "hint names shouldn't depend on declaration order");

		var upperHintName = hintNames.Single(name => run.Source(name).Contains("partial class Foo"));
		var lowerHintName = hintNames.Single(name => run.Source(name).Contains("partial class foo"));
		upperHintName.Should().StartWith("N.Foo.");
		lowerHintName.Should().StartWith("N.foo.");
		reversedRun.Source(upperHintName).Should().Contain("partial class Foo");
	}

	[TestMethod]
	public async Task When_Property_Type_Is_Nullable_Reference()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(DefaultValue = null)]
			public partial string? MyValue { get; set; }

			[GeneratedDependencyProperty]
			public partial int? Count { get; set; }
			"""));

		run.ShouldSucceed().ShouldContain(HintName, "public partial string MyValue", "public partial int? Count");
	}

	[TestMethod]
	public async Task When_Invalid_Property_Does_Not_Block_Others()
	{
		var run = await RunAsync(InstanceType("""
			[GeneratedDependencyProperty(ChangedCallback = true)]
			public partial int Broken { get; set; }

			[GeneratedDependencyProperty]
			public partial int Valid { get; set; }
			"""));

		run.ShouldReportSingle("UnoInternal0020");
		run.ShouldContain(HintName, "public partial int Valid").ShouldNotContain(HintName, "Broken");
	}
}
