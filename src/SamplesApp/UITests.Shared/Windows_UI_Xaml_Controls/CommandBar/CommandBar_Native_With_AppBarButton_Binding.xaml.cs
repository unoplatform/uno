using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.CommandBar
{

	[Sample("CommandBar", Name = "Native_AppBarButton_Binding", Description = "Shows a Native CommandBar with an AppBarButton. \n" + "The AppBarButton will show a FilterIcon and when Clicked it will show a CloseIcon. \n" + "This tests that the AppBarButton bindings are working.", IsManualTest = true)]
	public sealed partial class CommandBar_Native_With_AppBarButton_Binding : Page
	{
		public CommandBar_Native_With_AppBarButton_Binding()
		{
			this.InitializeComponent();
		}
	}

	public class FromNullableBoolToCustomValueConverter : IValueConverter
	{
		public object NullOrFalseValue { get; set; }

		public object TrueValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (parameter != null)
			{
				throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
			}

			if (value != null && !(value is bool))
			{
				throw new ArgumentException($"Value must either be null or of type bool. Got {value} ({value.GetType().FullName})");
			}

			if (value == null || !System.Convert.ToBoolean(value, CultureInfo.InvariantCulture))
			{
				return NullOrFalseValue;
			}
			else
			{
				return TrueValue;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (parameter != null)
			{
				throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
			}

			if (object.Equals(this.TrueValue, this.NullOrFalseValue))
			{
				throw new InvalidOperationException("Cannot convert back if both custom values are the same");
			}

			return this.TrueValue != null ?
				value.Equals(TrueValue) :
				!value.Equals(this.NullOrFalseValue);
		}
	}
}
