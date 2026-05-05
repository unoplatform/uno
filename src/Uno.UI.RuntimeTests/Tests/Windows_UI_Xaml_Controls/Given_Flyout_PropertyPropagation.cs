using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Flyout_PropertyPropagation
{
	[TestMethod]
	public async Task When_Focus_Properties_Set_On_Flyout_Propagate_To_Content()
	{
		var SUT = new Button();
		var flyoutContent = new Grid
		{
			Children = { SUT }
		};

		var flyout = new Flyout
		{
			AllowFocusOnInteraction = false,
			AllowFocusWhenDisabled = true,
			Content = flyoutContent,
		};

		var flyoutOwner = new Button { Flyout = flyout };

		TestServices.WindowHelper.WindowContent = flyoutOwner;
		await TestServices.WindowHelper.WaitForLoaded(flyoutOwner);

		try
		{
			flyout.ShowAt(flyoutOwner);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.AllowFocusOnInteraction);
			Assert.IsTrue(SUT.AllowFocusWhenDisabled);

			flyout.AllowFocusOnInteraction = true;
			flyout.AllowFocusWhenDisabled = false;

			Assert.IsTrue(SUT.AllowFocusOnInteraction);
			Assert.IsFalse(SUT.AllowFocusWhenDisabled);
		}
		finally
		{
			flyout.Hide();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Focus_Properties_Set_On_Flyout_Propagate_To_Popup()
	{
		var flyoutContent = new Grid
		{
			Children = { new Button() }
		};

		var flyout = new Flyout
		{
			AllowFocusOnInteraction = false,
			AllowFocusWhenDisabled = true,
			Content = flyoutContent,
		};

		var flyoutOwner = new Button { Flyout = flyout };

		TestServices.WindowHelper.WindowContent = flyoutOwner;
		await TestServices.WindowHelper.WaitForLoaded(flyoutOwner);

		try
		{
			flyout.ShowAt(flyoutOwner);
			await TestServices.WindowHelper.WaitForIdle();

			var popup = flyout.GetPopupPanel().Popup;

			Assert.IsFalse(popup.AllowFocusOnInteraction);
			Assert.IsTrue(popup.AllowFocusWhenDisabled);

			flyout.AllowFocusOnInteraction = true;
			flyout.AllowFocusWhenDisabled = false;

			Assert.IsTrue(popup.AllowFocusOnInteraction);
			Assert.IsFalse(popup.AllowFocusWhenDisabled);
		}
		finally
		{
			flyout.Hide();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
}
