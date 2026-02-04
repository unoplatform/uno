using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.Flyout
{
	[Sample("Flyouts", Name = "Flyout_Events")]
	public sealed partial class Flyout_Events : UserControl
	{
		public Flyout_Events()
		{
			this.InitializeComponent();

			MyFlyout.Opening += (s, e) =>
			{
				Log("Opening");
			};

			MyFlyout.Closing += (s, e) =>
			{
				Log("Closing");
				if (CancelClosing.IsChecked == true)
				{
					e.Cancel = true;
					Log("Closing canceled");
				}
			};

			MyFlyout.Opened += (s, e) =>
			{
				Log("Opened");
			};

			MyFlyout.Closed += (s, e) =>
			{
				Log("Closed");
			};
		}

		private void Log(string message)
		{
			Events.Text += (message += "\n");
		}
	}
}
