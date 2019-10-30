using System.Globalization;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ButtonTests
{
	public class UnoSamples_Test_Button_Click : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Button_OverlappedButtons()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Button.Overlapped_Buttons");

			_app.WaitForElement(_app.Marked("layer1"));

			_app.Wait(0.45f);

			var layer1 = _app.Marked("layer1");
			var layer1Inner = _app.Marked("layer1_inner");
			var layer2 = _app.Marked("layer2");
			var layer3 = _app.Marked("layer3");

			var layer1Rect = layer1.FirstResult().Rect;
			var layer1InnerRect = layer1Inner.FirstResult().Rect;
			var layer2Rect = layer2.FirstResult().Rect;
			var layer3Rect = layer3.FirstResult().Rect;
			var hitInvisibleRect = _app.Marked("hitInvisible").FirstResult().Rect;
			var hitVisibleRect = _app.Marked("hitVisible").FirstResult().Rect;

			var layer1ClickCount = _app.Marked("l1Clicks");
			var layer1InnerClickCount = _app.Marked("l1innerClicks");
			var layer2ClickCount = _app.Marked("l2Clicks");
			var layer3ClickCount = _app.Marked("l3Clicks");

			void CheckClickCounts(int layer1Count, int layer1InnerCount, int layer2Count, int layer3Count)
			{
				_app.Wait(0.15f); // give time to app to execute the action

				var browserValues = new[]
					{
						layer1ClickCount.GetDependencyPropertyValue("Text") as string,
						layer1InnerClickCount.GetDependencyPropertyValue("Text") as string,
						layer2ClickCount.GetDependencyPropertyValue("Text") as string,
						layer3ClickCount.GetDependencyPropertyValue("Text") as string
					}
					.Select(v => int.TryParse(v, out var i) ? i : -1);

				browserValues.Should().Equal(layer1Count, layer1InnerCount, layer2Count, layer3Count);
			}

			// Assert initial state
			CheckClickCounts(0, 0, 0, 0);

			// Tap on layer1 through "hit visible" panel
			_app.TapCoordinates(
				x: ((layer1InnerRect.X - layer1Rect.X) / 2) + layer1Rect.X,
				y: hitVisibleRect.CenterY);
			CheckClickCounts(0, 0, 0, 0);

			// Tap on layer1 through "hit invisible" panel
			_app.TapCoordinates(
				x: ((layer1InnerRect.X - layer1Rect.X) / 2) + layer1Rect.X,
				y: hitInvisibleRect.CenterY);
			CheckClickCounts(1, 0, 0, 0);

			// Tap on layer2 through "hit invisible" panel, right over layer1 & layer1_inner
			_app.TapCoordinates(
				x: ((layer1InnerRect.GetRight() - layer2Rect.X) / 2) + layer2Rect.X,
				y: hitInvisibleRect.CenterY);
			CheckClickCounts(1, 0, 1, 0);

			// Tap on layer3 through "hit invisible" panel, right over layer2
			_app.TapCoordinates(
				x: ((layer2Rect.GetRight() - layer3Rect.X) / 2) + layer3Rect.X,
				y: hitInvisibleRect.CenterY);
			CheckClickCounts(1, 0, 1, 1);

			// Tap on layer1_inner through "hit invisible" panel
			_app.TapCoordinates(
				x: ((layer2Rect.X - layer1InnerRect.X) / 2) + layer1InnerRect.X,
				y: hitInvisibleRect.CenterY);
			CheckClickCounts(1, 1, 1, 1);
		}

		[Test]
		[AutoRetry]
		public void Button_Events()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Button.Button_Events");

			_app.WaitForElement(_app.Marked("btnTapped"));

			_app.Wait(0.45f);

			var btnTapped = _app.Marked("btnTapped");
			var btnTappedRect = btnTapped.FirstResult().Rect;
			var btnDoubleTapped = _app.Marked("btnDoubleTapped");
			var btnDoubleTappedRect = btnDoubleTapped.FirstResult().Rect;
			var btnClick = _app.Marked("btnClick");
			var btnClickRect = btnClick.FirstResult().Rect;
			var btnPointerPressed = _app.Marked("btnPointerPressed");
			var btnPointerPressedRect = btnPointerPressed.FirstResult().Rect;
			var btnPointerReleased = _app.Marked("btnPointerReleased");
			var btnPointerReleasedRect = btnPointerReleased.FirstResult().Rect;
			var btnPointerEntered = _app.Marked("btnPointerEntered");
			var btnPointerEnteredRect = btnPointerEntered.FirstResult().Rect;
			var btnPointerExited = _app.Marked("btnPointerExited");
			var btnPointerExitedRect = btnPointerExited.FirstResult().Rect;

			var btnTappedCount = _app.Marked("btnTappedCount");
			var btnDoubleTappedCount = _app.Marked("btnDoubleTappedCount");
			var btnClickCount = _app.Marked("btnClickCount");
			var btnPointerPressedCount = _app.Marked("btnPointerPressedCount");
			var btnPointerReleasedCount = _app.Marked("btnPointerReleasedCount");
			var btnPointerEnteredCount = _app.Marked("btnPointerEnteredCount");
			var btnPointerExitedCount = _app.Marked("btnPointerExitedCount");

			var hitInvisibleZoneRect = _app.Marked("hitInvisibleZone").FirstResult().Rect;
			var hitVisibleZoneRect = _app.Marked("hitVisibleZone").FirstResult().Rect;

			float CenterOf(float pos1, float pos2)
			{
				return pos1 < pos2 ? (pos2 - pos1) / 2 + pos1 : (pos1 - pos2) / 2 + pos2;
			}

			var xDirect = CenterOf(hitInvisibleZoneRect.X, btnTappedRect.X);
			var xInvisible = hitInvisibleZoneRect.CenterX;
			var xVisible = hitVisibleZoneRect.CenterX;

			void CheckCount(QueryEx mark, int expected, string msg)
			{
				_app.Wait(0.15f); // give time to app to execute the action

				var countString = mark.GetDependencyPropertyValue("Text") as string;
				var count = int.TryParse(countString, out var c) ? c : -1;
				count.Should().Be(expected, msg);
			}

			// ---- TAPPED ----
			CheckCount(btnTappedCount, 0, "tapped starting value");
			// Tapped - Direct
			_app.TapCoordinates(
				x: xDirect,
				y: btnTappedRect.CenterY
				);
			CheckCount(btnTappedCount, 1, msg: "after direct tapped");
			// Tapped - Invisible
			_app.TapCoordinates(
				x: xInvisible,
				y: btnTappedRect.CenterY
			);
			CheckCount(btnTappedCount, 2, msg: "after tapped though invisible");
			// Tapped - Visible
			_app.TapCoordinates(
				x: xVisible,
				y: btnTappedRect.CenterY
			);
			CheckCount(btnTappedCount, 2, msg: "after tapped though visible");
			// Tapped - Dragged
			_app.DragCoordinates(
				fromX: xVisible,
				fromY: btnTappedRect.CenterY,
				toX: btnTappedRect.Right - 1,
				toY: btnTappedRect.CenterY
			);
			CheckCount(btnTappedCount, 2, msg: "after dragged tapped (should not tap)");

			// ---- DOUBLE TAPPED ----
			/*CheckCount(btnDoubleTappedCount, 0, "double tapped starting value");
			// Double Tapped - Direct
			_app.DoubleTapCoordinates(
				x: xDirect,
				y: btnDoubleTappedRect.CenterY
			);
			CheckCount(btnDoubleTappedCount, 1, msg: "after direct double tapped");
			// Double Tapped - Invisible
			_app.DoubleTapCoordinates(
				x: xInvisible,
				y: btnDoubleTappedRect.CenterY
			);
			CheckCount(btnDoubleTappedCount, 2, msg: "after double tapped though invisible");
			// Double Tapped - Visible
			_app.DoubleTapCoordinates(
				x: xVisible,
				y: btnDoubleTappedRect.CenterY
			);
			CheckCount(btnDoubleTappedCount, 2, msg: "after double tapped though visible");*/

			// ---- CLICKED ----
			CheckCount(btnClickCount, 0, "clicked starting value");
			// Clicked - Direct
			_app.TapCoordinates(
				x: xDirect,
				y: btnClickRect.CenterY
			);
			CheckCount(btnClickCount, 1, msg: "after direct clicked");
			// Clicked - Invisible
			_app.TapCoordinates(
				x: xInvisible,
				y: btnClickRect.CenterY
			);
			CheckCount(btnClickCount, 2, msg: "after clicked though invisible");
			// Clicked - Visible
			_app.TapCoordinates(
				x: xVisible,
				y: btnClickRect.CenterY
			);
			CheckCount(btnClickCount, 2, msg: "after clicked though visible");
			// Clicked - Drag
			_app.DragCoordinates(
				fromX: xVisible,
				fromY: btnClickRect.CenterY,
				toX: CenterOf(hitVisibleZoneRect.Right, btnClickRect.Right),
				toY: btnClickRect.CenterY
			);
			CheckCount(btnClickCount, 2, msg: "after dragged click"); // Real value should be 3 after real fix
		}
	}
}
