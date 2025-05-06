using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls;

[TestClass]
public class MenuFlyoutIntegrationTests
{
	private string m_menuCommandParam1;
	private string m_menuCommandParam2;

	public MenuFlyoutIntegrationTests()
	{
		m_menuCommandParam1 = "MenuItem1Command";
		m_menuCommandParam2 = "ToggleMenuItemCommand";
	}

	[TestMethod]
	public void CanInstantiate()
	{
		try
		{
			var flyout = new MenuFlyout();
		}
		catch (Exception ex)
		{
			Assert.Fail($"Failed to instantiate MenuFlyout: {ex}");
		}
	}

	[TestMethod]
	public async Task CanMenuFlyoutOpenClose()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;

		var menuFlyoutOpenedEvent = new Event();
		var menuFlyoutClosedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left'> " +
				"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Left' FontSize='25' Padding='25,10' Margin='50'> " +
				"    <Button.Flyout> " +
				"      <MenuFlyout Placement='Bottom'> " +
				"        <MenuFlyoutItem FontSize='30' Text='SUPERMAN' Foreground='RoyalBlue' Width='300' /> " +
				"        <MenuFlyoutSeparator Width='300' /> " +
				"        <ToggleMenuFlyoutItem FontSize='30' Text='THE FLASH' Foreground='RoyalBlue' Width='300' IsChecked='False' /> " +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			VERIFY_IS_NOT_NULL(rootPanel);
			TestServices.WindowHelper.WindowContent = rootPanel;

			button = (Button)(rootPanel.FindName("button"));
			VERIFY_IS_NOT_NULL(button);

