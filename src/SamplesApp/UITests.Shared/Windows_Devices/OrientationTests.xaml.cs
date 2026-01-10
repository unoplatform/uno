using System;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Windows.Devices.Sensors;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_Devices
{
	[Sample("Windows.Devices", Name = "Orientation", IgnoreInSnapshotTests = true)]

	public sealed partial class OrientationTests : Page
	{
		long lastTime;
		public OrientationTests()
		{
			this.InitializeComponent();
			lastTime = DateTime.UtcNow.ToUnixTimeMilliseconds();
			this.Loaded += (s, e) =>
			{
				var sensor = SimpleOrientationSensor.GetDefault();
				if (sensor is { })
				{
					sensor.OrientationChanged += OnSensorOrientationChanged;
				}
				else
				{
					message.Text = "This device does not have an orientation sensor.";
				}
			};
		}



		private void OnSensorOrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
		{
			var now = DateTime.UtcNow.ToUnixTimeMilliseconds();

			var diff = now - lastTime;
			var s = diff / 1000;
			var ms = diff % 1000;
			timeSince.Text = $"~{s}.{ms} seconds since last orientation changed to {args.Orientation}";
			lastTime = now;
		}
	}
}
