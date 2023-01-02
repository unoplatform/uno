using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	public partial class Given_Binder_TestNullValue
	{
		[TestMethod]
		public void When_Converted_Value_NotNull()
		{
			var target = new MyControl();
			var viewModel = new ViewModel();
			var converter = new IdentityConverter();
			var targetNullValue = "this is the targetNullValue";

			target.DataContext = viewModel;
			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Windows.UI.Xaml.Data.Binding(nameof(viewModel.NotNullValue))
				{
					Converter = converter,
					TargetNullValue = targetNullValue,
				}
			);

			Assert.AreEqual(viewModel.NotNullValue, target.MyProperty);

			Assert.AreEqual(1, converter.ConvertHitCount);
		}

		[TestMethod]
		public void When_Converted_Value_Null()
		{
			var target = new MyControl();
			var viewModel = new ViewModel();
			var converter = new IdentityConverter();
			var targetNullValue = "this is the targetNullValue";

			target.DataContext = viewModel;
			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Windows.UI.Xaml.Data.Binding(nameof(viewModel.NullValue))
				{
					Converter = converter,
					TargetNullValue = targetNullValue,
				}
			);

			Assert.AreEqual(targetNullValue, target.MyProperty);

			Assert.AreEqual(
				2,
				converter.ConvertHitCount,
				"The converted is called first to check whether TargetNullValue should be called, then once more to convert TargetNullValue itself");
		}

		[TestMethod]
		public void When_Value_Null_Converted_Value_Not_Null()
		{
			var target = new MyControl();
			var viewModel = new ViewModel();
			var converter = new NeverNullConverter();
			var targetNullValue = "this is the targetNullValue";

			target.DataContext = viewModel;
			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Windows.UI.Xaml.Data.Binding(nameof(viewModel.NullValue))
				{
					Converter = converter,
					TargetNullValue = targetNullValue,
				}
			);

			Assert.AreEqual(
				NeverNullConverter.ConverterNullReplacement,
				target.MyProperty,
				"TargetNullValue is only called when the converted value is null, not necessarily when the value itself is null");

			Assert.AreEqual(1, converter.ConvertHitCount);
		}

		public abstract class TestConverter : IValueConverter
		{
			public int ConvertHitCount { get; set; }
			public abstract object Convert(object value, Type targetType, object parameter, string culture);
			public abstract object ConvertBack(object value, Type targetType, object parameter, string culture);
		}

		public class IdentityConverter : TestConverter
		{
			public override object Convert(object value, Type targetType, object parameter, string culture)
			{
				ConvertHitCount++;
				return value;
			}

			public override object ConvertBack(object value, Type targetType, object parameter, string culture)
			{
				return value;
			}
		}

		public class NeverNullConverter : TestConverter
		{
			public const string ConverterNullReplacement = "converted from null";

			public override object Convert(object value, Type targetType, object parameter, string culture)
			{
				ConvertHitCount++;
				return value ?? ConverterNullReplacement;
			}

			public override object ConvertBack(object value, Type targetType, object parameter, string culture)
			{
				return value;
			}
		}

		public class ViewModel : System.ComponentModel.INotifyPropertyChanged
		{
			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }

			public string NullValue => null;

			public string NotNullValue => "hello";
		}

		public partial class MyControl : DependencyObject
		{
			public MyControl(MyControl parent = null)
			{
				this.SetParent(parent);
			}

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
