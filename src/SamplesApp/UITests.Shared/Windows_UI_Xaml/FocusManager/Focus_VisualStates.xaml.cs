using System;
using System.Collections.Generic;
using System.IO;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml.FocusTests
{
	[Sample("Focus")]
	public sealed partial class Focus_VisualStates : Page
	{
		public Focus_VisualStates() => InitializeComponent();

		private void FocusPointer(object sender, RoutedEventArgs e)
		{
			FocusButton.Focus(FocusState.Pointer);
		}

		private void FocusKeyboard(object sender, RoutedEventArgs e)
		{
			FocusButton.Focus(FocusState.Keyboard);
		}
	}
}
