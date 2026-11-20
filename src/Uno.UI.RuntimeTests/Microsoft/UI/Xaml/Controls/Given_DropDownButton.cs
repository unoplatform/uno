using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_DropDownButton
{
	[TestMethod]
	public async Task When_MenuFlyout_Replaced_Then_Old_Flyout_No_Longer_Expands()
	{
		var oldFlyout = new MenuFlyout();
		oldFlyout.Items.Add(new MenuFlyoutItem { Text = "Old" });
		var SUT = new DropDownButton { Content = "Test", Flyout = oldFlyout };
		await UITestHelper.Load(SUT);

		SUT.Flyout = new Flyout { Content = new TextBlock { Text = "New" } };

		await AssertOldFlyoutDoesNotExpand(SUT, oldFlyout);
	}

	[TestMethod]
	public async Task When_Template_Reapplied_And_Flyout_Replaced_Then_Old_Flyout_No_Longer_Expands()
	{
		var oldFlyout = new Flyout { Content = new TextBlock { Text = "Old" } };
		var SUT = new CountingDropDownButton { Content = "Test", Flyout = oldFlyout };
		await UITestHelper.Load(SUT);

		var template = SUT.Template;
		SUT.Template = null;
		SUT.Template = template;
		SUT.ApplyTemplate();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(2, SUT.ApplyTemplateCount);

		SUT.Flyout = new Flyout { Content = new TextBlock { Text = "New" } };

		await AssertOldFlyoutDoesNotExpand(SUT, oldFlyout);
	}

	[TestMethod]
	public async Task When_Disabled_Before_Load_Then_Disabled_Visual_State()
	{
		var SUT = new DropDownButton { Content = "Test", IsEnabled = false };
		await UITestHelper.Load(SUT);

		var root = (FrameworkElement)VisualTreeHelper.GetChild(SUT, 0);
		var commonStates = VisualStateManager.GetVisualStateGroups(root).Single(g => g.Name == "CommonStates");
		Assert.AreEqual("Disabled", commonStates.CurrentState?.Name);
	}

	private static async Task AssertOldFlyoutDoesNotExpand(DropDownButton dropDownButton, FlyoutBase oldFlyout)
	{
		var openedCount = 0;
		oldFlyout.Opened += (_, _) => openedCount++;
		var peer = (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(dropDownButton);

		try
		{
			oldFlyout.ShowAt(dropDownButton);
			await TestServices.WindowHelper.WaitFor(() => openedCount == 1);

			Assert.AreEqual(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);
		}
		finally
		{
			oldFlyout.Hide();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	private sealed partial class CountingDropDownButton : DropDownButton
	{
		public int ApplyTemplateCount { get; private set; }

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			ApplyTemplateCount++;
		}
	}
}
