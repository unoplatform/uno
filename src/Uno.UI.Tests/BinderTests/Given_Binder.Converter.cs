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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

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
				new Windows.UI.Xaml.Data.Binding()
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
