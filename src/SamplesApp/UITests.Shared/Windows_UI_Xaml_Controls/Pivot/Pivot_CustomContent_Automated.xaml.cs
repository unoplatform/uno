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

namespace UITests.Shared.Windows_UI_Xaml_Controls.Pivot
{
	[Sample("Pivot", nameof(Pivot_CustomContent_Automated))]
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
