using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;

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
		}
	}

	public partial class MyLocationPointControl : Windows.UI.Xaml.Controls.Control
	{
		public Location LocationPoint { get; set; }
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
}