			menuFlyout = (MenuFlyout)(button.Flyout);
			VERIFY_IS_NOT_NULL(menuFlyout);

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				LOG_OUTPUT("CanMenuFlyoutOpenClose: MenuFlyout Opened event is fired!");
				menuFlyoutOpenedEvent.Set();
			});

			closedRegistration.Attach(menuFlyout, (s, e) =>
			{
				LOG_OUTPUT("CanMenuFlyoutOpenClose: MenuFlyout Closed event is fired!");
				menuFlyoutClosedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Button Tap operation to show the MenuFlyout.");
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("RootPanel Tap operation to close the MenuFlyout.");
			menuFlyout.Hide();
		});

		await menuFlyoutClosedEvent.WaitForDefault();
	}

	[TestMethod]
	public async Task VerifyBackButtonClosesMenuFlyout()
	{
		Button button1 = null;
		IList<MenuFlyoutItemBase> items = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

		var menuFlyoutClosedEvent = new Event();
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		await RunOnUIThread(() =>
		{
			button1 = (Button)(rootPanel.FindName("button1"));
			items = menuFlyout.Items;
			closedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutClosedEvent.Set();
			});
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button1, -50, 50);

		var subItem = await GetSubItem(items);
		await TapSubMenuItem(subItem);

		LOG_OUTPUT("Close the MenuFlyout using the Back button.");
		var backButtonPressHandled = await TestServices.Utilities.InjectBackButtonPress();
		VERIFY_IS_TRUE(backButtonPressHandled);
		await menuFlyoutClosedEvent.WaitForDefault();

		LOG_OUTPUT("After closing a MenuFlyout, further back button presses should not get handled");
		backButtonPressHandled = await TestServices.Utilities.InjectBackButtonPress();
		VERIFY_IS_FALSE(backButtonPressHandled);
	}

	[TestMethod]
	public async Task VerifyMenuFlyoutPresenterStyle()
	{
		var menuFlyout = await CreateMenuFlyout(FlyoutPlacementMode.Bottom);
		VERIFY_IS_NOT_NULL(menuFlyout);

		var target = await FlyoutHelper.CreateTarget(
			100 /*width*/, 100 /*height*/,
			ThicknessHelper.FromUniformLength(10),
			HorizontalAlignment.Center,
			VerticalAlignment.Top);
		VERIFY_IS_NOT_NULL(target);

		await RunOnUIThread(() =>

		   {
			   var rootPanel = new Grid();
			   rootPanel.Children.Add(target);
			   VERIFY_IS_NOT_NULL(rootPanel);

			   TestServices.WindowHelper.WindowContent = rootPanel;
		   });

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("VerifyMenuFlyoutPresenterStyle: Execute the ShowAt.");
		FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>

		   {
			   var presenter = GetMenuFlyoutPresenter(menuFlyout);
			   VERIFY_IS_NOT_NULL(presenter);
			   var tag = presenter.GetValue(MenuFlyoutPresenter.TagProperty);
			   VERIFY_IS_NOT_NULL(tag);
			   VERIFY_ARE_EQUAL("presenter_style", tag.ToString());
		   });

		LOG_OUTPUT("VerifyMenuFlyoutPresenterStyle: Execute the Hide.");
		FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task CanChangeMenuFlyoutPresenterStyleAtRuntime()
	{
		var menuFlyout = await CreateMenuFlyout(FlyoutPlacementMode.Bottom);

		var target = await FlyoutHelper.CreateTarget(
			100 /*width*/, 100 /*height*/,
			ThicknessHelper.FromUniformLength(10),
			HorizontalAlignment.Center,
			VerticalAlignment.Top);

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			VERIFY_IS_NOT_NULL(rootPanel);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var style = new Style(typeof(MenuFlyoutPresenter));
			style.Setters.Add(new Setter(MenuFlyoutPresenter.TagProperty, "presenter_style_2"));

			menuFlyout.MenuFlyoutPresenterStyle = style;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var presenter = GetMenuFlyoutPresenter(menuFlyout);
			var tag = presenter.GetValue(MenuFlyoutPresenter.TagProperty);
			VERIFY_ARE_EQUAL("presenter_style_2", tag.ToString());
		});

		FlyoutHelper.HideFlyout(menuFlyout);

		await RunOnUIThread(() =>
		{
			var style = new Style(typeof(MenuFlyoutPresenter));
			style.Setters.Add(new Setter(MenuFlyoutPresenter.TagProperty, "presenter_style_3"));

			menuFlyout.MenuFlyoutPresenterStyle = style;
		});

		await TestServices.WindowHelper.WaitForIdle();

		FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var presenter = GetMenuFlyoutPresenter(menuFlyout);
			var tag = presenter.GetValue(MenuFlyoutPresenter.TagProperty);
			VERIFY_ARE_EQUAL("presenter_style_3", tag.ToString());
		});

		FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task CanAttachedMenuFlyoutShowHide()
	{
		var menuFlyout = await CreateMenuFlyout(FlyoutPlacementMode.Left);
		VERIFY_IS_NOT_NULL(menuFlyout);

		var target = await FlyoutHelper.CreateTarget(
			200 /*width*/, 200 /*height*/,
			ThicknessHelper.FromUniformLength(10),
			HorizontalAlignment.Center,
			VerticalAlignment.Center);
		VERIFY_IS_NOT_NULL(target);

		await RunOnUIThread(() =>

		   {
			   var rootPanel = new Grid();
			   rootPanel.Children.Add(target);
			   VERIFY_IS_NOT_NULL(rootPanel);

			   TestServices.WindowHelper.WindowContent = rootPanel;

			   var attachedMenuFlyout = MenuFlyout.GetAttachedFlyout(target);
			   VERIFY_IS_NULL(attachedMenuFlyout);

			   MenuFlyout.SetAttachedFlyout(target, menuFlyout);

			   attachedMenuFlyout = MenuFlyout.GetAttachedFlyout(target);
			   VERIFY_IS_NOT_NULL(attachedMenuFlyout);
		   });

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("CanAttachedMenuFlyoutShowHide: Execute ShowAttachedMenuFlyout.");
		FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAttachedFlyout);

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("CanAttachedMenuFlyoutShowHide: Execute the Hide.");
		FlyoutHelper.HideFlyout(menuFlyout);

		LOG_OUTPUT("CanAttachedMenuFlyoutShowHide: Execute ShowAt.");
		FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("CanAttachedMenuFlyoutShowHide: Execute the Hide.");
		FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task CanClickMenuFlyoutItem()
	{
		MenuFlyoutItem menuItem = null;
		var menuItemClickEvent = new Event();
		var clickRegistration = CreateSafeEventRegistration<MenuFlyoutItem, RoutedEventHandler>(nameof(MenuFlyoutItem.Click));

		var menuFlyout = await CreateMenuFlyout(FlyoutPlacementMode.Bottom);
		VERIFY_IS_NOT_NULL(menuFlyout);

		var target = await FlyoutHelper.CreateTarget(
			100 /*width*/, 100 /*height*/,
			ThicknessHelper.FromUniformLength(10),
			HorizontalAlignment.Left,
			VerticalAlignment.Top);
		VERIFY_IS_NOT_NULL(target);

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			VERIFY_IS_NOT_NULL(rootPanel);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var commandExecute = (object p) =>
			{
				LOG_OUTPUT("VerifyMenuFlyoutPresenterStyle: Process the delegate!");
			};

			menuItem = (MenuFlyoutItem)(menuFlyout.Items[1]);
			VERIFY_IS_NOT_NULL(menuItem);

			var menuItemCommand = new MenuCommand(commandExecute, true /*canExecute*/, m_menuCommandParam1);
			menuItem.Command = menuItemCommand;
			menuItem.CommandParameter = m_menuCommandParam1;

			clickRegistration.Attach(menuItem, (s, e) =>
			{
				menuItemClickEvent.Set();
			});
		});

		LOG_OUTPUT("VerifyMenuFlyoutPresenterStyle: Execute the ShowAt.");
		FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("MenuItem Tap operation to execute menu item command.");
		TestServices.InputHelper.Tap(menuItem);
		await menuItemClickEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			menuItem.Command = null;
			menuItem.CommandParameter = null;
		});
	}

	[TestMethod]
	public async Task CanClickToggleMenuFlyoutItem()
	{
		ToggleMenuFlyoutItem toggleMenuItem = null;

		var toggleMenuItemClickEvent = new Event();
		var clickRegistration = CreateSafeEventRegistration<MenuFlyoutItem, RoutedEventHandler>(nameof(MenuFlyoutItem.Click));

		var menuFlyout = await CreateMenuFlyout(FlyoutPlacementMode.Bottom);
		VERIFY_IS_NOT_NULL(menuFlyout);

		var target = await FlyoutHelper.CreateTarget(
			50 /*width*/, 50 /*height*/,
			ThicknessHelper.FromUniformLength(10),
			HorizontalAlignment.Right,
			VerticalAlignment.Top);
		VERIFY_IS_NOT_NULL(target);

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			VERIFY_IS_NOT_NULL(rootPanel);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var commandExecute = (object param) =>
			{
				LOG_OUTPUT("VerifyMenuFlyoutPresenterStyle: Process the delegate!");
			};

			toggleMenuItem = (ToggleMenuFlyoutItem)(menuFlyout.Items[4]);
			VERIFY_IS_NOT_NULL(toggleMenuItem);

			var toggleMenuItemCommand = new MenuCommand(commandExecute, true /*canExecute*/, m_menuCommandParam2);
			toggleMenuItem.Command = toggleMenuItemCommand;
			toggleMenuItem.CommandParameter = m_menuCommandParam2;

			clickRegistration.Attach(toggleMenuItem, (s, e) =>
			{
				toggleMenuItemClickEvent.Set();
			});
		});

		LOG_OUTPUT("VerifyMenuFlyoutPresenterStyle: Execute the ShowAt.");
		FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("ToggleMenuItem Tap operation to execute menu item command.");
		TestServices.InputHelper.Tap(toggleMenuItem);
		await toggleMenuItemClickEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			toggleMenuItem.Command = null;
			toggleMenuItem.CommandParameter = null;
		});
	}

	private async Task<MenuFlyout> CreateMenuFlyout(FlyoutPlacementMode placement)
	{
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>

		   {
			   var item1 = new MenuFlyoutItem();
			   item1.Text = "COPY";
			   item1.Width = 250;
			   item1.FontSize = 20;

			   var item2 = new MenuFlyoutItem();
			   item2.Text = "SELECT";
			   item2.Width = 250;
			   item2.FontSize = 20;

			   var item3 = new MenuFlyoutItem();
			   item3.Text = "PASTE";
			   item3.Width = 250;
			   item3.FontSize = 20;
			   item3.IsEnabled = false;

			   var item4 = new MenuFlyoutSeparator();
			   item4.Width = 250;

			   var item5 = new ToggleMenuFlyoutItem();
			   item5.Text = "SMALL";
			   item5.Width = 250;
			   item5.FontSize = 20;

			   var item6 = new ToggleMenuFlyoutItem();
			   item6.Text = "MEDIUM";
			   item6.Width = 250;
			   item6.FontSize = 20;
			   item6.IsChecked = true;
			   item6.IsEnabled = false;

			   var item7 = new ToggleMenuFlyoutItem();
			   item7.Text = "LARGE";
			   item7.Width = 250;
			   item7.FontSize = 20;

			   var item8 = new MenuFlyoutSeparator();
			   item8.Width = 250;

			   var item9 = new MenuFlyoutItem();
			   item9.Text = "EXIT";
			   item9.Width = 250;
			   item9.FontSize = 20;

			   var style = new Style(typeof(MenuFlyoutPresenter));
			   style.Setters.Add(new Setter(MenuFlyoutPresenter.TagProperty, "presenter_style"));

			   menuFlyout = new MenuFlyout();
			   menuFlyout.Placement = placement;
			   menuFlyout.MenuFlyoutPresenterStyle = style;

			   menuFlyout.Items.Add(item1);
			   menuFlyout.Items.Add(item2);
			   menuFlyout.Items.Add(item3);
			   menuFlyout.Items.Add(item4);
			   menuFlyout.Items.Add(item5);
			   menuFlyout.Items.Add(item6);
			   menuFlyout.Items.Add(item7);
			   menuFlyout.Items.Add(item8);
			   menuFlyout.Items.Add(item9);
		   });
		return menuFlyout;
	}

	private MenuFlyoutPresenter GetMenuFlyoutPresenter(MenuFlyout menuFlyout)
	{
		VERIFY_IS_TRUE(menuFlyout.Items.Count > 0);
		var item = (DependencyObject)(menuFlyout.Items[0]);
		VERIFY_IS_NOT_NULL(item);

		return TreeHelper.FindAncestor<MenuFlyoutPresenter>(item);
	}

	//[TestMethod]
	//public async Task ValidateShowAtTargetPosition()
	//{
	//	// The rules for positioning the menuflyout when calling ShowAt with Point(X,Y) are as follows:
	//	//   - The menu should be positioned so that (X,Y) is the Top Left corner of the menu.
	//	//   - For touch input (X,Y) should be the Bottom Left corner instead.
	//	//   - If this would place the menu so that it is vertically clipped off-screen we do the opposite of above (i.e. Bottom Left for mouse, Top Left for touch).
	//	//   - If the menuflyout is too tall for either of these to fit, we align it to the top of the screen.
	//	// We also adjust in the horizontal direction:
	//	//   - We try to position the menuflyout so that X is at the Left edge of the menu.
	//	//   - If this would result in the menu being clipped horizontally, we try position it so that X is at the Right edge.
	//	//   - If the menuflyout is too wide to position either its Right or Left edge at X, we align to the Left of the screen.
	//	//   - When in Right-To-Left mode, the logic is the same as the above, with Right and Left swapped.
	//	//   - When using Pen, if  SPI_GETHANDEDNESS returns "Right", then the menuflyout should show with the right edge at the X location.

	//	// We test the above logic by choosing values for Point(X,Y), the input mode and the FlowDirection.
	//	//  - Point(X,Y):
	//	//      This is the point passed to MenuFlyout ShowAt.
	//	//      Values for X are chosen to be either near the left edge of the screen, the right edge of the screen, or the center.
	//	//      Values for Y are chosen to be either near the top of the screen, the bottom of the screen or the center.
	//	//  - Input Mode:
	//	//      Either Mouse, Keyboard or Touch
	//	//      (note, we mostly test with Keyboard and Touch because Mouse input helper doesn't work on phone).
	//	//  - FlowDirection:
	//	//      Either LeftToRight or RightToLeft
	//	//
	//	// Points that are "near" a particular edge of the screen are such that the point is too close to that edge for the menuflyout to open in that direction
	//	//
	//	// For each set of values that we test we specify the expected position of the open flyout:
	//	//   HorizontalOpenDirection:
	//	//     OpenRight: The menu should open to the Right, i.e. Point(X,Y) is on the Left edge of the menu.
	//	//     OpenLeft: The menu should open to the Left, i.e. Point(X,Y) is on the Right edge of the menu.
	//	//   VerticalOpenDirection:
	//	//     OpenUp: the menu should open Up, i.e. Point(X,Y) is on the Bottom edge of the menu.
	//	//     OpenDown: the menu should open Down, i.e. Point(X,Y) is on the Top edge of the menu.

	//	await RunOnUIThread(() =>
	//	{
	//		var rootPanel = new Grid();
	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	Rect windowBounds = default;
	//	await RunOnUIThread(() =>
	//	{
	//		windowBounds = TestServices.WindowHelper.WindowBounds;
	//	});
	//	LOG_OUTPUT("Windows bounds left=%f top=%f width=%f height=%f", windowBounds.Left, windowBounds.Top, windowBounds.Width, windowBounds.Height);

	//	float nearTop = 50;
	//	float nearBottom = windowBounds.Bottom - 50;
	//	float verticalCenter = floor(windowBounds.Y + (windowBounds.Height / 2));
	//	float nearLeft = 50;
	//	float nearRight = windowBounds.Right - 50;
	//	float horizontalCenter = floor(windowBounds.X + (windowBounds.Width / 2));

	//	// Simple Mouse case:
	//	if (!TestServices.Utilities.IsOneCore)
	//	{
	//		// Mouse input helper doesn't work on phone or onecore.
	//		LOG_OUTPUT("-------------");
	//		LOG_OUTPUT("TEST: Simple Mouse case");
	//		DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Mouse, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);
	//	}

	//	// Simple Keyboard case:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Simple Keyboard case");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Keyboard, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Simple Touch case:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Simple Touch case");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Touch, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near bottom, non-touch:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near bottom, non-touch");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, nearBottom), InputMethod.Keyboard, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near bottom, touch:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near bottom, touch");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, nearBottom), InputMethod.Touch, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near top, non-touch:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near top, non-touch");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, nearTop), InputMethod.Keyboard, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near top, touch:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near top, touch");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, nearTop), InputMethod.Touch, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near right:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near right");
	//	DoValidateShowAtTargetPosition(Point(nearRight, verticalCenter), InputMethod.Keyboard, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near left:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near left");
	//	DoValidateShowAtTargetPosition(Point(nearLeft, verticalCenter), InputMethod.Keyboard, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Simple RTL case:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Simple RTL case");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Keyboard, FlowDirection.RightToLeft,
	//		HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near right, RTL
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near right, RTL");
	//	DoValidateShowAtTargetPosition(Point(nearRight, verticalCenter), InputMethod.Keyboard, FlowDirection.RightToLeft,
	//		HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near left, RTL
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near left, RTL");
	//	DoValidateShowAtTargetPosition(Point(nearLeft, verticalCenter), InputMethod.Keyboard, FlowDirection.RightToLeft,
	//		HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Right handed touch case");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Touch, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// RTL touch case:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: RTL touch case");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Touch, FlowDirection.RightToLeft,
	//		HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// MOCK10_REMOVAL : Reenable with bug 20936312
	//	// Left handed touch case
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Left handed touch case");
	//	//DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Touch, FlowDirection.LeftToRight,
	//	//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);
	//}

	//[TestMethod]
	//public async Task ValidateShowAtTargetPositionForPen()
	//{


	//	await RunOnUIThread(() =>

	//	   {
	//		   var rootPanel = new Grid();
	//		   TestServices.WindowHelper.WindowContent = rootPanel;
	//	   });
	//	await TestServices.WindowHelper.WaitForIdle();

	//	Rect windowBounds = default;
	//	await RunOnUIThread(() =>

	//	   {
	//		   windowBounds = TestServices.WindowHelper.WindowBounds;
	//	   });
	//	LOG_OUTPUT("Windows bounds left=%f top=%f width=%f height=%f", windowBounds.Left, windowBounds.Top, windowBounds.Width, windowBounds.Height);

	//	float nearTop = 50;
	//	float nearBottom = windowBounds.Bottom - 50;
	//	float verticalCenter = floor(windowBounds.Y + (windowBounds.Height / 2));
	//	float nearLeft = 50;
	//	//float nearRight = windowBounds.Right - 50; // MOCK10_REMOVAL avoid local variable is initialized but not referenced error - Reenable with bug 20936312
	//	float horizontalCenter = floor(windowBounds.X + (windowBounds.Width / 2));

	//	// Right handed (default) Pen cases:

	//	// Simple Pen case:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Simple Pen case");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near top, pen:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near top, pen");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, nearTop), InputMethod.Pen, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near bottom, pen:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near bottom, pen");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, nearBottom), InputMethod.Pen, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// RTL cases, Pen should still open to the left

	//	// Simple Pen case:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Simple Pen case (RTL)");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
	//		HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near top, pen:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near top, pen (RTL)");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, nearTop), InputMethod.Pen, FlowDirection.RightToLeft,
	//		HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	//// MOCK10_REMOVAL : Reenable with bug 20936312
	//	//// Left handed cases, now menus should open to the right.

	//	//// Simple Pen case:
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Simple Pen case (left handed)");
	//	//DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
	//	//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	//// Simple Pen case:
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Simple Pen case (RTL, left-handed)");
	//	//DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
	//	//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	//// Near top, pen:
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Near top, pen (left-handed)");
	//	//DoValidateShowAtTargetPosition(Point(horizontalCenter, nearTop), InputMethod.Pen, FlowDirection.LeftToRight,
	//	//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near bottom, pen:
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Near bottom, pen (left-handed)");
	//	//DoValidateShowAtTargetPosition(Point(horizontalCenter, nearBottom), InputMethod.Pen, FlowDirection.LeftToRight,
	//	//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// RTL right-handed cases, Pen should still open to the left

	//	// Simple Pen case:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Simple Pen case (RTL, right-handed)");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
	//		HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near top, pen:
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near top, pen (RTL, right-handed)");
	//	DoValidateShowAtTargetPosition(Point(horizontalCenter, nearTop), InputMethod.Pen, FlowDirection.RightToLeft,
	//		HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// Near left, pen, right-handed (should flip to right)
	//	LOG_OUTPUT("-------------");
	//	LOG_OUTPUT("TEST: Near left, pen, right-handed -- should flip to right");
	//	DoValidateShowAtTargetPosition(Point(nearLeft, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
	//		HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	// MOCK10_REMOVAL : Reenable with bug 20936312
	//	// Near left, pen, left-handed (should flip to left)
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Near right, pen, left-handed -- should flip to left");
	//	//DoValidateShowAtTargetPosition(Point(nearRight, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
	//	//    HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

	//	//// Explicitly force popups to be not windowed even on desktop using FlyoutBase.ShouldConstrainToRootBounds and run the Pen scenarios
	//	//// again to exercise the non-windowed codepath.

	//	//// Simple Pen case:
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Simple Pen case (MOCK: non-HWND path)");
	//	//DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
	//	//    HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

	//	//// RTL cases, Pen should still open to the left

	//	//// Simple Pen case:
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Simple Pen case (RTL) (MOCK: non-HWND path)");
	//	//DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
	//	//    HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

	//	//// Near top, pen:
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Near top, pen (RTL) (MOCK: non-HWND path)");
	//	//DoValidateShowAtTargetPosition(Point(horizontalCenter, nearTop), InputMethod.Pen, FlowDirection.RightToLeft,
	//	//    HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

	//	//// Change handedness to left, now menus should open to the right.

	//	//// Simple Pen case:
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Simple Pen case (left handed) (MOCK: non-HWND path)");
	//	//DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
	//	//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

	//	//// Simple Pen case:
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Simple Pen case (RTL, left-handed) (MOCK: non-HWND path)");
	//	//DoValidateShowAtTargetPosition(Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
	//	//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

	//	//// Near left, pen, right-handed (should flip to right)
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Near left, pen, right-handed -- should flip to right (MOCK: non-HWND path)");
	//	//DoValidateShowAtTargetPosition(Point(nearLeft, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
	//	//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

	//	//// Near left, pen, left-handed (should flip to left)
	//	//LOG_OUTPUT("-------------");
	//	//LOG_OUTPUT("TEST: Near right, pen, left-handed -- should flip to left (MOCK: non-HWND path)");
	//	//DoValidateShowAtTargetPosition(Point(nearRight, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
	//	//    HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);
	//}

	//void DoValidateShowAtTargetPosition(
	//	Point showAtPosition,
	//	InputMethod inputMethod,
	//	FlowDirection flowDirection,
	//	HorizontalOpenDirection expectedHorizontalOpenDirection,
	//	VerticalOpenDirection expectedVerticalOpenDirection,
	//	bool /*mockLeftHandedness*/,
	//	bool /*disableFullHwndSupport*/)
	//{
	//	/* MOCK10_REMOVAL : Reenable with bug 20936312
	//       typedef Mock10.MockFunction<BOOL NTAPI(uint, uint, PVOID, uint)>.Prototype SystemParametersInfoWPrototype;

	//       DWORD dwMockHandedness = mockLeftHandedness ? HANDEDNESS_LEFT : HANDEDNESS_RIGHT;

	//       // Within this scope, SystemParametersInfo is mocked
	//       Mock10.MockFunction<SystemParametersInfoWPrototype> functionSystemParametersInfo(Mock10.Mock.Function<SystemParametersInfoWPrototype>(SystemParametersInfoW));
	//       functionSystemParametersInfo.Set(
	//           [&](uint uiAction,
	//               uint uiParam,
	//               PVOID pvParam,
	//               uint fWinIni) . BOOL volatile
	//       {
	//           if (uiAction == SPI_GETHANDEDNESS)
	//           {
	//               *(LPDWORD)(pvParam) = dwMockHandedness;
	//               return true;
	//           }

	//           return SystemParametersInfoW(uiAction, uiParam, pvParam, fWinIni);
	//       });
	//       */

	//	var menuFlyout = CreateMenuFlyout();

	//	FrameworkElement ^ rootPanel;
	//	Rect windowBounds;

	//	await RunOnUIThread(() =>

	//	   {
	//		   rootPanel = (FrameworkElement)(TestServices.WindowHelper.WindowContent);
	//		   rootPanel.FlowDirection = flowDirection;
	//		   windowBounds = TestServices.WindowHelper.WindowBounds;

	//		   // TODO: As part of Task 23865114, set menuFlyout.ShouldConstrainToRootBounds based on disableFullHwndSupport
	//		   // to handle windowed popups.
	//	   });
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var openedEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	openedRegistration.Attach(menuFlyout, [&](){ openedEvent.Set(); });

	//	// Send some input of the appropriate type to the app.
	//	// The placement of the menuflyout is affected by the most recently used input device type.
	//	if (inputMethod == InputMethod.Mouse)
	//	{
	//		LOG_OUTPUT("Calling InputHelper.LeftMouseClick");
	//		TestServices.InputHelper.LeftMouseClick(rootPanel);
	//	}
	//	else if (inputMethod == InputMethod.Touch)
	//	{
	//		LOG_OUTPUT("Calling InputHelper.Tap");
	//		TestServices.InputHelper.Tap(rootPanel);
	//	}
	//	else if (inputMethod == InputMethod.Keyboard)
	//	{
	//		LOG_OUTPUT("Calling KeyboardHelper.PressKeySequence");
	//		TestServices.KeyboardHelper.PressKeySequence(" ");
	//	}
	//	else if (inputMethod == InputMethod.Pen)
	//	{
	//		LOG_OUTPUT("Calling InputHelper.PenTap");
	//		TestServices.InputHelper.PenTap(rootPanel);
	//	}
	//	else
	//	{
	//		WEX.Common.Throw.Exception(E_NOTIMPL);
	//	}
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Show the MenuFlyout:
	//	await RunOnUIThread(() =>

	//	   {
	//		   menuFlyout.ShowAt(null, showAtPosition);
	//	   });
	//	await openedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await RunOnUIThread(() =>

	//	   {
	//		   var flyoutPresenter = FlyoutHelper.GetOpenFlyoutPresenter();
	//		   var menuFlyoutBounds = await ControlHelper.GetBounds(flyoutPresenter);

	//		   LOG_OUTPUT("Windows bounds left=%f top=%f width=%f height=%f", windowBounds.Left, windowBounds.Top, windowBounds.Width, windowBounds.Height);
	//		   LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);
	//		   LOG_OUTPUT("showAtPosition (%f,%f)", showAtPosition.X, showAtPosition.Y);

	//		   VERIFY_IS_TRUE(ControlHelper.IsContainedIn(menuFlyoutBounds, windowBounds));

	//		   // Validate Horizontal position:
	//		   if (expectedHorizontalOpenDirection == HorizontalOpenDirection.OpenRight)
	//		   {
	//			   VERIFY_ARE_EQUAL(menuFlyoutBounds.Left, showAtPosition.X);
	//		   }
	//		   else if (expectedHorizontalOpenDirection == HorizontalOpenDirection.OpenLeft)
	//		   {
	//			   VERIFY_ARE_EQUAL(menuFlyoutBounds.Right, showAtPosition.X);
	//		   }

	//		   // Validate Vertical position:
	//		   if (expectedVerticalOpenDirection == VerticalOpenDirection.OpenDown)
	//		   {
	//			   VERIFY_ARE_EQUAL(menuFlyoutBounds.Top, showAtPosition.Y);
	//		   }
	//		   else if (expectedVerticalOpenDirection == VerticalOpenDirection.OpenUp)
	//		   {
	//			   VERIFY_ARE_EQUAL(menuFlyoutBounds.Bottom, showAtPosition.Y);
	//		   }
	//	   });

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod]
	//public async Task ValidateShowAtTargetPositionRelativeToElement()
	//{
	//	// When a UIElement is passed to MenuFlyout.ShowAt(UIElement, Point), we verify that the Point is transformed from the UIElement's space to
	//	// the global space for the positioning of the MenuFlyout.


	//	var menuFlyout = CreateMenuFlyout();

	//	Button button1;
	//	Point showAtPositionRelative = Point(50, 50); // This is the position we pass to ShowAt. It is relative to button1.
	//	Point showAtPositionAbsolute; // The showAtPositionRelative point in absolute space.
	//								  // We pass showAtPositionRelative to ShowAt and expect the menuflyout to be positioned at showAtPositionAbsolute.

	//	await RunOnUIThread(() =>

	//	   {
	//		   var rootPanel = Grid > (XamlReader.Load(
	//			   LR"(<Grid Background="Orange"



	//					   xmlns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns: x = "http://schemas.microsoft.com/winfx/2006/xaml" >



	//					 < Button x: Name = "button1" Content = "Button" Width = "100" Height = "50" HorizontalAlignment = "Left" VerticalAlignment = "Top" Margin = "50, 200, 0, 0" />



	//				   </ Grid >)"));




	//		   button1 = (Button)(rootPanel.FindName("button1"));

	//		   TestServices.WindowHelper.WindowContent = rootPanel;
	//	   });
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ShowMenuFlyout(menuFlyout, button1, showAtPositionRelative.X, showAtPositionRelative.Y, true);

	//	await RunOnUIThread(() =>

	//	   {
	//		   showAtPositionAbsolute = button1.TransformToVisual(null).TransformPoint(showAtPositionRelative);

	//		   var flyoutPresenter = FlyoutHelper.GetOpenFlyoutPresenter();
	//		   var menuFlyoutBounds = await ControlHelper.GetBounds(flyoutPresenter);

	//		   VERIFY_ARE_EQUAL(menuFlyoutBounds.Left, showAtPositionAbsolute.X);
	//		   VERIFY_ARE_EQUAL(menuFlyoutBounds.Bottom, showAtPositionAbsolute.Y);
	//	   });

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod]
	//public async Task TallMenuFlyoutShouldAlignToTopOfScreen()
	//{


	//	if (!TestServices.Utilities.IsDesktop)
	//	{
	//		Windows.Foundation.Size size(400, 400);
	//		TestServices.WindowHelper.SetWindowSizeOverride(size);
	//	}

	//	// MenuFlyout will normally align either it's top or bottom edge to the point passed to ShowAt.
	//	// But if the MenuFlyout is too tall to fit in either of these positions, it aligns to the top of the screen.
	//	//
	//	// To test this:
	//	// We set the height of a menuflyoutitem so that the menuflyout size is greater than half the height of the screen.
	//	// We call ShowAt with a point in the middle of the screen.
	//	// This ensures that it can not align either it's top or bottom edge to the given point without getting clipped.
	//	// We validate that the menuflyout opens aligned to the top of the screen.

	//	var menuFlyout = CreateMenuFlyout();

	//	await RunOnUIThread(() =>

	//	   {
	//		   TestServices.WindowHelper.WindowContent = new Grid();
	//	   });
	//	await TestServices.WindowHelper.WaitForIdle();

	//	Rect windowBounds = default;
	//	await RunOnUIThread(() =>

	//	   {
	//		   windowBounds = TestServices.WindowHelper.WindowBounds;
	//		   menuFlyout.Items[0].Height = windowBounds.Height * 0.75;
	//	   });
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ShowMenuFlyout(menuFlyout, null, windowBounds.Width / 2, windowBounds.Height / 2);

	//	await RunOnUIThread(() =>

	//	   {
	//		   var flyoutPresenter = FlyoutHelper.GetOpenFlyoutPresenter();
	//		   var menuFlyoutBounds = await ControlHelper.GetBounds(flyoutPresenter);

	//		   // Verify that the top of the menuflyout is at the top of the screen.
	//		   VERIFY_ARE_EQUAL(menuFlyoutBounds.Top, -windowBounds.Y);
	//	   });

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod]
	//public async Task WideMenuFlyoutShouldAlignToLeftOfScreen()
	//{


	//	// Similar to TallMenuFlyoutShouldAlignToTopOfScreen, if a MenuFlyout is too wide to open at the point passed to ShowAt, it will
	//	// open aligned to the left instead.

	//	MenuFlyout menuFlyout;
	//	MenuFlyoutItem menuFlyoutItem;

	//	await RunOnUIThread(() =>

	//	   {
	//		   // We need to set MenuFlyoutPresenter.MaxWidth to infinity, otherwise its default value may prevent us from creating
	//		   // a menuflyout wide enough to hit the scenario we are trying to test.
	//		   var rootPanel = Grid > (XamlReader.Load(
	//			   "(<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" >


	//					   < Grid.Resources >



	//						   < MenuFlyout x: Name = "menuFlyout" >



	//							   < MenuFlyout.MenuFlyoutPresenterStyle >



	//								   < Style TargetType = "MenuFlyoutPresenter" >



	//									   < Setter Property = "MaxWidth" Value = "Infinity" />



	//								   </ Style >



	//							   </ MenuFlyout.MenuFlyoutPresenterStyle >



	//							   < MenuFlyoutItem x: Name = "menuFlyoutItem" Text = "MenuFlyoutItem" />



	//						   </ MenuFlyout >



	//					   </ Grid.Resources >



	//				   </ Grid >)"));




	//		   menuFlyout = (MenuFlyout)(rootPanel.FindName("menuFlyout"));
	//		   menuFlyoutItem = (MenuFlyoutItem)(rootPanel.FindName("menuFlyoutItem"));

	//		   TestServices.WindowHelper.WindowContent = rootPanel;
	//	   });
	//	await TestServices.WindowHelper.WaitForIdle();

	//	Rect windowBounds = default;
	//	await RunOnUIThread(() =>

	//	   {
	//		   windowBounds = TestServices.WindowHelper.WindowBounds;
	//		   menuFlyoutItem.Width = windowBounds.Width * 0.75;
	//	   });
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ShowMenuFlyout(menuFlyout, null, windowBounds.Width / 2, windowBounds.Height / 2);

	//	await RunOnUIThread(() =>

	//	   {
	//		   var flyoutPresenter = FlyoutHelper.GetOpenFlyoutPresenter();
	//		   var menuFlyoutBounds = await ControlHelper.GetBounds(flyoutPresenter);

	//		   // Verify that the left of the menuflyout is at the left of the screen.
	//		   VERIFY_ARE_EQUAL(menuFlyoutBounds.Left, -windowBounds.X);
	//	   });

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	private async Task<MenuFlyout> CreateMenuFlyout()
	{
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>

		   {
			   var item1 = new MenuFlyoutItem();
			   item1.Text = "Menu Item1";
			   item1.Tag = "MFI1";

			   var item2 = new MenuFlyoutItem();
			   item2.Text = "Menu Item2";
			   item2.Tag = "MFI2";
			   item2.IsEnabled = false;

			   var item3 = new MenuFlyoutSeparator();
			   item3.Tag = "MFS1";

			   var subItem = new MenuFlyoutSubItem();
			   subItem.Text = "Menu SubItem";
			   subItem.Tag = "MFSI1";

			   var subItemItem1 = new MenuFlyoutItem();
			   subItemItem1.Text = "Menu SubItem Item1";
			   subItemItem1.Tag = "MFSII1";

			   var subItemItem2 = new MenuFlyoutItem();
			   subItemItem2.Text = "Menu SubItem Item2";
			   subItemItem2.Tag = "MFSII2";

			   subItem.Items.Add(subItemItem1);
			   subItem.Items.Add(subItemItem2);

			   menuFlyout = new MenuFlyout();
			   menuFlyout.Items.Add(item1);
			   menuFlyout.Items.Add(item2);
			   menuFlyout.Items.Add(item3);
			   menuFlyout.Items.Add(subItem);
		   });

		return menuFlyout;
	}

	private async Task<MenuFlyout> CreateMenuFlyoutWithSubItem()
	{
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>

		   {
			   var item1 = new MenuFlyoutItem();
			   item1.Text = "Menu Item1";

			   var item2 = new MenuFlyoutSubItem();
			   item2.Text = "Menu Item2";

			   var item3 = new MenuFlyoutItem();
			   item3.Text = "Menu Item3";

			   item2.Items.Add(item3);

			   menuFlyout = new MenuFlyout();
			   menuFlyout.Items.Add(item1);
			   menuFlyout.Items.Add(item2);
		   });

		return menuFlyout;
	}

	[TestMethod]
	public async Task ValidateOnlyOneSubMenuItemIsOpenAtATimeByTouch()
	{
		Button button1 = null;
		IList<MenuFlyoutItemBase> items = null;
		MenuFlyoutSubItem subItem = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = null;

		var subItem1LostFocusEvent = new Event();
		var subItem1LostFocusRegistration = CreateSafeEventRegistration<MenuFlyoutSubItem, RoutedEventHandler>(nameof(MenuFlyoutSubItem.LostFocus));

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutSubItem x:Name='subItem1' Text='Menu sub item 1'>" +
				"        <MenuFlyoutItem>Menu item 1.1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem>Menu item 1.2</MenuFlyoutItem>" +
				"    </MenuFlyoutSubItem>" +
				"    <MenuFlyoutSubItem x:Name='subItem2' Text='Menu sub item 2'>" +
				"        <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>" +
				"    </MenuFlyoutSubItem>" +
				"</MenuFlyout>"));

			button1 = (Button)(rootPanel.FindName("button1"));
		});

		await ShowMenuFlyout(menuFlyout, button1, -50, 50);

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
		});

		// Get sub menu items 1, set up event registration, and tap on it
		subItem = await GetSubItem(items, 0);
		await RunOnUIThread(async () =>

		   {
			   subItem1LostFocusRegistration.Attach(subItem, (s, e) =>


				{
					subItem1LostFocusEvent.Set();
				});
		   });
		await TapSubMenuItem(subItem);

		// Tap sub menu item 2 and verify that subItem1 is closed
		subItem = await GetSubItem(items, 1);
		await TapSubMenuItem(subItem);
		await subItem1LostFocusEvent.WaitForDefault();

		FlyoutHelper.HideFlyout(menuFlyout);
	}

	//[TestMethod]
	//public async Task ValidateSubMenuItemByGamepad()
	//{
	//	PerformValidateSubMenuItem(InputMethod.Gamepad);
	//}

	//[TestMethod]
	//public async Task ValidateSubMenuItemByKeyboard()
	//{
	//	PerformValidateSubMenuItem(InputMethod.Keyboard);
	//}

	//[TestMethod]
	//public async Task ValidateSubMenuItemByMouse()
	//{
	//	PerformValidateSubMenuItem(InputMethod.Mouse);
	//}

	//[TestMethod]
	//public async Task ValidateSubMenuItemByRemote()
	//{
	//	PerformValidateSubMenuItem(InputMethod.Remote);
	//}

	//[TestMethod]
	//public async Task ValidateSubMenuItemByTouch()
	//{
	//	PerformValidateSubMenuItem(InputMethod.Touch);
	//}

	//[TestMethod]
	//public async Task ValidateTraverseMenuFlyoutItemsByGamepad()
	//{
	//	PerformValidateTraverseMenuFlyoutItems(InputMethod.Gamepad, true);
	//	PerformValidateTraverseMenuFlyoutItems(InputMethod.Gamepad, false);
	//}

	//[TestMethod]
	//public async Task ValidateTraverseMenuFlyoutItemsByKeyboard()
	//{
	//	PerformValidateTraverseMenuFlyoutItems(InputMethod.Keyboard, true);
	//	PerformValidateTraverseMenuFlyoutItems(InputMethod.Keyboard, false);
	//}

	//[TestMethod] public async Task ValidateSubMenuItemInPage()
	//{


	//	Button button1 = null;

	//	Page ^ page = null;
	//	Canvas rootPanel = null;
	//	MenuFlyout menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

	//	var loadedEvent = new Event();
	//	var loadedRegistration = CreateSafeEventRegistration(Page, Loaded);

	// await RunOnUIThread(() =>

	//	{
	//		page = TestServices.WindowHelper.SetupSimulatedAppPage();
	//		rootPanel = Canvas> (XamlReader.Load(
	//			"<Canvas Background='RoyalBlue' "

	//			" xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >"

	//			"    <Button x:Name='button1' Content='Button' Width='100' Height='50' Canvas.Left='50' Canvas.Top='50' />"

	//			"</Canvas>"));
	//		button1 = Button> (rootPanel.FindName("button1"));

	//		page.Content = rootPanel;
	//		loadedRegistration.Attach(page, [loadedEvent]() { loadedEvent.Set(); });
	//	});

	//	await loadedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ShowMenuFlyout(menuFlyout, button1, -50, 50);
	//	NavigateSubMenu(menuFlyout, button1, InputDevice.Keyboard);
	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateCollapsingResetsVisualState()
	//{


	//	MenuFlyout menuFlyout;
	//	MenuFlyoutItem menuFlyoutItem;
	//	Grid menuFlyoutItemLayoutRoot;
	//	MenuFlyoutSubItem menuFlyoutSubItem;
	//	Grid menuFlyoutSubItemLayoutRoot;

	//	StackPanel ^ rootPanel = null;
	//	Button rootButton = null;

	// await RunOnUIThread(() =>

	//	{
	//		rootPanel = new StackPanel();

	//		menuFlyout = new MenuFlyout();

	//		menuFlyoutItem = new MenuFlyoutItem();
	//		menuFlyoutItem.Text = "Item 1";
	//		menuFlyout.Items.Add(menuFlyoutItem);

	//		menuFlyoutSubItem = new MenuFlyoutSubItem();
	//		menuFlyoutSubItem.Text = "Item 2";
	//		menuFlyout.Items.Add(menuFlyoutSubItem);

	//		rootButton = new Button();
	//		rootButton.Flyout = menuFlyout;
	//		rootPanel.Children.Add(rootButton);

	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ShowMenuFlyout(menuFlyout, rootButton, 0, 0, false /* forceTapAsPreviousInputMessage */);

	// await RunOnUIThread(() =>

	//	{
	//		VERIFY_IS_TRUE(ControlHelper.IsInVisualState(menuFlyoutItem, "CommonStates", "Normal"));

	//		VisualStateManager.GoToState(menuFlyoutItem, "Pressed", false);
	//		VisualStateManager.GoToState(menuFlyoutSubItem, "Pressed", false);
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		VERIFY_IS_TRUE(ControlHelper.IsInVisualState(menuFlyoutItem, "CommonStates", "Pressed"));

	//		menuFlyoutItem.Visibility = Visibility.Collapsed;
	//		menuFlyoutSubItem.Visibility = Visibility.Collapsed;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		VERIFY_IS_TRUE(ControlHelper.IsInVisualState(menuFlyoutItem, "CommonStates", "Normal"));
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//MenuFlyoutSubItem CreateMenuFlyoutSubItem()
	//{
	//	var subItem = new MenuFlyoutSubItem();
	//	subItem.Text = "Sub Item";

	//	var item1 = new MenuFlyoutItem();
	//	item1.Text = "Item1";

	//	var item2 = new MenuFlyoutItem();
	//	item2.Text = "Item2";

	//	var item3 = new MenuFlyoutSeparator();

	//	var item4 = new ToggleMenuFlyoutItem();
	//	item4.Text = "Item4";

	//	subItem.Items.Add(item1);
	//	subItem.Items.Add(item2);
	//	subItem.Items.Add(item3);
	//	subItem.Items.Add(item4);

	//	return subItem;
	//}

	//[TestMethod] public async Task ValidateSubMenuItemPosition()
	//{


	//	TestServices.WindowHelper.SetWindowSizeOverride(Size(1024, 768));

	//	IList < MenuFlyoutItemBase> items = null;
	//	IList < MenuFlyoutItemBase> ^subItems = null;
	//	MenuFlyoutSubItem subItem = null;
	//	Rect windowBounds = default;
	//	Rect menuFlyoutBounds = default;
	//	Rect subMenu1tBounds = default;
	//	Rect subMenu2tBounds = default;

	//	Canvas rootPanel = await SetupRootPanelForSubMenuTest();
	//	MenuFlyout menuFlyout = null;

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout = MenuFlyout> (XamlReader.Load(
	//			"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >"

	//			"    <MenuFlyoutItem>item1</MenuFlyoutItem>"

	//			"    <MenuFlyoutSeparator/>"

	//			"    <MenuFlyoutSubItem x:Name='subItem1' Text='sub item4'>"

	//			"        <MenuFlyoutItem>item1</MenuFlyoutItem>"

	//			"        <MenuFlyoutSeparator/>"

	//			"        <MenuFlyoutSubItem  x:Name='subItem2' Text='subItem2'>"

	//			"            <MenuFlyoutItem>item1</MenuFlyoutItem>"

	//			"            <MenuFlyoutSeparator/>"

	//			"            <MenuFlyoutSubItem x:Name='subItem1' Text='subitem3'/>"

	//			"        </MenuFlyoutSubItem>"

	//			"    </MenuFlyoutSubItem>"

	//			"</MenuFlyout>"));

	//		windowBounds = TestServices.WindowHelper.WindowBounds;

	//		LOG_OUTPUT("Windows bounds left=%f top=%f width=%f height=%f", windowBounds.Left, windowBounds.Top, windowBounds.Width, windowBounds.Height);
	//	});

	//	await ShowMenuFlyout(menuFlyout, null, windowBounds.Width / 4, 50);

	//	var presenter = GetCurrentPresenter();

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
	//		LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);
	//	});

	// await RunOnUIThread(() =>

	//	{
	//		items = menuFlyout.Items;
	//	});

	//	subItem = await GetSubItem(items);
	//	await TapSubMenuItem(subItem);

	//	// Verify the sub menu1 position that it must be positioned to the left side of the MenuFlyout.
	//	presenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		subMenu1tBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
	//		LOG_OUTPUT("SubMenuFlyout bounds left=%f top=%f width=%f height=%f", subMenu1tBounds.Left, subMenu1tBounds.Top, subMenu1tBounds.Width, subMenu1tBounds.Height);
	//	});
	//	VERIFY_IS_TRUE(menuFlyoutBounds.Left < subMenu1tBounds.Left);

	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		subItems = subItem.Items;
	//	});

	//	await TapSubMenuItem(GetSubItem(subItems));

	//	// Verify the sub menu2 position that it must be positioned to the right side of the sub menu2 flyout.
	//	presenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		subMenu2tBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
	//		LOG_OUTPUT("SubMenuFlyout bounds left=%f top=%f width=%f height=%f", subMenu2tBounds.Left, subMenu2tBounds.Top, subMenu2tBounds.Width, subMenu2tBounds.Height);
	//	});
	//	VERIFY_IS_TRUE(subMenu1tBounds.Left <= subMenu2tBounds.Left);

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateSubMenuItemUIElementTree()
	//{


	//	TestServices.WindowHelper.SetWindowSizeOverride(Size(400, 600));

	//	Button button1 = null;
	//	IList < MenuFlyoutItemBase> items = null;

	//	Canvas rootPanel = await SetupRootPanelForSubMenuTest();
	//	MenuFlyout menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout.LightDismissOverlayMode = LightDismissOverlayMode.Off;

	//		button1 = Button> (rootPanel.FindName("button1"));
	//	});

	//	// Tap on the button to ensure the button's initial focus state
	//	TestServices.InputHelper.Tap(button1);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ShowMenuFlyout(menuFlyout, button1, -50, 50);
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		items = menuFlyout.Items;
	//	});

	//	var subItem = await GetSubItem(items);
	//	await TapSubMenuItem(subItem);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	if (TestServices.Utilities.IsOneCore)
	//	{
	//		TestServices.Utilities.VerifyUIElementTree("WindowlessPopup");
	//	}
	//	else
	//	{
	//		TestServices.Utilities.VerifyUIElementTree("WindowedPopup");
	//	}

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidatePopupWindowedPosition()
	//{
	//	ValidatePopupWindowedPosition(true /* expectWindowedPopup */);
	//}

	//[TestMethod] public async Task ValidatePopupWindowedPositionInSimulatedHolographic()
	//{
	//	HolographicOverride holographicOverride;
	//	ValidatePopupWindowedPosition(false /* expectWindowedPopup */);

	//}

	//[TestMethod] public async Task ValidateWindowedPositionNearMonitorEdge()
	//{
	//	TestCleanupWrapper cleanup([&]()

	//	{
	//		TestServices.WindowHelper.ResetWindowContentAndWaitForIdle();
	//	});

	//	Rect menuFlyoutBounds = default;
	//	IList < MenuFlyoutItemBase> items = null;
	//	Canvas rootPanel = await SetupRootPanelForSubMenuTest();

	//	MenuFlyout menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

	//	Rect monitor = TestServices.WindowHelper.MonitorBounds;
	//	LOG_OUTPUT("Monitor size: %f, %f", monitor.Width, monitor.Height);

	//	LOG_OUTPUT("Calling ShowAt %f, %f", monitor.Width - 10, monitor.Height - 50);
	//	await ShowMenuFlyout(menuFlyout, null, monitor.Width - 10, monitor.Height - 50);

	//	var presenter = GetCurrentPresenter();

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutBounds = await ControlHelper.GetBounds(presenter);
	//		LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);
	//		VERIFY_IS_LESS_THAN(monitor.Width - 260, menuFlyoutBounds.Left);
	//		VERIFY_IS_GREATER_THAN(monitor.Width - 240, menuFlyoutBounds.Left);
	//	});

	// await RunOnUIThread(() =>

	//	{
	//		items = menuFlyout.Items;
	//	});

	//	var subItem = await GetSubItem(items);
	//	await TapSubMenuItem(subItem);

	//	// Validate the position
	//	var subPresenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		Rect subMenu = await ControlHelper.GetBounds(subPresenter);
	//		LOG_OUTPUT("Sub Menu bounds left=%f top=%f width=%f height=%f", subMenu.Left, subMenu.Top, subMenu.Width, subMenu.Height);
	//		VERIFY_IS_LESS_THAN(monitor.Width - 540, subMenu.Left);
	//		VERIFY_IS_GREATER_THAN(monitor.Width - 440, subMenu.Left);
	//	});

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}


	//[TestMethod] public async Task ValidatePopupWindowedPosition(bool expectWindowedPopup)
	//{
	//	TestCleanupWrapper cleanup([&]()

	//	{
	//		if (expectWindowedPopup)
	//		{
	//			TestServices.WindowHelper.MaximizeDesktopWindow();
	//			await TestServices.WindowHelper.WaitForIdle();
	//		}

	//		TestServices.WindowHelper.ResetWindowContentAndWaitForIdle();
	//	});

	//	// If we're expecting a windowed popup, we'll set the desktop window size
	//	// in order to test the popup breaking out of the bounds of the desktop window.
	//	// Otherwise, we'll just set the window size in XAML.
	//	if (!expectWindowedPopup)
	//	{
	//		TestServices.WindowHelper.SetWindowSizeOverride(Size(400, 600));
	//	}

	//	Rect menuFlyoutBounds = default;
	//	IList < MenuFlyoutItemBase> items = null;
	//	Canvas rootPanel = await SetupRootPanelForSubMenuTest();

	//	if (expectWindowedPopup)
	//	{
	//		TestServices.WindowHelper.SetDesktopWindowSize(400, 600);
	//		await TestServices.WindowHelper.WaitForIdle();
	//	}

	//	MenuFlyout menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

	//	await ShowMenuFlyout(menuFlyout, null, 400, 280);

	//	var presenter = GetCurrentPresenter();

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutBounds = await ControlHelper.GetBounds(presenter);
	//		LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);

	//		if (expectWindowedPopup)
	//		{
	//			VERIFY_IS_TRUE(menuFlyoutBounds.Left == 400);
	//		}
	//		else
	//		{
	//			// It is not at 400 because the menu needs to shift left to fit inside the app window.
	//			// This is similar for the checks below that do not expectWindowedPopup, the popup needs
	//			// to be adjusted to fit in the windowed dimensions.
	//			VERIFY_IS_FALSE(menuFlyoutBounds.Left == 400);
	//		}
	//	});

	// await RunOnUIThread(() =>

	//	{
	//		items = menuFlyout.Items;
	//	});

	//	var subItem = await GetSubItem(items);
	//	await TapSubMenuItem(subItem);

	//	// Validate the position
	//	var subPresenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		Rect subMenu = await ControlHelper.GetBounds(subPresenter);
	//		LOG_OUTPUT("Sub Menu bounds left=%f top=%f width=%f height=%f", subMenu.Left, subMenu.Top, subMenu.Width, subMenu.Height);

	//		if (expectWindowedPopup)
	//		{
	//			VERIFY_IS_TRUE(subMenu.Left > 550);
	//			VERIFY_IS_TRUE(subMenu.Top + subMenu.Bottom > menuFlyoutBounds.Top + subMenu.Bottom);
	//		}
	//		else
	//		{
	//			VERIFY_IS_FALSE(subMenu.Left > 550);
	//		}
	//	});

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateSubMenuPositionWithinWindow()
	//{


	//	TestServices.WindowHelper.SetWindowSizeOverride(Size(400, 600));

	//	Button button = null;
	//	MenuFlyout menuFlyout = null;
	//	IList < MenuFlyoutItemBase> items = null;

	//	var menuFlyoutOpenedEvent = new Event();
	//	var menuFlyoutClosedEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

	// await RunOnUIThread(() =>

	//	{
	//		var rootPanel = (Grid)(XamlReader.Load(
	//			"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' "

	//			"      x:Name='root' Background='SlateBlue' > "

	//			"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Center' FontSize='20'> "

	//			"    <Button.Flyout> "

	//			"      <MenuFlyout Placement='Bottom'> "

	//			"        <MenuFlyoutItem Text='Copy' /> "

	//			"        <MenuFlyoutItem Text='Paste' /> "

	//			"        <MenuFlyoutSeparator Width='300' /> "

	//			"        <ToggleMenuFlyoutItem FontSize='30' Text='Cut' IsChecked='True' /> "

	//			"        <MenuFlyoutSeparator Width='300' /> "

	//			"        <MenuFlyoutSubItem x:Name='subItem1' Text='Menu sub item 4'>"

	//			"          <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>"

	//			"          <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>"

	//			"          <MenuFlyoutSeparator/>"

	//			"          <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 2.3</ToggleMenuFlyoutItem>"

	//			"          <MenuFlyoutSeparator/>"

	//			"          <MenuFlyoutSubItem  x:Name='subItem2' Text='Menu sub item 2.4'>"

	//			"            <MenuFlyoutItem>Menu item 3.1</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.2</MenuFlyoutItem>"

	//			"            <MenuFlyoutSeparator/>"

	//			"            <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 3.3</ToggleMenuFlyoutItem>"

	//			"            <MenuFlyoutSeparator/>"

	//			"          </MenuFlyoutSubItem>"

	//			"         </MenuFlyoutSubItem>"

	//			"      </MenuFlyout> "

	//			"    </Button.Flyout> "

	//			"  </Button> "

	//			"</Grid>"));

	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//		button = (Button)(rootPanel.FindName("button"));
	//		menuFlyout = (MenuFlyout)(button.Flyout);

	//		openedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			menuFlyoutOpenedEvent.Set();
	//		}));

	//		closedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			menuFlyoutClosedEvent.Set();
	//		}));
	//	});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	TestServices.InputHelper.Tap(button);
	//	await menuFlyoutOpenedEvent.WaitForDefault();

	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		items = menuFlyout.Items;
	//	});

	//	var subItem = await GetSubItem(items);
	//	await TapSubMenuItem(subItem);

	//	var subPresenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		Rect subMenu = await ControlHelper.GetBounds(subPresenter);
	//		LOG_OUTPUT("Sub Menu bounds left=%f top=%f width=%f height=%f", subMenu.Left, subMenu.Top, subMenu.Width, subMenu.Height);

	//		VERIFY_IS_TRUE(subMenu.Left + subMenu.Width <= 400);
	//		VERIFY_IS_TRUE(subMenu.Top + subMenu.Height <= 600);
	//	});

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout.Hide();
	//	});

	//	await menuFlyoutClosedEvent.WaitForDefault();
	//}

	//private async Task PerformValidateSubMenuItem(InputMethod inputMethod)
	//{


	//	Button button1 = null;
	//	IList<MenuFlyoutItemBase> items = null;
	//	MenuFlyoutSubItem subItem = null;

	//	Canvas rootPanel = await SetupRootPanelForSubMenuTest();
	//	MenuFlyout menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

	//	await RunOnUIThread(() =>
	//	   {
	//		   button1 = (Button)(rootPanel.FindName("button1"));
	//	   });

	//	await ShowMenuFlyout(menuFlyout, button1, -50, 50);

	//	await RunOnUIThread(() =>

	//	   {
	//		   items = menuFlyout.Items;
	//	   });

	//	subItem = await GetSubItem(items);

	//	switch (inputMethod)
	//	{
	//		case InputMethod.Touch:
	//			await TapSubMenuItem(subItem);
	//			break;
	//		case InputMethod.Mouse:
	//			MoveToSubMenuItem(subItem);
	//			break;
	//		case InputMethod.Keyboard:
	//			NavigateSubMenu(menuFlyout, button1, InputDevice.Keyboard);
	//			break;
	//		case InputMethod.Gamepad:
	//			NavigateSubMenu(menuFlyout, button1, InputDevice.Gamepad);
	//			break;
	//	}

	//	if (inputMethod == InputMethod.Mouse)
	//	{
	//		var presenter = GetCurrentPresenter();

	//		// Mouse over on the sub menu presenter and out of sub menu
	//		Private.Infrastructure.TestServices.InputHelper.MoveMouse(presenter);
	//		Private.Infrastructure.TestServices.InputHelper.MoveMouse(Point(0, 400));

	//		// Mouse over on the main menu presenter and out of main menu flyout to close it
	//		Private.Infrastructure.TestServices.InputHelper.MoveMouse(subItem);
	//		Private.Infrastructure.TestServices.InputHelper.MoveMouse(Point(0, 400));
	//	}

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//void PerformValidateTraverseMenuFlyoutItems(InputMethod inputMethod, boolean goDownFirst)
	//{


	//	Button button = null;
	//	MenuFlyout menuFlyout = CreateMenuFlyout();
	//	IList < MenuFlyoutItemBase> items = null;
	//	uint itemsSize = 0;

	//	var loadedEvent = new Event();
	//	var gotFocusEvent = new Event();
	//	var loadedRegistration = CreateSafeEventRegistration(Grid, Loaded);
	//	var buttonGotFocusRegistration = CreateSafeEventRegistration(Button, GotFocus);

	//	RoutedEventHandler ^ gotFocusHandler = null;
	//	vector < SafeEventRegistrationType(Control, GotFocus) > gotFocusRegistrations;

	//	// When a MenuFlyout is opened, the first focusable element will receive focus. So regardless of which direction we attempt to traverse the list
	//	// the fist focused element will be MFI1.
	//	// For list of size n, we keep going in one direction for n+1 times and then we go in the opposite direction for n+1 times.
	//	// If we're using the keyboard, which allows wrapping, then we'll see 2*(n+1) focus changes, since we'll be wrapping along the way.
	//	// On the other hand, if we're using gamepad or remote, wrapping is disabled.  In this circumstance,
	//	// if we start by traversing down this will mean we transition focus twice, and so the expectedFocusSequence with have three tags,
	//	// whereas if we start by trying to navigate up, we will see no change in focus because the first element will already have focus
	//	// and menu flyout does not allow focus looping with those input methods. For this reason, you will see only two tags in that expected focus sequence.
	//	// This is because the number of focusable items is 2 (MFI1 - MenuFlyoutItem1, MFSI1 - MenuFlyoutSubItem1) whereas the
	//	// other 2 items in this MenuFlyout are, a disabled MenuFlyoutItem and a MenuFlyoutSeparator which do not get focus.
	//	string expectedFocusSequence =
	//		inputMethod == InputMethod.Keyboard ?
	//			"[MFI1][MFSI1][MFI1][MFSI1][MFI1][MFSI1][MFI1][MFSI1][MFI1][MFSI1][MFI1]" :
	//			goDownFirst ? "[MFI1][MFSI1][MFI1]" : "[MFI1][MFSI1]";
	//	string focusSequence = "";

	// await RunOnUIThread(() =>

	//	{
	//		var rootPanel = Grid> (XamlReader.Load(
	//			LR"(<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	//					< Button Content = "Initial focus button" />

	//					< Button x: Name = "button" Content = "Button" Width = "100" Height = "50" />

	//				</ Grid >)"));

	//		TestServices.WindowHelper.WindowContent = rootPanel;

	//		button = Button> (rootPanel.FindName("button"));
	//		button.Flyout = menuFlyout;

	//		items = menuFlyout.Items;
	//		itemsSize = Items.Count;

	//		gotFocusHandler = new RoutedEventHandler([&](object sender, RoutedEventArgs ^ args)

	//		{
	//			focusSequence += "[" + FrameworkElement^>(sender).Tag + "]";
	//		});

	//		for (uint i = 0; i < itemsSize; i++)
	//		{
	//			var item = Control ^> (items[i]);
	//			var gotFocusRegistration = CreateSafeEventRegistration(Control, GotFocus);
	//			gotFocusRegistration.Attach(item, gotFocusHandler);
	//			gotFocusRegistrations.push_back(move(gotFocusRegistration));
	//		}

	//		loadedRegistration.Attach(rootPanel, [&]() { loadedEvent.Set(); });
	//		buttonGotFocusRegistration.Attach(button, [&]() { gotFocusEvent.Set(); });

	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});

	//	await loadedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		button.Focus(FocusState.Keyboard);
	//	});

	//	gotFocusEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	InputDevice inputDevice = InputDevice.Gamepad;
	//	switch (inputMethod)
	//	{
	//		case InputMethod.Gamepad:
	//			inputDevice = InputDevice.Gamepad;
	//			break;
	//		case InputMethod.Keyboard:
	//			inputDevice = InputDevice.Keyboard;
	//			break;
	//	}

	//	CommonInputHelper.Accept(inputDevice);

	//	// Go in one direction, for the length of the list + 1.
	//	for (uint i = 0; i < itemsSize + 1; i++)
	//	{
	//		if (goDownFirst)
	//		{
	//			CommonInputHelper.Down(inputDevice);
	//		}
	//		else
	//		{
	//			CommonInputHelper.Up(inputDevice);
	//		}
	//		await TestServices.WindowHelper.WaitForIdle();
	//	}

	//	// Go in the opposite direction, for the length of the list + 1.
	//	for (uint i = 0; i < itemsSize + 1; i++)
	//	{
	//		if (goDownFirst)
	//		{
	//			CommonInputHelper.Up(inputDevice);
	//		}
	//		else
	//		{
	//			CommonInputHelper.Down(inputDevice);
	//		}
	//		await TestServices.WindowHelper.WaitForIdle();
	//	}

	//	LOG_OUTPUT("Expected focus sequence: %s", expectedFocusSequence.Data());
	//	LOG_OUTPUT("Actual focus sequence: %s", focusSequence.Data());
	//	VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);

	//	CommonInputHelper.Cancel(inputDevice);
	//}

	private async Task<Canvas> SetupRootPanelForSubMenuTest()
	{
		Canvas rootPanel = null;

		await RunOnUIThread(() =>
		{
			rootPanel = (Canvas)XamlReader.Load(
				"<Canvas Background='RoyalBlue' " +
				" xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <Button x:Name='button1' Content='Button' Width='100' Height='50' Canvas.Left='50' Canvas.Top='50' />" +
				"</Canvas>");

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		return rootPanel;
	}

	private async Task TapSubMenuItem(MenuFlyoutSubItem subItem)
	{
		MenuFlyoutPresenter menuFlyoutPresenter = null;
		var lostFocusEvent = new Event();
		var lostFocusRegistration = CreateSafeEventRegistration<MenuFlyoutPresenter, RoutedEventHandler>(nameof(MenuFlyoutPresenter.LostFocus));

		await RunOnUIThread(async () =>
		{
			menuFlyoutPresenter = await GetCurrentPresenter();

			lostFocusRegistration.Attach(
				menuFlyoutPresenter,
				(s, e) =>
				{
					lostFocusEvent.Set();
				});
		});

		TestServices.InputHelper.Tap(subItem);

		await lostFocusEvent.WaitForDefault();

		await TestServices.WindowHelper.WaitForIdle();
	}

	//// Navigate in and out of submenu using both directional and Space/Escape keys.
	//// Also test Gamepad functionality (using the internal Gamepad . kb mapping).
	//void NavigateSubMenu(
	//	MenuFlyout menuFlyout,
	//	Button^ button,
	//	InputDevice device
	//	)
	//{
	//	MenuFlyoutPresenter menuFlyoutPresenter = null;

	//	var lostFocusEvent = new Event();
	//	var lostFocusRegistration = CreateSafeEventRegistration<MenuFlyoutPresenter, RoutedEventHandler>(nameof(MenuFlyoutPresenter.LostFocus))

	//	var menuFlyoutClosedEvent = new Event();
	//	var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutPresenter = GetCurrentPresenter();

	//		lostFocusRegistration.Attach(
	//			menuFlyoutPresenter,
	//			new RoutedEventHandler(
	//			[lostFocusEvent](object sender, IRoutedEventArgs ^)

	//		{
	//			lostFocusEvent.Set();
	//		}));

	//		closedRegistration.Attach(
	//			menuFlyout,
	//			new EventHandler<object> (
	//			[menuFlyoutClosedEvent](object, object)

	//		{
	//			menuFlyoutClosedEvent.Set();
	//		}));

	//	});

	//	// Go down four times to give a focus to the submenu item
	//	// from the first menu item.
	//	for (int i = 0; i < 4; i++)
	//	{
	//		CommonInputHelper.Down(device);
	//	}

	//	// Expand/collapse the submenu using right and left.
	//	CommonInputHelper.Right(device);
	//	CommonInputHelper.Down(device);
	//	CommonInputHelper.Left(device);

	//	await lostFocusEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Expand/collapse the submenu using accept and cancel.
	//	CommonInputHelper.Accept(device);
	//	CommonInputHelper.Down(device);
	//	CommonInputHelper.Cancel(device);

	//	await lostFocusEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Close the top-level menu (which will close the MenuFlyout itself)
	//	CommonInputHelper.Cancel(device);

	//	await menuFlyoutClosedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Opened flyout expected by caller
	//	await ShowMenuFlyout(menuFlyout, button, -50, 50);
	//}

	//void MoveToSubMenuItem(MenuFlyoutSubItem subItem)
	//{
	//	MenuFlyoutPresenter menuFlyoutPresenter = null;

	//	var lostFocusEvent = new Event();
	//	var lostFocusRegistration = CreateSafeEventRegistration<MenuFlyoutPresenter, RoutedEventHandler>(nameof(MenuFlyoutPresenter.LostFocus))

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutPresenter = GetCurrentPresenter();

	//		lostFocusRegistration.Attach(
	//			menuFlyoutPresenter,
	//			new RoutedEventHandler(
	//			[lostFocusEvent](object sender, IRoutedEventArgs ^)

	//		{
	//			lostFocusEvent.Set();
	//		}));
	//	});

	//	Private.Infrastructure.TestServices.InputHelper.MoveMouse(subItem);
	//	Private.Infrastructure.TestServices.InputHelper.LeftMouseClick(subItem);

	//	await TestServices.WindowHelper.WaitForIdle();

	//	await lostFocusEvent.WaitForDefault();

	//	await TestServices.WindowHelper.WaitForIdle();
	//}

	private async Task<MenuFlyoutSubItem> GetSubItem(IList<MenuFlyoutItemBase> items)
	{
		MenuFlyoutSubItem subItem = null;

		await RunOnUIThread(async () =>
		{
			subItem = await GetSubItem(items, items.Count - 1);
		});

		return subItem;
	}

	private async Task<MenuFlyoutSubItem> GetSubItem(IList<MenuFlyoutItemBase> items, int index)
	{
		MenuFlyoutSubItem subItem = null;

		await RunOnUIThread(() =>
		{
			subItem = (MenuFlyoutSubItem)(items[index]);
		});

		return subItem;
	}

	private async Task<MenuFlyoutPresenter> GetCurrentPresenter()
	{
		MenuFlyoutPresenter menuFlyoutPresenter = null;

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot);

			var popup = popups[0];
			menuFlyoutPresenter = (MenuFlyoutPresenter)(popup.Child);
		});

		return menuFlyoutPresenter;
	}

	private async Task ShowMenuFlyout(MenuFlyout menuFlyout, UIElement relativeTo, float horizontalOffset, float verticalOffset, bool forceTapAsPreviousInputMessage = true)
	{
		var openedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));

		if (forceTapAsPreviousInputMessage)
		{
			XamlRoot xamlRoot = null;
			await RunOnUIThread(() =>
			{
				xamlRoot = TestServices.WindowHelper.WindowContent.XamlRoot;
			});

			// Inject a tap. MenuFlyout looks different depending on how it was opened (mouse gives narrower padding than touch). We're
			// opening a flyout with ShowAt, which just grabs the last input device type and uses that. Set it explicitly to tap so that
			// the previous test doesn't mess up the state for this test. Use a test hook for this - tapping at arbitrary places can mess
			// up focus and flyout state.

			await TestServices.WindowHelper.SetLastInputMethod(InputDeviceType.Touch, xamlRoot);
		}

		await RunOnUIThread(() =>
		{
			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				openedEvent.Set();
			});

			menuFlyout.XamlRoot = TestServices.WindowHelper.WindowContent.XamlRoot;
			menuFlyout.ShowAt(relativeTo, new Point(horizontalOffset, verticalOffset));
		});

		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	//[TestMethod] public async Task ValidateUIElementTree()
	//{

	//	TestServices.WindowHelper.SetWindowSizeOverride(Size(800, 730));

	//	var validationRules = new Platform.String(
	//		LR"(<?xml version='1.0' encoding='UTF-8'?>
	//			< Rules >

	//				< Rule Applicability = '//Element' Inclusion = 'Blacklist' >

	//					< Property Name = 'FocusState' />

	//					< Property Name = 'IsPressed' />

	//				</ Rule >

	//			</ Rules >)");


	//	MenuFlyout menuFlyout;
	//	MenuFlyoutItem restMenuFlyoutItem;
	//	MenuFlyoutItem restMenuFlyoutItemWithKeyboardAccelerator;
	//	MenuFlyoutItem pointerOverMenuFlyoutItem;
	//	MenuFlyoutItem pointerOverMenuFlyoutItemWithKeyboardAccelerator;
	//	MenuFlyoutItem pressedMenuFlyoutItem;
	//	MenuFlyoutItem pressedMenuFlyoutItemWithKeyboardAccelerator;
	//	MenuFlyoutItem disabledMenuFlyoutItem;
	//	MenuFlyoutItem disabledMenuFlyoutItemWithKeyboardAccelerator;
	//	MenuFlyoutItem focusedMenuFlyoutItem;

	//	MenuFlyoutSeparator ^ firstMenuFlyoutSeparator;

	//	ToggleMenuFlyoutItem restUncheckedToggleMenuFlyoutItem;
	//	ToggleMenuFlyoutItem restUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator;
	//	ToggleMenuFlyoutItem pointerOverUncheckedToggleMenuFlyoutItem;
	//	ToggleMenuFlyoutItem pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator;
	//	ToggleMenuFlyoutItem pressedUncheckedToggleMenuFlyoutItem;
	//	ToggleMenuFlyoutItem pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator;
	//	ToggleMenuFlyoutItem disabledUncheckedToggleMenuFlyoutItem;
	//	ToggleMenuFlyoutItem disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator;
	//	ToggleMenuFlyoutItem focusedUncheckedToggleMenuFlyoutItem;

	//	MenuFlyoutSeparator ^ secondMenuFlyoutSeparator;

	//	MenuFlyoutSubItem restMenuFlyoutSubItem;
	//	MenuFlyoutSubItem pointerOverMenuFlyoutSubItem;
	//	MenuFlyoutSubItem pressedMenuFlyoutSubItem;
	//	MenuFlyoutSubItem disabledMenuFlyoutSubItem;
	//	MenuFlyoutSubItem focusedMenuFlyoutSubItem;

	//	MenuFlyoutPresenter thirdMenuFlyoutPresenter;

	//	ToggleMenuFlyoutItem restCheckedToggleMenuFlyoutItem;
	//	ToggleMenuFlyoutItem restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator;
	//	ToggleMenuFlyoutItem pointerOverCheckedToggleMenuFlyoutItem;
	//	ToggleMenuFlyoutItem pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator;
	//	ToggleMenuFlyoutItem pressedCheckedToggleMenuFlyoutItem;
	//	ToggleMenuFlyoutItem pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator;
	//	ToggleMenuFlyoutItem disabledCheckedToggleMenuFlyoutItem;
	//	ToggleMenuFlyoutItem disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator;
	//	ToggleMenuFlyoutItem focusedCheckedToggleMenuFlyoutItem;

	//	StackPanel ^ rootPanel = null;
	//	Button rootButton = null;

	// await RunOnUIThread(() =>

	//	{
	//		rootPanel = new StackPanel();

	//		menuFlyout = new MenuFlyout();
	//		menuFlyout.LightDismissOverlayMode = LightDismissOverlayMode.Off;

	//		restMenuFlyoutItem = new MenuFlyoutItem();
	//		restMenuFlyoutItem.Text = "Rest MenuFlyoutItem";
	//		menuFlyout.Items.Add(restMenuFlyoutItem);

	//		restMenuFlyoutItemWithKeyboardAccelerator = new MenuFlyoutItem();
	//		restMenuFlyoutItemWithKeyboardAccelerator.Text = "Rest MenuFlyoutItem with keyboard accelerator";
	//		restMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.A, Windows.System.VirtualKeyModifiers.None));
	//		menuFlyout.Items.Add(restMenuFlyoutItemWithKeyboardAccelerator);

	//		pointerOverMenuFlyoutItem = new MenuFlyoutItem();
	//		pointerOverMenuFlyoutItem.Text = "Pointer Over MenuFlyoutItem";
	//		menuFlyout.Items.Add(pointerOverMenuFlyoutItem);

	//		pointerOverMenuFlyoutItemWithKeyboardAccelerator = new MenuFlyoutItem();
	//		pointerOverMenuFlyoutItemWithKeyboardAccelerator.Text = "Pointer Over MenuFlyoutItem with keyboard accelerator";
	//		pointerOverMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.S, Windows.System.VirtualKeyModifiers.Control));
	//		menuFlyout.Items.Add(pointerOverMenuFlyoutItemWithKeyboardAccelerator);

	//		pressedMenuFlyoutItem = new MenuFlyoutItem();
	//		pressedMenuFlyoutItem.Text = "Pressed MenuFlyoutItem";
	//		menuFlyout.Items.Add(pressedMenuFlyoutItem);

	//		pressedMenuFlyoutItemWithKeyboardAccelerator = new MenuFlyoutItem();
	//		pressedMenuFlyoutItemWithKeyboardAccelerator.Text = "Pressed MenuFlyoutItem with keyboard accelerator";
	//		pressedMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.D, Windows.System.VirtualKeyModifiers.Shift));
	//		menuFlyout.Items.Add(pressedMenuFlyoutItemWithKeyboardAccelerator);

	//		disabledMenuFlyoutItem = new MenuFlyoutItem();
	//		disabledMenuFlyoutItem.Text = "Disabled MenuFlyoutItem";
	//		disabledMenuFlyoutItem.IsEnabled = false;
	//		menuFlyout.Items.Add(disabledMenuFlyoutItem);

	//		disabledMenuFlyoutItemWithKeyboardAccelerator = new MenuFlyoutItem();
	//		disabledMenuFlyoutItemWithKeyboardAccelerator.Text = "Disabled MenuFlyoutItem with keyboard accelerator";
	//		disabledMenuFlyoutItemWithKeyboardAccelerator.IsEnabled = false;
	//		disabledMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.F, Windows.System.VirtualKeyModifiers.Menu | Windows.System.VirtualKeyModifiers.Windows));
	//		menuFlyout.Items.Add(disabledMenuFlyoutItemWithKeyboardAccelerator);

	//		focusedMenuFlyoutItem = new MenuFlyoutItem();
	//		focusedMenuFlyoutItem.Text = "Focused MenuFlyoutItem";
	//		menuFlyout.Items.Add(focusedMenuFlyoutItem);

	//		firstMenuFlyoutSeparator = new MenuFlyoutSeparator();
	//		menuFlyout.Items.Add(firstMenuFlyoutSeparator);

	//		restMenuFlyoutSubItem = new MenuFlyoutSubItem();
	//		restMenuFlyoutSubItem.Text = "Rest MenuFlyoutSubItem";
	//		menuFlyout.Items.Add(restMenuFlyoutSubItem);

	//		pointerOverMenuFlyoutSubItem = new MenuFlyoutSubItem();
	//		pointerOverMenuFlyoutSubItem.Text = "Pointer Over MenuFlyoutSubItem";
	//		menuFlyout.Items.Add(pointerOverMenuFlyoutSubItem);

	//		pressedMenuFlyoutSubItem = new MenuFlyoutSubItem();
	//		pressedMenuFlyoutSubItem.Text = "Pressed MenuFlyoutSubItem";
	//		menuFlyout.Items.Add(pressedMenuFlyoutSubItem);

	//		disabledMenuFlyoutSubItem = new MenuFlyoutSubItem();
	//		disabledMenuFlyoutSubItem.Text = "Disabled MenuFlyoutSubItem";
	//		disabledMenuFlyoutSubItem.IsEnabled = false;
	//		menuFlyout.Items.Add(disabledMenuFlyoutSubItem);

	//		focusedMenuFlyoutSubItem = new MenuFlyoutSubItem();
	//		focusedMenuFlyoutSubItem.Text = "Focused MenuFlyoutSubItem";
	//		menuFlyout.Items.Add(focusedMenuFlyoutSubItem);

	//		secondMenuFlyoutSeparator = new MenuFlyoutSeparator();
	//		menuFlyout.Items.Add(secondMenuFlyoutSeparator);

	//		restUncheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
	//		restUncheckedToggleMenuFlyoutItem.Text = "Rest Unchecked ToggleMenuFlyoutItem";
	//		menuFlyout.Items.Add(restUncheckedToggleMenuFlyoutItem);

	//		restUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
	//		restUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Rest Unchecked ToggleMenuFlyoutItem with keyboard accelerator";
	//		restUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.A, Windows.System.VirtualKeyModifiers.None));
	//		menuFlyout.Items.Add(restUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

	//		pointerOverUncheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
	//		pointerOverUncheckedToggleMenuFlyoutItem.Text = "Pointer Over Unchecked ToggleMenuFlyoutItem";
	//		menuFlyout.Items.Add(pointerOverUncheckedToggleMenuFlyoutItem);

	//		pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
	//		pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Pointer Over Unchecked ToggleMenuFlyoutItem with keyboard accelerator";
	//		pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.S, Windows.System.VirtualKeyModifiers.Control));
	//		menuFlyout.Items.Add(pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

	//		pressedUncheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
	//		pressedUncheckedToggleMenuFlyoutItem.Text = "Pressed Unchecked ToggleMenuFlyoutItem";
	//		menuFlyout.Items.Add(pressedUncheckedToggleMenuFlyoutItem);

	//		pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
	//		pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Pressed Unchecked ToggleMenuFlyoutItem with keyboard accelerator";
	//		pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.D, Windows.System.VirtualKeyModifiers.Shift));
	//		menuFlyout.Items.Add(pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

	//		disabledUncheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
	//		disabledUncheckedToggleMenuFlyoutItem.Text = "Disabled Unchecked ToggleMenuFlyoutItem";
	//		disabledUncheckedToggleMenuFlyoutItem.IsEnabled = false;
	//		menuFlyout.Items.Add(disabledUncheckedToggleMenuFlyoutItem);

	//		disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
	//		disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Disabled Unchecked ToggleMenuFlyoutItem with keyboard accelerator";
	//		disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsEnabled = false;
	//		disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.F, Windows.System.VirtualKeyModifiers.Menu | Windows.System.VirtualKeyModifiers.Windows));
	//		menuFlyout.Items.Add(disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

	//		focusedUncheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
	//		focusedUncheckedToggleMenuFlyoutItem.Text = "Focused Unchecked ToggleMenuFlyoutItem";
	//		menuFlyout.Items.Add(focusedUncheckedToggleMenuFlyoutItem);

	//		restCheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
	//		restCheckedToggleMenuFlyoutItem.Text = "Rest Checked ToggleMenuFlyoutItem";
	//		restCheckedToggleMenuFlyoutItem.IsChecked = true;
	//		restMenuFlyoutSubItem.Items.Add(restCheckedToggleMenuFlyoutItem);

	//		restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
	//		restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Rest Checked ToggleMenuFlyoutItem with keyboard accelerator";
	//		restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsChecked = true;
	//		restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.A, Windows.System.VirtualKeyModifiers.None));
	//		menuFlyout.Items.Add(restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

	//		pointerOverCheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
	//		pointerOverCheckedToggleMenuFlyoutItem.Text = "Pointer Over Checked ToggleMenuFlyoutItem";
	//		pointerOverCheckedToggleMenuFlyoutItem.IsChecked = true;
	//		restMenuFlyoutSubItem.Items.Add(pointerOverCheckedToggleMenuFlyoutItem);

	//		pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
	//		pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Pointer Over Checked ToggleMenuFlyoutItem with keyboard accelerator";
	//		pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsChecked = true;
	//		pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.S, Windows.System.VirtualKeyModifiers.Control));
	//		menuFlyout.Items.Add(pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

	//		pressedCheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
	//		pressedCheckedToggleMenuFlyoutItem.Text = "Pressed Checked ToggleMenuFlyoutItem";
	//		pressedCheckedToggleMenuFlyoutItem.IsChecked = true;
	//		restMenuFlyoutSubItem.Items.Add(pressedCheckedToggleMenuFlyoutItem);

	//		pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
	//		pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Pressed Checked ToggleMenuFlyoutItem with keyboard accelerator";
	//		pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsChecked = true;
	//		pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.D, Windows.System.VirtualKeyModifiers.Shift));
	//		menuFlyout.Items.Add(pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

	//		disabledCheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
	//		disabledCheckedToggleMenuFlyoutItem.Text = "Disabled Checked ToggleMenuFlyoutItem";
	//		disabledCheckedToggleMenuFlyoutItem.IsChecked = true;
	//		disabledCheckedToggleMenuFlyoutItem.IsEnabled = false;
	//		restMenuFlyoutSubItem.Items.Add(disabledCheckedToggleMenuFlyoutItem);

	//		disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
	//		disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Disabled Checked ToggleMenuFlyoutItem with keyboard accelerator";
	//		disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsChecked = true;
	//		disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsEnabled = false;
	//		disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.F, Windows.System.VirtualKeyModifiers.Menu | Windows.System.VirtualKeyModifiers.Windows));
	//		menuFlyout.Items.Add(disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

	//		focusedCheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
	//		focusedCheckedToggleMenuFlyoutItem.Text = "Focused Checked ToggleMenuFlyoutItem";
	//		focusedCheckedToggleMenuFlyoutItem.IsChecked = true;
	//		restMenuFlyoutSubItem.Items.Add(focusedCheckedToggleMenuFlyoutItem);

	//		rootButton = new Button();
	//		rootButton.Flyout = menuFlyout;
	//		rootPanel.Children.Add(rootButton);

	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var setupValidation = [&]()

	//		{
	//		await ShowMenuFlyout(menuFlyout, rootButton, 0, 0, true /* forceTapAsPreviousInputMessage */);
	//		await TapSubMenuItem(restMenuFlyoutSubItem);

	//		await RunOnUIThread(() =>

	//			{
	//			// MenuFlyoutItems
	//			VisualStateManager.GoToState(pointerOverMenuFlyoutItem, "PointerOver", false);
	//			VisualStateManager.GoToState(pointerOverMenuFlyoutItemWithKeyboardAccelerator, "PointerOver", false);
	//			VisualStateManager.GoToState(pressedMenuFlyoutItem, "Pressed", false);
	//			VisualStateManager.GoToState(pressedMenuFlyoutItemWithKeyboardAccelerator, "Pressed", false);
	//			VisualStateManager.GoToState(focusedMenuFlyoutItem, "Focused", false);

	//			// Unchecked ToggleMenuFlyoutItems
	//			VisualStateManager.GoToState(pointerOverUncheckedToggleMenuFlyoutItem, "PointerOver", false);
	//			VisualStateManager.GoToState(pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator, "PointerOver", false);
	//			VisualStateManager.GoToState(pressedUncheckedToggleMenuFlyoutItem, "Pressed", false);
	//			VisualStateManager.GoToState(pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator, "Pressed", false);
	//			VisualStateManager.GoToState(focusedUncheckedToggleMenuFlyoutItem, "Focused", false);

	//			// MenuFlyoutSubItems
	//			VisualStateManager.GoToState(pointerOverMenuFlyoutSubItem, "PointerOver", false);
	//			VisualStateManager.GoToState(pressedMenuFlyoutSubItem, "Pressed", false);
	//			VisualStateManager.GoToState(focusedMenuFlyoutSubItem, "Focused", false);

	//			// Checked ToggleMenuFlyoutItems
	//			VisualStateManager.GoToState(pointerOverCheckedToggleMenuFlyoutItem, "PointerOver", false);
	//			VisualStateManager.GoToState(pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator, "PointerOver", false);
	//			VisualStateManager.GoToState(pressedCheckedToggleMenuFlyoutItem, "Pressed", false);
	//			VisualStateManager.GoToState(pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator, "Pressed", false);
	//			VisualStateManager.GoToState(focusedCheckedToggleMenuFlyoutItem, "Focused", false);
	//		});
	//		await TestServices.WindowHelper.WaitForIdle();
	//	}
	//	;

	//	// Validate the Dark theme of controls.
	//	{
	//		setupValidation();
	//		if (TestServices.Utilities.IsOneCore)
	//		{
	//			TestServices.Utilities.VerifyUIElementTreeWithRulesInline("WindowlessPopup_Dark", validationRules);
	//		}
	//		else
	//		{
	//			TestServices.Utilities.VerifyUIElementTreeWithRulesInline("WindowedPopup_Dark", validationRules);
	//		}

	//		await RunOnUIThread(() =>

	//		{
	//			menuFlyout.Hide();
	//		});
	//		await TestServices.WindowHelper.WaitForIdle();
	//	}

	//	// Validate the light theme of controls.
	//	{
	//		await RunOnUIThread(() =>

	//		{
	//			rootPanel.RequestedTheme = ElementTheme.Light;
	//			rootPanel.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
	//		});
	//		await TestServices.WindowHelper.WaitForIdle();

	//		setupValidation();
	//		if (TestServices.Utilities.IsOneCore)
	//		{
	//			TestServices.Utilities.VerifyUIElementTreeWithRulesInline("WindowlessPopup_Light", validationRules);
	//		}
	//		else
	//		{
	//			TestServices.Utilities.VerifyUIElementTreeWithRulesInline("WindowedPopup_Light", validationRules);
	//		}

	//		await RunOnUIThread(() =>

	//		{
	//			menuFlyout.Hide();
	//		});
	//		await TestServices.WindowHelper.WaitForIdle();
	//	}

	//	// Validate the high-contrast theme of controls.
	//	{
	//		setupValidation();

	//		// This method will turn on high-contrast mode before it does the validation.
	//		if (TestServices.Utilities.IsOneCore)
	//		{
	//			ControlHelper.ValidateUIElementTreeForHighContrast("WindowlessPopup_HC", rootPanel, validationRules);
	//		}
	//		else
	//		{
	//			ControlHelper.ValidateUIElementTreeForHighContrast("WindowedPopup_HC", rootPanel, validationRules);
	//		}

	//		await RunOnUIThread(() =>

	//		{
	//			menuFlyout.Hide();
	//		});
	//		await TestServices.WindowHelper.WaitForIdle();
	//	}
	//}

	private async Task<MenuFlyout> CreateMenuFlyoutSubItemsFromXaml()
	{
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutItem>Menu item 1</MenuFlyoutItem>" +
				"    <MenuFlyoutItem>Menu item 2</MenuFlyoutItem>" +
				"    <MenuFlyoutSeparator/>" +
				"    <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 3</ToggleMenuFlyoutItem>" +
				"    <MenuFlyoutSeparator/>" +
				"    <MenuFlyoutSubItem x:Name='subItem1' Text='Menu sub item 4' />" +
				"    <MenuFlyoutSubItem x:Name='subItem2' Text='Menu sub item 5'>" +
				"        <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>" +
				"        <MenuFlyoutSeparator/>" +
				"        <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 2.3</ToggleMenuFlyoutItem>" +
				"        <MenuFlyoutSeparator/>" +
				"        <MenuFlyoutSubItem  x:Name='subItem3' Text='Menu sub item 2.4'>" +
				"            <MenuFlyoutItem>Menu item 3.1</MenuFlyoutItem>" +
				"            <MenuFlyoutItem>Menu item 3.2</MenuFlyoutItem>" +
				"            <MenuFlyoutSeparator/>" +
				"            <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 3.3</ToggleMenuFlyoutItem>" +
				"            <MenuFlyoutSeparator/>" +
				"        </MenuFlyoutSubItem>" +
				"    </MenuFlyoutSubItem>" +
				"</MenuFlyout>");
		});

		return menuFlyout;
	}

	//void GetMenuFlyoutItemsHorizontalPadding(IList<MenuFlyoutItemBase^>^ items, double &leftPadding, double &rightPadding)
	//{
	//	for (uint i = 0; i < Items.Count; i++)
	//	{
	//		MenuFlyoutSubItem menuFlyoutSubItem = (MenuFlyoutSubItem)(items[i]);
	//		ToggleMenuFlyoutItem toggleMenuFlyoutItem = (ToggleMenuFlyoutItem)(items[i]);
	//		MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)(items[i]);

	//		if (menuFlyoutSubItem != null)
	//		{
	//			Grid layoutRoot = Grid> (TreeHelper.GetVisualChildByName(menuFlyoutSubItem, "LayoutRoot"));
	//			leftPadding = layoutRoot.Padding.Left;
	//			rightPadding = layoutRoot.Padding.Right;
	//		}
	//		else if (toggleMenuFlyoutItem != null)
	//		{
	//			Grid layoutRoot = Grid> (TreeHelper.GetVisualChildByName(toggleMenuFlyoutItem, "LayoutRoot"));
	//			leftPadding = layoutRoot.Padding.Left;
	//			rightPadding = layoutRoot.Padding.Right;
	//		}
	//		else if (menuFlyoutItem != null)
	//		{
	//			Grid layoutRoot = Grid> (TreeHelper.GetVisualChildByName(menuFlyoutItem, "LayoutRoot"));
	//			leftPadding = layoutRoot.Padding.Left;
	//			rightPadding = layoutRoot.Padding.Right;
	//		}
	//	}
	//}

	//[TestMethod] public async Task VerifyMenuFlyoutItemsPadding(IList<MenuFlyoutItemBase^>^ items, Thickness expectedPadding)
	//{
	//	for (uint i = 0; i < Items.Count; i++)
	//	{
	//		MenuFlyoutSubItem menuFlyoutSubItem = (MenuFlyoutSubItem)(items[i]);
	//		ToggleMenuFlyoutItem toggleMenuFlyoutItem = (ToggleMenuFlyoutItem)(items[i]);
	//		MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)(items[i]);

	//		if (menuFlyoutSubItem != null)
	//		{
	//			Grid layoutRoot = Grid> (TreeHelper.GetVisualChildByName(menuFlyoutSubItem, "LayoutRoot"));

	//			LOG_OUTPUT("SubItem InnerBorder top=%f bottom=%f", layoutRoot.Padding.Top, layoutRoot.Padding.Bottom);

	//			VERIFY_ARE_EQUAL(expectedPadding, layoutRoot.Padding);

	//			VerifyMenuFlyoutItemsPadding(menuFlyoutSubItem.Items, expectedPadding);
	//		}
	//		else if (toggleMenuFlyoutItem != null)
	//		{
	//			Grid layoutRoot = Grid> (TreeHelper.GetVisualChildByName(toggleMenuFlyoutItem, "LayoutRoot"));

	//			LOG_OUTPUT("ToggleMenuItem InnerBorder top=%f bottom=%f", layoutRoot.Padding.Top, layoutRoot.Padding.Bottom);

	//			VERIFY_ARE_EQUAL(expectedPadding, layoutRoot.Padding);
	//		}
	//		else if (menuFlyoutItem != null)
	//		{
	//			Grid layoutRoot = Grid> (TreeHelper.GetVisualChildByName(menuFlyoutItem, "LayoutRoot"));

	//			LOG_OUTPUT("MenuItem InnerBorder top=%f bottom=%f", layoutRoot.Padding.Top, layoutRoot.Padding.Bottom);

	//			VERIFY_ARE_EQUAL(expectedPadding, layoutRoot.Padding);
	//		}
	//	}
	//}

	//[TestMethod] public async Task ValidateRTLSubMenuItemPosition()
	//{


	//	IList < MenuFlyoutItemBase> items = null;
	//	IList < MenuFlyoutItemBase> ^subItems = null;
	//	MenuFlyoutSubItem subItem = null;
	//	Rect windowBounds = default;
	//	Rect menuFlyoutBounds = default;
	//	Rect subMenu1Bounds = default;
	//	Rect subMenu2Bounds = default;

	//	Canvas rootPanel = await SetupRootPanelForSubMenuTest();
	//	MenuFlyout menuFlyout = null;

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout = MenuFlyout> (XamlReader.Load(
	//			"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >"

	//			"    <MenuFlyoutItem>item1</MenuFlyoutItem>"

	//			"    <MenuFlyoutSeparator/>"

	//			"    <MenuFlyoutSubItem x:Name='subItem1' Text='subitem1'>"

	//			"        <MenuFlyoutItem>item1</MenuFlyoutItem>"

	//			"        <MenuFlyoutSeparator/>"

	//			"        <MenuFlyoutSubItem  x:Name='subItem2' Text='subItem2' FlowDirection='RightToLeft'>"

	//			"            <MenuFlyoutItem>item1</MenuFlyoutItem>"

	//			"            <MenuFlyoutSeparator/>"

	//			"            <MenuFlyoutSubItem x:Name='subItem3' Text='subitem3' FlowDirection='RightToLeft'/>"

	//			"        </MenuFlyoutSubItem>"

	//			"    </MenuFlyoutSubItem>"

	//			"</MenuFlyout>"));

	//		windowBounds = TestServices.WindowHelper.WindowBounds;
	//		LOG_OUTPUT("Windows bounds left=%f top=%f width=%f height=%f", windowBounds.Left, windowBounds.Top, windowBounds.Width, windowBounds.Height);
	//	});

	//	await ShowMenuFlyout(menuFlyout, null, 50, 100);

	//	var presenter = GetCurrentPresenter();

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
	//		LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);
	//	});

	// await RunOnUIThread(() =>

	//	{
	//		items = menuFlyout.Items;
	//	});

	//	subItem = await GetSubItem(items);
	//	await TapSubMenuItem(subItem);

	//	presenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		subMenu1Bounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
	//		LOG_OUTPUT("SubMenuFlyout bounds left=%f top=%f width=%f height=%f", subMenu1Bounds.Left, subMenu1Bounds.Top, subMenu1Bounds.Width, subMenu1Bounds.Height);
	//	});

	//	VERIFY_IS_TRUE(menuFlyoutBounds.Left < subMenu1Bounds.Left);

	// await RunOnUIThread(() =>

	//	{
	//		subItems = subItem.Items;
	//	});

	//	var subItem2 = GetSubItem(subItems);
	//	await TapSubMenuItem(subItem2);

	//	presenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		subMenu2Bounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
	//		LOG_OUTPUT("SubMenuFlyout RTL bounds left=%f top=%f width=%f height=%f", subMenu2Bounds.Left, subMenu2Bounds.Top, subMenu2Bounds.Width, subMenu2Bounds.Height);
	//	});

	//	VERIFY_IS_TRUE(subMenu2Bounds.Left < subMenu1Bounds.Left);
	//	VERIFY_IS_TRUE(subMenu1Bounds.Left < subMenu2Bounds.Left + subMenu2Bounds.Width);

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateSubMenuItemProperties()
	//{


	//	// Bug 14794166: Leak in MenuFlyoutIntegrationTests.ValidateSubMenuItemProperties due to DataContext (796 bytes)
	//	TestServices.ErrorHandlingHelper.IgnoreLeaksForTest();

	//	Button button = null;
	//	MenuFlyout menuFlyout = null;
	//	IList < MenuFlyoutItemBase> items = null;

	//	var menuFlyoutOpenedEvent = new Event();
	//	var menuFlyoutClosedEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

	// await RunOnUIThread(() =>

	//	{
	//		var rootPanel = (Grid)(XamlReader.Load(
	//			"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' "

	//			"      x:Name='root' Background='SlateBlue' >"

	//			"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Center' "

	//			"          RequestedTheme='light' FlowDirection='LeftToRight' Language='En-UK' IsTextScaleFactorEnabled='true' >"

	//			"    <Button.Flyout> "

	//			"      <MenuFlyout Placement='Bottom'> "

	//			"        <MenuFlyoutItem Text='Item 1' /> "

	//			"        <MenuFlyoutItem Text='Item 2' /> "

	//			"        <MenuFlyoutSeparator Width='300' /> "

	//			"        <ToggleMenuFlyoutItem Text='Toggle Item3' IsChecked='True' /> "

	//			"        <MenuFlyoutSeparator Width='300' /> "

	//			"        <MenuFlyoutSubItem x:Name='subItem1' Text='Sub item 4'>"

	//			"          <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>"

	//			"          <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>"

	//			"          <MenuFlyoutSeparator/>"

	//			"          <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 2.3</ToggleMenuFlyoutItem>"

	//			"         </MenuFlyoutSubItem>"

	//			"      </MenuFlyout> "

	//			"    </Button.Flyout> "

	//			"  </Button> "

	//			"</Grid>"));

	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//		button = (Button)(rootPanel.FindName("button"));
	//		menuFlyout = (MenuFlyout)(button.Flyout);

	//		// Set the DataContext to button from the root panel
	//		button.DataContext = rootPanel;

	//		// Set the MenuFlyout presenter style
	//		wxaml_interop.TypeName type = wxaml_interop.TypeName();
	//		type.Name = "Microsoft.UI.Xaml.Controls.MenuFlyoutPresenter";
	//		type.Kind = wxaml_interop.TypeKind.Metadata;
	//		var style = new Style(type);
	//		style.Setters.Add(new Setter(MenuFlyoutPresenter.TagProperty, "presenter_style"));
	//		menuFlyout.MenuFlyoutPresenterStyle = style;

	//		openedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			menuFlyoutOpenedEvent.Set();
	//		}));

	//		closedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			menuFlyoutClosedEvent.Set();
	//		}));
	//	});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	TestServices.InputHelper.Tap(button);
	//	await menuFlyoutOpenedEvent.WaitForDefault();

	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		items = menuFlyout.Items;
	//	});

	//	var subItem = await GetSubItem(items);
	//	await TapSubMenuItem(subItem);

	//	var subPresenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		VERIFY_IS_TRUE(subPresenter.RequestedTheme == ElementTheme.Light);
	//		VERIFY_IS_TRUE(subPresenter.FlowDirection == FlowDirection.LeftToRight);
	//		VERIFY_IS_TRUE(subPresenter.Language == "En-UK");
	//		VERIFY_IS_TRUE(subPresenter.IsTextScaleFactorEnabled);

	//		VERIFY_IS_NOT_NULL(subPresenter.DataContext);

	//		var tag = subPresenter.GetValue(MenuFlyoutPresenter.TagProperty);
	//		VERIFY_IS_NOT_NULL(tag);
	//		VERIFY_ARE_EQUAL("presenter_style", tag.ToString());

	//	});

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout.Hide();
	//	});

	//	await menuFlyoutClosedEvent.WaitForDefault();
	//}

	//[TestMethod] public async Task ValidateThatLayoutTransitionsDoRun()
	//{


	//	Button button;
	//	IList < MenuFlyoutItemBase> items;
	//	var storyboardMonitor = new StoryboardMonitorWrapper();
	//	int startedStoryboardCount = 0;

	//	// Prepare the menu flyout
	// await RunOnUIThread(() =>

	//	{
	//		var grid = new Grid();
	//		button = new Button();
	//		button.Content = "ValidateThatLayoutTransitionsDoRun";
	//		button.Flyout = await CreateMenuFlyoutSubItemsFromXaml();
	//		items = MenuFlyout> (button.Flyout).Items;
	//		grid.Children.Add(button);
	//		TestServices.WindowHelper.WindowContent = grid;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Validate storboard count
	//	storyboardMonitor.AttachStartedHandler(
	//		[&](xaml_animation.Storyboard ^, UIElement ^ target)

	//	{
	//		if ((MenuFlyoutPresenter)(target))
	//		{
	//			++startedStoryboardCount;
	//		}
	//	});

	//	LOG_OUTPUT("Open the menu flyout");
	// await RunOnUIThread(() =>

	//	{
	//		button.Flyout.ShowAt(button);
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Windowed menus, which we have on desktop, don't have layout transitions.
	//	VERIFY_ARE_EQUAL(TestServices.Utilities.IsDesktop ? 0 : 1, startedStoryboardCount);

	//	LOG_OUTPUT("Open the sub menu");
	//	await TapSubMenuItem(GetSubItem(items));

	//	VERIFY_ARE_EQUAL(TestServices.Utilities.IsDesktop ? 0 : 2, startedStoryboardCount);

	//	LOG_OUTPUT("Close menu and sub menu");
	//	bool backButtonPressHandled = false;
	//	TestServices.Utilities.InjectBackButtonPress(&backButtonPressHandled);
	//	await TestServices.WindowHelper.WaitForIdle();
	//	VERIFY_ARE_EQUAL(TestServices.Utilities.IsDesktop ? 0 : 4, startedStoryboardCount);
	//}

	//[TestMethod] public async Task ValidateRightClickChaining()
	//{


	//	Button button1 = null;
	//	Button button2 = null;
	//	MenuFlyout menuFlyout = null;

	//	var menuFlyoutOpenedEvent = new Event();
	//	var menuFlyoutClosedEvent = new Event();
	//	var rightTappedEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));
	//	var rightTappedRegistration = CreateSafeEventRegistration(Button, RightTapped);

	// await RunOnUIThread(() =>

	//	{
	//		var rootPanel = (StackPanel ^)(XamlReader.Load(
	//			"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' "

	//			"      x:Name='root' Background='SlateBlue' Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left'> "

	//			"  <Button x:Name='button1' Content='button.righttapped' VerticalAlignment='Center' HorizontalAlignment='Left' FontSize='25' Padding='25,10' Margin='50'> "

	//			"  </Button> "

	//			"  <Button x:Name='button2' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Left' FontSize='25' Padding='25,10' Margin='50'> "

	//			"    <Button.Flyout> "

	//			"      <MenuFlyout Placement='Bottom'> "

	//			"        <MenuFlyoutItem FontSize='30' Text='SUPERMAN' Foreground='RoyalBlue' Width='300' /> "

	//			"        <MenuFlyoutSeparator Width='300' /> "

	//			"        <ToggleMenuFlyoutItem FontSize='30' Text='THE FLASH' Foreground='RoyalBlue' Width='300' IsChecked='False' /> "

	//			"      </MenuFlyout> "

	//			"    </Button.Flyout> "

	//			"  </Button> "

	//			"</StackPanel>"));

	//		VERIFY_IS_NOT_NULL(rootPanel);
	//		TestServices.WindowHelper.WindowContent = rootPanel;

	//		button1 = (Button)(rootPanel.FindName("button1"));
	//		VERIFY_IS_NOT_NULL(button1);
	//		button2 = (Button)(rootPanel.FindName("button2"));
	//		VERIFY_IS_NOT_NULL(button2);

	//		rightTappedRegistration.Attach(button1, new Input.RightTappedEventHandler([rightTappedEvent](object sender, RoutedEventArgs ^ e) {
	//			rightTappedEvent.Set();
	//		}));

	//		menuFlyout = (MenuFlyout)(button2.Flyout);
	//		VERIFY_IS_NOT_NULL(menuFlyout);

	//		openedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			LOG_OUTPUT("CanMenuFlyoutOpenClose: MenuFlyout Opened event is fired!");
	//			menuFlyoutOpenedEvent.Set();
	//		}));

	//		closedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			LOG_OUTPUT("CanMenuFlyoutOpenClose: MenuFlyout Closed event is fired!");
	//			menuFlyoutClosedEvent.Set();
	//		}));
	//	});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Button Tap operation to show the MenuFlyout.");
	//	TestServices.InputHelper.Tap(button2);
	//	await menuFlyoutOpenedEvent.WaitForDefault();

	//	// Inject right-click.
	//	TestServices.InputHelper.MoveMouse(button1);
	//	TestServices.InputHelper.MouseButtonDown(button1, 0, 0, MouseButton.Right);
	//	TestServices.InputHelper.MouseButtonUp(button1, 0, 0, MouseButton.Right);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Make sure that the right tap tiggered the flyout's light dismiss
	//	await menuFlyoutClosedEvent.WaitForDefault();

	//	// Make sure that the right tap gesture was chained through the MenuFlyout's light dismiss layer
	//	// and received by the next hit target - button1
	//	rightTappedEvent.WaitForDefault();
	//}


	//MenuFlyout CreateMenuFlyoutLongItemsFromXaml()
	//{
	//	MenuFlyout menuFlyout = null;

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout = MenuFlyout> (XamlReader.Load(
	//			"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >"

	//			"    <MenuFlyoutItem>Menu item 0</MenuFlyoutItem>"

	//			"    <MenuFlyoutSeparator/>"

	//			"    <MenuFlyoutSubItem x:Name='subItem1' Text='Menu sub item 1'>"

	//			"        <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>"

	//			"        <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>"

	//			"        <MenuFlyoutSeparator/>"

	//			"        <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 2.3</ToggleMenuFlyoutItem>"

	//			"        <MenuFlyoutSeparator/>"

	//			"        <MenuFlyoutSubItem  x:Name='subItem2' Text='Menu sub item 2.4'>"

	//			"            <MenuFlyoutItem>Menu item 3.1</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.2</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.3</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.4</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.5</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.6</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.7</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.8</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.9</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.10</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.11</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.12</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.13</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.14</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.15</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.16</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.17</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.18</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.19</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.20</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.21</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.22</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.23</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.24</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.25</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.26</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.27</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.28</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.29</MenuFlyoutItem>"

	//			"            <MenuFlyoutItem>Menu item 3.30</MenuFlyoutItem>"

	//			"        </MenuFlyoutSubItem>"

	//			"    </MenuFlyoutSubItem>"

	//			"    <MenuFlyoutSeparator/>"

	//			"    <MenuFlyoutItem>Menu item 2</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 3</MenuFlyoutItem>"

	//			"    <MenuFlyoutSeparator/>"

	//			"    <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 3</ToggleMenuFlyoutItem>"

	//			"    <MenuFlyoutSeparator/>"

	//			"    <MenuFlyoutItem>Menu item 4</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 5</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 6</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 7</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 8</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 9</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 10</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 11</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 12</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 13</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 14</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 15</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 16</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 17</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 18</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 19</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 20</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 21</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 22</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 23</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 24</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 25</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 26</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 27</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 28</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 29</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 30</MenuFlyoutItem>"

	//			"</MenuFlyout>"));
	//	});

	//	return menuFlyout;
	//}

	//[TestMethod] public async Task ValidateSubMenuItemWithLongItems()
	//{


	//	Rect subItemBounds = default;
	//	Rect subPresenterBounds = default;
	//	Button button1 = null;
	//	MenuFlyoutSubItem subItem = null;
	//	IList < MenuFlyoutItemBase> items = null;
	//	IList < MenuFlyoutItemBase> ^subItems = null;
	//	ScrollViewer ^ scrollViewer = null;

	//	Canvas rootPanel = await SetupRootPanelForSubMenuTest();
	//	MenuFlyout menuFlyout = CreateMenuFlyoutLongItemsFromXaml();

	// await RunOnUIThread(() =>

	//	{
	//		rootPanel.RequestedTheme = ElementTheme.Light;
	//		button1 = Button> (rootPanel.FindName("button1"));
	//	});

	//	// Set the mouse operation for showing the ScrollBar on the long menu items
	//	TestServices.InputHelper.LeftMouseClick(button1);

	//	// Show the long main MenuFlyout
	//	await ShowMenuFlyout(menuFlyout, button1, 200, 50);

	//	var subPresenter = GetCurrentPresenter();

	// await RunOnUIThread(() =>

	//	{
	//		scrollViewer = ScrollViewer ^> (TreeHelper.GetVisualChildByName(subPresenter, "MenuFlyoutPresenterScrollViewer"));
	//		LOG_OUTPUT("Scrollable Height=%f", scrollViewer.ScrollableHeight);
	//	});
	//	VERIFY_IS_TRUE(scrollViewer.ScrollableHeight > 0);

	// await RunOnUIThread(() =>

	//	{
	//		items = menuFlyout.Items;
	//		subItem = (MenuFlyoutSubItem)(items[2]);
	//	});

	//	MoveToSubMenuItem(subItem);

	//	subPresenter = GetCurrentPresenter();

	// await RunOnUIThread(() =>

	//	{
	//		subItemBounds = await ControlHelper.GetBounds((FrameworkElement)(subItem));
	//		LOG_OUTPUT("MenuFlyoutSubItem bounds left=%f top=%f width=%f height=%f", subItemBounds.Left, subItemBounds.Top, subItemBounds.Width, subItemBounds.Height);
	//	});

	//	// Move the mouse to the boundary of sub menu item then move to the sub presenter
	//	Private.Infrastructure.TestServices.InputHelper.MoveMouse(Point(subItemBounds.Left + subItemBounds.Width, subItemBounds.Top + subItemBounds.Height / 2));
	//	Private.Infrastructure.TestServices.InputHelper.MoveMouse(Point(subItemBounds.Left + subItemBounds.Width + 100, subItemBounds.Top + subItemBounds.Height / 2));
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Verify the sub presenter
	//	VERIFY_IS_TRUE(subPresenter == GetCurrentPresenter());

	//	// Get the sub items to invoke the second menu sub presenter
	// await RunOnUIThread(() =>

	//	{
	//		subItems = subItem.Items;
	//	});

	//	var subItem2 = GetSubItem(subItems);

	//	// Show the second menu sub presenter
	//	await TapSubMenuItem(subItem2);

	//	var subPresenter2 = GetCurrentPresenter();

	// await RunOnUIThread(() =>

	//	{
	//		subPresenterBounds = await ControlHelper.GetBounds((FrameworkElement)(subPresenter2));
	//		LOG_OUTPUT("Second SubPresenter bounds left=%f top=%f width=%f height=%f", subPresenterBounds.Left, subPresenterBounds.Top, subPresenterBounds.Width, subPresenterBounds.Height);
	//	});

	//	Private.Infrastructure.TestServices.InputHelper.MoveMouse(Point(subPresenterBounds.Left + subPresenterBounds.Width / 2, subPresenterBounds.Top + subPresenterBounds.Height / 2));
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		scrollViewer = ScrollViewer ^> (TreeHelper.GetVisualChildByName(subPresenter2, "MenuFlyoutPresenterScrollViewer"));
	//		LOG_OUTPUT("Scrollable Height=%f", scrollViewer.ScrollableHeight);
	//	});
	//	VERIFY_IS_TRUE(scrollViewer.ScrollableHeight > 0);

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//void OpenSubItemWithMouse(MenuFlyoutSubItem menuFlyoutSubItem)
	//{
	//	// Open the MenuFlyoutSubItem by moving the mouse on the first sub item
	//	TestServices.InputHelper.MoveMouse(menuFlyoutSubItem);
	//	// Wait for the sub menu to open. It opens after a delay - clicking and waiting for idle doesn't open it.
	//	// MSFT: 4815582 <MenuFlyout sub items don't expand on mouse click - they need to wait for the timeout> tracks this bug.
	//	TestServices.WindowHelper.SynchronouslyTickUIThread(60);
	//}

	//[TestMethod] public async Task ValidateOpenMultiSubMenuItemByMouse()
	//{


	//	Button button = null;
	//	MenuFlyout menuFlyout = null;
	//	MenuFlyoutSubItem menuFlyoutSubItem = null;
	//	Rect menuFlyoutSubItem1Bounds = default;
	//	Rect menuFlyoutSubItem2Bounds = default;

	//	var menuFlyoutOpenedEvent = new Event();
	//	var menuFlyoutClosedEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

	//	var menuFlyoutSubItemClosedEvent = new Event();
	//	var subItemClosedRegistration = CreateSafeEventRegistration(Popup, Closed);

	// await RunOnUIThread(() =>

	//	{
	//		var rootPanel = (Grid)(XamlReader.Load(
	//			"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' "

	//			"      x:Name='root' Background='SlateBlue' Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left'> "

	//			"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Left' FontSize='25' Padding='25,10' Margin='50'> "

	//			"    <Button.Flyout> "

	//			"      <MenuFlyout Placement='Bottom'> "

	//			"        <MenuFlyoutItem Text='Item 1' /> "

	//			"        <MenuFlyoutSubItem Text='Sub Item 1'> "

	//			"            <MenuFlyoutItem>Sub item 1.1</MenuFlyoutItem> "

	//			"            <MenuFlyoutItem>Sub item 1.2</MenuFlyoutItem> "

	//			"        </MenuFlyoutSubItem> "

	//			"        <MenuFlyoutSeparator Width='300' /> "

	//			"        <MenuFlyoutItem Text='Item 2' /> "

	//			"        <MenuFlyoutSubItem Text='Sub Item 2'> "

	//			"            <MenuFlyoutItem>Sub item 2.1</MenuFlyoutItem> "

	//			"            <MenuFlyoutItem>Sub item 2.2</MenuFlyoutItem> "

	//			"        </MenuFlyoutSubItem> "

	//			"        <MenuFlyoutSeparator Width='300' /> "

	//			"        <MenuFlyoutItem Text='Item 3' /> "

	//			"        <MenuFlyoutSubItem Text='Sub Item 3'> "

	//			"            <MenuFlyoutItem>Sub item 3.1</MenuFlyoutItem> "

	//			"            <MenuFlyoutItem>Sub item 3.2</MenuFlyoutItem> "

	//			"        </MenuFlyoutSubItem> "

	//			"      </MenuFlyout> "

	//			"    </Button.Flyout> "

	//			"  </Button> "

	//			"</Grid>"));

	//		TestServices.WindowHelper.WindowContent = rootPanel;

	//		button = (Button)(rootPanel.FindName("button"));
	//		menuFlyout = (MenuFlyout)(button.Flyout);
	//		menuFlyoutSubItem = (MenuFlyoutSubItem)(menuFlyout.Items[1]);

	//		openedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			menuFlyoutOpenedEvent.Set();
	//		}));

	//		closedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			menuFlyoutClosedEvent.Set();
	//		}));
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Open the MenuFlyout by tapping the button
	//	TestServices.InputHelper.Tap(button);
	//	await menuFlyoutOpenedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Open the MenuFlyoutSubItem by moving the mouse on the first sub item
	//	OpenSubItemWithMouse(menuFlyoutSubItem);

	//	var presenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutSubItem1Bounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
	//		LOG_OUTPUT("MenuFlyoutSubItem1 bounds left=%f top=%f width=%f height=%f", menuFlyoutSubItem1Bounds.Left, menuFlyoutSubItem1Bounds.Top, menuFlyoutSubItem1Bounds.Width, menuFlyoutSubItem1Bounds.Height);

	//		var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(
	//			button.XamlRoot);

	//		var popup = popups[0];
	//		subItemClosedRegistration.Attach(popup, new EventHandler<object> ([menuFlyoutSubItemClosedEvent](object, object)

	//		{
	//			menuFlyoutSubItemClosedEvent.Set();
	//		}));
	//	});

	//	// Move the mouse to the out of the MenuFlyout bounds
	//	Private.Infrastructure.TestServices.InputHelper.MoveMouse(Point(menuFlyoutSubItem1Bounds.Left + menuFlyoutSubItem1Bounds.Width / 2, 0));
	//	Private.Infrastructure.TestServices.InputHelper.MoveMouse(Point(menuFlyoutSubItem1Bounds.Left + menuFlyoutSubItem1Bounds.Width / 2, 1));
	//	menuFlyoutSubItemawait closedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		// Get the second sub item
	//		menuFlyoutSubItem = (MenuFlyoutSubItem)(menuFlyout.Items[4]);
	//	});

	//	// Move the mouse to the second sub menu item to close the previous sub item and open the second sub item
	//	OpenSubItemWithMouse(menuFlyoutSubItem);

	//	presenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutSubItem2Bounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
	//		LOG_OUTPUT("MenuFlyoutSubItem2 bounds left=%f top=%f width=%f height=%f", menuFlyoutSubItem2Bounds.Left, menuFlyoutSubItem2Bounds.Top, menuFlyoutSubItem2Bounds.Width, menuFlyoutSubItem2Bounds.Height);

	//		VERIFY_IS_TRUE(menuFlyoutSubItem1Bounds.Top < menuFlyoutSubItem2Bounds.Top);

	//		// Close the MenuFlyout
	//		menuFlyout.Hide();
	//	});

	//	await menuFlyoutClosedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();
	//}

	//[TestMethod] public async Task ValidateRequestedThemeOnPresenterTakesEffect()
	//{


	//	Button button = null;
	//	MenuFlyout menuFlyout = null;

	//	var menuFlyoutOpenedEvent = new Event();
	//	var menuFlyoutClosedEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

	// await RunOnUIThread(() =>

	//	{
	//		var rootPanel = (Grid)(XamlReader.Load(
	//			"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' "

	//			"      x:Name='root' Background='SlateBlue' Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left'> "

	//			"  <Grid.Resources> "

	//			"      <ResourceDictionary> "

	//			"          <ResourceDictionary.ThemeDictionaries> "

	//			"              <ResourceDictionary x:Key='Default'> "

	//			"                  <SolidColorBrush x:Key='CustomPresenterBackground' Color='Green' /> "

	//			"              </ResourceDictionary> "

	//			"              <ResourceDictionary x:Key='Light'> "

	//			"                  <SolidColorBrush x:Key='CustomPresenterBackground' Color='Blue' /> "

	//			"              </ResourceDictionary> "

	//			"          </ResourceDictionary.ThemeDictionaries> "

	//			"      </ResourceDictionary> "

	//			"  </Grid.Resources> "

	//			"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Left' FontSize='25' Padding='25,10' Margin='50' RequestedTheme='Dark'> "

	//			"    <Button.Flyout> "

	//			"      <MenuFlyout Placement='Bottom'> "

	//			"        <MenuFlyout.MenuFlyoutPresenterStyle> "

	//			"          <Style TargetType='MenuFlyoutPresenter'> "

	//			"            <Setter Property='Background' Value='{ThemeResource CustomPresenterBackground}' /> "

	//			"            <Setter Property='RequestedTheme' Value='Light' /> "

	//			"          </Style> "

	//			"        </MenuFlyout.MenuFlyoutPresenterStyle> "

	//			"        <MenuFlyoutItem Text='Item 1' /> "

	//			"        <MenuFlyoutItem Text='Item 2' /> "

	//			"        <MenuFlyoutItem Text='Item 3' /> "

	//			"      </MenuFlyout> "

	//			"    </Button.Flyout> "

	//			"  </Button> "

	//			"</Grid>"));

	//		TestServices.WindowHelper.WindowContent = rootPanel;

	//		button = (Button)(rootPanel.FindName("button"));
	//		menuFlyout = (MenuFlyout)(button.Flyout);

	//		openedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			menuFlyoutOpenedEvent.Set();
	//		}));

	//		closedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			menuFlyoutClosedEvent.Set();
	//		}));
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Open the MenuFlyout by tapping the button
	//	TestServices.InputHelper.Tap(button);
	//	await menuFlyoutOpenedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var presenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		var backgroundColor = SolidColorBrush ^> (presenter.Background).Color;

	//		VERIFY_ARE_EQUAL(0xFF, backgroundColor.A);
	//		VERIFY_ARE_EQUAL(0x00, backgroundColor.R);
	//		VERIFY_ARE_EQUAL(0x00, backgroundColor.G);
	//		VERIFY_ARE_EQUAL(0xFF, backgroundColor.B);
	//	});

	// await RunOnUIThread(() =>

	//	{
	//		// Close the MenuFlyout
	//		menuFlyout.Hide();
	//	});

	//	await menuFlyoutClosedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();
	//}

	//[TestMethod] public async Task ValidateCanChangeRequestedThemeOnPresenterOwner()
	//{


	//	Button button = null;
	//	MenuFlyout menuFlyout = null;

	//	var menuFlyoutOpenedEvent = new Event();
	//	var menuFlyoutClosedEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

	// await RunOnUIThread(() =>

	//	{
	//		var rootPanel = Grid> (XamlReader.Load(
	//			"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' "

	//			"      x:Name='root' Background='SlateBlue' Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left'> "

	//			"  <Grid.Resources> "

	//			"      <ResourceDictionary> "

	//			"          <ResourceDictionary.ThemeDictionaries> "

	//			"              <ResourceDictionary x:Key='Default'> "

	//			"                  <SolidColorBrush x:Key='CustomPresenterBackground' Color='Green' /> "

	//			"              </ResourceDictionary> "

	//			"              <ResourceDictionary x:Key='Light'> "

	//			"                  <SolidColorBrush x:Key='CustomPresenterBackground' Color='Blue' /> "

	//			"              </ResourceDictionary> "

	//			"          </ResourceDictionary.ThemeDictionaries> "

	//			"      </ResourceDictionary> "

	//			"  </Grid.Resources> "

	//			"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Left' FontSize='25' Padding='25,10' Margin='50' RequestedTheme='Dark'> "

	//			"    <Button.Flyout> "

	//			"      <MenuFlyout Placement='Bottom'> "

	//			"        <MenuFlyout.MenuFlyoutPresenterStyle> "

	//			"          <Style TargetType='MenuFlyoutPresenter'> "

	//			"            <Setter Property='Background' Value='{ThemeResource CustomPresenterBackground}' /> "

	//			"          </Style> "

	//			"        </MenuFlyout.MenuFlyoutPresenterStyle> "

	//			"        <MenuFlyoutItem Text='Item 1' /> "

	//			"        <MenuFlyoutItem Text='Item 2' /> "

	//			"        <MenuFlyoutItem Text='Item 3' /> "

	//			"      </MenuFlyout> "

	//			"    </Button.Flyout> "

	//			"  </Button> "

	//			"</Grid>"));

	//		TestServices.WindowHelper.WindowContent = rootPanel;

	//		button = Button> (rootPanel.FindName("button"));
	//		menuFlyout = MenuFlyout> (button.Flyout);

	//		openedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			menuFlyoutOpenedEvent.Set();
	//		}));

	//		closedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			menuFlyoutClosedEvent.Set();
	//		}));
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Open the MenuFlyout by tapping the button
	//	TestServices.InputHelper.Tap(button);
	//	await menuFlyoutOpenedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var presenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		var backgroundColor = SolidColorBrush ^> (presenter.Background).Color;

	//		VERIFY_ARE_EQUAL(0xFF, backgroundColor.A);
	//		VERIFY_ARE_EQUAL(0x00, backgroundColor.R);
	//		VERIFY_ARE_EQUAL(0x80, backgroundColor.G);
	//		VERIFY_ARE_EQUAL(0x00, backgroundColor.B);
	//	});

	// await RunOnUIThread(() =>

	//	{
	//		// Close the MenuFlyout
	//		menuFlyout.Hide();
	//	});

	//	await menuFlyoutClosedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		button.RequestedTheme = ElementTheme.Light;
	//	});

	//	// Open the MenuFlyout by tapping the button
	//	TestServices.InputHelper.Tap(button);
	//	await menuFlyoutOpenedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	presenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		var backgroundColor = SolidColorBrush ^> (presenter.Background).Color;

	//		VERIFY_ARE_EQUAL(0xFF, backgroundColor.A);
	//		VERIFY_ARE_EQUAL(0x00, backgroundColor.R);
	//		VERIFY_ARE_EQUAL(0x00, backgroundColor.G);
	//		VERIFY_ARE_EQUAL(0xFF, backgroundColor.B);
	//	});

	// await RunOnUIThread(() =>

	//	{
	//		// Close the MenuFlyout
	//		menuFlyout.Hide();
	//	});

	//	await menuFlyoutClosedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();
	//}

	//void PerformFlowDirectionTest(bool hasElement, bool hasPoint, bool isRTL)
	//{

	//	Button button1 = null;
	//	Rect button1Bounds = default;
	//	var openedEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	var closedEvent = new Event();
	//	var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

	//	var menuFlyout = CreateMenuFlyoutWithSubItem();

	// await RunOnUIThread(() =>

	//	{
	//		var rootPanel = Grid> (XamlReader.Load(
	//			"<Grid Background='Orange' "

	//			" xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >"

	//			"    <Button x:Name='button1' Content='Button' Width='100' Height='50' />"

	//			"</Grid>"));

	//		button1 = Button> (rootPanel.FindName("button1"));

	//		if (isRTL)
	//		{
	//			rootPanel.FlowDirection = FlowDirection.RightToLeft;
	//		}

	//		openedRegistration.Attach(
	//			menuFlyout,
	//			new EventHandler<object> ([&](object sender, object)

	//		{
	//			openedEvent.Set();
	//		}));

	//		closedRegistration.Attach(
	//			menuFlyout,
	//			new EventHandler<object> ([&](object sender, object)

	//		{
	//			closedEvent.Set();
	//		}));

	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		LOG_OUTPUT("Is Relative=%d Has Offset=%d Is RTL=%d", hasElement, hasPoint, isRTL);
	//		if (hasPoint)
	//		{
	//			if (hasElement)
	//			{
	//				menuFlyout.ShowAt(button1, Point(50, 50));
	//			}
	//			else
	//			{
	//				menuFlyout.ShowAt(null, Point(50, 50));
	//			}
	//		}
	//		else
	//		{
	//			menuFlyout.ShowAt(button1);
	//		}
	//	});
	//	await openedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(
	//			button1.XamlRoot);
	//		var popup = popups.GetAt(popups.Size - 1);
	//		var child = popup.Child;

	//		VERIFY_IS_TRUE(popup.IsOpen);

	//		if (isRTL)
	//		{
	//			VERIFY_ARE_EQUAL(FlowDirection.RightToLeft, ((FrameworkElement)(child)).FlowDirection);
	//		}
	//		else
	//		{
	//			VERIFY_ARE_EQUAL(FlowDirection.LeftToRight, ((FrameworkElement)(child)).FlowDirection);
	//		}
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//	await TestServices.WindowHelper.WaitForIdle();
	//}

	//[TestMethod] public async Task ValidateShowAtFlowDirection()
	//{

	//	//Test Case:ShowAt(element)
	//	PerformFlowDirectionTest(true, false, false);
	//	//Test Case:ShowAt(element,point)
	//	PerformFlowDirectionTest(true, true, false);
	//	//Test Case:ShowAt(null,point)
	//	PerformFlowDirectionTest(false, true, false);
	//	//RTL
	//	//Test Case:ShowAt(element)
	//	PerformFlowDirectionTest(true, false, true);
	//	//Test Case:ShowAt(element,point)
	//	PerformFlowDirectionTest(true, true, true);
	//	//Test Case:ShowAt(null,point)
	//	PerformFlowDirectionTest(false, true, true);
	//}

	//[TestMethod] public async Task ValidateFlyoutSizingForDifferentInputModes()
	//{


	//	// There is a reliability issue for keyboard injecting that waits for the event from the InputManager
	//	// after sending the input injection. This is the work around to disable WaitForEvent.
	//	KeyboardInjectionIgnoreEventWaitOverride keyboardEventsOverride(KeyboardWaitKind.None);

	//	double expectedFlyoutWidth_Touch = 240;
	//	double expectedFlyoutWidth_NonTouch = 96;
	//	Thickness expectedFlyoutContentMargin = Thickness({ 0, 4, 0, 4 });

	//	// Narrow should occur when interacting using a mouse, pen, or keyboard.
	//	// Wide should occur when interacting using touch, a gamepad, or a remote.
	//	Thickness expectedMenuFlyoutItemPadding_Narrow = Thickness({ 12, 5, 12, 7 });
	//	Thickness expectedMenuFlyoutItemPadding_Wide = Thickness({ 12, 11, 12, 13 });

	//	TestServices.WindowHelper.SetWindowSizeOverride(Size(400, 600));

	//	Button button;
	//	MenuFlyout menuFlyout;
	//	MenuFlyoutSubItem menuFlyoutSubItem;

	//	FrameworkElement ^ menuFlyoutRoot;
	//	ItemsPresenter ^ menuFlyoutItemsPresenter;

	// await RunOnUIThread(() =>

	//	{
	//		var root = Grid> (XamlReader.Load(
	//			LR"(<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'

	//					  x: Name = 'root' >

	//					< Button x: Name = 'button' Content = 'button.flyout' >

	//						< Button.Flyout >

	//							< MenuFlyout >

	//								< MenuFlyoutItem Text = 'Item 1' />

	//								< MenuFlyoutSeparator />

	//								< ToggleMenuFlyoutItem Text = 'Item 2' />

	//								< MenuFlyoutSubItem Text = 'Sub Item 1' >

	//									< (MenuFlyoutItem)Sub item 1 </ MenuFlyoutItem >

	//									< (MenuFlyoutItem)Sub item 2 </ MenuFlyoutItem >

	//								</ MenuFlyoutSubItem >

	//							</ MenuFlyout >

	//						</ Button.Flyout >

	//					</ Button >

	//				</ Grid >)"));


	//		TestServices.WindowHelper.WindowContent = root;

	//		button = Button> (root.FindName("button"));
	//		menuFlyout = MenuFlyout> (button.Flyout);
	//		menuFlyoutSubItem = MenuFlyoutSubItem> (menuFlyout.Items[3]);

	//		button.ContextFlyout = menuFlyout;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Open MenuFlyout via touch.  The items should have wide padding.");
	//	FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Touch);

	//	TestServices.InputHelper.Tap(menuFlyoutSubItem);
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutRoot = (FrameworkElement)(TreeHelper.GetVisualChildByNameFromOpenPopups("MenuFlyoutPresenterScrollViewer", button));
	//		menuFlyoutItemsPresenter = TreeHelper.GetVisualChildByType<ItemsPresenter>(menuFlyoutRoot);
	//		VERIFY_IS_NOT_NULL(menuFlyoutRoot);
	//		VERIFY_IS_NOT_NULL(menuFlyoutItemsPresenter);

	//		VERIFY_ARE_EQUAL(expectedFlyoutWidth_Touch, menuFlyoutRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedFlyoutContentMargin, menuFlyoutItemsPresenter.Margin);
	//		VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Wide);
	//	});
	//	FlyoutHelper.HideFlyout(menuFlyout);

	//	LOG_OUTPUT("Open MenuFlyout via gamepad.  The items should have wide padding.");
	//	FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Gamepad);

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutSubItem.Focus(FocusState.Keyboard);
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();
	//	CommonInputHelper.Accept(InputDevice.Gamepad);

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutRoot = (FrameworkElement)(TreeHelper.GetVisualChildByNameFromOpenPopups("MenuFlyoutPresenterScrollViewer", button));
	//		menuFlyoutItemsPresenter = TreeHelper.GetVisualChildByType<ItemsPresenter>(menuFlyoutRoot);

	//		VERIFY_ARE_EQUAL(expectedFlyoutWidth_Touch, menuFlyoutRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedFlyoutContentMargin, menuFlyoutItemsPresenter.Margin);
	//		VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Wide);
	//	});
	//	FlyoutHelper.HideFlyout(menuFlyout);

	//	LOG_OUTPUT("Open MenuFlyout via pen.  The items should have narrow padding.");
	//	FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Pen);

	//	await TestServices.WindowHelper.WaitForIdle();
	//	//Sub menu opens with pen hover and not with a tap, but InputHelper doesn't currently have a way to input pen hovers.
	//	//Luckily PenHold does the trick.
	//	TestServices.InputHelper.PenHold(menuFlyoutSubItem);
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutRoot = (FrameworkElement)(TreeHelper.GetVisualChildByNameFromOpenPopups("MenuFlyoutPresenterScrollViewer", button));
	//		menuFlyoutItemsPresenter = TreeHelper.GetVisualChildByType<ItemsPresenter>(menuFlyoutRoot);

	//		VERIFY_ARE_EQUAL(expectedFlyoutWidth_NonTouch, menuFlyoutRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedFlyoutContentMargin, menuFlyoutItemsPresenter.Margin);
	//		VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Narrow);
	//	});
	//	FlyoutHelper.HideFlyout(menuFlyout);

	//	if (!TestServices.Utilities.IsOneCore)
	//	{
	//		LOG_OUTPUT("Open MenuFlyout via mouse.  The items should have narrow padding.");
	//		FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Mouse);

	//		OpenSubItemWithMouse(menuFlyoutSubItem);

	//		await RunOnUIThread(() =>

	//		{
	//			VERIFY_ARE_EQUAL(expectedFlyoutWidth_NonTouch, menuFlyoutRoot.MinWidth);
	//			VERIFY_ARE_EQUAL(expectedFlyoutContentMargin, menuFlyoutItemsPresenter.Margin);
	//			VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Narrow);
	//		});
	//		FlyoutHelper.HideFlyout(menuFlyout);
	//	}

	//	LOG_OUTPUT("Open MenuFlyout via keyboard.  The items should have narrow padding.");
	//	FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Keyboard);

	//	// Move to the sub menu item from the first item that requires two down
	//	TestServices.KeyboardHelper.Down();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	TestServices.KeyboardHelper.Down();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Open the sub menu item by sending the right keyboard
	//	TestServices.KeyboardHelper.Right();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		VERIFY_ARE_EQUAL(expectedFlyoutWidth_NonTouch, menuFlyoutRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedFlyoutContentMargin, menuFlyoutItemsPresenter.Margin);
	//		VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Narrow);
	//	});
	//	FlyoutHelper.HideFlyout(menuFlyout);

	//	LOG_OUTPUT("Open MenuFlyout programmatically.  The items should have the same padding as before.");
	//	FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Programmatic_ShowAt);
	// await RunOnUIThread(() =>

	//	{
	//		VERIFY_ARE_EQUAL(expectedFlyoutWidth_NonTouch, menuFlyoutRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedFlyoutContentMargin, menuFlyoutItemsPresenter.Margin);
	//		VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Narrow);
	//	});
	//	FlyoutHelper.HideFlyout(menuFlyout);

	//	LOG_OUTPUT("Open MenuFlyout via gamepad.  The items should have wide padding.");
	//	FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Gamepad);

	//	// Move to the sub menu item from the first item that requires two Gamepad Dpad down
	//	TestServices.KeyboardHelper.GamepadDpadDown();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	TestServices.KeyboardHelper.GamepadDpadDown();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Open the sub menu item by using Dpad right
	//	TestServices.KeyboardHelper.GamepadDpadRight();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		VERIFY_ARE_EQUAL(expectedFlyoutWidth_Touch, menuFlyoutRoot.MinWidth);
	//		VERIFY_ARE_EQUAL(expectedFlyoutContentMargin, menuFlyoutItemsPresenter.Margin);
	//		VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Wide);
	//	});
	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateMenuFlyoutSizeByTouch()
	//{


	//	TestServices.WindowHelper.SetWindowSizeOverride(Size(400, 600));

	//	double expectedMenuFlyoutWidth_Touch = 240;           // 240(touch min width with touch)
	//	double expectedMenuFlyoutHeight_Touch = 137;
	//	double expectedMenuFlyoutSubWidth_Touch = 240;        // 240(touch min width with touch)
	//	double expectedMenuFlyoutSubHeight_Touch = 106;
	//	double expectedMenuFlyoutItemWidth_Touch = 240;       // 240(touch min width with touch)
	//	double expectedMenuFlyoutItemHeight_Touch = 40;
	//	double expectedMenuFlyoutSeparatorWidth_Touch = 240;  // 240(touch min width with touch)
	//	double expectedMenuFlyoutSeparatorHeight_Touch = 9;
	//	double expectedMenuFlyoutMinWidth_Touch = 240;        // 240(touch min width with touch)
	//	double expectedMenuFlyoutMinHeight_Touch = 32;

	//	Grid root = null;
	//	Button button = null;
	//	MenuFlyout menuFlyout = null;
	//	MenuFlyoutSubItem menuFlyoutSubItem = null;

	// await RunOnUIThread(() =>

	//	{
	//		root = Grid> (XamlReader.Load(
	//			LR"(<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'

	//					  x: Name = 'root' >

	//					< Button x: Name = 'button' Content = 'button.flyout' >

	//						< Button.Flyout >

	//							< MenuFlyout >

	//								< MenuFlyoutItem x: Name = 'firstMenuFlyoutItem' Text = 'Item 1' />

	//								< MenuFlyoutSeparator x: Name = 'menuFlyoutSeparator' />

	//								< ToggleMenuFlyoutItem x: Name = 'toggleMenuFlyoutItem' Text = 'Item 2' />

	//								< MenuFlyoutSubItem x: Name = 'menuFlyoutSubItem' Text = 'Sub Item 1' >

	//									< (MenuFlyoutItem)Sub item 1 </ MenuFlyoutItem >

	//									< (MenuFlyoutItem)Sub item 2 </ MenuFlyoutItem >

	//								</ MenuFlyoutSubItem >

	//							</ MenuFlyout >

	//						</ Button.Flyout >

	//					</ Button >

	//				</ Grid >)"));


	//		TestServices.WindowHelper.WindowContent = root;

	//		button = Button> (root.FindName("button"));
	//		menuFlyout = MenuFlyout> (button.Flyout);
	//		menuFlyoutSubItem = MenuFlyoutSubItem> (menuFlyout.Items[3]);
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Touch);

	// await RunOnUIThread(() =>

	//	{
	//		// Verify MenuFlyout actual width/height.
	//		var menuFlyoutPresenter = GetCurrentPresenter();
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutWidth_Touch, menuFlyoutPresenter.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutHeight_Touch, menuFlyoutPresenter.ActualHeight);

	//		// Verify MenuFlyoutItem actual width/height.
	//		var firstMenuFlyoutItem = MenuFlyoutItem> (menuFlyoutPresenter.FindName("firstMenuFlyoutItem"));
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemWidth_Touch, firstMenuFlyoutItem.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemHeight_Touch, firstMenuFlyoutItem.ActualHeight);

	//		// Verify MenuFlyoutSeparator actual width/height.
	//		var menuFlyoutSeparator = MenuFlyoutSeparator ^> (menuFlyoutPresenter.FindName("menuFlyoutSeparator"));
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutSeparatorWidth_Touch, menuFlyoutSeparator.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutSeparatorHeight_Touch, menuFlyoutSeparator.ActualHeight);

	//		// Verify ToggleMenuFlyout actual width/height.
	//		var toggleMenuFlyoutItem = ToggleMenuFlyoutItem> (menuFlyoutPresenter.FindName("toggleMenuFlyoutItem"));
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemWidth_Touch, toggleMenuFlyoutItem.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemHeight_Touch, toggleMenuFlyoutItem.ActualHeight);

	//		// Verify MenuFlyoutSubItem actual width/height.
	//		var menuFlyoutSubItem = MenuFlyoutSubItem> (menuFlyoutPresenter.FindName("menuFlyoutSubItem"));
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemWidth_Touch, menuFlyoutSubItem.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemHeight_Touch, menuFlyoutSubItem.ActualHeight);
	//	});

	//	// Verify the empty MenuFlyout width/height with touch.
	//	TestServices.InputHelper.Tap(root);
	//	await TestServices.WindowHelper.WaitForIdle();
	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout = new MenuFlyout();
	//	});
	//	await ShowMenuFlyout(menuFlyout, button, 0, 50);
	// await RunOnUIThread(() =>

	//	{
	//		var menuFlyoutPresenter = GetCurrentPresenter();
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutMinWidth_Touch, menuFlyoutPresenter.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutMinHeight_Touch, menuFlyoutPresenter.ActualHeight);
	//	});
	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateMenuFlyoutSizeByMouse()
	//{


	//	TestServices.WindowHelper.SetWindowSizeOverride(Size(400, 600));

	//	double expectedMenuFlyoutWidth_Mouse = 155;
	//	double expectedMenuFlyoutHeight_Mouse = 113;
	//	double expectedMenuFlyoutSubWidth_Mouse = 96;
	//	double expectedMenuFlyoutSubHeight_Mouse = 72;
	//	double expectedMenuFlyoutItemWidth_Mouse = 155;
	//	double expectedMenuFlyoutItemHeight_Mouse = 32;
	//	double expectedMenuFlyoutSeparatorWidth_Mouse = 155;
	//	double expectedMenuFlyoutSeparatorHeight_Mouse = 9;
	//	double expectedMenuFlyoutMinWidth_Mouse = 96;
	//	double expectedMenuFlyoutMinHeight_Mouse = 32;

	//	Grid root = null;
	//	Button button = null;
	//	MenuFlyout menuFlyout = null;
	//	MenuFlyoutSubItem menuFlyoutSubItem = null;

	// await RunOnUIThread(() =>

	//	{
	//		root = Grid> (XamlReader.Load(
	//			LR"(<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'

	//					  x: Name = 'root' >

	//					< Button x: Name = 'button' Content = 'button.flyout' >

	//						< Button.Flyout >

	//							< MenuFlyout >

	//								< MenuFlyoutItem x: Name = 'firstMenuFlyoutItem' Text = 'Item 1' />

	//								< MenuFlyoutSeparator x: Name = 'menuFlyoutSeparator' />

	//								< ToggleMenuFlyoutItem x: Name = 'toggleMenuFlyoutItem' Text = 'Item 2' />

	//								< MenuFlyoutSubItem x: Name = 'menuFlyoutSubItem' Text = 'Sub Item 1' >

	//									< (MenuFlyoutItem)Sub item 1 </ MenuFlyoutItem >

	//									< (MenuFlyoutItem)Sub item 2 </ MenuFlyoutItem >

	//								</ MenuFlyoutSubItem >

	//							</ MenuFlyout >

	//						</ Button.Flyout >

	//					</ Button >

	//				</ Grid >)"));


	//		TestServices.WindowHelper.WindowContent = root;

	//		button = Button> (root.FindName("button"));
	//		menuFlyout = MenuFlyout> (button.Flyout);
	//		menuFlyoutSubItem = MenuFlyoutSubItem> (menuFlyout.Items[3]);
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Verif the MenuFlyout presenter's actual width/height with mouse.
	//	FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Mouse);
	// await RunOnUIThread(() =>

	//	{
	//		var menuFlyoutPresenter = GetCurrentPresenter();
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutWidth_Mouse, menuFlyoutPresenter.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutHeight_Mouse, menuFlyoutPresenter.ActualHeight);

	//		var firstMenuFlyoutItem = MenuFlyoutItem> (menuFlyoutPresenter.FindName("firstMenuFlyoutItem"));
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemWidth_Mouse, firstMenuFlyoutItem.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemHeight_Mouse, firstMenuFlyoutItem.ActualHeight);

	//		var menuFlyoutSeparator = MenuFlyoutSeparator ^> (menuFlyoutPresenter.FindName("menuFlyoutSeparator"));
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutSeparatorWidth_Mouse, menuFlyoutSeparator.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutSeparatorHeight_Mouse, menuFlyoutSeparator.ActualHeight);

	//		var toggleMenuFlyoutItem = ToggleMenuFlyoutItem> (menuFlyoutPresenter.FindName("toggleMenuFlyoutItem"));
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemWidth_Mouse, toggleMenuFlyoutItem.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemHeight_Mouse, toggleMenuFlyoutItem.ActualHeight);

	//		var menuFlyoutSubItem = MenuFlyoutSubItem> (menuFlyoutPresenter.FindName("menuFlyoutSubItem"));
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemWidth_Mouse, menuFlyoutSubItem.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutItemHeight_Mouse, menuFlyoutSubItem.ActualHeight);
	//	});

	//	// Verif the MenuFlyoutSubItem presenter's actual width/height with mouse.
	//	OpenSubItemWithMouse(menuFlyoutSubItem);
	// await RunOnUIThread(() =>

	//	{
	//		var menuFlyoutPresenter = GetCurrentPresenter();
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutSubWidth_Mouse, menuFlyoutPresenter.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutSubHeight_Mouse, menuFlyoutPresenter.ActualHeight);
	//	});
	//	FlyoutHelper.HideFlyout(menuFlyout);

	//	// Verify the empty MenuFlyout width/height with mouse.
	//	Private.Infrastructure.TestServices.InputHelper.LeftMouseClick(root);
	//	await TestServices.WindowHelper.WaitForIdle();
	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout = new MenuFlyout();
	//	});
	//	await ShowMenuFlyout(menuFlyout, button, 0, 50, false /* forceTapAsPreviousInputMessage */);
	// await RunOnUIThread(() =>

	//	{
	//		var menuFlyoutPresenter = GetCurrentPresenter();
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutMinWidth_Mouse, menuFlyoutPresenter.ActualWidth);
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutMinHeight_Mouse, menuFlyoutPresenter.ActualHeight);
	//	});
	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateScrollableItems()
	//{


	//	TestServices.WindowHelper.SetWindowSizeOverride(Size(400, 600));

	//	double expectedMenuFlyoutHeight = 300;
	//	double expectedMenuFlyoutScrollableHeight = 1024;

	//	Grid root = null;

	// await RunOnUIThread(() =>

	//	{
	//		var root = new Grid();
	//		TestServices.WindowHelper.WindowContent = root;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var menuFlyout = CreateMenuFlyoutLongItemsFromXaml();

	//	// Specify the MenuFlyout height with 300
	// await RunOnUIThread(() =>

	//	{
	//		var type = wxaml_interop.TypeName();
	//		type.Name = "Microsoft.UI.Xaml.Controls.MenuFlyoutPresenter";
	//		type.Kind = wxaml_interop.TypeKind.Metadata;

	//		var style = new Style(type);
	//		style.Setters.Add(new Setter(MenuFlyoutPresenter.MaxHeightProperty, "300"));

	//		menuFlyout.MenuFlyoutPresenterStyle = style;
	//	});

	//	await ShowMenuFlyout(menuFlyout, null, 100, 200);
	// await RunOnUIThread(() =>

	//	{
	//		var menuFlyoutPresenter = GetCurrentPresenter();
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutHeight, menuFlyoutPresenter.ActualHeight);

	//		var scrollViewer = ScrollViewer ^> (TreeHelper.GetVisualChildByName(menuFlyoutPresenter, "MenuFlyoutPresenterScrollViewer"));
	//		VERIFY_ARE_EQUAL(expectedMenuFlyoutScrollableHeight, scrollViewer.ScrollableHeight);
	//	});
	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateMenuFlyoutInVisibleBounds()
	//{
	//	// Flyout uses the visible bounds since TH2 platform.
	//	ValidateMenuFlyoutPosition(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseVisible);
	//}

	//[TestMethod] public async Task ValidateMenuFlyoutPosition(Windows.UI.ViewManagement.ApplicationViewBoundsMode expectedBoundsMode)
	//{


	//	// Set the specified visible bounds to verify the MenuFlyout position on the visible bounds.
	//	TestServices.WindowHelper.SetVisibleBounds(Rect(0, 50, 480, 700));

	//	var menuFlyout = CreateMenuFlyoutWithSubItem();

	// await RunOnUIThread(() =>

	//	{
	//		var rootPanel = Grid> (XamlReader.Load(
	//			LR"(<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Background='Orange' >
	//					< TextBlock Text = 'Test MenuFlyout on the visible bounds.' FontSize = '20' />

	//				</ Grid >)"));


	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Show the MenuFlout on the out of the visible bounds.
	//	await ShowMenuFlyout(menuFlyout, null, 100, 0, true /* forceTapAsPreviousInputMessage */);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var presenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		var menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
	//		LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);

	//		if (expectedBoundsMode == Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseVisible)
	//		{
	//			VERIFY_ARE_EQUAL(menuFlyoutBounds.Top, 50);
	//		}
	//		else
	//		{
	//			VERIFY_IS_LESS_THAN(menuFlyoutBounds.Top, 50);
	//		}
	//	});

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout.Hide();
	//	});

	//	// Set the window with the landscape mode with specifying the visible bounds.
	//	TestServices.WindowHelper.SetWindowSizeOverride(Size(800, 480));
	//	TestServices.WindowHelper.SetVisibleBounds(Rect(50, 0, 700, 480));
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Show the MenuFlout on the out of the visible bounds.
	//	await ShowMenuFlyout(menuFlyout, null, 550, 100, true /* forceTapAsPreviousInputMessage */);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	presenter = GetCurrentPresenter();
	// await RunOnUIThread(() =>

	//	{
	//		var menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
	//		LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);

	//		if (expectedBoundsMode == Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseVisible)
	//		{
	//			VERIFY_IS_LESS_THAN(menuFlyoutBounds.Left + menuFlyoutBounds.Width, 750);
	//		}
	//		else
	//		{
	//			VERIFY_IS_GREATER_THAN(menuFlyoutBounds.Left + menuFlyoutBounds.Width, 750);
	//		}
	//	});

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout.Hide();
	//	});
	//}

	//[TestMethod] public async Task ValidateOverlayBrush()
	//{

	//	FlyoutHelper.ValidateOverlayBrush<MenuFlyout>("MenuFlyoutLightDismissOverlayBackground");
	//}

	//[TestMethod] public async Task ValidateCanUseBothShowAtMethods()
	//{


	//	var menuFlyout = CreateMenuFlyout();
	//	Button button = null;

	//	var loadedEvent = new Event();
	//	var loadedRegistration = CreateSafeEventRegistration(Button, Loaded);

	// await RunOnUIThread(() =>

	//	{
	//		button = new Button();
	//		button.Content = "Button for flyout";

	//		loadedRegistration.Attach(button, [loadedEvent]() { loadedEvent.Set(); });
	//		TestServices.WindowHelper.WindowContent = button;
	//	});

	//	await loadedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var openedEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	openedRegistration.Attach(menuFlyout, [&](){ openedEvent.Set(); });

	// await RunOnUIThread(() =>

	//	{
	//		LOG_OUTPUT("Show the MenuFlyout first using ShowAt() with one parameter.");
	//		menuFlyout.ShowAt(button);
	//	});

	//	await openedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	FlyoutHelper.HideFlyout(menuFlyout);

	// await RunOnUIThread(() =>

	//	{
	//		LOG_OUTPUT("Now show the MenuFlyout using ShowAt() with two parameters.");
	//		menuFlyout.ShowAt(button, { 0, 0});
	//	});
	//	await openedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	FlyoutHelper.HideFlyout(menuFlyout);

	//	openedRegistration.Detach();

	//	LOG_OUTPUT("Now create a new MenuFlyout to test the other calling order.");
	//	menuFlyout = CreateMenuFlyout();
	//	openedRegistration.Attach(menuFlyout, [&](){ openedEvent.Set(); });

	// await RunOnUIThread(() =>

	//	{
	//		LOG_OUTPUT("Show the MenuFlyout first using ShowAt() with two parameters.");
	//		menuFlyout.ShowAt(button, { 0, 0});
	//	});

	//	await openedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	FlyoutHelper.HideFlyout(menuFlyout);

	// await RunOnUIThread(() =>

	//	{
	//		LOG_OUTPUT("Now show the MenuFlyout using ShowAt() with one parameter.");
	//		menuFlyout.ShowAt(button);
	//	});
	//	await openedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidatePopupWindowedScrollingWithMouse()
	//{


	//	TestServices.WindowHelper.SetWindowSizeOverride(Size(400, 600));
	//	Grid root = null;
	//	Thumb ^ thumb = null;
	//	Rect thumbBoundsBeforeDrag = default;
	//	Rect thumbBoundsAfterDrag = default;

	// await RunOnUIThread(() =>

	//	{
	//		root = new Grid();
	//		TestServices.WindowHelper.WindowContent = root;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var menuFlyout = CreateMenuFlyoutLongItemsFromXaml();

	//	await ShowMenuFlyout(menuFlyout, null, 100, 200);
	// await RunOnUIThread(() =>

	//	{
	//		var menuFlyoutPresenter = GetCurrentPresenter();
	//		var scrollViewer = ScrollViewer ^> (TreeHelper.GetVisualChildByName(menuFlyoutPresenter, "MenuFlyoutPresenterScrollViewer"));
	//		VERIFY_IS_NOT_NULL(scrollViewer);

	//		thumb = Thumb ^> (TreeHelper.GetVisualChildByName(scrollViewer, "VerticalThumb"));
	//		VERIFY_IS_NOT_NULL(thumb);

	//		thumbBoundsBeforeDrag = await ControlHelper.GetBounds((FrameworkElement)(thumb));
	//		LOG_OUTPUT("ValidatePopupWindowedScrollingWithMouse: Thumb bounds left=%f top=%f width=%f height=%f", thumbBoundsBeforeDrag.Left, thumbBoundsBeforeDrag.Top, thumbBoundsBeforeDrag.Width, thumbBoundsBeforeDrag.Height);
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Dragging the thumb to scroll ListViewItem content
	//	TestServices.InputHelper.MoveMouse(thumb);
	//	await TestServices.WindowHelper.WaitForIdle();
	//	TestServices.InputHelper.MouseButtonDown(thumb, 0, 0, MouseButton.Left);
	//	LOG_OUTPUT("ValidatePopupWindowedScrollingWithMouse: Dragging the thumb to scroll the content.");
	//	TestServices.InputHelper.MouseDrag(
	//		Point(thumbBoundsBeforeDrag.Left + (thumbBoundsBeforeDrag.Width / 2), thumbBoundsBeforeDrag.Top + (thumbBoundsBeforeDrag.Height / 2)),
	//		Point(thumbBoundsBeforeDrag.Left + (thumbBoundsBeforeDrag.Width / 2), thumbBoundsBeforeDrag.Top + (thumbBoundsBeforeDrag.Height / 2) + 200),
	//		MouseButton.Left);
	//	await TestServices.WindowHelper.WaitForIdle();
	//	TestServices.InputHelper.MouseButtonUp(thumb, 0, 0, MouseButton.Left);
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		thumbBoundsAfterDrag = await ControlHelper.GetBounds((FrameworkElement)(thumb));
	//		LOG_OUTPUT("ValidatePopupWindowedScrollingWithMouse: Thumb bounds after dragging left=%f top=%f width=%f height=%f", thumbBoundsAfterDrag.Left, thumbBoundsAfterDrag.Top, thumbBoundsAfterDrag.Width, thumbBoundsAfterDrag.Height);
	//		// Conscious scrollbars feature results in the thumb expanding to 16 wide when the pointer is over the scrollbar, which it will be after dragging here
	//		// So checking the left and width of the thumb is not desirable
	//		VERIFY_IS_GREATER_THAN(thumbBoundsAfterDrag.Top, thumbBoundsBeforeDrag.Top);
	//		VERIFY_ARE_EQUAL(thumbBoundsBeforeDrag.Height, thumbBoundsAfterDrag.Height);
	//	});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateKeyboardNavigationAfterClosingSubMenu()
	//{


	//	var loadedEvent = new Event();
	//	var loadedRegistration = CreateSafeEventRegistration(Grid, Loaded);

	//	var openedEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration(Flyout, Opened);

	//	var closedEvent = new Event();
	//	var closedRegistration = CreateSafeEventRegistration(Flyout, Closed);

	// await RunOnUIThread(() =>

	//	{
	//		var rootGrid = (Grid)(XamlReader.Load(
	//			"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'"

	//			"      Width='400' Height='400'>"

	//			"  <Button x:Name='button' Content='Button with Flyout' HorizontalAlignment='Center' VerticalAlignment='Center'>"

	//			"    <Button.Flyout>"

	//			"      <Flyout x:Name='flyout'>"

	//			"        <StackPanel>"

	//			"          <MenuFlyoutItem>MenuFlyoutItem</MenuFlyoutItem>"

	//			"          <MenuFlyoutSubItem Text='MenuFlyoutSubItem'>"

	//			"            <MenuFlyoutItem>MenuFlyoutItem</MenuFlyoutItem>"

	//			"          </MenuFlyoutSubItem>"

	//			"        </StackPanel>"

	//			"      </Flyout>"

	//			"    </Button.Flyout>"

	//			"  </Button>"

	//			"</Grid>"));
	//		VERIFY_IS_NOT_NULL(rootGrid);

	//		loadedRegistration.Attach(rootGrid, [loadedEvent]()

	//		{
	//			LOG_OUTPUT("Grid loaded.");
	//			loadedEvent.Set();
	//		});

	//		var button = Button> (rootGrid.FindName("button"));
	//		VERIFY_IS_NOT_NULL(button);

	//		var flyout = Flyout ^> (button.Flyout);
	//		VERIFY_IS_NOT_NULL(flyout);

	//		openedRegistration.Attach(flyout, (s, e) =>

	//		{
	//			LOG_OUTPUT("Flyout opened.");
	//			openedEvent.Set();
	//		});

	//		closedRegistration.Attach(flyout, (s, e) =>

	//		{
	//			LOG_OUTPUT("Flyout closed.");
	//			closedEvent.Set();
	//		});

	//		TestServices.WindowHelper.WindowContent = rootGrid;
	//	});

	//	await loadedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Tab into the Button.");
	//	TestServices.KeyboardHelper.Tab();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Open menu.");
	//	TestServices.KeyboardHelper.Enter();
	//	await openedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Move down to the sub menu.");
	//	TestServices.KeyboardHelper.Tab();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Open the sub menu.");
	//	CommonInputHelper.Right(InputDevice.Keyboard);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Close the sub menu.");
	//	CommonInputHelper.Left(InputDevice.Keyboard);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Hit up arrow key.");
	//	TestServices.KeyboardHelper.Up();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Close menu.");
	//	TestServices.KeyboardHelper.Escape();
	//	await closedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();
	//}

	//[TestMethod] public async Task ValidateOpenedSubMenuFocusItemByKeyboard()
	//{


	//	Button button1 = null;
	//	MenuFlyout menuFlyout = null;
	//	MenuFlyoutSubItem subItem1 = null;
	//	MenuFlyoutItem subItem11 = null;

	//	Canvas rootPanel = await SetupRootPanelForSubMenuTest();

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout = MenuFlyout> (XamlReader.Load(
	//			"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >"

	//			"    <MenuFlyoutSubItem x:Name='subItem1' Text='Menu sub item 1'>"

	//			"        <MenuFlyoutItem x:Name='subItem1.1' >Menu item 1.1</MenuFlyoutItem>"

	//			"        <MenuFlyoutItem x:Name='subItem1.2' >Menu item 1.2</MenuFlyoutItem>"

	//			"        <MenuFlyoutItem x:Name='subItem1.3' >Menu item 1.3</MenuFlyoutItem>"

	//			"    </MenuFlyoutSubItem>"

	//			"    <MenuFlyoutItem>item2</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>item3</MenuFlyoutItem>"

	//			"</MenuFlyout>"));

	//		button1 = Button> (rootPanel.FindName("button1"));
	//	});

	//	await ShowMenuFlyout(menuFlyout, button1, -50, 50);

	//	// Open the sub menu by using the Keyboard Right key
	//	CommonInputHelper.Right(InputDevice.Keyboard);
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		var menuFlyoutPresenter = GetCurrentPresenter();
	//		subItem11 = MenuFlyoutItem> (menuFlyoutPresenter.FindName("subItem1.1"));
	//	});

	//	var subItem11GotFocusEvent = new Event();
	//	var subItem11gotFocusRegistration = CreateSafeEventRegistration(MenuFlyoutItem, GotFocus);
	//	subItem11gotFocusRegistration.Attach(subItem11, [&](){ subItem11GotFocusEvent.Set(); });

	//	// Close the sub menu by using the Keyboard Left key
	//	CommonInputHelper.Left(InputDevice.Keyboard);
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Re-open the sub menu to verify the focused first item
	//	CommonInputHelper.Right(InputDevice.Keyboard);
	//	subItem11GotFocusEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	// Close the sub menu
	//	CommonInputHelper.Left(InputDevice.Keyboard);
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		var menuFlyoutPresenter = GetCurrentPresenter();
	//		subItem1 = MenuFlyoutSubItem> (menuFlyoutPresenter.FindName("subItem1"));
	//	});

	//	// Re-open the sub menu to verify the focused first item
	//	CommonInputHelper.Right(InputDevice.Keyboard);
	//	subItem11GotFocusEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var subItem1GotFocusEvent = new Event();
	//	var subItem1GotFocusRegistration = CreateSafeEventRegistration(MenuFlyoutSubItem, GotFocus);
	//	subItem1GotFocusRegistration.Attach(subItem1, [&]() { subItem1GotFocusEvent.Set(); });

	//	// Close the sub menu
	//	TestServices.KeyboardHelper.Escape();
	//	await TestServices.WindowHelper.WaitForIdle();
	//	subItem1GotFocusEvent.WaitForDefault();

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateFocusedItemDownAndUpAfterOverrideFocusItem()
	//{


	//	MenuFlyout menuFlyout = null;
	//	MenuFlyoutItem secondMenuItem = null;
	//	MenuFlyoutItem thirdMenuItem = null;

	// await RunOnUIThread(() =>

	//	{
	//		var rootPanel = new Grid();
	//		TestServices.WindowHelper.WindowContent = rootPanel;

	//		menuFlyout = MenuFlyout> (XamlReader.Load(
	//			LR"(<MenuFlyout x:Name="menuFlyout" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" >

	//					< (MenuFlyoutItem)MenuItem1 </ MenuFlyoutItem >

	//					< (MenuFlyoutItem)MenuItem2 </ MenuFlyoutItem >

	//					< (MenuFlyoutItem)MenuItem3 </ MenuFlyoutItem >

	//				</ MenuFlyout >)"));

	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var openedEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	openedRegistration.Attach(menuFlyout, [&]()

	//	{
	//		var menuItems = menuFlyout.Items;

	//		var firstMenuItem = MenuFlyoutItem> (menuItems[0]);
	//		secondMenuItem = MenuFlyoutItem> (menuItems[1]);
	//		thirdMenuItem = MenuFlyoutItem> (menuItems[2]);

	//		// Override the focus on the first menu item
	//		firstMenuItem.Focus(FocusState.Keyboard);

	//		openedEvent.Set();
	//	});

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout.ShowAt(null, Point(50, 50));
	//	});
	//	await openedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var gotFocusEvent = new Event();
	//	var gotFocusRegistration = CreateSafeEventRegistration(MenuFlyoutItem, GotFocus);
	//	gotFocusRegistration.Attach(secondMenuItem, [&]()

	//	{
	//		VERIFY_IS_TRUE(secondMenuItem.Text == "MenuItem2");
	//		gotFocusEvent.Set();
	//	});

	//	CommonInputHelper.Down(InputDevice.Keyboard);
	//	gotFocusEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	FlyoutHelper.HideFlyout(menuFlyout);

	//	// Navigate to the last menu item through Keyboard Up event
	//	openedEvent.Reset();
	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout.ShowAt(null, Point(50, 50));
	//	});
	//	await openedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var gotFocusEvent2 = new Event();
	//	var gotFocusRegistration2 = CreateSafeEventRegistration(MenuFlyoutItem, GotFocus);
	//	gotFocusRegistration2.Attach(thirdMenuItem, [&]()

	//	{
	//		VERIFY_IS_TRUE(thirdMenuItem.Text == "MenuItem3");
	//		gotFocusEvent2.Set();
	//	});

	//	CommonInputHelper.Up(InputDevice.Keyboard);
	//	gotFocusEvent2.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateSingleItemGetsInitialKeyboardFocus()
	//{


	//	Button button = null;
	//	MenuFlyout menuFlyout = null;
	//	MenuFlyoutItem menuFlyoutItem = null;

	//	var loadedEvent = new Event();
	//	var loadedRegistration = CreateSafeEventRegistration(Button, Loaded);

	// await RunOnUIThread(() =>

	//	{
	//		button = Button> (XamlReader.Load(
	//			LR"(<Button xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

	//				Click for flyout
	//				<Button.Flyout>
	//					< MenuFlyout x:Name = 'menuFlyout' xmlns = 'http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x = 'http://schemas.microsoft.com/winfx/2006/xaml' >

	//						< (MenuFlyoutItem)The menu item </ MenuFlyoutItem >

	//					</ MenuFlyout >

	//				</ Button.Flyout >

	//			</ Button >) "));


	//		loadedRegistration.Attach(button, [loadedEvent]() { loadedEvent.Set(); });
	//		TestServices.WindowHelper.WindowContent = button;
	//	});

	//	await loadedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	var openedEvent = new Event();
	//	var gotFocusEvent = new Event();
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	var gotFocusRegistration = CreateSafeEventRegistration(MenuFlyoutItem, GotFocus);

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout = MenuFlyout> (button.Flyout);
	//		menuFlyoutItem = MenuFlyoutItem> (menuFlyout.Items[0]);

	//		openedRegistration.Attach(menuFlyout, [&]() { openedEvent.Set(); });
	//		gotFocusRegistration.Attach(menuFlyoutItem, [&]() { gotFocusEvent.Set(); });
	//	});

	//	ControlHelper.EnsureFocused(button);
	//	CommonInputHelper.Accept(InputDevice.Keyboard);

	//	await openedEvent.WaitForDefault();
	//	gotFocusEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateMenuItemsShowIcons()
	//{


	//	ControlHelper.ValidateUIElementTree(
	//		Size(400, 600),
	//		1.f,
	//		[&]()

	//		{
	//		Grid rootPanel = null;
	//		Button button = null;
	//		MenuFlyout menuFlyout = null;

	//		await RunOnUIThread(() =>

	//			{
	//			rootPanel = Grid> (XamlReader.Load(
	//				LR"(<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	//						< Button x: Name = "button1" Content = "Button 1" />

	//					</ Grid >)"));

	//				TestServices.WindowHelper.WindowContent = rootPanel;

	//			menuFlyout = MenuFlyout> (XamlReader.Load(
	//				LR"(<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	//						< MenuFlyoutItem Icon = "Accept" Text = "menu item 1" />

	//						< MenuFlyoutItem Text = "menu item 2" />

	//						< MenuFlyoutSeparator />

	//						< ToggleMenuFlyoutItem Icon = "Play" Text = "toggle menu item 1" />

	//						< ToggleMenuFlyoutItem IsChecked = "True" Text = "toggle menu item 2" />

	//						< MenuFlyoutSeparator />

	//						< MenuFlyoutSubItem Icon = "Like" Text = "sub menu item 1" >

	//							< MenuFlyoutItem Icon = "Setting" Text = "menu item 1.1" />

	//							< MenuFlyoutItem Text = "menu item 2.2" />

	//						</ MenuFlyoutSubItem >

	//					</ MenuFlyout >)"));


	//				button = Button> (rootPanel.FindName("button1"));
	//		});
	//		await TestServices.WindowHelper.WaitForIdle();

	//		await ShowMenuFlyout(menuFlyout, button, 50, 50);

	//		await TestServices.WindowHelper.WaitForIdle();

	//		FlyoutHelper.HideFlyout(menuFlyout);

	//		return rootPanel;
	//	});
	//}

	//void FocusDoesNotJumpWhenUsingGamepadTriggersFollowedByDPad()
	//{


	//	MenuFlyout menuFlyout = null;
	//	Button button = null;

	//	DependencyObject ^ beforeTriggerFocusedObject = null;
	//	DependencyObject ^ afterTriggerFocusedObject = null;

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout = new MenuFlyout();
	//		for (int i = 0; i < 50; ++i)
	//		{
	//			var item = new MenuFlyoutItem();
	//			item.Text = "Item";

	//			menuFlyout.Items.Add(item);
	//		}

	//		button = new Button();
	//		button.Content = "Button";

	//		TestServices.WindowHelper.WindowContent = button;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ShowMenuFlyout(menuFlyout, button, 0, 0, true /* forceTapAsPreviousInputMessage */);

	// await RunOnUIThread(() =>

	//	{
	//		beforeTriggerFocusedObject = DependencyObject ^> (FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
	//		WEX.Common.Throw.IfNull(reinterpret_cast<void*>(beforeTriggerFocusedObject), "An item should get focus when the MenuFlyout opens.");
	//	});

	//	TestServices.KeyboardHelper.GamepadRightTrigger();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		afterTriggerFocusedObject = DependencyObject ^> (FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
	//		WEX.Common.Throw.If(beforeTriggerFocusedObject.Equals(afterTriggerFocusedObject), E_FAIL, "The RightTrigger should have moved focus.");
	//	});

	//	TestServices.KeyboardHelper.GamepadDpadDown();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		var focusedElement = DependencyObject ^> (FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
	//		VERIFY_IS_FALSE(afterTriggerFocusedObject.Equals(focusedElement));

	//		var presenter = GetMenuFlyoutPresenter(menuFlyout);

	//		var expectedFocusItemIndex = presenter.IndexFromContainer(afterTriggerFocusedObject) + 1;
	//		WEX.Common.Throw.If(expectedFocusItemIndex == -1, E_FAIL, "The expected focused element should have an item index.");

	//		var focusedItemIndex = presenter.IndexFromContainer(focusedElement);
	//		WEX.Common.Throw.If(focusedItemIndex == -1, E_FAIL, "The new focused element should have an item index.");

	//		VERIFY_ARE_EQUAL(expectedFocusItemIndex, focusedItemIndex);

	//		beforeTriggerFocusedObject = focusedElement;
	//	});

	//	TestServices.KeyboardHelper.GamepadLeftTrigger();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		afterTriggerFocusedObject = DependencyObject ^> (FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
	//		WEX.Common.Throw.If(beforeTriggerFocusedObject.Equals(afterTriggerFocusedObject), E_FAIL, "The LeftTrigger should have moved focus.");
	//	});

	//	TestServices.KeyboardHelper.GamepadDpadDown();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		var focusedElement = DependencyObject ^> (FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
	//		VERIFY_IS_FALSE(afterTriggerFocusedObject.Equals(focusedElement));

	//		var presenter = GetMenuFlyoutPresenter(menuFlyout);

	//		var expectedFocusItemIndex = presenter.IndexFromContainer(afterTriggerFocusedObject) + 1;
	//		WEX.Common.Throw.If(expectedFocusItemIndex == -1, E_FAIL, "The expected focused element should have an item index.");

	//		var focusedItemIndex = presenter.IndexFromContainer(focusedElement);
	//		WEX.Common.Throw.If(focusedItemIndex == -1, E_FAIL, "The new focused element should have an item index.");

	//		VERIFY_ARE_EQUAL(expectedFocusItemIndex, focusedItemIndex);

	//		beforeTriggerFocusedObject = focusedElement;
	//	});

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task CanDetectChangesToCanExecuteWithoutBeingInVisualTree()
	//{


	//	MenuFlyout menuFlyout = null;
	//	MenuFlyoutItem menuFlyoutItem = null;
	//	Button button = null;

	//	MenuCommand ^ command = new MenuCommand(new ExecuteDelegate([](object) { }), false /*canExecute*/, null);

	// await RunOnUIThread(() =>

	//	{
	//		menuFlyoutItem = new MenuFlyoutItem();
	//		menuFlyoutItem.Text = "Item";
	//		menuFlyoutItem.Command = command;

	//		menuFlyout = new MenuFlyout();
	//		menuFlyout.Items.Add(menuFlyoutItem);

	//		button = new Button();
	//		button.Flyout = menuFlyout;
	//		button.Content = "Button";

	//		TestServices.WindowHelper.WindowContent = button;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ShowMenuFlyout(menuFlyout, button, 0, 0);

	// await RunOnUIThread(() =>

	//	{
	//		WEX.Common.Throw.IfFalse(ControlHelper.IsInVisualState(menuFlyoutItem, "CommonStates", "Disabled"), E_FAIL, "Expected that the menu flyout item starts out as disabled.");
	//	});

	//	FlyoutHelper.HideFlyout(menuFlyout);

	//	LOG_OUTPUT("Change the MenuFlyoutItem's attached command's CanExecute property to true . item should now show as enabled.");
	//	command.CanExecuteFlag = true;

	//	await ShowMenuFlyout(menuFlyout, button, 0, 0);

	// await RunOnUIThread(() =>

	//	{
	//		VERIFY_IS_TRUE(ControlHelper.IsInVisualState(menuFlyoutItem, "CommonStates", "Normal"));
	//	});

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task VerifyIsEnabledPropagatesTreeFromCommand()
	//{


	//	// Regression coverage for:
	//	// MSFT:11947475 - High Contrast Desktop : Photos : Selected item in aspect ratio doesn't following High contrast standards after modifying the value
	//	// The issue is that MenuFlyoutItem.Command.CanExecute returns false, but this was not propagating down the visual tree to the TextBlock template part.
	//	// Note: IsEnabled is only publically visible on Control via the public api even though internally it is on all UIElements.
	//	// So, to test this scenario, we insert a dummy ContentControl into the MenuFlyoutItem's template so we have a way of reading the IsEnabled property
	//	// on the descendant elements of the MenuFlyoutItem.

	//	MenuFlyout menuFlyout;
	//	MenuFlyoutItem menuFlyoutItem;

	//	MenuCommand ^ command = new MenuCommand(new ExecuteDelegate([](object) { }), false /*canExecute*/, null);

	// await RunOnUIThread(() =>

	//	{
	//		var rootGrid = (Grid)(XamlReader.Load(
	//			LR"(<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	//					  Background = "SlateBlue" >

	//					< Grid.Resources >

	//						< Style x: Key = "MenuFlyoutItemStyle" TargetType = "MenuFlyoutItem" >

	//							< Setter Property = "Template" >

	//								< Setter.Value >

	//									< ControlTemplate TargetType = "MenuFlyoutItem" >

	//										< Grid x: Name = "LayoutRoot" >

	//											< ContentControl x: Name = "testContentControl" >

	//												< TextBlock x: Name = "TextBlock" />

	//											</ ContentControl >

	//										</ Grid >

	//									</ ControlTemplate >

	//								</ Setter.Value >

	//							</ Setter >

	//						</ Style >

	//					</ Grid.Resources >

	//					< FlyoutBase.AttachedFlyout >

	//						< MenuFlyout x: Name = "menuFlyout" >

	//							< MenuFlyoutItem x: Name = "menuFlyoutItem" Style = "{StaticResource MenuFlyoutItemStyle}" > Some Text </ MenuFlyoutItem >

	//						</ MenuFlyout >

	//					</ FlyoutBase.AttachedFlyout >

	//				</ Grid >)"));


	//		menuFlyout = (MenuFlyout)(rootGrid.FindName("menuFlyout"));
	//		menuFlyoutItem = (MenuFlyoutItem)(rootGrid.FindName("menuFlyoutItem"));

	//		menuFlyoutItem.Command = command;

	//		TestServices.WindowHelper.WindowContent = rootGrid;
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	await ShowMenuFlyout(menuFlyout, null, 0, 0);

	// await RunOnUIThread(() =>

	//	{
	//		var testContentControl = ContentControl ^> (TreeHelper.GetVisualChildByName(menuFlyoutItem, "testContentControl"));
	//		VERIFY_IS_FALSE(testContentControl.IsEnabled);
	//	});

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//// Shows the same MenuFlyout twice in a row, without closing it, attempting to position it beyond the screen's boundaries.
	//// Ensures it is moved within the screen's boundaries.
	//void MenuFlyoutRemainsInBoundsWhenShownTwice()
	//{


	//	Grid rootGrid = null;
	//	MenuFlyout menuFlyout = null;

	//	Point initialMenuItemPosition{ }
	//	;
	//	Point showAtPosition{ }
	//	;

	//	var loadedEvent = new Event();
	//	var menuFlyoutOpenedEvent = new Event();
	//	var menuFlyoutClosedEvent = new Event();

	//	var loadedRegistration = CreateSafeEventRegistration(Grid, Loaded);
	//	var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
	//	var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

	// await RunOnUIThread(() =>

	//	{
	//		rootGrid = (Grid)(XamlReader.Load(
	//			"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' "

	//			"  x:Name='root' Background='SlateBlue'> "

	//			"  <FlyoutBase.AttachedFlyout> "

	//			"    <MenuFlyout> "

	//			"      <MenuFlyoutItem>Some Text</MenuFlyoutItem> "

	//			"    </MenuFlyout> "

	//			"  </FlyoutBase.AttachedFlyout> "

	//			"</Grid>"));
	//		VERIFY_IS_NOT_NULL(rootGrid);

	//		loadedRegistration.Attach(rootGrid, [&]() { loadedEvent.Set(); });

	//		TestServices.WindowHelper.WindowContent = rootGrid;

	//		menuFlyout = MenuFlyout> (MenuFlyout.GetAttachedFlyout(rootGrid));
	//		VERIFY_IS_NOT_NULL(menuFlyout);

	//		openedRegistration.Attach(menuFlyout, new EventHandler<object> ([rootGrid, menuFlyout, menuFlyoutOpenedEvent, &initialMenuItemPosition](object, object)

	//		{
	//			LOG_OUTPUT("MenuFlyout.Opened event raised");

	//			MenuFlyoutItem menuItem = MenuFlyoutItem> (menuFlyout.Items[0]);
	//			VERIFY_IS_NOT_NULL(menuItem);

	//			initialMenuItemPosition = menuItem.TransformToVisual(rootGrid).TransformPoint(Point(0, 0));
	//			LOG_OUTPUT("Initial MenuFlyoutItem position in MenuFlyout.Opened event handler: %f, %f.", initialMenuItemPosition.X, initialMenuItemPosition.Y);

	//			menuFlyoutOpenedEvent.Set();
	//		}));

	//		closedRegistration.Attach(menuFlyout, (s, e) =>

	//		{
	//			LOG_OUTPUT("MenuFlyout.Closed event raised");
	//			menuFlyoutClosedEvent.Set();
	//		}));
	//	});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Waiting for root Grid to load.");
	//	await loadedEvent.WaitForDefault();

	// await RunOnUIThread(() =>

	//	{
	//		showAtPosition.X = (float)(rootGrid.ActualWidth) - 25.0f;
	//		showAtPosition.Y = (float)(rootGrid.ActualHeight) - 25.0f;

	//		LOG_OUTPUT("Showing MenuFlyout at position %f, %f.", showAtPosition.X, showAtPosition.Y);
	//		menuFlyout.ShowAt(rootGrid, showAtPosition);
	//	});

	//	LOG_OUTPUT("Waiting for MenuFlyout.Opened event.");
	//	await menuFlyoutOpenedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		LOG_OUTPUT("Showing MenuFlyout at position %f, %f again.", showAtPosition.X, showAtPosition.Y);
	//		menuFlyout.ShowAt(rootGrid, showAtPosition);
	//	});

	//	LOG_OUTPUT("Waiting for a few ticks to make sure the MenuFlyout has a chance to reposition itself.");
	//	TestServices.WindowHelper.SynchronouslyTickUIThread(2);
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		MenuFlyoutItem menuItem = MenuFlyoutItem> (menuFlyout.Items[0]);
	//		VERIFY_IS_NOT_NULL(menuItem);

	//		Point finalMenuItemPosition = menuItem.TransformToVisual(rootGrid).TransformPoint(Point(0, 0));
	//		LOG_OUTPUT("Final MenuFlyoutItem position: %f, %f.", finalMenuItemPosition.X, finalMenuItemPosition.Y);

	//		LOG_OUTPUT("Verifying initial and final MenuFlyoutItem positions are identical.");
	//		VERIFY_ARE_EQUAL(finalMenuItemPosition.X, initialMenuItemPosition.X);
	//		VERIFY_ARE_EQUAL(finalMenuItemPosition.Y, initialMenuItemPosition.Y);

	//		LOG_OUTPUT("Verifying MenuFlyoutItem position was shifted to be completely visible.");
	//		VERIFY_IS_LESS_THAN(finalMenuItemPosition.X, (float)(rootGrid.ActualWidth - menuItem.ActualWidth));
	//		VERIFY_IS_LESS_THAN(finalMenuItemPosition.Y, (float)(rootGrid.ActualHeight - menuItem.ActualHeight));

	//		LOG_OUTPUT("Closing the MenuFlyout.");
	//		menuFlyout.Hide();
	//	});

	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Waiting for MenuFlyout.Closed event.");
	//	await menuFlyoutClosedEvent.WaitForDefault();
	//}

	//[TestMethod] public async Task ValidateSettingKeyboardAcceleratorCreatesDefaultItemKeyboardAcceleratorText()
	//{


	//	await RunOnUIThread(() =>

	//	{
	//		var item = new MenuFlyoutItem();

	//		var keyboardAccelerator = new KeyboardAccelerator();
	//		keyboardAccelerator.Key = Windows.System.VirtualKey.A;
	//		keyboardAccelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
	//		item.KeyboardAccelerators.Add(keyboardAccelerator);

	//		string expectedKeyboardAcceleratorText = "Ctrl+A";

	//		LOG_OUTPUT("Expected keyboard accelerator text: \"%s\"", expectedKeyboardAcceleratorText.Data());
	//		LOG_OUTPUT("Actual keyboard accelerator text:   \"%s\"", item.KeyboardAcceleratorTextOverride.Data());
	//		VERIFY_IS_TRUE(string.CompareOrdinal(expectedKeyboardAcceleratorText, item.KeyboardAcceleratorTextOverride) == 0);
	//	});
	//}

	//[TestMethod] public async Task ValidateSettingKeyboardAcceleratorDoesNotOverrideItemCustomKeyboardAcceleratorText()
	//{


	//	await RunOnUIThread(() =>

	//	{
	//		var item = new MenuFlyoutItem();

	//		string customKeyboardAcceleratorText = "Custom keyboard accelerator text";
	//		item.KeyboardAcceleratorTextOverride = customKeyboardAcceleratorText;

	//		var keyboardAccelerator = new KeyboardAccelerator();
	//		keyboardAccelerator.Key = Windows.System.VirtualKey.A;
	//		keyboardAccelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
	//		item.KeyboardAccelerators.Add(keyboardAccelerator);

	//		LOG_OUTPUT("Expected keyboard accelerator text: \"%s\"", customKeyboardAcceleratorText.Data());
	//		LOG_OUTPUT("Actual keyboard accelerator text:   \"%s\"", item.KeyboardAcceleratorTextOverride.Data());
	//		VERIFY_IS_TRUE(string.CompareOrdinal(customKeyboardAcceleratorText, item.KeyboardAcceleratorTextOverride) == 0);
	//	});
	//}

	//[TestMethod] public async Task ValidateSettingKeyboardAcceleratorCreatesDefaultToggleItemKeyboardAcceleratorText()
	//{


	//	await RunOnUIThread(() =>

	//	{
	//		var item = new ToggleMenuFlyoutItem();

	//		var keyboardAccelerator = new KeyboardAccelerator();
	//		keyboardAccelerator.Key = Windows.System.VirtualKey.A;
	//		keyboardAccelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
	//		item.KeyboardAccelerators.Add(keyboardAccelerator);

	//		string expectedKeyboardAcceleratorText = "Ctrl+A";

	//		LOG_OUTPUT("Expected keyboard accelerator text: \"%s\"", expectedKeyboardAcceleratorText.Data());
	//		LOG_OUTPUT("Actual keyboard accelerator text:   \"%s\"", item.KeyboardAcceleratorTextOverride.Data());
	//		VERIFY_IS_TRUE(string.CompareOrdinal(expectedKeyboardAcceleratorText, item.KeyboardAcceleratorTextOverride) == 0);
	//	});
	//}

	[TestMethod]
	public async Task ValidateSettingKeyboardAcceleratorDoesNotOverrideToggleItemCustomKeyboardAcceleratorText()
	{
		await RunOnUIThread(() =>
		{
			var item = new ToggleMenuFlyoutItem();

			string customKeyboardAcceleratorText = "Custom keyboard accelerator text";
			item.KeyboardAcceleratorTextOverride = customKeyboardAcceleratorText;

			var keyboardAccelerator = new KeyboardAccelerator();
			keyboardAccelerator.Key = Windows.System.VirtualKey.A;
			keyboardAccelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
			item.KeyboardAccelerators.Add(keyboardAccelerator);

			LOG_OUTPUT("Expected keyboard accelerator text: \"%s\"", customKeyboardAcceleratorText);
			LOG_OUTPUT("Actual keyboard accelerator text:   \"%s\"", item.KeyboardAcceleratorTextOverride);
			VERIFY_IS_TRUE(string.CompareOrdinal(customKeyboardAcceleratorText, item.KeyboardAcceleratorTextOverride) == 0);
		});
	}

	[TestMethod]
	public async Task ValidateDefaultKeyboardAcceleratorTextPopulatesFully()
	{
		Grid rootGrid = null;
		MenuFlyout menuFlyout = null;

		var loadedEvent = new Event();
		var menuFlyoutOpenedEvent = new Event();
		var menuFlyoutClosedEvent = new Event();

		var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>(nameof(Grid.Loaded));
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		await RunOnUIThread(() =>
		{
			rootGrid = (Grid)(XamlReader.Load(
			"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
			"  x:Name='root' Background='SlateBlue'> " +
			"  <FlyoutBase.AttachedFlyout> " +
			"    <MenuFlyout> " +
			"      <MenuFlyoutItem Icon='AddFriend' Text='MenuFlyoutItemWithIcon'> " +
			"          <MenuFlyoutItem.KeyboardAccelerators> " +
			"              <KeyboardAccelerator Key='F10' Modifiers='Control' /> " +
			"          </MenuFlyoutItem.KeyboardAccelerators> " +
			"      </MenuFlyoutItem> " +
			"      <MenuFlyoutItem Text='MenuFlyoutItem1'> " +
			"          <MenuFlyoutItem.KeyboardAccelerators> " +
			"              <KeyboardAccelerator Key='F1' Modifiers='Control' /> " +
			"          </MenuFlyoutItem.KeyboardAccelerators> " +
			"      </MenuFlyoutItem> " +
			"      <ToggleMenuFlyoutItem Text='ToggleMenuFlyoutItem'> " +
			"          <MenuFlyoutItem.KeyboardAccelerators> " +
			"              <KeyboardAccelerator Key='F11' Modifiers='Control' /> " +
			"          </MenuFlyoutItem.KeyboardAccelerators> " +
			"      </ToggleMenuFlyoutItem> " +
			"      <MenuFlyoutItem Text='MenuFlyoutItem2'> " +
			"          <MenuFlyoutItem.KeyboardAccelerators> " +
			"              <KeyboardAccelerator Key='F2' Modifiers='Control' /> " +
			"          </MenuFlyoutItem.KeyboardAccelerators> " +
			"      </MenuFlyoutItem> " +
			"    </MenuFlyout> " +
			"  </FlyoutBase.AttachedFlyout> " +
			"</Grid>"));

			loadedRegistration.Attach(rootGrid, (s, e) => { loadedEvent.Set(); });

			menuFlyout = (MenuFlyout)(MenuFlyout.GetAttachedFlyout(rootGrid));
			VERIFY_IS_NOT_NULL(menuFlyout);

			openedRegistration.Attach(menuFlyout, (s, e) =>

				{
					LOG_OUTPUT("MenuFlyout.Opened event raised");
					menuFlyoutOpenedEvent.Set();
				});

			closedRegistration.Attach(menuFlyout, (s, e) =>

				{
					LOG_OUTPUT("MenuFlyout.Closed event raised");
					menuFlyoutClosedEvent.Set();
				});

			TestServices.WindowHelper.WindowContent = rootGrid;
		});

		await TestServices.WindowHelper.WaitForIdle();
		await loadedEvent.WaitForDefault();

		await RunOnUIThread(() =>

		   {
			   menuFlyout.ShowAt(rootGrid);
		   });

		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				var fourthKeyboardAcceleratorTextBlock = (TextBlock)(TreeHelper.GetVisualChildByName(menuFlyout.Items[3], "KeyboardAcceleratorTextBlock"));
				var fourthKeyboardAcceleratorText = fourthKeyboardAcceleratorTextBlock.Text;
				LOG_OUTPUT("Fourth keyboard accelerator text: %s", fourthKeyboardAcceleratorText);
				LOG_OUTPUT("Expected: Ctrl+F2");
				VERIFY_IS_TRUE(string.CompareOrdinal("Ctrl+F2", fourthKeyboardAcceleratorText) == 0);

				menuFlyout.Hide();
			});

		await menuFlyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	//[TestMethod]
	//public async Task ValidateSettingUICommandSetsProperties()
	//{
	//	CommandHelper.ValidateSettingUICommandSetsProperties<MenuFlyoutItem>(
	//		MenuFlyoutItem.CommandProperty,
	//		MenuFlyoutItem.TextProperty,
	//		MenuFlyoutItem.IconProperty);
	//}

	//[TestMethod]
	//public async Task ValidateSettingUICommandDoesNotOverwriteProperties()
	//{
	//	CommandHelper.ValidateSettingUICommandDoesNotOverwriteProperties<MenuFlyoutItem>(
	//		MenuFlyoutItem.CommandProperty,
	//		MenuFlyoutItem.TextProperty,
	//		MenuFlyoutItem.IconProperty);
	//}

	//[TestMethod]
	//public async Task ValidateSettingUICommandSetsToggleProperties()
	//{
	//	CommandHelper.ValidateSettingUICommandSetsProperties<ToggleMenuFlyoutItem>(
	//		ToggleMenuFlyoutItem.CommandProperty,
	//		ToggleMenuFlyoutItem.TextProperty,
	//		ToggleMenuFlyoutItem.IconProperty);
	//}

	//[TestMethod] public async Task ValidateSettingUICommandDoesNotOverwriteToggleProperties()
	//{
	//	CommandHelper.ValidateSettingUICommandDoesNotOverwriteProperties<ToggleMenuFlyoutItem>(
	//		ToggleMenuFlyoutItem.CommandProperty,
	//		ToggleMenuFlyoutItem.TextProperty,
	//		ToggleMenuFlyoutItem.IconProperty);
	//}

	//[TestMethod] public async Task ValidateMenuFlyoutTouchPositionForExclusionRect()
	//{


	//	TestServices.WindowHelper.SetWindowSizeOverride(Size(400, 600));

	//	Grid root = null;
	//	Button buttonTop = null;
	//	Button buttonBottom = null;

	// await RunOnUIThread(() =>

	//	{
	//		root = Grid> (XamlReader.Load(
	//			LR"(<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'

	//					  x: Name = 'root' >

	//					< Button x: Name = 'buttonTop' Content = 'button top' VerticalAlignment = 'Top' />

	//					< Button x: Name = 'buttonBottom' Content = 'button bottom'  VerticalAlignment = 'Bottom' />

	//				</ Grid >)"));


	//		TestServices.WindowHelper.WindowContent = root;

	//		buttonTop = Button> (root.FindName("buttonTop"));
	//		buttonBottom = Button> (root.FindName("buttonBottom"));
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	//	XamlRoot xamlRoot = null;
	// await RunOnUIThread(() =>

	//	{
	//		xamlRoot = root.XamlRoot;
	//	});

	//	LOG_OUTPUT("Setting last input method to Touch");
	//	TestServices.WindowHelper.SetLastInputMethod(Private.Infrastructure.LastInputDeviceType.Touch, xamlRoot);

	//	MenuFlyout menuFlyout = null;
	// await RunOnUIThread(() =>

	//	{
	//		menuFlyout = MenuFlyout> (XamlReader.Load(
	//			"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >"

	//			"    <MenuFlyoutItem>Menu item 1</MenuFlyoutItem>"

	//			"    <MenuFlyoutItem>Menu item 2</MenuFlyoutItem>"

	//			"</MenuFlyout>"));
	//	});

	//	FlyoutShowOptions ^ showOptions = null;

	// await RunOnUIThread(() =>

	//	{
	//		showOptions = new FlyoutShowOptions();
	//		showOptions.Position = Point(0, (float)buttonBottom.ActualHeight);
	//		showOptions.ExclusionRect = Rect(0, 0, (float)buttonBottom.ActualWidth, (float)buttonBottom.ActualHeight);

	//		LOG_OUTPUT("Showing flyout on bottom button");
	//		menuFlyout.ShowAt(buttonBottom, showOptions);
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		// Verify MenuFlyout is above the bottom button.
	//		var menuFlyoutPresenter = GetCurrentPresenter();
	//		var menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(menuFlyoutPresenter));
	//		var buttonBounds = await ControlHelper.GetBounds((FrameworkElement)(buttonBottom));

	//		VERIFY_ARE_EQUAL(menuFlyoutBounds.Bottom, buttonBounds.Top);
	//	});

	//	FlyoutHelper.HideFlyout(menuFlyout);

	// await RunOnUIThread(() =>

	//	{
	//		showOptions = new FlyoutShowOptions();
	//		showOptions.Position = Point(0, (float)buttonTop.ActualHeight);
	//		showOptions.ExclusionRect = Rect(0, 0, (float)buttonTop.ActualWidth, (float)buttonTop.ActualHeight);

	//		LOG_OUTPUT("Showing flyout on top button");
	//		menuFlyout.ShowAt(buttonTop, showOptions);
	//	});
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		// Verify MenuFlyout is below the top button.
	//		var menuFlyoutPresenter = GetCurrentPresenter();
	//		var menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(menuFlyoutPresenter));
	//		var buttonBounds = await ControlHelper.GetBounds((FrameworkElement)(buttonTop));

	//		VERIFY_ARE_EQUAL(menuFlyoutBounds.Top, buttonBounds.Bottom);
	//	});

	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	//[TestMethod] public async Task ValidateShowAtMonitorEdge()
	//{
	//	ValidateShowAtMonitorEdge(1.0f  /*scaleFactor*/);
	//	ValidateShowAtMonitorEdge(1.25f /*scaleFactor*/);
	//	ValidateShowAtMonitorEdge(1.5f  /*scaleFactor*/);
	//	ValidateShowAtMonitorEdge(1.75f /*scaleFactor*/);
	//	ValidateShowAtMonitorEdge(2.25f /*scaleFactor*/);
	//}

	//[TestMethod] public async Task ValidateShowAtMonitorEdge(float scaleFactor)
	//{


	//	LOG_OUTPUT("Setting Window Size to 421x421 and global Scale to %f.", scaleFactor);
	//	Windows.Foundation.Size size(421, 421);
	//	TestServices.WindowHelper.SetWindowSizeOverrideWithScale(size, scaleFactor);

	//	Rect windowBounds;
	//	Rect monitorBounds;

	// await RunOnUIThread(() =>

	//	{
	//		windowBounds = TestServices.WindowHelper.WindowBounds;
	//		LOG_OUTPUT("WindowBounds: %f,%f,%f,%f.", windowBounds.X, windowBounds.Y, windowBounds.Width, windowBounds.Height);

	//		monitorBounds = TestServices.WindowHelper.MonitorBounds;
	//		LOG_OUTPUT("MonitorBounds: %f,%f,%f,%f.", monitorBounds.X, monitorBounds.Y, monitorBounds.Width, monitorBounds.Height);
	//	});

	//	var menuFlyout = await CreateMenuFlyout(FlyoutPlacementMode.Bottom);
	//	VERIFY_IS_NOT_NULL(menuFlyout);

	//	var loadedEvent = new Event();
	//	var loadedRegistration = CreateSafeEventRegistration(Grid, Loaded);
	//	var flyoutOpenedEvent = new Event();
	//	var flyoutOpenedCount = new uint();
	//	var flyoutOpenedRegistration = CreateSafeEventRegistration(MenuFlyout, Opened);
	//	var flyoutClosedEvent = new Event();
	//	var flyoutClosedCount = new uint();
	//	var flyoutClosedRegistration = CreateSafeEventRegistration(MenuFlyout, Closed);

	//	*flyoutOpenedCount = 0;
	//	*flyoutClosedCount = 0;

	// await RunOnUIThread(() =>

	//	{
	//		flyoutOpenedRegistration.Attach(menuFlyout, [flyoutOpenedEvent, flyoutOpenedCount]()

	//		{
	//			LOG_OUTPUT("MenuFlyout.Opened event raised.");
	//			flyoutOpenedEvent.Set();
	//			(*flyoutOpenedCount)++;
	//		});

	//		flyoutClosedRegistration.Attach(menuFlyout, [flyoutClosedEvent, flyoutClosedCount]()

	//		{
	//			LOG_OUTPUT("MenuFlyout.Closed event raised.");
	//			flyoutClosedEvent.Set();
	//			(*flyoutClosedCount)++;
	//		});

	//		var rootPanel = new Grid();

	//		loadedRegistration.Attach(rootPanel, [loadedEvent]()

	//		{
	//			LOG_OUTPUT("Grid.Loaded event raised.");
	//			loadedEvent.Set();
	//		});

	//		LOG_OUTPUT("Setting WindowContent.");
	//		TestServices.WindowHelper.WindowContent = rootPanel;
	//	});

	//	LOG_OUTPUT("Waiting for Loaded event...");
	//	await loadedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	// await RunOnUIThread(() =>

	//	{
	//		Point targetPointClientLogical = { 1, 1 };
	//		Rect availableMonitorBounds = default;
	//		Point screenOffset = default;
	//		Point targetPointScreenPhysical = default;
	//		Rect inputPaneOccludeRectScreenLogical = default;

	//		TestServices.WindowHelper.GetAvailableMonitorBounds(
	//			TestServices.WindowHelper.WindowContent,
	//			targetPointClientLogical,
	//			&availableMonitorBounds,
	//			&screenOffset,
	//			&targetPointScreenPhysical,
	//			&inputPaneOccludeRectScreenLogical);

	//		LOG_OUTPUT("availableMonitorBounds: %f,%f,%f,%f.", availableMonitorBounds.X, availableMonitorBounds.Y, availableMonitorBounds.Width, availableMonitorBounds.Height);
	//		LOG_OUTPUT("screenOffset: %f,%f.", screenOffset.X, screenOffset.Y);
	//		LOG_OUTPUT("targetPointScreenPhysical: %f,%f.", targetPointScreenPhysical.X, targetPointScreenPhysical.Y);
	//		LOG_OUTPUT("inputPaneOccludeRectScreenLogical: %f,%f,%f,%f.", inputPaneOccludeRectScreenLogical.X, inputPaneOccludeRectScreenLogical.Y, inputPaneOccludeRectScreenLogical.Width, inputPaneOccludeRectScreenLogical.Height);

	//		LOG_OUTPUT("Showing MenuFlyout at position %f,%f as a windowed popup.", monitorBounds.Width / scaleFactor, (monitorBounds.Height - 32.0f) / scaleFactor);
	//		Point showAtPosition = { monitorBounds.Width / scaleFactor, (monitorBounds.Height - 32.0f) / scaleFactor };
	//		menuFlyout.ShowAt(null, showAtPosition);
	//	});

	//	LOG_OUTPUT("Waiting for Opened event...");
	//	flyoutawait openedEvent.WaitForDefault();
	//	await TestServices.WindowHelper.WaitForIdle();

	//	LOG_OUTPUT("Hiding MenuFlyout.");
	//	FlyoutHelper.HideFlyout(menuFlyout);
	//}

	[TestMethod]
	public async Task VerifyDependencyPropertyDefaultValues()
	{
		var menuFlyout = await CreateMenuFlyout(FlyoutPlacementMode.Bottom);
		VERIFY_IS_NOT_NULL(menuFlyout);

		var target = await FlyoutHelper.CreateTarget(
			100 /*width*/, 100 /*height*/,
			ThicknessHelper.FromUniformLength(10),
			HorizontalAlignment.Center,
			VerticalAlignment.Top);
		VERIFY_IS_NOT_NULL(target);

		await RunOnUIThread(() =>
		   {
			   var rootPanel = new Grid();
			   rootPanel.Children.Add(target);
			   VERIFY_IS_NOT_NULL(rootPanel);

			   TestServices.WindowHelper.WindowContent = rootPanel;
		   });

		await TestServices.WindowHelper.WaitForIdle();

		FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		   {
			   var presenter = GetMenuFlyoutPresenter(menuFlyout);
			   VERIFY_IS_NOT_NULL(presenter);
			   VERIFY_ARE_EQUAL(true, presenter.IsDefaultShadowEnabled);
		   });

		FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task VerifyLargeNonWindowedMenuIsPositionedCorrectly()
	{
		MenuFlyout flyout = null;
		MenuFlyoutSubItem subItem = null;
		Button flyoutTarget = null;

		var openedEvent = new Event();
		var closedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		await RunOnUIThread(() =>

		   {
			   flyout = new MenuFlyout();
			   flyout.Placement = FlyoutPlacementMode.Right;
			   flyout.ShouldConstrainToRootBounds = true;

			   for (int i = 0; i < 10; i++)
			   {
				   var item = new MenuFlyoutItem();
				   item.Text = "Item";
				   flyout.Items.Add(item);
			   }

			   subItem = new MenuFlyoutSubItem();
			   subItem.Text = "Sub item";
			   flyout.Items.Add(subItem);

			   for (int i = 0; i < 20; i++)
			   {
				   var item = new MenuFlyoutItem();
				   item.Text = "Item";
				   flyout.Items.Add(item);
			   }

			   for (int i = 0; i < 30; i++)
			   {
				   var item = new MenuFlyoutItem();
				   item.Text = "Item";
				   subItem.Items.Add(item);
			   }

			   openedRegistration.Attach(flyout, (s, e) =>
			   {
				   LOG_OUTPUT("MenuFlyout opened.");
				   openedEvent.Set();
			   });

			   closedRegistration.Attach(flyout, (s, e) =>
			   {
				   LOG_OUTPUT("MenuFlyout closed.");
				   closedEvent.Set();
			   });

			   flyoutTarget = new Button();
			   flyoutTarget.Content = "Click for flyout";
			   flyoutTarget.HorizontalAlignment = HorizontalAlignment.Left;
			   flyoutTarget.VerticalAlignment = VerticalAlignment.Center;
			   flyoutTarget.Flyout = flyout;

			   var rootPanel = new Grid();
			   rootPanel.Children.Add(flyoutTarget);
			   TestServices.WindowHelper.WindowContent = rootPanel;
		   });

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Tapping on the button to show the MenuFlyout.");
		TestServices.InputHelper.Tap(flyoutTarget);

		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Tapping on the sub item to show the sub-menu.");
		await TapSubMenuItem(subItem);

		await RunOnUIThread(async () =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot);

			LOG_OUTPUT("There should be two popups: one for the MenuFlyout, and one for its sub-menu.");
			VERIFY_ARE_EQUAL(2, popups.Count);

			var flyoutPopup = popups[1];
			var flyoutPresenter = (FrameworkElement)(flyoutPopup.Child);
			var flyoutBounds = await ControlHelper.GetBounds(flyoutPresenter);

			var flyoutSubMenuPopup = popups[0];
			var flyoutSubMenuPresenter = (FrameworkElement)(flyoutSubMenuPopup.Child);
			var flyoutSubMenuBounds = await ControlHelper.GetBounds(flyoutSubMenuPresenter);

			var xamlRootSize = TestServices.WindowHelper.WindowContent.XamlRoot.Size;
			LOG_OUTPUT("XAML root size:             width=%.0f height=%.0f", xamlRootSize.Width, xamlRootSize.Height);
			LOG_OUTPUT("MenuFlyout bounds:          left=%.0f top=%.0f width=%.0f height=%.0f", flyoutBounds.Left, flyoutBounds.Top, flyoutBounds.Width, flyoutBounds.Height);
			LOG_OUTPUT("MenuFlyout sub-menu bounds: left=%.0f top=%.0f width=%.0f height=%.0f", flyoutSubMenuBounds.Left, flyoutSubMenuBounds.Top, flyoutSubMenuBounds.Width, flyoutSubMenuBounds.Height);

			VERIFY_ARE_EQUAL(0.0, flyoutBounds.Top);
			VERIFY_ARE_EQUAL(xamlRootSize.Height, flyoutBounds.Height);
			VERIFY_ARE_EQUAL(0.0, flyoutSubMenuBounds.Top);
			VERIFY_ARE_EQUAL(xamlRootSize.Height, flyoutSubMenuBounds.Height);
		});

		LOG_OUTPUT("Tapping on the button again to hide the MenuFlyout.");
		TestServices.InputHelper.Tap(flyoutTarget);

		await closedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	private KeyboardAccelerator CreateKeyboardAccelerator(Windows.System.VirtualKey key, Windows.System.VirtualKeyModifiers modifiers)
	{
		var keyboardAccelerator = new KeyboardAccelerator();
		keyboardAccelerator.Key = key;
		keyboardAccelerator.Modifiers = modifiers;
		return keyboardAccelerator;
	}
}
