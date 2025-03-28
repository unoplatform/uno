using System;
using System.Linq;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

using SwipeItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeItem;
using SwipeItemInvokedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeItemInvokedEventArgs;

namespace UITests.Windows_UI_Xaml_Controls.SwipeControlTests
{
	[Sample("SwipeControl")]
	public sealed partial class SwipeControl_Automated : Page
	{
		public SwipeControl_Automated()
		{
			this.InitializeComponent();
		}

		private void ItemInvoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
		{
			Output.Text = sender.Text;
		}
	}
}
