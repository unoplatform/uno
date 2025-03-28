using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
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

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_Explicit_Items", typeof(ViewModelBase), description: "ListView containing explicitly-defined ListViewItems. Items shouldn't be corrupted by scrolling.")]
	public sealed partial class ListView_Explicit_Items : UserControl
	{
		public ListView_Explicit_Items()
		{
			this.InitializeComponent();
			var thickness = new Thickness(5);
			ListView.ItemsSource =
				Enumerable.Range(0, 30)
					.Select(i => new ListViewItem
					{
						Content = i,
						Margin = thickness,
					})
					.ToArray();
		}
	}
}
