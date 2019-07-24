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
				_app.Wait(0.15f); // give time to browser to execute the action

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
				y: hitVisibleRect.GetCenter().y);
			CheckClickCounts(0, 0, 0, 0);

			// Tap on layer1 through "hit invisible" panel
			_app.TapCoordinates(
				x: ((layer1InnerRect.X - layer1Rect.X) / 2) + layer1Rect.X,
				y: hitInvisibleRect.GetCenter().y);
			CheckClickCounts(1, 0, 0, 0);

			// Tap on layer2 through "hit invisible" panel, right over layer1 & layer1_inner
			_app.TapCoordinates(
				x: ((layer1InnerRect.GetRight() - layer2Rect.X) / 2) + layer2Rect.X,
				y: hitInvisibleRect.GetCenter().y);
			CheckClickCounts(1, 0, 1, 0);

			// Tap on layer3 through "hit invisible" panel, right over layer2
			_app.TapCoordinates(
				x: ((layer2Rect.GetRight() - layer3Rect.X) / 2) + layer3Rect.X,
				y: hitInvisibleRect.GetCenter().y);
			CheckClickCounts(1, 0, 1, 1);

			// Tap on layer1_inner through "hit invisible" panel
			_app.TapCoordinates(
				x: ((layer2Rect.X - layer1InnerRect.X) / 2) + layer1InnerRect.X,
				y: hitInvisibleRect.GetCenter().y);
			CheckClickCounts(1, 1, 1, 1);
		}
	}
}
