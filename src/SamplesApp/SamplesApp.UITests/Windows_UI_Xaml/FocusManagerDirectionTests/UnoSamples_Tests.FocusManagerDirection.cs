using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml.FocusManagerDirectionTests
{
	public partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void FocusManager_FocusDirection_Next_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var firstTextBox = _app.Marked("FirstTextBox");
			var secondTextBox = _app.Marked("SecondTextBox");
			var thirdTextBox = _app.Marked("ThirdTextBox");
			var nextButton = _app.Marked("NextButton");

			_app.Tap(firstTextBox);

			// Click on next button to move to next text box
			_app.Tap(nextButton);

			// Assert initial state for textbox 2
			Assert.AreEqual("2", secondTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Second text box got focus");

			// Assert final state for textbox 2
			Assert.AreEqual("Second text box got focus", secondTextBox.GetDependencyPropertyValue("Text")?.ToString());

			// Click on next button to move to next text box
			_app.Tap(nextButton);

			// Assert initial state for textbox 3
			Assert.AreEqual("3", thirdTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Third text box got focus");

			// Assert final state for textbox 3
			Assert.AreEqual("Third text box got focus", thirdTextBox.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void FocusManager_FocusDirection_Previous_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var fourthTextBox = _app.Marked("FourthTextBox");
			var fifthTextBox = _app.Marked("FifthTextBox");
			var sixthTextBox = _app.Marked("SixthTextBox");
			var previousButton = _app.Marked("PreviousButton");

			_app.Tap(sixthTextBox);

			// Click on previous button to move to previous text box
			_app.Tap(previousButton);

			// Assert initial state for textbox 5
			Assert.AreEqual("5", fifthTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Fifth text box got focus");

			// Assert final state for textbox 5
			Assert.AreEqual("Fifth text box got focus", fifthTextBox.GetDependencyPropertyValue("Text")?.ToString());

			// Click on previous button to move to previous text box
			_app.Tap(previousButton);

			// Assert initial state for textbox 4
			Assert.AreEqual("4", fourthTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Fourth text box got focus");

			// Assert final state for textbox 4
			Assert.AreEqual("Fourth text box got focus", fourthTextBox.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void FocusManager_FocusDirection_Up_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var secondTextBox = _app.Marked("SecondTextBox");
			var fifthTextBox = _app.Marked("FifthTextBox");
			var eighthTextBox = _app.Marked("EighthTextBox");
			var upButton = _app.Marked("UpButton");

			_app.Tap(eighthTextBox);

			// Click on up button to move to 5th text box
			_app.Tap(upButton);

			// Assert initial state for textbox 5
			Assert.AreEqual("5", fifthTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Fifth text box got focus");

			// Assert final state for textbox 5
			Assert.AreEqual("Fifth text box got focus", fifthTextBox.GetDependencyPropertyValue("Text")?.ToString());

			// Click on up button to move to text box 2
			_app.Tap(upButton);

			// Assert initial state for textbox 2
			Assert.AreEqual("2", secondTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Second text box got focus");

			// Assert final state for textbox 2
			Assert.AreEqual("Second text box got focus", secondTextBox.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void FocusManager_FocusDirection_Down_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var secondTextBox = _app.Marked("SecondTextBox");
			var fifthTextBox = _app.Marked("FifthTextBox");
			var eighthTextBox = _app.Marked("EighthTextBox");
			var downButton = _app.Marked("DownButton");

			_app.Tap(secondTextBox);

			// Click on down button to move to text box 5
			_app.Tap(downButton);

			// Assert initial state for textbox 5
			Assert.AreEqual("5", fifthTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Fifth text box got focus");

			// Assert final state for textbox 5
			Assert.AreEqual("Fifth text box got focus", fifthTextBox.GetDependencyPropertyValue("Text")?.ToString());

			// Click on down button to move to text box 8
			_app.Tap(downButton);

			// Assert initial state for textbox 8
			Assert.AreEqual("8", eighthTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Eighth text box got focus");

			// Assert final state for textbox 8
			Assert.AreEqual("Eighth text box got focus", eighthTextBox.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void FocusManager_FocusDirection_Left_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var fourthTextBox = _app.Marked("FourthTextBox");
			var fifthTextBox = _app.Marked("FifthTextBox");
			var sixthTextBox = _app.Marked("SixthTextBox");
			var leftButton = _app.Marked("LeftButton");

			_app.Tap(sixthTextBox);

			// Click on left button to move to left text box
			_app.Tap(leftButton);

			// Assert initial state for textbox 5
			Assert.AreEqual("5", fifthTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Fifth text box got focus");

			// Assert final state for textbox 5
			Assert.AreEqual("Fifth text box got focus", fifthTextBox.GetDependencyPropertyValue("Text")?.ToString());

			// Click on left button to move to left text box
			_app.Tap(leftButton);

			// Assert initial state for textbox 4
			Assert.AreEqual("4", fourthTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Fourth text box got focus");

			// Assert final state for textbox 4
			Assert.AreEqual("Fourth text box got focus", fourthTextBox.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void FocusManager_FocusDirection_Right_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var firstTextBox = _app.Marked("FirstTextBox");
			var secondTextBox = _app.Marked("SecondTextBox");
			var thirdTextBox = _app.Marked("ThirdTextBox");
			var rightButton = _app.Marked("RightButton");

			_app.Tap(firstTextBox);

			// Click on right button to move to right text box
			_app.Tap(rightButton);

			// Assert initial state for textbox 2
			Assert.AreEqual("2", secondTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Second text box got focus");

			// Assert final state for textbox 2
			Assert.AreEqual("Second text box got focus", secondTextBox.GetDependencyPropertyValue("Text")?.ToString());

			// Click on right button to move to right text box
			_app.Tap(rightButton);

			// Assert initial state for textbox 3
			Assert.AreEqual("3", thirdTextBox.GetDependencyPropertyValue("Text")?.ToString());

			_app.ClearText();
			_app.EnterText("Third text box got focus");

			// Assert final state for textbox 3
			Assert.AreEqual("Third text box got focus", thirdTextBox.GetDependencyPropertyValue("Text")?.ToString());
		}
	}
}
