using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Input.RoutedEvents
{
	[Sample("Routed Events", "TappedAndReleaseEvents")]
	public sealed partial class RoutedEvent_TappedAndRelease : Page
	{
		public RoutedEvent_TappedAndRelease()
		{
			this.InitializeComponent();
		}

		private void ElementTapped(object sender, TappedRoutedEventArgs e)
		{
			txtRoot.Text += "\tTapped\r\n";
		}
		private void ElementPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			txtRoot.Text += "\tPressed\r\n";
		}

		private void ElementPointerReleased(object sender, PointerRoutedEventArgs e)
		{
			txtRoot.Text += "\tReleased\r\n";
		}
	}
}
