using System;
using System.Collections.Generic;
using System.ComponentModel;
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
				new Microsoft.UI.Xaml.Data.Binding(nameof(viewModel.NotNullValue))
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
				new Microsoft.UI.Xaml.Data.Binding(nameof(viewModel.NullValue))
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
				new Microsoft.UI.Xaml.Data.Binding(nameof(viewModel.NullValue))
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

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25251")]
		public void When_TwoWay_Target_Equals_TargetNullValue_Source_Receives_Null()
		{
			var target = new MyControl();
			var viewModel = new EditableViewModel { Name = null };

			target.DataContext = viewModel;
			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Microsoft.UI.Xaml.Data.Binding(nameof(viewModel.Name))
				{
					Mode = BindingMode.TwoWay,
					TargetNullValue = "none",
					UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
				}
			);

			Assert.AreEqual("none", target.MyProperty);

			target.GetBindingExpression(MyControl.MyPropertyProperty).UpdateSource();

			Assert.IsNull(viewModel.Name);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25251")]
		public void When_TwoWay_Target_Differs_From_TargetNullValue_Source_Receives_Value()
		{
			var target = new MyControl();
			var viewModel = new EditableViewModel { Name = null };

			target.DataContext = viewModel;
			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Microsoft.UI.Xaml.Data.Binding(nameof(viewModel.Name))
				{
					Mode = BindingMode.TwoWay,
					TargetNullValue = "none",
					UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
				}
			);

			target.MyProperty = "typed";
			target.GetBindingExpression(MyControl.MyPropertyProperty).UpdateSource();

			Assert.AreEqual("typed", viewModel.Name);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25251")]
		public void When_TwoWay_ConvertBack_Returns_UnsetValue_Source_Untouched()
		{
			var target = new MyControl();
			var viewModel = new EditableViewModel { Name = "initial" };
			viewModel.NameSetCount = 0;

			target.DataContext = viewModel;
			target.SetBinding(
				MyControl.MyPropertyProperty,
				new Microsoft.UI.Xaml.Data.Binding(nameof(viewModel.Name))
				{
					Mode = BindingMode.TwoWay,
					Converter = new UnsetValueConverter(),
					UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
				}
			);

			target.MyProperty = "typed";
			target.GetBindingExpression(MyControl.MyPropertyProperty).UpdateSource();

			Assert.AreEqual("initial", viewModel.Name);
			Assert.AreEqual(0, viewModel.NameSetCount);
		}

		public class UnsetValueConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, string culture) => value;

			public object ConvertBack(object value, Type targetType, object parameter, string culture) => DependencyProperty.UnsetValue;
		}

		public class EditableViewModel
		{
			private string _name;

			public int NameSetCount { get; set; }

			public string Name
			{
				get => _name;
				set
				{
					NameSetCount++;
					_name = value;
				}
			}
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

		public partial class MyControl : FrameworkElement
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
