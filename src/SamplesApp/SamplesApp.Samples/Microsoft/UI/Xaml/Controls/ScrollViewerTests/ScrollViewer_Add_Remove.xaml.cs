using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample("Scrolling")]
	public sealed partial class ScrollViewer_Add_Remove : UserControl
	{
		public ScrollViewer_Add_Remove()
		{
			this.InitializeComponent();
		}

		private UIElement _unloadedChild;
		private void YoinkButton_Click(object sender, object args)
		{
			if (YoinkBorder.Child != null)
			{
				_unloadedChild = YoinkBorder.Child;
				YoinkBorder.Child = null;
				ChildStatusTextBlock.Text = "Gone";
			}
			else
			{
				YoinkBorder.Child = _unloadedChild;
				ChildStatusTextBlock.Text = "Present";
			}
		}

		private void ScrollToBottomButton_Click(object sender, object args)
		{
			YoinkableScrollViewer.ChangeView(null, 10000, null, disableAnimation: true);
			ChildStatusTextBlock.Text = "Scrolled";
		}
	}
}
