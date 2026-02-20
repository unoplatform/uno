#pragma warning disable 168 // for cleanup imported member

#if HAS_UNO
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Uno.UI.RuntimeTests.Helpers;

using static Private.Infrastructure.TestServices;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests;
using MUXControlsTestApp.Utilities;

#if !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

namespace Windows.UI.Tests.Enterprise
{
	[TestClass]
	public class AppBarIntegrationTests : BaseDxamlTestClass
	{
		[ClassInitialize]
		public static void ClassSetup()
		{
			CommonTestSetupHelper.CommonTestClassSetup();
		}

		[ClassCleanup]
		public static void TestCleanup()
		{
			TestServices.WindowHelper.VerifyTestCleanup();
		}

		//
		// Test Cases
		//
		//void AppBarIntegrationTests::CanInstantiate()
		//{
		//	Generic::DependencyObjectTests < AppBar >::CanInstantiate();
		//}

		[TestMethod]
		[Description("Validates that we can successfully add/remove an AppBar from the live tree.")]
		public async Task CanEnterAndLeaveLiveTree()
		{
			TestCleanupWrapper cleanup;

			AppBar appBar = null;
			Page page = null;

			var hasLoadedEvent = new Event();
			var hasUnloadedEvent = new Event();

			var loadedRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("Loaded");
			var unloadedRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("Unloaded");

			await RunOnUIThread(() =>
			{
				appBar = new AppBar();
				loadedRegistration.Attach(appBar, (s, e) =>
				{
					hasLoadedEvent.Set();
				});

				unloadedRegistration.Attach(appBar, (s, e) =>
				{
					hasUnloadedEvent.Set();
				});

				page = TestServices.WindowHelper.SetupSimulatedAppPage();
			});

			await TestServices.WindowHelper.WaitForIdle();

			// Verify enter/leave for top appbar.
			LOG_OUTPUT("Verify enter/leave for top appbar.");
			await RunOnUIThread(() =>
			{
				page.TopAppBar = appBar;
				appBar.IsOpen = true;
			});
			await hasLoadedEvent.WaitForDefault();

			await RunOnUIThread(() =>
			{
				page.TopAppBar = null;
			});
			await hasUnloadedEvent.WaitForDefault();

			// Verify enter/leave for bottom appbar.
			LOG_OUTPUT("Verify enter/leave for bottom appbar.");
			await RunOnUIThread(() =>
			{
				page.BottomAppBar = appBar;
				appBar.IsOpen = true;
			});
			await hasLoadedEvent.WaitForDefault();

			await RunOnUIThread(() =>
			{
				page.BottomAppBar = null;
			});
			await hasUnloadedEvent.WaitForDefault();

			// Verify enter/leave for inline appbar.
			LOG_OUTPUT("Verify enter/leave for inline appbar.");
			await RunOnUIThread(() =>
			{
				SetPageContent(appBar, page);
				appBar.IsOpen = true;
			});
			await hasLoadedEvent.WaitForDefault();

			await RunOnUIThread(() =>
			{
				SetPageContent(null, page);
			});
			await hasUnloadedEvent.WaitForDefault();
		}


		[TestMethod]
		[Description("Validates that AppBar opens/closes in response to calls to AppBar.IsOpen.")]
		public async Task CanOpenAndCloseUsingAPI()
		{
			TestCleanupWrapper cleanup;

			AppBar appBar = null;
			Page page = null;

			var openedEvent = new Event();
			var closedEvent = new Event();

			var openedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var closedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");

			// Setup our environment.
			await RunOnUIThread(() =>
			{
				appBar = new AppBar();
				openedRegistration.Attach(appBar, (s, e) => openedEvent.Set());
				closedRegistration.Attach(appBar, (s, e) => closedEvent.Set());

				page = TestServices.WindowHelper.SetupSimulatedAppPage();
			});
			await WindowHelper.WaitForIdle();

			// Verify open/close for top appbar.
			LOG_OUTPUT("Verify open/close for top appbar.");
			await RunOnUIThread(() =>
			{
				page.TopAppBar = appBar;
				appBar.IsOpen = true;
			});
			await openedEvent.WaitForDefault();

			await RunOnUIThread(() => appBar.IsOpen = false);
			await closedEvent.WaitForDefault();

			await RunOnUIThread(() => page.TopAppBar = null);
			await WindowHelper.WaitForIdle();

			// Verify open/close for bottom appbar.
			LOG_OUTPUT("Verify open/close for bottom appbar.");
			await RunOnUIThread(() =>
			{
				page.BottomAppBar = appBar;
				appBar.IsOpen = true;
			});
			await openedEvent.WaitForDefault();

			await RunOnUIThread(() => appBar.IsOpen = false);
			await closedEvent.WaitForDefault();

			await RunOnUIThread(() => page.BottomAppBar = null);
			await WindowHelper.WaitForIdle();

			// Verify open/close for inline appbar.
			LOG_OUTPUT("Verify open/close for inline appbar.");
			await RunOnUIThread(() =>
			{
				SetPageContent(appBar, page);
				appBar.IsOpen = true;
			});
			await openedEvent.WaitForDefault();

			await RunOnUIThread(() => appBar.IsOpen = false);
			await closedEvent.WaitForDefault();

			await RunOnUIThread(() => SetPageContent(null, page));
		}

		[TestMethod]
		[Description("Validates that Top and Bottom (and not Inline) AppBars open/close in response to ContextMenu key.")]
		public async Task CanOpenAndCloseUsingKeyboard()
		{
			TestCleanupWrapper cleanup;

			AppBar topAppBar = null;
			AppBar bottomAppBar = null;
			AppBar inlineAppBar = null;
			Page page = await SetupTopBottomInlineAppBarsPage();

			string contextMenuKeySequence = "$d$_apps#$u$_apps";

			var topOpenedEvent = new Event();
			var topClosedEvent = new Event();
			var bottomOpenedEvent = new Event();
			var bottomClosedEvent = new Event();
			var inlineOpenedEvent = new Event();
			var inlineClosedEvent = new Event();

			var topOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var topClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");
			var bottomOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var bottomClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");
			var inlineOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var inlineClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");

			await RunOnUIThread(() =>
			{
				topAppBar = page.TopAppBar;
				bottomAppBar = page.BottomAppBar;
				inlineAppBar = ((Panel)page.Content).FindName("inlineAppBar") as AppBar;
			});
			await WindowHelper.WaitForIdle();

			AttachOpenedAndClosedHandlers(topAppBar, topOpenedEvent, topOpenedRegistration, topClosedEvent, topClosedRegistration);
			AttachOpenedAndClosedHandlers(bottomAppBar, bottomOpenedEvent, bottomOpenedRegistration, bottomClosedEvent, bottomClosedRegistration);
			AttachOpenedAndClosedHandlers(inlineAppBar, inlineOpenedEvent, inlineOpenedRegistration, inlineClosedEvent, inlineClosedRegistration);

			LOG_OUTPUT("Pressing the CONTEXTMENU key opens the top and bottom AppBars only (and not the inline AppBar).");
			await KeyboardHelper.PressKeySequence(contextMenuKeySequence);
			await topOpenedEvent.WaitForDefault();
			await bottomOpenedEvent.WaitForDefault();
			VERIFY_IS_FALSE(inlineOpenedEvent.HasFired());

			LOG_OUTPUT("Pressing the CONTEXTMENU key closes the top and bottom AppBars only (and not the inline AppBar).");
			await KeyboardHelper.PressKeySequence(contextMenuKeySequence);
			await topClosedEvent.WaitForDefault();
			await bottomClosedEvent.WaitForDefault();
			VERIFY_IS_FALSE(inlineClosedEvent.HasFired());
		}

		[TestMethod]
		[Description("Validates that only non-sticky AppBars can be closed by using the Escape key.")]
		public async Task CanCloseNonStickyAppBarUsingEscapeKey()
		{
			TestCleanupWrapper cleanup;

			Button stickyTopExpandButton = null;
			Button bottomExpandButton = null;
			Button inlineExpandButton = null;
			Button stickyInlineExpandButton = null;
			AppBar stickyTopAppBar = null;
			AppBar bottomAppBar = null;
			AppBar inlineAppBar = null;
			AppBar stickyInlineAppBar = null;
			Page page = await SetupTopBottomInlineAppBarsPage();

			string escapeKeySequence = "$d$_esc#$u$_esc";
			string expectedFocusSequence = "[BEB][STEB][FEB][STEB]";
			string focusSequence = "";

			var stickyTopOpenedEvent = new Event();
			var stickyTopClosedEvent = new Event();
			var bottomOpenedEvent = new Event();
			var bottomClosedEvent = new Event();
			var inlineOpenedEvent = new Event();
			var inlineClosedEvent = new Event();
			var stickyInlineOpenedEvent = new Event();
			var stickyInlineClosedEvent = new Event();

			var stickyTopOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var stickyTopClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var bottomOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var bottomClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var inlineOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var inlineClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var stickyInlineOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var stickyInlineClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");

			var pageGotFocusRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("GotFocus");
			var topAppBarGotFocusRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("GotFocus");
			var bottomAppBarGotFocusRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("GotFocus");

			await RunOnUIThread(() =>
			{
				RoutedEventHandler gotFocusHandler = (s, e) => focusSequence += "[" + ((FrameworkElement)e.OriginalSource).Tag + "]";

				pageGotFocusRegistration.Attach(page, gotFocusHandler);
				topAppBarGotFocusRegistration.Attach(page.TopAppBar, gotFocusHandler);
				bottomAppBarGotFocusRegistration.Attach(page.BottomAppBar, gotFocusHandler);

				stickyTopAppBar = page.TopAppBar;
				stickyTopAppBar.IsSticky = true;

				bottomAppBar = page.BottomAppBar;

				var panel = (Panel)page.Content;
				inlineAppBar = (AppBar)panel.FindName("inlineAppBar");

				stickyInlineAppBar = (AppBar)XamlReader.Load(@"
					<AppBar  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                        x:Name=""stickyInlineAppBar"" IsSticky=""True"">
                        <Rectangle Width=""100"" Height=""60"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"" Fill=""Red""/>
                    </AppBar>");

				panel.Children.Add(stickyInlineAppBar);
			});
			await WindowHelper.WaitForIdle();

			AttachOpenedAndClosedHandlers(stickyTopAppBar, stickyTopOpenedEvent, stickyTopOpenedRegistration, stickyTopClosedEvent, stickyTopClosedRegistration);
			AttachOpenedAndClosedHandlers(bottomAppBar, bottomOpenedEvent, bottomOpenedRegistration, bottomClosedEvent, bottomClosedRegistration);
			AttachOpenedAndClosedHandlers(inlineAppBar, inlineOpenedEvent, inlineOpenedRegistration, inlineClosedEvent, inlineClosedRegistration);
			AttachOpenedAndClosedHandlers(stickyInlineAppBar, stickyInlineOpenedEvent, stickyInlineOpenedRegistration, stickyInlineClosedEvent, stickyInlineClosedRegistration);

			// Setup for the test by opening the AppBars.
			await RunOnUIThread(() =>
			{
				stickyTopExpandButton = (Button)TreeHelper.GetVisualChildByName(stickyTopAppBar, "ExpandButton");
				stickyTopExpandButton.Tag = "STEB";

				bottomExpandButton = (Button)TreeHelper.GetVisualChildByName(bottomAppBar, "ExpandButton");
				bottomExpandButton.Tag = "BEB";

				inlineExpandButton = (Button)TreeHelper.GetVisualChildByName(inlineAppBar, "ExpandButton");
				inlineExpandButton.Tag = "FEB";

				stickyInlineExpandButton = (Button)TreeHelper.GetVisualChildByName(stickyInlineAppBar, "ExpandButton");
				stickyInlineExpandButton.Tag = "SFEB";

				stickyTopAppBar.IsOpen = true;
				bottomAppBar.IsOpen = true;
				inlineAppBar.IsOpen = true;
				stickyInlineAppBar.IsOpen = true;
			});
			await stickyTopOpenedEvent.WaitForDefault();
			await bottomOpenedEvent.WaitForDefault();
			await inlineOpenedEvent.WaitForDefault();
			await stickyInlineOpenedEvent.WaitForDefault();

			await RunOnUIThread(() =>
			{
				focusSequence = "";

				// Start with the focus on the bottom AppBar expand button.
				bottomExpandButton.Focus(FocusState.Programmatic);
			});

			await KeyboardHelper.Tab();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Try closing the non-sticky bottom AppBar using ESC key.");
			await KeyboardHelper.PressKeySequence(escapeKeySequence);
			await bottomClosedEvent.WaitForDefault();


			LOG_OUTPUT("Try closing the non-sticky inline Appbar using ESC key.");
			await KeyboardHelper.PressKeySequence(escapeKeySequence);
			await inlineClosedEvent.WaitForDefault();

			await RunOnUIThread(() =>
			{
				// Verify that IsOpen is set to false for all AppBars EXCEPT the sticky ones.
				VERIFY_IS_TRUE(stickyTopAppBar.IsOpen);
				VERIFY_IS_FALSE(bottomAppBar.IsOpen);
				VERIFY_IS_FALSE(inlineAppBar.IsOpen);
				VERIFY_IS_TRUE(stickyInlineAppBar.IsOpen);

				LOG_OUTPUT($"Expected focus sequence: {expectedFocusSequence}");
				LOG_OUTPUT($"Actual focus sequence: {focusSequence}");
				VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);
			});
		}

