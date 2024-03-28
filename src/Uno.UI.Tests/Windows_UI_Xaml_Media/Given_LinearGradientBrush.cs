using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	public class Given_LinearGradientBrush
	{
		private const float Delta = 0.001f;

		[TestMethod]
		public void When_DefaultConstructor()
		{
			var linear = new LinearGradientBrush();
			AssertPointsAlmostEqual(new Point(), linear.StartPoint);
			AssertPointsAlmostEqual(new Point(1, 1), linear.EndPoint);
		}

		[TestMethod]
		public void When_ZeroAngle()
		{
			var linear = new LinearGradientBrush(new GradientStopCollection(), 0);
			AssertPointsAlmostEqual(new Point(), linear.StartPoint);
			AssertPointsAlmostEqual(new Point(1, 0), linear.EndPoint);
		}

		[TestMethod]
		public void When_FullAngle()
		{
			var linear = new LinearGradientBrush(new GradientStopCollection(), 360);
			AssertPointsAlmostEqual(new Point(), linear.StartPoint);
			AssertPointsAlmostEqual(new Point(1, 0), linear.EndPoint);
		}

		[TestMethod]
		public void When_ThreeTimesFullAngle()
		{
			var linear = new LinearGradientBrush(new GradientStopCollection(), 1080);
			AssertPointsAlmostEqual(new Point(), linear.StartPoint);
			AssertPointsAlmostEqual(new Point(1, 0), linear.EndPoint);
		}

		[TestMethod]
		public void When_NegativeFullAngle()
		{
			var linear = new LinearGradientBrush(new GradientStopCollection(), -360);
			AssertPointsAlmostEqual(new Point(), linear.StartPoint);
			AssertPointsAlmostEqual(new Point(1, 0), linear.EndPoint);
		}

		[TestMethod]
		public void When_HalfAngle()
		{
			var linear = new LinearGradientBrush(new GradientStopCollection(), 180);
			AssertPointsAlmostEqual(new Point(), linear.StartPoint);
			AssertPointsAlmostEqual(new Point(-1, 0), linear.EndPoint);
		}

		[TestMethod]
		public void When_RightAngle()
		{
			var linear = new LinearGradientBrush(new GradientStopCollection(), 90);
			AssertPointsAlmostEqual(new Point(), linear.StartPoint);
			AssertPointsAlmostEqual(new Point(0, 1), linear.EndPoint);
		}

		[TestMethod]
		public void When_NegativeRightAngle()
		{
			var linear = new LinearGradientBrush(new GradientStopCollection(), -90);
			AssertPointsAlmostEqual(new Point(), linear.StartPoint);
			AssertPointsAlmostEqual(new Point(0, -1), linear.EndPoint);
		}

		[TestMethod]
		public void When_135Angle()
		{
			var linear = new LinearGradientBrush(new GradientStopCollection(), 135);
			AssertPointsAlmostEqual(new Point(), linear.StartPoint);
			AssertPointsAlmostEqual(new Point(-0.7071, 0.7071), linear.EndPoint);
		}

		[TestMethod]
		public void When_ArbitraryAngle()
		{
			var linear = new LinearGradientBrush(new GradientStopCollection(), 78035);
			AssertPointsAlmostEqual(new Point(), linear.StartPoint);
			AssertPointsAlmostEqual(new Point(0.0871, -0.9961), linear.EndPoint);
		}

		private void AssertPointsAlmostEqual(Point expected, Point actual)
		{
			Assert.AreEqual(expected.X, actual.X, Delta);
			Assert.AreEqual(expected.Y, actual.Y, Delta);
		}
	}
}
