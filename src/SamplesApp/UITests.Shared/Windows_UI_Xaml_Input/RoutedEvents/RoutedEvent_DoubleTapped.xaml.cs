using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Input.RoutedEvents
{
	[Sample("Routed Events", "DoubleTappedEvent")]
	public sealed partial class RoutedEvent_DoubleTapped : Page
	{
		public RoutedEvent_DoubleTapped()
		{
			this.InitializeComponent();

			AddHandler(DoubleTappedEvent, new DoubleTappedEventHandler(RootHandler), handledEventsToo: true);
		}

		private void RootHandler(object sender, DoubleTappedRoutedEventArgs e)
		{
			txtRoot.Text += "DBLTAPPED (root) - handledEventsToo: true\n";
		}

		protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
		{
			base.OnDoubleTapped(e);

			txtRoot.Text += "DBLTAPPED (root) - override, should not happen when double-tapped on children element\n";
		}
	}
}
