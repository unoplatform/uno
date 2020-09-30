using System;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Windows.Devices.Sensors;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_Devices
{
	[SampleControlInfo("Windows.Devices", "Orientation")]

	public sealed partial class OrientationTests : Page
	{
		long lastTime;
		public OrientationTests()
        {
            this.InitializeComponent();
			lastTime = DateTime.UtcNow.ToUnixTimeMilliseconds();
			SimpleOrientationSensor.GetDefault().OrientationChanged += OnSensorOrientationChanged;
		}

        private void OnSensorOrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
	        var now = DateTime.UtcNow.ToUnixTimeMilliseconds();

	        var diff = now - lastTime;
	        var s = diff / 1000;
	        var ms = diff % 1000;
	        timeSince.Text = $"~{s}.{ms} seconds since last orientation change.";
	        lastTime = now;
        }
    }
}
