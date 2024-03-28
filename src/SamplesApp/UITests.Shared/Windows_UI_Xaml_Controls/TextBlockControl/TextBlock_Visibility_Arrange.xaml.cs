using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl
{
	[SampleControlInfo("TextBlock", "TextBlock_Visibility_Arrange", description: "The TextBlock's visibility changes during the arrange pass - this shouldn't crash!")]
	public sealed partial class TextBlock_Visibility_Arrange : UserControl
	{
		public TextBlock_Visibility_Arrange()
		{
			this.InitializeComponent();
		}
	}

	public class DoubleToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is double d)
			{
				return d > 10 ?
					Visibility.Visible :
					Visibility.Collapsed;
			}

			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
