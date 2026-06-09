#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_Binder_Converter
	{
		[TestMethod]
		public void When_Converter_Returns_UnsetValue()
		{
			var SUT = new MyControl();

			var myTestConverter = new MyTestConverter();

			SUT.SetBinding(
				MyControl.MyPropertyProperty,
				new Microsoft.UI.Xaml.Data.Binding()
				{
					Path = new PropertyPath("."),
					Converter = myTestConverter,
					FallbackValue = "fallback value",
					TargetNullValue = "null value"
				}
			);

			myTestConverter.OutputValue = v => v;
			SUT.DataContext = "hello";
			Assert.AreEqual("hello", SUT.MyProperty);

			myTestConverter.OutputValue = v => DependencyProperty.UnsetValue;
			SUT.DataContext = "hello 3";
			Assert.AreEqual("fallback value", SUT.MyProperty);
		}

		[TestMethod]
		public void When_TwoWay_With_Enum()
		{
			var SUT = new MyControl();
			var myTestConverter = new EnumStringConverter();
			var enumSource = new EnumSource();

			SUT.SetBinding(
				MyControl.MyPropertyProperty,
				new Microsoft.UI.Xaml.Data.Binding()
				{
					Path = new PropertyPath("Value"),
					Converter = myTestConverter,
					Mode = BindingMode.TwoWay
				}
			);
			SUT.DataContext = enumSource;
			Assert.AreEqual("Hello", SUT.MyProperty);
			SUT.MyProperty = "World";
			Assert.AreEqual(TestEnum.World, enumSource.Value);
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

		internal class EnumStringConverter : IValueConverter
		{
			public object? Convert(object value, Type targetType, object parameter, string language)
			{
				if (value is Enum e)
				{
					return e.ToString();
				}
				return null;
			}
			public object ConvertBack(object value, Type targetType, object parameter, string language)
			{
				if (value is string s && targetType.IsEnum)
				{
					return Enum.Parse(targetType, s);
				}
				throw new NotImplementedException();
			}
		}

		internal class MyTestConverter : IValueConverter
		{
			public Func<object, object?> OutputValue { get; set; } = o => null;

			public object? Convert(object value, Type targetType, object parameter, string language) => OutputValue(value);

			public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
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
