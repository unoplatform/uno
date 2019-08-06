using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Shared.Windows_Devices
{
	[SampleControlInfo("Windows.Devices", "Barometer", description: "Demonstrates use of Windows.Devices.Sensors.Barometer")]
	public sealed partial class BarometerTests : UserControl
	{
		private Barometer _barometer;

		public BarometerTests()
		{
			this.InitializeComponent();
			_barometer = Barometer.GetDefault();
			if (_barometer == null)
			{
				ResultsTextBlock.Text = "Barometer not available on this device";
			}
			MainContent.IsEnabled = _barometer != null;
		}

		private void StartReadingClick(object sender, RoutedEventArgs e)
		{
			_barometer.ReadingChanged += _barometer_ReadingChanged;
			StartReadingButton.IsEnabled = false;
			EndReadingButton.IsEnabled = true;
		}

		private void EndReadingClick(object sender, RoutedEventArgs e)
		{
			_barometer.ReadingChanged -= _barometer_ReadingChanged;
			StartReadingButton.IsEnabled = true;
			EndReadingButton.IsEnabled = false;
		}


		private async void _barometer_ReadingChanged(Barometer sender, BarometerReadingChangedEventArgs args)
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				ResultsTextBlock.Text = $"StationPressureInHectopascals: {args.Reading.StationPressureInHectopascals}\r\nat {args.Reading.Timestamp}";
			});
		}
	}
}