		[TestMethod]
		[Description("Validates that AppBar opens/closes in response to mouse input.")]
		[TestProperty("TestPass:IncludeOnlyOn", "Desktop")]
		[TestProperty("Hosting:Mode", "UAP")]
		[Ignore("MouseHelper not implemented")]
		public async Task CanOpenAndCloseUsingMouse()
		{
			await CanOpenAndCloseUsingRightTappedEvent(usePen: false);
		}

		[TestMethod]
		[Description("Validates that AppBar opens/closes in response to pen + barrel button.")]
		[TestProperty("TestPass:IncludeOnlyOn", "Desktop")]
		[Ignore("MouseHelper not implemented")]
		public async Task CanOpenAndCloseUsingPen()
		{
			await CanOpenAndCloseUsingRightTappedEvent(usePen: true);
		}

		private async Task CanOpenAndCloseUsingRightTappedEvent(bool usePen)
		{
			TestCleanupWrapper cleanup;

			AppBar topAppBar = null;
			AppBar bottomAppBar = null;
			Page page = null;

			var pageBounds = new Rect();

			var topOpenedEvent = new Event();
			var topClosedEvent = new Event();
			var bottomOpenedEvent = new Event();
			var bottomClosedEvent = new Event();

			var topOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var topClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");
			var bottomOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var bottomClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");

			// Setup our environment.
			await RunOnUIThread(() =>
			{
				topAppBar = new AppBar();
				topOpenedRegistration.Attach(topAppBar, (s, e) => topOpenedEvent.Set());
				topClosedRegistration.Attach(topAppBar, (s, e) => topClosedEvent.Set());

				bottomAppBar = new AppBar();
				bottomOpenedRegistration.Attach(bottomAppBar, (s, e) => bottomOpenedEvent.Set());
				bottomClosedRegistration.Attach(bottomAppBar, (s, e) => bottomClosedEvent.Set());

				page = WindowHelper.SetupSimulatedAppPage();
				page.TopAppBar = topAppBar;
				page.BottomAppBar = bottomAppBar;

				page.Focus(FocusState.Keyboard);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(async () => pageBounds = await ControlHelper.GetBounds(page));

			if (usePen)
			{
				LOG_OUTPUT("Open the appbars using pen + barrel button.");
				TestServices.InputHelper.PenBarrelTap(page);
			}
			if (!usePen)
			{
				// Move the mouse into the center of page
				TestServices.InputHelper.MoveMouse(page);
				await WindowHelper.WaitForIdle();

				LOG_OUTPUT("Open the appbars using right mouse click.");
				TestServices.InputHelper.ClickMouseButton(MouseButton.Right, new Point(pageBounds.Left + pageBounds.Width / 2, pageBounds.Top + pageBounds.Height / 2));
			}

			await topOpenedEvent.WaitForDefault();
			await bottomOpenedEvent.WaitForDefault();

			if (usePen)
			{
				LOG_OUTPUT("Open the appbars using pen + barrel button.");
				TestServices.InputHelper.PenBarrelTap(page);
			}
			else
			{
				LOG_OUTPUT("Close the appbars using right mouse click.");
				TestServices.InputHelper.ClickMouseButton(MouseButton.Right, new Point(pageBounds.Left + pageBounds.Width / 2, pageBounds.Top + pageBounds.Height / 2));
			}


			await topClosedEvent.WaitForDefault();
			await bottomClosedEvent.WaitForDefault();
		}

		[TestMethod]
		[Description("Validates that an AppBar with AppBar.ClosedDisplayMode=Minimal can be opened by clicking the bar itself.")]
		[TestProperty("TestPass:IncludeOnlyOn", "Desktop")]
#if !__SKIA__
		[Ignore("Test is failing on non-Skia targets https://github.com/unoplatform/uno/issues/17984")]
#endif
		public async Task CanOpenMinimalAppBarUsingMouse()
		{
			TestCleanupWrapper cleanup;

			var page = await SetupTopBottomInlineAppBarsPage();

			AppBar topAppBar = null;
			AppBar bottomAppBar = null;
			AppBar inlineAppBar = null;

			await RunOnUIThread(() =>
			{
				topAppBar = page.TopAppBar;
				bottomAppBar = page.BottomAppBar;
				inlineAppBar = (AppBar)((Panel)page.Content).FindName("inlineAppBar");
			});
			await WindowHelper.WaitForIdle();

			await CanOpenMinimalAppBarUsingMouseHelper(topAppBar);
			await CanOpenMinimalAppBarUsingMouseHelper(bottomAppBar);
			await CanOpenMinimalAppBarUsingMouseHelper(inlineAppBar);
		}

		[TestMethod]
		[Description("Validates tapping on the '...' button opens both AppBars if at least one is closed, and closes them if they're both open.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		public async Task CanOpenAndCloseUsingExpandButton()
		{
			TestCleanupWrapper cleanup;

			Page page = await SetupClosedDisplayModeTestEnvironment(setClosedDisplayModeValues: true);
			Button expandButton = null;

			var bottomOpenedEvent = new Event();
			var bottomClosedEvent = new Event();

			var bottomOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var bottomClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");

			await RunOnUIThread(() =>
			{
				bottomOpenedRegistration.Attach(page.BottomAppBar, (s, e) => bottomOpenedEvent.Set());
				bottomClosedRegistration.Attach(page.BottomAppBar, (s, e) => bottomClosedEvent.Set());

				expandButton = (Button)TreeHelper.GetVisualChildByName(page.BottomAppBar, "ExpandButton");
			});

			TestServices.InputHelper.Tap(expandButton);

			await bottomOpenedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => VERIFY_IS_TRUE(page.BottomAppBar.IsOpen));

			TestServices.InputHelper.Tap(expandButton);

			await bottomClosedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => VERIFY_IS_FALSE(page.BottomAppBar.IsOpen));
		}

		[TestMethod]
		[Description("Validates that Tab navigation works on AppBar child items.")]
		public async Task CanTabThroughChildItems()
		{
			TestCleanupWrapper cleanup;

			//AppBar appBar = null;
			Page page = null;
			Button button = null;

			var focusSequence = "";

			List<SafeEventRegistration<AppBarButton, RoutedEventHandler>> topGotFocusRegistrations = new List<SafeEventRegistration<AppBarButton, RoutedEventHandler>>();
			List<SafeEventRegistration<AppBarButton, RoutedEventHandler>> bottomGotFocusRegistrations = new List<SafeEventRegistration<AppBarButton, RoutedEventHandler>>();
			var buttonGotFocusRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>("GotFocus");

			const int itemsPerBar = 3;
			var expectedFocusSequence = "[B0][B1][B2][T0][T1][T2][BTN][B0]";

			// Setup our environment.
			await RunOnUIThread(() =>
			{
				page = WindowHelper.SetupSimulatedAppPage();
				page.TopAppBar = new AppBar();
				page.TopAppBar.IsOpen = true;
				page.TopAppBar.IsSticky = true;

				var topStackPanel = new StackPanel();
				topStackPanel.Orientation = Orientation.Horizontal;

				RoutedEventHandler gotFocusHandler = (s, e) => focusSequence += "[" + ((FrameworkElement)s).Name + "]";

				for (int i = 0; i < itemsPerBar; ++i)
				{
					var button = new AppBarButton();
					button.Name = $"T{i}";
					topStackPanel.Children.Add(button);

					var gotFocusRegistration = CreateSafeEventRegistration<AppBarButton, RoutedEventHandler>("GotFocus");
					gotFocusRegistration.Attach(button, gotFocusHandler);
					topGotFocusRegistrations.Add(gotFocusRegistration);
				}

				page.BottomAppBar = new AppBar();
				page.BottomAppBar.IsOpen = true;
				page.BottomAppBar.IsSticky = true;

				var bottomStackPanel = new StackPanel();
				bottomStackPanel.Orientation = Orientation.Horizontal;

				for (int i = 0; i < itemsPerBar; ++i)
				{
					var button = new AppBarButton();
					button.Name = $"B{i}";
					bottomStackPanel.Children.Add(button);

					var gotFocusRegistration = CreateSafeEventRegistration<AppBarButton, RoutedEventHandler>("GotFocus");
					gotFocusRegistration.Attach(button, gotFocusHandler);
					bottomGotFocusRegistrations.Add(gotFocusRegistration);
				}

				button = new Button();
				button.Name = "BTN";
				buttonGotFocusRegistration.Attach(button, gotFocusHandler);

				SetPageContent(button, page);
				page.TopAppBar.Content = topStackPanel;
				page.BottomAppBar.Content = bottomStackPanel;

				SetWindowContent(page);
			});
			await WindowHelper.WaitForIdle();

			// Start the test off with the button focused.
			await RunOnUIThread(() => button.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			// Clear out the focus sequence before we run our scenario.
			focusSequence = "";

			LOG_OUTPUT("Validate tabbing through app bar items and page content.");

			// We iterate (itemsPerBar * 2 + 4) times because we want to
			// tab through all the top and bottom app bar items, the expand buttons, and the
			// button in the page's content, and then back into the first
			// app bar item in the bottom app bar.
			for (int i = 0; i < (itemsPerBar * 2 + 4); ++i)
			{
				await SimulateTabAsync(page);
				await WindowHelper.WaitForIdle();
			}

			LOG_OUTPUT($"Expected focus sequence: {expectedFocusSequence}");
			LOG_OUTPUT($"Actual focus sequence: {focusSequence}");
			VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);
		}

		[TestMethod]
		[Description("Validates that items in an app bar are clickable.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		[Ignore("Test is failing on all targets https://github.com/unoplatform/uno/issues/17984")]
		public async Task CanClickAButtonInAnAppBar()
		{
			TestCleanupWrapper cleanup;

			AppBar appBar = null;
			Page page = null;
			AppBarButton button = null;

			var appBarLoadedEvent = new Event();
			var clickedEvent = new Event();

			var loadedRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("Loaded");
			var clickRegistration = CreateSafeEventRegistration<AppBarButton, RoutedEventHandler>("Click");

			// Setup our environment.
			await RunOnUIThread(() =>
			{
				button = new AppBarButton();
				clickRegistration.Attach(button, (s, e) => clickedEvent.Set());

				appBar = new AppBar();
				appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
				appBar.Content = button;
				loadedRegistration.Attach(appBar, (s, e) => appBarLoadedEvent.Set());

				page = WindowHelper.SetupSimulatedAppPage();
				page.TopAppBar = appBar;
			});
			await appBarLoadedEvent.WaitForDefault();

			// Wait for edge theme animation to finish.
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Validate clicking a button in the top app bar.");
			TestServices.InputHelper.Tap(button);
			await clickedEvent.WaitForDefault();

			LOG_OUTPUT("Validate clicking a button in the bottom app bar.");
			await RunOnUIThread(() =>
			{
				page.TopAppBar = null;
				page.BottomAppBar = appBar;
			});
			await WindowHelper.WaitForIdle();

			TestServices.InputHelper.Tap(button);
			await clickedEvent.WaitForDefault();

			LOG_OUTPUT("Validate clicking a button in an inline app bar.");
			await RunOnUIThread(() =>
			{
				page.BottomAppBar = null;
				SetPageContent(appBar, page);
			});
			await WindowHelper.WaitForIdle();

			TestServices.InputHelper.Tap(button);
			await clickedEvent.WaitForDefault();
		}

		[TestMethod]
		[Description("Validates that the AppBar.ClosedDisplayMode property is accessible and has the correct default value in Threshold.")]
		public async Task CanGetAndSetClosedDisplayMode()
		{
			TestCleanupWrapper cleanup;

			Page page = null;

			page = await SetupClosedDisplayModeTestEnvironment(setClosedDisplayModeValues: false);

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(page.TopAppBar.ClosedDisplayMode, AppBarClosedDisplayMode.Minimal);

				page.TopAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
				VERIFY_ARE_EQUAL(page.TopAppBar.ClosedDisplayMode, AppBarClosedDisplayMode.Compact);
			});
		}

		[TestMethod]
		[Description("Validates that setting AppBar.ClosedDisplayMode causes the control to be sized appropriately and to be able to be expanded by tapping on the ellipsis button.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		[TestProperty("HasAssociatedMasterFile", "True")]
		[Ignore("Missing VerifyUIElementTreeHelper")]
		public void CanClosedDisplayModesControlLayout()
		{
			//	TestCleanupWrapper cleanup;

			//	auto validationRules = ref new Platform::String(DefaultUIElementTreeValidationRules);

			//	// Set the content first before send Tab() key not to hang for injecting input.
			//	RunOnUIThread([&]()

			//{
			//		TestServices::WindowHelper->WindowContent = ref new xaml_controls::Grid();
			//	});
			//	TestServices::WindowHelper->WaitForIdle();

			//	// AppBars give programmatic focus to their first button, which shows up
			//	// in the FocusState property on the button in the visual tree dump.
			//	// Programmatic focus sets FocusState to whatever was the last
			//	// method of user input, which means that, without something to make it
			//	// deterministic, FocusState will be whatever the previous test did
			//	// in terms of user input.  This leads to inconsistent FocusState values.
			//	// To fix that issue, we inject keyboard input here to ensure that
			//	// the last method of user input is always keyboard, so we'll have a
			//	// consistent value for FocusState in the visual tree dump.
			//	TestServices::KeyboardHelper->Tab();

			//	TestServices::WindowHelper->SetWindowSizeOverride(wf::Size(400, 300));

			//	xaml_controls::Page ^ page = SetupClosedDisplayModeTestEnvironment(true /* setClosedDisplayModeValues */); ;

			//	auto topOpenedEvent = std::make_shared<Event>();
			//	auto bottomOpenedEvent = std::make_shared<Event>();

			//	auto topOpenedRegistration = CreateSafeEventRegistration(xaml_controls::AppBar, Opened);
			//	auto bottomOpenedRegistration = CreateSafeEventRegistration(xaml_controls::AppBar, Opened);

			//	TestServices::Utilities->VerifyUIElementTreeWithRulesInline("Closed", validationRules);

			//	RunOnUIThread([&]()

			//{
			//		topOpenedRegistration.Attach(page->TopAppBar, [&](){ topOpenedEvent->Set(); });
			//		bottomOpenedRegistration.Attach(page->BottomAppBar, [&](){ bottomOpenedEvent->Set(); });

			//		page->TopAppBar->IsOpen = true;
			//		page->TopAppBar->LightDismissOverlayMode = xaml_controls::LightDismissOverlayMode::Off;

			//		page->BottomAppBar->IsOpen = true;
			//		page->BottomAppBar->LightDismissOverlayMode = xaml_controls::LightDismissOverlayMode::Off;
			//	});

			//	topOpenedEvent->WaitForDefault();
			//	bottomOpenedEvent->WaitForDefault();

			//	TestServices::WindowHelper->WaitForIdle();
			//	TestServices::Utilities->VerifyUIElementTreeWithRulesInline("Open", validationRules);
		}

		[TestMethod]
		[Description("Validates that setting ClosedDisplayMode to Hidden removes the AppBar from the visual tree.")]
				public async Task CanHideAppBarWithHiddenClosedDisplayMode()
		{
			TestCleanupWrapper cleanup;

			var page = await SetupClosedDisplayModeTestEnvironment(setClosedDisplayModeValues: true);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Set TopAppBar.ClosedDisplayMode to Hidden.");
				page.TopAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(page.TopAppBar.ActualHeight, 0);

				LOG_OUTPUT("Now set TopAppBar.ClosedDisplayMode back to Minimal.");
				page.TopAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => VERIFY_IS_GREATER_THAN(page.TopAppBar.ActualHeight, 0));
		}

		[TestMethod]
		[Description("Validates that setting ClosedDisplayMode to Minimal or Compact changes the AppBar's height.")]
		public async Task CanChangeAppBarHeightWithClosedDisplayMode()
		{
			TestCleanupWrapper cleanup;

			double originalTopAppBarHeight = 0;
			AppBar appBar = null;

			await RunOnUIThread(() =>
			{
				var root = (FrameworkElement)XamlReader.Load(@"
					<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""   xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
						<AppBar x:Name=""appBar"" ClosedDisplayMode=""Minimal"" Width=""320""/>
					</Grid>
				");

				appBar = (AppBar)root.FindName("appBar");

				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				originalTopAppBarHeight = appBar.ActualHeight;

				LOG_OUTPUT("Set ClosedDisplayMode to Compact - its actual height should become larger.");
				appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_IS_GREATER_THAN(appBar.ActualHeight, originalTopAppBarHeight);

				LOG_OUTPUT("Now set ClosedDisplayMode back to Minimal - its actual height should return to its original height.");
				appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => VERIFY_ARE_EQUAL(appBar.ActualHeight, originalTopAppBarHeight));
		}

		[TestMethod]
		[Description("Validates the focus stays on the current focused element and does not shift to the AppBar when a closed AppBar is added dynamically.")]
				public async Task ValidateFocusShiftWhenClosedAppBarIsAdded()
		{
			TestCleanupWrapper cleanup;

			var page = await SetupFocusShiftTestPage();
			var appBar = await CreateFocusShiftTestAppBar(isOpen: false);
			Button pageButton = null;

			// Start the focus on the button on the page.
			await RunOnUIThread(() =>
			{
				pageButton = (Button)TreeHelper.GetVisualChildByName(page, "PageButton");
				pageButton.Focus(FocusState.Keyboard);
			});
			await WindowHelper.WaitForIdle();

			// Add Closed Compact AppBar dynamically.
			await RunOnUIThread(() =>
			{
				page.BottomAppBar = appBar;
			});
			await WindowHelper.WaitForIdle();

			// Verify that the focus stayed on the pageButton.
			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(WindowHelper.XamlRoot).Equals(pageButton));
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Description("Validates the focus shift between last focused element and an opened AppBar when it is added dynamically.")]
				public async Task ValidateFocusShiftWhenOpenedAppBarIsAdded()
		{
			TestCleanupWrapper cleanup;

			var page = await SetupFocusShiftTestPage();
			var appBar = await CreateFocusShiftTestAppBar(isOpen: true);
			Button pageButton = null;
			AppBarButton appBarButton = null;

			// Start the focus on the button on the page.
			await RunOnUIThread(() =>
			{
				pageButton = (Button)TreeHelper.GetVisualChildByName(page, "PageButton");
				pageButton.Focus(FocusState.Keyboard);
			});
			await WindowHelper.WaitForIdle();

			// Add Closed Compact AppBar dynamically.
			await RunOnUIThread(() =>
			{
				page.BottomAppBar = appBar;
			});
			await WindowHelper.WaitForIdle();

			// Verify that the focus stayed on the pageButton.
			await RunOnUIThread(() =>
			{
				appBarButton = (AppBarButton)TreeHelper.GetVisualChildByName(page.BottomAppBar, "AppBarButton");
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(WindowHelper.XamlRoot).Equals(appBarButton));
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Description("Validates the focus shift between last focused element and the appBarButton of a closed AppBar when it is opened/closed.")]
				public async Task ValidateFocusShiftWhenClosedUnfocusedAppBarIsOpenedAndClosed()
		{
			TestCleanupWrapper cleanup;

			var page = await SetupFocusShiftTestPage();
			var appBar = await CreateFocusShiftTestAppBar(isOpen: false);
			Button pageButton = null;
			AppBarButton appBarButton = null;

			// Start the focus on the button on the page.
			await RunOnUIThread(() =>
			{
				pageButton = (Button)TreeHelper.GetVisualChildByName(page, "PageButton");
				pageButton.Focus(FocusState.Keyboard);
			});
			await WindowHelper.WaitForIdle();

			// Add Closed Compact AppBar dynamically.
			await RunOnUIThread(() =>
			{
				page.BottomAppBar = appBar;
			});
			await WindowHelper.WaitForIdle();

			// Then, open the AppBar programmatically.
			await RunOnUIThread(() =>
			{
				appBar.IsOpen = true;
			});
			await WindowHelper.WaitForIdle();

			// Verify that the focus moved to the appBarButton (first focusable element of the AppBar) since the AppBar was opened.
			// Then, close the AppBar programmatically.
			await RunOnUIThread(() =>
			{
				appBarButton = (AppBarButton)TreeHelper.GetVisualChildByName(page.BottomAppBar, "AppBarButton");
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(WindowHelper.XamlRoot).Equals(appBarButton));
				appBar.IsOpen = false;
			});
			await WindowHelper.WaitForIdle();

			// Verify that the focus moved back to the pageButton (previously focused element) since the AppBar was closed.
			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(WindowHelper.XamlRoot).Equals(pageButton));
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Description("Validates the focus stays on the AppBar if it was was already there before the AppBar was opened/closed.")]
				public async Task ValidateFocusShiftWhenClosedFocusedAppBarIsOpenedAndClosed()
		{
			TestCleanupWrapper cleanup;

			var page = await SetupFocusShiftTestPage();
			var appBar = await CreateFocusShiftTestAppBar(isOpen: false);
			Button expandButton = null;

			// Add Closed Compact AppBar dynamically.
			await RunOnUIThread(() =>
			{
				page.BottomAppBar = appBar;
			});
			await WindowHelper.WaitForIdle();

			// Start the focus on the expandButton of the AppBar.
			await RunOnUIThread(() =>
			{
				expandButton = (Button)TreeHelper.GetVisualChildByName(page, "ExpandButton");
				expandButton.Focus(FocusState.Keyboard);
			});
			await WindowHelper.WaitForIdle();

			// Then, open the AppBar programmatically.
			await RunOnUIThread(() =>
			{
				appBar.IsOpen = true;
			});
			await WindowHelper.WaitForIdle();

			// Verify that the focus stays on the expandButton since the AppBar already had focus.
			// Then, close the AppBar programmatically.
			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(WindowHelper.XamlRoot).Equals(expandButton));
				appBar.IsOpen = false;
			});
			await WindowHelper.WaitForIdle();

			// Verify that the focus is still on the expandButton after the AppBar closed.
			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(WindowHelper.XamlRoot).Equals(expandButton));
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Description("Validates that resizing the AppBar after opening and closing causes its width to properly get updated.")]
				public async Task CanResizeAppBarAfterOpeningAndClosing()
		{
			TestCleanupWrapper cleanup;

			var page = await SetupClosedDisplayModeTestEnvironment(setClosedDisplayModeValues: true);
			Button expandButton = null;
			Point originalExpandButtonPosition = new Point();

			await RunOnUIThread(() =>
			{
				expandButton = (Button)TreeHelper.GetVisualChildByName(page.BottomAppBar, "ExpandButton");

				page.BottomAppBar.Margin = new Thickness(0, 0, 300, 0);
				page.BottomAppBar.IsOpen = true;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				originalExpandButtonPosition = expandButton.TransformToVisual(null).TransformPoint(new Point(0, 0));
				page.BottomAppBar.IsOpen = false;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				page.BottomAppBar.Margin = new Thickness(0, 0, 0, 0);
				page.BottomAppBar.IsOpen = true;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var newExpandButtonPosition = expandButton.TransformToVisual(null).TransformPoint(new Point(0, 0));
				VERIFY_IS_GREATER_THAN(newExpandButtonPosition.X, originalExpandButtonPosition.X);
			});
		}

