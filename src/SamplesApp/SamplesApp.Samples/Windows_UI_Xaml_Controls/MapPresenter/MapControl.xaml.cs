using System;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Windows.Devices.Geolocation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Maps;

#if HAS_UNO
using Uno.Foundation.Logging;
#else
using Microsoft.Extensions.Logging;
using Uno.Logging;
#endif

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.MapControl
{
	[Sample("Mapping", Name = "MapControl")]
	public sealed partial class MapControl : UserControl
	{
#pragma warning disable CS0109
#if HAS_UNO
		private new readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(MapControl));
#else
		private static readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(MapControl));
#endif
#pragma warning restore CS0109

		public Geopoint PinPoint { get; set; }

		public MapControl()
		{
			this.InitializeComponent();
			try
			{
				AddMapIcon();
			}
			catch (Exception e)
			{
				_log.Error("Map initialization failed to complete", e);
			}
		}

		private void AddMapIcon()
		{
			var myLandmarks = new List<MapElement>();
			BasicGeoposition snPosition = new BasicGeoposition { Latitude = 47.620, Longitude = -122.349 };
			PinPoint = new Geopoint(snPosition);
			var spaceNeedleIcon = new MapIcon
			{
				Location = PinPoint,
				ZIndex = 0,
				Title = "Pin"
			};
			myLandmarks.Add(spaceNeedleIcon);
			var landmarksLayer = new MapElementsLayer
			{
				ZIndex = 1,
				MapElements = myLandmarks
			};
			MyMapControl.Layers.Add(landmarksLayer);
			MyMapControl.Center = PinPoint;
			MyMapControl.ZoomLevel = 14;
		}
	}
}
