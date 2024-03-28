using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

using SwipeItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeItem;
using SwipeItemInvokedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeItemInvokedEventArgs;

namespace UITests.Windows_UI_Xaml_Controls.SwipeControlTests
{
	[Sample("SwipeControl", "ScrollViewer")]
	public sealed partial class SwipeControl_ScrollViewer : Page
	{
		public SwipeControl_ScrollViewer()
		{
			this.InitializeComponent();

			TheSecondSUT.ItemsSource = new[]
			{
				"#FFFF0018",
				"#FFFFA52C",
				"#FFFFFF41",
				"#FF008018",
				"#FF0000F9",
				"#FF86007D",

				"#CCFF0018",
				"#CCFFA52C",
				"#CCFFFF41",
				"#CC008018",
				"#CC0000F9",
				"#CC86007D",

				"#66FF0018",
				"#66FFA52C",
				"#66FFFF41",
				"#66008018",
				"#660000F9",
				"#6686007D",

				"#33FF0018",
				"#33FFA52C",
				"#33FFFF41",
				"#33008018",
				"#330000F9",
				"#3386007D"
			};
		}

		private void ItemInvoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
		{
			Output.Text = args.SwipeControl.DataContext?.ToString();
		}
	}
}
