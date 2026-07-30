using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_CompositeTransform
	{
		private const double PointTolerance = 0.5;

		// Each test hosts an element with a CompositeTransform + RenderTransformOrigin and verifies
		// individual point mappings via TransformToVisual. These are the exact scenarios that broke
		// in the user's clock app and they run identically on every backend (Uno Skia, Uno mobile,
		// Uno WASM, native WinUI), so they double as parity checks against the reference platform.

		[TestMethod]
		public async Task When_OnlyLocalCenter_NoOrigin_MatchesRotateTransform()
		{
			// CompositeTransform with only its own CenterX/Y and a 90° rotation should behave
			// identically to a plain RotateTransform with the same center (no RenderTransformOrigin
			// involved on either side).
			var composite = await HostAndGetTransform(
				new CompositeTransform { CenterX = 50, CenterY = 50, Rotation = 90 });
			var rotate = await HostAndGetTransform(
				new RotateTransform { CenterX = 50, CenterY = 50, Angle = 90 });

			// Three telltale points: a corner, the local pivot, and a far corner.
			AssertSameMapping(composite, rotate, new Point(0, 0));
			AssertSameMapping(composite, rotate, new Point(50, 50));
			AssertSameMapping(composite, rotate, new Point(100, 100));
		}

		[TestMethod]
		public async Task When_Rotation_AndOrigin_PivotsAroundOrigin()
		{
			// 100x100 element, RenderTransformOrigin (0.5, 0.5) = (50, 50) in element-local pixels,
			// CompositeTransform Rotation = 90°. The top-left corner (0, 0) rotates 90° around the
			// center and lands at (100, 0); the pivot stays put.
			var transform = await HostAndGetTransform(new CompositeTransform { Rotation = 90 },
				origin: new Point(0.5, 0.5));

			AssertMappedTo(transform, new Point(50, 50), new Point(50, 50));
			AssertMappedTo(transform, new Point(0, 0), new Point(100, 0));
		}

		[TestMethod]
		public async Task When_Skew_AndOrigin_PivotsAroundOrigin()
		{
			// 100x100 element, SkewX = 30° around the bottom-center (0.5, 1) = (50, 100).
			// Under a skew the pivot point is invariant; this is what the buggy code got wrong.
			var transform = await HostAndGetTransform(new CompositeTransform { SkewX = 30 },
				origin: new Point(0.5, 1));

			AssertMappedTo(transform, new Point(50, 100), new Point(50, 100));
		}

		[TestMethod]
		public async Task When_Scale_AndOrigin_PivotsAroundOrigin()
		{
			// 100x100 element, ScaleX/Y = 0.5 around the bottom-right (1, 1) = (100, 100).
			// The bottom-right pixel must be invariant under the scale.
			var transform = await HostAndGetTransform(new CompositeTransform { ScaleX = 0.5, ScaleY = 0.5 },
				origin: new Point(1, 1));

			AssertMappedTo(transform, new Point(100, 100), new Point(100, 100));
		}

		[TestMethod]
		public async Task When_Origin_AndLocalCenter_Stack()
		{
			// RenderTransformOrigin (0.5, 0.5) -> (50, 50) plus the CompositeTransform's own
			// CenterX/Y = 10 must add up: the effective rotation pivot is (60, 60).
			var transform = await HostAndGetTransform(
				new CompositeTransform { CenterX = 10, CenterY = 10, Rotation = 90 },
				origin: new Point(0.5, 0.5));

			AssertMappedTo(transform, new Point(60, 60), new Point(60, 60));
			AssertMappedTo(transform, new Point(0, 0), new Point(120, 0));
		}

		[TestMethod]
		public async Task When_OnlyTranslate_OriginIsIrrelevant()
		{
			// Pure translate is independent of the origin (the implicit origin wrap-around cancels
			// for translation). Two transforms with different origins must map points identically.
			var atZero = await HostAndGetTransform(
				new CompositeTransform { TranslateX = 15, TranslateY = 20 },
				origin: new Point(0, 0));
			var atArbitrary = await HostAndGetTransform(
				new CompositeTransform { TranslateX = 15, TranslateY = 20 },
				origin: new Point(0.73, 0.41));

			AssertSameMapping(atZero, atArbitrary, new Point(0, 0));
			AssertSameMapping(atZero, atArbitrary, new Point(40, 60));
			AssertSameMapping(atZero, atArbitrary, new Point(100, 100));
		}

		[TestMethod]
		public async Task When_ClockHand_RotatesFromBase()
		{
			// Mirrors the user's nightstand-clock repro: a 14x200 rectangle with
			// RenderTransformOrigin (0.5, 1) -> absolute pivot (7, 200) and a CompositeTransform
			// rotated 180°. The base of the hand stays put; the tip swings to y = 400.
			var sut = new Rectangle
			{
				Width = 14,
				Height = 200,
				Fill = new SolidColorBrush(Colors.White),
				RenderTransformOrigin = new Point(0.5, 1),
				RenderTransform = new CompositeTransform { Rotation = 180 },
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
			};
			var host = new Grid { Width = 100, Height = 500, Children = { sut } };

			TestServices.WindowHelper.WindowContent = host;
			await TestServices.WindowHelper.WaitForLoaded(sut);
			await TestServices.WindowHelper.WaitForIdle();

			var pivot = sut.TransformToVisual(host).TransformPoint(new Point(7, 200));
			Assert.AreEqual(7, pivot.X, PointTolerance);
			Assert.AreEqual(200, pivot.Y, PointTolerance);

			var tip = sut.TransformToVisual(host).TransformPoint(new Point(7, 0));
			Assert.AreEqual(7, tip.X, PointTolerance);
			Assert.AreEqual(400, tip.Y, PointTolerance);
		}

		[TestMethod]
		public async Task When_AllSubTransforms_HonourOrigin()
		{
			// All four CompositeTransform components together (Scale, Skew, Rotate, Translate),
			// with both a RenderTransformOrigin and a non-zero local CenterX/Y. The pivot point
			// (RenderTransformOrigin + CompositeTransform.CenterX/Y) must be invariant under
			// Scale/Skew/Rotate; the final Translate then shifts it by exactly TranslateX/Y.
			var transform = await HostAndGetTransform(
				new CompositeTransform
				{
					CenterX = 10,
					CenterY = 10,
					ScaleX = 0.8,
					ScaleY = 0.8,
					SkewX = 30,
					SkewY = 30,
					Rotation = 30,
					TranslateX = 15,
					TranslateY = 20,
				},
				origin: new Point(0.5, 0.5));

			// Effective pivot = origin (50, 50) + local (10, 10) = (60, 60).
			// After the Translate, that point lands at (60 + 15, 60 + 20) = (75, 80).
			AssertMappedTo(transform, new Point(60, 60), new Point(75, 80));
		}

		// Hosts a 100x100 Border with the given RenderTransform and (optional) RenderTransformOrigin,
		// then returns the GeneralTransform from the element to its host so individual points can be
		// mapped with TransformPoint(). The host is large enough to fit the rotated rectangle.
		private static async Task<GeneralTransform> HostAndGetTransform(Transform renderTransform, Point origin = default)
		{
			var sut = new Border
			{
				Width = 100,
				Height = 100,
				Background = new SolidColorBrush(Colors.Red),
				RenderTransformOrigin = origin,
				RenderTransform = renderTransform,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
			};
			var host = new Grid { Width = 300, Height = 300, Children = { sut } };

			TestServices.WindowHelper.WindowContent = host;
			await TestServices.WindowHelper.WaitForLoaded(sut);
			await TestServices.WindowHelper.WaitForIdle();

			return sut.TransformToVisual(host);
		}

		private static void AssertMappedTo(GeneralTransform transform, Point input, Point expected)
		{
			var actual = transform.TransformPoint(input);
			Assert.AreEqual(expected.X, actual.X, PointTolerance, $"X for input {input}: expected {expected}, got {actual}");
			Assert.AreEqual(expected.Y, actual.Y, PointTolerance, $"Y for input {input}: expected {expected}, got {actual}");
		}

		private static void AssertSameMapping(GeneralTransform a, GeneralTransform b, Point input)
		{
			var pa = a.TransformPoint(input);
			var pb = b.TransformPoint(input);
			Assert.AreEqual(pa.X, pb.X, PointTolerance, $"X for input {input}: a={pa}, b={pb}");
			Assert.AreEqual(pa.Y, pb.Y, PointTolerance, $"Y for input {input}: a={pa}, b={pb}");
		}
	}
}
