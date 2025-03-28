using System;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls;

namespace XamlGenerationTests.Shared
{
	public sealed partial class CreateFromString : UserControl
	{
		public CreateFromString()
		{
			this.InitializeComponent();
		}
	}
}

namespace XamlGenerationTests.Shared.CreateFromStringTests
{
	public partial class MyLocationPointControl : Control
	{
		public Location LocationPoint { get; set; }

		public Location2 LocationPoint2 { get; set; }

		public Location3 LocationPoint3 { get; set; }
	}

	[Windows.Foundation.Metadata.CreateFromString(MethodName = "XamlGenerationTests.Shared.CreateFromStringTests.Location.ConvertToLatLong")]
	public class Location
	{
		public double Latitude { get; set; }

		public double Longitude { get; set; }

		public static Location ConvertToLatLong(string rawString)
		{
			var coords = rawString.Split(',');

			return new Location
			{
				Latitude = Convert.ToDouble(coords[0]),
				Longitude = Convert.ToDouble(coords[1])
			};
		}
	}

	[CreateFromString(MethodName = nameof(ConvertToLatLong))]
	public class Location2
	{
		public double Latitude { get; set; }

		public double Longitude { get; set; }

		public static Location2 ConvertToLatLong(string rawString)
		{
			var coords = rawString.Split(',');

			return new Location2
			{
				Latitude = Convert.ToDouble(coords[0]),
				Longitude = Convert.ToDouble(coords[1])
			};
		}
	}

	[CreateFromString(MethodName = "ConvertToLatLong")]
	public class Location3
	{
		public double Latitude { get; set; }

		public double Longitude { get; set; }

		public static Location3 ConvertToLatLong(string rawString)
		{
			var coords = rawString.Split(',');

			return new Location3
			{
				Latitude = Convert.ToDouble(coords[0]),
				Longitude = Convert.ToDouble(coords[1])
			};
		}
	}
}
