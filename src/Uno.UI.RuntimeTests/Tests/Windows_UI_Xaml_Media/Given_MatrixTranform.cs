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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	class Given_MatrixTranform
	{
		[TestMethod]
		public async Task When_Identity_And_TransformPoint()
		{
			await Dispatch(() =>
			{
				var SUT = new MatrixTransform();

				Assert.AreEqual(new Point(0, 0), SUT.TransformPoint(new Point(0, 0)));
			});
		}

		private async Task Dispatch(DispatchedHandler p)
		{
#if !NETFX_CORE
			await CoreApplication.GetCurrentView().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, p);
#else
			await CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, p);
#endif
		}

		[TestMethod]
		public async Task When_Identity_And_TransformBounds()
		{
			await Dispatch(() =>
			{
				var SUT = new MatrixTransform();

				Assert.AreEqual(new Rect(0, 0, 0, 0), SUT.TransformBounds(new Rect(0, 0, 0, 0)));
			});
		}

		[TestMethod]
		public async Task When_Translate_And_TransformPoint()
		{
			await Dispatch(() =>
			{
				var SUT = new MatrixTransform()
				{
					Matrix = new Matrix(1, 0, 0, 1, 10, 20)
				};

				Assert.AreEqual(new Point(10, 20), SUT.TransformPoint(new Point(0, 0)));
			});
		}

		[TestMethod]
		public async Task When_Translate_And_TransformBounds()
		{
			await Dispatch(() =>
			{
				var SUT = new MatrixTransform()
				{
					Matrix = new Matrix(1, 0, 0, 1, 10, 20)
				};

				Assert.AreEqual(
					new Rect(10, 20, 5, 6),
					SUT.TransformBounds(new Rect(0, 0, 5, 6))
				);
			});
		}

		[TestMethod]
		public async Task When_Rotate_And_TransformPoint()
		{
			await Dispatch(() =>
			{
				var SUT = new MatrixTransform()
				{
					Matrix = MatrixHelper.FromMatrix3x2(Matrix3x2.CreateRotation((float)Math.PI / 2))
				};

				Assert.AreEqual(new Point(-1, 1), SUT.TransformPoint(new Point(1, 1)));
			});
		}

		[TestMethod]
		public async Task When_Rotate_And_TransformBounds()
		{
			await Dispatch(() =>
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
		}

		[TestMethod]
		public async Task When_RotateQuarter_And_TransformPoint()
		{
			await Dispatch(() =>
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
		}

		[TestMethod]
		public async Task When_RotateQuarter_And_TransformBounds()
		{
			await Dispatch(() =>
			{
				var SUT = new MatrixTransform()
				{
					Matrix = MatrixHelper.FromMatrix3x2(Matrix3x2.CreateRotation((float)Math.PI / 4))
				};

				var expected = new Rect(-4.24264097213745, 1.41421353816986, 7.77817463874817, 7.77817499637604);
				var actual = SUT.TransformBounds(new Rect(1, 1, 5, 6));

				Assert.AreEqual(expected.Y, actual.Y, 1e-10, $"{expected} != {actual}");
				Assert.AreEqual(expected.X, actual.X, 1e-10, $"{expected} != {actual}");
				Assert.AreEqual(expected.Width, actual.Width, 1e-10, $"{expected} != {actual}");
				Assert.AreEqual(expected.Height, actual.Height, 1e-10, $"{expected} != {actual}");
			});
		}
	}

	public class MatrixHelper
	{
		public static Matrix FromMatrix3x2(Matrix3x2 m) =>
			new Matrix(
				m.M11,
				m.M12,
				m.M21,
				m.M22,
				m.M31,
				m.M32
			);

		public static Matrix3x2 ToMatrix3x2(Matrix m) =>
			new Matrix3x2(
				(float)m.M11,
				(float)m.M12,
				(float)m.M21,
				(float)m.M22,
				(float)m.OffsetX,
				(float)m.OffsetY
			);
	}
}
