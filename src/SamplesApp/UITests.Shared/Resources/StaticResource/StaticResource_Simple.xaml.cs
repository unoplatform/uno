using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Resources.StaticResource
{
	[Sample("Resources", "StaticResource_Simple")]
	public sealed partial class StaticResource_Simple : UserControl
	{
		public StaticResource_Simple()
		{
			Application.Current.Resources["CSharp_Resource"] = "This resource was registered in C#";
			Application.Current.Resources["HelloConverter"] = new HelloConverter();

			this.InitializeComponent();

			DataContext = new object();
		}

		private class HelloConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, string language)
			{
				return "Hello Converter!";
			}

			public object ConvertBack(object value, Type targetType, object parameter, string language)
			{
				return "Hello Converter!";
			}
		}
	}
}
