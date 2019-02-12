using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UITests.Shared.Windows_UI_Xaml_Input.RoutedEvents
{
	public sealed partial class RoutedEvent_TappedControl : UserControl
	{
		public RoutedEvent_TappedControl()
		{
			this.InitializeComponent();

			Tapped += (snd, evt) =>
			{
				txtChildren.Text += "(2/2) Tapped (event handler) + handled=true\n";

				evt.Handled = true;
			};
		}

		protected override void OnTapped(TappedRoutedEventArgs e)
		{
			base.OnTapped(e);

			txtChildren.Text += "(1/2) Tapped (override)\n";
		}
	}
}
