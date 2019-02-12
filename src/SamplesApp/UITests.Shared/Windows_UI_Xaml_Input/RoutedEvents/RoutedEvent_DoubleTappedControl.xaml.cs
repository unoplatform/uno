using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UITests.Shared.Windows_UI_Xaml_Input.RoutedEvents
{
	public sealed partial class RoutedEvent_DoubleTappedControl : UserControl
	{
		public RoutedEvent_DoubleTappedControl()
		{
			this.InitializeComponent();

			DoubleTapped += (snd, evt) =>
			{
				txtChildren.Text += "(2/2) DoubleTapped (event handler) + handled=true\n";

				evt.Handled = true;
			};
		}

		protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
		{
			base.OnDoubleTapped(e);

			txtChildren.Text += "(1/2) DoubleTapped (override)\n";
		}
	}
}
