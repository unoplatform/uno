using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.RadioButtonTests
{
	[UITestClass]
	public class Given_RadioButton
	{
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26384")]
		public void When_Checked_Button_Joins_Group_Then_Other_Checked_Button_Is_Unchecked()
		{
			var a = new RadioButton { GroupName = "Given_RadioButton_Join_g" };
			var b = new RadioButton { GroupName = "Given_RadioButton_Join_h" };
			var c = new RadioButton { GroupName = "Given_RadioButton_Join_g" };

			a.IsChecked = true;
			b.IsChecked = true;

			b.GroupName = "Given_RadioButton_Join_g";

			Assert.IsFalse(a.IsChecked);
			Assert.IsTrue(b.IsChecked);
			Assert.IsFalse(c.IsChecked);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26384")]
		public void When_Unchecked_Button_Joins_Group_Then_Checked_Button_Stays_Checked()
		{
			var a = new RadioButton { GroupName = "Given_RadioButton_Unchecked_g" };
			var b = new RadioButton { GroupName = "Given_RadioButton_Unchecked_h" };

			a.IsChecked = true;

			b.GroupName = "Given_RadioButton_Unchecked_g";

			Assert.IsTrue(a.IsChecked);
			Assert.IsFalse(b.IsChecked);
		}
	}
}
