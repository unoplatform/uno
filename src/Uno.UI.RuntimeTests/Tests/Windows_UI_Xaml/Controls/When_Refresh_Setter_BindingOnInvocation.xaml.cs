using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace Uno.UI.RuntimeTests
{
	public sealed partial class When_Refresh_Setter_BindingOnInvocation : UserControl
	{
		public When_Refresh_Setter_BindingOnInvocation()
		{
			InitializeComponent();
		}
	}

	public class When_Refresh_Setter_BindingOnInvocation_Converter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var ctrl = value as ContentControl;

			if (ctrl is not null)
			{
				return ctrl.Tag ?? -10;
			}

			return -10;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
