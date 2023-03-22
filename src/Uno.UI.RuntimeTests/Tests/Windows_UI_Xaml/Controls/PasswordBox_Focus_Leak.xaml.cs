using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public sealed partial class PasswordBox_Focus_Leak : UserControl
	{
		public PasswordBox_Focus_Leak()
		{
			this.InitializeComponent();

			Loaded += PasswordBox_Focus_Leak_Loaded;
		}

		private void PasswordBox_Focus_Leak_Loaded(object sender, RoutedEventArgs e)
		{
			TestPasswordBox.Focus(FocusState.Programmatic);
		}
	}
}