		[TestMethod]
		[Description("Validates that AppBars can be placed with other items in the visual tree.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		[TestProperty("HasAssociatedMasterFile", "True")]
		[Ignore("ValidateUIElementTree not implemented")]
		public void ValidateInlineAppBars()
		{
			//ControlHelper::ValidateUIElementTree(
			//   wf::Size(400, 600),
			//   1.f,

			//   []()

			//	{
			//		{
			//			// Inject a tab to ensure a consistent FocusState when we create
			//			// the visual tree dump.
			//			KeyboardInjectionIgnoreEventWaitOverride keyboardEventsOverride;
			//			TestServices::KeyboardHelper->Tab();
			//		}
			//		xaml_controls::Grid ^ rootGrid = nullptr;

			//		RunOnUIThread([&]()

			//		{
			//			rootGrid = safe_cast < xaml_controls::Grid ^> (xaml_markup::XamlReader::Load(
			//				LR"(<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

			//						< Grid.RowDefinitions >

			//							< RowDefinition />

			//							< RowDefinition />

			//							< RowDefinition />

			//							< RowDefinition />

			//							< RowDefinition />

			//							< RowDefinition />

			//						</ Grid.RowDefinitions >

			//						< AppBar Grid.Row = "0" IsOpen = "False" IsSticky = "True" ClosedDisplayMode = "Compact" LightDismissOverlayMode = "Off" />

			//						< AppBar Grid.Row = "1" IsOpen = "True" IsSticky = "True" ClosedDisplayMode = "Compact" LightDismissOverlayMode = "Off" />

			//						< AppBar Grid.Row = "2" IsOpen = "False" IsSticky = "True" ClosedDisplayMode = "Minimal" LightDismissOverlayMode = "Off" />

			//						< AppBar Grid.Row = "3" IsOpen = "True" IsSticky = "True" ClosedDisplayMode = "Minimal" LightDismissOverlayMode = "Off" />

			//						< AppBar Grid.Row = "4" IsOpen = "False" IsSticky = "True" ClosedDisplayMode = "Hidden" LightDismissOverlayMode = "Off" />

			//						< AppBar Grid.Row = "5" IsOpen = "True" IsSticky = "True" ClosedDisplayMode = "Hidden" LightDismissOverlayMode = "Off" />

			//					</ Grid >)"));


			//			TestServices::WindowHelper->WindowContent = rootGrid;
			//		});
			//		TestServices::WindowHelper->WaitForIdle();

			//		return rootGrid;
			//	},
			//	nullptr /*cleanupFunc*/,
			//	true /*disableHitTestingOnRoot*/,
			//	true /*ignorePopups*/
			//);
		}

		[TestMethod]
		[Description("Validates that AppBars can be placed inside a root (example: Popup, or MediaElement) which requires the LTE to be parented explicitly.")]
		[Ignore("LTE not implemented")]
		public async Task ValidateAppBarWithParentedLTE()
		{
			// ParentedLTE here refers to the fact that an AppBar placed inside a PopupRoot or
			// a MediaElementRoot will have it's LTE explicitly parented. This scenario was not
			// being covered by existing tests and we needed to cover this, even though AppBar
			// within a PopupRoot is a rare actual scenario, to safeguard against breaking
			// CommandBar that is used by MediaElement.
			TestCleanupWrapper cleanup;

			AppBar appBar = null;
			Flyout flyout = null;

			var target = await FlyoutHelper.CreateTarget(
				width: 100,
				height: 100,
				ThicknessHelper.FromUniformLength(100),
				HorizontalAlignment.Center,
				VerticalAlignment.Center);

			await RunOnUIThread(() =>
			{
				var rootPanel = new Grid();
				rootPanel.Children.Add(target);

				appBar = (AppBar)XamlReader.Load(@"
					<AppBar  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" Width=""200"" Height=""80"">
                        <AppBarButton Label=""AppBarButton""/>
                    </AppBar>
				");

				flyout = new Flyout();
				flyout.Content = appBar;

				SetWindowContent(rootPanel);
			});
			await WindowHelper.WaitForIdle();

			// Validate that we can open and close this AppBar placed inside the Popup.
			var openedEvent = new Event();
			var closedEvent = new Event();

			var openedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var closedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");

			AttachOpenedAndClosedHandlers(appBar, openedEvent, openedRegistration, closedEvent, closedRegistration);

			FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

			await RunOnUIThread(() => appBar.IsOpen = true);
			await openedEvent.WaitForDefault();

			await RunOnUIThread(() => appBar.IsOpen = false);
			await closedEvent.WaitForDefault();

			FlyoutHelper.HideFlyout(flyout);
		}

		[TestMethod]
		[Description("Validates that setting AppBar.ClosedDisplayMode causes the tab experience to be different when closed depending on the visible items that exist.")]
		[TestProperty("Hosting:Mode", "UAP")]
				public async Task CanClosedDisplayModesAffectTabbingWhenClosed()
		{
			TestCleanupWrapper cleanup;
			Page rootPage = null;
			Button button = null;

			var loadedEvent = new Event();

			var loadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");
			var pageGotFocusRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("GotFocus");
			var topAppBarGotFocusRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("GotFocus");
			var bottomAppBarGotFocusRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("GotFocus");

			rootPage = (Page)XamlReader.Load(@"
				<Page
					xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
					xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
					<Page.TopAppBar>
						<AppBar  ClosedDisplayMode=""Minimal"">
							<StackPanel Orientation=""Horizontal"">
								<AppBarButton x:Name=""FirstAppBarButton"" Icon=""ReShare"" Label=""Share"" Tag=""AB1""/>
								<AppBarSeparator />
								<AppBarToggleButton Icon=""Favorite"" Label=""Favorite"" Tag=""AB2""/>
								<AppBarButton Icon=""Edit"" Label=""Edit"" Tag=""AB3""/>
							</StackPanel>
						</AppBar>
					</Page.TopAppBar>

					<Page.BottomAppBar>
						<AppBar  ClosedDisplayMode=""Compact"">
							<StackPanel Orientation=""Horizontal"">
								<AppBarButton Icon=""ReShare"" Label=""Share"" Tag=""AB4""/>
								<AppBarSeparator />
								<AppBarToggleButton Icon=""Favorite"" Label=""Favorite"" Tag=""AB5""/>
								<AppBarButton Icon=""Edit"" Label=""Edit"" Tag=""AB6""/>
							</StackPanel>
						</AppBar>
					</Page.BottomAppBar>

					<Grid>
						<Button x:Name=""ExternalButton"" Tag=""B"" Content=""Button outside AppBar"" VerticalAlignment=""Center"" />
					</Grid>
				</Page>
			");
			loadedRegistration.Attach(rootPage, (s, e) => loadedEvent.Set());

			string focusSequence = "";
			string expectedFocusSequence = "[AB1][AB2][AB3][TEB][B][AB4][AB5][AB6][BEB][AB1]";

			await RunOnUIThread(() =>
			{
				button = (Button)TreeHelper.GetVisualChildByName(rootPage, "ExternalButton");

				RoutedEventHandler gotFocusHandler = (s, e) => focusSequence = "[" + ((FrameworkElement)e.OriginalSource).Tag + "]";

				pageGotFocusRegistration.Attach(rootPage, gotFocusHandler);
				topAppBarGotFocusRegistration.Attach(rootPage.TopAppBar, gotFocusHandler);
				bottomAppBarGotFocusRegistration.Attach(rootPage.BottomAppBar, gotFocusHandler);

				SetWindowContent(rootPage);
			});
			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				Button topExpandButton = (Button)TreeHelper.GetVisualChildByName(rootPage.TopAppBar, "ExpandButton");
				Button bottomExpandButton = (Button)TreeHelper.GetVisualChildByName(rootPage.BottomAppBar, "ExpandButton");

				topExpandButton.Tag = "TEB";
				bottomExpandButton.Tag = "BEB";
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => button.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			focusSequence = "";

			LOG_OUTPUT("Open both app bars, which should now enable us to tab into the top app bar that was previously minimized.");

			await RunOnUIThread(() =>
			{
				rootPage.TopAppBar.IsOpen = true;
				rootPage.BottomAppBar.IsOpen = true;
			});
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Tab nine times, which should move focus to the first AppBarButton in the TopAppBar again.");

			for (int i = 0; i < 9; ++i)
			{
				await SimulateTabAsync(rootPage);
				await WindowHelper.WaitForIdle();
			}

			LOG_OUTPUT($"Expected focus sequence: {expectedFocusSequence}");
			LOG_OUTPUT($"Actual focus sequence: {focusSequence}");
			VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);
		}

		[TestMethod]
		[Description("Validates that setting AppBar.ClosedDisplayMode to Hidden and IsSticky to false on all AppBars causes the WinBlue tabbing experience to occur.")]
		[TestProperty("Hosting:Mode", "UAP")]
				public async Task ValidateWinBlueTabbingIsPreserved()
		{
			TestCleanupWrapper cleanup;
			Page rootPage = null;
			Button button = null;

			var loadedEvent = new Event();

			var loadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");
			var pageGotFocusRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("GotFocus");
			var topAppBarGotFocusRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("GotFocus");
			var bottomAppBarGotFocusRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("GotFocus");

			rootPage = (Page)XamlReader.Load(@"
				<Page
					xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
					xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
					<Page.TopAppBar>
						<AppBar  ClosedDisplayMode=""Minimal"">
							<StackPanel Orientation=""Horizontal"">
								<AppBarButton x:Name=""FirstAppBarButton"" Icon=""ReShare"" Label=""Share"" Tag=""AB1""/>
								<AppBarSeparator />
								<AppBarToggleButton Icon=""Favorite"" Label=""Favorite"" Tag=""AB2""/>
								<AppBarButton Icon=""Edit"" Label=""Edit"" Tag=""AB3""/>
							</StackPanel>
						</AppBar>
					</Page.TopAppBar>

					<Page.BottomAppBar>
						<AppBar  ClosedDisplayMode=""Compact"">
							<StackPanel Orientation=""Horizontal"">
								<AppBarButton Icon=""ReShare"" Label=""Share"" Tag=""AB4""/>
								<AppBarSeparator />
								<AppBarToggleButton Icon=""Favorite"" Label=""Favorite"" Tag=""AB5""/>
								<AppBarButton Icon=""Edit"" Label=""Edit"" Tag=""AB6""/>
							</StackPanel>
						</AppBar>
					</Page.BottomAppBar>

					<Grid>
						<Button x:Name=""ExternalButton"" Tag=""B"" Content=""Button outside AppBar"" VerticalAlignment=""Center"" />
					</Grid>
				</Page>
			");
			loadedRegistration.Attach(rootPage, (s, e) => loadedEvent.Set());

			string focusSequence = "";
			string expectedFocusSequence = "[AB2][AB3][TEB][AB4][AB5][AB6][BEB][AB1]";

			await RunOnUIThread(() =>
			{
				button = (Button)TreeHelper.GetVisualChildByName(rootPage, "ExternalButton");

				RoutedEventHandler gotFocusHandler = (s, e) => focusSequence = "[" + ((FrameworkElement)e.OriginalSource).Tag + "]";

				pageGotFocusRegistration.Attach(rootPage, gotFocusHandler);
				topAppBarGotFocusRegistration.Attach(rootPage.TopAppBar, gotFocusHandler);
				bottomAppBarGotFocusRegistration.Attach(rootPage.BottomAppBar, gotFocusHandler);

				SetWindowContent(rootPage);
			});
			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				Button topExpandButton = (Button)TreeHelper.GetVisualChildByName(rootPage.TopAppBar, "ExpandButton");
				Button bottomExpandButton = (Button)TreeHelper.GetVisualChildByName(rootPage.BottomAppBar, "ExpandButton");

				topExpandButton.Tag = "TEB";
				bottomExpandButton.Tag = "BEB";
				rootPage.TopAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;
				rootPage.TopAppBar.IsSticky = false;
				rootPage.TopAppBar.IsOpen = true;
				rootPage.BottomAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;
				rootPage.BottomAppBar.IsSticky = false;
				rootPage.BottomAppBar.IsOpen = true;
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => button.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			focusSequence = "";

			LOG_OUTPUT("Tab eight times, which should move focus through the AppBars without ever giving the external button focus.");

			for (int i = 0; i < 8; ++i)
			{
				await SimulateTabAsync(rootPage);
				await WindowHelper.WaitForIdle();
			}

			LOG_OUTPUT($"Expected focus sequence: {expectedFocusSequence}");
			LOG_OUTPUT($"Actual focus sequence: {focusSequence}");
			VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);
		}

		[TestMethod]
		[Description("Validates that AppBars can be closed by pressing the Back button.")]
				public async Task CanCloseAppBarUsingBackButton()
		{
			TestCleanupWrapper cleanup;

			var page = await SetupTopBottomInlineAppBarsPage();

			await CanCloseAppBarHelper((expectedHandledValue, appBar) =>
			{
				bool backButtonPressHandled = false;
				TestServices.Utilities.InjectBackButtonPress(ref backButtonPressHandled);
				VERIFY_ARE_EQUAL(backButtonPressHandled, expectedHandledValue);
			},
			page);
		}

		[TestMethod]
		[Description("Validates that AppBars can be closed by pressing the B button when using a gamepad.")]
				public async Task CanCloseAppBarUsingGamepadB()
		{
			await CanCloseAppBarUsingDevice(InputDevice.Gamepad);
		}

		[TestMethod]
		[Description("Validates that AppBars can be closed by pressing the Escape keyboard key.")]
				public async Task CanCloseAppBarUsingEsc()
		{
			await CanCloseAppBarUsingDevice(InputDevice.Keyboard);
		}

		[TestMethod]
		[Description("Validates that the AppBar unregisters back button event handling when it leaves the tree.")]
		public async Task DoesAppBarUnregisterBackButtonHandlerWhenNotInTree()
		{
			TestCleanupWrapper cleanup;

			AppBar appBar = null;
			Page page = null;

			var hasUnloadedEvent = new Event();
			var unloadedRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("Unloaded");

			await RunOnUIThread(() =>
			{
				appBar = new AppBar();
				appBar.IsOpen = true;
				unloadedRegistration.Attach(appBar, (s, e) => hasUnloadedEvent.Set());

				page = WindowHelper.SetupSimulatedAppPage();
				SetPageContent(appBar, page);
			});
			await WindowHelper.WaitForIdle();

			// Now remove the appBar from the tree.
			await RunOnUIThread(() => SetPageContent(null, page));
			await hasUnloadedEvent.WaitForDefault();

			//UNO TODO InjectBackButtonPress not implemented
			//bool wasBackButtonPressHandled = false;
			//TestServices::Utilities->InjectBackButtonPress(&wasBackButtonPressHandled);
			//VERIFY_IS_FALSE(wasBackButtonPressHandled);
		}

		[TestMethod]
		[Description("When the AppBar is Disabled, the expand button should be greyed out")]
				public async Task ValidateExpandButtonVisualInDisabledState()
		{
			TestCleanupWrapper cleanup;

			AppBar appBar = null;
			Page page = null;
			FontIcon ellipsisIcon = null; //Ellipsis FontIcon in the ExpandButton.
			Brush expectedBrushEnabled = null; //The expected Brush used for the Fill of the ellipsis when enabled
			Brush expectedBrushDisabled = null; //The expected Brush used for the Fill of the ellipsis when disabled.

			await RunOnUIThread(() =>
			{
				appBar = new AppBar();
				page = WindowHelper.SetupSimulatedAppPage();
				page.BottomAppBar = appBar;

				expectedBrushEnabled = (Brush)Application.Current.Resources.Lookup("SystemControlForegroundBaseHighBrush");
				expectedBrushDisabled = (Brush)Application.Current.Resources.Lookup("SystemControlDisabledBaseMediumLowBrush");
				VERIFY_IS_NOT_NULL(expectedBrushEnabled);
				VERIFY_IS_NOT_NULL(expectedBrushDisabled);
			});
			await WindowHelper.WaitForIdle();

			//Verify that the ellipsis is the correct color in the Enabled AppBar:
			await RunOnUIThread(() =>
			{
				ellipsisIcon = (FontIcon)TreeHelper.GetVisualChildByName(appBar, "EllipsisIcon");

				VERIFY_ARE_EQUAL(expectedBrushEnabled, ellipsisIcon.Foreground);

				appBar.IsEnabled = false;
			});
			await WindowHelper.WaitForIdle();

			//Verify that the ellipsis is the correct color in the Disabled AppBar:
			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(expectedBrushDisabled, ellipsisIcon.Foreground);

				page.BottomAppBar = null;
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Description("Validates that tabbing will not focus the AppBar when it's ClosedDisplayMode=Hidden and it's closed.")]
		public async Task CanNotTabIntoWhenClosedAndHidden()
		{
			TestCleanupWrapper cleanup;

			Button button = null;

			var appBarGotFocusRegistration = CreateSafeEventRegistration<AppBar, RoutedEventHandler>("GotFocus");
			bool didAppBarGetFocus = false;

			await RunOnUIThread(() =>
			{
				var appBar = new AppBar();
				appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;

				// Add some content to make sure it doesn't get focus either.
				appBar.Content = new AppBarButton();

				appBarGotFocusRegistration.Attach(appBar, (s, e) => didAppBarGetFocus = true);

				button = new Button();
				button.Content = "button";

				var root = new Grid();
				root.Children.Add(button);
				root.Children.Add(appBar);

				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => button.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();

			await SimulateTabAsync(WindowHelper.WindowContent);
			await WindowHelper.WaitForIdle();

			VERIFY_IS_FALSE(didAppBarGetFocus);
		}

		[TestMethod]
		[Description("Validates the ActualWidth & Height of AppBar in various configurations.")]
		[RequiresFullWindow]
		public async Task ValidateFootprint()
		{
			TestCleanupWrapper cleanup;

			double expectedAppBarWidth = 400;

			double expectedAppBarCompactClosedHeight = 40;
			double expectedAppBarCompactOpenHeight = 40;

			double expectedAppBarMinimalClosedHeight = 24;
			double expectedAppBarMinimalOpenHeight = 24;

			double expectedAppBarHiddenClosedHeight = 0;
			double expectedAppBarHiddenOpenHeight = 0;

			AppBar appBarCompactClosed = null;
			AppBar appBarCompactOpen = null;
			AppBar appBarMinimalClosed = null;
			AppBar appBarMinimalOpen = null;
			AppBar appBarHiddenClosed = null;
			AppBar appBarHiddenOpen = null;

			WindowHelper.SetWindowSizeOverride(new Size(400, 600));

			await RunOnUIThread(() =>
			{
				var rootPanel = (StackPanel)XamlReader.Load(@"
					<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
								xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""

								Width=""400""
								Height=""600"">
                        <AppBar x:Name=""appBarCompactClosed"" IsOpen=""False"" ClosedDisplayMode=""Compact""/>
                        <AppBar x:Name=""appBarCompactOpen"" IsOpen=""True"" ClosedDisplayMode=""Compact""/>
                        <AppBar x:Name=""appBarMinimalClosed"" IsOpen=""False"" ClosedDisplayMode=""Minimal""/>
                        <AppBar x:Name=""appBarMinimalOpen"" IsOpen=""True"" ClosedDisplayMode=""Minimal""/>
                        <AppBar x:Name=""appBarHiddenClosed"" IsOpen=""False"" ClosedDisplayMode=""Hidden""/>
                        <AppBar x:Name=""appBarHiddenOpen"" IsOpen=""True"" ClosedDisplayMode=""Hidden""/>
                    </StackPanel>
				");

				appBarCompactClosed = (AppBar)rootPanel.FindName("appBarCompactClosed");
				appBarCompactOpen = (AppBar)rootPanel.FindName("appBarCompactOpen");
				appBarMinimalClosed = (AppBar)rootPanel.FindName("appBarMinimalClosed");
				appBarMinimalOpen = (AppBar)rootPanel.FindName("appBarMinimalOpen");
				appBarHiddenClosed = (AppBar)rootPanel.FindName("appBarHiddenClosed");
				appBarHiddenOpen = (AppBar)rootPanel.FindName("appBarHiddenOpen");

				SetWindowContent(rootPanel);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(appBarCompactClosed.ActualWidth, expectedAppBarWidth);
				VERIFY_ARE_EQUAL(appBarCompactClosed.ActualHeight, expectedAppBarCompactClosedHeight);

				VERIFY_ARE_EQUAL(appBarCompactOpen.ActualWidth, expectedAppBarWidth);
				VERIFY_ARE_EQUAL(appBarCompactOpen.ActualHeight, expectedAppBarCompactOpenHeight);

				VERIFY_ARE_EQUAL(appBarMinimalClosed.ActualWidth, expectedAppBarWidth);
				VERIFY_ARE_EQUAL(appBarMinimalClosed.ActualHeight, expectedAppBarMinimalClosedHeight);

				VERIFY_ARE_EQUAL(appBarMinimalOpen.ActualWidth, expectedAppBarWidth);
				VERIFY_ARE_EQUAL(appBarMinimalOpen.ActualHeight, expectedAppBarMinimalOpenHeight);

				VERIFY_ARE_EQUAL(appBarHiddenClosed.ActualWidth, expectedAppBarWidth);
				VERIFY_ARE_EQUAL(appBarHiddenClosed.ActualHeight, expectedAppBarHiddenClosedHeight);

				VERIFY_ARE_EQUAL(appBarHiddenOpen.ActualWidth, expectedAppBarWidth);
				VERIFY_ARE_EQUAL(appBarHiddenOpen.ActualHeight, expectedAppBarHiddenOpenHeight);
			});
		}

		[TestMethod]
		[Description("Validates the behavior of the LightDismissOverlayMode property.")]
		public async Task ValidateLightDismissOverlayMode()
		{
			TestCleanupWrapper cleanup;

			AppBar appBar = null;

			await RunOnUIThread(() =>
			{
				appBar = new AppBar();
				appBar.IsOpen = true;

				SetWindowContent(appBar);
			});
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Validate that the default is Auto and the AppBar's overlay is not visible (or visible if on Xbox)");
			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(appBar.LightDismissOverlayMode, LightDismissOverlayMode.Auto);
				ValidateVisibilityOfOverlayElement(appBar, TestServices.Utilities.IsXBox);
			});

			LOG_OUTPUT("Validate that when set to On the AppBar's overlay is visible.");
			await RunOnUIThread(() =>
			{
				appBar.LightDismissOverlayMode = LightDismissOverlayMode.On;
				ValidateVisibilityOfOverlayElement(appBar, true);
			});

			LOG_OUTPUT("Validate that when set to Off the AppBar's overlay is not visible.");
			await RunOnUIThread(() =>
			{
				appBar.LightDismissOverlayMode = LightDismissOverlayMode.Off;
				ValidateVisibilityOfOverlayElement(appBar, false);
			});
		}

		[TestMethod]
		[Description("Validates the behavior of the LightDismissOverlayMode property when set on Top/Bottom app bars.")]
		[Ignore("TestServices.Utilities.GetPopupOverlayElement not implemented")]
		public async Task ValidateLightDismissOverlayModeForTopBottomAppBars()
		{
			TestCleanupWrapper cleanup;

			Page page = null;

			await RunOnUIThread(() =>
			{
				var topAppBar = new AppBar();
				var bottomAppBar = new AppBar();

				page = WindowHelper.SetupSimulatedAppPage();
				page.TopAppBar = topAppBar;
				page.BottomAppBar = bottomAppBar;

				SetWindowContent(page);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// Make sure we don't see the "See more" tooltip, which fails some
				// assertions in the test where only 1 popup should be open at a time.
				var topExpandButton = (Button)TreeHelper.GetVisualChildByName(page.TopAppBar, "ExpandButton");
				ToolTipService.SetToolTip(topExpandButton, null);
			});

			// Top and Bottom AppBars are hosted in the ApplicationBarService which
			// uses a popup to display them above all other content.  Overlays
			// for top and bottom AppBars end up configuring this popup's overlay
			// so this test is just going to check that that is configured as
			// expected.
			// Since there is one overlay shared between the 2 app bars, the
			// expected mode represents the most-visible mode after examining
			// the setting on both.
			for (int topMode = 0; topMode < 3; ++topMode)
			{
				for (int bottomMode = 0; bottomMode < 3; ++bottomMode)
				{
					var expectedMode = LightDismissOverlayMode.Off;

					var topOverlayMode = (LightDismissOverlayMode)topMode;
					var bottomOverlayMode = (LightDismissOverlayMode)bottomMode;

					// Determine the expected mode.
					if (topOverlayMode == LightDismissOverlayMode.On ||
						bottomOverlayMode == LightDismissOverlayMode.On)
					{
						expectedMode = LightDismissOverlayMode.On;
					}
					else if (topOverlayMode == LightDismissOverlayMode.Auto ||
						bottomOverlayMode == LightDismissOverlayMode.Auto)
					{
						expectedMode = TestServices.Utilities.IsXBox ? LightDismissOverlayMode.On : LightDismissOverlayMode.Off;
					}

					await RunOnUIThread(() =>
					{
						page.TopAppBar.LightDismissOverlayMode = (LightDismissOverlayMode)topMode;
						page.TopAppBar.IsOpen = true;

						page.BottomAppBar.LightDismissOverlayMode = (LightDismissOverlayMode)bottomMode;
						page.BottomAppBar.IsOpen = true;
					});
					await WindowHelper.WaitForIdle();

					await RunOnUIThread(() =>
					{
						var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(page.XamlRoot);
						if (popups.Count != 1)
						{
							throw new InvalidOperationException("Expected exactly one open Popup.");
						}

						var appBarServicePopup = popups[0];
						VERIFY_ARE_EQUAL(appBarServicePopup.LightDismissOverlayMode, expectedMode);

						if (expectedMode == LightDismissOverlayMode.On)
						{
							var overlayElement = TestServices.Utilities.GetPopupOverlayElement(appBarServicePopup);
							VERIFY_IS_NOT_NULL(overlayElement);
						}
					});

					// The overlay mode only gets refreshed when you toggle the app bars open, so close it
					// in preparation for the next iteration.
					await RunOnUIThread(() =>
					{
						page.TopAppBar.IsOpen = false;
						page.BottomAppBar.IsOpen = false;
					});
					await WindowHelper.WaitForIdle();
				}
			}
		}

		[TestMethod]
		[Description("Validates that setting LightDismissOverlayMode to Auto on Xbox causes the overlay to be visible.")]
		[Ignore]
		public async Task IsAutoLightDismissOverlayModeVisibleOnXbox()
		{
			TestCleanupWrapper cleanup;

			AppBar appBar = null;

			await RunOnUIThread(() =>
			{
				appBar = new AppBar();
				appBar.LightDismissOverlayMode = LightDismissOverlayMode.Auto;
				appBar.IsOpen = true;

				SetWindowContent(appBar);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => ValidateVisibilityOfOverlayElement(appBar, true));
		}

		[TestMethod]
		[Description("Validates that setting LightDismissOverlayMode to Auto on Xbox causes the overlay to be visible for Top/Bottom app bars.")]
		[Ignore]
		public async Task IsAutoLightDismissOverlayModeVisibleForTopBottomAppBarsOnXbox()
		{
			TestCleanupWrapper cleanup;

			Page page = null;

			await RunOnUIThread(() =>
			{
				var topAppBar = new AppBar();
				topAppBar.LightDismissOverlayMode = LightDismissOverlayMode.Auto;
				topAppBar.IsOpen = true;

				var bottomAppBar = new AppBar();
				bottomAppBar.LightDismissOverlayMode = LightDismissOverlayMode.Auto;
				bottomAppBar.IsOpen = true;

				page = WindowHelper.SetupSimulatedAppPage();
				page.TopAppBar = topAppBar;
				page.BottomAppBar = bottomAppBar;

				SetWindowContent(page);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(page.XamlRoot);
				if (popups.Count != 1)
				{
					throw new InvalidOperationException("Expected exactly one open Popup.");
				}

				var appBarServicePopup = popups[0];
				VERIFY_ARE_EQUAL(appBarServicePopup.LightDismissOverlayMode, LightDismissOverlayMode.On);

				page.TopAppBar.IsOpen = false;
				page.BottomAppBar.IsOpen = false;
			});
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Description("Validates DComp tree with an overlay-enabled app bar.")]
		[Ignore]// Not stable between runs; there is a phantom visual that keeps showing up..
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		[TestProperty("HasAssociatedMasterFile", "True")]
		public void ValidateOverlayDCompTree()
		{
			//	TestServices::WindowHelper->SetWindowSizeOverride(wf::Size(400, 400));
			//	WUCRenderingScopeGuard guard(DCompRendering::WUCCompleteSynchronousCompTree, false /*resizeWindow*/);

			//	// Set the content first before injecting the touch input
			//	xaml_controls::Grid ^ grid = nullptr;
			//	RunOnUIThread([&]()

			//{
			//		grid = ref new xaml_controls::Grid();
			//		TestServices::WindowHelper->WindowContent = grid;
			//	});
			//	TestServices::WindowHelper->WaitForIdle();

			//	// Inject the touch input not to set the focus with keyboard state
			//	TestServices::InputHelper->Tap(grid);
			//	TestServices::WindowHelper->WaitForIdle();

			//	auto root = SetupOverlayTreeValidationTest();

			//	LOG_OUTPUT(L"Validate the dark theme of the overlay.");
			//	TestServices::Utilities->VerifyMockDCompOutput(MockDComp::SurfaceComparison::NoComparison, "Dark");

			//	LOG_OUTPUT(L"Validate the light theme of the overlay.");
			//	RunOnUIThread([&]()

			//	{
			//			root->RequestedTheme = xaml::ElementTheme::Light;
			//	});
			//	TestServices::WindowHelper->WaitForIdle();
			//	TestServices::Utilities->VerifyMockDCompOutput(MockDComp::SurfaceComparison::NoComparison, "Light");

			//	LOG_OUTPUT(L"Validate the high-contrast theme of the overlay.");
			//	RunOnUIThread([&]()

			//	{
			//			TestServices::ThemingHelper->HighContrastTheme = HighContrastTheme::Test;
			//	});
			//	TestServices::WindowHelper->WaitForIdle();
			//	TestServices::Utilities->VerifyMockDCompOutput(MockDComp::SurfaceComparison::NoComparison, "HC");
		}

		[TestMethod]
		[Description("Validates UIElement tree with an overlay-enabled app bar.")]
		[TestProperty("TestPass:ExcludeOn", "WindowsCore")]
		[TestProperty("HasAssociatedMasterFile", "True")]
		[Ignore("ValidateUIElementTree not implemented")]
		public void ValidateOverlayUIETree()
		{
			TestCleanupWrapper cleanup;

			ControlHelper.ValidateUIElementTree(new Size(400, 400), 1f, async () => await SetupOverlayTreeValidationTest());
		}

		private async Task<Panel> SetupOverlayTreeValidationTest()
		{
			Grid root = null;
			Button button = null;

			await RunOnUIThread(() =>
			{
				root = (Grid)XamlReader.Load(@"
					<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                            Background=""{ThemeResource SystemControlBackgroundAltHighBrush}"" >
                            <AppBar  IsOpen=""True"" LightDismissOverlayMode=""On"">
                                <AppBarButton x:Name=""button""/>
                            </AppBar>
                        </Grid>
				");

				button = (Button)root.FindName("button");

				SetWindowContent(root);
			});
			await WindowHelper.WaitForIdle();

			// Set focus to the button to prevent focus from defaulting ot the expand button, which
			// can cause the tooltip for "See More" to show up and destabilize these tree validations.
			await RunOnUIThread(() => button.Focus(FocusState.Programmatic));

			return root;
		}

		[TestMethod]
		[Description("Validates that the brush used for the overlay matches the 'AppBarLightDismissOverlayBackground' resource.")]
		public async Task ValidateOverlayBrush()
		{
			TestCleanupWrapper cleanup;

			AppBar appBar = null;

			await RunOnUIThread(() =>
			{
				appBar = new AppBar();
				appBar.LightDismissOverlayMode = LightDismissOverlayMode.On;
				appBar.IsOpen = true;

				SetWindowContent(appBar);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var expectedBrush = (SolidColorBrush)Application.Current.Resources.Lookup("AppBarLightDismissOverlayBackground");

				var overlayElement = GetAppBarOverlayElement(appBar);
				THROW_IF_NULL_WITH_MSG(overlayElement, "An overlay element should exist.");

				var overlayRect = overlayElement as Rectangle;
				THROW_IF_NULL_WITH_MSG(overlayRect, "The overlay element should be a rectangle.");

				var overlayBrush = overlayRect.Fill as SolidColorBrush;
				VERIFY_IS_NOT_NULL(overlayBrush);
				VERIFY_IS_TRUE(overlayBrush.Equals(expectedBrush));
			});
		}

		[TestMethod]
		[Description("Validates that the brush used for the overlay for top/bottom app bars matches the 'AppBarLightDismissOverlayBackground' resource.")]
		[Ignore("GetPopupOverlayElement not implemented.")]
		public async Task ValidateOverlayBrushForTopBottomAppBars()
		{
			TestCleanupWrapper cleanup;

			Page page = null;

			await RunOnUIThread(() =>
			{
				var topAppBar = new AppBar();
				topAppBar.LightDismissOverlayMode = LightDismissOverlayMode.Auto;
				topAppBar.IsOpen = true;

				page = WindowHelper.SetupSimulatedAppPage();
				page.TopAppBar = topAppBar;

				SetWindowContent(page);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var expectedBrush = (SolidColorBrush)Application.Current.Resources.Lookup("AppBarLightDismissOverlayBackground");

				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(page.XamlRoot);
				if (popups.Count != 1)
				{
					throw new InvalidOperationException("Expected exactly one open Popup.");
				}

				var appBarServicePopup = popups[0];
				var overlayElement = TestServices.Utilities.GetPopupOverlayElement(appBarServicePopup);
				THROW_IF_NULL_WITH_MSG(overlayElement, "An overlay element should exist.");

				var overlayRect = overlayElement as Rectangle;
				THROW_IF_NULL_WITH_MSG(overlayRect, "The overlay element should be a rectangle.");

				var overlayBrush = overlayRect.Fill as SolidColorBrush;
				VERIFY_IS_NOT_NULL(overlayBrush);
				VERIFY_IS_TRUE(overlayBrush.Equals(expectedBrush));
			});
		}

		//[TestMethod]
		//[Description("Verifies that AutomationPeer.IsOffScreen returns false for items in an open appbar")]
		//public async Task VerifyAutomationPeerIsOffscreenIsFalseForElementInOpenAppBar()
		//{
		//	TestCleanupWrapper cleanup;

		//	AppBar appBar = null;
		//	AppBarButton appBarButton = null;

		//	await RunOnUIThread(() =>
		//	{
		//		var root = (FrameworkElement)XamlReader.Load(@"
		//			<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
		//                      Background=""LightBlue"" Width=""400"" Height=""400"">
		//                      <StackPanel>
		//                          <AppBar  x:Name=""appBar"">
		//                              <AppBarButton x:Name=""appBarButton"" Icon=""Add"" Label=""Add""/>
		//                          </AppBar>
		//                      </StackPanel>
		//                  </Grid>
		//		");

		//		appBar = (AppBar)root.FindName("appBar");
		//		appBarButton = (AppBarButton)root.FindName("appBarButton");

		//		SetWindowContent(root);
		//	});
		//	await WindowHelper.WaitForIdle();

		//	await RunOnUIThread(() => appBar.IsOpen = true);
		//	await WindowHelper.WaitForIdle();

		//	await RunOnUIThread(() =>
		//	{
		//		var automationPeer = FrameworkElementAutomationPeer.CreatePeerForElement(appBarButton);

		//		VERIFY_IS_FALSE(automationPeer.IsOffscreen());
		//	});

		//	await RunOnUIThread(() => appBar.IsOpen = false);
		//	await WindowHelper.WaitForIdle();
		//}

		private void ValidateVisibilityOfOverlayElement(AppBar appBar, bool expectedIsVisible)
		{
			var overlayElement = GetAppBarOverlayElement(appBar);
			THROW_IF_NULL_WITH_MSG(overlayElement, "An overlay element should exist.");

			var overlayRect = overlayElement as Rectangle;
			THROW_IF_NULL_WITH_MSG(overlayRect, "The overlay element should be a rectangle.");

			var overlayBrush = overlayRect.Fill as SolidColorBrush;
			THROW_IF_NULL_WITH_MSG(overlayBrush, "The overlay element should have a brush.");

			var brushColor = overlayBrush.Color;
			if (expectedIsVisible)
			{
				VERIFY_IS_GREATER_THAN(brushColor.A, 0);
			}
			else
			{
				VERIFY_ARE_EQUAL(brushColor.A, 0);
			}
		}

		private FrameworkElement GetAppBarOverlayElement(AppBar appBar)
		{
			if (appBar.IsOpen == false)
			{
				throw new InvalidOperationException("AppBar should be opened before calling this helper.");
			}

			// Get the layoutRoot element of the app bar.
			var layoutRoot = (Grid)VisualTreeHelper.GetChild(appBar, 0);

			// When open, the overlay element should be the first child under the layout root.
			return (FrameworkElement)layoutRoot.Children[0];
		}

		private async Task CanCloseAppBarUsingDevice(InputDevice device)
		{
			TestCleanupWrapper cleanup;

			var page = await SetupTopBottomInlineAppBarsPage();

			await CanCloseAppBarHelper(async (expectedHandledValue, appbar) =>
			{
				// We want to make sure the the key press gets handled/not handled as expected.
				// We cannot listen to the Page.KeyDown event, because Page.TopAppBar/Page.BottomAppBar is not
				// actually a visual child of the Page (they are hosted in a Popup), so events don't route
				// from the AppBar to the Page.
				// So, we listen to the KeyDown event on the parent of the appbar.

				UIElement parent = null;
				await RunOnUIThread(() => parent = (UIElement)appbar.Parent);

				var pageKeyDownEvent = new Event();
				var pageKeyDownRegistration = CreateSafeEventRegistration<UIElement, KeyEventHandler>("KeyDown");
				pageKeyDownRegistration.Attach(parent, (s, e) =>
				{
					VERIFY_ARE_EQUAL(e.Handled, expectedHandledValue);
					pageKeyDownEvent.Set();
				});

				await CommonInputHelper.Cancel(device);

				await pageKeyDownEvent.WaitForDefault();
			},
			page);
		}

		private async Task CanCloseAppBarHelper(Action<bool, AppBar> closeFunction, Page page)
		{
			AppBar topAppBar = null;
			AppBar bottomAppBar = null;
			AppBar inlineAppBar = null;
			Button expandButton = null;

			await RunOnUIThread(() =>
			{
				topAppBar = page.TopAppBar;
				bottomAppBar = page.BottomAppBar;
				inlineAppBar = (AppBar)((Panel)page.Content).FindName("inlineAppBar");
			});
			await WindowHelper.WaitForIdle();


			var topOpenedEvent = new Event();
			var topClosedEvent = new Event();
			var bottomOpenedEvent = new Event();
			var bottomClosedEvent = new Event();
			var inlineOpenedEvent = new Event();
			var inlineClosedEvent = new Event();

			var topOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var topClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");
			var bottomOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var bottomClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");
			var inlineOpenedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var inlineClosedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");

			AttachOpenedAndClosedHandlers(topAppBar, topOpenedEvent, topOpenedRegistration, topClosedEvent, topClosedRegistration);
			AttachOpenedAndClosedHandlers(bottomAppBar, bottomOpenedEvent, bottomOpenedRegistration, bottomClosedEvent, bottomClosedRegistration);
			AttachOpenedAndClosedHandlers(inlineAppBar, inlineOpenedEvent, inlineOpenedRegistration, inlineClosedEvent, inlineClosedRegistration);

			await RunOnUIThread(() =>
			{
				topAppBar.IsOpen = true;
				bottomAppBar.IsOpen = true;
			});
			await topOpenedEvent.WaitForDefault();
			await bottomOpenedEvent.WaitForDefault();

			LOG_OUTPUT("Close both Top and Bottom AppBars using the 'close function'.");
			closeFunction(true, topAppBar);
			await topClosedEvent.WaitForDefault();
			await bottomClosedEvent.WaitForDefault();

			await RunOnUIThread(() => inlineAppBar.IsOpen = true);
			await inlineOpenedEvent.WaitForDefault();

			LOG_OUTPUT("Close the inline AppBar using the 'close function'.");
			closeFunction(true, inlineAppBar);
			await inlineClosedEvent.WaitForDefault();

			LOG_OUTPUT("After closing AppBars, further calls to 'close function', while focus is on inline AppBar, should not get handled.");
			closeFunction(false, inlineAppBar);

			// Move focus to top AppBar.
			await RunOnUIThread(() =>
			{
				expandButton = (Button)TreeHelper.GetVisualChildByName(page.TopAppBar, "ExpandButton");
				expandButton.Focus(FocusState.Keyboard);
			});
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() => VERIFY_IS_TRUE(FocusManager.GetFocusedElement(WindowHelper.XamlRoot).Equals(expandButton)));
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("After closing AppBars, further calls to 'close function', while focus is on top AppBar, should not get handled.");
			closeFunction(false, topAppBar);
		}

		private async Task<Page> SetupFocusShiftTestPage()
		{
			Page page = null;

			await RunOnUIThread(() =>
			{
				var pageButton = new Button();
				pageButton.Content = "Add CommandBar";
				pageButton.Name = "PageButton";
				pageButton.Tag = "PB";

				page = WindowHelper.SetupSimulatedAppPage();
				SetPageContent(pageButton, page);
			});
			await WindowHelper.WaitForIdle();

			return page;
		}


		private async Task<AppBar> CreateFocusShiftTestAppBar(bool isOpen)
		{
			AppBar appBar = null;

			await RunOnUIThread(() =>
			{
				appBar = new AppBar();
				appBar.IsOpen = isOpen;
				// Use a Compact AppBar so the AppBarButton is visible and focusable even when the AppBar is closed.
				appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;

				var appBarButton = new AppBarButton();
				appBarButton.Label = "AppBarButton";
				appBarButton.Name = "AppBarButton";
				appBarButton.Tag = "ABB";
				appBar.Content = appBarButton;
			});
			await WindowHelper.WaitForIdle();

			return appBar;
		}

		private async Task SimulateTabAsync(UIElement container) => await SimulateNavigationDirectionAsync(container, FocusNavigationDirection.Next);

		private async Task SimulateShiftTabAsync(UIElement container) => await SimulateNavigationDirectionAsync(container, FocusNavigationDirection.Previous);

		private async Task SimulateUpAsync(UIElement container) => await SimulateNavigationDirectionAsync(container, FocusNavigationDirection.Up);

		private async Task SimulateNavigationDirectionAsync(UIElement container, FocusNavigationDirection direction)
		{
			await RunOnUIThread(() =>
			{
				var nextElement = FocusManager.FindNextElement(direction, new FindNextElementOptions()
				{
#if !WINAPPSDK
					SearchRoot = container.XamlRoot.Content
#endif
				});
				if (nextElement != null && nextElement is UIElement uiElement)
				{
					uiElement.Focus(FocusState.Keyboard);
				}
			});
			// Small delay to ensure the focus event have time to fire
			await Task.Delay(50);
		}

		private async Task<Page> SetupClosedDisplayModeTestEnvironment(bool setClosedDisplayModeValues)
		{
			Page page = null;

			// Setup our environment.
			await RunOnUIThread(() =>
			{
				page = WindowHelper.SetupSimulatedAppPage();

				page.TopAppBar = new AppBar();

				if (setClosedDisplayModeValues)
				{
					page.TopAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
				}

				var topStackPanel = new StackPanel();
				topStackPanel.Orientation = Orientation.Horizontal;

				var topButton1 = new AppBarButton();
				topButton1.Label = "First button";
				topStackPanel.Children.Add(topButton1);


				var topButton2 = new AppBarButton();
				topButton1.Label = "Second button";
				topStackPanel.Children.Add(topButton2);

				page.BottomAppBar = new AppBar();

				if (setClosedDisplayModeValues)
				{
					page.BottomAppBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
				}

				var bottomStackPanel = new StackPanel();
				bottomStackPanel.Orientation = Orientation.Horizontal;

				var bottomButton1 = new AppBarButton();
				bottomButton1.Label = "First button";
				bottomStackPanel.Children.Add(bottomButton1);


				var bottomButton2 = new AppBarButton();
				bottomButton2.Label = "Second button";
				bottomStackPanel.Children.Add(bottomButton2);

				page.TopAppBar.Content = topStackPanel;
				page.BottomAppBar.Content = bottomStackPanel;
			});
			await WindowHelper.WaitForIdle();

			return page;
		}

		private async Task CanOpenMinimalAppBarUsingMouseHelper(AppBar appBar)
		{
			var appBarBounds = new Rect();

			await RunOnUIThread(() =>
			{
				// We are only using AppBarClosedDisplayMode::Minimal since only Minimal AppBars can
				// be opened by clicking on the bar itself.
				appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
			});
			await WindowHelper.WaitForIdle();

			var openedEvent = new Event();
			var closedEvent = new Event();
			var openedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Opened");
			var closedRegistration = CreateSafeEventRegistration<AppBar, EventHandler<object>>("Closed");
			AttachOpenedAndClosedHandlers(appBar, openedEvent, openedRegistration, closedEvent, closedRegistration);

			await RunOnUIThread(async () => appBarBounds = await ControlHelper.GetBounds(appBar));
			await WindowHelper.WaitForIdle();

			// Click the center of the minimal AppBars where there are no buttons, to open it.
			//TestServices.InputHelper.MoveMouse(appBar);
			//TestServices.InputHelper.ClickMouseButton(MouseButton.Left, new Point(appBarBounds.Left + appBarBounds.Width / 2, appBarBounds.Top + appBarBounds.Height / 2));
			TestServices.InputHelper.LeftMouseClick(appBar);
			await openedEvent.WaitForDefault();

			await RunOnUIThread(() => appBar.IsOpen = false);
			await closedEvent.WaitForDefault();
		}

		private async Task<Page> SetupTopBottomInlineAppBarsPage()
		{
			Page page = null;

			await RunOnUIThread(() =>
			{
				var topAppBar = new AppBar();
				var bottomAppBar = new AppBar();

				var panel = (StackPanel)XamlReader.Load(@"
					<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
						<AppBar x:Name=""inlineAppBar"" VerticalAlignment=""Center"">
							<Rectangle Width=""100"" Height=""60"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"" Fill=""Orange""/>
						</AppBar>
					</StackPanel>");

				page = WindowHelper.SetupSimulatedAppPage();
				page.TopAppBar = topAppBar;
				page.BottomAppBar = bottomAppBar;
				SetPageContent(panel, page);
			});
			await WindowHelper.WaitForIdle();

			return page;
		}



		private void AttachOpenedAndClosedHandlers(
			AppBar appbar,
			Event openedEvent,
			SafeEventRegistration<AppBar, EventHandler<object>> opendedRegistration,
			Event closedEvent,
			SafeEventRegistration<AppBar, EventHandler<object>> closedRegistration)
		{
			opendedRegistration.Attach(appbar, (s, e) => openedEvent.Set());
			closedRegistration.Attach(appbar, (s, e) => closedEvent.Set());
		}

		private void SetWindowContent(UIElement content)
		{
			WindowHelper.WindowContent = content;
		}

		private void SetPageContent(UIElement content, Page page)
		{
			page.Content = content;
		}
	}
}
#endif
