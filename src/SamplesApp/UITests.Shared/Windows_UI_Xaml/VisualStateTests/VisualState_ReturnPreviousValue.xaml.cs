using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UITests.Windows_UI_Xaml.VisualStateTests
{
	[Sample("Visual states")]
	public sealed partial class VisualState_ReturnPreviousValue : Page
    {
        public VisualState_ReturnPreviousValue()
        {
            this.InitializeComponent();
        }

		private void SetState_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			VisualStateManager.GoToState(this, button.Tag.ToString(), true);
		}

		private void ChangeBackground_Click(object sender, RoutedEventArgs e)
		{
			RootGrid.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
		}
	}
}
