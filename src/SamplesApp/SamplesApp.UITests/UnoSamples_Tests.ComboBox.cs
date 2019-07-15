using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest.Helpers;

namespace SamplesApp.UITests
{
	[TestFixture]
	partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		public void ComboBox_Kidnapping()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ComboBox.ComboBox_ComboBoxItem_Selection");

			var scrollingInitial = Configuration.AttemptToFindTargetBeforeScrolling;
			Configuration.AttemptToFindTargetBeforeScrolling = true; //Needed on WASM because ScrollUpTo() currently not implemented
			var comboBox = _app.Marked("_combo2");
			_app.WaitForElement(comboBox);
			var presenter = _app.FindWithin(marked: "ContentPresenter", within: comboBox);

			var button = _app.Marked("ChangeSelectionButton");
			_app.Tap(button);

			var first = _app.FindWithin("_tb1", presenter);
			Assert.AreEqual("Item 1", GetText(first));

			_app.Tap(comboBox);
			_app.TapCoordinates(300, 100);
			first = _app.FindWithin("_tb1", presenter);
			Assert.AreEqual("Item 1", GetText(first));

			_app.Tap(button);
			var second = _app.FindWithin("_tb2", presenter);
			Assert.AreEqual("Item 2", GetText(second));

			Configuration.AttemptToFindTargetBeforeScrolling = scrollingInitial;
		}
	}
}
