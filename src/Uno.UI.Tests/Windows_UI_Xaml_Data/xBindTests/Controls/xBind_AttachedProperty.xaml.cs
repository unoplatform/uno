using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	public sealed partial class xBind_AttachedProperty : UserControl
	{
		public xBind_AttachedProperty()
		{
			this.InitializeComponent();
		}
	}

	public partial class CustomDO : DependencyObject
	{
		public static object GetTag(DependencyObject obj)
		{
			return ((FrameworkElement)obj).Tag;
		}

		public static void SetTag(DependencyObject obj, object value)
		{
			obj.SetValue(TagProperty, value);
		}

		// Using a DependencyProperty as the backing store for Tag.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TagProperty =
			DependencyProperty.RegisterAttached("Tag", typeof(object), typeof(CustomDO), new PropertyMetadata(0));


	}
}
