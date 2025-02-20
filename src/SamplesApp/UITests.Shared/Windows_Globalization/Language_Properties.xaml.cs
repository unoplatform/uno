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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Shared.Windows_Globalization
{
	[SampleControlInfo("Windows.Globalization", "Language", description: "Properties of the Windows.Globalization.Language class")]
	public sealed partial class Language_Properties : UserControl
	{

		public Language_Properties()
		{
			this.InitializeComponent();
			Refresh();
		}

		private void Refresh_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
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
