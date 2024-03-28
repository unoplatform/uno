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
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Navigation;

namespace UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests;

[Sample("AutoSuggestBox", IsManualTest = true,
	Description = "The AutoSuggestBox should have a search icon on the right. Clicking the button should randomly change the width of the control.")]
public sealed partial class AutoSuggestBox_BitmapIcon : Page
{
	public AutoSuggestBox_BitmapIcon()
	{
		this.InitializeComponent();
	}

	Random r = new Random();

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		RootPanel.Width = r.Next(200, 400);
	}
}
