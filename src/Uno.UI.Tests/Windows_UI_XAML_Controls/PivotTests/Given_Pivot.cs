using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.PivotTests
{
	[TestClass]
	public class Given_Pivot
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_Empty()
		{
			var SUT = new Pivot() { Name = "test" };

			var grid = new Grid();
			grid.Children.Add(SUT);
			grid.ForceLoaded();

			SUT.Measure(default(Size));
			SUT.Arrange(default(Rect));

			// A templated Pivot lays out its header/title chrome even with no items, so an
			// empty Pivot has a non-zero DesiredSize on real Skia (0,0 was the mock-era value
			// when no template was applied).
			Assert.AreNotEqual(default(Size), SUT.DesiredSize);
		}
	}
}
