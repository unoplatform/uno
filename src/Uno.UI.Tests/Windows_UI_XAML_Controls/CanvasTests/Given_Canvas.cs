using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.CanvasTests
{
	[TestClass]
	public partial class Given_Canvas
	{
		private partial class View : FrameworkElement
		{
		}

		[TestMethod]
		public void When_Canvas_have_Fixed_size()
		{
			var CAV = new Canvas() { Name = "test", RequestedDesiredSize = new Windows.Foundation.Size(60, 60) };

			var c1 = CAV.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(50, 50) }
			);

			var c2 = CAV.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(20, 15) }
			);

			CAV.Measure(new Windows.Foundation.Size(60, 60));
			Assert.AreEqual(new Windows.Foundation.Size(60, 60), CAV.DesiredSize, "measuredSize");

			c1.Measure(new Windows.Foundation.Size(50, 50));
			Assert.AreEqual(new Windows.Foundation.Size(50, 50), c1.RequestedDesiredSize, "measuredSizeChild1");
			c2.Measure(new Windows.Foundation.Size(20, 15));
			Assert.AreEqual(new Windows.Foundation.Size(20, 15), c2.RequestedDesiredSize, "measuredSizeChild2");

			Assert.AreEqual(2, CAV.GetChildren().Count());
		}

		[TestMethod]
		public void When_SimpleLayout()
		{
			var CAV = new Canvas() { Name = "test" };

			var c1 = CAV.AddChild(
				new View { Name = "Child01", RequestedDesiredSize = new Windows.Foundation.Size(50, 50) }
			);

			var c2 = CAV.AddChild(
				new View { Name = "Child02", RequestedDesiredSize = new Windows.Foundation.Size(20, 15) }
			);

			c1.Measure(new Windows.Foundation.Size(50, 50));
			Assert.AreEqual(new Windows.Foundation.Size(50, 50), c1.RequestedDesiredSize, "measuredSizeChild1");
			c2.Measure(new Windows.Foundation.Size(20, 15));
			Assert.AreEqual(new Windows.Foundation.Size(20, 15), c2.RequestedDesiredSize, "measuredSizeChild2");

			Assert.AreEqual(2, CAV.GetChildren().Count());
		}

		[TestMethod]
		public void When_CanvasPropertyConvert()
		{
			var SUT = new Canvas();

			SUT.SetValue(Canvas.LeftProperty, "42");
			Assert.AreEqual(42d, SUT.GetValue(Canvas.LeftProperty));

			SUT.SetValue(Canvas.LeftProperty, 43);
			Assert.AreEqual(43d, SUT.GetValue(Canvas.LeftProperty));

			SUT.SetValue(Canvas.LeftProperty, 44f);
			Assert.AreEqual(44d, SUT.GetValue(Canvas.LeftProperty));

			SUT.SetValue(Canvas.TopProperty, "42");
			Assert.AreEqual(42d, SUT.GetValue(Canvas.TopProperty));

			SUT.SetValue(Canvas.ZIndexProperty, "42");
			Assert.AreEqual(42, SUT.GetValue(Canvas.ZIndexProperty));
		}
	}
}
