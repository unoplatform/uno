using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.FlyoutTests
{
	[SampleControlInfo("Flyouts", "Vanilla")]
	public sealed partial class Flyout_Vanilla : UserControl
	{
		public Flyout_Vanilla()
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
