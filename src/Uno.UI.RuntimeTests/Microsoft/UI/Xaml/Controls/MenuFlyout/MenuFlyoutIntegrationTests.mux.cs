using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
#if HAS_UNO
using Uno.UI.Xaml.Input;
#endif
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls;

[TestClass]
[RequiresFullWindow] // The upstream tests expect window-absolute coordinates.
public class MenuFlyoutIntegrationTests
{
#if HAS_UNO
	private bool? _useWindowedPopups;

	[TestInitialize]
	public void SaveWindowedPopups() => _useWindowedPopups = global::Uno.UI.FeatureConfiguration.Popup.UseWindowedPopups;

	[TestCleanup]
	public void RestoreWindowedPopups() => global::Uno.UI.FeatureConfiguration.Popup.UseWindowedPopups = _useWindowedPopups;
#endif

	private string m_menuCommandParam1;
	private string m_menuCommandParam2;
	private string m_menuCommandParam3;

	public MenuFlyoutIntegrationTests()
	{
		m_menuCommandParam1 = "MenuItem1Command";
		m_menuCommandParam2 = "ToggleMenuItemCommand";
		m_menuCommandParam3 = "SplitMenuItemCommand";
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
	[Ignore("Not applicable: pins the projected shadow rendering mode (RuntimeEnabledFeature ForceProjectedShadowsOnByDefault), which does not exist in Uno; also ignored upstream")]
	public void CanMenuFlyoutOpenCloseProjectedShadow()
	{
		// Verify the MenuFlyout open and close, projected shadow mode.
	}

	[TestMethod]
	[Ignore("Not applicable: pins the DComp drop shadow mode, which does not exist in Uno; also ignored upstream (TODO 36060166)")]
	public void CanMenuFlyoutOpenCloseDropShadow()
	{
		// Verify the MenuFlyout open and close, drop shadow mode.
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
		await FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>

		   {
			   var presenter = GetMenuFlyoutPresenter(menuFlyout);
			   VERIFY_IS_NOT_NULL(presenter);
			   var tag = presenter.GetValue(MenuFlyoutPresenter.TagProperty);
			   VERIFY_IS_NOT_NULL(tag);
			   VERIFY_ARE_EQUAL("presenter_style", tag.ToString());
		   });

		LOG_OUTPUT("VerifyMenuFlyoutPresenterStyle: Execute the Hide.");
		await FlyoutHelper.HideFlyout(menuFlyout);
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

		await FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

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

		await FlyoutHelper.HideFlyout(menuFlyout);

		await RunOnUIThread(() =>
		{
			var style = new Style(typeof(MenuFlyoutPresenter));
			style.Setters.Add(new Setter(MenuFlyoutPresenter.TagProperty, "presenter_style_3"));

			menuFlyout.MenuFlyoutPresenterStyle = style;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var presenter = GetMenuFlyoutPresenter(menuFlyout);
			var tag = presenter.GetValue(MenuFlyoutPresenter.TagProperty);
			VERIFY_ARE_EQUAL("presenter_style_3", tag.ToString());
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
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
		await FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAttachedFlyout);

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("CanAttachedMenuFlyoutShowHide: Execute the Hide.");
		await FlyoutHelper.HideFlyout(menuFlyout);

		LOG_OUTPUT("CanAttachedMenuFlyoutShowHide: Execute ShowAt.");
		await FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("CanAttachedMenuFlyoutShowHide: Execute the Hide.");
		await FlyoutHelper.HideFlyout(menuFlyout);
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
		await FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

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
		await FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

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

	[TestMethod]
	public async Task CanClickSplitMenuFlyoutItem()
	{
		SplitMenuFlyoutItem splitMenuItem = null;

		var splitMenuItemClickEvent = new Event();
		var clickRegistration = CreateSafeEventRegistration<SplitMenuFlyoutItem, RoutedEventHandler>(nameof(SplitMenuFlyoutItem.Click));

		var menuFlyout = await CreateMenuFlyoutWithSplitItems();
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
			splitMenuItem = menuFlyout.Items[3] as SplitMenuFlyoutItem;
			VERIFY_IS_NOT_NULL(splitMenuItem);

			var commandExecute = (object param) =>
			{
				LOG_OUTPUT("CanClickSplitMenuFlyoutItem: Process the delegate!");
			};

			var splitMenuItemCommand = new MenuCommand(commandExecute, true /*canExecute*/, m_menuCommandParam3);
			splitMenuItem.Command = splitMenuItemCommand;
			splitMenuItem.CommandParameter = m_menuCommandParam3;

			clickRegistration.Attach(splitMenuItem, (s, e) =>
			{
				splitMenuItemClickEvent.Set();
			});
		});

		LOG_OUTPUT("CanClickSplitMenuFlyoutItem: Execute the ShowAt.");
		await FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("SplitMenuItem Tap operation to execute menu item command.");
		TestServices.InputHelper.Tap(splitMenuItem);
		await splitMenuItemClickEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			splitMenuItem.Command = null;
			splitMenuItem.CommandParameter = null;
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

	[TestMethod]
	public async Task ValidateShowAtTargetPosition()
	{
		// The rules for positioning the menuflyout when calling ShowAt with Point(X,Y) are as follows:
		//   - The menu should be positioned so that (X,Y) is the Top Left corner of the menu.
		//   - For touch input (X,Y) should be the Bottom Left corner instead.
		//   - If this would place the menu so that it is vertically clipped off-screen we do the opposite of above (i.e. Bottom Left for mouse, Top Left for touch).
		//   - If the menuflyout is too tall for either of these to fit, we align it to the top of the screen.
		// We also adjust in the horizontal direction:
		//   - We try to position the menuflyout so that X is at the Left edge of the menu.
		//   - If this would result in the menu being clipped horizontally, we try position it so that X is at the Right edge.
		//   - If the menuflyout is too wide to position either its Right or Left edge at X, we align to the Left of the screen.
		//   - When in Right-To-Left mode, the logic is the same as the above, with Right and Left swapped.
		//   - When using Pen, if  SPI_GETHANDEDNESS returns "Right", then the menuflyout should show with the right edge at the X location.

		// We test the above logic by choosing values for Point(X,Y), the input mode and the FlowDirection.
		//  - Point(X,Y):
		//      This is the point passed to MenuFlyout ShowAt.
		//      Values for X are chosen to be either near the left edge of the screen, the right edge of the screen, or the center.
		//      Values for Y are chosen to be either near the top of the screen, the bottom of the screen or the center.
		//  - Input Mode:
		//      Either Mouse, Keyboard or Touch
		//      (note, we mostly test with Keyboard and Touch because Mouse input helper doesn't work on phone).
		//  - FlowDirection:
		//      Either LeftToRight or RightToLeft
		//
		// Points that are "near" a particular edge of the screen are such that the point is too close to that edge for the menuflyout to open in that direction
		//
		// For each set of values that we test we specify the expected position of the open flyout:
		//   HorizontalOpenDirection:
		//     OpenRight: The menu should open to the Right, i.e. Point(X,Y) is on the Left edge of the menu.
		//     OpenLeft: The menu should open to the Left, i.e. Point(X,Y) is on the Right edge of the menu.
		//   VerticalOpenDirection:
		//     OpenUp: the menu should open Up, i.e. Point(X,Y) is on the Bottom edge of the menu.
		//     OpenDown: the menu should open Down, i.e. Point(X,Y) is on the Top edge of the menu.

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		Rect windowBounds = default;
		await RunOnUIThread(() =>
		{
			windowBounds = TestServices.WindowHelper.WindowBounds;
		});
		LOG_OUTPUT($"Windows bounds left={windowBounds.Left} top={windowBounds.Top} width={windowBounds.Width} height={windowBounds.Height}");

		float nearTop = 50;
		float nearBottom = (float)(windowBounds.Bottom - 50);
		float verticalCenter = (float)Math.Floor(windowBounds.Y + (windowBounds.Height / 2));
		float nearLeft = 50;
		float nearRight = (float)(windowBounds.Right - 50);
		float horizontalCenter = (float)Math.Floor(windowBounds.X + (windowBounds.Width / 2));

		// Simple Mouse case:
		if (true)
		{
			// Mouse input helper doesn't work on phone or onecore.
			LOG_OUTPUT("-------------");
			LOG_OUTPUT("TEST: Simple Mouse case");
			await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Mouse, FlowDirection.LeftToRight,
				HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);
		}

		// Simple Keyboard case:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Simple Keyboard case");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Keyboard, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Simple Touch case:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Simple Touch case");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Touch, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near bottom, non-touch:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near bottom, non-touch");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, nearBottom), InputMethod.Keyboard, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near bottom, touch:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near bottom, touch");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, nearBottom), InputMethod.Touch, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near top, non-touch:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near top, non-touch");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, nearTop), InputMethod.Keyboard, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near top, touch:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near top, touch");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, nearTop), InputMethod.Touch, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near right:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near right");
		await DoValidateShowAtTargetPosition(new Point(nearRight, verticalCenter), InputMethod.Keyboard, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near left:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near left");
		await DoValidateShowAtTargetPosition(new Point(nearLeft, verticalCenter), InputMethod.Keyboard, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Simple RTL case:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Simple RTL case");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Keyboard, FlowDirection.RightToLeft,
			HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near right, RTL
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near right, RTL");
		await DoValidateShowAtTargetPosition(new Point(nearRight, verticalCenter), InputMethod.Keyboard, FlowDirection.RightToLeft,
			HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near left, RTL
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near left, RTL");
		await DoValidateShowAtTargetPosition(new Point(nearLeft, verticalCenter), InputMethod.Keyboard, FlowDirection.RightToLeft,
			HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Right handed touch case");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Touch, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// RTL touch case:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: RTL touch case");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Touch, FlowDirection.RightToLeft,
			HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// MOCK10_REMOVAL : Reenable with bug 20936312
		// Left handed touch case
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Left handed touch case");
		//await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Touch, FlowDirection.LeftToRight,
		//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);
	}

	[TestMethod]
	// Uno: its "right handed (default)" cases read SPI_GETHANDEDNESS. Other hosts have no such setting, and get the left-handed fallback.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task ValidateShowAtTargetPositionForPen()
	{
		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		Rect windowBounds = default;
		await RunOnUIThread(() =>
		{
			windowBounds = TestServices.WindowHelper.WindowBounds;
		});
		LOG_OUTPUT($"Windows bounds left={windowBounds.Left} top={windowBounds.Top} width={windowBounds.Width} height={windowBounds.Height}");

		float nearTop = 50;
		float nearBottom = (float)(windowBounds.Bottom - 50);
		float verticalCenter = (float)Math.Floor(windowBounds.Y + (windowBounds.Height / 2));
		float nearLeft = 50;
		//float nearRight = windowBounds.Right - 50; // MOCK10_REMOVAL avoid local variable is initialized but not referenced error - Reenable with bug 20936312
		float horizontalCenter = (float)Math.Floor(windowBounds.X + (windowBounds.Width / 2));

		// Right handed (default) Pen cases:

		// Simple Pen case:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Simple Pen case");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near top, pen:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near top, pen");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, nearTop), InputMethod.Pen, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near bottom, pen:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near bottom, pen");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, nearBottom), InputMethod.Pen, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenUp, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// RTL cases, Pen should still open to the left

		// Simple Pen case:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Simple Pen case (RTL)");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
			HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near top, pen:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near top, pen (RTL)");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, nearTop), InputMethod.Pen, FlowDirection.RightToLeft,
			HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		//// MOCK10_REMOVAL : Reenable with bug 20936312
		//// Left handed cases, now menus should open to the right.

		//// Simple Pen case:
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Simple Pen case (left handed)");
		//await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
		//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		//// Simple Pen case:
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Simple Pen case (RTL, left-handed)");
		//await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
		//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		//// Near top, pen:
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Near top, pen (left-handed)");
		//await DoValidateShowAtTargetPosition(new Point(horizontalCenter, nearTop), InputMethod.Pen, FlowDirection.LeftToRight,
		//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near bottom, pen:
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Near bottom, pen (left-handed)");
		//await DoValidateShowAtTargetPosition(new Point(horizontalCenter, nearBottom), InputMethod.Pen, FlowDirection.LeftToRight,
		//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenUp, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// RTL right-handed cases, Pen should still open to the left

		// Simple Pen case:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Simple Pen case (RTL, right-handed)");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
			HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near top, pen:
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near top, pen (RTL, right-handed)");
		await DoValidateShowAtTargetPosition(new Point(horizontalCenter, nearTop), InputMethod.Pen, FlowDirection.RightToLeft,
			HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// Near left, pen, right-handed (should flip to right)
		LOG_OUTPUT("-------------");
		LOG_OUTPUT("TEST: Near left, pen, right-handed -- should flip to right");
		await DoValidateShowAtTargetPosition(new Point(nearLeft, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
			HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		// MOCK10_REMOVAL : Reenable with bug 20936312
		// Near left, pen, left-handed (should flip to left)
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Near right, pen, left-handed -- should flip to left");
		//await DoValidateShowAtTargetPosition(new Point(nearRight, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
		//    HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, false /*disableFullHwndSupport*/);

		//// Explicitly force popups to be not windowed even on desktop using FlyoutBase.ShouldConstrainToRootBounds and run the Pen scenarios
		//// again to exercise the non-windowed codepath.

		//// Simple Pen case:
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Simple Pen case (MOCK: non-HWND path)");
		//await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
		//    HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

		//// RTL cases, Pen should still open to the left

		//// Simple Pen case:
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Simple Pen case (RTL) (MOCK: non-HWND path)");
		//await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
		//    HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

		//// Near top, pen:
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Near top, pen (RTL) (MOCK: non-HWND path)");
		//await DoValidateShowAtTargetPosition(new Point(horizontalCenter, nearTop), InputMethod.Pen, FlowDirection.RightToLeft,
		//    HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

		//// Change handedness to left, now menus should open to the right.

		//// Simple Pen case:
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Simple Pen case (left handed) (MOCK: non-HWND path)");
		//await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
		//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

		//// Simple Pen case:
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Simple Pen case (RTL, left-handed) (MOCK: non-HWND path)");
		//await DoValidateShowAtTargetPosition(new Point(horizontalCenter, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
		//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

		//// Near left, pen, right-handed (should flip to right)
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Near left, pen, right-handed -- should flip to right (MOCK: non-HWND path)");
		//await DoValidateShowAtTargetPosition(new Point(nearLeft, verticalCenter), InputMethod.Pen, FlowDirection.LeftToRight,
		//    HorizontalOpenDirection.OpenRight, VerticalOpenDirection.OpenDown, false /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);

		//// Near left, pen, left-handed (should flip to left)
		//LOG_OUTPUT("-------------");
		//LOG_OUTPUT("TEST: Near right, pen, left-handed -- should flip to left (MOCK: non-HWND path)");
		//DoValidateShowAtPosition(new Point(nearRight, verticalCenter), InputMethod.Pen, FlowDirection.RightToLeft,
		//    HorizontalOpenDirection.OpenLeft, VerticalOpenDirection.OpenDown, true /*mockLeftHandedness*/, true /*disableFullHwndSupport*/);
	}

	private async Task InjectInput(InputMethod inputMethod)
	{
		FrameworkElement rootPanel = null;
		await RunOnUIThread(() =>
		{
			rootPanel = (FrameworkElement)TestServices.WindowHelper.WindowContent;
		});

		// Send some input of the appropriate type to the app.
		// The placement of the menuflyout is affected by the most recently used input device type.
		// Note: it is NOT sufficient at this time to simply set the last input type.  This is becuase the flyout present will explicitly query
		//       the popup window to determine whether the keyboard focus or accelerators are being shown.  If they are, last input type is forced
		//       to keyboard and if they aren't and last input type is keyboard, it will be changed to none.  So, make sure we do real input.
		if (inputMethod == InputMethod.Mouse)
		{
			LOG_OUTPUT("Calling InputHelper->LeftMouseClick");
			TestServices.InputHelper.LeftMouseClick(rootPanel);
		}
		else if (inputMethod == InputMethod.Touch)
		{
			LOG_OUTPUT("Calling InputHelper->Tap");
			TestServices.InputHelper.Tap(rootPanel);
		}
		else if (inputMethod == InputMethod.Keyboard)
		{
			LOG_OUTPUT("Calling KeyboardHelper->PressKeySequence");
			await TestServices.KeyboardHelper.PressKeySequence(" ");
		}
		else if (inputMethod == InputMethod.Pen)
		{
			LOG_OUTPUT("Calling InputHelper->PenTap");
#if HAS_UNO
			TestServices.InputHelper.PenTap(rootPanel);
#endif
		}
		else
		{
			throw new NotImplementedException();
		}
		await TestServices.WindowHelper.WaitForIdle();
	}

	private async Task DoValidateShowAtTargetPosition(
		Point showAtPosition,
		InputMethod inputMethod,
		FlowDirection flowDirection,
		HorizontalOpenDirection expectedHorizontalOpenDirection,
		VerticalOpenDirection expectedVerticalOpenDirection,
		bool mockLeftHandedness,
		bool disableFullHwndSupport)
	{
		/* MOCK10_REMOVAL : Reenable when mock support is available
        typedef Mock10::MockFunction<BOOL NTAPI(UINT, UINT, PVOID, UINT)>::Prototype SystemParametersInfoWPrototype;

        DWORD dwMockHandedness = mockLeftHandedness ? HANDEDNESS_LEFT : HANDEDNESS_RIGHT;

        // Within this scope, SystemParametersInfo is mocked
        Mock10::MockFunction<SystemParametersInfoWPrototype> functionSystemParametersInfo(Mock10::Mock::Function<SystemParametersInfoWPrototype>(SystemParametersInfoW));
        functionSystemParametersInfo.Set(
            [&](UINT uiAction,
                UINT uiParam,
                PVOID pvParam,
                UINT fWinIni) -> BOOL volatile
        {
            if (uiAction == SPI_GETHANDEDNESS)
            {
                *static_cast<LPDWORD>(pvParam) = dwMockHandedness;
                return TRUE;
            }

            return SystemParametersInfoW(uiAction, uiParam, pvParam, fWinIni);
        });
        */

		var menuFlyout = await CreateMenuFlyout();

		FrameworkElement rootPanel = null;
		Rect windowBounds = default;

		await RunOnUIThread(() =>
		{
			rootPanel = (FrameworkElement)TestServices.WindowHelper.WindowContent;
			rootPanel.FlowDirection = flowDirection;
			menuFlyout.XamlRoot = rootPanel.XamlRoot;
			windowBounds = TestServices.WindowHelper.WindowBounds;

			// TODO: Set menuFlyout->ShouldConstrainToRootBounds based on disableFullHwndSupport
			// to handle windowed popups.
#if HAS_UNO
			// Uno: this asserts against the window, which upstream's harness has fill the screen. The property alone won't do: ShowAt with options makes a MenuFlyout windowed regardless.
			global::Uno.UI.FeatureConfiguration.Popup.UseWindowedPopups = false;
#endif
		});
		await TestServices.WindowHelper.WaitForIdle();

		var openedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		openedRegistration.Attach(menuFlyout, (s, e) => { openedEvent.Set(); });

		// Make sure our last input is of the correct type.
		await InjectInput(inputMethod);

		// Show the MenuFlyout:
		await RunOnUIThread(() =>
		{
			menuFlyout.ShowAt(null, showAtPosition);
		});
		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		var flyoutPresenter = FlyoutHelper.GetOpenFlyoutPresenter();
		var menuFlyoutBounds = await ControlHelper.GetBounds(flyoutPresenter);

		LOG_OUTPUT($"Windows bounds left={windowBounds.Left} top={windowBounds.Top} width={windowBounds.Width} height={windowBounds.Height}");
		LOG_OUTPUT($"MenuFlyout bounds left={menuFlyoutBounds.Left} top={menuFlyoutBounds.Top} width={menuFlyoutBounds.Width} height={menuFlyoutBounds.Height}");
		LOG_OUTPUT($"showAtPosition ({showAtPosition.X},{showAtPosition.Y})");

		VERIFY_IS_TRUE(ControlHelper.IsContainedIn(menuFlyoutBounds, windowBounds));

		// Validate Horizontal position:
		if (expectedHorizontalOpenDirection == HorizontalOpenDirection.OpenRight)
		{
			VERIFY_IS_LESS_THAN(menuFlyoutBounds.Left, showAtPosition.X + 1);
			VERIFY_IS_GREATER_THAN(menuFlyoutBounds.Left, showAtPosition.X - 1);
		}
		else if (expectedHorizontalOpenDirection == HorizontalOpenDirection.OpenLeft)
		{
			VERIFY_IS_LESS_THAN(menuFlyoutBounds.Right, showAtPosition.X + 1);
			VERIFY_IS_GREATER_THAN(menuFlyoutBounds.Right, showAtPosition.X - 1);
		}

		// Validate Vertical position:
		if (expectedVerticalOpenDirection == VerticalOpenDirection.OpenDown)
		{
			VERIFY_IS_LESS_THAN(menuFlyoutBounds.Top, showAtPosition.Y + 1);
			VERIFY_IS_GREATER_THAN(menuFlyoutBounds.Top, showAtPosition.Y - 1);
		}
		else if (expectedVerticalOpenDirection == VerticalOpenDirection.OpenUp)
		{
			VERIFY_IS_LESS_THAN(menuFlyoutBounds.Bottom, showAtPosition.Y + 1);
			VERIFY_IS_GREATER_THAN(menuFlyoutBounds.Bottom, showAtPosition.Y - 1);
		}

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	private enum InputMethod
	{
		Mouse,
		Keyboard,
		Touch,
		Pen,
		Gamepad,
		Remote,
	}

	private enum HorizontalOpenDirection
	{
		OpenLeft,
		OpenRight
	}

	private enum VerticalOpenDirection
	{
		OpenUp,
		OpenDown
	}

	[TestMethod]
	public async Task ValidateShowAtTargetPositionRelativeToElement()
	{
		// When a UIElement is passed to MenuFlyout.ShowAt(UIElement, Point), we verify that the Point is transformed from the UIElement's space to
		// the global space for the positioning of the MenuFlyout.


		var menuFlyout = await CreateMenuFlyout();

		Button button1 = null;
		Point showAtPositionRelative = new Point(50, 50); // This is the position we pass to ShowAt. It is relative to button1.
		Point showAtPositionAbsolute = default; // The showAtPositionRelative point in absolute space.
												// We pass showAtPositionRelative to ShowAt and expect the menuflyout to be positioned at showAtPositionAbsolute.

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)XamlReader.Load(
				""""
				<Grid Background="Orange" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x = "http://schemas.microsoft.com/winfx/2006/xaml">
					<Button x:Name = "button1" Content = "Button" Width = "100" Height = "50" HorizontalAlignment = "Left" VerticalAlignment = "Top" Margin = "50, 200, 0, 0" />
				</Grid>
				"""");

			button1 = (Button)(rootPanel.FindName("button1"));

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button1, showAtPositionRelative.X, showAtPositionRelative.Y, true);

		await RunOnUIThread(async () =>
		{
			showAtPositionAbsolute = button1.TransformToVisual(null).TransformPoint(showAtPositionRelative);

			var flyoutPresenter = FlyoutHelper.GetOpenFlyoutPresenter();
			var menuFlyoutBounds = await ControlHelper.GetBounds(flyoutPresenter);

			VERIFY_ARE_EQUAL(menuFlyoutBounds.Left, showAtPositionAbsolute.X);
			VERIFY_ARE_EQUAL(menuFlyoutBounds.Bottom, showAtPositionAbsolute.Y);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task TallMenuFlyoutShouldAlignToTopOfScreen()
	{
		// Upstream only sets the window size when not on desktop.
		var isWindowed = Microsoft.UI.Xaml.Controls.Primitives.Popup.DoesPlatformSupportWindowedPopup();
		if (!isWindowed)
		{
			TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));
		}

		// MenuFlyout will normally align either it's top or bottom edge to the point passed to ShowAt.
		// But if the MenuFlyout is too tall to fit in either of these positions, it aligns to the top of the screen.
		//
		// To test this:
		// We set the height of a menuflyoutitem so that the menuflyout size is greater than half the height of the screen.
		// We call ShowAt with a point in the middle of the screen.
		// This ensures that it can not align either it's top or bottom edge to the given point without getting clipped.
		// We validate that the menuflyout opens aligned to the top of the screen.

		var menuFlyout = await CreateMenuFlyout();

		await RunOnUIThread(() =>
		{
			TestServices.WindowHelper.WindowContent = new Grid();
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Uno: a windowed menu goes by its monitor, and the test window doesn't fill its monitor as upstream's does.
		Rect windowBounds = default;
		await RunOnUIThread(() =>
		{
			windowBounds = isWindowed ? TestServices.WindowHelper.MonitorBounds : TestServices.WindowHelper.WindowBounds;
			menuFlyout.Items[0].Height = windowBounds.Height * 0.75;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, null, windowBounds.X + windowBounds.Width / 2, windowBounds.Y + windowBounds.Height / 2);

		var flyoutPresenter = FlyoutHelper.GetOpenFlyoutPresenter();
		var menuFlyoutBounds = await ControlHelper.GetBounds(flyoutPresenter);

		// Verify that the top of the menuflyout is at the top of the screen.
		VERIFY_ARE_EQUAL(menuFlyoutBounds.Top, isWindowed ? windowBounds.Y : -windowBounds.Y);

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task WideMenuFlyoutShouldAlignToLeftOfScreen()
	{
		// Similar to TallMenuFlyoutShouldAlignToTopOfScreen, if a MenuFlyout is too wide to open at the point passed to ShowAt, it will
		// open aligned to the left instead.

		MenuFlyout menuFlyout = null;
		MenuFlyoutItem menuFlyoutItem = null;

		await RunOnUIThread(() =>
		{
			// We need to set MenuFlyoutPresenter.MaxWidth to infinity, otherwise its default value may prevent us from creating
			// a menuflyout wide enough to hit the scenario we are trying to test.
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' > " +
				"    <Grid.Resources> " +
				"        <MenuFlyout x:Name='menuFlyout'> " +
				"            <MenuFlyout.MenuFlyoutPresenterStyle> " +
				"                <Style TargetType='MenuFlyoutPresenter'> " +
				"                    <Setter Property='MaxWidth' Value='Infinity' /> " +
				"                </Style> " +
				"            </MenuFlyout.MenuFlyoutPresenterStyle> " +
				"            <MenuFlyoutItem x:Name='menuFlyoutItem' Text='MenuFlyoutItem' /> " +
				"        </MenuFlyout> " +
				"    </Grid.Resources> " +
				"</Grid>"));

			// TODO Uno: FindName doesn't reach a flyout in resources, nor its items.
			menuFlyout = (MenuFlyout)rootPanel.Resources["menuFlyout"];
			menuFlyoutItem = (MenuFlyoutItem)menuFlyout.Items[0];
#if HAS_UNO
			// Uno: this asserts against the window, which upstream's harness has fill the screen. The property alone won't do: ShowAt with options makes a MenuFlyout windowed regardless.
			global::Uno.UI.FeatureConfiguration.Popup.UseWindowedPopups = false;
#endif

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		Rect windowBounds = default;
		await RunOnUIThread(() =>
		{
			windowBounds = TestServices.WindowHelper.WindowBounds;
			menuFlyoutItem.Width = windowBounds.Width * 0.75;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, null, windowBounds.Width / 2, windowBounds.Height / 2);

		var flyoutPresenter = FlyoutHelper.GetOpenFlyoutPresenter();
		var menuFlyoutBounds = await ControlHelper.GetBounds(flyoutPresenter);

		// Verify that the left of the menuflyout is at the left of the screen.
		VERIFY_ARE_EQUAL(menuFlyoutBounds.Left, -windowBounds.X);

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

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

	private async Task<MenuFlyout> CreateMenuFlyoutWithSplitItems()
	{
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = new MenuFlyout();

			var item1 = new MenuFlyoutItem();
			item1.Text = "Menu Item 1";
			item1.Tag = "MFI1";
			menuFlyout.Items.Add(item1);

			var item2 = new MenuFlyoutItem();
			item2.Text = "Menu Item 2";
			item2.Tag = "MFI2";
			item2.IsEnabled = false;
			menuFlyout.Items.Add(item2);

			var item3 = new MenuFlyoutSeparator();
			item3.Tag = "MFS1";
			menuFlyout.Items.Add(item3);

			var splitItem = new SplitMenuFlyoutItem();
			splitItem.Text = "Split Menu Item 1";
			splitItem.Tag = "SMFI1";
			splitItem.Width = 250;

			var splitItemSubItem1 = new MenuFlyoutItem();
			splitItemSubItem1.Text = "SubItem 1.1";
			splitItemSubItem1.Tag = "SMFISI1";

			var splitItemSubItem2 = new MenuFlyoutItem();
			splitItemSubItem2.Text = "SubItem 1.2";
			splitItemSubItem2.Tag = "SMFISI2";

			splitItem.Items.Add(splitItemSubItem1);
			splitItem.Items.Add(splitItemSubItem2);

			menuFlyout.Items.Add(splitItem);
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

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateOnlyOneSplitMenuItemIsOpenAtATimeByTouch()
	{
		Button button1 = null;
		IList<MenuFlyoutItemBase> items = null;
		SplitMenuFlyoutItem splitItem = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = null;

		var splitItem1LostFocusEvent = new Event();
		var splitItem1LostFocusRegistration = CreateSafeEventRegistration<SplitMenuFlyoutItem, RoutedEventHandler>(nameof(SplitMenuFlyoutItem.LostFocus));

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split menu item 1'>" +
				"        <MenuFlyoutItem>Menu item 1.1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem>Menu item 1.2</MenuFlyoutItem>" +
				"    </SplitMenuFlyoutItem>" +
				"    <SplitMenuFlyoutItem x:Name='splitItem2' Text='Split menu item 2'>" +
				"        <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>" +
				"    </SplitMenuFlyoutItem>" +
				"</MenuFlyout>"));

			button1 = (Button)(rootPanel.FindName("button1"));
		});

		await ShowMenuFlyout(menuFlyout, button1, -50, 50);

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
		});

		// Get split menu item 1, set up event registration, and tap on its secondary part
		await RunOnUIThread(() =>
		{
			splitItem = items[0] as SplitMenuFlyoutItem;
			splitItem1LostFocusRegistration.Attach(splitItem, (s, e) =>
			{
				splitItem1LostFocusEvent.Set();
			});
		});
		await TapSplitMenuItemSecondary(splitItem);

		// Tap split menu item 2's secondary part and verify that splitItem1 is closed
		await RunOnUIThread(() =>
		{
			splitItem = items[1] as SplitMenuFlyoutItem;
		});
		await TapSplitMenuItemSecondary(splitItem);
		await splitItem1LostFocusEvent.WaitForDefault();

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateSubMenuItemByGamepad()
	{
		await PerformValidateSubMenuItem(InputMethod.Gamepad);
	}

	[TestMethod]
	public async Task ValidateSubMenuItemByKeyboard()
	{
		await PerformValidateSubMenuItem(InputMethod.Keyboard);
	}

	[TestMethod]
	public async Task ValidateSubMenuItemByMouse()
	{
		await PerformValidateSubMenuItem(InputMethod.Mouse);
	}

	[TestMethod]
	public async Task ValidateSubMenuItemByRemote()
	{
		await PerformValidateSubMenuItem(InputMethod.Remote);
	}

	[TestMethod]
	public async Task ValidateSubMenuItemByTouch()
	{
		await PerformValidateSubMenuItem(InputMethod.Touch);
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemByGamepad()
	{
		await PerformValidateSplitMenuItem(InputMethod.Gamepad);
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemByKeyboard()
	{
		await PerformValidateSplitMenuItem(InputMethod.Keyboard);
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemByMouse()
	{
		await PerformValidateSplitMenuItem(InputMethod.Mouse);
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemByTouch()
	{
		await PerformValidateSplitMenuItem(InputMethod.Touch);
	}

	[TestMethod]
	public async Task ValidateTraverseMenuFlyoutItemsByGamepad()
	{
		await PerformValidateTraverseMenuFlyoutItems(InputMethod.Gamepad, true);
		await PerformValidateTraverseMenuFlyoutItems(InputMethod.Gamepad, false);
	}

	[TestMethod]
	public async Task ValidateTraverseMenuFlyoutItemsByKeyboard()
	{
		await PerformValidateTraverseMenuFlyoutItems(InputMethod.Keyboard, true);
		await PerformValidateTraverseMenuFlyoutItems(InputMethod.Keyboard, false);
	}

	[TestMethod]
	public async Task ValidateTraverseSplitMenuFlyoutItemsByGamepad()
	{
		await PerformValidateTraverseSplitMenuFlyoutItems(InputMethod.Gamepad, true);
		await PerformValidateTraverseSplitMenuFlyoutItems(InputMethod.Gamepad, false);
	}

	[TestMethod]
	public async Task ValidateTraverseSplitMenuFlyoutItemsByKeyboard()
	{
		await PerformValidateTraverseSplitMenuFlyoutItems(InputMethod.Keyboard, true);
		await PerformValidateTraverseSplitMenuFlyoutItems(InputMethod.Keyboard, false);
	}

	[TestMethod]
	public async Task ValidateSubMenuItemInPage()
	{
		Button button1 = null;

		Page page = null;
		Canvas rootPanel = null;
		MenuFlyout menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");

		await RunOnUIThread(() =>
		{
			page = TestServices.WindowHelper.SetupSimulatedAppPage();
			rootPanel = (Canvas)(XamlReader.Load(
			 "<Canvas Background='RoyalBlue' " +
			 " xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
			 "    <Button x:Name='button1' Content='Button' Width='100' Height='50' Canvas.Left='50' Canvas.Top='50' />" +
			 "</Canvas>"));
			button1 = (Button)(rootPanel.FindName("button1"));

			page.Content = rootPanel;
			loadedRegistration.Attach(page, (s, e) => { loadedEvent.Set(); });
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button1, -50, 50);
		await NavigateSubMenu(menuFlyout, button1, InputDevice.Keyboard);
		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemInPage()
	{
		Button button1 = null;

		Page page = null;
		Canvas rootPanel = null;
		MenuFlyout menuFlyout = await CreateSplitMenuFlyoutItemsFromXaml();

		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");

		await RunOnUIThread(() =>
		{
			page = TestServices.WindowHelper.SetupSimulatedAppPage();
			rootPanel = (Canvas)(XamlReader.Load(
			 "<Canvas Background='RoyalBlue' " +
			 " xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
			 "    <Button x:Name='button1' Content='Button' Width='100' Height='50' Canvas.Left='50' Canvas.Top='50' />" +
			 "</Canvas>"));
			button1 = (Button)(rootPanel.FindName("button1"));

			page.Content = rootPanel;
			loadedRegistration.Attach(page, (s, e) => { loadedEvent.Set(); });
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button1, -50, 50);
		await NavigateSplitMenu(menuFlyout, button1, InputDevice.Keyboard);
		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateCollapsingResetsVisualState()
	{
		MenuFlyout menuFlyout = null;
		MenuFlyoutItem menuFlyoutItem = null;
		MenuFlyoutSubItem menuFlyoutSubItem = null;
		SplitMenuFlyoutItem splitMenuFlyoutItem = null;

		StackPanel rootPanel = null;
		Button rootButton = null;

		await RunOnUIThread(() =>
		{
			rootPanel = new StackPanel();

			menuFlyout = new MenuFlyout();

			menuFlyoutItem = new MenuFlyoutItem();
			menuFlyoutItem.Text = "Item 1";
			menuFlyout.Items.Add(menuFlyoutItem);

			menuFlyoutSubItem = new MenuFlyoutSubItem();
			menuFlyoutSubItem.Text = "Item 2";
			menuFlyout.Items.Add(menuFlyoutSubItem);

			splitMenuFlyoutItem = new SplitMenuFlyoutItem();
			splitMenuFlyoutItem.Text = "Item 3";
			menuFlyout.Items.Add(splitMenuFlyoutItem);

			rootButton = new Button();
			rootButton.Flyout = menuFlyout;
			rootPanel.Children.Add(rootButton);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, rootButton, 0, 0, false /* forceTapAsPreviousInputMessage */);

		VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(menuFlyoutItem, "CommonStates", "Normal"));

		await RunOnUIThread(() =>
		{
			VisualStateManager.GoToState(menuFlyoutItem, "Pressed", false);
			VisualStateManager.GoToState(menuFlyoutSubItem, "Pressed", false);
			VisualStateManager.GoToState(splitMenuFlyoutItem, "Pressed", false);
		});
		await TestServices.WindowHelper.WaitForIdle();

		VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(menuFlyoutItem, "CommonStates", "Pressed"));

		await RunOnUIThread(() =>
		{
			menuFlyoutItem.Visibility = Visibility.Collapsed;
			menuFlyoutSubItem.Visibility = Visibility.Collapsed;
			splitMenuFlyoutItem.Visibility = Visibility.Collapsed;
		});
		await TestServices.WindowHelper.WaitForIdle();

		VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(menuFlyoutItem, "CommonStates", "Normal"));
		VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(menuFlyoutSubItem, "CommonStates", "Normal"));
		VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(splitMenuFlyoutItem, "CommonStates", "Normal"));
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	private MenuFlyoutSubItem CreateMenuFlyoutSubItem()
	{
		var subItem = new MenuFlyoutSubItem();
		subItem.Text = "Sub Item";

		var item1 = new MenuFlyoutItem();
		item1.Text = "Item1";

		var item2 = new MenuFlyoutItem();
		item2.Text = "Item2";

		var item3 = new MenuFlyoutSeparator();

		var item4 = new ToggleMenuFlyoutItem();
		item4.Text = "Item4";

		subItem.Items.Add(item1);
		subItem.Items.Add(item2);
		subItem.Items.Add(item3);
		subItem.Items.Add(item4);

		return subItem;
	}

	[TestMethod]
	public async Task ValidateSubMenuItemPosition()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(1024, 768));

		IList<MenuFlyoutItemBase> items = null;
		IList<MenuFlyoutItemBase> subItems = null;
		MenuFlyoutSubItem subItem = null;
		Rect windowBounds = default;
		Rect menuFlyoutBounds = default;
		Rect subMenu1tBounds = default;
		Rect subMenu2tBounds = default;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>

		   {
			   menuFlyout = (MenuFlyout)(XamlReader.Load(
				   "<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				   "    <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				   "    <MenuFlyoutSeparator/>" +
				   "    <MenuFlyoutSubItem x:Name='subItem1' Text='sub item4'>" +
				   "        <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				   "        <MenuFlyoutSeparator/>" +
				   "        <MenuFlyoutSubItem  x:Name='subItem2' Text='subItem2'>" +
				   "            <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				   "            <MenuFlyoutSeparator/>" +
				   "            <MenuFlyoutSubItem x:Name='subItem1' Text='subitem3'/>" +
				   "        </MenuFlyoutSubItem>" +
				   "    </MenuFlyoutSubItem>" +
				   "</MenuFlyout>"));

			   windowBounds = TestServices.WindowHelper.WindowBounds;

			   LOG_OUTPUT("Windows bounds left=%f top=%f width=%f height=%f", windowBounds.Left, windowBounds.Top, windowBounds.Width, windowBounds.Height);
		   });

		await ShowMenuFlyout(menuFlyout, null, windowBounds.Width / 4, 50);

		var presenter = await GetCurrentPresenter();

		await RunOnUIThread(async () =>
		{
			menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);
		});

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
		});

		subItem = await GetSubItem(items);
		await TapSubMenuItem(subItem);

		// Verify the sub menu1 position that it must be positioned to the left side of the MenuFlyout.
		presenter = await GetCurrentPresenter();
		await RunOnUIThread(async () =>
		{
			subMenu1tBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("SubMenuFlyout bounds left=%f top=%f width=%f height=%f", subMenu1tBounds.Left, subMenu1tBounds.Top, subMenu1tBounds.Width, subMenu1tBounds.Height);
		});

		VERIFY_IS_TRUE(menuFlyoutBounds.Left < subMenu1tBounds.Left);

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			subItems = subItem.Items;
		});

		await TapSubMenuItem(await GetSubItem(subItems));

		// Verify the sub menu2 position that it must be positioned to the right side of the sub menu2 flyout.
		presenter = await GetCurrentPresenter();
		await RunOnUIThread(async () =>
		{
			subMenu2tBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("SubMenuFlyout bounds left=%f top=%f width=%f height=%f", subMenu2tBounds.Left, subMenu2tBounds.Top, subMenu2tBounds.Width, subMenu2tBounds.Height);
		});
		VERIFY_IS_TRUE(subMenu1tBounds.Left <= subMenu2tBounds.Left);

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemPosition()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(1024, 768));

		IList<MenuFlyoutItemBase> items = null;
		IList<MenuFlyoutItemBase> subItems = null;
		SplitMenuFlyoutItem splitItem = null;
		Rect windowBounds = default;
		Rect menuFlyoutBounds = default;
		Rect subMenu1tBounds = default;
		Rect subMenu2tBounds = default;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				"    <MenuFlyoutSeparator/>" +
				"    <SplitMenuFlyoutItem x:Name='splitItem1' Text='split item4'>" +
				"        <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				"        <MenuFlyoutSeparator/>" +
				"        <SplitMenuFlyoutItem  x:Name='splitItem2' Text='splitItem2'>" +
				"            <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				"            <MenuFlyoutSeparator/>" +
				"            <SplitMenuFlyoutItem x:Name='splitItem3' Text='splititem3'/>" +
				"        </SplitMenuFlyoutItem>" +
				"    </SplitMenuFlyoutItem>" +
				"</MenuFlyout>"));

			windowBounds = TestServices.WindowHelper.WindowBounds;

			LOG_OUTPUT("Windows bounds left=%f top=%f width=%f height=%f", windowBounds.Left, windowBounds.Top, windowBounds.Width, windowBounds.Height);
		});

		await ShowMenuFlyout(menuFlyout, null, windowBounds.Width / 4, 50);

		var presenter = await GetCurrentPresenter();

		await RunOnUIThread(async () =>
		{
			menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);
		});

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
			// SplitMenuFlyoutItem is at index 2 (after item1 and separator)
			splitItem = items[2] as SplitMenuFlyoutItem;
		});

		VERIFY_IS_NOT_NULL(splitItem);
		await TapSplitMenuItemSecondary(splitItem);

		// Verify the split menu1 position that it must be positioned to the right side of the MenuFlyout.
		presenter = await GetCurrentPresenter();
		await RunOnUIThread(async () =>
		{
			subMenu1tBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("SplitMenuFlyout bounds left=%f top=%f width=%f height=%f", subMenu1tBounds.Left, subMenu1tBounds.Top, subMenu1tBounds.Width, subMenu1tBounds.Height);
		});
		VERIFY_IS_TRUE(menuFlyoutBounds.Left < subMenu1tBounds.Left);

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			subItems = splitItem.Items;
			// Nested SplitMenuFlyoutItem is at index 2 (after item1 and separator)
			splitItem = subItems[2] as SplitMenuFlyoutItem;
		});

		VERIFY_IS_NOT_NULL(splitItem);
		await TapSplitMenuItemSecondary(splitItem);

		// Verify the split menu2 position that it must be positioned to the right side of the split menu1 flyout.
		presenter = await GetCurrentPresenter();
		await RunOnUIThread(async () =>
		{
			subMenu2tBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("SplitMenuFlyout2 bounds left=%f top=%f width=%f height=%f", subMenu2tBounds.Left, subMenu2tBounds.Top, subMenu2tBounds.Width, subMenu2tBounds.Height);
		});
		VERIFY_IS_TRUE(subMenu1tBounds.Left <= subMenu2tBounds.Left);

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateNestedSubMenuItemPosition()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(1024, 768));

		Rect windowBounds = default;
		Rect menuFlyoutBounds = default;
		Rect subMenuBounds = default;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = null;
		MenuFlyoutSubItem subItem = null;
		IList<MenuFlyoutItemBase> items = null;
		IList<MenuFlyoutItemBase> subItems = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutSubItem x:Name='item1' Text='item1'>" +
				"        <MenuFlyoutSubItem x:Name='item11' Text='item11'>" +
				"            <MenuFlyoutItem x:Name='item111'>item111</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item112'>item112</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item113'>item113</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item114'>item114</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item115'>item115</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item116'>item116</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item117'>item117</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item118'>item118</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item119'>item119</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11A'>item11A</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11B'>item11B</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11C'>item11C</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11D'>item11D</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11E'>item11E</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11F'>item11F</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11G'>item11G</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11H'>item11H</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11I'>item11I</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11H'>item11J</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11I'>item11K</MenuFlyoutItem>" +
				"        </MenuFlyoutSubItem>" +
				"    </MenuFlyoutSubItem>" +
				"    <MenuFlyoutItem x:Name='item2'>item2</MenuFlyoutItem>" +
				"    <MenuFlyoutItem x:Name='item3'>item3</MenuFlyoutItem>" +
				"</MenuFlyout>"));

			windowBounds = TestServices.WindowHelper.WindowBounds;

			LOG_OUTPUT("Windows bounds left=%f top=%f width=%f height=%f", windowBounds.Left, windowBounds.Top, windowBounds.Width, windowBounds.Height);
			VERIFY_IS_TRUE(windowBounds.Width == 1024);
			VERIFY_IS_TRUE(windowBounds.Height == 768);
		});

		await ShowMenuFlyout(menuFlyout, null, windowBounds.Width / 4, windowBounds.Height);

		var presenter = await GetCurrentPresenter();

		await RunOnUIThread(async () =>
		{
			menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);
		});

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
		});

		subItem = await GetSubItem(items, 0);
		await TapSubMenuItem(subItem);

		presenter = await GetCurrentPresenter();

		await RunOnUIThread(async () =>
		{
			subMenuBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("SubMenuFlyout bounds left=%f top=%f width=%f height=%f", subMenuBounds.Left, subMenuBounds.Top, subMenuBounds.Width, subMenuBounds.Height);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			subItems = subItem.Items;
		});

		await TapSubMenuItem(await GetSubItem(subItems, 0));

		presenter = await GetCurrentPresenter();
		await RunOnUIThread(async () =>
		{
			subMenuBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("SubMenuFlyout bounds left=%f top=%f width=%f height=%f", subMenuBounds.Left, subMenuBounds.Top, subMenuBounds.Width, subMenuBounds.Height);
		});
		// Verify the sub menu is positioned towards the top of the window and not the bottom.
		VERIFY_IS_TRUE(subMenuBounds.Top <= 0);

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateNestedSplitMenuItemPosition()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(1024, 768));

		Rect windowBounds = default;
		Rect menuFlyoutBounds = default;
		Rect splitMenuBounds = default;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitItem = null;
		IList<MenuFlyoutItemBase> items = null;
		IList<MenuFlyoutItemBase> subItems = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <SplitMenuFlyoutItem x:Name='item1' Text='item1'>" +
				"        <SplitMenuFlyoutItem x:Name='item11' Text='item11'>" +
				"            <MenuFlyoutItem x:Name='item111'>item111</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item112'>item112</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item113'>item113</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item114'>item114</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item115'>item115</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item116'>item116</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item117'>item117</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item118'>item118</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item119'>item119</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11A'>item11A</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11B'>item11B</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11C'>item11C</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11D'>item11D</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11E'>item11E</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11F'>item11F</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11G'>item11G</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11H'>item11H</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11I'>item11I</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11J'>item11J</MenuFlyoutItem>" +
				"            <MenuFlyoutItem x:Name='item11K'>item11K</MenuFlyoutItem>" +
				"        </SplitMenuFlyoutItem>" +
				"    </SplitMenuFlyoutItem>" +
				"    <MenuFlyoutItem x:Name='item2'>item2</MenuFlyoutItem>" +
				"    <MenuFlyoutItem x:Name='item3'>item3</MenuFlyoutItem>" +
				"</MenuFlyout>"));

			windowBounds = TestServices.WindowHelper.WindowBounds;

			LOG_OUTPUT("Windows bounds left=%f top=%f width=%f height=%f", windowBounds.Left, windowBounds.Top, windowBounds.Width, windowBounds.Height);
			VERIFY_IS_TRUE(windowBounds.Width == 1024);
			VERIFY_IS_TRUE(windowBounds.Height == 768);
		});

		await ShowMenuFlyout(menuFlyout, null, windowBounds.Width / 4, windowBounds.Height);

		var presenter = await GetCurrentPresenter();

		await RunOnUIThread(async () =>
		{
			menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);
		});

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
			splitItem = items[0] as SplitMenuFlyoutItem;
		});

		VERIFY_IS_NOT_NULL(splitItem);
		await TapSplitMenuItemSecondary(splitItem);

		presenter = await GetCurrentPresenter();

		await RunOnUIThread(async () =>
		{
			splitMenuBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("SplitMenuFlyout bounds left=%f top=%f width=%f height=%f", splitMenuBounds.Left, splitMenuBounds.Top, splitMenuBounds.Width, splitMenuBounds.Height);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			subItems = splitItem.Items;
			splitItem = subItems[0] as SplitMenuFlyoutItem;
		});

		VERIFY_IS_NOT_NULL(splitItem);
		await TapSplitMenuItemSecondary(splitItem);

		presenter = await GetCurrentPresenter();
		await RunOnUIThread(async () =>
		{
			splitMenuBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("SplitMenuFlyout bounds left=%f top=%f width=%f height=%f", splitMenuBounds.Left, splitMenuBounds.Top, splitMenuBounds.Width, splitMenuBounds.Height);
		});
		// Verify the split menu is positioned towards the top of the window and not the bottom.
		VERIFY_IS_TRUE(splitMenuBounds.Top <= 0);

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task ValidateSubMenuItemUIElementTree()
	{
		// The upstream master dumps the visual tree with the two open popups. The master content is asserted explicitly.
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

		Button button1 = null;
		IList<MenuFlyoutItemBase> items = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

		await RunOnUIThread(() =>
		{
			menuFlyout.LightDismissOverlayMode = LightDismissOverlayMode.Off;

			button1 = (Button)(rootPanel.FindName("button1"));
		});

		// Tap on the button to ensure the button's initial focus state
		TestServices.InputHelper.Tap(button1);
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button1, -50, 50);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
		});

		var subItem = await GetSubItem(items);
		await TapSubMenuItem(subItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// Master: Canvas (RoyalBlue) holding the 100x50 button at (50, 50).
			VERIFY_ARE_EQUAL(((SolidColorBrush)rootPanel.Background).Color, Microsoft.UI.Colors.RoyalBlue);
			VERIFY_ARE_EQUAL(Canvas.GetLeft(button1), 50);
			VERIFY_ARE_EQUAL(Canvas.GetTop(button1), 50);
			VERIFY_ARE_EQUAL(button1.Width, 100);
			VERIFY_ARE_EQUAL(button1.Height, 50);

			// Master: two open popups, the sub menu first and then the main menu.
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(rootPanel.XamlRoot);
			VERIFY_ARE_EQUAL(2, popups.Count);
			VERIFY_IS_TRUE(popups[0].IsOpen);
			VERIFY_IS_TRUE(popups[1].IsOpen);

			var subPresenter = (MenuFlyoutPresenter)popups[0].Child;
			VERIFY_ARE_EQUAL(subPresenter.Padding, (Thickness)Application.Current.Resources["MenuFlyoutPresenterThemePadding"]);
			ValidateItems(subPresenter, new (string, Type, bool)[]
			{
				("Menu item 2.1", typeof(MenuFlyoutItem), false),
				("Menu item 2.2", typeof(MenuFlyoutItem), false),
				(null, typeof(MenuFlyoutSeparator), false),
				("Toggle item 2.3", typeof(ToggleMenuFlyoutItem), true),
				(null, typeof(MenuFlyoutSeparator), false),
				("Menu sub item 2.4", typeof(MenuFlyoutSubItem), false),
			});

			var mainPresenter = (MenuFlyoutPresenter)popups[1].Child;
			VERIFY_ARE_EQUAL(mainPresenter.Padding, (Thickness)Application.Current.Resources["MenuFlyoutPresenterThemePadding"]);
			VERIFY_IS_TRUE(double.IsNaN(mainPresenter.Width));
			VERIFY_IS_TRUE(double.IsNaN(mainPresenter.Height));
			ValidateItems(mainPresenter, new (string, Type, bool)[]
			{
				("Menu item 1", typeof(MenuFlyoutItem), false),
				("Menu item 2", typeof(MenuFlyoutItem), false),
				(null, typeof(MenuFlyoutSeparator), false),
				("Toggle item 3", typeof(ToggleMenuFlyoutItem), true),
				(null, typeof(MenuFlyoutSeparator), false),
				("Menu sub item 4", typeof(MenuFlyoutSubItem), false),
				("Menu sub item 5", typeof(MenuFlyoutSubItem), false),
			});
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	private void ValidateItems(MenuFlyoutPresenter presenter, (string Text, Type Type, bool IsChecked)[] expectedItems)
	{
		var items = presenter.Items;
		VERIFY_ARE_EQUAL(expectedItems.Length, items.Count);

		for (int i = 0; i < expectedItems.Length; i++)
		{
			var (text, type, isChecked) = expectedItems[i];
			var item = items[i];
			VERIFY_ARE_EQUAL(type, item.GetType());

			if (item is MenuFlyoutSeparator separator)
			{
				// The master runs on plain generic.xaml (12,4,12,4). Uno's default theme is XamlControlsResources' MenuFlyoutSeparatorThemePadding.
				VERIFY_ARE_EQUAL(separator.Padding, new Thickness(-4, 1, -4, 1));
			}
			else if (item is MenuFlyoutSubItem subItem)
			{
				VERIFY_ARE_EQUAL(subItem.Text, text);
				VERIFY_ARE_EQUAL(subItem.Padding, (Thickness)Application.Current.Resources["MenuFlyoutItemThemePadding"]);
			}
			else
			{
				var menuItem = (MenuFlyoutItem)item;
				VERIFY_ARE_EQUAL(menuItem.Text, text);
				VERIFY_ARE_EQUAL(menuItem.Padding, (Thickness)Application.Current.Resources["MenuFlyoutItemThemePadding"]);
				if (menuItem is ToggleMenuFlyoutItem toggleItem)
				{
					VERIFY_ARE_EQUAL(toggleItem.IsChecked, isChecked);
				}
			}
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task ValidatePopupWindowedPosition()
	{
		await ValidatePopupWindowedPosition(true /* expectWindowedPopup */);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: TestPass:ExcludeOn Desktop, it tests popup positions within a window, which doesn't apply with windowed popups.
	public async Task ValidatePopupWindowedPositionInSimulatedHolographic()
	{
		// TODO Uno: there is no HolographicOverride in the test services.
		await ValidatePopupWindowedPosition(false /* expectWindowedPopup */);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task ValidateWindowedPositionNearMonitorEdge()
	{
		try
		{
			Rect menuFlyoutBounds = default;
			IList<MenuFlyoutItemBase> items = null;
			Canvas rootPanel = await SetupRootPanelForSubMenuTest();

			MenuFlyout menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

			// Uno: the monitor isn't at the origin of the client area, as a test window has a title bar.
			Rect monitor = TestServices.WindowHelper.MonitorBounds;
			LOG_OUTPUT($"Monitor size: {monitor.Width}, {monitor.Height}");

			LOG_OUTPUT($"Calling ShowAt {monitor.Right - 10}, {monitor.Bottom - 50}");
			await ShowMenuFlyout(menuFlyout, null, monitor.Right - 10, monitor.Bottom - 50);

			var presenter = await GetCurrentPresenter();

			menuFlyoutBounds = await ControlHelper.GetBounds(presenter);
			LOG_OUTPUT($"MenuFlyout bounds left={menuFlyoutBounds.Left} top={menuFlyoutBounds.Top} width={menuFlyoutBounds.Width} height={menuFlyoutBounds.Height}");
			VERIFY_IS_LESS_THAN(monitor.Right - 260, menuFlyoutBounds.Left);
			VERIFY_IS_GREATER_THAN(monitor.Right - 240, menuFlyoutBounds.Left);

			await RunOnUIThread(() =>
			{
				items = menuFlyout.Items;
			});

			var subItem = await GetSubItem(items);
			await TapSubMenuItem(subItem);

			// Validate the position
			var subPresenter = await GetCurrentPresenter();
			Rect subMenu = await ControlHelper.GetBounds(subPresenter);
			LOG_OUTPUT($"Sub Menu bounds left={subMenu.Left} top={subMenu.Top} width={subMenu.Width} height={subMenu.Height}");
			VERIFY_IS_LESS_THAN(monitor.Right - 540, subMenu.Left);
			VERIFY_IS_GREATER_THAN(monitor.Right - 440, subMenu.Left);

			await FlyoutHelper.HideFlyout(menuFlyout);
		}
		finally
		{
			TestServices.WindowHelper.ResetWindowContentAndWaitForIdle();
		}
	}

	private async Task ValidatePopupWindowedPosition(bool expectWindowedPopup)
	{
		try
		{
			// If we're expecting a windowed popup, we'll set the desktop window size
			// in order to test the popup breaking out of the bounds of the desktop window.
			// Otherwise, we'll just set the window size in XAML.
			if (!expectWindowedPopup)
			{
				TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));
			}

			Rect menuFlyoutBounds = default;
			IList<MenuFlyoutItemBase> items = null;
			Canvas rootPanel = await SetupRootPanelForSubMenuTest();

			if (expectWindowedPopup)
			{
				// TODO Uno: there is no SetDesktopWindowSize in the test services, the window override is the closest equivalent.
				TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));
				await TestServices.WindowHelper.WaitForIdle();
			}

			MenuFlyout menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

			await ShowMenuFlyout(menuFlyout, null, 400, 280);

			var presenter = await GetCurrentPresenter();

			menuFlyoutBounds = await ControlHelper.GetBounds(presenter);
			LOG_OUTPUT($"MenuFlyout bounds left={menuFlyoutBounds.Left} top={menuFlyoutBounds.Top} width={menuFlyoutBounds.Width} height={menuFlyoutBounds.Height}");

			if (expectWindowedPopup)
			{
				VERIFY_IS_TRUE(menuFlyoutBounds.Left == 400);
			}
			else
			{
				// It is not at 400 because the menu needs to shift left to fit inside the app window.
				// This is similar for the checks below that do not expectWindowedPopup, the popup needs
				// to be adjusted to fit in the windowed dimensions.
				VERIFY_IS_FALSE(menuFlyoutBounds.Left == 400);
			}

			await RunOnUIThread(() =>
			{
				items = menuFlyout.Items;
			});

			var subItem = await GetSubItem(items);
			await TapSubMenuItem(subItem);

			// Validate the position
			var subPresenter = await GetCurrentPresenter();
			Rect subMenu = await ControlHelper.GetBounds(subPresenter);
			LOG_OUTPUT($"Sub Menu bounds left={subMenu.Left} top={subMenu.Top} width={subMenu.Width} height={subMenu.Height}");

			if (expectWindowedPopup)
			{
				VERIFY_IS_TRUE(subMenu.Left > 550);
				VERIFY_IS_TRUE(subMenu.Top + subMenu.Bottom > menuFlyoutBounds.Top + subMenu.Bottom);
			}
			else
			{
				VERIFY_IS_FALSE(subMenu.Left > 550);
			}

			await FlyoutHelper.HideFlyout(menuFlyout);
		}
		finally
		{
			if (expectWindowedPopup)
			{
				// TODO Uno: there is no MaximizeDesktopWindow in the test services.
				TestServices.WindowHelper.ResetWindowSizeOverride();
				await TestServices.WindowHelper.WaitForIdle();
			}

			TestServices.WindowHelper.ResetWindowContentAndWaitForIdle();
		}
	}

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: TestPass:ExcludeOn Desktop, windowed popups make this test moot.
	public async Task ValidateSubMenuPositionWithinWindow()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

		Button button = null;
		MenuFlyout menuFlyout = null;
		IList<MenuFlyoutItemBase> items = null;

		var menuFlyoutOpenedEvent = new Event();
		var menuFlyoutClosedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		await RunOnUIThread(async () =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' > " +
				"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Center' FontSize='20'> " +
				"    <Button.Flyout> " +
				"      <MenuFlyout Placement='Bottom'> " +
				"        <MenuFlyoutItem Text='Copy' /> " +
				"        <MenuFlyoutItem Text='Paste' /> " +
				"        <MenuFlyoutSeparator Width='300' /> " +
				"        <ToggleMenuFlyoutItem FontSize='30' Text='Cut' IsChecked='True' /> " +
				"        <MenuFlyoutSeparator Width='300' /> " +
				"        <MenuFlyoutSubItem x:Name='subItem1' Text='Menu sub item 4'>" +
				"          <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>" +
				"          <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>" +
				"          <MenuFlyoutSeparator/>" +
				"          <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 2.3</ToggleMenuFlyoutItem>" +
				"          <MenuFlyoutSeparator/>" +
				"          <MenuFlyoutSubItem  x:Name='subItem2' Text='Menu sub item 2.4'>" +
				"            <MenuFlyoutItem>Menu item 3.1</MenuFlyoutItem>" +
				"            <MenuFlyoutItem>Menu item 3.2</MenuFlyoutItem>" +
				"            <MenuFlyoutSeparator/>" +
				"            <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 3.3</ToggleMenuFlyoutItem>" +
				"            <MenuFlyoutSeparator/>" +
				"          </MenuFlyoutSubItem>" +
				"         </MenuFlyoutSubItem>" +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});

			closedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutClosedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>

			{
				items = menuFlyout.Items;
			});

		var subItem = await GetSubItem(items);
		await TapSubMenuItem(subItem);

		var subPresenter = await GetCurrentPresenter();
		await RunOnUIThread(async () =>
		{
			Rect subMenu = await ControlHelper.GetBounds(subPresenter);
			LOG_OUTPUT("Sub Menu bounds left=%f top=%f width=%f height=%f", subMenu.Left, subMenu.Top, subMenu.Width, subMenu.Height);

			VERIFY_IS_TRUE(subMenu.Left + subMenu.Width <= 400);
			VERIFY_IS_TRUE(subMenu.Top + subMenu.Height <= 600);
		});

		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});

		await menuFlyoutClosedEvent.WaitForDefault();
	}

	private async Task PerformValidateSubMenuItem(InputMethod inputMethod)
	{


		Button button1 = null;
		IList<MenuFlyoutItemBase> items = null;
		MenuFlyoutSubItem subItem = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

		await RunOnUIThread(() =>
		{
			button1 = (Button)(rootPanel.FindName("button1"));
		});

		await ShowMenuFlyout(menuFlyout, button1, -50, 50);

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
		});

		subItem = await GetSubItem(items);

		switch (inputMethod)
		{
			case InputMethod.Touch:
				await TapSubMenuItem(subItem);
				break;
			case InputMethod.Mouse:
				await MoveToSubMenuItem(subItem);
				break;
			case InputMethod.Keyboard:
				await NavigateSubMenu(menuFlyout, button1, InputDevice.Keyboard);
				break;
			case InputMethod.Gamepad:
				await NavigateSubMenu(menuFlyout, button1, InputDevice.Gamepad);
				break;
		}

		if (inputMethod == InputMethod.Mouse)
		{
			var presenter = await GetCurrentPresenter();

			// Mouse over on the sub menu presenter and out of sub menu
			Private.Infrastructure.TestServices.InputHelper.MoveMouse(presenter);
			Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(0, 400));

			// Mouse over on the main menu presenter and out of main menu flyout to close it
			Private.Infrastructure.TestServices.InputHelper.MoveMouse(subItem);
			Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(0, 400));
		}

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	private async Task PerformValidateSplitMenuItem(InputMethod inputMethod)
	{
		Button button1 = null;
		IList<MenuFlyoutItemBase> items = null;
		SplitMenuFlyoutItem splitItem = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = await CreateSplitMenuFlyoutItemsFromXaml();

		await RunOnUIThread(() =>
		{
			button1 = (Button)(rootPanel.FindName("button1"));
		});

		await ShowMenuFlyout(menuFlyout, button1, -50, 50);

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
			// Get the second SplitMenuFlyoutItem (index 6) which has a submenu
			// Structure: MenuFlyoutItem(0), MenuFlyoutItem(1), Separator(2), ToggleMenuFlyoutItem(3),
			// Separator(4), SplitMenuFlyoutItem(5), SplitMenuFlyoutItem with submenu(6)
			splitItem = (SplitMenuFlyoutItem)(items[6]);
		});

		switch (inputMethod)
		{
			case InputMethod.Touch:
				// Tap the secondary button to open submenu
				await TapSplitMenuItemSecondary(splitItem);
				break;
			case InputMethod.Mouse:
				// Move to and click the secondary button to open submenu
				await MoveToSplitMenuItemSecondary(splitItem);
				break;
			case InputMethod.Keyboard:
				await NavigateSplitMenu(menuFlyout, button1, InputDevice.Keyboard);
				break;
			case InputMethod.Gamepad:
				await NavigateSplitMenu(menuFlyout, button1, InputDevice.Gamepad);
				break;
		}

		if (inputMethod == InputMethod.Mouse)
		{
			var presenter = await GetCurrentPresenter();

			// Mouse over on the split menu presenter and out of sub menu
			Private.Infrastructure.TestServices.InputHelper.MoveMouse(presenter);
			Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(0, 400));

			// Mouse over on the main menu presenter and out of main menu flyout to close it
			Private.Infrastructure.TestServices.InputHelper.MoveMouse(splitItem);
			Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(0, 400));
		}

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	private async Task PerformValidateTraverseMenuFlyoutItems(InputMethod inputMethod, bool goDownFirst)
	{
		Button button = null;
		MenuFlyout menuFlyout = await CreateMenuFlyout();
		IList<MenuFlyoutItemBase> items = null;
		int itemsSize = 0;
		var loadedEvent = new Event();
		var gotFocusEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>("Loaded");
		var buttonGotFocusRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>("GotFocus");

		RoutedEventHandler gotFocusHandler = null;
		List<SafeEventRegistration<Control, RoutedEventHandler>> gotFocusRegistrations = new();

		// When a MenuFlyout is opened, the first focusable element will receive focus. So regardless of which direction we attempt to traverse the list
		// the fist focused element will be MFI1.
		// For list of size n, we keep going in one direction for n+1 times and then we go in the opposite direction for n+1 times.
		// If we're using the keyboard, which allows wrapping, then we'll see 2*(n+1) focus changes, since we'll be wrapping along the way.
		// On the other hand, if we're using gamepad or remote, wrapping is disabled.  In this circumstance,
		// if we start by traversing down this will mean we transition focus twice, and so the expectedFocusSequence with have three tags,
		// whereas if we start by trying to navigate up, we will see no change in focus because the first element will already have focus
		// and menu flyout does not allow focus looping with those input methods. For this reason, you will see only two tags in that expected focus sequence.
		// This is because the number of focusable items is 2 (MFI1 - MenuFlyoutItem1, MFSI1 - MenuFlyoutSubItem1) whereas the
		// other 2 items in this MenuFlyout are, a disabled MenuFlyoutItem and a MenuFlyoutSeparator which do not get focus.
		string expectedFocusSequence =
			inputMethod == InputMethod.Keyboard ?
				"[MFI1][MFSI1][MFI1][MFSI1][MFI1][MFSI1][MFI1][MFSI1][MFI1][MFSI1][MFI1]" :
				goDownFirst ? "[MFI1][MFSI1][MFI1]" : "[MFI1][MFSI1]";
		string focusSequence = "";

		await RunOnUIThread(async () =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
			 """
			
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
			<Button Content="Initial focus button" />
			<Button x:Name="button" Content = "Button" Width = "100" Height = "50" />
			</Grid>
			
			"""));


			TestServices.WindowHelper.WindowContent = rootPanel;

			button = (Button)(rootPanel.FindName("button"));
			button.Flyout = menuFlyout;

			items = menuFlyout.Items;
			itemsSize = items.Count;

			gotFocusHandler = new RoutedEventHandler((object sender, RoutedEventArgs args) =>
			 {
				 focusSequence += "[" + ((FrameworkElement)(sender)).Tag + "]";
			 });

			for (int i = 0; i < itemsSize; i++)
			{
				var item = (Control)(items[i]);
				var gotFocusRegistration = CreateSafeEventRegistration<Control, RoutedEventHandler>("GotFocus");
				gotFocusRegistration.Attach(item, gotFocusHandler);
				gotFocusRegistrations.Add(gotFocusRegistration);
			}

			loadedRegistration.Attach(rootPanel, (s, e) => { loadedEvent.Set(); });
			buttonGotFocusRegistration.Attach(button, (s, e) => { gotFocusEvent.Set(); });

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			button.Focus(FocusState.Keyboard);
		});

		await gotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		InputDevice inputDevice = InputDevice.Gamepad;
		switch (inputMethod)
		{
			case InputMethod.Gamepad:
				inputDevice = InputDevice.Gamepad;
				break;
			case InputMethod.Keyboard:
				inputDevice = InputDevice.Keyboard;
				break;
		}

		await CommonInputHelper.Accept(inputDevice);

		// Go in one direction, for the length of the list + 1.
		for (uint i = 0; i < itemsSize + 1; i++)
		{
			if (goDownFirst)
			{
				await CommonInputHelper.Down(inputDevice);
			}
			else
			{
				await CommonInputHelper.Up(inputDevice);
			}
			await TestServices.WindowHelper.WaitForIdle();
		}

		// Go in the opposite direction, for the length of the list + 1.
		for (uint i = 0; i < itemsSize + 1; i++)
		{
			if (goDownFirst)
			{
				await CommonInputHelper.Up(inputDevice);
			}
			else
			{
				await CommonInputHelper.Down(inputDevice);
			}
			await TestServices.WindowHelper.WaitForIdle();
		}

		LOG_OUTPUT("Expected focus sequence: %s", expectedFocusSequence);
		LOG_OUTPUT("Actual focus sequence: %s", focusSequence);
		VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);

		await CommonInputHelper.Cancel(inputDevice);
	}

	private async Task PerformValidateTraverseSplitMenuFlyoutItems(InputMethod inputMethod, bool goDownFirst)
	{
		Button button = null;
		MenuFlyout menuFlyout = await CreateMenuFlyoutWithSplitItems();
		IList<MenuFlyoutItemBase> items = null;
		int itemsSize = 0;

		var loadedEvent = new Event();
		var gotFocusEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>("Loaded");
		var buttonGotFocusRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>("GotFocus");

		RoutedEventHandler gotFocusHandler = null;
		List<SafeEventRegistration<Control, RoutedEventHandler>> gotFocusRegistrations = new();

		// When a MenuFlyout with SplitMenuFlyoutItems is opened, the first focusable element will receive focus.
		// SplitMenuFlyoutItem has two focusable parts: primary button and secondary button.
		// When navigating down focus moves to a SplitMenuFlyoutItem's Primary Button first, then to its Secondary Button.
		// When navigating up, focus moves to a SplitMenuFlyoutItem's Secondary Button first, then to its Primary Button.
		// And then on the next navigation it moves to the next or previous menu items respectively.
		// Primary button is tagged as SMFI1-P, and secondary button is tagged as SMFI1-S.
		//
		// For a list with items: MFI1 (focusable), MFI2 (disabled), MFS1 (separator), SMFI1 (split item with 2 parts)
		// The focusable items are: MFI1, SMFI1-P, SMFI1-S (3 focusable elements)
		//
		// For keyboard (which allows wrapping):
		// - Going down from MFI1 -> SMFI1-P -> SMFI1-S -> (wrap) MFI1 -> ...
		// - Going up from MFI1 -> SMFI1-S -> SMFI1-P -> (wrap) MFI1 -> ...
		//
		// For gamepad/remote (no wrapping):
		// - Starting at MFI1, going down: MFI1 -> SMFI1-P -> SMFI1-S (stops)
		// - Starting at MFI1, going up: MFI1 (stops, no wrap)
		string expectedFocusSequence =
			inputMethod == InputMethod.Keyboard ?
				(goDownFirst ? "[MFI1][SMFI1-P][SMFI1-S][MFI1][SMFI1-P][SMFI1-S][SMFI1-P][MFI1][SMFI1-S][SMFI1-P][MFI1]" :
							   "[MFI1][SMFI1-S][SMFI1-P][MFI1][SMFI1-S][SMFI1-P][SMFI1-S][MFI1][SMFI1-P][SMFI1-S][MFI1]") :
				(goDownFirst ? "[MFI1][SMFI1-P][SMFI1-S][SMFI1-P][MFI1]" : "[MFI1][SMFI1-P][SMFI1-S]");
		string focusSequence = "";

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
			 """
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Button Content="Initial focus button" />
				<Button x:Name="button" Content="Button" Width="100" Height="50"/>
			</Grid>
			"""));
			TestServices.WindowHelper.WindowContent = rootPanel;

			button = (Button)(rootPanel.FindName("button"));
			button.Flyout = menuFlyout;

			items = menuFlyout.Items;
			itemsSize = items.Count;

			gotFocusHandler = new RoutedEventHandler((object sender, RoutedEventArgs args) =>
			{
				var tag = ((FrameworkElement)(sender)).Tag;
				if (tag != null)
				{
					focusSequence += "[" + tag.ToString() + "]";
				}
			});

			// Using itemsSize-1 to skip the last SplitMenuFlyoutItem which has sub parts.
			// And we want to assign focus handlers to those parts and not the complete control.
			for (int i = 0; i < itemsSize - 1; i++)
			{
				var item = (Control)(items[i]);
				var gotFocusRegistration = CreateSafeEventRegistration<Control, RoutedEventHandler>("GotFocus");
				gotFocusRegistration.Attach(item, gotFocusHandler);
				gotFocusRegistrations.Add(gotFocusRegistration);
			}

			loadedRegistration.Attach(rootPanel, (s, e) => { loadedEvent.Set(); });
			buttonGotFocusRegistration.Attach(button, (s, e) => { gotFocusEvent.Set(); });

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			button.Focus(FocusState.Keyboard);
		});

		await gotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		InputDevice inputDevice = InputDevice.Gamepad;
		switch (inputMethod)
		{
			case InputMethod.Gamepad:
				inputDevice = InputDevice.Gamepad;
				break;
			case InputMethod.Keyboard:
				inputDevice = InputDevice.Keyboard;
				break;
		}

		await CommonInputHelper.Accept(inputDevice);
		await TestServices.WindowHelper.WaitForIdle();

		// After the menu is opened, find and register focus handlers for split item buttons
		await RunOnUIThread(() =>
		{
			var item = (Control)(items[itemsSize - 1]);
			var splitItem = item as SplitMenuFlyoutItem;
			if (splitItem != null)
			{
				// Now that the menu is shown, the visual tree should be loaded
				// splitItem->ApplyTemplate();
				// splitItem->UpdateLayout();

				// Find the primary button (usually named "PrimaryButton" or similar)
				var primaryButton = TreeHelper.GetVisualChildByName(splitItem, "PrimaryButton") as Button;
				if (primaryButton != null)
				{
					primaryButton.Tag = splitItem.Tag + "-P";
					var primaryGotFocusRegistration = CreateSafeEventRegistration<Control, RoutedEventHandler>("GotFocus");
					primaryGotFocusRegistration.Attach(primaryButton, gotFocusHandler);
					gotFocusRegistrations.Add(primaryGotFocusRegistration);
				}

				// Find the secondary button (usually named "SecondaryButton" or similar)
				var secondaryButton = TreeHelper.GetVisualChildByName(splitItem, "SecondaryButton") as Button;
				if (secondaryButton != null)
				{
					secondaryButton.Tag = splitItem.Tag + "-S";
					var secondaryGotFocusRegistration = CreateSafeEventRegistration<Control, RoutedEventHandler>("GotFocus");
					secondaryGotFocusRegistration.Attach(secondaryButton, gotFocusHandler);
					gotFocusRegistrations.Add(secondaryGotFocusRegistration);
				}
			}
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Go in one direction, for the length of itemsSize + 1.
		for (uint i = 0; i < itemsSize + 1; i++)
		{
			if (goDownFirst)
			{
				await CommonInputHelper.Down(inputDevice);
			}
			else
			{
				await CommonInputHelper.Up(inputDevice);
			}
			await TestServices.WindowHelper.WaitForIdle();
		}

		// Go in the opposite direction, for the length of itemsSize + 1.
		for (uint i = 0; i < itemsSize + 1; i++)
		{
			if (goDownFirst)
			{
				await CommonInputHelper.Up(inputDevice);
			}
			else
			{
				await CommonInputHelper.Down(inputDevice);
			}
			await TestServices.WindowHelper.WaitForIdle();
		}

		LOG_OUTPUT("Expected focus sequence: %s", expectedFocusSequence);
		LOG_OUTPUT("Actual focus sequence: %s", focusSequence);
		VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);

		await CommonInputHelper.Cancel(inputDevice);
	}

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

	// Navigate in and out of submenu using both directional and Space/Escape keys.
	// Also test Gamepad functionality (using the internal Gamepad . kb mapping).
	private async Task NavigateSubMenu(
		MenuFlyout menuFlyout,
		Button button,
		InputDevice device)
	{
		MenuFlyoutPresenter menuFlyoutPresenter = null;

		var lostFocusEvent = new Event();
		var lostFocusRegistration = CreateSafeEventRegistration<MenuFlyoutPresenter, RoutedEventHandler>(nameof(MenuFlyoutPresenter.LostFocus));

		var menuFlyoutClosedEvent = new Event();
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		await RunOnUIThread(async () =>
		{
			menuFlyoutPresenter = await GetCurrentPresenter();

			lostFocusRegistration.Attach(
			menuFlyoutPresenter,
			(s, e) =>
				{
					lostFocusEvent.Set();
				});

			closedRegistration.Attach(
				menuFlyout,
				(s, e) =>
				{
					menuFlyoutClosedEvent.Set();
				});

		});

		// Go down four times to give a focus to the submenu item
		// from the first menu item.
		for (int i = 0; i < 4; i++)
		{
			await CommonInputHelper.Down(device);
		}

		// Expand/collapse the submenu using right and left.
		await CommonInputHelper.Right(device);
		await CommonInputHelper.Down(device);
		await CommonInputHelper.Left(device);

		await lostFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Expand/collapse the submenu using accept and cancel.
		await CommonInputHelper.Accept(device);
		await CommonInputHelper.Down(device);
		await CommonInputHelper.Cancel(device);

		await lostFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Close the top-level menu (which will close the MenuFlyout itself)
		await CommonInputHelper.Cancel(device);

		await menuFlyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Opened flyout expected by caller
		await ShowMenuFlyout(menuFlyout, button, -50, 50);
	}

	private async Task MoveToSubMenuItem(MenuFlyoutSubItem subItem)
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

		Private.Infrastructure.TestServices.InputHelper.MoveMouse(subItem);
		Private.Infrastructure.TestServices.InputHelper.LeftMouseClick(subItem);

		await TestServices.WindowHelper.WaitForIdle();

		await lostFocusEvent.WaitForDefault();

		await TestServices.WindowHelper.WaitForIdle();
	}

	private async Task TapSplitMenuItemPrimary(SplitMenuFlyoutItem splitItem)
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

		// Tap on the left side (primary button area) - approximately 35% from the left, 50% from the top
		await global::Uno.UI.RuntimeTests.MUX.Helpers.InputHelper.TapAtPercent(splitItem, 0.35f, 0.5f);

		await lostFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	private async Task TapSplitMenuItemSecondary(SplitMenuFlyoutItem splitItem)
	{
		MenuFlyoutPresenter menuFlyoutPresenter = null;
		Button secondaryButton = null;

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

			// Find the secondary button in the visual tree
			secondaryButton = TreeHelper.GetVisualChildByName(splitItem, "SecondaryButton") as Button;
			VERIFY_IS_NOT_NULL(secondaryButton);
		});

		// Tap on the secondary button
		TestServices.InputHelper.Tap(secondaryButton);

		await lostFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	private async Task MoveToSplitMenuItemPrimary(SplitMenuFlyoutItem splitItem)
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

		// Move to and click on the left side (primary button area) - approximately 35% from the left
		var primaryPosition = await global::Uno.UI.RuntimeTests.MUX.Helpers.InputHelper.GetElementPosition(splitItem, 0.35f, 0.5f);

		Private.Infrastructure.TestServices.InputHelper.MoveMouse(primaryPosition);
		Private.Infrastructure.TestServices.InputHelper.LeftMouseClick(splitItem);

		await TestServices.WindowHelper.WaitForIdle();

		await lostFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	private async Task MoveToSplitMenuItemSecondary(SplitMenuFlyoutItem splitItem)
	{
		MenuFlyoutPresenter menuFlyoutPresenter = null;
		Button secondaryButton = null;

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

			// Find the secondary button in the visual tree
			secondaryButton = TreeHelper.GetVisualChildByName(splitItem, "SecondaryButton") as Button;
			VERIFY_IS_NOT_NULL(secondaryButton);
		});

		// Move to and click on the secondary button
		Private.Infrastructure.TestServices.InputHelper.MoveMouse(secondaryButton);
		Private.Infrastructure.TestServices.InputHelper.LeftMouseClick(secondaryButton);

		await TestServices.WindowHelper.WaitForIdle();

		await lostFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	// Navigate in and out of split menu flyout item's submenu using both directional and Space/Escape keys.
	// Also test Gamepad functionality (using the internal Gamepad -> kb mapping).
	// This method assumes the MenuFlyout is created with CreateSplitMenuFlyoutItemsFromXaml which contains
	// SplitMenuFlyoutItem controls with submenus. It navigates to a SplitMenuFlyoutItem and tests
	// expanding/collapsing its submenu using keyboard/gamepad inputs.
	private async Task NavigateSplitMenu(
		MenuFlyout menuFlyout,
		Button button,
		InputDevice device)
	{
		MenuFlyoutPresenter menuFlyoutPresenter = null;

		var lostFocusEvent = new Event();
		var lostFocusRegistration = CreateSafeEventRegistration<MenuFlyoutPresenter, RoutedEventHandler>(nameof(MenuFlyoutPresenter.LostFocus));

		var menuFlyoutClosedEvent = new Event();
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		await RunOnUIThread(async () =>
		{
			menuFlyoutPresenter = await GetCurrentPresenter();

			lostFocusRegistration.Attach(
				menuFlyoutPresenter,
				(s, e) =>
				{
					lostFocusEvent.Set();
				});

			closedRegistration.Attach(
				menuFlyout,
				(s, e) =>
				{
					menuFlyoutClosedEvent.Set();
				});
		});

		// Navigate down to a SplitMenuFlyoutItem that has a submenu
		// The exact number of Down presses will depend on the menu structure from CreateSplitMenuFlyoutItemsFromXaml
		// Typical structure: MenuFlyoutItem, MenuFlyoutItem, Separator, ToggleMenuFlyoutItem, Separator,
		// SplitMenuFlyoutItem (no submenu), SplitMenuFlyoutItem (with submenu)
		// Navigate to the SplitMenuFlyoutItem with submenu (around 6-7 down presses)
		for (int i = 0; i < 6; i++)
		{
			await CommonInputHelper.Down(device);
		}

		// Expand/collapse the submenu using right and left.
		await CommonInputHelper.Right(device);
		await CommonInputHelper.Down(device);
		await CommonInputHelper.Left(device);

		await lostFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Expand/collapse the submenu using accept and cancel.
		await CommonInputHelper.Accept(device);
		await CommonInputHelper.Down(device);
		await CommonInputHelper.Cancel(device);

		await lostFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Close the top-level menu (which will close the MenuFlyout itself)
		await CommonInputHelper.Cancel(device);

		await menuFlyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Opened flyout expected by caller
		await ShowMenuFlyout(menuFlyout, button, -50, 50);
	}

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

	private async Task ShowMenuFlyout(MenuFlyout menuFlyout, UIElement relativeTo, double horizontalOffset, double verticalOffset, bool forceTapAsPreviousInputMessage = true)
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

