using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	public class Given_MatrixTranform
	{
		[TestMethod]
		public Task When_Identity_And_TransformPoint() =>
			RunOnUIThread.ExecuteAsync(() =>
			{
				var SUT = new MatrixTransform();

				Assert.AreEqual(new Point(0, 0), SUT.TransformPoint(new Point(0, 0)));
			});

		[TestMethod]
		public Task When_Identity_And_TransformBounds() =>
			RunOnUIThread.ExecuteAsync(() =>
			{
				var SUT = new MatrixTransform();

				Assert.AreEqual(new Rect(0, 0, 0, 0), SUT.TransformBounds(new Rect(0, 0, 0, 0)));
			});

		[TestMethod]
		public Task When_Translate_And_TransformPoint() =>
			RunOnUIThread.ExecuteAsync(() =>
			{
				var SUT = new MatrixTransform()
				{
					Matrix = MatrixHelper.Create(1, 0, 0, 1, 10, 20)
				};

				Assert.AreEqual(new Point(10, 20), SUT.TransformPoint(new Point(0, 0)));
			});

		[TestMethod]
		public Task When_Translate_And_TransformBounds() =>
			RunOnUIThread.ExecuteAsync(() =>
			{
				var SUT = new MatrixTransform()
				{
					Matrix = MatrixHelper.Create(1, 0, 0, 1, 10, 20)
				};

				Assert.AreEqual(
					new Rect(10, 20, 5, 6),
					SUT.TransformBounds(new Rect(0, 0, 5, 6))
				);
			});

		[TestMethod]
		public Task When_Rotate_And_TransformPoint() =>
			RunOnUIThread.ExecuteAsync(() =>
			{
				var SUT = new MatrixTransform()
				{
					Matrix = MatrixHelper.FromMatrix3x2(Matrix3x2.CreateRotation((float)Math.PI / 2))
				};

				Assert.AreEqual(new Point(-1, 1), SUT.TransformPoint(new Point(1, 1)));
			});

		[TestMethod]
		public Task When_Rotate_And_TransformBounds() =>
			RunOnUIThread.ExecuteAsync(() =>
			{
				var SUT = new MatrixTransform()
				{
					Matrix = MatrixHelper.FromMatrix3x2(Matrix3x2.CreateRotation((float)Math.PI / 2))
				};

				Assert.AreEqual(
					new Rect(-7, 1, 6, 5),
					SUT.TransformBounds(new Rect(1, 1, 5, 6))
				);
			});

		[TestMethod]
		public Task When_RotateQuarter_And_TransformPoint() =>
			RunOnUIThread.ExecuteAsync(() =>
			{
				var SUT = new MatrixTransform()
				{
					Matrix = MatrixHelper.FromMatrix3x2(Matrix3x2.CreateRotation((float)Math.PI / 4))
				};

				var expected = new Point(0, 1.41421353816986);
				var res = SUT.TransformPoint(new Point(1, 1));
				Assert.AreEqual(expected.X, res.X, 1e-10, $"{expected} != {res}");
				Assert.AreEqual(expected.Y, res.Y, 1e-10, $"{expected} != {res}");
			});

		[TestMethod]
		public Task When_RotateQuarter_And_TransformBounds() =>
			RunOnUIThread.ExecuteAsync(() =>
			{
				var SUT = new MatrixTransform()
				{
					Matrix = MatrixHelper.FromMatrix3x2(Matrix3x2.CreateRotation((float)Math.PI / 4))
				};

				var expected = new Rect(-4.242640495300293, 1.4142135381698608, 7.77817440032959, 7.7781739234924316);
				var actual = SUT.TransformBounds(new Rect(1, 1, 5, 6));

				Assert.AreEqual(expected.X, actual.X, 1e-5, $"X: {expected} != {actual}");
				Assert.AreEqual(expected.Y, actual.Y, 1e-5, $"Y: {expected} != {actual}");
				Assert.AreEqual(expected.Width, actual.Width, 1e-5, $"W: {expected} != {actual}");
				Assert.AreEqual(expected.Height, actual.Height, 1e-5, $"H: {expected} != {actual}");
			});
	}

	public class MatrixHelper
	{
		public static Matrix FromMatrix3x2(Matrix3x2 m) =>
			new Matrix
			{
				M11 = m.M11,
				M12 = m.M12,
				M21 = m.M21,
				M22 = m.M22,
				OffsetX = m.M31,
				OffsetY = m.M32
			};

		public static Matrix3x2 ToMatrix3x2(Matrix m) =>
			new Matrix3x2(
				(float)m.M11,
				(float)m.M12,
				(float)m.M21,
				(float)m.M22,
				(float)m.OffsetX,
				(float)m.OffsetY
			);

		internal static Matrix Create(int M11, int M12, int M21, int M22, int OffsetX, int OffsetY)
		{
			return new Matrix
			{
				M11 = M11,
				M12 = M12,
				M21 = M21,
				M22 = M22,
				OffsetX = OffsetX,
				OffsetY = OffsetY
			};
		}
	}
}
