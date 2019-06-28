using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{
	[TestFixture]
	partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		public void Keyboard_Textbox_InsideScrollViewer_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.Input_Test_InsideScrollerViewer_Automated");

			_app.WaitForElement(_app.Marked("MultilineTextBox"));

			var normalTextBox = _app.Marked("NormalTextBox");
			var filledTextBox = _app.Marked("FilledTextBox");
			var placeholderTextTextBox = _app.Marked("PlaceholderTextTextBox");
			var disabledTextBox = _app.Marked("DisabledTextBox");
			var multilineTextBox = _app.Marked("MultilineTextBox");
			var numberTextBox = _app.Marked("NumberTextBox");

			// Assert inital state 
			Assert.AreEqual(string.Empty, normalTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("Text", filledTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, placeholderTextTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, disabledTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, multilineTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, numberTextBox.GetDependencyPropertyValue("Text")?.ToString());

			{
				// Setting focus on normalTextBox
				_app.Tap(normalTextBox);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 0 - Focus on normalTextBox ");

				// Removing focus on normalTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 0 - Remove Focus on normalTextBox");
			}

			{
				// Setting focus on normalTextBox
				_app.Tap(filledTextBox);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 1 - Focus on filledTextBox");

				// Removing focus on normalTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 1 - Remove Focus on filledTextBox");
			}

			{
				// Setting focus on placeholderTextTextBox
				_app.Tap(placeholderTextTextBox);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 2 - Focus on placeholderTextTextBox");

				// Removing focus on placeholderTextTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 2 - Remove Focus on placeholderTextTextBox");
			}

			{
				// Setting focus on disabledTextBox
				_app.Tap(disabledTextBox);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 3 - Focus on disabledTextBox");

				// Removing focus on disabledTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 3 - Remove Focus on disabledTextBox");
			}

			{
				// Setting focus on multilineTextBox
				_app.Tap(multilineTextBox);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 4 - Focus on multilineTextBox");

				// Removing focus on multilineTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 4 - Remove Focus on multilineTextBox");
			}

			{
				// Setting focus on numberTextBox
				_app.Tap(numberTextBox);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 5 - Focus on numberTextBox");

				// Removing focus on numberTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("Inside ScrollViewer - 5 - Remove Focus on numberTextBox");
			}
		}

		[Test]
		public void Keyboard_Textbox_NoScrollViewer_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.Input_Test_NoScrollViewer_Automated");

			_app.WaitForElement(_app.Marked("MultilineTextBox"));

			var normalTextBox = _app.Marked("NormalTextBox");
			var filledTextBox = _app.Marked("FilledTextBox");
			var placeholderTextTextBox = _app.Marked("PlaceholderTextTextBox");
			var disabledTextBox = _app.Marked("DisabledTextBox");
			var multilineTextBox = _app.Marked("MultilineTextBox");
			var numberTextBox = _app.Marked("NumberTextBox");

			// Assert inital state 
			Assert.AreEqual(string.Empty, normalTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("Text", filledTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, placeholderTextTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, disabledTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, multilineTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, numberTextBox.GetDependencyPropertyValue("Text")?.ToString());

			{
				// Setting focus on normalTextBox
				_app.Tap(normalTextBox);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 0 - Focus on normalTextBox");

				// Removing focus on normalTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 0 - Remove Focus on normalTextBox");
			}

			{
				// Setting focus on normalTextBox
				_app.Tap(filledTextBox);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 1 - Focus on filledTextBox");

				// Removing focus on normalTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 1 - Remove Focus on filledTextBox");
			}

			{
				// Setting focus on placeholderTextTextBox
				_app.Tap(placeholderTextTextBox);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 2 - Focus on placeholderTextTextBox");

				// Removing focus on placeholderTextTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 2 - Remove Focus on placeholderTextTextBox");
			}

			{
				// Setting focus on disabledTextBox
				_app.Tap(disabledTextBox);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 3 - Focus on disabledTextBox");

				// Removing focus on disabledTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 3 - Remove Focus on disabledTextBox");
			}

			{
				// Setting focus on multilineTextBox
				_app.Tap(multilineTextBox);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 4 - Focus on multilineTextBox");

				// Removing focus on multilineTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 4 - Remove Focus on multilineTextBox");
			}

			{
				// Setting focus on numberTextBox
				_app.Tap(numberTextBox);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 5 - Focus on numberTextBox");

				// Removing focus on numberTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
				_app.Screenshot("No ScrollViewer - 5 - Remove Focus on numberTextBox");
			}
		}

		[Test]
		public void Keyboard_Textbox_IsEnabled_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.TextBoxControl.Input_Test_InsideScrollerViewer_Automated");

			_app.WaitForElement(_app.Marked("DisabledTextBox"));

			var normalTextBox = _app.Marked("NormalTextBox");
			var disabledTextBox = _app.Marked("DisabledTextBox");

			// Assert inital state 
			Assert.AreEqual(string.Empty, normalTextBox.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual(string.Empty, disabledTextBox.GetDependencyPropertyValue("Text")?.ToString());

			{
				// Setting focus on normalTextBox
				_app.Tap(normalTextBox);
				_app.Wait(1);

				// Writting in normalTextBox 
				_app.EnterText("Test 1");
				Assert.AreEqual("Test 1", normalTextBox.GetDependencyPropertyValue("Text")?.ToString());

				// Removing focus on normalTextBox
				_app.TapCoordinates(0f, 0f);
				_app.Wait(1);
			}

			{
				// Setting focus on disabledTextBox
				_app.Tap(disabledTextBox);
				_app.Wait(1);

				// Writting in disabledTextBox 
				_app.EnterText("Test 2");
				Assert.AreEqual(string.Empty, disabledTextBox.GetDependencyPropertyValue("Text")?.ToString());

				// Removing focus on disabledTextBox
				_app.TapCoordinates(0f, 0f);
			}
		}

		[Test]
		[Ignore("Mobile only TODO")]
		public void Keyboard_DismissTesting()
		{
			Run(("Uno.UI.Samples.Content.UITests.ButtonTestsControl.AppBar_KeyBoard"));
			var appCommandBar = _app.Marked("AppCommandBar");
			var buttonDisabled = _app.Marked("ButtonDisabled");
			var buttonDone = _app.Marked("ButtonDone");
			var textBoxMessage = _app.Marked("TextBoxMessage");
			var singleTextBox = _app.Marked("SingleTextBox");

			_app.WaitForElement(singleTextBox);

			var f = _app.Screenshot("initial");

			singleTextBox.Tap();
			_app.Wait(2);

			// Click on AppBar Button
			var appCommandBarPosition = appCommandBar.FirstResult().Rect;
			float xPosition = appCommandBarPosition.Width - 90;
			float yPosition = appCommandBarPosition.Height / 2;
			_app.TapCoordinates(appCommandBarPosition.X + xPosition, appCommandBarPosition.Y + yPosition);
			_app.Wait(3);
			_app.Back();

			var t = _app.Screenshot("final");
			_app.Wait(2);

			// TODO do not rely on GDI to do the image comparison
			//
			//Bitmap img1 = new Bitmap(f.ToString());
			//Bitmap img2 = new Bitmap(t.ToString());

			//float diffPercentage = 0;
			//float diff = 0;

			//if (img1.Size != img2.Size)
			//{
			//	throw new Exception("Images are of different sizes");
			//}
			//else
			//{
			//	for (int x = 0; x < img1.Width; x++)
			//	{
			//		for (int y = 0; y < img1.Height; y++)
			//		{
			//			Color img1P = img1.GetPixel(x, y);
			//			Color img2P = img2.GetPixel(x, y);

			//			diff += Math.Abs(img1P.R - img2P.R);
			//			diff += Math.Abs(img1P.G - img2P.G);
			//			diff += Math.Abs(img1P.B - img2P.B);
			//		}
			//	}

			//	diffPercentage = 100 * (diff / 255) / (img1.Width * img1.Height * 3);
			//	if (diffPercentage > 1)
			//	{
			//		Assert.Fail("Images are not same");
			//	}
			//}
		}
	}
}