#if HAS_UNO
			await TestServices.WindowHelper.SetLastInputMethod(InputDeviceType.Touch, xamlRoot);
#endif
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

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task ValidateUIElementTree()
	{
		// The upstream master files dump the whole visual tree. They are checked here with explicit assertions on the
		// popups, the items (text, enabled, checked, foreground) and the sizes found in the Windowless masters.
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(800, 730));

		const double textLineTolerance = 2; // TODO Uno: Skia's text lines are taller than DirectWrite's, once for each item in the menu.

		MenuFlyout menuFlyout = null;
		MenuFlyoutItem restMenuFlyoutItem = null;
		MenuFlyoutItem restMenuFlyoutItemWithKeyboardAccelerator = null;
		MenuFlyoutItem pointerOverMenuFlyoutItem = null;
		MenuFlyoutItem pointerOverMenuFlyoutItemWithKeyboardAccelerator = null;
		MenuFlyoutItem pressedMenuFlyoutItem = null;
		MenuFlyoutItem pressedMenuFlyoutItemWithKeyboardAccelerator = null;
		MenuFlyoutItem disabledMenuFlyoutItem = null;
		MenuFlyoutItem disabledMenuFlyoutItemWithKeyboardAccelerator = null;
		MenuFlyoutItem focusedMenuFlyoutItem = null;

		MenuFlyoutSeparator firstMenuFlyoutSeparator = null;

		ToggleMenuFlyoutItem restUncheckedToggleMenuFlyoutItem = null;
		ToggleMenuFlyoutItem restUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = null;
		ToggleMenuFlyoutItem pointerOverUncheckedToggleMenuFlyoutItem = null;
		ToggleMenuFlyoutItem pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = null;
		ToggleMenuFlyoutItem pressedUncheckedToggleMenuFlyoutItem = null;
		ToggleMenuFlyoutItem pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = null;
		ToggleMenuFlyoutItem disabledUncheckedToggleMenuFlyoutItem = null;
		ToggleMenuFlyoutItem disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = null;
		ToggleMenuFlyoutItem focusedUncheckedToggleMenuFlyoutItem = null;

		MenuFlyoutSeparator secondMenuFlyoutSeparator = null;

		MenuFlyoutSubItem restMenuFlyoutSubItem = null;
		MenuFlyoutSubItem pointerOverMenuFlyoutSubItem = null;
		MenuFlyoutSubItem pressedMenuFlyoutSubItem = null;
		MenuFlyoutSubItem disabledMenuFlyoutSubItem = null;
		MenuFlyoutSubItem focusedMenuFlyoutSubItem = null;

		ToggleMenuFlyoutItem restCheckedToggleMenuFlyoutItem = null;
		ToggleMenuFlyoutItem restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = null;
		ToggleMenuFlyoutItem pointerOverCheckedToggleMenuFlyoutItem = null;
		ToggleMenuFlyoutItem pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = null;
		ToggleMenuFlyoutItem pressedCheckedToggleMenuFlyoutItem = null;
		ToggleMenuFlyoutItem pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = null;
		ToggleMenuFlyoutItem disabledCheckedToggleMenuFlyoutItem = null;
		ToggleMenuFlyoutItem disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = null;
		ToggleMenuFlyoutItem focusedCheckedToggleMenuFlyoutItem = null;

		StackPanel rootPanel = null;
		Button rootButton = null;

		await RunOnUIThread(() =>
		{
			rootPanel = new StackPanel();

			menuFlyout = new MenuFlyout();
			menuFlyout.LightDismissOverlayMode = LightDismissOverlayMode.Off;
#if HAS_UNO
			// Uno: the sizes below come from the Windowless masters.
			global::Uno.UI.FeatureConfiguration.Popup.UseWindowedPopups = false;
#endif

			restMenuFlyoutItem = new MenuFlyoutItem();
			restMenuFlyoutItem.Text = "Rest MenuFlyoutItem";
			menuFlyout.Items.Add(restMenuFlyoutItem);

			restMenuFlyoutItemWithKeyboardAccelerator = new MenuFlyoutItem();
			restMenuFlyoutItemWithKeyboardAccelerator.Text = "Rest MenuFlyoutItem with keyboard accelerator";
			restMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.A, Windows.System.VirtualKeyModifiers.None));
			menuFlyout.Items.Add(restMenuFlyoutItemWithKeyboardAccelerator);

			pointerOverMenuFlyoutItem = new MenuFlyoutItem();
			pointerOverMenuFlyoutItem.Text = "Pointer Over MenuFlyoutItem";
			menuFlyout.Items.Add(pointerOverMenuFlyoutItem);

			pointerOverMenuFlyoutItemWithKeyboardAccelerator = new MenuFlyoutItem();
			pointerOverMenuFlyoutItemWithKeyboardAccelerator.Text = "Pointer Over MenuFlyoutItem with keyboard accelerator";
			pointerOverMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.S, Windows.System.VirtualKeyModifiers.Control));
			menuFlyout.Items.Add(pointerOverMenuFlyoutItemWithKeyboardAccelerator);

			pressedMenuFlyoutItem = new MenuFlyoutItem();
			pressedMenuFlyoutItem.Text = "Pressed MenuFlyoutItem";
			menuFlyout.Items.Add(pressedMenuFlyoutItem);

			pressedMenuFlyoutItemWithKeyboardAccelerator = new MenuFlyoutItem();
			pressedMenuFlyoutItemWithKeyboardAccelerator.Text = "Pressed MenuFlyoutItem with keyboard accelerator";
			pressedMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.D, Windows.System.VirtualKeyModifiers.Shift));
			menuFlyout.Items.Add(pressedMenuFlyoutItemWithKeyboardAccelerator);

			disabledMenuFlyoutItem = new MenuFlyoutItem();
			disabledMenuFlyoutItem.Text = "Disabled MenuFlyoutItem";
			disabledMenuFlyoutItem.IsEnabled = false;
			menuFlyout.Items.Add(disabledMenuFlyoutItem);

			disabledMenuFlyoutItemWithKeyboardAccelerator = new MenuFlyoutItem();
			disabledMenuFlyoutItemWithKeyboardAccelerator.Text = "Disabled MenuFlyoutItem with keyboard accelerator";
			disabledMenuFlyoutItemWithKeyboardAccelerator.IsEnabled = false;
			disabledMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.F, Windows.System.VirtualKeyModifiers.Menu | Windows.System.VirtualKeyModifiers.Windows));
			menuFlyout.Items.Add(disabledMenuFlyoutItemWithKeyboardAccelerator);

			focusedMenuFlyoutItem = new MenuFlyoutItem();
			focusedMenuFlyoutItem.Text = "Focused MenuFlyoutItem";
			menuFlyout.Items.Add(focusedMenuFlyoutItem);

			firstMenuFlyoutSeparator = new MenuFlyoutSeparator();
			menuFlyout.Items.Add(firstMenuFlyoutSeparator);

			restMenuFlyoutSubItem = new MenuFlyoutSubItem();
			restMenuFlyoutSubItem.Text = "Rest MenuFlyoutSubItem";
			menuFlyout.Items.Add(restMenuFlyoutSubItem);

			pointerOverMenuFlyoutSubItem = new MenuFlyoutSubItem();
			pointerOverMenuFlyoutSubItem.Text = "Pointer Over MenuFlyoutSubItem";
			menuFlyout.Items.Add(pointerOverMenuFlyoutSubItem);

			pressedMenuFlyoutSubItem = new MenuFlyoutSubItem();
			pressedMenuFlyoutSubItem.Text = "Pressed MenuFlyoutSubItem";
			menuFlyout.Items.Add(pressedMenuFlyoutSubItem);

			disabledMenuFlyoutSubItem = new MenuFlyoutSubItem();
			disabledMenuFlyoutSubItem.Text = "Disabled MenuFlyoutSubItem";
			disabledMenuFlyoutSubItem.IsEnabled = false;
			menuFlyout.Items.Add(disabledMenuFlyoutSubItem);

			focusedMenuFlyoutSubItem = new MenuFlyoutSubItem();
			focusedMenuFlyoutSubItem.Text = "Focused MenuFlyoutSubItem";
			menuFlyout.Items.Add(focusedMenuFlyoutSubItem);

			secondMenuFlyoutSeparator = new MenuFlyoutSeparator();
			menuFlyout.Items.Add(secondMenuFlyoutSeparator);

			restUncheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
			restUncheckedToggleMenuFlyoutItem.Text = "Rest Unchecked ToggleMenuFlyoutItem";
			menuFlyout.Items.Add(restUncheckedToggleMenuFlyoutItem);

			restUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
			restUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Rest Unchecked ToggleMenuFlyoutItem with keyboard accelerator";
			restUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.A, Windows.System.VirtualKeyModifiers.None));
			menuFlyout.Items.Add(restUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

			pointerOverUncheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
			pointerOverUncheckedToggleMenuFlyoutItem.Text = "Pointer Over Unchecked ToggleMenuFlyoutItem";
			menuFlyout.Items.Add(pointerOverUncheckedToggleMenuFlyoutItem);

			pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
			pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Pointer Over Unchecked ToggleMenuFlyoutItem with keyboard accelerator";
			pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.S, Windows.System.VirtualKeyModifiers.Control));
			menuFlyout.Items.Add(pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

			pressedUncheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
			pressedUncheckedToggleMenuFlyoutItem.Text = "Pressed Unchecked ToggleMenuFlyoutItem";
			menuFlyout.Items.Add(pressedUncheckedToggleMenuFlyoutItem);

			pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
			pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Pressed Unchecked ToggleMenuFlyoutItem with keyboard accelerator";
			pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.D, Windows.System.VirtualKeyModifiers.Shift));
			menuFlyout.Items.Add(pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

			disabledUncheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
			disabledUncheckedToggleMenuFlyoutItem.Text = "Disabled Unchecked ToggleMenuFlyoutItem";
			disabledUncheckedToggleMenuFlyoutItem.IsEnabled = false;
			menuFlyout.Items.Add(disabledUncheckedToggleMenuFlyoutItem);

			disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
			disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Disabled Unchecked ToggleMenuFlyoutItem with keyboard accelerator";
			disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsEnabled = false;
			disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.F, Windows.System.VirtualKeyModifiers.Menu | Windows.System.VirtualKeyModifiers.Windows));
			menuFlyout.Items.Add(disabledUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

			focusedUncheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
			focusedUncheckedToggleMenuFlyoutItem.Text = "Focused Unchecked ToggleMenuFlyoutItem";
			menuFlyout.Items.Add(focusedUncheckedToggleMenuFlyoutItem);

			restCheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
			restCheckedToggleMenuFlyoutItem.Text = "Rest Checked ToggleMenuFlyoutItem";
			restCheckedToggleMenuFlyoutItem.IsChecked = true;
			restMenuFlyoutSubItem.Items.Add(restCheckedToggleMenuFlyoutItem);

			restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
			restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Rest Checked ToggleMenuFlyoutItem with keyboard accelerator";
			restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsChecked = true;
			restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.A, Windows.System.VirtualKeyModifiers.None));
			menuFlyout.Items.Add(restCheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

			pointerOverCheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
			pointerOverCheckedToggleMenuFlyoutItem.Text = "Pointer Over Checked ToggleMenuFlyoutItem";
			pointerOverCheckedToggleMenuFlyoutItem.IsChecked = true;
			restMenuFlyoutSubItem.Items.Add(pointerOverCheckedToggleMenuFlyoutItem);

			pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
			pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Pointer Over Checked ToggleMenuFlyoutItem with keyboard accelerator";
			pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsChecked = true;
			pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.S, Windows.System.VirtualKeyModifiers.Control));
			menuFlyout.Items.Add(pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

			pressedCheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
			pressedCheckedToggleMenuFlyoutItem.Text = "Pressed Checked ToggleMenuFlyoutItem";
			pressedCheckedToggleMenuFlyoutItem.IsChecked = true;
			restMenuFlyoutSubItem.Items.Add(pressedCheckedToggleMenuFlyoutItem);

			pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
			pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Pressed Checked ToggleMenuFlyoutItem with keyboard accelerator";
			pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsChecked = true;
			pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.D, Windows.System.VirtualKeyModifiers.Shift));
			menuFlyout.Items.Add(pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

			disabledCheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
			disabledCheckedToggleMenuFlyoutItem.Text = "Disabled Checked ToggleMenuFlyoutItem";
			disabledCheckedToggleMenuFlyoutItem.IsChecked = true;
			disabledCheckedToggleMenuFlyoutItem.IsEnabled = false;
			restMenuFlyoutSubItem.Items.Add(disabledCheckedToggleMenuFlyoutItem);

			disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator = new ToggleMenuFlyoutItem();
			disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.Text = "Disabled Checked ToggleMenuFlyoutItem with keyboard accelerator";
			disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsChecked = true;
			disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.IsEnabled = false;
			disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator.KeyboardAccelerators.Add(CreateKeyboardAccelerator(Windows.System.VirtualKey.F, Windows.System.VirtualKeyModifiers.Menu | Windows.System.VirtualKeyModifiers.Windows));
			menuFlyout.Items.Add(disabledCheckedToggleMenuFlyoutItemWithKeyboardAccelerator);

			focusedCheckedToggleMenuFlyoutItem = new ToggleMenuFlyoutItem();
			focusedCheckedToggleMenuFlyoutItem.Text = "Focused Checked ToggleMenuFlyoutItem";
			focusedCheckedToggleMenuFlyoutItem.IsChecked = true;
			restMenuFlyoutSubItem.Items.Add(focusedCheckedToggleMenuFlyoutItem);

			rootButton = new Button();
			rootButton.Flyout = menuFlyout;
			rootPanel.Children.Add(rootButton);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		async Task SetupValidation()
		{
			await ShowMenuFlyout(menuFlyout, rootButton, 0, 0, true /* forceTapAsPreviousInputMessage */);
			await TapSubMenuItem(restMenuFlyoutSubItem);

			await RunOnUIThread(() =>
			{
				// MenuFlyoutItems
				VisualStateManager.GoToState(pointerOverMenuFlyoutItem, "PointerOver", false);
				VisualStateManager.GoToState(pointerOverMenuFlyoutItemWithKeyboardAccelerator, "PointerOver", false);
				VisualStateManager.GoToState(pressedMenuFlyoutItem, "Pressed", false);
				VisualStateManager.GoToState(pressedMenuFlyoutItemWithKeyboardAccelerator, "Pressed", false);
				VisualStateManager.GoToState(focusedMenuFlyoutItem, "Focused", false);

				// Unchecked ToggleMenuFlyoutItems
				VisualStateManager.GoToState(pointerOverUncheckedToggleMenuFlyoutItem, "PointerOver", false);
				VisualStateManager.GoToState(pointerOverUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator, "PointerOver", false);
				VisualStateManager.GoToState(pressedUncheckedToggleMenuFlyoutItem, "Pressed", false);
				VisualStateManager.GoToState(pressedUncheckedToggleMenuFlyoutItemWithKeyboardAccelerator, "Pressed", false);
				VisualStateManager.GoToState(focusedUncheckedToggleMenuFlyoutItem, "Focused", false);

				// MenuFlyoutSubItems
				VisualStateManager.GoToState(pointerOverMenuFlyoutSubItem, "PointerOver", false);
				VisualStateManager.GoToState(pressedMenuFlyoutSubItem, "Pressed", false);
				VisualStateManager.GoToState(focusedMenuFlyoutSubItem, "Focused", false);

				// Checked ToggleMenuFlyoutItems
				VisualStateManager.GoToState(pointerOverCheckedToggleMenuFlyoutItem, "PointerOver", false);
				VisualStateManager.GoToState(pointerOverCheckedToggleMenuFlyoutItemWithKeyboardAccelerator, "PointerOver", false);
				VisualStateManager.GoToState(pressedCheckedToggleMenuFlyoutItem, "Pressed", false);
				VisualStateManager.GoToState(pressedCheckedToggleMenuFlyoutItemWithKeyboardAccelerator, "Pressed", false);
				VisualStateManager.GoToState(focusedCheckedToggleMenuFlyoutItem, "Focused", false);
			});
			await TestServices.WindowHelper.WaitForIdle();
		}

		// Master: the sub menu popup holds the checked items that belong to the sub item, the main popup holds the rest.
		async Task ValidateTree(Windows.UI.Color expectedForeground, Windows.UI.Color expectedSeparatorBackground)
		{
			await RunOnUIThread(() =>
			{
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(rootPanel.XamlRoot);
				VERIFY_ARE_EQUAL(2, popups.Count);

				var subPresenter = (MenuFlyoutPresenter)popups[0].Child;
				var mainPresenter = (MenuFlyoutPresenter)popups[1].Child;

				VERIFY_ARE_EQUAL(mainPresenter.Padding, (Thickness)Application.Current.Resources["MenuFlyoutPresenterThemePadding"]);
				VERIFY_ARE_EQUAL(subPresenter.Padding, (Thickness)Application.Current.Resources["MenuFlyoutPresenterThemePadding"]);
				VERIFY_ARE_EQUAL(mainPresenter.ActualHeight, 730);
				VERIFY_ARE_VERY_CLOSE(subPresenter.ActualHeight, 210, 5 * textLineTolerance);

				foreach (var item in menuFlyout.Items)
				{
					VERIFY_IS_NOT_NULL(item);
					if (item is MenuFlyoutSeparator separator)
					{
						VERIFY_ARE_EQUAL(((SolidColorBrush)separator.Background).Color, expectedSeparatorBackground);
						// The master runs on plain generic.xaml (9 tall, 12,4,12,4). Uno's default theme is XamlControlsResources' MenuFlyoutSeparatorThemePadding.
						VERIFY_ARE_EQUAL(separator.ActualHeight, 3);
						VERIFY_ARE_EQUAL(separator.Padding, new Thickness(-4, 1, -4, 1));
					}
					else
					{
						var control = (Control)item;
						VERIFY_ARE_EQUAL(((SolidColorBrush)control.Foreground).Color, expectedForeground);
						VERIFY_ARE_EQUAL(control.Padding, (Thickness)Application.Current.Resources["MenuFlyoutItemThemePadding"]);
					}
				}

				foreach (var item in menuFlyout.Items)
				{
					if (item is MenuFlyoutItem menuItem)
					{
						VERIFY_ARE_VERY_CLOSE(menuItem.ActualHeight, 40, textLineTolerance);
						VERIFY_ARE_EQUAL(menuItem.IsEnabled, !menuItem.Text.StartsWith("Disabled"));
					}
					else if (item is MenuFlyoutSubItem subItem)
					{
						VERIFY_ARE_VERY_CLOSE(subItem.ActualHeight, 40, textLineTolerance);
						VERIFY_ARE_EQUAL(subItem.IsEnabled, !subItem.Text.StartsWith("Disabled"));
					}
				}

				foreach (var item in menuFlyout.Items)
				{
					if (item is ToggleMenuFlyoutItem toggleItem)
					{
						VERIFY_ARE_EQUAL(toggleItem.IsChecked, toggleItem.Text.Contains("Checked") && !toggleItem.Text.Contains("Unchecked"));
					}
				}

				foreach (var item in restMenuFlyoutSubItem.Items)
				{
					var toggleItem = (ToggleMenuFlyoutItem)item;
					VERIFY_IS_TRUE(toggleItem.IsChecked);
					VERIFY_ARE_VERY_CLOSE(toggleItem.ActualHeight, 40, textLineTolerance);
					VERIFY_ARE_EQUAL(((SolidColorBrush)toggleItem.Foreground).Color, expectedForeground);
					VERIFY_ARE_EQUAL(toggleItem.IsEnabled, !toggleItem.Text.StartsWith("Disabled"));
				}
			});
		}

		// Validate the Dark theme of controls.
		{
			await RunOnUIThread(() =>
			{
				rootPanel.RequestedTheme = ElementTheme.Dark;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await SetupValidation();
			// The master's separator is 0x66FFFFFF on plain generic.xaml, Uno's default theme uses XamlControlsResources' DividerStrokeColorDefault.
			await ValidateTree(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), Windows.UI.Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF));

			await RunOnUIThread(() =>
			{
				menuFlyout.Hide();
			});
			await TestServices.WindowHelper.WaitForIdle();
		}

		// Validate the light theme of controls.
		{
			await RunOnUIThread(() =>
			{
				rootPanel.RequestedTheme = ElementTheme.Light;
				rootPanel.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
			});
			await TestServices.WindowHelper.WaitForIdle();

			await SetupValidation();
			// Same here, with 0xFF000000 and 0x66000000 on plain generic.xaml.
			await ValidateTree(Windows.UI.Color.FromArgb(0xE4, 0x00, 0x00, 0x00), Windows.UI.Color.FromArgb(0x0F, 0x00, 0x00, 0x00));

			await RunOnUIThread(() =>
			{
				menuFlyout.Hide();
			});
			await TestServices.WindowHelper.WaitForIdle();
		}

		// TODO Uno: the high-contrast variant needs a way to turn high-contrast on (ControlHelper.ValidateUIElementTreeForHighContrast).
	}

	private async Task<MenuFlyout> CreateSplitMenuFlyoutItemsFromXaml()
	{
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutItem>Menu item 1</MenuFlyoutItem>" +
				"    <MenuFlyoutItem>Menu item 2</MenuFlyoutItem>" +
				"    <MenuFlyoutSeparator/>" +
				"    <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 3</ToggleMenuFlyoutItem>" +
				"    <MenuFlyoutSeparator/>" +
				"    <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split item 4' />" +
				"    <SplitMenuFlyoutItem x:Name='splitItem2' Text='Split item 5'>" +
				"        <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>" +
				"        <MenuFlyoutSeparator/>" +
				"        <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 2.3</ToggleMenuFlyoutItem>" +
				"        <MenuFlyoutSeparator/>" +
				"        <MenuFlyoutSubItem x:Name='subItem1' Text='Menu sub item 2.4'>" +
				"            <MenuFlyoutItem>Menu item 3.1</MenuFlyoutItem>" +
				"            <MenuFlyoutItem>Menu item 3.2</MenuFlyoutItem>" +
				"            <MenuFlyoutSeparator/>" +
				"            <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 3.3</ToggleMenuFlyoutItem>" +
				"            <MenuFlyoutSeparator/>" +
				"        </MenuFlyoutSubItem>" +
				"    </SplitMenuFlyoutItem>" +
				"</MenuFlyout>"));
		});

		return menuFlyout;
	}

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

	private void GetMenuFlyoutItemsHorizontalPadding(IList<MenuFlyoutItemBase> items, ref double leftPadding, ref double rightPadding)
	{
		for (int i = 0; i < items.Count; i++)
		{
			MenuFlyoutSubItem menuFlyoutSubItem = items[i] as MenuFlyoutSubItem;
			ToggleMenuFlyoutItem toggleMenuFlyoutItem = items[i] as ToggleMenuFlyoutItem;
			MenuFlyoutItem menuFlyoutItem = items[i] as MenuFlyoutItem;

			if (menuFlyoutSubItem != null)
			{
				Grid layoutRoot = (Grid)(TreeHelper.GetVisualChildByName(menuFlyoutSubItem, "LayoutRoot"));
				leftPadding = layoutRoot.Padding.Left;
				rightPadding = layoutRoot.Padding.Right;
			}
			else if (toggleMenuFlyoutItem != null)
			{
				Grid layoutRoot = (Grid)(TreeHelper.GetVisualChildByName(toggleMenuFlyoutItem, "LayoutRoot"));
				leftPadding = layoutRoot.Padding.Left;
				rightPadding = layoutRoot.Padding.Right;
			}
			else if (menuFlyoutItem != null)
			{
				Grid layoutRoot = (Grid)(TreeHelper.GetVisualChildByName(menuFlyoutItem, "LayoutRoot"));
				leftPadding = layoutRoot.Padding.Left;
				rightPadding = layoutRoot.Padding.Right;
			}
		}
	}

	private async Task VerifyMenuFlyoutItemsPadding(IList<MenuFlyoutItemBase> items, Thickness expectedPadding)
	{
		for (int i = 0; i < items.Count; i++)
		{
			MenuFlyoutSubItem menuFlyoutSubItem = items[i] as MenuFlyoutSubItem;
			ToggleMenuFlyoutItem toggleMenuFlyoutItem = items[i] as ToggleMenuFlyoutItem;
			MenuFlyoutItem menuFlyoutItem = items[i] as MenuFlyoutItem;

			if (menuFlyoutSubItem != null)
			{
				Grid layoutRoot = (Grid)(TreeHelper.GetVisualChildByName(menuFlyoutSubItem, "LayoutRoot"));

				LOG_OUTPUT("SubItem InnerBorder top=%f bottom=%f", layoutRoot.Padding.Top, layoutRoot.Padding.Bottom);

				VERIFY_ARE_EQUAL(expectedPadding, layoutRoot.Padding);

				await VerifyMenuFlyoutItemsPadding(menuFlyoutSubItem.Items, expectedPadding);
			}
			else if (toggleMenuFlyoutItem != null)
			{
				Grid layoutRoot = (Grid)(TreeHelper.GetVisualChildByName(toggleMenuFlyoutItem, "LayoutRoot"));

				LOG_OUTPUT("ToggleMenuItem InnerBorder top=%f bottom=%f", layoutRoot.Padding.Top, layoutRoot.Padding.Bottom);

				VERIFY_ARE_EQUAL(expectedPadding, layoutRoot.Padding);
			}
			else if (menuFlyoutItem != null)
			{
				Grid layoutRoot = (Grid)(TreeHelper.GetVisualChildByName(menuFlyoutItem, "LayoutRoot"));

				LOG_OUTPUT("MenuItem InnerBorder top=%f bottom=%f", layoutRoot.Padding.Top, layoutRoot.Padding.Bottom);

				VERIFY_ARE_EQUAL(expectedPadding, layoutRoot.Padding);
			}
		}
	}

	[TestMethod]
	public async Task ValidateRTLSubMenuItemPosition()
	{
		IList<MenuFlyoutItemBase> items = null;
		IList<MenuFlyoutItemBase> subItems = null;
		MenuFlyoutSubItem subItem = null;
		Rect windowBounds = default;
		Rect menuFlyoutBounds = default;
		Rect subMenu1Bounds = default;
		Rect subMenu2Bounds = default;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				"    <MenuFlyoutSeparator/>" +
				"    <MenuFlyoutSubItem x:Name='subItem1' Text='subitem1'>" +
				"        <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				"        <MenuFlyoutSeparator/>" +
				"        <MenuFlyoutSubItem  x:Name='subItem2' Text='subItem2' FlowDirection='RightToLeft'>" +
				"            <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				"            <MenuFlyoutSeparator/>" +
				"            <MenuFlyoutSubItem x:Name='subItem3' Text='subitem3' FlowDirection='RightToLeft'/>" +
				"        </MenuFlyoutSubItem>" +
				"    </MenuFlyoutSubItem>" +
				"</MenuFlyout>"));

			windowBounds = TestServices.WindowHelper.WindowBounds;
			LOG_OUTPUT($"Windows bounds left={windowBounds.Left} top={windowBounds.Top} width={windowBounds.Width} height={windowBounds.Height}");
		});

		await ShowMenuFlyout(menuFlyout, null, 50, 100);

		var presenter = await GetCurrentPresenter();

		menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
		LOG_OUTPUT($"MenuFlyout bounds left={menuFlyoutBounds.Left} top={menuFlyoutBounds.Top} width={menuFlyoutBounds.Width} height={menuFlyoutBounds.Height}");

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
		});

		subItem = await GetSubItem(items);
		await TapSubMenuItem(subItem);

		presenter = await GetCurrentPresenter();
		subMenu1Bounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
		LOG_OUTPUT($"SubMenuFlyout bounds left={subMenu1Bounds.Left} top={subMenu1Bounds.Top} width={subMenu1Bounds.Width} height={subMenu1Bounds.Height}");

		VERIFY_IS_TRUE(menuFlyoutBounds.Left < subMenu1Bounds.Left);

		await RunOnUIThread(() =>
		{
			subItems = subItem.Items;
		});

		var subItem2 = await GetSubItem(subItems);
		await TapSubMenuItem(subItem2);

		presenter = await GetCurrentPresenter();
		subMenu2Bounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
		LOG_OUTPUT($"SubMenuFlyout RTL bounds left={subMenu2Bounds.Left} top={subMenu2Bounds.Top} width={subMenu2Bounds.Width} height={subMenu2Bounds.Height}");

		VERIFY_IS_TRUE(subMenu2Bounds.Left < subMenu1Bounds.Left);
		VERIFY_IS_TRUE(subMenu1Bounds.Left < subMenu2Bounds.Left + subMenu2Bounds.Width);

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateRTLSplitMenuItemPosition()
	{
		IList<MenuFlyoutItemBase> items = null;
		IList<MenuFlyoutItemBase> subItems = null;
		SplitMenuFlyoutItem splitItem = null;
		Rect windowBounds = default;
		Rect menuFlyoutBounds = default;
		Rect subMenu1Bounds = default;
		Rect subMenu2Bounds = default;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				"    <MenuFlyoutSeparator/>" +
				"    <SplitMenuFlyoutItem x:Name='splitItem1' Text='splititem1'>" +
				"        <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				"        <MenuFlyoutSeparator/>" +
				"        <SplitMenuFlyoutItem x:Name='splitItem2' Text='splitItem2' FlowDirection='RightToLeft'>" +
				"            <MenuFlyoutItem>item1</MenuFlyoutItem>" +
				"            <MenuFlyoutSeparator/>" +
				"            <SplitMenuFlyoutItem x:Name='splitItem3' Text='splititem3' FlowDirection='RightToLeft'/>" +
				"        </SplitMenuFlyoutItem>" +
				"    </SplitMenuFlyoutItem>" +
				"</MenuFlyout>"));

			windowBounds = TestServices.WindowHelper.WindowBounds;
			LOG_OUTPUT("Windows bounds left=%f top=%f width=%f height=%f", windowBounds.Left, windowBounds.Top, windowBounds.Width, windowBounds.Height);
		});

		await ShowMenuFlyout(menuFlyout, null, 50, 100);

		var presenter = await GetCurrentPresenter();

		await RunOnUIThread(async () =>
		{
			menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("MenuFlyout bounds left=%f top=%f width=%f height=%f", menuFlyoutBounds.Left, menuFlyoutBounds.Top, menuFlyoutBounds.Width, menuFlyoutBounds.Height);
		});

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
		});

		// Find the SplitMenuFlyoutItem (last item)
		await RunOnUIThread(() =>
		{
			splitItem = items[items.Count - 1] as SplitMenuFlyoutItem;
		});
		await TapSplitMenuItemSecondary(splitItem);

		presenter = await GetCurrentPresenter();
		await RunOnUIThread(async () =>
		{
			subMenu1Bounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("SplitMenuFlyout bounds left=%f top=%f width=%f height=%f", subMenu1Bounds.Left, subMenu1Bounds.Top, subMenu1Bounds.Width, subMenu1Bounds.Height);
		});

		VERIFY_IS_TRUE(menuFlyoutBounds.Left < subMenu1Bounds.Left);

		await RunOnUIThread(() =>
		{
			subItems = splitItem.Items;
		});

		// Find the nested SplitMenuFlyoutItem (last item in subItems)
		SplitMenuFlyoutItem splitItem2 = null;
		await RunOnUIThread(() =>
		{
			splitItem2 = subItems[subItems.Count - 1] as SplitMenuFlyoutItem;
		});
		await TapSplitMenuItemSecondary(splitItem2);

		presenter = await GetCurrentPresenter();
		await RunOnUIThread(async () =>
		{
			subMenu2Bounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("SplitMenuFlyout RTL bounds left=%f top=%f width=%f height=%f", subMenu2Bounds.Left, subMenu2Bounds.Top, subMenu2Bounds.Width, subMenu2Bounds.Height);
		});

		VERIFY_IS_TRUE(subMenu2Bounds.Left < subMenu1Bounds.Left);
		VERIFY_IS_TRUE(subMenu1Bounds.Left < subMenu2Bounds.Left + subMenu2Bounds.Width);

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateSubMenuItemProperties()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;
		IList<MenuFlyoutItemBase> items = null;

		var menuFlyoutOpenedEvent = new Event();
		var menuFlyoutClosedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' >" +
				"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Center' " +
				"          RequestedTheme='light' FlowDirection='LeftToRight' Language='En-UK' IsTextScaleFactorEnabled='true' >" +
				"    <Button.Flyout> " +
				"      <MenuFlyout Placement='Bottom'> " +
				"        <MenuFlyoutItem Text='Item 1' /> " +
				"        <MenuFlyoutItem Text='Item 2' /> " +
				"        <MenuFlyoutSeparator Width='300' /> " +
				"        <ToggleMenuFlyoutItem Text='Toggle Item3' IsChecked='True' /> " +
				"        <MenuFlyoutSeparator Width='300' /> " +
				"        <MenuFlyoutSubItem x:Name='subItem1' Text='Sub item 4'>" +
				"          <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>" +
				"          <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>" +
				"          <MenuFlyoutSeparator/>" +
				"          <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 2.3</ToggleMenuFlyoutItem>" +
				"         </MenuFlyoutSubItem>" +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);

			// Set the DataContext to button from the root panel
			button.DataContext = rootPanel;

			// Set the MenuFlyout presenter style
			var style = new Style(typeof(MenuFlyoutPresenter));
			style.Setters.Add(new Setter(MenuFlyoutPresenter.TagProperty, "presenter_style"));
			menuFlyout.MenuFlyoutPresenterStyle = style;

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});

			closedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutClosedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
		});

		var subItem = await GetSubItem(items);
		await TapSubMenuItem(subItem);

		var subPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(subPresenter.RequestedTheme == ElementTheme.Light);
			VERIFY_IS_TRUE(subPresenter.FlowDirection == FlowDirection.LeftToRight);
			VERIFY_IS_TRUE(subPresenter.Language == "En-UK");
			VERIFY_IS_TRUE(subPresenter.IsTextScaleFactorEnabled);

			VERIFY_IS_NOT_NULL(subPresenter.DataContext);

			var tag = subPresenter.GetValue(MenuFlyoutPresenter.TagProperty);
			VERIFY_IS_NOT_NULL(tag);
			VERIFY_ARE_EQUAL("presenter_style", tag.ToString());
		});

		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});

		await menuFlyoutClosedEvent.WaitForDefault();
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemProperties()
	{
		// Keeping it similar to ValidateSuMenuItemProperties.
		// TestServices.ErrorHandlingHelper.IgnoreLeaksForTest();

		Button button = null;
		MenuFlyout menuFlyout = null;
		IList<MenuFlyoutItemBase> items = null;

		var menuFlyoutOpenedEvent = new Event();
		var menuFlyoutClosedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' >" +
				"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Center' " +
				"          RequestedTheme='light' FlowDirection='LeftToRight' Language='En-UK' IsTextScaleFactorEnabled='true' >" +
				"    <Button.Flyout> " +
				"      <MenuFlyout Placement='Bottom'> " +
				"        <MenuFlyoutItem Text='Item 1' /> " +
				"        <MenuFlyoutItem Text='Item 2' /> " +
				"        <MenuFlyoutSeparator Width='300' /> " +
				"        <ToggleMenuFlyoutItem Text='Toggle Item3' IsChecked='True' /> " +
				"        <MenuFlyoutSeparator Width='300' /> " +
				"        <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split item 4'>" +
				"          <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>" +
				"          <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>" +
				"          <MenuFlyoutSeparator/>" +
				"          <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 2.3</ToggleMenuFlyoutItem>" +
				"         </SplitMenuFlyoutItem>" +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);

			// Set the DataContext to button from the root panel
			button.DataContext = rootPanel;

			// Set the MenuFlyout presenter style
			var style = new Style(typeof(MenuFlyoutPresenter));
			style.Setters.Add(new Setter(MenuFlyoutPresenter.TagProperty, "presenter_style"));
			menuFlyout.MenuFlyoutPresenterStyle = style;

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});

			closedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutClosedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
		});

		SplitMenuFlyoutItem splitItem = null;
		await RunOnUIThread(() =>
		{
			// SplitMenuFlyoutItem is at index 5 (after 2 items, separator, toggle item, separator)
			splitItem = items[5] as SplitMenuFlyoutItem;
		});
		VERIFY_IS_NOT_NULL(splitItem);
		await TapSplitMenuItemSecondary(splitItem);

		var subPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(subPresenter.RequestedTheme == ElementTheme.Light);
			VERIFY_IS_TRUE(subPresenter.FlowDirection == FlowDirection.LeftToRight);
			VERIFY_IS_TRUE(subPresenter.Language == "En-UK");
			VERIFY_IS_TRUE(subPresenter.IsTextScaleFactorEnabled);

			VERIFY_IS_NOT_NULL(subPresenter.DataContext);

			var tag = subPresenter.GetValue(MenuFlyoutPresenter.TagProperty);
			VERIFY_IS_NOT_NULL(tag);
			VERIFY_ARE_EQUAL("presenter_style", tag.ToString());
		});

		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});

		await menuFlyoutClosedEvent.WaitForDefault();
	}

	[TestMethod]
	[Ignore("Needs layout transition elements (CLayoutTransitionElement) for Popup.ChildTransitions on the MenuFlyoutPresenter, and a StoryboardMonitor to count the started storyboards")]
	public async Task ValidateThatLayoutTransitionsDoRun()
	{
		// TODO Uno: the non-desktop expectations are used, see the Ignore above.
		const bool isDesktop = false;

		Button button = null;
		IList<MenuFlyoutItemBase> items = null;
		var storyboardMonitor = new StoryboardMonitorWrapper();
		int startedStoryboardCount = 0;

		var menuFlyout = await CreateMenuFlyoutSubItemsFromXaml();

		// Prepare the menu flyout
		await RunOnUIThread(() =>
		{
			var grid = new Grid();
			button = new Button();
			button.Content = "ValidateThatLayoutTransitionsDoRun";
			button.Flyout = menuFlyout;
			items = ((MenuFlyout)button.Flyout).Items;
			grid.Children.Add(button);
			TestServices.WindowHelper.WindowContent = grid;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Validate storboard count
		storyboardMonitor.AttachStartedHandler(
			(Storyboard storyboard, UIElement target) =>
			{
				if (target is MenuFlyoutPresenter)
				{
					++startedStoryboardCount;
				}
			});

		LOG_OUTPUT("Open the menu flyout");
		await RunOnUIThread(() =>
		{
			button.Flyout.ShowAt(button);
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Windowed menus, which we have on desktop, don't have layout transitions.
		VERIFY_ARE_EQUAL(startedStoryboardCount, isDesktop ? 0 : 1);

		LOG_OUTPUT("Open the sub menu");
		await TapSubMenuItem(await GetSubItem(items));

		VERIFY_ARE_EQUAL(startedStoryboardCount, isDesktop ? 0 : 2);

		LOG_OUTPUT("Close menu and sub menu");
		await TestServices.Utilities.InjectBackButtonPress();
		await TestServices.WindowHelper.WaitForIdle();
		VERIFY_ARE_EQUAL(startedStoryboardCount, isDesktop ? 0 : 4);
	}

	private class StoryboardMonitorWrapper
	{
		// TODO Uno: nothing reports the started storyboards.
		public void AttachStartedHandler(Action<Storyboard, UIElement> handler)
		{
		}
	}

	[TestMethod]
	public async Task ValidateRightClickChaining()
	{
		Button button1 = null;
		Button button2 = null;
		MenuFlyout menuFlyout = null;

		var menuFlyoutOpenedEvent = new Event();
		var menuFlyoutClosedEvent = new Event();
		var rightTappedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));
		var rightTappedRegistration = CreateSafeEventRegistration<Button, RightTappedEventHandler>("RightTapped");

		await RunOnUIThread(() =>

		   {
			   var rootPanel = (StackPanel)(XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left'> " +
				"  <Button x:Name='button1' Content='button.righttapped' VerticalAlignment='Center' HorizontalAlignment='Left' FontSize='25' Padding='25,10' Margin='50'> " +
				"  </Button> " +
				"  <Button x:Name='button2' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Left' FontSize='25' Padding='25,10' Margin='50'> " +
				"    <Button.Flyout> " +
				"      <MenuFlyout Placement='Bottom'> " +
				"        <MenuFlyoutItem FontSize='30' Text='SUPERMAN' Foreground='RoyalBlue' Width='300' /> " +
				"        <MenuFlyoutSeparator Width='300' /> " +
				"        <ToggleMenuFlyoutItem FontSize='30' Text='THE FLASH' Foreground='RoyalBlue' Width='300' IsChecked='False' /> " +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</StackPanel>"));

			   VERIFY_IS_NOT_NULL(rootPanel);
			   TestServices.WindowHelper.WindowContent = rootPanel;

			   button1 = (Button)(rootPanel.FindName("button1"));
			   VERIFY_IS_NOT_NULL(button1);
			   button2 = (Button)(rootPanel.FindName("button2"));
			   VERIFY_IS_NOT_NULL(button2);

			   rightTappedRegistration.Attach(button1, (object sender, RightTappedRoutedEventArgs e) =>
			   {
				   rightTappedEvent.Set();
			   });

			   menuFlyout = (MenuFlyout)(button2.Flyout);
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
		TestServices.InputHelper.Tap(button2);
		await menuFlyoutOpenedEvent.WaitForDefault();

		// Inject right-click.
		TestServices.InputHelper.MoveMouse(button1);
		TestServices.InputHelper.MouseButtonDown(button1, 0, 0, MouseButton.Right);
		TestServices.InputHelper.MouseButtonUp(button1, 0, 0, MouseButton.Right);
		await TestServices.WindowHelper.WaitForIdle();

		// Make sure that the right tap tiggered the flyout's light dismiss
		await menuFlyoutClosedEvent.WaitForDefault();

		// Make sure that the right tap gesture was chained through the MenuFlyout's light dismiss layer
		// and received by the next hit target - button1
		await rightTappedEvent.WaitForDefault();
	}


	private async Task<MenuFlyout> CreateMenuFlyoutLongItemsFromXaml()
	{
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		   {
			   // TODO: Replace the inline MaxHeight style with a test-level window size override
			   // once SetWindowSizeOverride is implemented for the runtime test harness (#23018).
			   // The explicit MaxHeight is required so ScrollableHeight > 0 is deterministic on
			   // SkiaWasm / SkiaIOS, where VisibleBounds is otherwise unbounded.
			   menuFlyout = (MenuFlyout)(XamlReader.Load(
				   "<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				   "    <MenuFlyout.MenuFlyoutPresenterStyle>" +
				   "        <Style TargetType='MenuFlyoutPresenter'>" +
				   "            <Setter Property='MaxHeight' Value='400' />" +
				   "        </Style>" +
				   "    </MenuFlyout.MenuFlyoutPresenterStyle>" +
				   "    <MenuFlyoutItem>Menu item 0</MenuFlyoutItem>" +
				   "    <MenuFlyoutSeparator/>" +
				   "    <MenuFlyoutSubItem x:Name='subItem1' Text='Menu sub item 1'>" +
				   "        <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>" +
				   "        <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>" +
				   "        <MenuFlyoutSeparator/>" +
				   "        <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 2.3</ToggleMenuFlyoutItem>" +
				   "        <MenuFlyoutSeparator/>" +
				   "        <MenuFlyoutSubItem  x:Name='subItem2' Text='Menu sub item 2.4'>" +
				   "            <MenuFlyoutItem>Menu item 3.1</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.2</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.3</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.4</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.5</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.6</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.7</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.8</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.9</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.10</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.11</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.12</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.13</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.14</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.15</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.16</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.17</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.18</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.19</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.20</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.21</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.22</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.23</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.24</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.25</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.26</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.27</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.28</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.29</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Menu item 3.30</MenuFlyoutItem>" +
				   "        </MenuFlyoutSubItem>" +
				   "    </MenuFlyoutSubItem>" +
				   "    <MenuFlyoutSeparator/>" +
				   "    <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split menu item 2'>" +
				   "        <MenuFlyoutItem>Split item 4.1</MenuFlyoutItem>" +
				   "        <MenuFlyoutItem>Split item 4.2</MenuFlyoutItem>" +
				   "        <MenuFlyoutSeparator/>" +
				   "        <ToggleMenuFlyoutItem IsChecked='True'>Toggle split item 4.3</ToggleMenuFlyoutItem>" +
				   "        <MenuFlyoutSeparator/>" +
				   "        <SplitMenuFlyoutItem x:Name='splitItem2' Text='Split sub item 4.4'>" +
				   "            <MenuFlyoutItem>Split item 5.1</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.2</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.3</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.4</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.5</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.6</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.7</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.8</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.9</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.10</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.11</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.12</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.13</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.14</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.15</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.16</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.17</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.18</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.19</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.20</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.21</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.22</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.23</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.24</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.25</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.26</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.27</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.28</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.29</MenuFlyoutItem>" +
				   "            <MenuFlyoutItem>Split item 5.30</MenuFlyoutItem>" +
				   "        </SplitMenuFlyoutItem>" +
				   "    </SplitMenuFlyoutItem>" +
				   "    <MenuFlyoutSeparator/>" +
				   "    <MenuFlyoutItem>Menu item 2</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 3</MenuFlyoutItem>" +
				   "    <MenuFlyoutSeparator/>" +
				   "    <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 3</ToggleMenuFlyoutItem>" +
				   "    <MenuFlyoutSeparator/>" +
				   "    <MenuFlyoutItem>Menu item 4</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 5</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 6</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 7</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 8</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 9</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 10</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 11</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 12</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 13</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 14</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 15</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 16</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 17</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 18</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 19</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 20</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 21</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 22</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 23</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 24</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 25</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 26</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 27</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 28</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 29</MenuFlyoutItem>" +
				   "    <MenuFlyoutItem>Menu item 30</MenuFlyoutItem>" +
				   "</MenuFlyout>"));
		   });

		return menuFlyout;
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaIOS)] // Cascading sub-menu closes on mouse move past the sub-item boundary https://github.com/unoplatform/uno/issues/23032
	[BackendCondition(ConditionMode.Exclude, RuntimeTestBackends.WebGpu)] // Same as above: the slower frames of a GPU-less agent expose it
	public async Task ValidateSubMenuItemWithLongItems()
	{
		Rect subItemBounds = default;
		Rect subPresenterBounds = default;
		Button button1 = null;
		MenuFlyoutSubItem subItem = null;
		IList<MenuFlyoutItemBase> items = null;
		IList<MenuFlyoutItemBase> subItems = null;
		ScrollViewer scrollViewer = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = await CreateMenuFlyoutLongItemsFromXaml();

		await RunOnUIThread(() =>
		{
			rootPanel.RequestedTheme = ElementTheme.Light;
			button1 = (Button)(rootPanel.FindName("button1"));
#if HAS_UNO
			// Uno: the test window is what's smaller than the sub menus here, where upstream has a small test monitor.
			global::Uno.UI.FeatureConfiguration.Popup.UseWindowedPopups = false;
#endif
		});

		// Set the mouse operation for showing the ScrollBar on the long menu items
		TestServices.InputHelper.LeftMouseClick(button1);

		// Show the long main MenuFlyout
		await ShowMenuFlyout(menuFlyout, button1, 200, 50);

		var subPresenter = await GetCurrentPresenter();

		await RunOnUIThread(() =>
		{
			scrollViewer = (ScrollViewer)(TreeHelper.GetVisualChildByName(subPresenter, "MenuFlyoutPresenterScrollViewer"));
			LOG_OUTPUT("Scrollable Height=%f", scrollViewer.ScrollableHeight);
		});

		VERIFY_IS_TRUE(scrollViewer.ScrollableHeight > 0);

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
			subItem = (MenuFlyoutSubItem)(items[2]);
		});

		await MoveToSubMenuItem(subItem);

		subPresenter = await GetCurrentPresenter();

		await RunOnUIThread(async () =>
		{
			subItemBounds = await ControlHelper.GetBounds((FrameworkElement)(subItem));
			LOG_OUTPUT("MenuFlyoutSubItem bounds left=%f top=%f width=%f height=%f", subItemBounds.Left, subItemBounds.Top, subItemBounds.Width, subItemBounds.Height);
		});

		// Move the mouse to the boundary of sub menu item then move to the sub presenter
		Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(subItemBounds.Left + subItemBounds.Width, subItemBounds.Top + subItemBounds.Height / 2));
		Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(subItemBounds.Left + subItemBounds.Width + 100, subItemBounds.Top + subItemBounds.Height / 2));
		await TestServices.WindowHelper.WaitForIdle();

		// Verify the sub presenter
		VERIFY_IS_TRUE(subPresenter == await GetCurrentPresenter());

		// Get the sub items to invoke the second menu sub presenter
		await RunOnUIThread(() =>
		{
			subItems = subItem.Items;
		});

		var subItem2 = await GetSubItem(subItems);

		// Show the second menu sub presenter
		await TapSubMenuItem(subItem2);

		var subPresenter2 = await GetCurrentPresenter();

		await RunOnUIThread(async () =>
		{
			subPresenterBounds = await ControlHelper.GetBounds((FrameworkElement)(subPresenter2));
			LOG_OUTPUT("Second SubPresenter bounds left=%f top=%f width=%f height=%f", subPresenterBounds.Left, subPresenterBounds.Top, subPresenterBounds.Width, subPresenterBounds.Height);
		});

		Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(subPresenterBounds.Left + subPresenterBounds.Width / 2, subPresenterBounds.Top + subPresenterBounds.Height / 2));
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			scrollViewer = (ScrollViewer)(TreeHelper.GetVisualChildByName(subPresenter2, "MenuFlyoutPresenterScrollViewer"));
			LOG_OUTPUT("Scrollable Height=%f", scrollViewer.ScrollableHeight);
		});
		VERIFY_IS_TRUE(scrollViewer.ScrollableHeight > 0);

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemWithLongItems()
	{
		Rect splitItemBounds = default;
		Rect subPresenterBounds = default;
		Button button1 = null;
		SplitMenuFlyoutItem splitItem = null;
		IList<MenuFlyoutItemBase> items = null;
		IList<MenuFlyoutItemBase> subItems = null;
		ScrollViewer scrollViewer = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = await CreateMenuFlyoutLongItemsFromXaml();

		await RunOnUIThread(() =>
		{
			rootPanel.RequestedTheme = ElementTheme.Light;
			button1 = (Button)(rootPanel.FindName("button1"));
		});

		// Set the mouse operation for showing the ScrollBar on the long menu items
		TestServices.InputHelper.LeftMouseClick(button1);

		// Show the long main MenuFlyout
		await ShowMenuFlyout(menuFlyout, button1, 200, 50);

		var subPresenter = await GetCurrentPresenter();

		await RunOnUIThread(() =>
		{
			scrollViewer = (ScrollViewer)(TreeHelper.GetVisualChildByName(subPresenter, "MenuFlyoutPresenterScrollViewer"));
			LOG_OUTPUT("Scrollable Height=%f", scrollViewer.ScrollableHeight);
		});

		VERIFY_IS_TRUE(scrollViewer.ScrollableHeight > 0);

		await RunOnUIThread(() =>
		{
			items = menuFlyout.Items;
			// The SplitMenuFlyoutItem with submenu is at index 4 (after Menu item 0, separator, MenuFlyoutSubItem, separator)
			splitItem = items[4] as SplitMenuFlyoutItem;
		});

		VERIFY_IS_NOT_NULL(splitItem);
		await MoveToSplitMenuItemSecondary(splitItem);

		subPresenter = await GetCurrentPresenter();

		await RunOnUIThread(async () =>
		{
			splitItemBounds = await ControlHelper.GetBounds((FrameworkElement)(splitItem));
			LOG_OUTPUT("SplitMenuFlyoutItem bounds left=%f top=%f width=%f height=%f", splitItemBounds.Left, splitItemBounds.Top, splitItemBounds.Width, splitItemBounds.Height);
		});

		// Move the mouse to the boundary of split menu item then move to the sub presenter
		Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(splitItemBounds.Left + splitItemBounds.Width, splitItemBounds.Top + splitItemBounds.Height / 2));
		Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(splitItemBounds.Left + splitItemBounds.Width + 100, splitItemBounds.Top + splitItemBounds.Height / 2));
		await TestServices.WindowHelper.WaitForIdle();

		// Verify the sub presenter
		VERIFY_IS_TRUE(subPresenter == await GetCurrentPresenter());

		// Get the sub items to invoke the nested split menu sub presenter
		SplitMenuFlyoutItem nestedSplitItem = null;
		await RunOnUIThread(() =>
		{
			subItems = splitItem.Items;
			// The nested SplitMenuFlyoutItem with submenu is the last item in the collection
			nestedSplitItem = subItems[subItems.Count - 1] as SplitMenuFlyoutItem;
		});

		VERIFY_IS_NOT_NULL(nestedSplitItem);

		// Show the nested split menu sub presenter
		await TapSplitMenuItemSecondary(nestedSplitItem);

		var subPresenter2 = await GetCurrentPresenter();

		await RunOnUIThread(async () =>
		{
			subPresenterBounds = await ControlHelper.GetBounds((FrameworkElement)(subPresenter2));
			LOG_OUTPUT("Nested SplitMenuFlyoutItem SubPresenter bounds left=%f top=%f width=%f height=%f", subPresenterBounds.Left, subPresenterBounds.Top, subPresenterBounds.Width, subPresenterBounds.Height);
		});

		Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(subPresenterBounds.Left + subPresenterBounds.Width / 2, subPresenterBounds.Top + subPresenterBounds.Height / 2));
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			scrollViewer = (ScrollViewer)(TreeHelper.GetVisualChildByName(subPresenter2, "MenuFlyoutPresenterScrollViewer"));
			LOG_OUTPUT("Nested Scrollable Height=%f", scrollViewer.ScrollableHeight);
		});
		VERIFY_IS_TRUE(scrollViewer.ScrollableHeight > 0);

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	private async Task OpenSubItemWithMouse(MenuFlyoutSubItem menuFlyoutSubItem)
	{
		// Open the MenuFlyoutSubItem by moving the mouse on the first sub item
		TestServices.InputHelper.MoveMouse(menuFlyoutSubItem);
		// Wait for the sub menu to open. It opens after a delay - clicking and waiting for idle doesn't open it.
		// MSFT: 4815582 <MenuFlyout sub items don't expand on mouse click - they need to wait for the timeout> tracks this bug.
		//await TestServices.WindowHelper.SynchronouslyTickUIThread(500);
		await Task.Delay(500);
	}

	[TestMethod]
	public async Task ValidateOpenMultiSubMenuItemByMouse()
	{


		Button button = null;
		MenuFlyout menuFlyout = null;
		MenuFlyoutSubItem menuFlyoutSubItem = null;
		Rect menuFlyoutSubItem1Bounds = default;
		Rect menuFlyoutSubItem2Bounds = default;

		var menuFlyoutOpenedEvent = new Event();
		var menuFlyoutClosedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		var menuFlyoutSubItemClosedEvent = new Event();
		var subItemClosedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>("Closed");

		await RunOnUIThread(() =>

		   {
			   var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left'> " +
				"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Left' FontSize='25' Padding='25,10' Margin='50'> " +
				"    <Button.Flyout> " +
				"      <MenuFlyout Placement='Bottom'> " +
				"        <MenuFlyoutItem Text='Item 1' /> " +
				"        <MenuFlyoutSubItem Text='Sub Item 1'> " +
				"            <MenuFlyoutItem>Sub item 1.1</MenuFlyoutItem> " +
				"            <MenuFlyoutItem>Sub item 1.2</MenuFlyoutItem> " +
				"        </MenuFlyoutSubItem> " +
				"        <MenuFlyoutSeparator Width='300' /> " +
				"        <MenuFlyoutItem Text='Item 2' /> " +
				"        <MenuFlyoutSubItem Text='Sub Item 2'> " +
				"            <MenuFlyoutItem>Sub item 2.1</MenuFlyoutItem> " +
				"            <MenuFlyoutItem>Sub item 2.2</MenuFlyoutItem> " +
				"        </MenuFlyoutSubItem> " +
				"        <MenuFlyoutSeparator Width='300' /> " +
				"        <MenuFlyoutItem Text='Item 3' /> " +
				"        <MenuFlyoutSubItem Text='Sub Item 3'> " +
				"            <MenuFlyoutItem>Sub item 3.1</MenuFlyoutItem> " +
				"            <MenuFlyoutItem>Sub item 3.2</MenuFlyoutItem> " +
				"        </MenuFlyoutSubItem> " +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			   TestServices.WindowHelper.WindowContent = rootPanel;

			   button = (Button)(rootPanel.FindName("button"));
			   menuFlyout = (MenuFlyout)(button.Flyout);
			   menuFlyoutSubItem = (MenuFlyoutSubItem)(menuFlyout.Items[1]);

			   openedRegistration.Attach(menuFlyout, (s, e) =>

			{
				menuFlyoutOpenedEvent.Set();
			});

			   closedRegistration.Attach(menuFlyout, (s, e) =>

			   {
				   menuFlyoutClosedEvent.Set();
			   });
		   });
		await TestServices.WindowHelper.WaitForIdle();

		// Open the MenuFlyout by tapping the button
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Open the MenuFlyoutSubItem by moving the mouse on the first sub item
		await OpenSubItemWithMouse(menuFlyoutSubItem);

		var presenter = await GetCurrentPresenter();
		await RunOnUIThread(async () =>
		{
			menuFlyoutSubItem1Bounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("MenuFlyoutSubItem1 bounds left=%f top=%f width=%f height=%f", menuFlyoutSubItem1Bounds.Left, menuFlyoutSubItem1Bounds.Top, menuFlyoutSubItem1Bounds.Width, menuFlyoutSubItem1Bounds.Height);

			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(
				button.XamlRoot);

			var popup = popups[0];
			subItemClosedRegistration.Attach(popup, (s, e) =>
			{
				menuFlyoutSubItemClosedEvent.Set();
			});
		});

		// Move the mouse to the out of the MenuFlyout bounds
		Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(menuFlyoutSubItem1Bounds.Left + menuFlyoutSubItem1Bounds.Width / 2, 0));
		Private.Infrastructure.TestServices.InputHelper.MoveMouse(new Point(menuFlyoutSubItem1Bounds.Left + menuFlyoutSubItem1Bounds.Width / 2, 1));
		await menuFlyoutSubItemClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// Get the second sub item
			menuFlyoutSubItem = (MenuFlyoutSubItem)(menuFlyout.Items[4]);
		});

		// Move the mouse to the second sub menu item to close the previous sub item and open the second sub item
		await OpenSubItemWithMouse(menuFlyoutSubItem);

		presenter = await GetCurrentPresenter();
		await RunOnUIThread(async () =>
		{
			menuFlyoutSubItem2Bounds = await ControlHelper.GetBounds((FrameworkElement)(presenter));
			LOG_OUTPUT("MenuFlyoutSubItem2 bounds left=%f top=%f width=%f height=%f", menuFlyoutSubItem2Bounds.Left, menuFlyoutSubItem2Bounds.Top, menuFlyoutSubItem2Bounds.Width, menuFlyoutSubItem2Bounds.Height);

			VERIFY_IS_TRUE(menuFlyoutSubItem1Bounds.Top < menuFlyoutSubItem2Bounds.Top);

			// Close the MenuFlyout
			menuFlyout.Hide();
		});

		await menuFlyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task ValidateRequestedThemeOnPresenterTakesEffect()
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
				"  <Grid.Resources> " +
				"      <ResourceDictionary> " +
				"          <ResourceDictionary.ThemeDictionaries> " +
				"              <ResourceDictionary x:Key='Default'> " +
				"                  <SolidColorBrush x:Key='CustomPresenterBackground' Color='Green' /> " +
				"              </ResourceDictionary> " +
				"              <ResourceDictionary x:Key='Light'> " +
				"                  <SolidColorBrush x:Key='CustomPresenterBackground' Color='Blue' /> " +
				"              </ResourceDictionary> " +
				"          </ResourceDictionary.ThemeDictionaries> " +
				"      </ResourceDictionary> " +
				"  </Grid.Resources> " +
				"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Left' FontSize='25' Padding='25,10' Margin='50' RequestedTheme='Dark'> " +
				"    <Button.Flyout> " +
				"      <MenuFlyout Placement='Bottom'> " +
				"        <MenuFlyout.MenuFlyoutPresenterStyle> " +
				"          <Style TargetType='MenuFlyoutPresenter'> " +
				"            <Setter Property='Background' Value='{ThemeResource CustomPresenterBackground}' /> " +
				"            <Setter Property='RequestedTheme' Value='Light' /> " +
				"          </Style> " +
				"        </MenuFlyout.MenuFlyoutPresenterStyle> " +
				"        <MenuFlyoutItem Text='Item 1' /> " +
				"        <MenuFlyoutItem Text='Item 2' /> " +
				"        <MenuFlyoutItem Text='Item 3' /> " +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;

			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});

			closedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutClosedEvent.Set();
			});
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Open the MenuFlyout by tapping the button
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		var presenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			var backgroundColor = ((SolidColorBrush)presenter.Background).Color;

			VERIFY_ARE_EQUAL(backgroundColor.A, 0xFF);
			VERIFY_ARE_EQUAL(backgroundColor.R, 0x00);
			VERIFY_ARE_EQUAL(backgroundColor.G, 0x00);
			VERIFY_ARE_EQUAL(backgroundColor.B, 0xFF);
		});

		await RunOnUIThread(() =>
		{
			// Close the MenuFlyout
			menuFlyout.Hide();
		});

		await menuFlyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Ignore("A presenter style's {ThemeResource} from the opener's Resources keeps its first value when the opener's RequestedTheme changes.")]
	public async Task ValidateCanChangeRequestedThemeOnPresenterOwner()
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
				"  <Grid.Resources> " +
				"      <ResourceDictionary> " +
				"          <ResourceDictionary.ThemeDictionaries> " +
				"              <ResourceDictionary x:Key='Default'> " +
				"                  <SolidColorBrush x:Key='CustomPresenterBackground' Color='Green' /> " +
				"              </ResourceDictionary> " +
				"              <ResourceDictionary x:Key='Light'> " +
				"                  <SolidColorBrush x:Key='CustomPresenterBackground' Color='Blue' /> " +
				"              </ResourceDictionary> " +
				"          </ResourceDictionary.ThemeDictionaries> " +
				"      </ResourceDictionary> " +
				"  </Grid.Resources> " +
				"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Center' HorizontalAlignment='Left' FontSize='25' Padding='25,10' Margin='50' RequestedTheme='Dark'> " +
				"    <Button.Flyout> " +
				"      <MenuFlyout Placement='Bottom'> " +
				"        <MenuFlyout.MenuFlyoutPresenterStyle> " +
				"          <Style TargetType='MenuFlyoutPresenter'> " +
				"            <Setter Property='Background' Value='{ThemeResource CustomPresenterBackground}' /> " +
				"          </Style> " +
				"        </MenuFlyout.MenuFlyoutPresenterStyle> " +
				"        <MenuFlyoutItem Text='Item 1' /> " +
				"        <MenuFlyoutItem Text='Item 2' /> " +
				"        <MenuFlyoutItem Text='Item 3' /> " +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;

			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});

			closedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutClosedEvent.Set();
			});
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Open the MenuFlyout by tapping the button
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		var presenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			var backgroundColor = ((SolidColorBrush)presenter.Background).Color;

			VERIFY_ARE_EQUAL(backgroundColor.A, 0xFF);
			VERIFY_ARE_EQUAL(backgroundColor.R, 0x00);
			VERIFY_ARE_EQUAL(backgroundColor.G, 0x80);
			VERIFY_ARE_EQUAL(backgroundColor.B, 0x00);
		});

		await RunOnUIThread(() =>
		{
			// Close the MenuFlyout
			menuFlyout.Hide();
		});

		await menuFlyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			button.RequestedTheme = ElementTheme.Light;
		});

		// Open the MenuFlyout by tapping the button
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		presenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			var backgroundColor = ((SolidColorBrush)presenter.Background).Color;

			VERIFY_ARE_EQUAL(backgroundColor.A, 0xFF);
			VERIFY_ARE_EQUAL(backgroundColor.R, 0x00);
			VERIFY_ARE_EQUAL(backgroundColor.G, 0x00);
			VERIFY_ARE_EQUAL(backgroundColor.B, 0xFF);
		});

		await RunOnUIThread(() =>
		{
			// Close the MenuFlyout
			menuFlyout.Hide();
		});

		await menuFlyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	private async Task PerformFlowDirectionTest(bool hasElement, bool hasPoint, bool isRTL, bool expectSuccess)
	{
		Button button1 = null;
		var openedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var closedEvent = new Event();
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		var menuFlyout = await CreateMenuFlyoutWithSubItem();

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid Background='Orange' " +
				" xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <Button x:Name='button1' Content='Button' Width='100' Height='50' />" +
				"</Grid>"));

			button1 = (Button)(rootPanel.FindName("button1"));

			if (isRTL)
			{
				rootPanel.FlowDirection = FlowDirection.RightToLeft;
			}

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				openedEvent.Set();
			});

			closedRegistration.Attach(menuFlyout, (s, e) =>
			{
				closedEvent.Set();
			});

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT($"Is Relative={hasElement} Has Offset={hasPoint} Is RTL={isRTL}");

			Exception caughtException = null;
			try
			{
				if (hasPoint)
				{
					if (hasElement)
					{
						menuFlyout.ShowAt(button1, new Point(50, 50));
					}
					else
					{
						menuFlyout.ShowAt(null, new Point(50, 50));
					}
				}
				else
				{
					menuFlyout.ShowAt(button1);
				}
			}
			catch (Exception ex)
			{
				caughtException = ex;
				if (expectSuccess)
				{
					throw;
				}
				else
				{
					openedEvent.Set();
				}
			}

			if (!expectSuccess)
			{
				VERIFY_IS_NOT_NULL(caughtException);
			}
		});
		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(button1.XamlRoot);

			if (expectSuccess)
			{
				var popup = popups[popups.Count - 1];
				var child = popup.Child;

				VERIFY_IS_TRUE(popup.IsOpen);

				if (isRTL)
				{
					VERIFY_ARE_EQUAL(FlowDirection.RightToLeft, ((FrameworkElement)child).FlowDirection);
				}
				else
				{
					VERIFY_ARE_EQUAL(FlowDirection.LeftToRight, ((FrameworkElement)child).FlowDirection);
				}
			}
			else
			{
				VERIFY_ARE_EQUAL(0, popups.Count);
			}
		});
		await TestServices.WindowHelper.WaitForIdle();

		if (expectSuccess)
		{
			await FlyoutHelper.HideFlyout(menuFlyout);
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task ValidateShowAtFlowDirection()
	{
		//Test Case:ShowAt(element)
		await PerformFlowDirectionTest(true, false, false, true);
		//Test Case:ShowAt(element,point)
		await PerformFlowDirectionTest(true, true, false, true);
		//Test Case:ShowAt(null,point)
		await PerformFlowDirectionTest(false, true, false, false);
		//RTL
		//Test Case:ShowAt(element)
		await PerformFlowDirectionTest(true, false, true, true);
		//Test Case:ShowAt(element,point)
		await PerformFlowDirectionTest(true, true, true, true);
		//Test Case:ShowAt(null,point)
		await PerformFlowDirectionTest(false, true, true, false);
	}

	[TestMethod]
	[Ignore("Pen input injection is not available: FlyoutHelper.OpenFlyout(Pen) is not implemented and InputHelper.PenHold does not exist")]
	public async Task ValidateFlyoutSizingForDifferentInputModes()
	{
		// Upstream disables the keyboard injection event wait here (KeyboardInjectionIgnoreEventWaitOverride); not applicable to Uno.

		const double expectedFlyoutWidth_Touch = 240;
		const double expectedFlyoutWidth_NonTouch = 96;
		Thickness expectedFlyoutContentMargin = new Thickness(0, 4, 0, 4);

		// Narrow should occur when interacting using a mouse, pen, or keyboard.
		// Wide should occur when interacting using touch, a gamepad, or a remote.
		Thickness expectedMenuFlyoutItemPadding_Narrow = new Thickness(12, 5, 12, 7);
		Thickness expectedMenuFlyoutItemPadding_Wide = new Thickness(12, 11, 12, 13);

		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

		Button button = null;
		MenuFlyout menuFlyout = null;
		MenuFlyoutSubItem menuFlyoutSubItem = null;

		FrameworkElement menuFlyoutRoot = null;
		ItemsPresenter menuFlyoutItemsPresenter = null;

		await RunOnUIThread(() =>
		{
			var root = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"          x:Name='root'> " +
				"    <Button x:Name='button' Content='button.flyout'> " +
				"        <Button.Flyout> " +
				"            <MenuFlyout> " +
				"                <MenuFlyoutItem Text='Item 1' /> " +
				"                <MenuFlyoutSeparator /> " +
				"                <ToggleMenuFlyoutItem Text='Item 2' /> " +
				"                <MenuFlyoutSubItem Text='Sub Item 1'> " +
				"                    <MenuFlyoutItem>Sub item 1</MenuFlyoutItem> " +
				"                    <MenuFlyoutItem>Sub item 2</MenuFlyoutItem> " +
				"                </MenuFlyoutSubItem> " +
				"            </MenuFlyout> " +
				"        </Button.Flyout> " +
				"    </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = root;

			button = (Button)(root.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);
			menuFlyoutSubItem = (MenuFlyoutSubItem)(menuFlyout.Items[3]);

			button.ContextFlyout = menuFlyout;
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open MenuFlyout via touch.  The items should have wide padding.");
		await FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Touch);

		TestServices.InputHelper.Tap(menuFlyoutSubItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			menuFlyoutRoot = (FrameworkElement)(TreeHelper.GetVisualChildByNameFromOpenPopups("MenuFlyoutPresenterScrollViewer", button));
			menuFlyoutItemsPresenter = TreeHelper.GetVisualChildByType<ItemsPresenter>(menuFlyoutRoot);
			VERIFY_IS_NOT_NULL(menuFlyoutRoot);
			VERIFY_IS_NOT_NULL(menuFlyoutItemsPresenter);

			VERIFY_ARE_EQUAL(menuFlyoutRoot.MinWidth, expectedFlyoutWidth_Touch);
			VERIFY_ARE_EQUAL(menuFlyoutItemsPresenter.Margin, expectedFlyoutContentMargin);
			await VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Wide);
		});
		await FlyoutHelper.HideFlyout(menuFlyout);

		LOG_OUTPUT("Open MenuFlyout via gamepad.  The items should have wide padding.");
		await FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Gamepad);

		await RunOnUIThread(() =>
		{
			menuFlyoutSubItem.Focus(FocusState.Keyboard);
		});
		await TestServices.WindowHelper.WaitForIdle();
		await CommonInputHelper.Accept(InputDevice.Gamepad);

		await RunOnUIThread(async () =>
		{
			menuFlyoutRoot = (FrameworkElement)(TreeHelper.GetVisualChildByNameFromOpenPopups("MenuFlyoutPresenterScrollViewer", button));
			menuFlyoutItemsPresenter = TreeHelper.GetVisualChildByType<ItemsPresenter>(menuFlyoutRoot);

			VERIFY_ARE_EQUAL(menuFlyoutRoot.MinWidth, expectedFlyoutWidth_Touch);
			VERIFY_ARE_EQUAL(menuFlyoutItemsPresenter.Margin, expectedFlyoutContentMargin);
			await VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Wide);
		});
		await FlyoutHelper.HideFlyout(menuFlyout);

		LOG_OUTPUT("Open MenuFlyout via pen.  The items should have narrow padding.");
		await FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Pen);

		await TestServices.WindowHelper.WaitForIdle();
		//Sub menu opens with pen hover and not with a tap, but InputHelper doesn't currently have a way to input pen hovers.
		//Luckily PenHold does the trick.
		// TODO Uno: InputHelper.PenHold does not exist.
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			menuFlyoutRoot = (FrameworkElement)(TreeHelper.GetVisualChildByNameFromOpenPopups("MenuFlyoutPresenterScrollViewer", button));
			menuFlyoutItemsPresenter = TreeHelper.GetVisualChildByType<ItemsPresenter>(menuFlyoutRoot);

			VERIFY_ARE_EQUAL(menuFlyoutRoot.MinWidth, expectedFlyoutWidth_NonTouch);
			VERIFY_ARE_EQUAL(menuFlyoutItemsPresenter.Margin, expectedFlyoutContentMargin);
			await VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Narrow);
		});
		await FlyoutHelper.HideFlyout(menuFlyout);

		LOG_OUTPUT("Open MenuFlyout via mouse.  The items should have narrow padding.");
		await FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Mouse);

		await OpenSubItemWithMouse(menuFlyoutSubItem);

		await RunOnUIThread(async () =>
		{
			VERIFY_ARE_EQUAL(menuFlyoutRoot.MinWidth, expectedFlyoutWidth_NonTouch);
			VERIFY_ARE_EQUAL(menuFlyoutItemsPresenter.Margin, expectedFlyoutContentMargin);
			await VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Narrow);
		});
		await FlyoutHelper.HideFlyout(menuFlyout);

		LOG_OUTPUT("Open MenuFlyout via keyboard.  The items should have narrow padding.");
		await FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Keyboard);

		// Move to the sub menu item from the first item that requires two down
		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();

		// Open the sub menu item by sending the right keyboard
		await TestServices.KeyboardHelper.Right();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			VERIFY_ARE_EQUAL(menuFlyoutRoot.MinWidth, expectedFlyoutWidth_NonTouch);
			VERIFY_ARE_EQUAL(menuFlyoutItemsPresenter.Margin, expectedFlyoutContentMargin);
			await VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Narrow);
		});
		await FlyoutHelper.HideFlyout(menuFlyout);

		LOG_OUTPUT("Open MenuFlyout programmatically.  The items should have the same padding as before.");
		await FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Programmatic_ShowAt);
		await RunOnUIThread(async () =>
		{
			VERIFY_ARE_EQUAL(menuFlyoutRoot.MinWidth, expectedFlyoutWidth_NonTouch);
			VERIFY_ARE_EQUAL(menuFlyoutItemsPresenter.Margin, expectedFlyoutContentMargin);
			await VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Narrow);
		});
		await FlyoutHelper.HideFlyout(menuFlyout);

		LOG_OUTPUT("Open MenuFlyout via gamepad.  The items should have wide padding.");
		await FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Gamepad);

		// Move to the sub menu item from the first item that requires two Gamepad Dpad down
		await TestServices.KeyboardHelper.GamepadDpadDown();
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.KeyboardHelper.GamepadDpadDown();
		await TestServices.WindowHelper.WaitForIdle();

		// Open the sub menu item by using Dpad right
		await TestServices.KeyboardHelper.GamepadDpadRight();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			VERIFY_ARE_EQUAL(menuFlyoutRoot.MinWidth, expectedFlyoutWidth_Touch);
			VERIFY_ARE_EQUAL(menuFlyoutItemsPresenter.Margin, expectedFlyoutContentMargin);
			await VerifyMenuFlyoutItemsPadding(menuFlyout.Items, expectedMenuFlyoutItemPadding_Wide);
		});
		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task ValidateMenuFlyoutSizeByTouch()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

		const double expectedMenuFlyoutWidth_Touch = 242;           // 240 (touch min width with touch) + 2 (margin)
		const double expectedMenuFlyoutHeight_Touch = 169;          // Updated to include SplitMenuFlyoutItem
		const double expectedMenuFlyoutItemWidth_Touch = 240;       // 240 (touch min width with touch)
		const double expectedMenuFlyoutItemHeight_Touch = 40;
		const double textLineTolerance = 1; // TODO Uno: Skia's text lines are 1 taller than DirectWrite's, one for each item in the menu.
		const double expectedMenuFlyoutSeparatorWidth_Touch = 240;  // 240 (touch min width with touch)
		const double expectedMenuFlyoutSeparatorHeight_Touch = 3;
		const double expectedMenuFlyoutMinWidth_Touch = 242;        // 240 (touch min width with touch) + 2 (margin)
		const double expectedMenuFlyoutMinHeight_Touch = 32;

		Grid root = null;
		Button button = null;
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitMenuFlyoutItem = null;
		MenuFlyoutSubItem menuFlyoutSubItem = null;

		await RunOnUIThread(() =>
		{
			root = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"          x:Name='root'> " +
				"    <Button x:Name='button' Content='button.flyout'> " +
				"        <Button.Flyout> " +
				"            <MenuFlyout> " +
				"                <MenuFlyoutItem x:Name='firstMenuFlyoutItem' Text='Item 1' /> " +
				"                <MenuFlyoutSeparator x:Name='menuFlyoutSeparator' /> " +
				"                <ToggleMenuFlyoutItem x:Name='toggleMenuFlyoutItem' Text='Item 2' /> " +
				"                <SplitMenuFlyoutItem x:Name='splitMenuFlyoutItem' Text='Split Item 1'> " +
				"                    <MenuFlyoutItem>Split sub item 1</MenuFlyoutItem> " +
				"                    <MenuFlyoutItem>Split sub item 2</MenuFlyoutItem> " +
				"                </SplitMenuFlyoutItem> " +
				"                <MenuFlyoutSubItem x:Name='menuFlyoutSubItem' Text='Sub Item 1'> " +
				"                    <MenuFlyoutItem>Sub item 1</MenuFlyoutItem> " +
				"                    <MenuFlyoutItem>Sub item 2</MenuFlyoutItem> " +
				"                </MenuFlyoutSubItem> " +
				"            </MenuFlyout> " +
				"        </Button.Flyout> " +
				"    </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = root;

			button = (Button)(root.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);
			splitMenuFlyoutItem = (SplitMenuFlyoutItem)(menuFlyout.Items[3]);
			menuFlyoutSubItem = (MenuFlyoutSubItem)(menuFlyout.Items[4]);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Touch);

		var menuFlyoutPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			// Verify MenuFlyout actual width/height.
			VERIFY_ARE_EQUAL(menuFlyoutPresenter.ActualWidth, expectedMenuFlyoutWidth_Touch);
			VERIFY_ARE_VERY_CLOSE(menuFlyoutPresenter.ActualHeight, expectedMenuFlyoutHeight_Touch, 4 * textLineTolerance);

			// Verify MenuFlyoutItem actual width/height.
			var firstMenuFlyoutItem = (MenuFlyoutItem)(menuFlyoutPresenter.FindName("firstMenuFlyoutItem"));
			VERIFY_ARE_EQUAL(firstMenuFlyoutItem.ActualWidth, expectedMenuFlyoutItemWidth_Touch);
			VERIFY_ARE_VERY_CLOSE(firstMenuFlyoutItem.ActualHeight, expectedMenuFlyoutItemHeight_Touch, textLineTolerance);

			// Verify MenuFlyoutSeparator actual width/height.
			var menuFlyoutSeparator = (MenuFlyoutSeparator)(menuFlyoutPresenter.FindName("menuFlyoutSeparator"));
			VERIFY_ARE_EQUAL(menuFlyoutSeparator.ActualWidth, expectedMenuFlyoutSeparatorWidth_Touch);
			VERIFY_ARE_EQUAL(menuFlyoutSeparator.ActualHeight, expectedMenuFlyoutSeparatorHeight_Touch);

			// Verify ToggleMenuFlyout actual width/height.
			var toggleMenuFlyoutItem = (ToggleMenuFlyoutItem)(menuFlyoutPresenter.FindName("toggleMenuFlyoutItem"));
			VERIFY_ARE_EQUAL(toggleMenuFlyoutItem.ActualWidth, expectedMenuFlyoutItemWidth_Touch);
			VERIFY_ARE_VERY_CLOSE(toggleMenuFlyoutItem.ActualHeight, expectedMenuFlyoutItemHeight_Touch, textLineTolerance);

			// Verify SplitMenuFlyoutItem actual width/height.
			var splitMenuFlyoutItem = (SplitMenuFlyoutItem)(menuFlyoutPresenter.FindName("splitMenuFlyoutItem"));
			VERIFY_ARE_EQUAL(splitMenuFlyoutItem.ActualWidth, expectedMenuFlyoutItemWidth_Touch);
			VERIFY_ARE_VERY_CLOSE(splitMenuFlyoutItem.ActualHeight, expectedMenuFlyoutItemHeight_Touch, textLineTolerance);

			// Verify MenuFlyoutSubItem actual width/height.
			var menuFlyoutSubItem = (MenuFlyoutSubItem)(menuFlyoutPresenter.FindName("menuFlyoutSubItem"));
			VERIFY_ARE_EQUAL(menuFlyoutSubItem.ActualWidth, expectedMenuFlyoutItemWidth_Touch);
			VERIFY_ARE_VERY_CLOSE(menuFlyoutSubItem.ActualHeight, expectedMenuFlyoutItemHeight_Touch, textLineTolerance);
		});

		// Verify the empty MenuFlyout width/height with touch.
		TestServices.InputHelper.Tap(root);
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			menuFlyout = new MenuFlyout();
		});
		await ShowMenuFlyout(menuFlyout, button, 0, 50);
		menuFlyoutPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(menuFlyoutPresenter.ActualWidth, expectedMenuFlyoutMinWidth_Touch);
			VERIFY_ARE_EQUAL(menuFlyoutPresenter.ActualHeight, expectedMenuFlyoutMinHeight_Touch);
		});
		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task ValidateMenuFlyoutSizeByMouse()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

		const double expectedMenuFlyoutWidth_Mouse = 163;
		const double expectedMenuFlyoutHeight_Mouse = 137;
		const double expectedMenuFlyoutSubWidth_Mouse = 99;
		const double expectedMenuFlyoutSubHeight_Mouse = 70;
		const double expectedMenuFlyoutItemWidth_Mouse = 161;
		const double expectedMenuFlyoutItemHeight_Mouse = 32;
		const double textLineTolerance = 1; // TODO Uno: Skia's text lines are 1 taller than DirectWrite's, one for each item in the menu.
		const double textWidthTolerance = 8; // TODO Uno: Skia's text is wider than DirectWrite's, and this menu is as wide as its text.
		const double expectedMenuFlyoutSeparatorWidth_Mouse = 161;
		const double expectedMenuFlyoutSeparatorHeight_Mouse = 3;
		const double expectedMenuFlyoutMinWidth_Mouse = 98;
		const double expectedMenuFlyoutMinHeight_Mouse = 32;

		Grid root = null;
		Button button = null;
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitMenuFlyoutItem = null;
		MenuFlyoutSubItem menuFlyoutSubItem = null;

		await RunOnUIThread(() =>
		{
			root = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"          x:Name='root'> " +
				"    <Button x:Name='button' Content='button.flyout'> " +
				"        <Button.Flyout> " +
				"            <MenuFlyout> " +
				"                <MenuFlyoutItem x:Name='firstMenuFlyoutItem' Text='Item 1' /> " +
				"                <MenuFlyoutSeparator x:Name='menuFlyoutSeparator' /> " +
				"                <ToggleMenuFlyoutItem x:Name='toggleMenuFlyoutItem' Text='Item 2' /> " +
				"                <SplitMenuFlyoutItem x:Name='splitMenuFlyoutItem' Text='Split 1'> " +
				"                    <MenuFlyoutItem>Sub item 1</MenuFlyoutItem> " +
				"                    <MenuFlyoutItem>Sub item 2</MenuFlyoutItem> " +
				"                </SplitMenuFlyoutItem> " +
				"                <MenuFlyoutSubItem x:Name='menuFlyoutSubItem' Text='Sub Item 1'> " +
				"                    <MenuFlyoutItem>Sub item 1</MenuFlyoutItem> " +
				"                    <MenuFlyoutItem>Sub item 2</MenuFlyoutItem> " +
				"                </MenuFlyoutSubItem> " +
				"            </MenuFlyout> " +
				"        </Button.Flyout> " +
				"    </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = root;

			button = (Button)(root.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);
			splitMenuFlyoutItem = (SplitMenuFlyoutItem)(menuFlyout.Items[3]);
			menuFlyoutSubItem = (MenuFlyoutSubItem)(menuFlyout.Items[4]);
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Verif the MenuFlyout presenter's actual width/height with mouse.
		await FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Mouse);
		var menuFlyoutPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_VERY_CLOSE(menuFlyoutPresenter.ActualWidth, expectedMenuFlyoutWidth_Mouse, textWidthTolerance);
			VERIFY_ARE_VERY_CLOSE(menuFlyoutPresenter.ActualHeight, expectedMenuFlyoutHeight_Mouse, 4 * textLineTolerance);

			var firstMenuFlyoutItem = (MenuFlyoutItem)(menuFlyoutPresenter.FindName("firstMenuFlyoutItem"));
			VERIFY_ARE_VERY_CLOSE(firstMenuFlyoutItem.ActualWidth, expectedMenuFlyoutItemWidth_Mouse, textWidthTolerance);
			VERIFY_ARE_VERY_CLOSE(firstMenuFlyoutItem.ActualHeight, expectedMenuFlyoutItemHeight_Mouse, textLineTolerance);

			var menuFlyoutSeparator = (MenuFlyoutSeparator)(menuFlyoutPresenter.FindName("menuFlyoutSeparator"));
			VERIFY_ARE_VERY_CLOSE(menuFlyoutSeparator.ActualWidth, expectedMenuFlyoutSeparatorWidth_Mouse, textWidthTolerance);
			VERIFY_ARE_EQUAL(menuFlyoutSeparator.ActualHeight, expectedMenuFlyoutSeparatorHeight_Mouse);

			var toggleMenuFlyoutItem = (ToggleMenuFlyoutItem)(menuFlyoutPresenter.FindName("toggleMenuFlyoutItem"));
			VERIFY_ARE_VERY_CLOSE(toggleMenuFlyoutItem.ActualWidth, expectedMenuFlyoutItemWidth_Mouse, textWidthTolerance);
			VERIFY_ARE_VERY_CLOSE(toggleMenuFlyoutItem.ActualHeight, expectedMenuFlyoutItemHeight_Mouse, textLineTolerance);

			var splitMenuFlyoutItem = (SplitMenuFlyoutItem)(menuFlyoutPresenter.FindName("splitMenuFlyoutItem"));
			VERIFY_ARE_VERY_CLOSE(splitMenuFlyoutItem.ActualWidth, expectedMenuFlyoutItemWidth_Mouse, textWidthTolerance);
			VERIFY_ARE_VERY_CLOSE(splitMenuFlyoutItem.ActualHeight, expectedMenuFlyoutItemHeight_Mouse, textLineTolerance);

			var menuFlyoutSubItem = (MenuFlyoutSubItem)(menuFlyoutPresenter.FindName("menuFlyoutSubItem"));
			VERIFY_ARE_VERY_CLOSE(menuFlyoutSubItem.ActualWidth, expectedMenuFlyoutItemWidth_Mouse, textWidthTolerance);
			VERIFY_ARE_VERY_CLOSE(menuFlyoutSubItem.ActualHeight, expectedMenuFlyoutItemHeight_Mouse, textLineTolerance);
		});

		// Verif the MenuFlyoutSubItem presenter's actual width/height with mouse.
		await OpenSubItemWithMouse(menuFlyoutSubItem);
		menuFlyoutPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_VERY_CLOSE(menuFlyoutPresenter.ActualWidth, expectedMenuFlyoutSubWidth_Mouse, textWidthTolerance);
			VERIFY_ARE_VERY_CLOSE(menuFlyoutPresenter.ActualHeight, expectedMenuFlyoutSubHeight_Mouse, 2 * textLineTolerance);
		});
		await FlyoutHelper.HideFlyout(menuFlyout);

		// Verify the SplitMenuFlyoutItem submenu presenter's actual width/height with mouse.
		await FlyoutHelper.OpenFlyout(menuFlyout, button, FlyoutOpenMethod.Mouse);
		await MoveToSplitMenuItemSecondary(splitMenuFlyoutItem);
		menuFlyoutPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_VERY_CLOSE(menuFlyoutPresenter.ActualWidth, expectedMenuFlyoutSubWidth_Mouse, textWidthTolerance);
			VERIFY_ARE_VERY_CLOSE(menuFlyoutPresenter.ActualHeight, expectedMenuFlyoutSubHeight_Mouse, 2 * textLineTolerance);
		});
		await FlyoutHelper.HideFlyout(menuFlyout);

		// Verify the empty MenuFlyout width/height with mouse.
		TestServices.InputHelper.LeftMouseClick(root);
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			menuFlyout = new MenuFlyout();
		});
		await ShowMenuFlyout(menuFlyout, button, 0, 50, false /* forceTapAsPreviousInputMessage */);
		menuFlyoutPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(menuFlyoutPresenter.ActualWidth, expectedMenuFlyoutMinWidth_Mouse);
			VERIFY_ARE_EQUAL(menuFlyoutPresenter.ActualHeight, expectedMenuFlyoutMinHeight_Mouse);
		});
		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task ValidateScrollableItems()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

		const double expectedMenuFlyoutHeight = 300;
		const double expectedMenuFlyoutScrollableHeight = 1041;

		await RunOnUIThread(() =>
		{
			var root = new Grid();
			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		var menuFlyout = await CreateMenuFlyoutLongItemsFromXaml();

		// Specify the MenuFlyout height with 300
		await RunOnUIThread(() =>
		{
			var style = new Style(typeof(MenuFlyoutPresenter));
			style.Setters.Add(new Setter(MenuFlyoutPresenter.MaxHeightProperty, "300"));

			menuFlyout.MenuFlyoutPresenterStyle = style;
		});

		await ShowMenuFlyout(menuFlyout, null, 100, 200);
		var menuFlyoutPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(menuFlyoutPresenter.ActualHeight, expectedMenuFlyoutHeight);

			var scrollViewer = (ScrollViewer)(TreeHelper.GetVisualChildByName(menuFlyoutPresenter, "MenuFlyoutPresenterScrollViewer"));
#if HAS_UNO
			// TODO Uno: Skia's text lines are 1 taller than DirectWrite's, one for each of the 33 items. The 6 that remain aren't explained.
			VERIFY_ARE_VERY_CLOSE(scrollViewer.ScrollableHeight, expectedMenuFlyoutScrollableHeight, 33 + 6);
#else
			VERIFY_ARE_EQUAL(scrollViewer.ScrollableHeight, expectedMenuFlyoutScrollableHeight);
#endif
		});
		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateMenuFlyoutInVisibleBounds()
	{
		try
		{
#if HAS_UNO
			global::Uno.UI.FeatureConfiguration.Popup.UseWindowedPopups = false;
#endif
			// Flyout uses the visible bounds since TH2 platform.
			await ValidateMenuFlyoutPosition(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseVisible);
		}
		finally
		{
			_visibleBoundsOverride?.Dispose();
			_visibleBoundsOverride = null;
		}
	}

	private IDisposable _visibleBoundsOverride;

	private async Task SetVisibleBounds(Rect visibleBounds)
	{
		await RunOnUIThread(() =>
		{
			_visibleBoundsOverride?.Dispose();

			var windowBounds = Window.Current.Bounds;
			_visibleBoundsOverride = ScreenHelper.OverrideVisibleBounds(
				new Thickness(
					visibleBounds.Left,
					visibleBounds.Top,
					windowBounds.Width - visibleBounds.Right,
					windowBounds.Height - visibleBounds.Bottom),
				skipIfHasNativeUnsafeArea: false);
		});
	}

	private async Task ValidateMenuFlyoutPosition(Windows.UI.ViewManagement.ApplicationViewBoundsMode expectedBoundsMode)
	{
		// Set the specified visible bounds to verify the MenuFlyout position on the visible bounds.
		await SetVisibleBounds(new Rect(0, 50, 480, 700));

		var menuFlyout = await CreateMenuFlyoutWithSubItem();

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Background='Orange' > " +
				"    <TextBlock Text='Test MenuFlyout on the visible bounds.' FontSize='20' /> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Show the MenuFlout on the out of the visible bounds.
		await ShowMenuFlyout(menuFlyout, null, 100, 0, true /* forceTapAsPreviousInputMessage */);
		await TestServices.WindowHelper.WaitForIdle();

		var presenter = await GetCurrentPresenter();
		var menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)presenter);
		LOG_OUTPUT($"MenuFlyout bounds left={menuFlyoutBounds.Left} top={menuFlyoutBounds.Top} width={menuFlyoutBounds.Width} height={menuFlyoutBounds.Height}");

		if (expectedBoundsMode == Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseVisible)
		{
			VERIFY_ARE_EQUAL(menuFlyoutBounds.Top, 50);
		}
		else
		{
			VERIFY_IS_LESS_THAN(menuFlyoutBounds.Top, 50);
		}

		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});

		// Set the window with the landscape mode with specifying the visible bounds.
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(800, 480));
		await SetVisibleBounds(new Rect(50, 0, 700, 480));
		await TestServices.WindowHelper.WaitForIdle();

		// Show the MenuFlout on the out of the visible bounds.
		await ShowMenuFlyout(menuFlyout, null, 550, 100, true /* forceTapAsPreviousInputMessage */);
		await TestServices.WindowHelper.WaitForIdle();

		presenter = await GetCurrentPresenter();
		menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)presenter);
		LOG_OUTPUT($"MenuFlyout bounds left={menuFlyoutBounds.Left} top={menuFlyoutBounds.Top} width={menuFlyoutBounds.Width} height={menuFlyoutBounds.Height}");

		if (expectedBoundsMode == Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseVisible)
		{
			VERIFY_IS_LESS_THAN(menuFlyoutBounds.Left + menuFlyoutBounds.Width, 750);
		}
		else
		{
			VERIFY_IS_GREATER_THAN(menuFlyoutBounds.Left + menuFlyoutBounds.Width, 750);
		}

		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});
	}

	[TestMethod]
	public async Task ValidateOverlayBrush()
	{
		await FlyoutHelper.ValidateOverlayBrush<MenuFlyout>("MenuFlyoutLightDismissOverlayBackground");
	}

	[TestMethod]
	public async Task ValidateCanUseBothShowAtMethods()
	{
		var menuFlyout = await CreateMenuFlyout();
		Button button = null;

		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>(nameof(Button.Loaded));

		await RunOnUIThread(() =>
		{
			button = new Button();
			button.Content = "Button for flyout";

			loadedRegistration.Attach(button, (s, e) => { loadedEvent.Set(); });
			TestServices.WindowHelper.WindowContent = button;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		var openedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		openedRegistration.Attach(menuFlyout, (s, e) => { openedEvent.Set(); });

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Show the MenuFlyout first using ShowAt() with one parameter.");
			menuFlyout.ShowAt(button);
		});

		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.HideFlyout(menuFlyout);

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Now show the MenuFlyout using ShowAt() with two parameters.");
			menuFlyout.ShowAt(button, new Point(0, 0));
		});
		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.HideFlyout(menuFlyout);

		openedRegistration.Detach();

		LOG_OUTPUT("Now create a new MenuFlyout to test the other calling order.");
		menuFlyout = await CreateMenuFlyout();
		openedRegistration.Attach(menuFlyout, (s, e) => { openedEvent.Set(); });

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Show the MenuFlyout first using ShowAt() with two parameters.");
			menuFlyout.ShowAt(button, new Point(0, 0));
		});

		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.HideFlyout(menuFlyout);

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Now show the MenuFlyout using ShowAt() with one parameter.");
			menuFlyout.ShowAt(button);
		});
		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task ValidatePopupWindowedScrollingWithMouse()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));
		Grid root = null;
		Thumb thumb = null;
		Rect thumbBoundsBeforeDrag = default;
		Rect thumbBoundsAfterDrag = default;

		await RunOnUIThread(() =>
		{
			root = new Grid();
			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		var menuFlyout = await CreateMenuFlyoutLongItemsFromXaml();

		await ShowMenuFlyout(menuFlyout, null, 100, 200);
		var menuFlyoutPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			var scrollViewer = (ScrollViewer)(TreeHelper.GetVisualChildByName(menuFlyoutPresenter, "MenuFlyoutPresenterScrollViewer"));
			VERIFY_IS_NOT_NULL(scrollViewer);

			thumb = (Thumb)(TreeHelper.GetVisualChildByName(scrollViewer, "VerticalThumb"));
			VERIFY_IS_NOT_NULL(thumb);
		});
		thumbBoundsBeforeDrag = await ControlHelper.GetBounds(thumb);
		LOG_OUTPUT($"ValidatePopupWindowedScrollingWithMouse: Thumb bounds left={thumbBoundsBeforeDrag.Left} top={thumbBoundsBeforeDrag.Top} width={thumbBoundsBeforeDrag.Width} height={thumbBoundsBeforeDrag.Height}");
		await TestServices.WindowHelper.WaitForIdle();

		// Dragging the thumb to scroll ListViewItem content
		TestServices.InputHelper.MouseButtonDown(thumb, 0, 0, MouseButton.Left);
		LOG_OUTPUT("ValidatePopupWindowedScrollingWithMouse: Dragging the thumb to scroll the content.");
		MouseDrag(
			new Point(thumbBoundsBeforeDrag.Left + (thumbBoundsBeforeDrag.Width / 2), thumbBoundsBeforeDrag.Top + (thumbBoundsBeforeDrag.Height / 2)),
			new Point(thumbBoundsBeforeDrag.Left + (thumbBoundsBeforeDrag.Width / 2), thumbBoundsBeforeDrag.Top + (thumbBoundsBeforeDrag.Height / 2) + 200));
		await TestServices.WindowHelper.WaitForIdle();
		TestServices.InputHelper.MouseButtonUp(thumb, 0, 0, MouseButton.Left);
		await TestServices.WindowHelper.WaitForIdle();

		thumbBoundsAfterDrag = await ControlHelper.GetBounds(thumb);
		LOG_OUTPUT($"ValidatePopupWindowedScrollingWithMouse: Thumb bounds after dragging left={thumbBoundsAfterDrag.Left} top={thumbBoundsAfterDrag.Top} width={thumbBoundsAfterDrag.Width} height={thumbBoundsAfterDrag.Height}");
		// Conscious scrollbars feature results in the thumb expanding to 16 wide when the pointer is over the scrollbar, which it will be after dragging here
		// So checking the left and width of the thumb is not desirable
		VERIFY_IS_GREATER_THAN(thumbBoundsAfterDrag.Top, thumbBoundsBeforeDrag.Top);
		// Uno: the bounds come from float matrices.
		VERIFY_ARE_VERY_CLOSE(thumbBoundsBeforeDrag.Height, thumbBoundsAfterDrag.Height, 0.01);

		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	private void MouseDrag(Point from, Point to)
	{
		const int steps = 10;
		TestServices.InputHelper.MoveMouse(from);
		for (int i = 1; i <= steps; i++)
		{
			TestServices.InputHelper.MoveMouse(new Point(
				from.X + ((to.X - from.X) * i / steps),
				from.Y + ((to.Y - from.Y) * i / steps)));
		}
	}

	[TestMethod]
	public async Task ValidateKeyboardNavigationAfterClosingSubMenu()
	{
		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>(nameof(Grid.Loaded));

		var openedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));

		var closedEvent = new Event();
		var closedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		await RunOnUIThread(() =>
		{
			var rootGrid = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
				"      Width='400' Height='400'>" +
				"  <Button x:Name='button' Content='Button with Flyout' HorizontalAlignment='Center' VerticalAlignment='Center'>" +
				"    <Button.Flyout>" +
				"      <Flyout x:Name='flyout'>" +
				"        <StackPanel>" +
				"          <MenuFlyoutItem>MenuFlyoutItem</MenuFlyoutItem>" +
				"          <MenuFlyoutSubItem Text='MenuFlyoutSubItem'>" +
				"            <MenuFlyoutItem>MenuFlyoutItem</MenuFlyoutItem>" +
				"          </MenuFlyoutSubItem>" +
				"        </StackPanel>" +
				"      </Flyout>" +
				"    </Button.Flyout>" +
				"  </Button>" +
				"</Grid>"));
			VERIFY_IS_NOT_NULL(rootGrid);

			loadedRegistration.Attach(rootGrid, (s, e) =>
			{
				LOG_OUTPUT("Grid loaded.");
				loadedEvent.Set();
			});

			var button = (Button)(rootGrid.FindName("button"));
			VERIFY_IS_NOT_NULL(button);

			var flyout = (Flyout)(button.Flyout);
			VERIFY_IS_NOT_NULL(flyout);

			openedRegistration.Attach(flyout, (s, e) =>
			{
				LOG_OUTPUT("Flyout opened.");
				openedEvent.Set();
			});

			closedRegistration.Attach(flyout, (s, e) =>
			{
				LOG_OUTPUT("Flyout closed.");
				closedEvent.Set();
			});

			TestServices.WindowHelper.WindowContent = rootGrid;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Tab into the Button.");
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open menu.");
		await TestServices.KeyboardHelper.Enter();
		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Move down to the sub menu.");
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open the sub menu.");
		await CommonInputHelper.Right(InputDevice.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Close the sub menu.");
		await CommonInputHelper.Left(InputDevice.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Hit up arrow key.");
		await TestServices.KeyboardHelper.Up();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Close menu.");
		await TestServices.KeyboardHelper.Escape();
		await closedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task ValidateOpenedSubMenuFocusItemByKeyboard()
	{
		Button button1 = null;
		MenuFlyout menuFlyout = null;
		MenuFlyoutSubItem subItem1 = null;
		MenuFlyoutItem subItem11 = null;
		SplitMenuFlyoutItem splitItem1 = null;
		MenuFlyoutItem splitItem11 = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutSubItem x:Name='subItem1' Text='Menu sub item 1'>" +
				"        <MenuFlyoutItem x:Name='subItem1.1' >Menu item 1.1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem x:Name='subItem1.2' >Menu item 1.2</MenuFlyoutItem>" +
				"        <MenuFlyoutItem x:Name='subItem1.3' >Menu item 1.3</MenuFlyoutItem>" +
				"    </MenuFlyoutSubItem>" +
				"    <MenuFlyoutItem>item2</MenuFlyoutItem>" +
				"    <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split menu item 1'>" +
				"        <MenuFlyoutItem x:Name='splitItem1.1' >Split item 1.1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem x:Name='splitItem1.2' >Split item 1.2</MenuFlyoutItem>" +
				"        <MenuFlyoutItem x:Name='splitItem1.3' >Split item 1.3</MenuFlyoutItem>" +
				"    </SplitMenuFlyoutItem>" +
				"    <MenuFlyoutItem>item3</MenuFlyoutItem>" +
				"</MenuFlyout>"));

			button1 = (Button)(rootPanel.FindName("button1"));
		});

		await ShowMenuFlyout(menuFlyout, button1, -50, 50);

		// Open the sub menu by using the Keyboard Right key
		await CommonInputHelper.Right(InputDevice.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			var menuFlyoutPresenter = await GetCurrentPresenter();
			subItem11 = (MenuFlyoutItem)(menuFlyoutPresenter.FindName("subItem1.1"));
		});

		var subItem11GotFocusEvent = new Event();
		var subItem11gotFocusRegistration = CreateSafeEventRegistration<MenuFlyoutItem, RoutedEventHandler>(nameof(MenuFlyoutItem.GotFocus));
		subItem11gotFocusRegistration.Attach(subItem11, (s, e) => { subItem11GotFocusEvent.Set(); });

		// Close the sub menu by using the Keyboard Left key
		await CommonInputHelper.Left(InputDevice.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		// Re-open the sub menu to verify the focused first item
		await CommonInputHelper.Right(InputDevice.Keyboard);
		await subItem11GotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Close the sub menu
		await CommonInputHelper.Left(InputDevice.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			var menuFlyoutPresenter = await GetCurrentPresenter();
			subItem1 = (MenuFlyoutSubItem)(menuFlyoutPresenter.FindName("subItem1"));
		});

		// Re-open the sub menu to verify the focused first item
		await CommonInputHelper.Right(InputDevice.Keyboard);
		await subItem11GotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		var subItem1GotFocusEvent = new Event();
		var subItem1GotFocusRegistration = CreateSafeEventRegistration<MenuFlyoutSubItem, RoutedEventHandler>(nameof(MenuFlyoutSubItem.GotFocus));
		subItem1GotFocusRegistration.Attach(subItem1, (s, e) => { subItem1GotFocusEvent.Set(); });

		// Close the sub menu
		await TestServices.KeyboardHelper.Escape();
		await TestServices.WindowHelper.WaitForIdle();
		await subItem1GotFocusEvent.WaitForDefault();

		// Now test SplitMenuFlyoutItem focus behavior
		LOG_OUTPUT("Testing SplitMenuFlyoutItem submenu focus behavior.");

		// Navigate down to the split menu item
		await CommonInputHelper.Down(InputDevice.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();
		await CommonInputHelper.Down(InputDevice.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();
		await CommonInputHelper.Down(InputDevice.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		// Open the split menu item's sub menu by using the Keyboard Right key
		await CommonInputHelper.Right(InputDevice.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			var menuFlyoutPresenter = await GetCurrentPresenter();
			splitItem11 = (MenuFlyoutItem)(menuFlyoutPresenter.FindName("splitItem1.1"));
		});

		var splitItem11GotFocusEvent = new Event();
		var splitItem11gotFocusRegistration = CreateSafeEventRegistration<MenuFlyoutItem, RoutedEventHandler>(nameof(MenuFlyoutItem.GotFocus));
		splitItem11gotFocusRegistration.Attach(splitItem11, (s, e) => { splitItem11GotFocusEvent.Set(); });

		// Close the split menu's sub menu by using the Keyboard Left key
		await CommonInputHelper.Left(InputDevice.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		// Re-open the split menu's sub menu to verify the focused first item
		await CommonInputHelper.Right(InputDevice.Keyboard);
		await splitItem11GotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Close the split menu's sub menu
		await CommonInputHelper.Left(InputDevice.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			var menuFlyoutPresenter = await GetCurrentPresenter();
			splitItem1 = (SplitMenuFlyoutItem)(menuFlyoutPresenter.FindName("splitItem1"));
		});

		// Re-open the split menu's sub menu to verify the focused first item
		await CommonInputHelper.Right(InputDevice.Keyboard);
		await splitItem11GotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		var splitItem1GotFocusEvent = new Event();
		var splitItem1GotFocusRegistration = CreateSafeEventRegistration<SplitMenuFlyoutItem, RoutedEventHandler>(nameof(SplitMenuFlyoutItem.GotFocus));
		splitItem1GotFocusRegistration.Attach(splitItem1, (s, e) => { splitItem1GotFocusEvent.Set(); });

		// Close the split menu's sub menu
		await TestServices.KeyboardHelper.Escape();
		await TestServices.WindowHelper.WaitForIdle();
		await splitItem1GotFocusEvent.WaitForDefault();

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemInternalKeyboardTraversal()
	{
		Button button1 = null;
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitItem = null;
		Button primaryButton = null;
		Button secondaryButton = null;
		IList<MenuFlyoutItemBase> items = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		menuFlyout = await CreateSplitMenuFlyoutItemsFromXaml();
		VERIFY_IS_NOT_NULL(menuFlyout);

		await RunOnUIThread(() =>
		{
			button1 = (Button)(rootPanel.FindName("button1"));
			items = menuFlyout.Items;
			splitItem = (SplitMenuFlyoutItem)(items[6]);
			VERIFY_IS_NOT_NULL(splitItem);
		});

		await ShowMenuFlyout(menuFlyout, button1, -50, 50);

		// Navigate down to the split item with submenu (index 6)
		// Need to navigate through: Item(0), Item(1), Toggle(3), Split(5)-Primary, Split(5)-Secondary, Split(6)-Primary
		LOG_OUTPUT("Navigate to split menu item with submenu.");
		for (int i = 0; i < 5; i++)
		{
			await CommonInputHelper.Down(InputDevice.Keyboard);
			await TestServices.WindowHelper.WaitForIdle();
		}

		await RunOnUIThread(() =>
		{
			primaryButton = (Button)(TreeHelper.GetVisualChildByName(splitItem, "PrimaryButton"));
			secondaryButton = (Button)(TreeHelper.GetVisualChildByName(splitItem, "SecondaryButton"));
			VERIFY_IS_NOT_NULL(primaryButton);
			VERIFY_IS_NOT_NULL(secondaryButton);
		});

		// Test 1: Verify focus is on primary button
		LOG_OUTPUT("Test 1: Verify focus is on primary button.");
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(primaryButton.FocusState != FocusState.Unfocused);
		});

		// Test 2: Press Right to move focus from primary to secondary
		LOG_OUTPUT("Test 2: Press Right to move focus to secondary button.");
		var secondaryGotFocusEvent = new Event();
		var secondaryGotFocusRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>("GotFocus");
		secondaryGotFocusRegistration.Attach(secondaryButton, (s, e) => { secondaryGotFocusEvent.Set(); });

		await CommonInputHelper.Right(InputDevice.Keyboard);
		await secondaryGotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(secondaryButton.FocusState != FocusState.Unfocused);
			VERIFY_IS_TRUE(primaryButton.FocusState == FocusState.Unfocused);
		});

		// Test 3: Press Left to move focus from secondary back to primary
		LOG_OUTPUT("Test 3: Press Left to move focus back to primary button.");
		var primaryGotFocusEvent = new Event();
		var primaryGotFocusRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>("GotFocus");
		primaryGotFocusRegistration.Attach(primaryButton, (s, e) => { primaryGotFocusEvent.Set(); });

		await CommonInputHelper.Left(InputDevice.Keyboard);
		await primaryGotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(primaryButton.FocusState != FocusState.Unfocused);
			VERIFY_IS_TRUE(secondaryButton.FocusState == FocusState.Unfocused);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateFocusedItemDownAndUpAfterOverrideFocusItem()
	{
		MenuFlyout menuFlyout = null;
		MenuFlyoutItem secondMenuItem = null;
		MenuFlyoutItem thirdMenuItem = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			TestServices.WindowHelper.WindowContent = rootPanel;

			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' > " +
				"    <MenuFlyoutItem>MenuItem1</MenuFlyoutItem> " +
				"    <MenuFlyoutItem>MenuItem2</MenuFlyoutItem> " +
				"    <MenuFlyoutItem>MenuItem3</MenuFlyoutItem> " +
				"</MenuFlyout>"));
			menuFlyout.XamlRoot = rootPanel.XamlRoot;
		});
		await TestServices.WindowHelper.WaitForIdle();

		var openedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		openedRegistration.Attach(menuFlyout, (s, e) =>
		{
			var menuItems = menuFlyout.Items;

			var firstMenuItem = (MenuFlyoutItem)(menuItems[0]);
			secondMenuItem = (MenuFlyoutItem)(menuItems[1]);
			thirdMenuItem = (MenuFlyoutItem)(menuItems[2]);

			// Override the focus on the first menu item
			firstMenuItem.Focus(FocusState.Keyboard);

			openedEvent.Set();
		});

		await RunOnUIThread(() =>
		{
			menuFlyout.ShowAt(null, new Point(50, 50));
		});
		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		var gotFocusEvent = new Event();
		var gotFocusRegistration = CreateSafeEventRegistration<MenuFlyoutItem, RoutedEventHandler>(nameof(MenuFlyoutItem.GotFocus));
		gotFocusRegistration.Attach(secondMenuItem, (s, e) =>
		{
			VERIFY_IS_TRUE(secondMenuItem.Text == "MenuItem2");
			gotFocusEvent.Set();
		});

		await CommonInputHelper.Down(InputDevice.Keyboard);
		await gotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.HideFlyout(menuFlyout);

		// Navigate to the last menu item through Keyboard Up event
		openedEvent.Reset();
		await RunOnUIThread(() =>
		{
			menuFlyout.ShowAt(null, new Point(50, 50));
		});
		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		var gotFocusEvent2 = new Event();
		var gotFocusRegistration2 = CreateSafeEventRegistration<MenuFlyoutItem, RoutedEventHandler>(nameof(MenuFlyoutItem.GotFocus));
		gotFocusRegistration2.Attach(thirdMenuItem, (s, e) =>
		{
			VERIFY_IS_TRUE(thirdMenuItem.Text == "MenuItem3");
			gotFocusEvent2.Set();
		});

		await CommonInputHelper.Up(InputDevice.Keyboard);
		await gotFocusEvent2.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task ValidateSingleItemGetsInitialKeyboardFocus()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;
		MenuFlyoutItem menuFlyoutItem = null;

		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>(nameof(Button.Loaded));

		await RunOnUIThread(() =>
		{
			button = (Button)(XamlReader.Load(
				"<Button xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'> " +
				"    Click for flyout " +
				"    <Button.Flyout> " +
				"        <MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'> " +
				"            <MenuFlyoutItem>The menu item</MenuFlyoutItem> " +
				"        </MenuFlyout> " +
				"    </Button.Flyout> " +
				"</Button>"));

			loadedRegistration.Attach(button, (s, e) => { loadedEvent.Set(); });
			TestServices.WindowHelper.WindowContent = button;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		var openedEvent = new Event();
		var gotFocusEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var gotFocusRegistration = CreateSafeEventRegistration<MenuFlyoutItem, RoutedEventHandler>(nameof(MenuFlyoutItem.GotFocus));

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(button.Flyout);
			menuFlyoutItem = (MenuFlyoutItem)(menuFlyout.Items[0]);

			openedRegistration.Attach(menuFlyout, (s, e) => { openedEvent.Set(); });
			gotFocusRegistration.Attach(menuFlyoutItem, (s, e) => { gotFocusEvent.Set(); });
		});

		await ControlHelper.EnsureFocused(button);
		await CommonInputHelper.Accept(InputDevice.Keyboard);

		await openedEvent.WaitForDefault();
		await gotFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task ValidateMenuItemsShowIcons()
	{
		// The upstream master dumps the visual tree of the window content (popups are closed by then). Its content is
		// asserted explicitly below, after the flyout has been shown with its icons.
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

		Grid rootPanel = null;
		Button button = null;
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			rootPanel = (Grid)(XamlReader.Load(
				"<Grid x:Name='root' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'> " +
				"    <Button x:Name='button1' Content='Button 1'/> " +
				"</Grid>"));
			TestServices.WindowHelper.WindowContent = rootPanel;

			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'> " +
				"    <MenuFlyoutItem Icon='Accept' Text='menu item 1'/> " +
				"    <MenuFlyoutItem Text='menu item 2'/> " +
				"    <MenuFlyoutSeparator/> " +
				"    <ToggleMenuFlyoutItem Icon='Play' Text='toggle menu item 1'/> " +
				"    <ToggleMenuFlyoutItem IsChecked='True' Text='toggle menu item 2'/> " +
				"    <MenuFlyoutSeparator/> " +
				"    <SplitMenuFlyoutItem Icon='Home' Text='split menu item 1'> " +
				"        <MenuFlyoutItem Icon='Calendar' Text='split item 1.1'/> " +
				"        <MenuFlyoutItem Text='split item 1.2'/> " +
				"    </SplitMenuFlyoutItem> " +
				"    <MenuFlyoutSeparator/> " +
				"    <MenuFlyoutSubItem Icon='Like' Text='sub menu item 1'> " +
				"        <MenuFlyoutItem Icon='Setting' Text='menu item 1.1'/> " +
				"        <MenuFlyoutItem Text='menu item 2.2'/> " +
				"    </MenuFlyoutSubItem> " +
				"</MenuFlyout>"));

			button = (Button)(rootPanel.FindName("button1"));
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button, 50, 50);

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var items = menuFlyout.Items;
			VERIFY_IS_NOT_NULL(((MenuFlyoutItem)items[0]).Icon);
			VERIFY_IS_NULL(((MenuFlyoutItem)items[1]).Icon);
			VERIFY_IS_NOT_NULL(((ToggleMenuFlyoutItem)items[3]).Icon);
			VERIFY_IS_NULL(((ToggleMenuFlyoutItem)items[4]).Icon);
			VERIFY_IS_NOT_NULL(((SplitMenuFlyoutItem)items[6]).Icon);
			VERIFY_IS_NOT_NULL(((MenuFlyoutSubItem)items[8]).Icon);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);

		// Master: the window content once the flyout is closed.
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(rootPanel.Name, "root");
			VERIFY_ARE_VERY_CLOSE(rootPanel.ActualWidth, 400, 1);
			VERIFY_ARE_VERY_CLOSE(rootPanel.ActualHeight, 600, 1);

			VERIFY_ARE_EQUAL(button.Name, "button1");
			VERIFY_ARE_EQUAL((string)button.Content, "Button 1");
			VERIFY_ARE_VERY_CLOSE(button.ActualHeight, 32, 1);
			VERIFY_ARE_EQUAL(button.HorizontalAlignment, HorizontalAlignment.Left);
			VERIFY_ARE_EQUAL(button.VerticalAlignment, VerticalAlignment.Center);
		});
	}

	[TestMethod]
	public async Task FocusDoesNotJumpWhenUsingGamepadTriggersFollowedByDPad()
	{
		MenuFlyout menuFlyout = null;
		Button button = null;

		DependencyObject beforeTriggerFocusedObject = null;
		DependencyObject afterTriggerFocusedObject = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = new MenuFlyout();
#if HAS_UNO
			// Uno: a page has to stay under half of the items, as on upstream's small test monitor.
			global::Uno.UI.FeatureConfiguration.Popup.UseWindowedPopups = false;
#endif
			for (int i = 0; i < 50; ++i)
			{
				var item = new MenuFlyoutItem();
				item.Text = "Item";

				menuFlyout.Items.Add(item);
			}

			button = new Button();
			button.Content = "Button";

			TestServices.WindowHelper.WindowContent = button;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button, 0, 0, true /* forceTapAsPreviousInputMessage */);

		await RunOnUIThread(() =>
		{
			beforeTriggerFocusedObject = (DependencyObject)(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
			VERIFY_IS_NOT_NULL(beforeTriggerFocusedObject, "An item should get focus when the MenuFlyout opens.");
		});

		await TestServices.KeyboardHelper.PressKeySequence("$d$_GamepadRightTrigger#$u$_GamepadRightTrigger");
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			afterTriggerFocusedObject = (DependencyObject)(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
			VERIFY_IS_FALSE(beforeTriggerFocusedObject.Equals(afterTriggerFocusedObject), "The RightTrigger should have moved focus.");
		});

		await TestServices.KeyboardHelper.GamepadDpadDown();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var focusedElement = (DependencyObject)(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
			VERIFY_IS_FALSE(afterTriggerFocusedObject.Equals(focusedElement));

			var presenter = GetMenuFlyoutPresenter(menuFlyout);

			var expectedFocusItemIndex = presenter.IndexFromContainer(afterTriggerFocusedObject) + 1;
			VERIFY_IS_FALSE(expectedFocusItemIndex == -1, "The expected focused element should have an item index.");

			var focusedItemIndex = presenter.IndexFromContainer(focusedElement);
			VERIFY_IS_FALSE(focusedItemIndex == -1, "The new focused element should have an item index.");

			VERIFY_ARE_EQUAL(focusedItemIndex, expectedFocusItemIndex);

			beforeTriggerFocusedObject = focusedElement;
		});

		await TestServices.KeyboardHelper.PressKeySequence("$d$_GamepadLeftTrigger#$u$_GamepadLeftTrigger");
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			afterTriggerFocusedObject = (DependencyObject)(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
			VERIFY_IS_FALSE(beforeTriggerFocusedObject.Equals(afterTriggerFocusedObject), "The LeftTrigger should have moved focus.");
		});

		await TestServices.KeyboardHelper.GamepadDpadDown();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var focusedElement = (DependencyObject)(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
			VERIFY_IS_FALSE(afterTriggerFocusedObject.Equals(focusedElement));

			var presenter = GetMenuFlyoutPresenter(menuFlyout);

			var expectedFocusItemIndex = presenter.IndexFromContainer(afterTriggerFocusedObject) + 1;
			VERIFY_IS_FALSE(expectedFocusItemIndex == -1, "The expected focused element should have an item index.");

			var focusedItemIndex = presenter.IndexFromContainer(focusedElement);
			VERIFY_IS_FALSE(focusedItemIndex == -1, "The new focused element should have an item index.");

			VERIFY_ARE_EQUAL(focusedItemIndex, expectedFocusItemIndex);

			beforeTriggerFocusedObject = focusedElement;
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task CanDetectChangesToCanExecuteWithoutBeingInVisualTree()
	{
		MenuFlyout menuFlyout = null;
		MenuFlyoutItem menuFlyoutItem = null;
		Button button = null;

		MenuCommand command = new MenuCommand((object _) => { }, false /*canExecute*/, null);

		await RunOnUIThread(() =>
		{
			menuFlyoutItem = new MenuFlyoutItem();
			menuFlyoutItem.Text = "Item";
			menuFlyoutItem.Command = command;

			menuFlyout = new MenuFlyout();
			menuFlyout.Items.Add(menuFlyoutItem);

			button = new Button();
			button.Flyout = menuFlyout;
			button.Content = "Button";

			TestServices.WindowHelper.WindowContent = button;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button, 0, 0);

		VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(menuFlyoutItem, "CommonStates", "Disabled"), "Expected that the menu flyout item starts out as disabled.");

		await FlyoutHelper.HideFlyout(menuFlyout);

		LOG_OUTPUT("Change the MenuFlyoutItem's attached command's CanExecute property to true -> item should now show as enabled.");
		command.CanExecuteFlag = true;

		await ShowMenuFlyout(menuFlyout, button, 0, 0);

		VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(menuFlyoutItem, "CommonStates", "Normal"));

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task VerifyIsEnabledPropagatesTreeFromCommand()
	{
		// Regression coverage for:
		// MSFT:11947475 - High Contrast Desktop : Photos : Selected item in aspect ratio doesn't following High contrast standards after modifying the value
		// The issue is that MenuFlyoutItem.Command.CanExecute returns false, but this was not propagating down the visual tree to the TextBlock template part.
		// Note: IsEnabled is only publically visible on Control via the public api even though internally it is on all UIElements.
		// So, to test this scenario, we insert a dummy ContentControl into the MenuFlyoutItem's template so we have a way of reading the IsEnabled property
		// on the descendant elements of the MenuFlyoutItem.

		MenuFlyout menuFlyout = null;
		MenuFlyoutItem menuFlyoutItem = null;
		SplitMenuFlyoutItem splitMenuFlyoutItem = null;

		MenuCommand command = new MenuCommand((p) => { }, false /*canExecute*/, null);
		MenuCommand splitCommand = new MenuCommand((p) => { }, false /*canExecute*/, null);

		await RunOnUIThread(() =>
		{
			var rootGrid = (Grid)(XamlReader.Load(
				"""
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						Background="SlateBlue" >
					<Grid.Resources>
						<Style x:Key="MenuFlyoutItemStyle" TargetType="MenuFlyoutItem" >
							<Setter Property="Template" >
								<Setter.Value>
									<ControlTemplate TargetType="MenuFlyoutItem">
										<Grid x:Name="LayoutRoot">
											<ContentControl x:Name="testContentControl">
												<TextBlock x:Name="TextBlock" />
											</ContentControl>
										</Grid>
									</ControlTemplate>
								</Setter.Value>
							</Setter>
						</Style>
						<Style x:Key="SplitMenuFlyoutItemStyle" TargetType="SplitMenuFlyoutItem">
							<Setter Property="Template">
								<Setter.Value>
									<ControlTemplate TargetType="SplitMenuFlyoutItem">
										<Grid x:Name="LayoutRoot">
											<ContentControl x:Name="testSplitContentControl">
												<TextBlock x:Name="TextBlock" />
											</ContentControl>
										</Grid>
									</ControlTemplate>
								</Setter.Value>
							</Setter>
						</Style>
					</Grid.Resources>
					<FlyoutBase.AttachedFlyout>
						<MenuFlyout x:Name="menuFlyout">
							<MenuFlyoutItem x:Name="menuFlyoutItem" Style="{StaticResource MenuFlyoutItemStyle}">Some Text</MenuFlyoutItem>
							<SplitMenuFlyoutItem x:Name="splitMenuFlyoutItem" Style="{StaticResource SplitMenuFlyoutItemStyle}" Text="Split Item" />
						</MenuFlyout>
					</FlyoutBase.AttachedFlyout>
				</Grid>
				"""
				));
			menuFlyout = FlyoutBase.GetAttachedFlyout(rootGrid) as MenuFlyout;
			menuFlyoutItem = menuFlyout.Items[0] as MenuFlyoutItem;
			splitMenuFlyoutItem = menuFlyout.Items[1] as SplitMenuFlyoutItem;

			menuFlyoutItem.Command = command;
			splitMenuFlyoutItem.Command = splitCommand;

			TestServices.WindowHelper.WindowContent = rootGrid;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, null, 0, 0);

		await RunOnUIThread(() =>
		{
			var testContentControl = (ContentControl)(TreeHelper.GetVisualChildByName(menuFlyoutItem, "testContentControl"));
			VERIFY_IS_FALSE(testContentControl.IsEnabled);

			var testSplitContentControl = (ContentControl)(TreeHelper.GetVisualChildByName(splitMenuFlyoutItem, "testSplitContentControl"));
			VERIFY_IS_FALSE(testSplitContentControl.IsEnabled);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	// Shows the same MenuFlyout twice in a row, without closing it, attempting to position it beyond the screen's boundaries.
	// Ensures it is moved within the screen's boundaries.
	[TestMethod]
	public async Task MenuFlyoutRemainsInBoundsWhenShownTwice()
	{
		Grid rootGrid = null;
		MenuFlyout menuFlyout = null;

		Point initialMenuItemPosition = default;
		Point showAtPosition = default;

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
				"      <MenuFlyoutItem>Some Text</MenuFlyoutItem> " +
				"    </MenuFlyout> " +
				"  </FlyoutBase.AttachedFlyout> " +
				"</Grid>"));
			VERIFY_IS_NOT_NULL(rootGrid);

			loadedRegistration.Attach(rootGrid, (s, e) => { loadedEvent.Set(); });

			TestServices.WindowHelper.WindowContent = rootGrid;

			menuFlyout = (MenuFlyout)(FlyoutBase.GetAttachedFlyout(rootGrid));
			VERIFY_IS_NOT_NULL(menuFlyout);
#if HAS_UNO
			// Uno: this asserts against the window, which upstream's harness has fill the screen. The property alone won't do: ShowAt with options makes a MenuFlyout windowed regardless.
			global::Uno.UI.FeatureConfiguration.Popup.UseWindowedPopups = false;
#endif

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				LOG_OUTPUT("MenuFlyout.Opened event raised");

				var menuItem = (MenuFlyoutItem)(menuFlyout.Items[0]);
				VERIFY_IS_NOT_NULL(menuItem);

				initialMenuItemPosition = menuItem.TransformToVisual(rootGrid).TransformPoint(new Point(0, 0));
				LOG_OUTPUT($"Initial MenuFlyoutItem position in MenuFlyout.Opened event handler: {initialMenuItemPosition.X}, {initialMenuItemPosition.Y}.");

				menuFlyoutOpenedEvent.Set();
			});

			closedRegistration.Attach(menuFlyout, (s, e) =>
			{
				LOG_OUTPUT("MenuFlyout.Closed event raised");
				menuFlyoutClosedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Waiting for root Grid to load.");
		await loadedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			showAtPosition.X = (float)(rootGrid.ActualWidth) - 25.0f;
			showAtPosition.Y = (float)(rootGrid.ActualHeight) - 25.0f;

			LOG_OUTPUT($"Showing MenuFlyout at position {showAtPosition.X}, {showAtPosition.Y}.");
			menuFlyout.ShowAt(rootGrid, showAtPosition);
		});

		LOG_OUTPUT("Waiting for MenuFlyout.Opened event.");
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

#if HAS_UNO
		await RunOnUIThread(() =>
		{
			// TODO Uno: Opened is raised from the presenter's Loaded, which comes before its items are realized and placed.
			initialMenuItemPosition = ((MenuFlyoutItem)menuFlyout.Items[0]).TransformToVisual(rootGrid).TransformPoint(new Point(0, 0));
		});
#endif

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT($"Showing MenuFlyout at position {showAtPosition.X}, {showAtPosition.Y} again.");
			menuFlyout.ShowAt(rootGrid, showAtPosition);
		});

		LOG_OUTPUT("Waiting for a few ticks to make sure the MenuFlyout has a chance to reposition itself.");
		await TestServices.WindowHelper.SynchronouslyTickUIThread(2);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var menuItem = (MenuFlyoutItem)(menuFlyout.Items[0]);
			VERIFY_IS_NOT_NULL(menuItem);

			Point finalMenuItemPosition = menuItem.TransformToVisual(rootGrid).TransformPoint(new Point(0, 0));
			LOG_OUTPUT($"Final MenuFlyoutItem position: {finalMenuItemPosition.X}, {finalMenuItemPosition.Y}.");

			LOG_OUTPUT("Verifying initial and final MenuFlyoutItem positions are identical.");
			VERIFY_ARE_EQUAL(finalMenuItemPosition.X, initialMenuItemPosition.X);
			VERIFY_ARE_EQUAL(finalMenuItemPosition.Y, initialMenuItemPosition.Y);

			LOG_OUTPUT("Verifying MenuFlyoutItem position was shifted to be completely visible.");
			VERIFY_IS_LESS_THAN(finalMenuItemPosition.X, (float)(rootGrid.ActualWidth - menuItem.ActualWidth));
			VERIFY_IS_LESS_THAN(finalMenuItemPosition.Y, (float)(rootGrid.ActualHeight - menuItem.ActualHeight));

			LOG_OUTPUT("Closing the MenuFlyout.");
			menuFlyout.Hide();
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Waiting for MenuFlyout.Closed event.");
		await menuFlyoutClosedEvent.WaitForDefault();
	}

	[TestMethod]
	public async Task ValidateSettingKeyboardAcceleratorCreatesDefaultItemKeyboardAcceleratorText()
	{
		await RunOnUIThread(() =>
		{
			var item = new MenuFlyoutItem();

			var keyboardAccelerator = new KeyboardAccelerator();
			keyboardAccelerator.Key = Windows.System.VirtualKey.A;
			keyboardAccelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
			item.KeyboardAccelerators.Add(keyboardAccelerator);

			string expectedKeyboardAcceleratorText = "Ctrl+A";

			LOG_OUTPUT("Expected keyboard accelerator text: \"%s\"", expectedKeyboardAcceleratorText);
			LOG_OUTPUT("Actual keyboard accelerator text:   \"%s\"", item.KeyboardAcceleratorTextOverride);
			VERIFY_IS_TRUE(string.CompareOrdinal(expectedKeyboardAcceleratorText, item.KeyboardAcceleratorTextOverride) == 0);
		});
	}

	[TestMethod]
	public async Task ValidateSettingKeyboardAcceleratorDoesNotOverrideItemCustomKeyboardAcceleratorText()
	{
		await RunOnUIThread(() =>

		{
			var item = new MenuFlyoutItem();

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
	public async Task ValidateSettingKeyboardAcceleratorCreatesDefaultToggleItemKeyboardAcceleratorText()
	{
		await RunOnUIThread(() =>

		{
			var item = new ToggleMenuFlyoutItem();

			var keyboardAccelerator = new KeyboardAccelerator();
			keyboardAccelerator.Key = Windows.System.VirtualKey.A;
			keyboardAccelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
			item.KeyboardAccelerators.Add(keyboardAccelerator);

			string expectedKeyboardAcceleratorText = "Ctrl+A";

			LOG_OUTPUT("Expected keyboard accelerator text: \"%s\"", expectedKeyboardAcceleratorText);
			LOG_OUTPUT("Actual keyboard accelerator text:   \"%s\"", item.KeyboardAcceleratorTextOverride);
			VERIFY_IS_TRUE(string.CompareOrdinal(expectedKeyboardAcceleratorText, item.KeyboardAcceleratorTextOverride) == 0);
		});
	}

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
	public async Task ValidateSettingKeyboardAcceleratorCreatesDefaultSplitItemKeyboardAcceleratorText()
	{
		await RunOnUIThread(() =>
		{
			var item = new SplitMenuFlyoutItem();

			var keyboardAccelerator = new KeyboardAccelerator();
			keyboardAccelerator.Key = Windows.System.VirtualKey.A;
			keyboardAccelerator.Modifiers = Windows.System.VirtualKeyModifiers.Control;
			item.KeyboardAccelerators.Add(keyboardAccelerator);

			string expectedKeyboardAcceleratorText = "Ctrl+A";

			LOG_OUTPUT("Expected keyboard accelerator text: \"%s\"", expectedKeyboardAcceleratorText);
			LOG_OUTPUT("Actual keyboard accelerator text:   \"%s\"", item.KeyboardAcceleratorTextOverride);
			VERIFY_IS_TRUE(string.CompareOrdinal(expectedKeyboardAcceleratorText, item.KeyboardAcceleratorTextOverride) == 0);
		});
	}

	[TestMethod]
	public async Task ValidateSettingKeyboardAcceleratorDoesNotOverrideSplitItemCustomKeyboardAcceleratorText()
	{
		await RunOnUIThread(() =>
		{
			var item = new SplitMenuFlyoutItem();

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
	public async Task ValidateKeyboardAcceleratorTextHiddenInSplitMenuItem()
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
				"      <SplitMenuFlyoutItem Icon='AddFriend' Text='Split Item 1'> " +
				"          <SplitMenuFlyoutItem.KeyboardAccelerators> " +
				"              <KeyboardAccelerator Key='F10' Modifiers='Control' /> " +
				"          </SplitMenuFlyoutItem.KeyboardAccelerators> " +
				"          <MenuFlyoutItem Text='Sub Item 1.1'/> " +
				"      </SplitMenuFlyoutItem> " +
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
			var keyboardAcceleratorTextBlock = (TextBlock)(TreeHelper.GetVisualChildByName(menuFlyout.Items[0], "KeyboardAcceleratorTextBlock"));
			VERIFY_IS_NULL(keyboardAcceleratorTextBlock);

			menuFlyout.Hide();
		});

		await menuFlyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
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

	[TestMethod]
	public async Task ValidateSettingUICommandSetsProperties()
	{
		await CommandHelper.ValidateSettingUICommandSetsProperties<MenuFlyoutItem>(
			MenuFlyoutItem.CommandProperty,
			MenuFlyoutItem.TextProperty,
			MenuFlyoutItem.IconProperty);
	}

	[TestMethod]
	public async Task ValidateSettingUICommandDoesNotOverwriteProperties()
	{
		await CommandHelper.ValidateSettingUICommandDoesNotOverwriteProperties<MenuFlyoutItem>(
			MenuFlyoutItem.CommandProperty,
			MenuFlyoutItem.TextProperty,
			MenuFlyoutItem.IconProperty);
	}

	[TestMethod]
	public async Task ValidateSettingUICommandSetsToggleProperties()
	{
		await CommandHelper.ValidateSettingUICommandSetsProperties<ToggleMenuFlyoutItem>(
			ToggleMenuFlyoutItem.CommandProperty,
			ToggleMenuFlyoutItem.TextProperty,
			ToggleMenuFlyoutItem.IconProperty);
	}

	[TestMethod]
	public async Task ValidateSettingUICommandDoesNotOverwriteToggleProperties()
	{
		await CommandHelper.ValidateSettingUICommandDoesNotOverwriteProperties<ToggleMenuFlyoutItem>(
			ToggleMenuFlyoutItem.CommandProperty,
			ToggleMenuFlyoutItem.TextProperty,
			ToggleMenuFlyoutItem.IconProperty);
	}

	[TestMethod]
	public async Task ValidateSettingUICommandSetsSplitProperties()
	{
		await CommandHelper.ValidateSettingUICommandSetsProperties<SplitMenuFlyoutItem>(
			SplitMenuFlyoutItem.CommandProperty,
			SplitMenuFlyoutItem.TextProperty,
			SplitMenuFlyoutItem.IconProperty);
	}

	[TestMethod]
	public async Task ValidateSettingUICommandDoesNotOverwriteSplitProperties()
	{
		await CommandHelper.ValidateSettingUICommandDoesNotOverwriteProperties<SplitMenuFlyoutItem>(
			SplitMenuFlyoutItem.CommandProperty,
			SplitMenuFlyoutItem.TextProperty,
			SplitMenuFlyoutItem.IconProperty);
	}

	[TestMethod]
	[RequiresScaling(1f)]
	public async Task ValidateMenuFlyoutTouchPositionForExclusionRect()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

		Grid root = null;
		Button buttonTop = null;
		Button buttonBottom = null;

		await RunOnUIThread(() =>
		{
			root = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"          x:Name='root'> " +
				"    <Button x:Name='buttonTop' Content='button top' VerticalAlignment='Top'/> " +
				"    <Button x:Name='buttonBottom' Content='button bottom'  VerticalAlignment='Bottom'/> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = root;

			buttonTop = (Button)(root.FindName("buttonTop"));
			buttonBottom = (Button)(root.FindName("buttonBottom"));
		});
		await TestServices.WindowHelper.WaitForIdle();

		XamlRoot xamlRoot = null;
		await RunOnUIThread(() =>
		{
			xamlRoot = root.XamlRoot;
		});

		LOG_OUTPUT("Setting last input method to Touch");
		await InjectInput(InputMethod.Touch);

		MenuFlyout menuFlyout = null;
		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutItem>Menu item 1</MenuFlyoutItem>" +
				"    <MenuFlyoutItem>Menu item 2</MenuFlyoutItem>" +
				"</MenuFlyout>"));
		});

