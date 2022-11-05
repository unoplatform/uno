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
	public sealed partial class TextBox_Focus_Leak : UserControl
	{
		public TextBox_Focus_Leak()
		{
			this.InitializeComponent();

			Loaded += TextBox_Focus_Leak_Loaded;
		}

		private void TextBox_Focus_Leak_Loaded(object sender, RoutedEventArgs e)
		{
			TestTextBox.Focus(FocusState.Programmatic);
		}
	}
}
