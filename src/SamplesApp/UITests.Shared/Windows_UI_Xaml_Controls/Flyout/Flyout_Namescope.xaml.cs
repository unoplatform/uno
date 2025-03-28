using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.FlyoutTests
{
	[SampleControlInfo("Flyouts", "Namescope")]
	public sealed partial class Flyout_Namescope : UserControl
	{
		public Flyout_Namescope()
		{
			this.InitializeComponent();
		}

		private void DeleteConfirmation_Click(object sender, RoutedEventArgs e)
		{
			if (Control1.Flyout is Windows.UI.Xaml.Controls.Flyout f)
			{
				f.Hide();
			}
		}
	}
}
