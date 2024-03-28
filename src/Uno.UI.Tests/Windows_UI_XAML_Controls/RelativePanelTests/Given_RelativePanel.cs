using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using Uno.Extensions;

namespace Uno.UI.Tests.RelativePanelTests
{
	[TestClass]
	public class Given_RelativePanel : Context
	{
		[TestMethod]
		public void When_Empty_And_MeasuredEmpty()
		{
			var SUT = new RelativePanel() { Name = "test" };

			SUT.Measure(default(Size));
			SUT.Arrange(default(Rect));

			Assert.AreEqual(default(Size), SUT.DesiredSize);
			Assert.IsTrue(SUT.GetChildren().None());
		}
	}
}
