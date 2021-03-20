using System;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.SplitButtonTests
{
	public partial class Given_SplitButton : SampleControlUITestBase
	{
		// TODO: Additional tests can be ported when #3165 is fixed

		[Test]
		[AutoRetry]
		public void CommandTest()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.SplitButtonTests.SplitButtonPage");

			var splitButton = _app.Marked("CommandSplitButton");

			var canExecuteCheckBox = _app.Marked("CanExecuteCheckBox");
			var executeCountTextBlock = _app.Marked("ExecuteCountTextBlock");

			Console.WriteLine("Assert that the control starts out enabled");			
			Assert.IsTrue("true".Equals(canExecuteCheckBox.GetDependencyPropertyValue("IsChecked").ToString(), StringComparison.InvariantCultureIgnoreCase));
			Assert.IsTrue("true".Equals(splitButton.GetDependencyPropertyValue("IsEnabled").ToString(), StringComparison.InvariantCultureIgnoreCase));
			Assert.AreEqual("0", executeCountTextBlock.GetText());

			Console.WriteLine("Click primary button to execute command");
			TapPrimaryButton(splitButton);
			Assert.AreEqual("1", executeCountTextBlock.GetText());

			Console.WriteLine("Assert that setting CanExecute to false disables the primary button");
			canExecuteCheckBox.Tap();

			//Wait.ForIdle();

			TapPrimaryButton(splitButton);
			Assert.AreEqual("1", executeCountTextBlock.GetText());
		}

		public void TapPrimaryButton(QueryEx splitButton)
		{
			// This method taps the descendants and differs from MUX!
			Console.WriteLine("Tap primary button area");

			splitButton.Descendant().Marked("PrimaryButton").Tap();
			//Wait.ForIdle();
		}
	}
}
