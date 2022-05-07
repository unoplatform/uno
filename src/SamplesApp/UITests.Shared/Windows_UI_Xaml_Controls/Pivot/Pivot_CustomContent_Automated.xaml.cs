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

namespace UITests.Shared.Windows_UI_Xaml_Controls.Pivot
{
	[SampleControlInfo("Pivot", nameof(Pivot_CustomContent_Automated))]
	public sealed partial class Pivot_CustomContent_Automated : UserControl
	{
		public Pivot_CustomContent_Automated()
		{
			this.InitializeComponent();
		}

		public void OnMyControlLoaded(object sender, object args)
		{
			myPivot.Items.Add(new MyCustomPivotItem { Title = "item 1", Content = "My Item 1 Content" });
			myPivot.Items.Add(new MyCustomPivotItem { Title = "item 2", Content = "My Item 2 Content" });
		}
	}

	public class MyCustomPivotItem
	{
		public string Title { get; set; }
		public string Content { get; set; }
	}
}
