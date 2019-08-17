using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.FlyoutTests
{
	[TestClass]
	public class Given_Flyout
	{
		[TestMethod]
		public void When_ChildIsBigger_PlacementBottom()
		{
			var SUT = new Grid() { Name = "test" };

			var flyout = new Flyout()
			{
				LightDismissOverlayMode = LightDismissOverlayMode.On,
				Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
			};

			var button = new Button()
			{
				Width = 5,
				Flyout = flyout
			};



			SUT.AddChild(button);

			SUT.Measure(new Size(20, 20));
			SUT.Arrange(new Rect(0, 0, 20, 20));

			//button.Click;
			flyout.ShowAt(button);

			Assert.AreEqual(button.LayoutSlot.X, flyout._popup.LayoutSlot.X);
		}
	}
}
