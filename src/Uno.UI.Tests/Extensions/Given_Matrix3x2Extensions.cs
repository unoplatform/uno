using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;

namespace Uno.UI.Tests.Extensions
{
	[TestClass]
	public class Given_Matrix3x2Extensions
	{
		[TestMethod]
		public void When_TransformPoint_RotateQuarter()
		{
			var matrix = Matrix3x2.CreateRotation((float)Math.PI / 4);

			var expected = new Point(0, Math.Sqrt(2) * 42);
			var actual = matrix.Transform(new Point(42, 42));

			Assert.AreEqual(expected.X, actual.X, 1e-5, $"X: {expected} != {actual}");
			Assert.AreEqual(expected.Y, actual.Y, 1e-5, $"Y: {expected} != {actual}");
		}

		[TestMethod]
		public void When_TransformRect_RotateQuarter()
		{
			var matrix = Matrix3x2.CreateRotation((float) Math.PI / 4);

			var expected = new Rect(-4.242640495300293, 1.4142135381698608, 7.77817440032959, 7.7781739234924316);
			var actual = matrix.Transform(new Rect(1, 1, 5, 6));

			Assert.AreEqual(expected.X, actual.X, 1e-5, $"X: {expected} != {actual}");
			Assert.AreEqual(expected.Y, actual.Y, 1e-5, $"Y: {expected} != {actual}");
			Assert.AreEqual(expected.Width, actual.Width, 1e-5, $"W: {expected} != {actual}");
			Assert.AreEqual(expected.Height, actual.Height, 1e-5, $"H: {expected} != {actual}");
		}
	}
}
