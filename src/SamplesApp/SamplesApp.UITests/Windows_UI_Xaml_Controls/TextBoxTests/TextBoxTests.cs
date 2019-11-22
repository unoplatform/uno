using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.TextBoxTests
{
	[TestFixture]
	public class TextBoxTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void TextBox_NaturalSize_When_Empty_Is_Right_Width()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxes.TextBox_NaturalSize");

			var sut = _app.Marked("textbox_sut").FirstResult().Rect;
			var recth = _app.Marked("recth").FirstResult().Rect;

			sut.Width.Should().Be(recth.Width, "Invalid Width");
		}

		[Test]
		[AutoRetry]
		public void TextBox_NaturalSize_When_Empty_Is_Right_XPos()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxes.TextBox_NaturalSize");

			var sut = _app.Marked("textbox_sut").FirstResult().Rect;
			var recth = _app.Marked("recth").FirstResult().Rect;

			sut.X.Should().Be(recth.X, "Invalid X position");
		}

		[Test]
		[AutoRetry]
		public void TextBox_Foreground()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.TextBox_Foreground");

			var tb1 = _app.Marked("tb1");
			var tb2 = _app.Marked("tb2");

			tb1.Tap();
			TakeScreenshot("tb1 focused");

			tb2.Tap();
			TakeScreenshot("tb2 focused");
		}

		[Test]
		[AutoRetry]
		public void TextBox_RoundedCorners()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_RoundedCorners");

			var textBox1 = _app.Marked("textBox1");
			var textBox2 = _app.Marked("textBox2");

			var textBox1Result_Before = _app.Query(textBox1).First();
			var textBox2Result_Before = _app.Query(textBox2).First();

			textBox1.Tap();
			textBox1.EnterText("hello 01");

			_app.WaitForText(textBox1, "hello 01");

			textBox2.Tap();
			textBox2.EnterText("hello 02");

			_app.WaitForText(textBox2, "hello 02");

			var textBox1Result_After = _app.Query(textBox1).First();
			var textBox2Result_After = _app.Query(textBox2).First();

			textBox1Result_After.Rect.Width.Should().Be(textBox1Result_Before.Rect.Width);
			textBox1Result_After.Rect.Height.Should().Be(textBox1Result_Before.Rect.Height);
			textBox1Result_After.Rect.X.Should().Be(textBox1Result_Before.Rect.X);
			textBox1Result_After.Rect.Y.Should().Be(textBox1Result_Before.Rect.Y);
		}
	}
}
