using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml.FocusManagerDirectionTests
{
	public partial class FocusManagerDirection_Tests : SampleControlUITestBase
	{
		private void ChangeFocusAndAssertBeforeAfter(IApp app, Action<IApp> changeFocus, QueryEx target, string initialText, string finalText)
		{
			// Focus target
			changeFocus(app);

			// Assert initial state
			Assert.AreEqual(initialText, target.GetDependencyPropertyValue("Text")?.ToString());

			// Update text content
			_app.ClearText();
			_app.EnterText(finalText);

			// Assert final state
			Assert.AreEqual(finalText, target.GetDependencyPropertyValue("Text")?.ToString());
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)] // Wasm and Ios is disabled https://github.com/unoplatform/uno/issues/2476
		public void FocusManager_FocusDirection_Next_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var firstTextBox = _app.Marked("FirstTextBox");
			var secondTextBox = _app.Marked("SecondTextBox");
			var thirdTextBox = _app.Marked("ThirdTextBox");
			var nextButton = _app.Marked("NextButton");

			_app.Tap(firstTextBox);

			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(nextButton), secondTextBox, "2", "Second text box got focus");
			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(nextButton), thirdTextBox, "3", "Third text box got focus");
		}


		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)] // Wasm and Ios is disabled https://github.com/unoplatform/uno/issues/2476
		public void FocusManager_FocusDirection_Previous_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var fourthTextBox = _app.Marked("FourthTextBox");
			var fifthTextBox = _app.Marked("FifthTextBox");
			var sixthTextBox = _app.Marked("SixthTextBox");
			var previousButton = _app.Marked("PreviousButton");

			_app.Tap(sixthTextBox);

			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(previousButton), fifthTextBox, "5", "Fifth text box got focus");
			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(previousButton), fourthTextBox, "4", "Fourth text box got focus");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)] // Wasm is disabled https://github.com/unoplatform/uno/issues/2476
		public void FocusManager_FocusDirection_Up_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var secondTextBox = _app.Marked("SecondTextBox");
			var fifthTextBox = _app.Marked("FifthTextBox");
			var eighthTextBox = _app.Marked("EighthTextBox");
			var upButton = _app.Marked("UpButton");

			_app.Tap(eighthTextBox);

			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(upButton), fifthTextBox, "5", "Fifth text box got focus");
			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(upButton), secondTextBox, "2", "Second text box got focus");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)] // Wasm is disabled https://github.com/unoplatform/uno/issues/2476
		public void FocusManager_FocusDirection_Down_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var secondTextBox = _app.Marked("SecondTextBox");
			var fifthTextBox = _app.Marked("FifthTextBox");
			var eighthTextBox = _app.Marked("EighthTextBox");
			var downButton = _app.Marked("DownButton");

			_app.Tap(secondTextBox);

			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(downButton), fifthTextBox, "5", "Fifth text box got focus");
			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(downButton), eighthTextBox, "8", "Eighth text box got focus");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)] // Wasm and Ios is disabled https://github.com/unoplatform/uno/issues/2476
		public void FocusManager_FocusDirection_Left_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var fourthTextBox = _app.Marked("FourthTextBox");
			var fifthTextBox = _app.Marked("FifthTextBox");
			var sixthTextBox = _app.Marked("SixthTextBox");
			var leftButton = _app.Marked("LeftButton");

			_app.Tap(sixthTextBox);

			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(leftButton), fifthTextBox, "5", "Fifth text box got focus");
			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(leftButton), fourthTextBox, "4", "Fourth text box got focus");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)] // Wasm and Ios is disabled https://github.com/unoplatform/uno/issues/2476
		public void FocusManager_FocusDirection_Right_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.FocusManager.FocusManager_FocusDirection");

			var firstTextBox = _app.Marked("FirstTextBox");
			var secondTextBox = _app.Marked("SecondTextBox");
			var thirdTextBox = _app.Marked("ThirdTextBox");
			var rightButton = _app.Marked("RightButton");

			_app.Tap(firstTextBox);

			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(rightButton), secondTextBox, "2", "Second text box got focus");
			ChangeFocusAndAssertBeforeAfter(_app, app => app.Tap(rightButton), thirdTextBox, "3", "Third text box got focus");
		}
	}
}
