using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Extensions;
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

namespace Uno.UI.Samples.Content.UITests.GridView
{
	[Sample("GridView", ViewModelType = typeof(ViewModelBase), Description = "GridView with FirstVisibleIndex and LastVisibleIndex shown")]
	public sealed partial class GridViewFirstVisibleIndex : UserControl
	{
		public GridViewFirstVisibleIndex()
		{
			this.InitializeComponent();
		}

		private void ListView_Loaded(object sender, RoutedEventArgs e)
		{
			var sv = (sender as ListViewBase).FindFirstChild<global::Windows.UI.Xaml.Controls.ScrollViewer>();
			var panel = (sender as ListViewBase).ItemsPanelRoot as ItemsWrapGrid;
			sv.ViewChanged += (_, __) =>
			{
				FirstVisibleIndexTextBlock.Text = panel.FirstVisibleIndex.ToString();
				LastVisibleIndexTextBlock.Text = panel.LastVisibleIndex.ToString();
			};
		}
	}
}
