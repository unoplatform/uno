using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ContentPresenterPages;

public sealed partial class ContentPresenter_ContentBindingPath : Page
{
	public ContentPresenter_ContentBindingPath()
	{
		this.InitializeComponent();
		SetupLV.ItemsSource = new TestData[]
		{
			new TestData("Asd"),
		};
	}

	public class TestData(string value)
	{
		public string Value { get; set; } = value;
	}
}
