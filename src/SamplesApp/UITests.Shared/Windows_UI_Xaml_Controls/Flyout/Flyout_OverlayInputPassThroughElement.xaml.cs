using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Flyout
{
	[Sample("Flyouts", "Flyout_ButtonInContent")]
	public sealed partial class Flyout_OverlayInputPassThroughElement : Page
	{
		public Flyout_OverlayInputPassThroughElement()
		{
			this.InitializeComponent();

			stackPanel.Loaded += (_, _) =>
			{
				stackPanel.AddHandler(PointerPressedEvent, new PointerEventHandler(StackPanel_Click), true);
			};
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			textBlock.Text += " Button_Click";
		}

		private void FlyoutBase_OnClosed(object sender, object e)
		{
			textBlock.Text += " FlyoutBase_OnClosed";
		}
		private void FlyoutBase_OnClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
		{
			textBlock.Text += " FlyoutBase_OnClosing";
		}

		private void StackPanel_Click(object sender, RoutedEventArgs e)
		{
			textBlock.Text += " StackPanel_Click";
		}
	}
}
