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
	[UITestClass]
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
			var matrix = Matrix3x2.CreateRotation((float)Math.PI / 4);

			var expected = new Rect(-4.242640495300293, 1.4142135381698608, 7.77817440032959, 7.7781739234924316);
			var actual = matrix.Transform(new Rect(1, 1, 5, 6));

			Assert.AreEqual(expected.X, actual.X, 1e-5, $"X: {expected} != {actual}");
			Assert.AreEqual(expected.Y, actual.Y, 1e-5, $"Y: {expected} != {actual}");
			Assert.AreEqual(expected.Width, actual.Width, 1e-5, $"W: {expected} != {actual}");
			Assert.AreEqual(expected.Height, actual.Height, 1e-5, $"H: {expected} != {actual}");
		}

		[TestMethod]
		public void When_TransformRect_Translation_Then_Matches_Corner_Walk_Exactly()
		{
			var rects = new[]
			{
				new Rect(0, 0, 0, 0),
				new Rect(10.3, 20.7, 100.1, 50.9),
				new Rect(-1234.56789, 987.654321, 0.333333, 1e6),
				new Rect(1e7 + 0.1, -1e7 - 0.1, 3.14159, 2.71828),
			};
			var matrices = new[]
			{
				Matrix3x2.Identity,
				Matrix3x2.CreateTranslation(12.5f, -7.25f),
				Matrix3x2.CreateTranslation(0.1f, 0.2f),
				Matrix3x2.CreateTranslation(-1e6f, 3e5f),
			};

			foreach (var rect in rects)
			{
				foreach (var matrix in matrices)
				{
					Assert.AreEqual(CornerWalk(matrix, rect), matrix.Transform(rect), $"{rect} by {matrix}");
				}
			}
		}

		// The general path, applied to every matrix: transform the four corners and take their bounds.
		private static Rect CornerWalk(Matrix3x2 matrix, Rect rect)
		{
			var corners = new[]
			{
				matrix.Transform(rect.Left, rect.Top),
				matrix.Transform(rect.Left, rect.Bottom),
				matrix.Transform(rect.Right, rect.Top),
				matrix.Transform(rect.Right, rect.Bottom),
			};

			return new Rect(
				new Point(corners.Min(c => c.X), corners.Min(c => c.Y)),
				new Point(corners.Max(c => c.X), corners.Max(c => c.Y)));
		}
	}
}
