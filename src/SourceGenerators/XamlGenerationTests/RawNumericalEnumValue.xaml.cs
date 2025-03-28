using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace XamlGenerationTests.Shared
{
	public partial class RawNumericalEnumValue : UserControl
	{
	}

	public partial class RawNumericalEnumValueTest : Control
	{
		public enum MyEnum { }
		public enum MyUnsignedEnum : uint { }
		[Flags]
		public enum MyFlagEnum
		{
			None = 0,
			Qwe = 1 << 0,
			Asd = 1 << 1,
			Zxc = 1 << 2,
		}

		#region DependencyProperty: SomeEnumProperty

		public static DependencyProperty SomeEnumPropertyProperty { get; } = DependencyProperty.Register(
			nameof(SomeEnumProperty),
			typeof(MyEnum),
			typeof(RawNumericalEnumValue),
			new PropertyMetadata(default(MyEnum)));

		public MyEnum SomeEnumProperty
		{
			get => (MyEnum)GetValue(SomeEnumPropertyProperty);
			set => SetValue(SomeEnumPropertyProperty, value);
		}

		#endregion
		#region DependencyProperty: SomeUnsignedEnumProperty

		public static DependencyProperty SomeUnsignedEnumPropertyProperty { get; } = DependencyProperty.Register(
			nameof(SomeUnsignedEnumProperty),
			typeof(MyUnsignedEnum),
			typeof(RawNumericalEnumValue),
			new PropertyMetadata(default(MyUnsignedEnum)));

		public MyUnsignedEnum SomeUnsignedEnumProperty
		{
			get => (MyUnsignedEnum)GetValue(SomeUnsignedEnumPropertyProperty);
			set => SetValue(SomeUnsignedEnumPropertyProperty, value);
		}

		#endregion
		#region DependencyProperty: SomeFlagEnumProperty

		public static DependencyProperty SomeFlagEnumPropertyProperty { get; } = DependencyProperty.Register(
			nameof(SomeFlagEnumProperty),
			typeof(MyFlagEnum),
			typeof(RawNumericalEnumValue),
			new PropertyMetadata(default(MyFlagEnum)));

		public MyFlagEnum SomeFlagEnumProperty
		{
			get => (MyFlagEnum)GetValue(SomeFlagEnumPropertyProperty);
			set => SetValue(SomeFlagEnumPropertyProperty, value);
		}

		#endregion
	}
}
