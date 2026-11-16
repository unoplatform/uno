using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListView_Explicit_Items", ViewModelType = typeof(ViewModelBase), Description = "ListView containing explicitly-defined ListViewItems. Items shouldn't be corrupted by scrolling.")]
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
