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
	[UITestClass]
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

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25250")]
		public void When_ConverterLanguage_Is_Set()
		{
			AssertConverterLanguage("de-DE-x-uno-25250", setConverterLanguage: true);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25250")]
		public void When_ConverterLanguage_Is_Not_Set()
		{
			AssertConverterLanguage(string.Empty, setConverterLanguage: false);
		}

		private static void AssertConverterLanguage(string expectedLanguage, bool setConverterLanguage)
		{
			var sut = new MyControl();
			var converter = new LanguageRecordingConverter();
			var source = new StringSource { Value = "hello" };
			var binding = new Binding
			{
				Path = new PropertyPath(nameof(StringSource.Value)),
				Converter = converter,
				Mode = BindingMode.TwoWay,
			};

			if (setConverterLanguage)
			{
				binding.ConverterLanguage = expectedLanguage;
			}

			sut.SetBinding(MyControl.MyPropertyProperty, binding);
			sut.DataContext = source;

			Assert.AreEqual(expectedLanguage, converter.ConvertLanguage);

			sut.MyProperty = "world";

			Assert.AreEqual(expectedLanguage, converter.ConvertBackLanguage);
			Assert.AreEqual("world", source.Value);
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

		internal class LanguageRecordingConverter : IValueConverter
		{
			public string? ConvertLanguage { get; private set; }
			public string? ConvertBackLanguage { get; private set; }

			public object Convert(object value, Type targetType, object parameter, string language)
			{
				ConvertLanguage = language;
				return value;
			}

			public object ConvertBack(object value, Type targetType, object parameter, string language)
			{
				ConvertBackLanguage = language;
				return value;
			}
		}

		public class StringSource
		{
			public string Value { get; set; } = string.Empty;
		}

		public partial class MyControl : FrameworkElement
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
