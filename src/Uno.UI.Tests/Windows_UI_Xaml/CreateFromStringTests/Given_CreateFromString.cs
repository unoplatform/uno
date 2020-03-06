using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Windows.Foundation.Metadata;

namespace Uno.UI.Tests.Windows_UI_Xaml.CreateFromStringTests
{
	[TestClass]
	public class Given_CreateFromString
	{
		[TestMethod]
		public void When_AttributeCreatesEntity()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var control = new Test_CreateFromString();
			app.HostView.Children.Add(control);

			var pt = control.TestLocationPoint.LocationPoint;

			Assert.AreEqual(45.0392, pt.Latitude);
			Assert.AreEqual(25.29232, pt.Longitude);

			var pt2 = control.TestLocationPoint2.LocationPoint;

			Assert.AreEqual(33.33, pt2.Latitude);
			Assert.AreEqual(22, pt2.Longitude);

			var pt3 = control.TestLocationPoint3.LocationPoint;

			Assert.AreEqual(11.22, pt3.Latitude);
			Assert.AreEqual(33, pt3.Longitude);

			// -------------------------------------------------

			var pt4 = control.TestLocationPoint.LocationPoint2;

			Assert.AreEqual(44.44, pt4.Latitude);
			Assert.AreEqual(24.24, pt4.Longitude);

			var pt5 = control.TestLocationPoint2.LocationPoint2;

			Assert.AreEqual(44.44, pt5.Latitude);
			Assert.AreEqual(24.24, pt5.Longitude);

			var pt6 = control.TestLocationPoint3.LocationPoint2;

			Assert.AreEqual(44.44, pt6.Latitude);
			Assert.AreEqual(24.24, pt6.Longitude);

			// -------------------------------------------------

			var pt7 = control.TestLocationPoint.LocationPoint3;

			Assert.AreEqual(43.03, pt7.Latitude);
			Assert.AreEqual(23.29, pt7.Longitude);

			var pt8 = control.TestLocationPoint2.LocationPoint3;

			Assert.AreEqual(43.03, pt8.Latitude);
			Assert.AreEqual(23.29, pt8.Longitude);

			var pt9 = control.TestLocationPoint3.LocationPoint3;

			Assert.AreEqual(43.03, pt9.Latitude);
			Assert.AreEqual(23.29, pt9.Longitude);
		}
	}

	public partial class MyLocationPointControl : Windows.UI.Xaml.Controls.Control
	{
		public Location LocationPoint { get; set; }

		public Location2 LocationPoint2 { get; set; }

		public Location3 LocationPoint3 { get; set; }
	}

	[Windows.Foundation.Metadata.CreateFromString(MethodName = "Uno.UI.Tests.Windows_UI_Xaml.CreateFromStringTests.Location.ConvertToLatLong")]
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
