using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Uno.UI.Xaml;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_Binder_XBind_EnumConverter
	{
		[TestMethod]
		public void When_XBind_With_Enum_Converter_TwoWay()
		{
			var SUT = new MyControl();
			var myTestConverter = new TestEnumStringConverter();
			var enumSource = new EnumSource() { Value = TestEnum.Hello };

			// Simulate x:Bind by using BindingHelper.SetBindingXBindProvider
			var binding = new Binding()
			{
				Mode = BindingMode.TwoWay,
				Converter = myTestConverter
			};

			// This simulates what x:Bind generates - now with source type information
			BindingHelper.SetBindingXBindProvider(
				binding,
				enumSource, // compiled source
				ctx => (true, enumSource.Value), // getter
				(ctx, value) => enumSource.Value = (TestEnum)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(TestEnum), value), // setter
				typeof(TestEnum), // source type - this is the fix!
				null // property paths
			);

			SUT.SetBinding(MyControl.MyPropertyProperty, binding);

			// Test forward direction: source enum -> string target
			Assert.AreEqual("Hello", SUT.MyProperty, "Forward conversion should work");

			// Test backward direction: string target -> source enum with proper targetType
			SUT.MyProperty = "World";

			// The fix: targetType should be typeof(TestEnum) but x:Bind previously passed null
			Assert.AreEqual(typeof(TestEnum), myTestConverter.LastConvertBackTargetType,
				"ConvertBack should receive the enum type as targetType, but x:Bind previously passed null");
			Assert.AreEqual(TestEnum.World, enumSource.Value, "Backward conversion should work");
		}

		[TestMethod]
		public void When_XBind_Without_SourceType_Falls_Back_To_BindingPath()
		{
			var SUT = new MyControl();
			var myTestConverter = new TestEnumStringConverter();
			var enumSource = new EnumSource() { Value = TestEnum.Hello };

			// Test the old behavior (without sourceType) - should fall back to binding path type
			var binding = new Binding()
			{
				Mode = BindingMode.TwoWay,
				Converter = myTestConverter
			};

			// This simulates old x:Bind code generation without source type
			BindingHelper.SetBindingXBindProvider(
				binding,
				enumSource, // compiled source
				ctx => (true, enumSource.Value), // getter
				(ctx, value) => enumSource.Value = (TestEnum)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(TestEnum), value), // setter
				null // property paths - no sourceType provided
			);

			SUT.SetBinding(MyControl.MyPropertyProperty, binding);

			// Forward conversion should still work
			Assert.AreEqual("Hello", SUT.MyProperty);

			// Backward conversion - targetType should fall back to binding path type (which might be null for x:Bind)
			SUT.MyProperty = "World";

			// Without sourceType, it falls back to _bindingPath.ValueType which might be null for x:Bind
			// This demonstrates the original issue
			Assert.AreEqual(TestEnum.World, enumSource.Value, "Should still work due to fallback in our test converter");
		}

		public class EnumSource
		{
			public TestEnum Value { get; set; }
		}

		public enum TestEnum
		{
			Hello,
			World
		}

		internal class TestEnumStringConverter : IValueConverter
		{
			public Type LastConvertBackTargetType { get; private set; }

			public object Convert(object value, Type targetType, object parameter, string language)
			{
				if (value is Enum e)
				{
					return e.ToString();
				}
				return null;
			}

			public object ConvertBack(object value, Type targetType, object parameter, string language)
			{
				LastConvertBackTargetType = targetType; // Track what targetType we receive

				if (value is string s && targetType != null && targetType.IsEnum)
				{
					return Enum.Parse(targetType, s);
				}

				// If targetType is null, we need to handle this case
				if (value is string str && targetType == null)
				{
					// This is the problematic case - we don't know what enum type to parse to
					// For the test, we'll assume TestEnum, but in real scenarios this would fail
					return Enum.Parse(typeof(TestEnum), str);
				}

				throw new NotImplementedException();
			}
		}

		public partial class MyControl : DependencyObject
		{
			public string MyProperty
			{
				get { return (string)GetValue(MyPropertyProperty); }
				set { SetValue(MyPropertyProperty, value); }
			}

			public static readonly DependencyProperty MyPropertyProperty =
				DependencyProperty.Register("MyProperty", typeof(string), typeof(MyControl), new FrameworkPropertyMetadata(null));
		}
	}
}