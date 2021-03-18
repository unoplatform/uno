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

namespace UITests.Windows_UI_Xaml_Controls.TextBox
{
	[Sample]
	public sealed partial class TextBox_Focus_Programmatic : UserControl
	{
		public TextBox_Focus_Programmatic()
		{
			this.InitializeComponent();
		}

		private void SetFocus_Click(object sender, RoutedEventArgs e)
		{
			TargetTextBox.Focus(FocusState.Programmatic);
		}

		private void TargetTextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			StatusText.Text = "Got focus";
		}
	}
}
