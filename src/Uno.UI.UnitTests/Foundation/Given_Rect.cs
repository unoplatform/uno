using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Uno.UI.Tests.Foundation
{
	[TestClass]
	public class Given_Rect
	{
		[TestInitialize]
		public void Init() => Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight = false;

		[TestCleanup]
		public void Cleanup() => Uno.FoundationFeatureConfiguration.Rect.RestoreDefaults();

		[TestMethod]
		public void When_Create_WithNegativeWidth_With_FeatureFlagEnabled()
		{
			Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight = true;

			var sut = new Rect(0, 0, -42, 0);

			Assert.AreEqual(-42, sut.Width);
		}

		[TestMethod]
		public void When_Create_WithNegativeHeight_With_FeatureFlagEnabled()
		{
			Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight = true;

			var sut = new Rect(0, 0, 0, -42);

			Assert.AreEqual(-42, sut.Height);
		}

		[TestMethod]
		public void When_Create_WithNegativeWidth()
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Rect(0, 0, -42, 0));
		}

		[TestMethod]
		public void When_Create_WithNegativeHeight()
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Rect(0, 0, 0, -42));
		}

		[TestMethod]
		public void When_Create_WithNegativeSizeWidth()
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Rect(new Point(0, 0), new Size(-42, 0)));
		}

		[TestMethod]
		public void When_Create_WithNegativeSizeHeight()
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Rect(new Point(0, 0), new Size(0, -42)));
		}

		[TestMethod]
		public void When_Create_WithDisorderedPoints_1()
		{
			var sut = new Rect(new Point(0, 0), new Point(-42, 42));

			Assert.AreEqual(-42, sut.X);
			Assert.AreEqual(0, sut.Y);
			Assert.AreEqual(42, sut.Width);
			Assert.AreEqual(42, sut.Height);

			Assert.AreEqual(-42, sut.Left);
			Assert.AreEqual(0, sut.Top);
			Assert.AreEqual(0, sut.Right);
			Assert.AreEqual(42, sut.Bottom);
		}

		[TestMethod]
		public void When_Create_WithDisorderedPoints_2()
		{
			var sut = new Rect(new Point(0, 0), new Point(42, -42));

			Assert.AreEqual(0, sut.X);
			Assert.AreEqual(-42, sut.Y);
			Assert.AreEqual(42, sut.Width);
			Assert.AreEqual(42, sut.Height);

			Assert.AreEqual(0, sut.Left);
			Assert.AreEqual(-42, sut.Top);
			Assert.AreEqual(42, sut.Right);
			Assert.AreEqual(0, sut.Bottom);
		}

		[TestMethod]
		public void When_Create_WithDisorderedPoints_3()
		{
			var sut = new Rect(new Point(0, 0), new Point(-42, -42));

			Assert.AreEqual(-42, sut.X);
			Assert.AreEqual(-42, sut.Y);
			Assert.AreEqual(42, sut.Width);
			Assert.AreEqual(42, sut.Height);

			Assert.AreEqual(-42, sut.Left);
			Assert.AreEqual(-42, sut.Top);
			Assert.AreEqual(0, sut.Right);
			Assert.AreEqual(0, sut.Bottom);
		}

		[TestMethod]
		public void When_Create_WithNaNWidth()
		{
			var sut = new Rect(0, 0, double.NaN, 42);

			Assert.AreEqual(0, sut.X);
			Assert.AreEqual(0, sut.Y);
			Assert.AreEqual(double.NaN, sut.Width);
			Assert.AreEqual(42, sut.Height);

			Assert.AreEqual(0, sut.Left);
			Assert.AreEqual(0, sut.Top);
			Assert.AreEqual(double.NaN, sut.Right);
			Assert.AreEqual(42, sut.Bottom);
		}

		[TestMethod]
		public void When_Create_WithNaNHeight()
		{
			var sut = new Rect(0, 0, 42, double.NaN);

			Assert.AreEqual(0, sut.X);
			Assert.AreEqual(0, sut.Y);
			Assert.AreEqual(42, sut.Width);
			Assert.AreEqual(double.NaN, sut.Height);

			Assert.AreEqual(0, sut.Left);
			Assert.AreEqual(0, sut.Top);
			Assert.AreEqual(42, sut.Right);
			Assert.AreEqual(double.NaN, sut.Bottom);
		}

		[TestMethod]
		public void When_Create_WithNaNPoints_1()
		{
			var sut = new Rect(new Point(double.NaN, 0), new Point(-42, 42));

			Assert.AreEqual(double.NaN, sut.X);
			Assert.AreEqual(0, sut.Y);
			Assert.AreEqual(double.NaN, sut.Width);
			Assert.AreEqual(42, sut.Height);

			Assert.AreEqual(double.NaN, sut.Left);
			Assert.AreEqual(0, sut.Top);
			Assert.AreEqual(double.NaN, sut.Right);
			Assert.AreEqual(42, sut.Bottom);
		}

		[TestMethod]
		public void When_Create_WithNaNPoints_2()
		{
			var sut = new Rect(new Point(0, double.NaN), new Point(-42, 42));

			Assert.AreEqual(-42, sut.X);
			Assert.AreEqual(double.NaN, sut.Y);
			Assert.AreEqual(42, sut.Width);
			Assert.AreEqual(double.NaN, sut.Height);

			Assert.AreEqual(-42, sut.Left);
			Assert.AreEqual(double.NaN, sut.Top);
			Assert.AreEqual(0, sut.Right);
			Assert.AreEqual(double.NaN, sut.Bottom);
		}
	}
}
