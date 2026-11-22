using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;
using Windows.Storage.Pickers;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Flyout
{
	[Sample("Flyouts", Name = "Flyout_AppDeactivation", IsManualTest = true, Description = "A flyout kept open through a window deactivation, such as a file picker or leaving the app on mobile.")]
	public sealed partial class Flyout_AppDeactivation : UserControl
	{
		private bool _isCloseRequested;

#if HAS_UNO
		private readonly bool _initialPreventLightDismiss = Uno.UI.FeatureConfiguration.Popup.PreventLightDismissOnWindowDeactivated;
#endif

		public Flyout_AppDeactivation()
		{
			this.InitializeComponent();

#if HAS_UNO
			PreventLightDismissCheckBox.IsChecked = _initialPreventLightDismiss;
			Unloaded += (_, _) => Uno.UI.FeatureConfiguration.Popup.PreventLightDismissOnWindowDeactivated = _initialPreventLightDismiss;
#else
			PreventLightDismissCheckBox.IsEnabled = false;
#endif
		}

		private void PreventLightDismissCheckBox_Changed(object sender, RoutedEventArgs e)
		{
#if HAS_UNO
			Uno.UI.FeatureConfiguration.Popup.PreventLightDismissOnWindowDeactivated = PreventLightDismissCheckBox.IsChecked == true;
#endif
		}

		private void SampleFlyout_Opened(object sender, object e) => Log("Opened");

		private void SampleFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
		{
			if (_isCloseRequested)
			{
				_isCloseRequested = false;
				Log("Closing (Close button)");
				return;
			}

			args.Cancel = CancelClosingCheckBox.IsChecked == true;
			Log(args.Cancel ? "Closing (cancelled)" : "Closing");
		}

		// Light dismiss is cancelled as well while the check box is checked.
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			_isCloseRequested = true;
			SampleFlyout.Hide();
		}

		private void SampleFlyout_Closed(object sender, object e) => Log("Closed");

		private async void PickButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var picker = new FileOpenPicker();
				picker.FileTypeFilter.Add("*");
				var file = await picker.PickSingleFileAsync();
				Log($"Picked: {file?.Name ?? "nothing"}, flyout open: {SampleFlyout.IsOpen}");
			}
			catch (Exception ex)
			{
				Log($"Picker failed: {ex.Message}");
			}
		}

		private void ClearLog_Click(object sender, RoutedEventArgs e) => LogTextBlock.Text = "";

		private void Log(string message) => LogTextBlock.Text += $"{DateTime.Now:HH:mm:ss.fff} {message}\n";
	}
}
