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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.CommandBar
{

	[SampleControlInfo("CommandBar", "Native_AppBarButton_Binding", typeof(CommandBarViewModel), isManualTest: true)]
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
