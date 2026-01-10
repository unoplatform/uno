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
using Uno.UI.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", "ListView_With_ListViews_Count_Measure")]
	public sealed partial class ListView_With_ListViews_Count_Measure : UserControl
	{
		public ListView_With_ListViews_Count_Measure()
		{
			this.InitializeComponent();
		}
		private void ChangeViewButton_Clicked(object sender, RoutedEventArgs args)
		{
			var sv = OuterListView.FindFirstChild<ScrollViewer>();

			sv.ViewChanged += (o, e) =>
			{
				if (sv.VerticalOffset > 500)
				{
					ResultTextBlock.Text = "Scrolled";
				}
			};

			sv.ChangeView(horizontalOffset: null, verticalOffset: 3000d, zoomFactor: null, disableAnimation: false);
		}

	}

}
