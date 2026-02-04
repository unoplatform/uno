using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace UITests.Shared.Windows_Globalization
{
	[Sample("Windows.Globalization", Name = "Language", Description = "Properties of the Windows.Globalization.Language class")]
	public sealed partial class Language_Properties : UserControl
	{

		public Language_Properties()
		{
			this.InitializeComponent();
			Refresh();
		}

		private void Refresh_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			Refresh();
		}

		private void Refresh()
		{
			CurrentLanguageInputTagRun.Text = Windows.Globalization.Language.CurrentInputMethodLanguageTag;
		}

		public void TextChanged(object sender, TextChangedEventArgs eventArgs)
		{
			Refresh();
		}

		public void TextChanging(TextBox sender, TextBoxTextChangingEventArgs eventArgs)
		{
			Refresh();
		}
	}
}
