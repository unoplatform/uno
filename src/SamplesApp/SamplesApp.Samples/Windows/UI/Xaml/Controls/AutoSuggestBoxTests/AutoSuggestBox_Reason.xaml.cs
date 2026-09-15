using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests;

[Sample("AutoSuggestBox", IsManualTest = false)]
public sealed partial class AutoSuggestBox_Reason : Page
{

	public List<string> SampleList = new List<string>
	{
		"Africa",
		"America",
		"Europe",
	};

	public AutoSuggestBox_Reason()
	{
		this.InitializeComponent();

		ReasonAutoSuggestBox.ItemsSource = SampleList;
		ReasonAutoSuggestBox.TextChanged += (s, e) =>
		{
			txtConsole.Text = e.Reason.ToString();
		};
	}
	public void PopulateClick(object sender, RoutedEventArgs e)
	{
		ReasonAutoSuggestBox.Text = "A";
	}


}
