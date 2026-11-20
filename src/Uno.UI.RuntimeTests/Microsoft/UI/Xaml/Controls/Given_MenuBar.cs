using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;
using MenuBar = Microsoft.UI.Xaml.Controls.MenuBar;
using MenuBarItem = Microsoft.UI.Xaml.Controls.MenuBarItem;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_MenuBar
{
	[TestCleanup]
	public void Cleanup() => VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);

	[TestMethod]
	// KeyboardHelper is a no-op on WinAppSDK
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Right_Arrow_On_Item_Then_Focus_Moves_To_Next_Item()
	{
		var (_, items) = await Load(("File", true, 1), ("Edit", true, 1));

		items[0].Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.Right();

		Assert.AreEqual(items[1], FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		Assert.IsFalse(IsPopupOpen());
	}

	[TestMethod]
	// KeyboardHelper is a no-op on WinAppSDK
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Flyout_Open_And_Right_Arrow_Then_Focus_Skips_Disabled_Item()
	{
		var (_, items) = await Load(("File", true, 1), ("Edit", false, 1), ("Help", true, 1));

		Invoke(items[0]);
		await WindowHelper.WaitFor(() => items[0].IsFlyoutOpen());
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.Right();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(items[2], FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		Assert.IsFalse(items[0].IsFlyoutOpen());
		Assert.IsFalse(items[1].IsFlyoutOpen());
	}

	[TestMethod]
	public async Task When_Item_Has_No_Children_Then_Invoke_Opens_No_Flyout()
	{
		var (_, items) = await Load(("Empty", true, 0));

		Invoke(items[0]);
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(IsPopupOpen());
	}

	[TestMethod]
	public async Task When_Items_Change_Then_PositionInSet_And_SizeOfSet_Are_Updated()
	{
		var (menuBar, items) = await Load(("File", true, 1), ("Edit", true, 1));

		Assert.AreEqual(1, AutomationProperties.GetPositionInSet(items[0]));
		Assert.AreEqual(2, AutomationProperties.GetPositionInSet(items[1]));
		Assert.AreEqual(2, AutomationProperties.GetSizeOfSet(items[0]));

		menuBar.Items.Add(new MenuBarItem { Title = "Help" });

		Assert.AreEqual(3, AutomationProperties.GetPositionInSet(menuBar.Items[2]));
		Assert.AreEqual(3, AutomationProperties.GetSizeOfSet(items[0]));
	}

	[TestMethod]
	public async Task When_Flyout_Open_Then_MenuBar_Root_Is_Pass_Through_Element()
	{
		var (menuBar, items) = await Load(("File", true, 1));

		var flyout = FindChild<Button>(items[0], "ContentButton").ContextFlyout;

		Assert.AreEqual(FindChild<Grid>(menuBar, "LayoutRoot"), flyout.OverlayInputPassThroughElement);
	}

	private static async Task<(MenuBar, MenuBarItem[])> Load(params (string title, bool isEnabled, int childCount)[] definitions)
	{
		var menuBar = new MenuBar();
		foreach (var (title, isEnabled, childCount) in definitions)
		{
			var item = new MenuBarItem { Title = title, IsEnabled = isEnabled };
			for (var i = 0; i < childCount; i++)
			{
				item.Items.Add(new MenuFlyoutItem { Text = $"{title} {i}" });
			}
			menuBar.Items.Add(item);
		}

		WindowHelper.WindowContent = menuBar;
		await WindowHelper.WaitForLoaded(menuBar);
		await WindowHelper.WaitForIdle();

		return (menuBar, menuBar.Items.ToArray());
	}

	private static void Invoke(MenuBarItem item)
		=> ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(item).GetPattern(PatternInterface.Invoke)).Invoke();

	private static T FindChild<T>(DependencyObject parent, string name) where T : FrameworkElement
	{
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if ((child as T is { } match && match.Name == name ? match : FindChild<T>(child, name)) is { } found)
			{
				return found;
			}
		}

		return null;
	}

	private static bool IsPopupOpen() => VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count > 0;
}