#if HAS_UNO
		// Uno: upstream's test window starts at the top of its monitor, and a windowed menu goes above the button when the monitor has room.
		global::Uno.UI.FeatureConfiguration.Popup.UseWindowedPopups = false;
#endif

		FlyoutShowOptions showOptions = null;

		await RunOnUIThread(() =>
		{
			showOptions = new FlyoutShowOptions();
			showOptions.Position = new Point(0, (float)buttonBottom.ActualHeight);
			showOptions.ExclusionRect = new Rect(0, 0, (float)buttonBottom.ActualWidth, (float)buttonBottom.ActualHeight);

			LOG_OUTPUT("Showing flyout on bottom button");
			menuFlyout.ShowAt(buttonBottom, showOptions);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			// Verify MenuFlyout is above the bottom button.
			var menuFlyoutPresenter = await GetCurrentPresenter();
			var menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)menuFlyoutPresenter);
			var buttonBounds = await ControlHelper.GetBounds((FrameworkElement)buttonBottom);

			VERIFY_ARE_EQUAL(menuFlyoutBounds.Bottom, buttonBounds.Top);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);

		await RunOnUIThread(() =>
		{
			showOptions = new FlyoutShowOptions();
			showOptions.Position = new Point(0, (float)buttonTop.ActualHeight);
			showOptions.ExclusionRect = new Rect(0, 0, (float)buttonTop.ActualWidth, (float)buttonTop.ActualHeight);

			LOG_OUTPUT("Showing flyout on top button");
			menuFlyout.ShowAt(buttonTop, showOptions);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			// Verify MenuFlyout is below the top button.
			var menuFlyoutPresenter = await GetCurrentPresenter();
			var menuFlyoutBounds = await ControlHelper.GetBounds((FrameworkElement)menuFlyoutPresenter);
			var buttonBounds = await ControlHelper.GetBounds((FrameworkElement)buttonTop);

			VERIFY_ARE_EQUAL(menuFlyoutBounds.Top, buttonBounds.Bottom);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task ValidateShowAtMonitorEdge()
	{
		await ValidateShowAtMonitorEdge(1.0f /*scaleFactor*/);
		await ValidateShowAtMonitorEdge(1.25f /*scaleFactor*/);
		await ValidateShowAtMonitorEdge(1.5f /*scaleFactor*/);
		await ValidateShowAtMonitorEdge(1.75f /*scaleFactor*/);
		await ValidateShowAtMonitorEdge(2.25f /*scaleFactor*/);
	}

	private async Task ValidateShowAtMonitorEdge(float scaleFactor)
	{
		LOG_OUTPUT($"Setting Window Size to 421x421 and global Scale to {scaleFactor}.");
		Size size = new Size(421, 421);
		TestServices.WindowHelper.SetWindowSizeOverride(size);
#if HAS_UNO
		TestServices.WindowHelper.SetTestScaling(scaleFactor);
#endif

		Rect windowBounds = default;
		Rect monitorBounds = default;

		await RunOnUIThread(() =>
		{
			windowBounds = TestServices.WindowHelper.WindowBounds;
			LOG_OUTPUT($"WindowBounds: {windowBounds.X},{windowBounds.Y},{windowBounds.Width},{windowBounds.Height}.");

			monitorBounds = TestServices.WindowHelper.MonitorBounds;
			LOG_OUTPUT($"MonitorBounds: {monitorBounds.X},{monitorBounds.Y},{monitorBounds.Width},{monitorBounds.Height}.");
		});

		var menuFlyout = await CreateMenuFlyout(FlyoutPlacementMode.Bottom);
		VERIFY_IS_NOT_NULL(menuFlyout);

		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>(nameof(Grid.Loaded));
		var flyoutOpenedEvent = new Event();
		uint flyoutOpenedCount = 0;
		var flyoutOpenedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var flyoutClosedEvent = new Event();
		uint flyoutClosedCount = 0;
		var flyoutClosedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		try
		{
			await RunOnUIThread(() =>
			{
				flyoutOpenedRegistration.Attach(menuFlyout, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyout.Opened event raised.");
					flyoutOpenedEvent.Set();
					flyoutOpenedCount++;
				});

				flyoutClosedRegistration.Attach(menuFlyout, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyout.Closed event raised.");
					flyoutClosedEvent.Set();
					flyoutClosedCount++;
				});

				var rootPanel = new Grid();

				loadedRegistration.Attach(rootPanel, (s, e) =>
				{
					LOG_OUTPUT("Grid.Loaded event raised.");
					loadedEvent.Set();
				});

				LOG_OUTPUT("Setting WindowContent.");
				TestServices.WindowHelper.WindowContent = rootPanel;
			});

			LOG_OUTPUT("Waiting for Loaded event...");
			await loadedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// TODO Uno: WindowHelper.GetAvailableMonitorBounds is not available; it only logged the monitor geometry upstream.
				LOG_OUTPUT($"Showing MenuFlyout at position {monitorBounds.Width / scaleFactor},{(monitorBounds.Height - 32.0f) / scaleFactor} as a windowed popup.");
				Point showAtPosition = new Point(monitorBounds.Width / scaleFactor, (monitorBounds.Height - 32.0f) / scaleFactor);

				// Upstream has one root to go by, Uno has as many as it has windows.
				menuFlyout.XamlRoot = TestServices.WindowHelper.WindowContent.XamlRoot;
				menuFlyout.ShowAt(null, showAtPosition);
			});

			LOG_OUTPUT("Waiting for Opened event...");
			await flyoutOpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Hiding MenuFlyout.");
			await FlyoutHelper.HideFlyout(menuFlyout);
		}
		finally
		{
#if HAS_UNO
			TestServices.WindowHelper.UnsetTestScaling();
#endif
		}
	}

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

		await FlyoutHelper.OpenFlyout(menuFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		   {
			   var presenter = GetMenuFlyoutPresenter(menuFlyout);
			   VERIFY_IS_NOT_NULL(presenter);
			   VERIFY_ARE_EQUAL(true, presenter.IsDefaultShadowEnabled);
		   });

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaIOS)] // MenuFlyout does not honor ScreenHelper.OverrideVisibleBounds on iOS Skia https://github.com/unoplatform/uno/issues/23031
	public async Task VerifyLargeNonWindowedMenuIsPositionedCorrectly()
	{
		using var visibleBoundsOverride = ScreenHelper.OverrideVisibleBounds(
			new Thickness(0),
			skipIfHasNativeUnsafeArea: false);

		MenuFlyout flyout = null;
		MenuFlyoutSubItem subItem = null;
		SplitMenuFlyoutItem splitItem = null;
		Button flyoutTarget = null;

		var openedEvent = new Event();
		var closedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		await RunOnUIThread(() =>
		{
			flyout = new MenuFlyout();
			flyout.Placement = FlyoutPlacementMode.Right;
			global::Uno.UI.FeatureConfiguration.Popup.UseWindowedPopups = false;

			for (int i = 0; i < 10; i++)
			{
				var item = new MenuFlyoutItem();
				item.Text = "Item";
				flyout.Items.Add(item);
			}

			subItem = new MenuFlyoutSubItem();
			subItem.Text = "Sub item";
			flyout.Items.Add(subItem);

			splitItem = new SplitMenuFlyoutItem();
			splitItem.Text = "Split item";
			flyout.Items.Add(splitItem);

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

			for (int i = 0; i < 30; i++)
			{
				var item = new MenuFlyoutItem();
				item.Text = "Item";
				splitItem.Items.Add(item);
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
			LOG_OUTPUT($"XAML root size:             width={xamlRootSize.Width:F0} height={xamlRootSize.Height:F0}");
			LOG_OUTPUT($"MenuFlyout bounds:          left={flyoutBounds.Left:F0} top={flyoutBounds.Top:F0} width={flyoutBounds.Width:F0} height={flyoutBounds.Height:F0}");
			LOG_OUTPUT($"MenuFlyout sub-menu bounds: left={flyoutSubMenuBounds.Left:F0} top={flyoutSubMenuBounds.Top:F0} width={flyoutSubMenuBounds.Width:F0} height={flyoutSubMenuBounds.Height:F0}");

			VERIFY_ARE_EQUAL(0.0, flyoutBounds.Top);
			// TODO Uno: Upstream runs at 100%. At other scales, these differ by the rounding of a physical pixel.
			// Uno: at a fractional display scale, the menu's height rounds to whole pixels and the root's doesn't.
			VERIFY_ARE_VERY_CLOSE(xamlRootSize.Height, flyoutBounds.Height, 1);
			VERIFY_ARE_EQUAL(0.0, flyoutSubMenuBounds.Top);
			VERIFY_ARE_VERY_CLOSE(xamlRootSize.Height, flyoutSubMenuBounds.Height, 1);
		});

		LOG_OUTPUT("Tapping on the split item secondary part to show the split item sub-menu.");
		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot);

			LOG_OUTPUT("There should be two popups: one for the MenuFlyout, and one for the split item's sub-menu.");
			VERIFY_ARE_EQUAL(2, popups.Count);

			var flyoutPopup = popups[1];
			var flyoutPresenter = (FrameworkElement)(flyoutPopup.Child);
			var flyoutBounds = await ControlHelper.GetBounds(flyoutPresenter);

			var splitItemSubMenuPopup = popups[0];
			var splitItemSubMenuPresenter = (FrameworkElement)(splitItemSubMenuPopup.Child);
			var splitItemSubMenuBounds = await ControlHelper.GetBounds(splitItemSubMenuPresenter);

			var xamlRootSize = TestServices.WindowHelper.WindowContent.XamlRoot.Size;
			LOG_OUTPUT($"XAML root size:                    width={xamlRootSize.Width:F0} height={xamlRootSize.Height:F0}");
			LOG_OUTPUT($"MenuFlyout bounds:                 left={flyoutBounds.Left:F0} top={flyoutBounds.Top:F0} width={flyoutBounds.Width:F0} height={flyoutBounds.Height:F0}");
			LOG_OUTPUT($"Split item sub-menu bounds:        left={splitItemSubMenuBounds.Left:F0} top={splitItemSubMenuBounds.Top:F0} width={splitItemSubMenuBounds.Width:F0} height={splitItemSubMenuBounds.Height:F0}");

			VERIFY_ARE_EQUAL(0.0, flyoutBounds.Top);
			VERIFY_ARE_EQUAL(xamlRootSize.Height, flyoutBounds.Height);
			VERIFY_ARE_EQUAL(0.0, splitItemSubMenuBounds.Top);
			VERIFY_ARE_EQUAL(xamlRootSize.Height, splitItemSubMenuBounds.Height);
		});

		LOG_OUTPUT("Tapping on the button again to hide the MenuFlyout.");
		TestServices.InputHelper.Tap(flyoutTarget);

		await closedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task VerifyMenuFlyoutResizesWhenItemsChangeWhileOpen()
	{
		MenuFlyout flyout = null;
		MenuFlyoutSubItem subItem = null;
		SplitMenuFlyoutItem splitItem = null;
		Button flyoutTarget = null;

		var openedEvent = new Event();
		var closedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Closed));

		await RunOnUIThread(() =>
		{
			flyout = new MenuFlyout();
			flyout.Placement = FlyoutPlacementMode.Right;

			var item = new MenuFlyoutItem();
			item.Text = "Item 1";
			flyout.Items.Add(item);

			subItem = new MenuFlyoutSubItem();
			subItem.Text = "Sub item 2";
			flyout.Items.Add(subItem);

			item = new MenuFlyoutItem();
			item.Text = "Item 2.1";
			subItem.Items.Add(item);

			splitItem = new SplitMenuFlyoutItem();
			splitItem.Text = "Split item 3";
			flyout.Items.Add(splitItem);

			item = new MenuFlyoutItem();
			item.Text = "Item 3.1";
			splitItem.Items.Add(item);

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

		LOG_OUTPUT("Click on the button to show the MenuFlyout.");
		TestServices.InputHelper.LeftMouseClick(flyoutTarget);

		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		FrameworkElement presenter = null;
		Rect originalBounds = default;

		await RunOnUIThread(async () =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot);

			LOG_OUTPUT("There should be one popup for the MenuFlyout.");
			VERIFY_ARE_EQUAL(1, popups.Count);

			var flyoutPopup = popups[0];
			presenter = (FrameworkElement)(flyoutPopup.Child);
			originalBounds = await ControlHelper.GetBounds(presenter);

			LOG_OUTPUT("Adding another MenuFlyoutItem to the MenuFlyout.");

			var item = new MenuFlyoutItem();
			item.Text = "Item 3";
			flyout.Items.Add(item);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			var newBounds = await ControlHelper.GetBounds(presenter);

			VERIFY_IS_GREATER_THAN(newBounds.Height, originalBounds.Height);
		});

		LOG_OUTPUT("Move to the sub item to show the sub-menu.");
		await OpenSubItemWithMouse(subItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot);

			LOG_OUTPUT("There should be two popups: one for the MenuFlyout, and one for its sub-menu.");
			VERIFY_ARE_EQUAL(2, popups.Count);

			var flyoutSubMenuPopup = popups[0];
			presenter = (FrameworkElement)(flyoutSubMenuPopup.Child);
			originalBounds = await ControlHelper.GetBounds(presenter);

			LOG_OUTPUT("Adding another MenuFlyoutItem to the MenuFlyoutSubItem.");

			var item = new MenuFlyoutItem();
			item.Text = "Item 2.2";
			subItem.Items.Add(item);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			var newBounds = await ControlHelper.GetBounds(presenter);

			VERIFY_IS_GREATER_THAN(newBounds.Height, originalBounds.Height);
		});

		LOG_OUTPUT("Close the flyout after testing sub menu item.");
		await FlyoutHelper.HideFlyout(flyout);
		await closedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Reopen the flyout to test split menu item.");
		closedEvent.Reset();
		openedEvent.Reset();
		TestServices.InputHelper.LeftMouseClick(flyoutTarget);
		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Tap the split item secondary part to show the split item sub-menu.");
		await TapSplitMenuItemSecondary(splitItem);

		await RunOnUIThread(async () =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot);

			LOG_OUTPUT("There should be two popups: one for the MenuFlyout, and one for the split item's sub-menu.");
			VERIFY_ARE_EQUAL(2, popups.Count);

			var flyoutSplitItemSubMenuPopup = popups[0];
			presenter = (FrameworkElement)(flyoutSplitItemSubMenuPopup.Child);
			originalBounds = await ControlHelper.GetBounds(presenter);

			LOG_OUTPUT("Adding another MenuFlyoutItem to the SplitMenuFlyoutItem.");

			var item = new MenuFlyoutItem();
			item.Text = "Item 3.2";
			splitItem.Items.Add(item);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(async () =>
		{
			var newBounds = await ControlHelper.GetBounds(presenter);

			VERIFY_IS_GREATER_THAN(newBounds.Height, originalBounds.Height);
		});

		LOG_OUTPUT("Click on the button again to hide the MenuFlyout.");
		TestServices.InputHelper.LeftMouseClick(flyoutTarget);

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

	[TestMethod]
	public async Task ValidateSplitMenuItemSubMenuPresenterStyleFromXaml()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitItem = null;

		var menuFlyoutOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' >" +
				"  <Button x:Name='button' Content='Test Button' VerticalAlignment='Center' HorizontalAlignment='Center' >" +
				"    <Button.Flyout> " +
				"      <MenuFlyout> " +
				"        <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split Item with Custom Style'>" +
				"          <SplitMenuFlyoutItem.SubMenuPresenterStyle>" +
				"            <Style TargetType='MenuFlyoutPresenter'>" +
				"              <Setter Property='Tag' Value='CustomSubMenuStyle' />" +
				"              <Setter Property='Background' Value='LightBlue' />" +
				"            </Style>" +
				"          </SplitMenuFlyoutItem.SubMenuPresenterStyle>" +
				"          <MenuFlyoutItem Text='Sub Item 1' />" +
				"          <MenuFlyoutItem Text='Sub Item 2' />" +
				"          <MenuFlyoutSeparator />" +
				"          <ToggleMenuFlyoutItem Text='Sub Toggle Item' IsChecked='True' />" +
				"        </SplitMenuFlyoutItem>" +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);
			splitItem = (SplitMenuFlyoutItem)(rootPanel.FindName("splitItem1"));

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open the main MenuFlyout");
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Tap the secondary button of SplitMenuFlyoutItem to open submenu");
		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Verify that the custom SubMenuPresenterStyle is applied");
		var subPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_NOT_NULL(subPresenter);

			// Verify the custom Tag value from the style
			var tag = subPresenter.GetValue(MenuFlyoutPresenter.TagProperty);
			VERIFY_IS_NOT_NULL(tag);
			VERIFY_ARE_EQUAL("CustomSubMenuStyle", tag.ToString());

			// Verify the custom Background color
			var background = subPresenter.Background;
			VERIFY_IS_NOT_NULL(background);
			var solidColorBrush = background as SolidColorBrush;
			VERIFY_IS_NOT_NULL(solidColorBrush);

			var lightBlue = global::Microsoft.UI.Colors.LightBlue;
			VERIFY_ARE_EQUAL(lightBlue.A, solidColorBrush.Color.A);
			VERIFY_ARE_EQUAL(lightBlue.R, solidColorBrush.Color.R);
			VERIFY_ARE_EQUAL(lightBlue.G, solidColorBrush.Color.G);
			VERIFY_ARE_EQUAL(lightBlue.B, solidColorBrush.Color.B);
		});

		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});
	}

	[TestMethod]
	public async Task ValidateCanChangeSplitMenuItemSubMenuPresenterStyleAtRuntime()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitItem = null;

		var menuFlyoutOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' >" +
				"  <Button x:Name='button' Content='Test Button' VerticalAlignment='Center' HorizontalAlignment='Center' >" +
				"    <Button.Flyout> " +
				"      <MenuFlyout> " +
				"        <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split Item Runtime Style'>" +
				"          <SplitMenuFlyoutItem.SubMenuPresenterStyle>" +
				"            <Style TargetType='MenuFlyoutPresenter'>" +
				"              <Setter Property='Tag' Value='InitialSubMenuStyle' />" +
				"              <Setter Property='Background' Value='LightGreen' />" +
				"            </Style>" +
				"          </SplitMenuFlyoutItem.SubMenuPresenterStyle>" +
				"          <MenuFlyoutItem Text='Sub Item 1' />" +
				"          <MenuFlyoutItem Text='Sub Item 2' />" +
				"          <MenuFlyoutSeparator />" +
				"          <ToggleMenuFlyoutItem Text='Sub Toggle Item' IsChecked='True' />" +
				"        </SplitMenuFlyoutItem>" +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);
			splitItem = (SplitMenuFlyoutItem)(rootPanel.FindName("splitItem1"));

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Step 1: Test initial style from XAML");
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		var subPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_NOT_NULL(subPresenter);

			// Verify initial style
			var tag = subPresenter.GetValue(MenuFlyoutPresenter.TagProperty);
			VERIFY_IS_NOT_NULL(tag);
			VERIFY_ARE_EQUAL("InitialSubMenuStyle", tag.ToString());
		});

		// Close the menu
		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Step 2: Change SubMenuPresenterStyle at runtime");
		await RunOnUIThread(() =>
		{
			var newStyle = new Style(typeof(MenuFlyoutPresenter));
			newStyle.Setters.Add(new Setter(MenuFlyoutPresenter.TagProperty, "RuntimeModifiedStyle"));
			newStyle.Setters.Add(new Setter(MenuFlyoutPresenter.BackgroundProperty, new SolidColorBrush(global::Microsoft.UI.Colors.LightCoral)));

			splitItem.SubMenuPresenterStyle = newStyle;
		});

		// Reopen and test the modified style
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		subPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_NOT_NULL(subPresenter);

			// Verify the modified style
			var tag = subPresenter.GetValue(MenuFlyoutPresenter.TagProperty);
			VERIFY_IS_NOT_NULL(tag);
			VERIFY_ARE_EQUAL("RuntimeModifiedStyle", tag.ToString());

			var background = subPresenter.Background;
			VERIFY_IS_NOT_NULL(background);
			var solidColorBrush = background as SolidColorBrush;
			VERIFY_IS_NOT_NULL(solidColorBrush);

			var lightCoral = global::Microsoft.UI.Colors.LightCoral;
			VERIFY_ARE_EQUAL(lightCoral.A, solidColorBrush.Color.A);
			VERIFY_ARE_EQUAL(lightCoral.R, solidColorBrush.Color.R);
			VERIFY_ARE_EQUAL(lightCoral.G, solidColorBrush.Color.G);
			VERIFY_ARE_EQUAL(lightCoral.B, solidColorBrush.Color.B);
		});

		// Close the menu
		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Step 3: Clear SubMenuPresenterStyle (set to null)");
		await RunOnUIThread(() =>
		{
			splitItem.SubMenuPresenterStyle = null;
		});

		// Reopen and test that default style is used
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		subPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_NOT_NULL(subPresenter);

			// Verify that the custom Tag is no longer set (should be null for default style)
			var tag = subPresenter.GetValue(MenuFlyoutPresenter.TagProperty);
			// For default/cleared style, the Tag should be either null or not set to our custom values
			if (tag != null)
			{
				string tagString = tag.ToString();
				VERIFY_ARE_NOT_EQUAL("InitialSubMenuStyle", tagString);
				VERIFY_ARE_NOT_EQUAL("RuntimeModifiedStyle", tagString);
			}

			// The background should revert to default theme background (not our custom colors)
			var background = subPresenter.Background;
			if (background != null)
			{
				var solidColorBrush = background as SolidColorBrush;
				if (solidColorBrush != null)
				{
					// Should not be our custom colors anymore
					var lightGreen = global::Microsoft.UI.Colors.LightGreen;
					var lightCoral = global::Microsoft.UI.Colors.LightCoral;
					VERIFY_IS_FALSE(
						(lightGreen.A == solidColorBrush.Color.A && lightGreen.R == solidColorBrush.Color.R &&
						 lightGreen.G == solidColorBrush.Color.G && lightGreen.B == solidColorBrush.Color.B) ||
						(lightCoral.A == solidColorBrush.Color.A && lightCoral.R == solidColorBrush.Color.R &&
						 lightCoral.G == solidColorBrush.Color.G && lightCoral.B == solidColorBrush.Color.B)
					);
				}
			}
		});

		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemSubMenuPresenterStyleWithItemsWrapGrid()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitItem = null;

		var menuFlyoutOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' >" +
				"  <Button x:Name='button' Content='Test Button' VerticalAlignment='Center' HorizontalAlignment='Center' >" +
				"    <Button.Flyout> " +
				"      <MenuFlyout> " +
				"        <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split Item with WrapGrid Panel'>" +
				"          <SplitMenuFlyoutItem.SubMenuPresenterStyle>" +
				"            <Style TargetType='MenuFlyoutPresenter'>" +
				"              <Setter Property='Tag' Value='WrapGridPanelStyle' />" +
				"              <Setter Property='ItemsPanel'>" +
				"                <Setter.Value>" +
				"                  <ItemsPanelTemplate>" +
				"                    <ItemsWrapGrid Orientation='Horizontal' MaximumRowsOrColumns='2' />" +
				"                  </ItemsPanelTemplate>" +
				"                </Setter.Value>" +
				"              </Setter>" +
				"            </Style>" +
				"          </SplitMenuFlyoutItem.SubMenuPresenterStyle>" +
				"          <MenuFlyoutItem Text='Item 1' />" +
				"          <MenuFlyoutItem Text='Item 2' />" +
				"          <MenuFlyoutItem Text='Item 3' />" +
				"          <MenuFlyoutItem Text='Item 4' />" +
				"          <ToggleMenuFlyoutItem Text='Toggle Item' IsChecked='True' />" +
				"        </SplitMenuFlyoutItem>" +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);
			splitItem = (SplitMenuFlyoutItem)(rootPanel.FindName("splitItem1"));

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open the main MenuFlyout");
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Tap the secondary button of SplitMenuFlyoutItem to open submenu with ItemsWrapGrid");
		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Verify that the submenu with ItemsWrapGrid panel is displayed correctly");
		var subPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_NOT_NULL(subPresenter);

			// Verify the custom Tag value from the style
			var tag = subPresenter.GetValue(MenuFlyoutPresenter.TagProperty);
			VERIFY_IS_NOT_NULL(tag);
			VERIFY_ARE_EQUAL("WrapGridPanelStyle", tag.ToString());

			// Find the actual panel in the visual tree (the ItemsWrapGrid should be a child of ItemsPresenter)
			var itemsPresenter = TreeHelper.GetVisualChildByType<ItemsPresenter>(subPresenter);
			VERIFY_IS_NOT_NULL(itemsPresenter);

			// The actual panel is a child of the ItemsPresenter
			var panel = TreeHelper.GetVisualChildByType<ItemsWrapGrid>(itemsPresenter);
			VERIFY_IS_NOT_NULL(panel);

			// Verify the WrapGrid properties are set correctly
			VERIFY_ARE_EQUAL(Orientation.Horizontal, panel.Orientation);
			VERIFY_ARE_EQUAL(2, panel.MaximumRowsOrColumns);

			// Verify that menu items are actually displayed (should have child elements)
			var children = panel.Children;
			VERIFY_IS_NOT_NULL(children);
			VERIFY_IS_GREATER_THAN(children.Count, 0);

			LOG_OUTPUT("Successfully verified ItemsWrapGrid panel with %d child items", children.Count);
		});

		// Close the menu
		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Step 2: Clear SubMenuPresenterStyle and verify default ItemsPanel");
		await RunOnUIThread(() =>
		{
			splitItem.SubMenuPresenterStyle = null;
		});

		// Reopen and test that default ItemsPanel (StackPanel) is used
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Verify that the default ItemsPanel (StackPanel) is restored");
		subPresenter = await GetCurrentPresenter();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_NOT_NULL(subPresenter);

			// Verify that the custom Tag is no longer set
			var tag = subPresenter.GetValue(MenuFlyoutPresenter.TagProperty);
			if (tag != null)
			{
				string tagString = tag.ToString();
				VERIFY_ARE_NOT_EQUAL("WrapGridPanelStyle", tagString);
			}

			// Find the actual panel in the visual tree - should now be the default StackPanel
			var itemsPresenter = TreeHelper.GetVisualChildByType<ItemsPresenter>(subPresenter);
			VERIFY_IS_NOT_NULL(itemsPresenter);

			// The default panel should be a StackPanel, not an ItemsWrapGrid
			var wrapGridPanel = TreeHelper.GetVisualChildByType<ItemsWrapGrid>(itemsPresenter);
			VERIFY_IS_NULL(wrapGridPanel); // Should not find ItemsWrapGrid anymore

			var stackPanel = TreeHelper.GetVisualChildByType<StackPanel>(itemsPresenter);
			VERIFY_IS_NOT_NULL(stackPanel); // Should find the default StackPanel

			// Verify the StackPanel has the default Vertical orientation
			VERIFY_ARE_EQUAL(Orientation.Vertical, stackPanel.Orientation);

			// Verify that menu items are still displayed (should have child elements)
			var children = stackPanel.Children;
			VERIFY_IS_NOT_NULL(children);
			VERIFY_IS_GREATER_THAN(children.Count, 0);

			LOG_OUTPUT("Successfully verified default StackPanel with %d child items", children.Count);
		});

		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});
	}

	private async Task<Grid> SetupRootPanelForSubMenuItemStyleTests()
	{
		Grid rootPanel = null;

		await RunOnUIThread(() =>
		{
			rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' >" +
				"  <Grid.Resources>" +
				"    <Style x:Key='TestMenuItemStyle' TargetType='MenuFlyoutItem'>" +
				"      <Setter Property='Tag' Value='TestStyleTag' />" +
				"      <Setter Property='Foreground' Value='LightBlue' />" +
				"    </Style>" +
				"  </Grid.Resources>" +
				"  <Button x:Name='button' Content='Test Button' VerticalAlignment='Center' HorizontalAlignment='Center' >" +
				"    <Button.Flyout> " +
				"      <MenuFlyout> " +
				"        <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split Item with SubMenuItemStyle'>" +
				"          <SplitMenuFlyoutItem.SubMenuItemStyle>" +
				"            <Style TargetType='MenuFlyoutItem'>" +
				"              <Setter Property='Tag' Value='SubMenuItemStyleTag' />" +
				"              <Setter Property='Foreground' Value='LightGreen' />" +
				"            </Style>" +
				"          </SplitMenuFlyoutItem.SubMenuItemStyle>" +
				"          <MenuFlyoutItem Text='Sub Item 1' />" +
				"          <MenuFlyoutItem Text='Sub Item 2' />" +
				"          <MenuFlyoutItem Text='Sub Item 3' />" +
				"        </SplitMenuFlyoutItem>" +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		return rootPanel;
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemSubMenuItemStyleFromXaml()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitItem = null;

		var menuFlyoutOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));

		Grid rootPanel = await SetupRootPanelForSubMenuItemStyleTests();

		await RunOnUIThread(() =>
		{
			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);
			splitItem = (SplitMenuFlyoutItem)(rootPanel.FindName("splitItem1"));

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open the main MenuFlyout");
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Tap the secondary button of SplitMenuFlyoutItem to open submenu");
		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Verify that SubMenuItemStyle is applied to all sub menu items");
		await RunOnUIThread(() =>
		{
			var items = splitItem.Items;
			VERIFY_ARE_EQUAL(3, items.Count);

			for (int i = 0; i < items.Count; ++i)
			{
				var item = items[i];
				var menuItem = item as MenuFlyoutItem;
				VERIFY_IS_NOT_NULL(menuItem);

				// Verify the Tag is set from the SubMenuItemStyle
				var tag = menuItem.GetValue(MenuFlyoutItem.TagProperty);
				VERIFY_IS_NOT_NULL(tag);
				VERIFY_ARE_EQUAL("SubMenuItemStyleTag", tag.ToString());

				// Verify the Foreground color is set from the SubMenuItemStyle
				var foreground = menuItem.Foreground;
				VERIFY_IS_NOT_NULL(foreground);
				var solidColorBrush = foreground as SolidColorBrush;
				VERIFY_IS_NOT_NULL(solidColorBrush);

				var lightGreen = global::Microsoft.UI.Colors.LightGreen;
				VERIFY_ARE_EQUAL(lightGreen.A, solidColorBrush.Color.A);
				VERIFY_ARE_EQUAL(lightGreen.R, solidColorBrush.Color.R);
				VERIFY_ARE_EQUAL(lightGreen.G, solidColorBrush.Color.G);
				VERIFY_ARE_EQUAL(lightGreen.B, solidColorBrush.Color.B);
			}
		});

		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemSubMenuItemStyleWithLocalStyles()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitItem = null;

		var menuFlyoutOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' >" +
				"  <Button x:Name='button' Content='Test Button' VerticalAlignment='Center' HorizontalAlignment='Center' >" +
				"    <Button.Flyout> " +
				"      <MenuFlyout> " +
				"        <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split Item with Mixed Styles'>" +
				"          <SplitMenuFlyoutItem.SubMenuItemStyle>" +
				"            <Style TargetType='MenuFlyoutItem'>" +
				"              <Setter Property='Tag' Value='SubMenuItemStyleTag' />" +
				"              <Setter Property='Foreground' Value='LightGreen' />" +
				"            </Style>" +
				"          </SplitMenuFlyoutItem.SubMenuItemStyle>" +
				"          <MenuFlyoutItem Text='Sub Item 1' />" +
				"          <MenuFlyoutItem Text='Sub Item 2' />" +
				"          <MenuFlyoutItem Text='Sub Item 3' >" +
				"            <MenuFlyoutItem.Style>" +
				"              <Style TargetType='MenuFlyoutItem'>" +
				"                <Setter Property='Tag' Value='LocalStyleTag' />" +
				"                <Setter Property='Foreground' Value='LightCoral' />" +
				"              </Style>" +
				"            </MenuFlyoutItem.Style>" +
				"          </MenuFlyoutItem>" +
				"        </SplitMenuFlyoutItem>" +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);
			splitItem = (SplitMenuFlyoutItem)(rootPanel.FindName("splitItem1"));

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open the main MenuFlyout");
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Tap the secondary button of SplitMenuFlyoutItem to open submenu");
		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Verify mixed styles: SubMenuItemStyle applied where no local style, local style preserved where set");
		await RunOnUIThread(() =>
		{
			var items = splitItem.Items;
			VERIFY_ARE_EQUAL(3, items.Count);

			for (int i = 0; i < items.Count; ++i)
			{
				var menuItem = items[i] as MenuFlyoutItem;
				VERIFY_IS_NOT_NULL(menuItem);
				var tagString = menuItem.GetValue(MenuFlyoutItem.TagProperty);
				VERIFY_IS_NOT_NULL(tagString);
				VERIFY_ARE_EQUAL(i < 2 ? "SubMenuItemStyleTag" : "LocalStyleTag", tagString.ToString());
			}
		});

		await RunOnUIThread(() =>
		{
			menuFlyout.Hide();
		});
	}

	[TestMethod]
	public async Task ValidateCanChangeSplitMenuItemSubMenuItemStyle()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitItem = null;

		var menuFlyoutOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' >" +
				"  <Button x:Name='button' Content='Test Button' VerticalAlignment='Center' HorizontalAlignment='Center' >" +
				"    <Button.Flyout> " +
				"      <MenuFlyout> " +
				"        <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split Item Runtime Style Changes'>" +
				"          <MenuFlyoutItem Text='Sub Item 1' />" +
				"          <MenuFlyoutItem Text='Sub Item 2' />" +
				"        </SplitMenuFlyoutItem>" +
				"      </MenuFlyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);
			splitItem = (SplitMenuFlyoutItem)(rootPanel.FindName("splitItem1"));

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Step 1: Apply SubMenuItemStyle at runtime");
		await RunOnUIThread(() =>
		{
			var initialStyle = new Style(typeof(MenuFlyoutItem));
			initialStyle.Setters.Add(new Setter(MenuFlyoutItem.TagProperty, "InitialStyleTag"));
			initialStyle.Setters.Add(new Setter(MenuFlyoutItem.ForegroundProperty, new SolidColorBrush(global::Microsoft.UI.Colors.LightCoral)));

			splitItem.SubMenuItemStyle = initialStyle;
		});

		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var items = splitItem.Items;
			for (int i = 0; i < items.Count; ++i)
			{
				var item = items[i] as MenuFlyoutItem;
				var tag = item.GetValue(MenuFlyoutItem.TagProperty);
				VERIFY_IS_NOT_NULL(tag);
				VERIFY_ARE_EQUAL("InitialStyleTag", tag.ToString());
			}
			menuFlyout.Hide();
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Step 2: Change SubMenuItemStyle to different style");
		await RunOnUIThread(() =>
		{
			var modifiedStyle = new Style(typeof(MenuFlyoutItem));
			modifiedStyle.Setters.Add(new Setter(MenuFlyoutItem.TagProperty, "ModifiedStyleTag"));
			modifiedStyle.Setters.Add(new Setter(MenuFlyoutItem.ForegroundProperty, new SolidColorBrush(global::Microsoft.UI.Colors.LightBlue)));

			splitItem.SubMenuItemStyle = modifiedStyle;
		});

		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var items = splitItem.Items;
			for (int i = 0; i < items.Count; ++i)
			{
				var item = items[i] as MenuFlyoutItem;
				var tag = item.GetValue(MenuFlyoutItem.TagProperty);
				VERIFY_IS_NOT_NULL(tag);
				VERIFY_ARE_EQUAL("ModifiedStyleTag", tag.ToString());
			}
			menuFlyout.Hide();
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Step 3: Clear SubMenuItemStyle");
		await RunOnUIThread(() =>
		{
			splitItem.SubMenuItemStyle = null;
		});

		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var items = splitItem.Items;
			for (int i = 0; i < items.Count; ++i)
			{
				var item = items[i] as MenuFlyoutItem;
				var tag = item.GetValue(MenuFlyoutItem.TagProperty);
				// Tag should be null or not our custom values after clearing style
				if (tag != null)
				{
					string tagString = tag.ToString();
					VERIFY_ARE_NOT_EQUAL("InitialStyleTag", tagString);
					VERIFY_ARE_NOT_EQUAL("ModifiedStyleTag", tagString);
				}
			}
			menuFlyout.Hide();
		});
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemSubMenuItemStyleWithDynamicItems()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitItem = null;

		var menuFlyoutOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));

		Grid rootPanel = await SetupRootPanelForSubMenuItemStyleTests();

		await RunOnUIThread(() =>
		{
			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);
			splitItem = (SplitMenuFlyoutItem)(rootPanel.FindName("splitItem1"));

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Step 1: Add new item without local style - should get SubMenuItemStyle");
		await RunOnUIThread(() =>
		{
			var newItem = new MenuFlyoutItem();
			newItem.Text = "Added Item Without Style";
			splitItem.Items.Add(newItem);
		});

		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var items = splitItem.Items;
			VERIFY_ARE_EQUAL(4, items.Count);

			// All items should have the SubMenuItemStyle
			for (int i = 0; i < items.Count; ++i)
			{
				var item = items[i] as MenuFlyoutItem;
				var tag = item.GetValue(MenuFlyoutItem.TagProperty);
				VERIFY_IS_NOT_NULL(tag);
				VERIFY_ARE_EQUAL("SubMenuItemStyleTag", tag.ToString());
			}
			menuFlyout.Hide();
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Step 2: Add new item with local style - should preserve local style");
		await RunOnUIThread(() =>
		{
			var localStyle = new Style(typeof(MenuFlyoutItem));
			localStyle.Setters.Add(new Setter(MenuFlyoutItem.TagProperty, "LocalStyleTag"));
			localStyle.Setters.Add(new Setter(MenuFlyoutItem.ForegroundProperty, new SolidColorBrush(global::Microsoft.UI.Colors.Red)));

			var newItemWithStyle = new MenuFlyoutItem();
			newItemWithStyle.Text = "Added Item With Local Style";
			newItemWithStyle.Style = localStyle;
			splitItem.Items.Add(newItemWithStyle);
		});

		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var items = splitItem.Items;
			VERIFY_ARE_EQUAL(5, items.Count);

			// First four items should have SubMenuItemStyle
			for (int i = 0; i < 4; ++i)
			{
				var item = items[i] as MenuFlyoutItem;
				var tag = item.GetValue(MenuFlyoutItem.TagProperty);
				VERIFY_IS_NOT_NULL(tag);
				VERIFY_ARE_EQUAL("SubMenuItemStyleTag", tag.ToString());
			}

			// Third item should preserve its local style
			var item5 = items[4] as MenuFlyoutItem;
			var tag5 = item5.GetValue(MenuFlyoutItem.TagProperty);
			VERIFY_IS_NOT_NULL(tag5);
			VERIFY_ARE_EQUAL("LocalStyleTag", tag5.ToString());

			menuFlyout.Hide();
		});
	}

	[TestMethod]
	public async Task ValidateSplitMenuItemSubMenuItemStyleTypeCompatibility()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitItem = null;

		var menuFlyoutOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));

		Grid rootPanel = await SetupRootPanelForSubMenuItemStyleTests();

		await RunOnUIThread(() =>
		{
			button = (Button)(rootPanel.FindName("button"));
			menuFlyout = (MenuFlyout)(button.Flyout);
			splitItem = (SplitMenuFlyoutItem)(rootPanel.FindName("splitItem1"));

			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				menuFlyoutOpenedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Step 1: Verify initial setup - SubMenuItemStyle applies to normal MenuFlyoutItem");
		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var items = splitItem.Items;
			VERIFY_ARE_EQUAL(3, items.Count);

			menuFlyout.Hide();
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Step 2: Add ToggleMenuFlyoutItem with local style - should work correctly");
		await RunOnUIThread(() =>
		{
			var toggleLocalStyle = new Style(typeof(ToggleMenuFlyoutItem));
			toggleLocalStyle.Setters.Add(new Setter(ToggleMenuFlyoutItem.TagProperty, "ToggleLocalStyleTag"));

			var toggleItemWithStyle = new ToggleMenuFlyoutItem();
			toggleItemWithStyle.Text = "Toggle Item With Local Style";
			toggleItemWithStyle.Style = toggleLocalStyle;
			splitItem.Items.Add(toggleItemWithStyle);
		});

		TestServices.InputHelper.Tap(button);
		await menuFlyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await TapSplitMenuItemSecondary(splitItem);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var items = splitItem.Items;
			VERIFY_ARE_EQUAL(4, items.Count);

			// Second item should have its local style preserved
			var toggleItem = items[3] as ToggleMenuFlyoutItem;
			VERIFY_IS_NOT_NULL(toggleItem);
			var tag = toggleItem.GetValue(ToggleMenuFlyoutItem.TagProperty);
			VERIFY_IS_NOT_NULL(tag);
			VERIFY_ARE_EQUAL("ToggleLocalStyleTag", tag.ToString());

			menuFlyout.Hide();
		});
		await TestServices.WindowHelper.WaitForIdle();
	}
}
