using System.ComponentModel;
using System.Diagnostics;
using UITests.Shared.Helpers;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Devices
{
	public sealed partial class GeopositionDisplayControl : UserControl
	{
		public GeopositionDisplayControl()
		{
			this.InitializeComponent();
		}

		public Geoposition Geoposition
		{
			get { return (Geoposition)GetValue(GeopositionProperty); }
			set { SetValue(GeopositionProperty, value); }
		}

		public static DependencyProperty GeopositionProperty { get; } =
			DependencyProperty.Register(nameof(Geoposition), typeof(Geoposition), typeof(GeopositionDisplayControl), new PropertyMetadata(null, GeopositionChanged));


		private static void GeopositionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is GeopositionDisplayControl control && args.NewValue is Geoposition geoposition)
			{
				control.LatitudeRun.Text = geoposition.Coordinate.Point.Position.Longitude.ToString();
				control.LongitudeRun.Text = geoposition.Coordinate.Point.Position.Latitude.ToString();
				control.AltitudeRun.Text = geoposition.Coordinate.Point.Position.Altitude.ToString();
				control.AltitudeAccuracyRun.Text = geoposition.Coordinate.AltitudeAccuracy?.ToString() ?? "";
				control.AccuracyRun.Text = geoposition.Coordinate.Accuracy.ToString();
				control.HeadingRun.Text = geoposition.Coordinate.Heading?.ToString() ?? "";
				control.SpeedRun.Text = geoposition.Coordinate.Speed?.ToString() ?? "";
				control.TimestampRun.Text = geoposition.Coordinate.Timestamp.ToString();
			}
		}
	}
}
