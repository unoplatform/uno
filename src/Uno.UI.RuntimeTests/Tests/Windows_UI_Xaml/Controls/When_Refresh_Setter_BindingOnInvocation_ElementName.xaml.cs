using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Annotations = Uno.UI.RuntimeTests.Helpers.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.RuntimeTests
{
	public sealed partial class When_Refresh_Setter_BindingOnInvocation_ElementName : UserControl
	{
		public When_Refresh_Setter_BindingOnInvocation_ElementName()
		{
			InitializeComponent();
		}
	}

	public class When_Refresh_Setter_BindingOnInvocation_ElementName_Converter : IValueConverter
	{
		public object Convert(object value, [DynamicallyAccessedMembers(Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language)
		{
			var ctrl = value as FrameworkElement;

			if (ctrl is not null)
			{
				return ctrl.Tag ?? -10;
			}

			return -10;
		}

		public object ConvertBack(object value, [DynamicallyAccessedMembers(Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
