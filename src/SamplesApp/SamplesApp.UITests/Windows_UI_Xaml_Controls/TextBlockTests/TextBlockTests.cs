using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBlockTests
{
	[TestFixture]
	public class TextBlockTests_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)] // Disabled for Browser because of missing top level .All() support
		public void When_Visibility_Changed_During_Arrange()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl.TextBlock_Visibility_Arrange");

			QueryEx textBlock = new QueryEx(q => q.All().Marked("SubjectTextBlock"));

			_app.WaitForElement(textBlock);

			_app.WaitForDependencyPropertyValue(textBlock, "Text", "It worked!");
		}

		[Test]
		[AutoRetry]
		public void When_Multiple_Controls_And_Some_Are_Collapsed_Arrange()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl.TextBlock_LineHeight_MultipleControls");

			var leftControls = new[]
			{
				_app.Marked("stackPanel1"), _app.Marked("txt1_1"), _app.Marked("rect1"), _app.Marked("txt1_2")
			};

			var rightControls = new[]
			{
				_app.Marked("stackPanel2"), _app.Marked("txt2_1"), _app.Marked("rect2"), _app.Marked("txt2_2")
			};

			_app.WaitForElement(rightControls.Last());

			for (var i = 0; i < leftControls.Length; i++)
			{
				var left = leftControls[i].FirstResult().Rect;
				var right = rightControls[i].FirstResult().Rect;

				left.Width.Should().Be(right.Width, "Width");
				left.Height.Should().Be(right.Height, "Height");
				left.Y.Should().Be(right.Y, "Y position");
			}
		}

		[Test]
		[AutoRetry]
		public void When_Foreground_Changed_With_Visibility()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl.TextBlock_Foreground_While_Collapsed");

			_app.WaitForText("FunnyTextBlock", "Look at me now");

			var blueBefore = TakeScreenshot("Before - blue");

			_app.FastTap("ChangeTextBlockButton");

			var blackBefore = TakeScreenshot("Before - black");

			var textRect = _app.GetRect("FunnyTextBlock");

			ImageAssert.AreNotEqual(blueBefore, blackBefore, textRect);

			_app.FastTap("ChangeTextBlockButton");

			//_app.WaitForNoElement("FunnyTextBlock"); // This times out on WASM because view is considered to be still there when collapsed - https://github.com/unoplatform/Uno.UITest/issues/25

			_app.FastTap("ChangeTextBlockButton");

			_app.WaitForElement("FunnyTextBlock");

			var blueAfter = TakeScreenshot("After - blue");

			ImageAssert.AreEqual(blueBefore, blueAfter, textRect);
		}

		[Test]
		[AutoRetry]
		public async Task When_TextDecoration_Changed()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl.TextBlock_Decorations");

			var text01 = _app.Marked("text01");
			var text02 = _app.Marked("text02");

			var before = TakeScreenshot("Before");

			text01.SetDependencyPropertyValue("TextDecorations", "1"); // Underline
			text02.SetDependencyPropertyValue("TextDecorations", "2"); // Strikethrough

			var after = TakeScreenshot("Updated");

			ImageAssert.AreNotEqual(before, after);

			text01.SetDependencyPropertyValue("TextDecorations", "0"); // None
			text02.SetDependencyPropertyValue("TextDecorations", "0"); // None

			var restored = TakeScreenshot("Restored");

			ImageAssert.AreEqual(before, restored);
		}

		[Test]
		[AutoRetry]
		public void When_FontWeight_Changed()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl.TextBlock_FontWeight_Dynamic");

			var testBlock = _app.Marked("testBlock");

			testBlock.SetDependencyPropertyValue("FontWeight", "Thin");
			var rectBefore = _app.GetRect("testBlock");
			var widthBefore = rectBefore.Width;
			var heightBefore = rectBefore.Height;

			testBlock.SetDependencyPropertyValue("FontWeight", "Heavy");
			var rectAfter = _app.GetRect("testBlock");
			var widthAfter = rectAfter.Width;
			var heightAfter = rectAfter.Height;

			Assert.IsTrue(widthBefore < widthAfter);
			Assert.IsTrue(heightBefore == heightAfter);
		}

		[Test]
		[AutoRetry]
		public void When_Foreground_Brush_Color_Changed()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBlockControl.TextBlock_BrushColorChanging");

			var textColor = _app.Marked("textColor");

			var rectBefore = _app.GetRect("textResult");
			var beforeColorChanged = TakeScreenshot("beforeColor");

			textColor.SetDependencyPropertyValue("Text", "Blue");

			var afterColorChanged = TakeScreenshot("afterColor");
			ImageAssert.AreNotEqual(afterColorChanged, beforeColorChanged);

			var scale = (float)GetDisplayScreenScaling();
			ImageAssert.HasColorAt(afterColorChanged, (rectBefore.X + 10) * scale, (rectBefore.Y + 10) * scale, Color.Blue);
		}

		[Test]
		[AutoRetry]
		public void When_MaxLines_Changed_With_TextWrapping()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl.SimpleText_MaxLines_Different_Font_Size");
			var maxLineContainer = _app.Marked("container3");

			var rectBefore = _app.GetRect("container3");
			var heightBefore = rectBefore.Height;

			maxLineContainer.SetDependencyPropertyValue("MaxLines", "2");
			var rectAfter = _app.GetRect("container3");
			var heightAfter = rectAfter.Height;

			Assert.IsTrue(heightAfter > heightBefore);
		}

		[Test]
		[AutoRetry]
		public void When_MaxLines_Changed_Without_TextWrapping()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl.SimpleText_MaxLines_Different_Font_Size");
			var maxLineContainer = _app.Marked("container3");
			maxLineContainer.SetDependencyPropertyValue("TextWrapping", "NoWrap");

			var rectBefore = _app.GetRect("container3");
			var heightBefore = rectBefore.Height;

			maxLineContainer.SetDependencyPropertyValue("MaxLines", "2");
			var rectAfter = _app.GetRect("container3");
			var heightAfter = rectAfter.Height;

			Assert.IsTrue(heightAfter == heightBefore);
		}

		[Test]
		[AutoRetry()]
		public void When_Text_is_Constrained_Then_Clipping_is_Applied()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBlockControl.TextBlock_ConstrainedByContainer");


			// 1) Take a screenshot of the whole sample
			// 2) Switch opacity of text to zero
			// 3) Take new screenshot
			// 4) Compare Right zone -- must be identical
			// 5) Compare Bottom zone -- must be identical
			// 6) Do the same for subsequent text block in sample (1 to 5)
			//
			// +-sampleRootPanel------------+--------------+
			// |                            |              |
			// |    +-borderX---------------+              |
			// |    | [textX]Lorem ipsum... |              |
			// |    +-----------------------+  Right zone  |
			// |    |                       |              |
			// |    |    Bottom zone        |              |
			// |    |                       |              |
			// +----+-----------------------+--------------+

			// (1)
			var sampleScreenshot = this.TakeScreenshot("fullSample", ignoreInSnapshotCompare: true);
			var sampleRect = _app.GetRect("sampleRootPanel");

			using var _ = new AssertionScope();

			Test("text1", "border1");
			Test("text2", "border2");
			Test("text3", "border3");
			Test("text4", "border4");
			Test("text5", "border5");

			void Test(string textControl, string borderControl)
			{
				var textRect = _app.GetRect(borderControl);

				// (2)
				_app.Marked(textControl).SetDependencyPropertyValue("Opacity", "0");

				// (3)
				var afterScreenshot = this.TakeScreenshot("sample-" + textControl, ignoreInSnapshotCompare: true);

				// (4)
				var rect1 = new AppRect(
					x: textRect.Right,
					y:sampleRect.X,
					width: sampleRect.Right - textRect.Right,
					height:sampleRect.Height);

				ImageAssert.AreEqual(sampleScreenshot, rect1, afterScreenshot, rect1);

				// (5)
				var rect2 = new AppRect(
					x: textRect.X,
					y: textRect.Bottom,
					width: textRect.Width,
					height: sampleRect.Height - textRect.Bottom);

				ImageAssert.AreEqual(sampleScreenshot, rect2, afterScreenshot, rect2);
			}

		}
	}
}
