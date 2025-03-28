using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ContentPresenterPages;

public sealed partial class ContentPresenter_ContentBindings : Page
{
	public ContentPresenter_ContentBindings()
	{
		this.InitializeComponent();
		this.DataContext = new TestData();
	}

	public class TestData
	{
		public string Text { get; set; } = "lalala~";

		public override string ToString() => GetType().Name;
	}
}

