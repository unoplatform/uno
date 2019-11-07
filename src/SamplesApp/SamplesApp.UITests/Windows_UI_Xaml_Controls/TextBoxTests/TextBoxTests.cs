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
	}
}
