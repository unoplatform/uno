using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.PropertyDefaultsTests
{
	[UITestClass]
	public class Given_ControlPropertyDefaults
	{
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26427")]
		public void When_DatePicker_Created_Orientation_Is_Horizontal()
		{
			Assert.AreEqual(Orientation.Horizontal, new DatePicker().Orientation);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26427")]
		public void When_AutoSuggestBox_Created_AutoMaximizeSuggestionArea_Is_True()
		{
			Assert.IsTrue(new AutoSuggestBox().AutoMaximizeSuggestionArea);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26427")]
		public void When_PasswordBox_Created_IsPasswordRevealButtonEnabled_Is_False()
		{
			Assert.IsFalse(new PasswordBox().IsPasswordRevealButtonEnabled);
		}
	}
}
