using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

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
