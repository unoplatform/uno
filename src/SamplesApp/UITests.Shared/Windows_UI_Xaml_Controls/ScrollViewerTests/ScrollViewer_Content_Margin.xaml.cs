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

namespace UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample("Scrolling")]
	public sealed partial class ScrollViewer_Content_Margin : UserControl
	{
		public ScrollViewer_Content_Margin()
		{
			this.InitializeComponent();
		}

		private void ScrollToRightBottomButton_Click(object sender, object args)
		{
			BothDirectionsScrollViewer.ChangeView(500, 500, null, disableAnimation: true);
			ChildStatusTextBlock.Text = "Scrolled";
		}
	}
}
