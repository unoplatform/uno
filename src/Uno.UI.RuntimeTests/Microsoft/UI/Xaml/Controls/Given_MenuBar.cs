using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using MenuBar = Microsoft.UI.Xaml.Controls.MenuBar;
using MenuBarItem = Microsoft.UI.Xaml.Controls.MenuBarItem;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_MenuBar
{
	[TestCleanup]
	public void Cleanup() => UITestHelper.CloseAllPopups();

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
		await WindowHelper.WaitFor(() => IsFlyoutOpen(items[0]));
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.Right();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(items[2], FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		Assert.IsFalse(IsFlyoutOpen(items[0]));
		Assert.IsFalse(IsFlyoutOpen(items[1]));
	}

	[TestMethod]
	// KeyboardHelper is a no-op on WinAppSDK
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[Ignore("https://github.com/unoplatform/uno/issues/26654: the deferred Closed callback in FlyoutBase.RemoveFromOpenFlyouts hides the newly opened flyout.")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26654")]
	public async Task When_Flyout_Open_And_Right_Arrow_Then_Next_Flyout_Opens()
	{
		var (_, items) = await Load(("File", true, 1), ("Edit", true, 1));

		Invoke(items[0]);
		await WindowHelper.WaitFor(() => IsFlyoutOpen(items[0]));
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.Right();
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(IsFlyoutOpen(items[0]));
		Assert.IsTrue(IsFlyoutOpen(items[1]), "The next item's flyout should open and stay open.");
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

	[TestMethod]
	// Injects the mouse, which would take over the real pointer on WinUI.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Flyout_Open_And_Press_On_Other_Item_Then_Press_Reaches_It()
	{
		var (_, items) = await Load(("File", true, 1), ("Edit", true, 1));
		var editButton = FindChild<Button>(items[1], "ContentButton");
		var editPresses = 0;
		editButton.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, _) => editPresses++), handledEventsToo: true);

		var injector = Windows.UI.Input.Preview.Injection.InputInjector.TryCreate() ?? throw new System.InvalidOperationException("Failed to init the InputInjector");
		using var mouse = Uno.UI.DevTools.Input.InputInjectorExtensions.GetMouse(injector);

		Invoke(items[0]);
		await WindowHelper.WaitFor(() => IsFlyoutOpen(items[0]));
		await WindowHelper.WaitForIdle();

		// The flyout lets presses through to the menu bar (its OverlayInputPassThroughElement), as on WinUI.
		var editBounds = editButton.GetAbsoluteBounds();
		mouse.Press(new Windows.Foundation.Point(editBounds.X + editBounds.Width / 2, editBounds.Y + editBounds.Height / 2));
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(IsFlyoutOpen(items[0]), "The press outside the flyout should close it.");
		Assert.AreEqual(1, editPresses, "The press on the other menu bar item should reach it.");
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

	// MenuBarItem.IsFlyoutOpen() is public on Uno only; WinUI keeps it internal.
	private static bool IsFlyoutOpen(MenuBarItem item)
#if HAS_UNO
		=> item.IsFlyoutOpen();
#else
		=> FindChild<Button>(item, "ContentButton").ContextFlyout.IsOpen;
#endif

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
