using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_RequestedTheme
	{
		[TestInitialize]
		public void Initialize()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_RequestedTheme_Set_On_Parent_Child_Unaffected()
		{
			var child = new TextBlock();
			var control = new Grid()
			{
				Children =
				{
					child
				}
			};

			control.RequestedTheme = ElementTheme.Dark;

			Assert.AreEqual(ElementTheme.Default, child.RequestedTheme);
		}
	}
}
