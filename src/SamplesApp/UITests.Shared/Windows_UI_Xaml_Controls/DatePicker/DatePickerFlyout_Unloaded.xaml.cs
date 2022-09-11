using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;

namespace UITests.Shared.Windows_UI_Xaml_Controls.DatePicker
{
	[Sample("Pickers", IgnoreInSnapshotTests = true)]
	public sealed partial class DatePickerFlyout_Unloaded : UserControl
	{
		public DatePickerFlyout_Unloaded()
		{
			this.InitializeComponent();
		}

		public DateTimeOffset Date => new DateTimeOffset(2019, 05, 04, 0, 0, 0, TimeSpan.Zero);

		private async void UnloadClicked(object sender, RoutedEventArgs args)
		{
			unloadBtn.IsEnabled = false;
			for (var i = 5; i > 0; i--)
			{
				unloadBtn.Content = i.ToStringInvariant();
				await Task.Delay(1000);
			}

			root.Children.Remove(TestDatePickerFlyoutButton);
			root.Children.Remove(TestNativeDatePickerFlyoutButton);
			root.Children.Remove(theDatePicker);

			unloadBtn.Content = "unloaded!";
		}
	}
}
