using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.FlyoutTests
{
	[Sample("Flyouts", "Vanilla")]
	public sealed partial class Flyout_Vanilla : UserControl
	{
		public Flyout_Vanilla()
		{
			this.InitializeComponent();
		}

		private void DeleteConfirmation_Click(object sender, RoutedEventArgs e)
		{
			if (Control1.Flyout is Microsoft.UI.Xaml.Controls.Flyout f)
			{
				f.Hide();
			}
		}
	}
}
