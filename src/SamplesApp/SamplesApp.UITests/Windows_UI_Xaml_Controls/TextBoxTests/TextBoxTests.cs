using System;
using System.Drawing;
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
			TakeScreenshot("tb1 focused", ignoreInSnapshotCompare: true);

			tb2.Tap();
			TakeScreenshot("tb2 focused", ignoreInSnapshotCompare: true);
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

		[Test]
		[AutoRetry]
		public void TextBox_DeleteButton()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests.TextBox_DeleteButton_Automated");

			var textBox1 = _app.Marked("textBox1");
			var textBox2 = _app.Marked("textBox2");

			textBox1.Tap();
			textBox1.EnterText("hello 01");

			_app.WaitForText(textBox1, "hello 01");

			textBox2.Tap();
			textBox2.EnterText("hello 02");

			_app.WaitForText(textBox2, "hello 02");

			var textBox1Result = _app.Query(textBox1).First();
			var textBox2Result = _app.Query(textBox2).First();

			// Focus the first textbox
			textBox1.Tap();

			var deleteButton1 = FindDeleteButton(textBox1Result);

			_app.TapCoordinates(deleteButton1.Rect.CenterX, deleteButton1.Rect.CenterY);

			// Second tap is required on Wasm https://github.com/unoplatform/uno/issues/2138
			_app.TapCoordinates(deleteButton1.Rect.CenterX, deleteButton1.Rect.CenterY);

			_app.WaitForText(textBox1, "");

			// Focus the first textbox
			textBox2.Tap();

			var deleteButton2 = FindDeleteButton(textBox2Result);

			_app.TapCoordinates(deleteButton2.Rect.CenterX, deleteButton2.Rect.CenterY);

			// Second tap is required on Wasm https://github.com/unoplatform/uno/issues/2138
			_app.TapCoordinates(deleteButton2.Rect.CenterX, deleteButton2.Rect.CenterY);

			_app.WaitForText(textBox2, "");
		}

		private Uno.UITest.IAppResult FindDeleteButton(Uno.UITest.IAppResult source)
		{
			var deleteButtons = _app.Marked("DeleteButton");
			var appResult = _app.Query(deleteButtons).ToArray();
			var deleteButton = appResult
				.First(r =>
					r.Rect.CenterX > source.Rect.X
					&& r.Rect.CenterX < source.Rect.Right
					&& r.Rect.CenterY > source.Rect.Y
					&& r.Rect.CenterY < source.Rect.Bottom
				);
			return deleteButton;
		}

		[Test]
		[AutoRetry]
		public void TextBox_Readonly()
		{
			Run("Uno.UI.Samples.UITests.TextBoxControl.TextBox_IsReadOnly");

			var button = _app.Marked("button");
			var txt = _app.Marked("txt");

			_app.EnterText(txt, "Hello !");

			_app.WaitForText(txt, "This is the starting text...Hello !");

			button.Tap();
			_app.EnterText(txt, "Hello did not work!");

			_app.WaitForText(txt, "This is the starting text...Hello !");

			button.Tap();
			_app.EnterText(txt, "Works again!");
			_app.WaitForText(txt, "This is the starting text...Hello !Works again!");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser, Platform.iOS)] // Disabled on Android due to pixel color approximation, will be restored in next PR
		public void PasswordBox_RevealInScrollViewer()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.PasswordBox_Reveal_Scroll");

			var passwordBox = _app.WaitForElement("MyPasswordBox").Single();
			_app.Wait(TimeSpan.FromMilliseconds(500)); // Make sure to show the status bar
			var initial = TakeScreenshot("initial", ignoreInSnapshotCompare: true);

			// Focus the PasswordBox
			_app.TapCoordinates(passwordBox.Rect.X + 10, passwordBox.Rect.Y);

			// Press the reveal button, and move up (so the ScrollViewer will kick in and cancel the pointer), then release
			_app.DragCoordinates(passwordBox.Rect.X + 10, passwordBox.Rect.Right - 10, passwordBox.Rect.X - 100, passwordBox.Rect.Right - 10);

			var result = TakeScreenshot("result", ignoreInSnapshotCompare: true);

			ImageAssert.AreEqual(initial, result, new Rectangle(
				(int)passwordBox.Rect.X + 8, // +8 : ignore borders (as we are still focused)
				(int)passwordBox.Rect.Y + 8,
				100, // Ignore the reveal button on right (as we are still focused)
				(int)passwordBox.Rect.Height - 16));
		}
	}
}
