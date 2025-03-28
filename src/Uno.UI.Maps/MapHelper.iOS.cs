using System;
using CoreGraphics;
using CoreLocation;
using MapKit;
using Windows.Devices.Geolocation;

namespace Windows.UI.Xaml.Controls.Maps
{
	internal static class MapHelper
	{
		public const double MercatorRadius = MercatorOffset / Math.PI;

		//  Half of the earth circumference in pixels at zoom level 21.
		public const double MercatorOffset = 268435456;

		internal static MKCoordinateRegion CreateRegion(Geopoint centerCoordinate, double zoomLevel, CGSize size)
		{
			// convert center coordinate to pixel space 
			double centerPixelX = LongitudeToPixelSpaceX(centerCoordinate.Position.Longitude);
			double centerPixelY = LatitudeToPixelSpaceY(centerCoordinate.Position.Latitude);

			// determine the scale value from the zoom level 
			var zoomExponent = 21 - zoomLevel;
			double zoomScale = Math.Pow(2, zoomExponent);

			// scale the map’s size in pixel space 
			var mapSizeInPixels = size;
			double scaledMapWidth = mapSizeInPixels.Width * zoomScale;
			double scaledMapHeight = mapSizeInPixels.Height * zoomScale;

			// figure out the position of the top-left pixel 
			double topLeftPixelX = centerPixelX - (scaledMapWidth / 2);
			double topLeftPixelY = centerPixelY - (scaledMapHeight / 2);

			// find delta between left and right longitudes 
			var minLng = PixelSpaceXToLongitude(topLeftPixelX);
			var maxLng = PixelSpaceXToLongitude(topLeftPixelX + scaledMapWidth);
			var longitudeDelta = maxLng - minLng;

			// find delta between top and bottom latitudes 
			var minLat = PixelSpaceYToLatitude(topLeftPixelY);
			var maxLat = PixelSpaceYToLatitude(topLeftPixelY + scaledMapHeight);
			var latitudeDelta = -1 * (maxLat - minLat);

			// create and return the lat/lng span 
			var span = new MKCoordinateSpan(latitudeDelta, longitudeDelta);
			var region = new MKCoordinateRegion(centerCoordinate.ToLocation(), span);

			return region;
		}

		internal static double PixelSpaceXToLongitude(double pixelX)
		{
			return ((Math.Round(pixelX) - MercatorOffset) / MercatorRadius) * 180.0 / Math.PI;
		}

		internal static double PixelSpaceYToLatitude(double pixelY)
		{
			return (Math.PI / 2.0 - 2.0 * Math.Atan(Math.Exp((Math.Round(pixelY) - MercatorOffset) / MercatorRadius))) * 180.0 / Math.PI;
		}

		internal static double LongitudeToPixelSpaceX(double longitude)
		{
			return Math.Round(MercatorOffset + MercatorRadius * longitude * Math.PI / 180.0);
		}

		internal static double LatitudeToPixelSpaceY(double latitude)
		{
			if (latitude == 90.0)
			{
				return 0;
			}
			else if (latitude == -90.0)
			{
				return MercatorOffset * 2;
			}
			else
			{
				return Math.Round(MercatorOffset - MercatorRadius * Math.Log((1 + Math.Sin(latitude * Math.PI / 180.0)) / (1 - Math.Sin(latitude * Math.PI / 180.0))) / 2.0);
			}
		}

	}

	internal static class GeopointExtensions
	{
		public static CLLocationCoordinate2D ToLocation(this Geopoint c)
		{
			return new CLLocationCoordinate2D(c.Position.Latitude, c.Position.Longitude);
		}
		public static CLLocationCoordinate2D ToLocation(this BasicGeoposition c)
		{
			return new CLLocationCoordinate2D(c.Latitude, c.Longitude);
		}

		public static Geopoint ToGeopoint(this CLLocationCoordinate2D l)
		{
			return new Geopoint(new BasicGeoposition { Latitude = l.Latitude, Longitude = l.Longitude });
		}
	}
}
