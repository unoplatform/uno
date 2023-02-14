using System;
using System.Collections.Generic;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Tests.Windows_UI_Xaml.Controls
{
	public sealed partial class When_xLoad : UserControl
	{
		public When_xLoad()
		{
			this.InitializeComponent();
		}

		public bool MyVisibility
		{
			get { return (bool)GetValue(MyVisibilityProperty); }
			set { SetValue(MyVisibilityProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyVisibility.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyVisibilityProperty =
			DependencyProperty.Register("MyVisibility", typeof(bool), typeof(When_xLoad), new PropertyMetadata(false));
	}
}
