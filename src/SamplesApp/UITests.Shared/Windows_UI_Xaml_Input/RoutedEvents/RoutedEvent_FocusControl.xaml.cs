using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UITests.Shared.Windows_UI_Xaml_Input.RoutedEvents
{
	public sealed partial class RoutedEvent_FocusControl : UserControl
	{
		public RoutedEvent_FocusControl()
		{
			this.InitializeComponent();

			GotFocus += (snd, evt) => txtChildren.Text += "(2/2) Focus (event handler)\n";
			LostFocus += (snd, evt) => txtChildren.Text += "(2/2) Lost Focus (event handler)\n";
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			txtChildren.Text += "(1/2) Focus (override)\n";
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);

			txtChildren.Text += "(1/2) Lost Focus (override)\n";
		}
	}
}
