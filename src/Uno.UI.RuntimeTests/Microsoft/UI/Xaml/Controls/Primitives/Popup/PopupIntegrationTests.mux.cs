// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PopupIntegrationTests.cpp, commit bac7a9c33

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Media3D;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls.Primitives;

// Notes on the port:
// - The WindowedPopup* tests upstream compare DComp / UIElement dump masters (HasAssociatedMasterFile). Those
//   dumps are replaced by explicit assertions on what the masters pin: the HWND rect of the windowed popup
//   (via the PopupHelper test hooks), the bounds of the popup content, and the overlay element.
// - The HWND rects in the masters were recorded in a WPF host whose XAML island sits 31 px below the origin of the
//   HWND coordinate space (e.g. HwndTop = 21 for a popup at y = 0 with a 10 px gutter). The expectations below are
//   relative to the XamlRoot content origin, so that 31 px is subtracted. They include the 10 px drop shadow
//   gutter WinUI adds on every side of a windowed popup's window.
// - Upstream Events are auto-reset; Uno's Event is not, so repeated waits on the same event call Reset().
// - SetWindowSizeOverride is a no-op in the Uno test infrastructure, so the 400x400 window is not enforced.
[TestClass]
[RequiresFullWindow] // The upstream tests expect window-absolute coordinates.
public class PopupIntegrationTests
{
	[TestCleanup]
	public async Task TestCleanup()
	{
		await RunOnUIThread(() =>
		{
			if (TestServices.WindowHelper.XamlRoot is { } xamlRoot)
			{
				VisualTreeHelper.CloseAllPopups(xamlRoot);
			}
		});
	}

	private static bool AreClose(double x, double y, double tolerance)
		=> Math.Abs(x - y) <= tolerance;

	private static async Task WaitAndReset(Event e)
	{
		await e.WaitForDefault();
		e.Reset();
	}

	//
	// Test Cases
	//
	[TestMethod]
	public async Task CanInstantiate()
	{
		await RunOnUIThread(() =>
		{
			try
			{
				var popup = new Popup();
				VERIFY_IS_NOT_NULL(popup);
			}
			catch (Exception ex)
			{
				Assert.Fail($"Failed to instantiate Popup: {ex}");
			}
		});
	}

	[TestMethod]
	public async Task CanPopupOpenAndClose()
	{
		Popup popup1 = null;

		var popupOpenedEvent = new Event();
		var popupClosedEvent = new Event();

		var openedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Opened));
		var closedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Closed));

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='LayoutRoot' Background='Red' Width='500' Height='700' > \r\n" +
				"  <Button x:Name='button1' Content='OpenClosePopup' /> \r\n" +
				"      <Popup x:Name='popup1' VerticalOffset='1' HorizontalOffset='1' IsOpen='false' Height='200' Width='500'> \r\n" +
				"          <StackPanel Background='SlateBlue' Height='200' Width='500'> \r\n" +
				"              <TextBlock Text='popup1(no light dismiss)'></TextBlock> \r\n" +
				"          </StackPanel> \r\n" +
				"      </Popup> \r\n" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);

			TestServices.WindowHelper.WindowContent = rootPanel;

			popup1 = (Popup)rootPanel.FindName("popup1");
			VERIFY_IS_NOT_NULL(popup1);

			openedRegistration.Attach(popup1, (s, e) =>
			{
				LOG_OUTPUT("PopupOpenClose: Popup Opened event is fired!");
				popupOpenedEvent.Set();
			});

			closedRegistration.Attach(popup1, (s, e) =>
			{
				LOG_OUTPUT("PopupOpenClose: Popup Closed event is fired!");
				popupClosedEvent.Set();
			});

			LOG_OUTPUT("PopupOpenClose: Set Popup1 IsOpen=TRUE");
			popup1.IsOpen = true;
		});

		await popupOpenedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(popup1.IsOpen);
			LOG_OUTPUT("PopupOpenClose: Set Popup1 IsOpen=FALSE");
			popup1.IsOpen = false;
		});

		await popupClosedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(popup1.IsOpen);
		});
	}

	[TestMethod]
	public async Task PopupCloseDoesNotClearIsOpenBinding()
	{
		Button button1 = null;
		Popup popup1 = null;

		var popupLoadedEvent = new Event();
		var popupLoadedEventRegistration = CreateSafeEventRegistration<Popup, RoutedEventHandler>(nameof(Popup.Loaded));
		var popupClosedEvent = new Event();
		var closedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Closed));

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='LayoutRoot' Background='Red' Width='500' Height='700' > \r\n" +
				"  <Button x:Name='button1' Content='test button' /> \r\n" +
				"  <Popup x:Name='popup1' VerticalOffset='1' HorizontalOffset='1' DataContext='true' IsOpen='{Binding}' Height='200' Width='500' IsLightDismissEnabled='true'> \r\n" +
				"      <StackPanel Background='SlateBlue' Height='200' Width='500'> \r\n" +
				"          <TextBlock Text='popup1(no light dismiss)'></TextBlock> \r\n" +
				"      </StackPanel> \r\n" +
				"  </Popup> \r\n" +
				"</StackPanel>");

			button1 = (Button)rootPanel.FindName("button1");
			popup1 = (Popup)rootPanel.FindName("popup1");

			popupLoadedEventRegistration.Attach(popup1, (s, e) =>
			{
				LOG_OUTPUT("Popup loaded.");
				popupLoadedEvent.Set();
			});

			closedRegistration.Attach(popup1, (s, e) =>
			{
				LOG_OUTPUT("Popup closed.");
				popupClosedEvent.Set();
			});

			// Verify initial state.
			VERIFY_IS_TRUE(popup1.IsOpen);
			VERIFY_IS_TRUE(popup1.IsLightDismissEnabled);
			VERIFY_IS_NOT_NULL(popup1.GetBindingExpression(Popup.IsOpenProperty));

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await popupLoadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var ap = (IWindowProvider)FrameworkElementAutomationPeer.CreatePeerForElement(popup1);
			ap.Close();
		});

		await popupClosedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			// Verify the binding is still there.
			VERIFY_IS_NOT_NULL(popup1.GetBindingExpression(Popup.IsOpenProperty));
		});
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: IncludeOnlyOn Desktop, mouse injection is unreliable on OneCore
	public async Task DoesNotChainRightClickWithOpenLightDismissPopup()
	{
		Popup popup = null;
		Button target = null;

		var targetRightTappedRegistration = CreateSafeEventRegistration<Button, RightTappedEventHandler>(nameof(Button.RightTapped));
		var wasTargetRightTapped = false;

		await RunOnUIThread(() =>
		{
			target = new Button();
			target.Content = "Target";

			targetRightTappedRegistration.Attach(target, (s, e) => { wasTargetRightTapped = true; });

			var root = new Grid();
			root.Children.Add(target);

			TestServices.WindowHelper.WindowContent = root;

			var rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Colors.Orange);
			rect.Width = 100;
			rect.Height = 100;

			popup = new Popup();
			popup.Child = rect;
			popup.IsLightDismissEnabled = true;
			popup.XamlRoot = root.XamlRoot;
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Inject right-click.
		await PopupHelper.ClickMouseButton(MouseButton.Right, target);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			Assert.IsFalse(popup.IsOpen, "The popup should have light-dismissed.");

			// The right-click should not have been chained, so the target's handler should not have
			// been invoked.
			VERIFY_IS_FALSE(wasTargetRightTapped);
		});
	}

	[TestMethod]
	[Ignore("Pending Popup port")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: IncludeOnlyOn Desktop, mouse injection is unreliable on OneCore
	public async Task ReplayPointerUpdate_PopupClosedDuringReplay_DoesNotCrash()
	{
		// Original 2-popup repro (popupA + popupB).  Snapshot fits within the inline
		// stack_vector capacity (typicalOpenPopupCount = 4) so the inline arena is
		// exercised, not the heap-allocated path.
		await ReplayPointerUpdate_PopupClosedDuringReplay_DoesNotCrashHelper(0 /* extraOpenPopupCount */);
	}

	[TestMethod]
	[Ignore("Pending Popup port")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: IncludeOnlyOn Desktop, mouse injection is unreliable on OneCore
	public async Task ReplayPointerUpdate_PopupClosedDuringReplay_WithManyOpenPopups_DoesNotCrash()
	{
		// 5 extra dummy popups + popupA + popupB = 7 total open popups when
		// CPopupRoot::ReplayPointerUpdate iterates, exceeding typicalOpenPopupCount = 4
		// and forcing the snapshot Jupiter::stack_vector to spill from its inline
		// arena to the heap.  Exercises the same reentrant-close UAF path on the
		// heap-allocated snapshot.
		await ReplayPointerUpdate_PopupClosedDuringReplay_DoesNotCrashHelper(5 /* extraOpenPopupCount */);
	}

	private async Task ReplayPointerUpdate_PopupClosedDuringReplay_DoesNotCrashHelper(int extraOpenPopupCount)
	{
		// The bug: CPopupRoot::ReplayPointerUpdate iterates m_pOpenPopups and calls
		// CPopup::ReplayPointerUpdate per-popup, which delivers a synthesized pointer event.
		// App-side handlers may synchronously close *other* popups during that delivery
		// (light-dismiss, cascading menus, focus-loss handlers, etc.).  Closing a popup runs
		// RemoveFromOpenPopupList -> CXcpList::Remove, which deletes the XCPListNode the outer
		// iterator is referencing -> use-after-free on the next pNode = pNode->m_pNext step.
		//
		// The repro: open popupA, hover the mouse over its content, then open popupB at the
		// same location.  Opening popupB triggers a layout-driven ReplayPreviousPointerUpdate.
		// popupB is at the head of m_pOpenPopups (CXcpList::Add head-prepends, so head = most
		// recently opened) so its replay fires first; buttonB's PointerEntered handler
		// synchronously closes popupA, which frees popupA's XCPListNode -- the same node the
		// outer iterator is about to advance to.
		//
		// When extraOpenPopupCount > 0, additional dummy popups are opened *before* popupA so
		// they sit later in the head-prepend iteration order (after popupA).  They have no
		// PointerEntered handlers and sit far outside the popupA/popupB hit-test zone, so
		// their CPopup::ReplayPointerUpdate calls are no-ops (they never received a pointer
		// message, so WindowedPopupInputSiteAdapter::ReplayPointerUpdate early-returns when
		// m_previousPointerPoint is null).  Their only role is to inflate m_pOpenPopups: when
		// (extraOpenPopupCount + 2) > typicalOpenPopupCount the snapshot Jupiter::stack_vector
		// exceeds its inline arena capacity and spills to the heap.
		Popup popupA = null;
		Popup popupB = null;
		Button buttonA = null;
		Button buttonB = null;
		var dummyPopups = new Popup[extraOpenPopupCount];

		var popupAOpenedEvent = new Event();
		var popupBOpenedEvent = new Event();
		var popupAClosedEvent = new Event();

		var popupAOpenedReg = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Opened));
		var popupBOpenedReg = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Opened));
		var popupAClosedReg = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Closed));
		var buttonBPointerEnteredReg = CreateSafeEventRegistration<Button, PointerEventHandler>(nameof(Button.PointerEntered));

		await RunOnUIThread(() =>
		{
			var root = new Grid();
			root.Width = 600;
			root.Height = 600;
			root.Background = new SolidColorBrush(Colors.Red);

			buttonA = new Button();
			buttonA.Content = "A";
			buttonA.Width = 200;
			buttonA.Height = 200;

			buttonB = new Button();
			buttonB.Content = "B";
			buttonB.Width = 200;
			buttonB.Height = 200;

			popupA = new Popup();
			popupA.Child = buttonA;
			popupA.VerticalOffset = 10;
			popupA.HorizontalOffset = 10;
			popupA.IsLightDismissEnabled = true;

			// popupB sits over the same logical region as popupA so that the still-hot
			// pointer hits buttonB the moment popupB opens.
			popupB = new Popup();
			popupB.Child = buttonB;
			popupB.VerticalOffset = 10;
			popupB.HorizontalOffset = 10;
			popupB.IsLightDismissEnabled = true;

			// Dummy popups: a 1x1 Rectangle child at offset (400, 400), well outside the
			// (10, 10)..(210, 210) buttonA/buttonB hit-test zone.  Non-light-dismiss so
			// they stay open through the scenario and remain in m_pOpenPopups when
			// CPopupRoot::ReplayPointerUpdate iterates.
			for (var i = 0; i < dummyPopups.Length; ++i)
			{
				var dummyChild = new Rectangle();
				dummyChild.Width = 1;
				dummyChild.Height = 1;

				var dummy = new Popup();
				dummy.Child = dummyChild;
				dummy.VerticalOffset = 400;
				dummy.HorizontalOffset = 400;
				dummy.IsLightDismissEnabled = false;
				dummyPopups[i] = dummy;
			}

			popupAOpenedReg.Attach(popupA, (s, e) => popupAOpenedEvent.Set());
			popupBOpenedReg.Attach(popupB, (s, e) => popupBOpenedEvent.Set());
			popupAClosedReg.Attach(popupA, (s, e) => popupAClosedEvent.Set());

			TestServices.WindowHelper.WindowContent = root;

			foreach (var dummy in dummyPopups)
			{
				dummy.XamlRoot = root.XamlRoot;
			}
			popupA.XamlRoot = root.XamlRoot;
			popupB.XamlRoot = root.XamlRoot;

			// Open dummies first so they're prepended to m_pOpenPopups before popupA.
			// CXcpList::Add prepends at head, so post-open list order (head -> tail) becomes:
			//     popupA, dummyN, dummyN-1, ..., dummy1
			// After popupB opens later (head = most recent), it becomes:
			//     popupB, popupA, dummyN, ..., dummy1
			// i.e. popupA remains the next-iterated node after popupB.
			foreach (var dummy in dummyPopups)
			{
				dummy.IsOpen = true;
			}

			popupA.IsOpen = true;
		});

		await popupAOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Move the mouse over popupA's button so the input system caches a "previous pointer"
		// state that the subsequent layout-driven replay will use.
		TestServices.InputHelper.MoveMouse(buttonA);
		await TestServices.WindowHelper.WaitForIdle();

		// Arm the reentrancy bomb on buttonB.  When buttonB receives PointerEntered during
		// popupB's CPopup::ReplayPointerUpdate (head of m_pOpenPopups), close popupA
		// synchronously.  popupA is the *next* node the outer iterator will advance to;
		// closing it here frees that node's XCPListNode mid-iteration on unfixed code.
		await RunOnUIThread(() =>
		{
			buttonBPointerEnteredReg.Attach(buttonB, (s, e) =>
			{
				LOG_OUTPUT("buttonB PointerEntered fired during replay; closing popupA synchronously.");
				if (popupA != null)
				{
					popupA.IsOpen = false;
				}
			});
		});

		// Open popupB while the pointer is still hot.  Opening popupB mutates the visual tree
		// under the hot pointer; the input pipeline responds with a
		// CCoreServices::ReplayPreviousPointerUpdate that routes through
		// CXamlIslandRoot::ReplayPointerUpdate -> CPopupRoot::ReplayPointerUpdate.
		await RunOnUIThread(() =>
		{
			popupB.IsOpen = true;
		});

		await popupBOpenedEvent.WaitForDefault();
		await popupAClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// The whole point of this test: we should reach this line at all (no AV).
		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(popupA.IsOpen);
			VERIFY_IS_TRUE(popupB.IsOpen);

			// Dummies were strong-ref'd in the snapshot, then their ReplayPointerUpdate ran
			// as a no-op (no cached pointer message, so WindowedPopupInputSiteAdapter's
			// early-return path was hit).  Verify they remained open through the iteration,
			// which also confirms no cascading close happened during the replay.
			foreach (var dummy in dummyPopups)
			{
				VERIFY_IS_TRUE(dummy.IsOpen);
			}
		});
	}

	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task PopupTabStop()
	{
		Button button1 = null;
		Button button2 = null;
		Button button3 = null;
		Popup popup1 = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel HorizontalAlignment='Center' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='LayoutRoot' Background='Red' Width='500' Height='700' > \r\n" +
				" <Popup x:Name='popup1' IsOpen='False' IsLightDismissEnabled='True'> \r\n" +
				"      <StackPanel> \r\n" +
				"          <Button x:Name='button1' Background='Green' Width='100' Height='100'/> \r\n" +
				"          <Button x:Name='button2' Background='Red' Width='100' Height='100'/> \r\n" +
				"          <Button x:Name='button3' Background='Blue' Width='100' Height='100'/> \r\n" +
				"      </StackPanel> \r\n" +
				"  </Popup> \r\n" +
				"</StackPanel>");

			button1 = (Button)rootPanel.FindName("button1");
			button2 = (Button)rootPanel.FindName("button2");
			button3 = (Button)rootPanel.FindName("button3");
			popup1 = (Popup)rootPanel.FindName("popup1");
			VERIFY_IS_NOT_NULL(button1);
			VERIFY_IS_NOT_NULL(button2);
			VERIFY_IS_NOT_NULL(button3);
			VERIFY_IS_NOT_NULL(popup1);

			// Verify initial state.
			VERIFY_IS_TRUE(popup1.IsLightDismissEnabled);
			TestServices.WindowHelper.WindowContent = rootPanel;

			popup1.IsOpen = true;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// press tab key to move focus to the first button.
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// Focus should be on the first button now.
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button1));
		});

		await TestServices.WindowHelper.WaitForIdle();

		// press tab key to move focus to the second button.
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// Focus should be on the second button now.
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button2));
		});
		await TestServices.WindowHelper.WaitForIdle();

		// press tab key to move focus to the third button.
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// Focus should be on the third button now.
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button3));
		});

		await TestServices.WindowHelper.WaitForIdle();

		// press shift tab key to move focus to the second button.
		await TestServices.KeyboardHelper.ShiftTab();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// Focus should be on the second button now.
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button2));
		});
	}

	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task DoNotSetDefaultFocusOnPhoneWhenPopupIsClosed()
	{
		Button button1 = null;
		Button button2 = null;
		Popup popup1 = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel HorizontalAlignment='Center' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='LayoutRoot' Background='Red' Width='500' Height='700' > \r\n" +
				" <Popup x:Name='popup1' IsOpen='False' IsLightDismissEnabled='True'> \r\n" +
				"      <StackPanel> \r\n" +
				"          <Button x:Name='button1' Background='Green' Width='100' Height='100'/> \r\n" +
				"      </StackPanel> \r\n" +
				"  </Popup> \r\n" +
				"  <Button x:Name='button2'/> \r\n" +
				"</StackPanel>");

			button1 = (Button)rootPanel.FindName("button1");
			button2 = (Button)rootPanel.FindName("button2");
			popup1 = (Popup)rootPanel.FindName("popup1");
			VERIFY_IS_NOT_NULL(button1);
			VERIFY_IS_NOT_NULL(button2);
			VERIFY_IS_NOT_NULL(popup1);

			// Verify initial state.
			VERIFY_IS_TRUE(popup1.IsLightDismissEnabled);
			TestServices.WindowHelper.WindowContent = rootPanel;

			popup1.IsOpen = true;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// press tab key to move focus to the first button.
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// Focus should be on the first button now.
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button1));
		});

		await TestServices.WindowHelper.WaitForIdle();

		// close the popup.
		await RunOnUIThread(() =>
		{
			popup1.IsOpen = false;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// On phone nothing will be focused when popup is closed.
		// On anywhere else, focus manager will focus the next focusable element (button2).
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button2));
		});
	}

	[TestMethod]
	public async Task ChangeOffsetWhenShown()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));
		Popup popup1 = null;
		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel HorizontalAlignment='Center' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='LayoutRoot' Background='Red' Width='500' Height='700' > \r\n" +
				" <Popup x:Name='popup1' IsOpen='True' IsLightDismissEnabled='True'> \r\n" +
				"      <StackPanel> \r\n" +
				"          <Button x:Name='button1' Background='Green' Width='100' Height='100'/> \r\n" +
				"      </StackPanel> \r\n" +
				"  </Popup> \r\n" +
				"  <Button x:Name='button2'/> \r\n" +
				"</StackPanel>");
			popup1 = (Popup)rootPanel.FindName("popup1");
			VERIFY_IS_NOT_NULL(popup1);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup1.HorizontalOffset = 222;
			popup1.VerticalOffset = 111;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// Focus should be on the first button now.
			VERIFY_IS_TRUE(popup1.HorizontalOffset == 222);
			VERIFY_IS_TRUE(popup1.VerticalOffset == 111);
		});
	}

	[TestMethod]
	public async Task SetSameChild()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));
		Popup popup1 = null;
		Button button1 = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel HorizontalAlignment='Center' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='LayoutRoot' Background='Red' Width='500' Height='700' > \r\n" +
				" <Popup x:Name='popup1' IsOpen='True' IsLightDismissEnabled='True'> \r\n" +
				"      <Button x:Name='button1' Background='Green' Width='100' Height='100'/> \r\n" +
				"  </Popup> \r\n" +
				"</StackPanel>");
			popup1 = (Popup)rootPanel.FindName("popup1");
			VERIFY_IS_NOT_NULL(popup1);
			button1 = (Button)rootPanel.FindName("button1");
			VERIFY_IS_NOT_NULL(button1);

			VERIFY_IS_TRUE(popup1.Child == button1);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup1.Child = null;
			VERIFY_IS_NULL(popup1.Child);
			popup1.Child = button1;
			VERIFY_IS_TRUE(popup1.Child == button1);
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	// Verify windowed popup's open and close
	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupOpenAndClose()
	{
		await WindowedPopupOpenAndCloseHelper();
	}

	[TestMethod]
	[Ignore("Not applicable: Holographic mode (HolographicOverride) has no Uno concept. Upstream: Verify popup's open and close in Holographic mode, windowed mode is disabled.")]
	public void PopupInHolographicModeOpenAndClose()
	{
		// Upstream: HolographicOverride holographicOverride; WindowedPopupOpenAndCloseHelper();
		// Compared against the DComp master Controls_Primitives_Popup_PopupIntegrationTests_PopupInHolographicModeOpenAndClose.
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupParentCanvasCompNode()
	{
		Popup popup = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Width='400' HorizontalAlignment='Left' VerticalAlignment='Top'> \r\n" +
				" <Canvas CompositeMode='SourceOver' Translation='50.0,50.0,0.0'>" +
				"  <Canvas Translation='75.0,75.0,0.0'>" +
				"   <Popup x:Name='myPopup' HorizontalOffset='-10' VerticalOffset='-10'>" +
				"    <Rectangle Width='100' Height='100' Fill='Red'/>" +
				"   </Popup>" +
				"  </Canvas>" +
				" </Canvas>" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);

			popup = (Popup)rootPanel.FindName("myPopup");
			VERIFY_IS_NOT_NULL(popup);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			PopupHelper.SetWindowed(popup);
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison); master HwndLeft=105 HwndTop=136 (-31 host offset) 120x120.
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));
		PopupHelper.VerifyWindowedPopupRect(popup, 105, 105, 120, 120);
	}

	[TestMethod]
	public async Task PopupCycleDoesntInfiniteLoop()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel HorizontalAlignment='Center' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='LayoutRoot' Background='Red' Width='500' Height='700' > \r\n" +
				"</StackPanel>");
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		StackPanel sp = null;
		Popup p = null;
		await RunOnUIThread(() =>
		{
			// Make a cycle.  This should probably error out, but it doesn't.  In late 19h1 we discovered that the
			// Skype app creates a cycle like this, and XAML gets stuck in an infinite loop.  For this test, we
			// just validate that XAML doesn't freeze up.
			sp = new StackPanel();
			p = new Popup();
			p.Child = sp;
			sp.Children.Add(p);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// Break the cycle to avoid a leak
			p.Child = null;
			sp.Children.Clear();
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task TallPopupPlacedRelativeToAnotherPopupIsBroughtInView()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));

		var popupActualPlacementChangedEvent = new Event();
		var popupActualPlacementChangedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.ActualPlacementChanged));

		Grid rootGrid = null;

		await RunOnUIThread(() =>
		{
			rootGrid = new Grid();
			TestServices.WindowHelper.WindowContent = rootGrid;
		});

		await TestServices.WindowHelper.WaitForIdle();

		Popup absolutelyPlacedPopup = null;
		Popup relativelyPlacedPopup = null;

		Border absolutelyPlacedPopupChild = null;

		await RunOnUIThread(() =>
		{
			absolutelyPlacedPopup = new Popup();
			relativelyPlacedPopup = new Popup();

			absolutelyPlacedPopup.XamlRoot = rootGrid.XamlRoot;
			relativelyPlacedPopup.XamlRoot = rootGrid.XamlRoot;

			absolutelyPlacedPopup.ShouldConstrainToRootBounds = true;
			relativelyPlacedPopup.ShouldConstrainToRootBounds = true;

			var textBlock1 = new TextBlock();
			textBlock1.Text = "Absolutely placed popup";
			absolutelyPlacedPopupChild = new Border();
			absolutelyPlacedPopupChild.Height = 100;
			absolutelyPlacedPopupChild.Width = 100;
			absolutelyPlacedPopupChild.Background = new SolidColorBrush(Colors.Red);
			absolutelyPlacedPopupChild.Child = textBlock1;
			absolutelyPlacedPopup.Child = absolutelyPlacedPopupChild;

			var textBlock2 = new TextBlock();
			textBlock2.Text = "Relatively placed popup";
			var relativelyPlacedPopupChild = new Border();
			relativelyPlacedPopupChild.Height = 175;
			relativelyPlacedPopupChild.Width = 175;
			relativelyPlacedPopupChild.Background = new SolidColorBrush(Colors.Blue);
			relativelyPlacedPopupChild.Child = textBlock2;
			relativelyPlacedPopup.Child = relativelyPlacedPopupChild;

			absolutelyPlacedPopup.HorizontalOffset = 150;
			absolutelyPlacedPopup.VerticalOffset = 150;

			relativelyPlacedPopup.PlacementTarget = absolutelyPlacedPopupChild;

			popupActualPlacementChangedRegistration.Attach(relativelyPlacedPopup, (s, e) =>
			{
				popupActualPlacementChangedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Open the popups with the relatively-placed popup above the absolutely-placed popup.");
			relativelyPlacedPopup.DesiredPlacement = PopupPlacementMode.Top;
			absolutelyPlacedPopup.IsOpen = true;
			relativelyPlacedPopup.IsOpen = true;
		});

		await WaitAndReset(popupActualPlacementChangedEvent);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("The absolutely-placed popup should've been pushed down to accommodate the relatively-placed popup.");

		var bounds = await ControlHelper.GetBounds(absolutelyPlacedPopupChild);
		LOG_OUTPUT($"Absolute popup bounds: (X, Y, Width, Height) = ({bounds.X:0}, {bounds.Y:0}, {bounds.Width:0}, {bounds.Height:0})");

		VERIFY_IS_TRUE(AreClose(bounds.Y, 175, 5));

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Flip the desired placement of the relatively placed popup to the bottom.");
			relativelyPlacedPopup.DesiredPlacement = PopupPlacementMode.Bottom;
		});

		await WaitAndReset(popupActualPlacementChangedEvent);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("The absolutely-placed popup should've been pushed up to accommodate the relatively-placed popup.");

		bounds = await ControlHelper.GetBounds(absolutelyPlacedPopupChild);
		LOG_OUTPUT($"Absolute popup bounds: (X, Y, Width, Height) = ({bounds.X:0}, {bounds.Y:0}, {bounds.Width:0}, {bounds.Height:0})");

		VERIFY_IS_TRUE(AreClose(bounds.Y, 125, 5));

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Now move the relatively placed popup to the left.");
			relativelyPlacedPopup.DesiredPlacement = PopupPlacementMode.Left;
		});

		await WaitAndReset(popupActualPlacementChangedEvent);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("The absolutely-placed popup should've been pushed to the right to accommodate the relatively-placed popup.");

		bounds = await ControlHelper.GetBounds(absolutelyPlacedPopupChild);
		LOG_OUTPUT($"Absolute popup bounds: (X, Y, Width, Height) = ({bounds.X:0}, {bounds.Y:0}, {bounds.Width:0}, {bounds.Height:0})");

		VERIFY_IS_TRUE(AreClose(bounds.X, 175, 5));

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Flip the desired placement of the relatively placed popup to the right.");
			relativelyPlacedPopup.DesiredPlacement = PopupPlacementMode.Right;
		});

		await WaitAndReset(popupActualPlacementChangedEvent);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("The absolutely-placed popup should've been pushed to the left to accommodate the relatively-placed popup.");

		bounds = await ControlHelper.GetBounds(absolutelyPlacedPopupChild);
		LOG_OUTPUT($"Absolute popup bounds: (X, Y, Width, Height) = ({bounds.X:0}, {bounds.Y:0}, {bounds.Width:0}, {bounds.Height:0})");

		VERIFY_IS_TRUE(AreClose(bounds.X, 125, 5));

		await RunOnUIThread(() =>
		{
			absolutelyPlacedPopup.IsOpen = false;
			relativelyPlacedPopup.IsOpen = false;
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task PopupSizeIncreaseDoesntChangePosition()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));

		var popupActualPlacementChangedEvent = new Event();
		var popupActualPlacementChangedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.ActualPlacementChanged));

		var popupChildLayoutUpdatedEvent = new Event();
		var popupChildLayoutUpdatedRegistration = CreateSafeEventRegistration<Border, EventHandler<object>>(nameof(Border.LayoutUpdated));

		Grid rootGrid = null;

		await RunOnUIThread(() =>
		{
			rootGrid = new Grid();
			TestServices.WindowHelper.WindowContent = rootGrid;
		});

		await TestServices.WindowHelper.WaitForIdle();

		Popup absolutelyPlacedPopup = null;
		Popup relativelyPlacedPopup = null;

		Border absolutelyPlacedPopupChild = null;
		Border relativelyPlacedPopupChild = null;

		await RunOnUIThread(() =>
		{
			absolutelyPlacedPopup = new Popup();
			relativelyPlacedPopup = new Popup();

			absolutelyPlacedPopup.XamlRoot = rootGrid.XamlRoot;
			relativelyPlacedPopup.XamlRoot = rootGrid.XamlRoot;

			absolutelyPlacedPopup.ShouldConstrainToRootBounds = true;
			relativelyPlacedPopup.ShouldConstrainToRootBounds = true;

			var textBlock1 = new TextBlock();
			textBlock1.Text = "Absolutely placed popup";
			absolutelyPlacedPopupChild = new Border();
			absolutelyPlacedPopupChild.Height = 100;
			absolutelyPlacedPopupChild.Width = 100;
			absolutelyPlacedPopupChild.Background = new SolidColorBrush(Colors.Red);
			absolutelyPlacedPopupChild.Child = textBlock1;
			absolutelyPlacedPopup.Child = absolutelyPlacedPopupChild;

			var textBlock2 = new TextBlock();
			textBlock2.Text = "Relatively placed popup";
			relativelyPlacedPopupChild = new Border();
			relativelyPlacedPopupChild.Height = 50;
			relativelyPlacedPopupChild.Width = 50;
			relativelyPlacedPopupChild.Background = new SolidColorBrush(Colors.Blue);
			relativelyPlacedPopupChild.Child = textBlock2;
			relativelyPlacedPopup.Child = relativelyPlacedPopupChild;

			absolutelyPlacedPopup.HorizontalOffset = 100;
			absolutelyPlacedPopup.VerticalOffset = 100;

			relativelyPlacedPopup.PlacementTarget = absolutelyPlacedPopupChild;

			popupChildLayoutUpdatedRegistration.Attach(absolutelyPlacedPopupChild, (s, e) =>
			{
				LOG_OUTPUT("Popup.Child.LayoutUpdated raised.");
				popupChildLayoutUpdatedEvent.Set();
			});

			popupActualPlacementChangedRegistration.Attach(relativelyPlacedPopup, (s, e) =>
			{
				LOG_OUTPUT("Popup.ActualPlacementChanged raised.");
				popupActualPlacementChangedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Open the popups with the relatively-placed popup above the absolutely-placed popup.");
			relativelyPlacedPopup.DesiredPlacement = PopupPlacementMode.Top;
			absolutelyPlacedPopup.IsOpen = true;
			relativelyPlacedPopup.IsOpen = true;
		});

		await popupActualPlacementChangedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("The relatively-placed popup should be above the absolutely-placed popup.");

		var absolutelyPlacedPopupBounds = await ControlHelper.GetBounds(absolutelyPlacedPopupChild);
		LOG_OUTPUT($"Absolute popup bounds: (X, Y, Width, Height) = ({absolutelyPlacedPopupBounds.X:0}, {absolutelyPlacedPopupBounds.Y:0}, {absolutelyPlacedPopupBounds.Width:0}, {absolutelyPlacedPopupBounds.Height:0})");

		var relativelyPlacedPopupBounds = await ControlHelper.GetBounds(relativelyPlacedPopupChild);
		LOG_OUTPUT($"Relative popup bounds: (X, Y, Width, Height) = ({relativelyPlacedPopupBounds.X:0}, {relativelyPlacedPopupBounds.Y:0}, {relativelyPlacedPopupBounds.Width:0}, {relativelyPlacedPopupBounds.Height:0})");

		VERIFY_IS_LESS_THAN(relativelyPlacedPopupBounds.Y, absolutelyPlacedPopupBounds.Y);

		popupChildLayoutUpdatedEvent.Reset();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Make the relatively-placed popup no longer fit above the absolutely-placed popup.");
			relativelyPlacedPopupChild.Height = 150;
		});

		await popupChildLayoutUpdatedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("The relatively-placed popup should still be above the absolutely-placed popup even though it now fits below it. Instead, the absolutely-placed popup should be pushed down.");

		absolutelyPlacedPopupBounds = await ControlHelper.GetBounds(absolutelyPlacedPopupChild);
		LOG_OUTPUT($"Absolute popup bounds: (X, Y, Width, Height) = ({absolutelyPlacedPopupBounds.X:0}, {absolutelyPlacedPopupBounds.Y:0}, {absolutelyPlacedPopupBounds.Width:0}, {absolutelyPlacedPopupBounds.Height:0})");

		relativelyPlacedPopupBounds = await ControlHelper.GetBounds(relativelyPlacedPopupChild);
		LOG_OUTPUT($"Relative popup bounds: (X, Y, Width, Height) = ({relativelyPlacedPopupBounds.X:0}, {relativelyPlacedPopupBounds.Y:0}, {relativelyPlacedPopupBounds.Width:0}, {relativelyPlacedPopupBounds.Height:0})");

		VERIFY_IS_LESS_THAN(relativelyPlacedPopupBounds.Y, absolutelyPlacedPopupBounds.Y);

		await RunOnUIThread(() =>
		{
			absolutelyPlacedPopup.IsOpen = false;
			relativelyPlacedPopup.IsOpen = false;
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupHWNDBounds1UR()
	{
		await WindowedPopupHWNDBounds1Helper(false, false);
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupHWNDBounds1PR()
	{
		await WindowedPopupHWNDBounds1Helper(true, false);
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupHWNDBounds1UC()
	{
		await WindowedPopupHWNDBounds1Helper(false, true);
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupHWNDBounds1PC()
	{
		await WindowedPopupHWNDBounds1Helper(true, true);
	}

	private async Task WindowedPopupHWNDBounds1Helper(bool isParented, bool popupChildIsCanvas)
	{
		Popup popup = null;
		Canvas canvas = null;
		FrameworkElement popupChild = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Width='400' HorizontalAlignment='Left' VerticalAlignment='Top'> \r\n" +
				"  <Canvas x:Name='myCanvas' Width='150' Height='150' Background='Green'/>" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);

			canvas = (Canvas)rootPanel.FindName("myCanvas");
			VERIFY_IS_NOT_NULL(canvas);

			if (popupChildIsCanvas)
			{
				popup = (Popup)XamlReader.Load(
					"<Popup xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' HorizontalAlignment='Left'> " +
					"  <Canvas x:Name='popupChild'>" +
					"    <Rectangle Width='100' Height='100' Fill='Red'/>" +
					"  </Canvas>" +
					"</Popup>");
			}
			else
			{
				popup = (Popup)XamlReader.Load(
					"<Popup xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' HorizontalAlignment='Left'> " +
					"  <Rectangle x:Name='popupChild' Width='100' Height='100' Fill='Red'/>" +
					"</Popup>");
			}

			VERIFY_IS_NOT_NULL(popup);

			popupChild = (FrameworkElement)popup.FindName("popupChild");
			VERIFY_IS_NOT_NULL(popupChild);

			if (isParented)
			{
				canvas.Children.Add(popup);
			}

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			PopupHelper.SetWindowed(popup);
			popup.XamlRoot = canvas.XamlRoot;
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// A parented popup follows the green canvas, which sits at x = 125 (centered 150 wide in the 400 wide panel).
		var baseX = isParented ? 125 : 0;

		// Upstream: VerifyMockDCompOutput(NoComparison); masters HwndLeft = 115 / -10 (parented / not), HwndTop = 21.
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));
		PopupHelper.VerifyWindowedPopupRect(popup, baseX - 10, -10, 120, 120);

		await RunOnUIThread(() =>
		{
			popupChild.Margin = new Thickness(10, 10, 10, 10);
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "2"); masters HwndLeft = 125 / 0, HwndTop = 31.
		PopupHelper.VerifyWindowedPopupRect(popup, baseX, 0, 120, 120);

		await RunOnUIThread(() =>
		{
			popupChild.Translation = new Vector3(20, 20, 32);
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "3"); masters HwndLeft = 145 / 20, HwndTop = 51.
		PopupHelper.VerifyWindowedPopupRect(popup, baseX + 20, 20, 120, 120);
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupHWNDBounds2UR()
	{
		await WindowedPopupHWNDBounds2Helper(false, false);
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupHWNDBounds2PR()
	{
		await WindowedPopupHWNDBounds2Helper(true, false);
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupHWNDBounds2UC()
	{
		await WindowedPopupHWNDBounds2Helper(false, true);
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupHWNDBounds2PC()
	{
		await WindowedPopupHWNDBounds2Helper(true, true);
	}

	private async Task WindowedPopupHWNDBounds2Helper(bool isParented, bool popupChildIsCanvas)
	{
		Popup popup = null;
		Canvas canvas = null;
		FrameworkElement popupChild = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Width='400' HorizontalAlignment='Left' VerticalAlignment='Top'> \r\n" +
				"  <Canvas x:Name='myCanvas' Width='150' Height='150' Background='Green'/>" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);

			canvas = (Canvas)rootPanel.FindName("myCanvas");
			VERIFY_IS_NOT_NULL(canvas);

			if (popupChildIsCanvas)
			{
				popup = (Popup)XamlReader.Load(
					"<Popup xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' HorizontalAlignment='Left'> " +
					"  <Canvas x:Name='popupChild'>" +
					"    <Rectangle Width='100' Height='100' Fill='Red'/>" +
					"  </Canvas>" +
					"</Popup>");
			}
			else
			{
				popup = (Popup)XamlReader.Load(
					"<Popup xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' HorizontalAlignment='Left'> " +
					"  <Rectangle x:Name='popupChild' Width='100' Height='100' Fill='Red'/>" +
					"</Popup>");
			}

			VERIFY_IS_NOT_NULL(popup);

			popupChild = (FrameworkElement)popup.FindName("popupChild");
			VERIFY_IS_NOT_NULL(popupChild);

			if (isParented)
			{
				canvas.Children.Add(popup);
			}

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			PopupHelper.SetWindowed(popup);
			popup.XamlRoot = canvas.XamlRoot;
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// A parented popup follows the green canvas, which sits at x = 125 (centered 150 wide in the 400 wide panel).
		var baseX = isParented ? 125 : 0;

		// Upstream: VerifyMockDCompOutput(NoComparison); masters HwndLeft = 115 / -10 (parented / not), HwndTop = 21.
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));
		PopupHelper.VerifyWindowedPopupRect(popup, baseX - 10, -10, 120, 120);

		await RunOnUIThread(() =>
		{
			popup.HorizontalOffset = 10;
			popup.VerticalOffset = 10;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "2"); masters HwndLeft = 125 / 0, HwndTop = 31.
		PopupHelper.VerifyWindowedPopupRect(popup, baseX, 0, 120, 120);

		await RunOnUIThread(() =>
		{
			popup.Translation = new Vector3(20, 20, 32);
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "3"); masters HwndLeft = 145 / 20, HwndTop = 51.
		PopupHelper.VerifyWindowedPopupRect(popup, baseX + 20, 20, 120, 120);

		await RunOnUIThread(() =>
		{
			var clip = new RectangleGeometry();
			clip.Rect = new Rect(25, 25, 50, 50);
			canvas.Clip = clip;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "4"); the master is identical to step 3: a windowed
		// popup is not clipped by its parent.
		PopupHelper.VerifyWindowedPopupRect(popup, baseX + 20, 20, 120, 120);

		await RunOnUIThread(() =>
		{
			canvas.Translation = new Vector3(15, 15, 0);
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "5"); masters HwndLeft = 160, HwndTop = 66 (parented, follows
		// the canvas) and HwndLeft = 20, HwndTop = 51 (unparented, unchanged).
		if (isParented)
		{
			PopupHelper.VerifyWindowedPopupRect(popup, baseX + 20 + 15, 20 + 15, 120, 120);
		}
		else
		{
			PopupHelper.VerifyWindowedPopupRect(popup, baseX + 20, 20, 120, 120);
		}
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupHWNDBoundsNested()
	{
		Popup popup = null;
		Popup nestedPopup = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Width='400' HorizontalAlignment='Left' VerticalAlignment='Top'> \r\n" +
				"  <Popup x:Name='myPopup'>" +
				"    <Grid Width='150' Height='150' Background='Green'>" +
				"      <Popup x:Name='myNestedPopup'>" +
				"        <Rectangle Width='100' Height='100' Fill='Red'/>" +
				"      </Popup>" +
				"    </Grid>" +
				"  </Popup>" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);

			popup = (Popup)rootPanel.FindName("myPopup");
			VERIFY_IS_NOT_NULL(popup);

			nestedPopup = (Popup)rootPanel.FindName("myNestedPopup");
			VERIFY_IS_NOT_NULL(nestedPopup);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			PopupHelper.SetWindowed(popup);
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison); master HwndLeft = -10, HwndTop = 21, 170x170 (150 + gutters).
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));
		PopupHelper.VerifyWindowedPopupRect(popup, -10, -10, 170, 170);

		await RunOnUIThread(() =>
		{
			PopupHelper.SetWindowed(nestedPopup);
			nestedPopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "2"); masters have two windows: the nested popup at
		// (-10, 21, 120x120) and the outer popup unchanged.
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(nestedPopup));
		PopupHelper.VerifyWindowedPopupRect(nestedPopup, -10, -10, 120, 120);
		PopupHelper.VerifyWindowedPopupRect(popup, -10, -10, 170, 170);

		await RunOnUIThread(() =>
		{
			popup.Translation = new Vector3(20, 20, 0);
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "3"); both windows move to (10, 41).
		PopupHelper.VerifyWindowedPopupRect(nestedPopup, 10, 10, 120, 120);
		PopupHelper.VerifyWindowedPopupRect(popup, 10, 10, 170, 170);

		await RunOnUIThread(() =>
		{
			nestedPopup.Translation = new Vector3(30, 30, 0);
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "4"); nested moves to (40, 71), outer stays at (10, 41).
		PopupHelper.VerifyWindowedPopupRect(nestedPopup, 40, 40, 120, 120);
		PopupHelper.VerifyWindowedPopupRect(popup, 10, 10, 170, 170);
	}

	//  Verify Input in windowed popup's content
	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupInput()
	{
		// Create windowed popup and open it

		Popup popup = null;
		Button button1 = null;
		StackPanel rootPanel = null;

		var popupOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Opened));
		var popupClosedEvent = new Event();
		var closedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Closed));

		await RunOnUIThread(() =>
		{
			rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='LayoutRoot' Background='Red' Width='400' Height='400' > \r\n" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);
			TestServices.WindowHelper.WindowContent = rootPanel;

		});
		// The transition from splash screen to 1st Xaml causes a PositionChaged event
		// on the main app window, which would cause any windowed popups to close.
		// Therefore, defer opening the popup until first Xaml frame is drawn.
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup = (Popup)XamlReader.Load(
				"<Popup xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' HorizontalAlignment='Left'> " +
				"  <StackPanel Background='Green' Width='200' Height='200'> \r\n" +
				"    <Button x:Name='button1' Content='Button1InPopup' Width='100' Height='50'/> \r\n" +
				"  </StackPanel> \r\n" +
				"</Popup>");
			VERIFY_IS_NOT_NULL(popup);
			popup.VerticalOffset = 10;
			popup.HorizontalOffset = -10;
			PopupHelper.SetWindowed(popup);

			button1 = (Button)popup.FindName("button1");

			openedRegistration.Attach(popup, (s, e) =>
			{
				LOG_OUTPUT("WindowedPopupInput: Windowed popup: Open event fired");
				popupOpenedEvent.Set();
			});

			closedRegistration.Attach(popup, (s, e) =>
			{
				LOG_OUTPUT("WindowedPopupInput: Windowed popup: Closed event fired");
				popupClosedEvent.Set();
			});

			popup.XamlRoot = rootPanel.XamlRoot;
			LOG_OUTPUT("WindowedPopupInput: Set popup->IsOpen = true");
			popup.IsOpen = true;
		});

		await popupOpenedEvent.WaitForDefault();
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));

		// Test pointer input on windowed popup's button

		var spClickEvent = new Event();
		var clickRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>(nameof(Button.Click));

		await RunOnUIThread(() =>
		{
			clickRegistration.Attach(button1, (sender, e) =>
			{
				LOG_OUTPUT("WindowedPopupInput: Windowed popup content: Button Click event fired");
				spClickEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();
		LOG_OUTPUT("WindowedPopupInput: Windowed popup content: Pointer tap on button");
		TestServices.InputHelper.Tap(button1);
		await spClickEvent.WaitForDefault();
		spClickEvent.Reset();

		// Test keyboard input on windowed popup's button

		var spGotFocusEvent = new Event();
		var gotFocusRegistration = CreateSafeEventRegistration<UIElement, RoutedEventHandler>(nameof(UIElement.GotFocus));
		await RunOnUIThread(() =>
		{
			gotFocusRegistration.Attach(button1, (sender, e) =>
			{
				LOG_OUTPUT("WindowedPopupInput: Windowed popup content: Button GotFocus event fired");
				spGotFocusEvent.Set();
			});

			LOG_OUTPUT("WindowedPopupInput: Windowed popup content: Set focus on button");
			button1.Focus(FocusState.Keyboard);
		});

		await spGotFocusEvent.WaitForDefault();

		await TestServices.WindowHelper.WaitForIdle();
		LOG_OUTPUT("WindowedPopupInput: Windowed popup content: Press Enter key");
		await TestServices.KeyboardHelper.Enter();
		await spClickEvent.WaitForDefault();
		spClickEvent.Reset();

		// Test input using UIA automation on windowed popup's button

		await RunOnUIThread(() =>
		{
			var button1AP = (ButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(button1);
			LOG_OUTPUT("WindowedPopupInput: Windowed popup content: Click button using UIA");
			button1AP.Invoke();
		});
		await spClickEvent.WaitForDefault();
		spClickEvent.Reset();

		// Test pointer input on windowed popup's button after popup offset has changed

		await RunOnUIThread(() =>
		{
			popup.HorizontalOffset += 100;
		});
		await TestServices.WindowHelper.WaitForIdle();
		LOG_OUTPUT("WindowedPopupInput: Windowed popup content: Pointer tap on button");
		TestServices.InputHelper.Tap(button1);
		await spClickEvent.WaitForDefault();
		spClickEvent.Reset();

		// Close windowed popup

		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(popup.IsOpen);
			LOG_OUTPUT("WindowedPopupInput: Set popup->IsOpen = false");
			popup.IsOpen = false;
		});

		await popupClosedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(popup.IsOpen);
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupPointerInputCoords()
	{
		await WindowedPopupPointerInputCoordsHelper(false/*isRTL*/);
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupPointerInputCoordsRTL()
	{
		await WindowedPopupPointerInputCoordsHelper(true/*isRTL*/);
	}

	private async Task WindowedPopupPointerInputCoordsHelper(bool isRTL)
	{
		// Create windowed popup and open it

		Popup popup = null;
		FrameworkElement popupChild = null;
		StackPanel rootPanel = null;

		var popupOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Opened));
		var popupClosedEvent = new Event();
		var closedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Closed));

		await RunOnUIThread(() =>
		{
			rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='LayoutRoot' Background='Red' Width='400' Height='400' > \r\n" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);
			TestServices.WindowHelper.WindowContent = rootPanel;

			if (isRTL)
			{
				rootPanel.FlowDirection = FlowDirection.RightToLeft;
			}
		});
		// The transition from splash screen to 1st Xaml causes a PositionChaged event
		// on the main app window, which would cause any windowed popups to close.
		// Therefore, defer opening the popup until first Xaml frame is drawn.
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup = (Popup)XamlReader.Load(
				"<Popup xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' HorizontalAlignment='Left'> " +
				"  <StackPanel Background='Green' Width='200' Height='200'> \r\n" +
				"    <TextBlock Text='Some text'/> \r\n" +
				"  </StackPanel> \r\n" +
				"</Popup>");
			VERIFY_IS_NOT_NULL(popup);
			popup.VerticalOffset = 15;
			popup.HorizontalOffset = 26;
			PopupHelper.SetWindowed(popup);
			rootPanel.Children.Add(popup);

			popupChild = (FrameworkElement)popup.Child;

			openedRegistration.Attach(popup, (s, e) =>
			{
				LOG_OUTPUT("WindowedPopupPointerInputCoords: Windowed popup: Open event fired");
				popupOpenedEvent.Set();
			});

			closedRegistration.Attach(popup, (s, e) =>
			{
				LOG_OUTPUT("WindowedPopupPointerInputCoords: Windowed popup: Closed event fired");
				popupClosedEvent.Set();
			});

			popup.XamlRoot = rootPanel.XamlRoot;
			LOG_OUTPUT("WindowedPopupPointerInputCoords: Set popup->IsOpen = true");
			popup.IsOpen = true;
		});

		await popupOpenedEvent.WaitForDefault();
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));

		// Test pointer input accuracy on windowed popup

		var spPointerPressedEvent = new Event();
		var pressedRegistration = CreateSafeEventRegistration<UIElement, PointerEventHandler>(nameof(UIElement.PointerPressed));

		var targetX = 17;
		var targetY = 23;
		var popupChildWidth = 200;
		var popupChildHeight = 200;
		var clickOffsetFromCenterX = (-(popupChildWidth / 2) + targetX) * (isRTL ? -1 : 1);
		var clickOffsetFromCenterY = (-(popupChildHeight / 2) + targetY);

		// Assertions run after the wait: an exception thrown inside a routed event handler would not fail the test.
		Point pressedPosition = default;

		await RunOnUIThread(() =>
		{
			pressedRegistration.Attach(popupChild, (sender, args) =>
			{
				var pointerPoint = args.GetCurrentPoint(popupChild);
				pressedPosition = pointerPoint.Position;
				LOG_OUTPUT($"WindowedPopupPointerInputCoords: PointerPressed at: {pointerPoint.Position.X},{pointerPoint.Position.Y} (expecting ~{targetX},{targetY})");
				spPointerPressedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();
		LOG_OUTPUT("WindowedPopupPointerInputCoords: Windowed popup content: Click at specific point in popup");
		await PopupHelper.ClickMouseButton(MouseButton.Left, popupChild, clickOffsetFromCenterX, clickOffsetFromCenterY);
		await spPointerPressedEvent.WaitForDefault();
		spPointerPressedEvent.Reset();
		VERIFY_IS_TRUE(Math.Abs(pressedPosition.X - targetX) < 1.5, "Point should be within a pixel or so.");
		VERIFY_IS_TRUE(Math.Abs(pressedPosition.Y - targetY) < 1.5, "Point should be within a pixel or so.");

		// Test pointer input accuracy on windowed popup after popup offset has changed

		await RunOnUIThread(() =>
		{
			popup.HorizontalOffset += 100;
		});
		await TestServices.WindowHelper.WaitForIdle();
		LOG_OUTPUT("WindowedPopupPointerInputCoords: Windowed popup content: Click at specific point in popup");
		await PopupHelper.ClickMouseButton(MouseButton.Left, popupChild, clickOffsetFromCenterX, clickOffsetFromCenterY);
		await spPointerPressedEvent.WaitForDefault();
		spPointerPressedEvent.Reset();
		VERIFY_IS_TRUE(Math.Abs(pressedPosition.X - targetX) < 1.5, "Point should be within a pixel or so.");
		VERIFY_IS_TRUE(Math.Abs(pressedPosition.Y - targetY) < 1.5, "Point should be within a pixel or so.");

		// Close windowed popup

		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(popup.IsOpen);
			LOG_OUTPUT("WindowedPopupPointerInputCoords: Set popup->IsOpen = false");
			popup.IsOpen = false;
		});

		await popupClosedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(popup.IsOpen);
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	// Verify windowed popup's UIA tree
	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupUIATree()
	{
		// Create windowed popup and open it

		Popup popup = null;
		Button button1 = null;
		StackPanel rootPanel = null;

		var popupOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Opened));
		var popupClosedEvent = new Event();
		var closedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Closed));

		await RunOnUIThread(() =>
		{
			rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='LayoutRoot' Background='Red' Width='400' Height='400' > \r\n" +
				"    <Button Content='DummyButton' Width='100' Height='50' AutomationProperties.Name='DummyButton'/> \r\n" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);
			TestServices.WindowHelper.WindowContent = rootPanel;

		});
		// The transition from splash screen to 1st Xaml causes a PositionChaged event
		// on the main app window, which would cause any windowed popups to close.
		// Therefore, defer opening the popup until first Xaml frame is drawn.
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup = (Popup)XamlReader.Load(
				"<Popup xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' HorizontalAlignment='Left' AutomationProperties.Name='PopupAutomationName'> " +
				"  <StackPanel Background='Green' Width='200' Height='200'> \r\n" +
				"    <Button x:Name='button1' Content='Button1InPopup' Width='100' Height='50' AutomationProperties.Name='ButtonAutomationName'/> \r\n" +
				"  </StackPanel> \r\n" +
				"</Popup>");
			VERIFY_IS_NOT_NULL(popup);
			popup.VerticalOffset = 10;
			popup.HorizontalOffset = -10;
			PopupHelper.SetWindowed(popup);
			popup.XamlRoot = rootPanel.XamlRoot;

			button1 = (Button)popup.FindName("button1");

			openedRegistration.Attach(popup, (s, e) =>
			{
				LOG_OUTPUT("WindowedPopupUIATree: Windowed popup: Open event fired");
				popupOpenedEvent.Set();
			});

			closedRegistration.Attach(popup, (s, e) =>
			{
				LOG_OUTPUT("WindowedPopupUIATree: Windowed popup: Closed event fired");
				popupClosedEvent.Set();
			});

			LOG_OUTPUT("WindowedPopupUIATree: Set popup->IsOpen = true");
			popup.IsOpen = true;
		});

		await popupOpenedEvent.WaitForDefault();
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));

		// Verify UIA tree structure, and ensure that windowed popup is a window

		// Set focus on windowed popup's button
		var spGotFocusEvent = new Event();
		var gotFocusRegistration = CreateSafeEventRegistration<UIElement, RoutedEventHandler>(nameof(UIElement.GotFocus));
		await RunOnUIThread(() =>
		{
			gotFocusRegistration.Attach(button1, (sender, e) =>
			{
				LOG_OUTPUT("WindowedPopupUIATree: Windowed popup content: Button GotFocus event fired");
				spGotFocusEvent.Set();
			});

			LOG_OUTPUT("WindowedPopupUIATree: Windowed popup content: Set focus on button");
			button1.Focus(FocusState.Keyboard);
		});

		await spGotFocusEvent.WaitForDefault();

		// Upstream: PopupHelper::AreWindowedPopupsEnabled() -> AutomationClientInitializer::TEMP_WaitForOpenWindowedPopup(popup)
		// (temporary fix for UiaEndpoint synchronization). Not needed when walking the peer tree.

		// Walk up the automation tree from the focused button.
		// Uno: the UIA client is replaced by the AutomationPeer tree.
		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("WindowedPopupUIATree: Windowed popup content: Get focused button using UIA");
			var focusedElement = FocusManager.GetFocusedElement(rootPanel.XamlRoot) as UIElement;
			Assert.IsNotNull(focusedElement);
			var focusedPeer = FrameworkElementAutomationPeer.CreatePeerForElement(focusedElement);
			Assert.IsNotNull(focusedPeer);
			Assert.AreEqual("ButtonAutomationName", focusedPeer.GetName());

			LOG_OUTPUT("WindowedPopupUIATree: Windowed popup content: Get button's UIA parent");
			var popupPeer = focusedPeer.GetParent();
			VERIFY_IS_NOT_NULL(popupPeer);
			Assert.AreEqual("PopupAutomationName", popupPeer.GetName());

			var next = popupPeer.Navigate(AutomationNavigationDirection.NextSibling);
			Assert.IsNull(next, "Popup node should have no next sibling");

			var previous = popupPeer.Navigate(AutomationNavigationDirection.PreviousSibling);
			Assert.IsNull(previous, "Popup node should have no previous sibling");

			// Get the uia parent. This will be the UIAWindow for the windowed popup. Note that we are not checking its name
			// because it will have a different name when running in uap versus wpfmode.
			LOG_OUTPUT("WindowedPopupUIATree: Windowed popup content: Get popup's UIA parent.");
			var popupWindowPeer = popupPeer.GetParent();
			VERIFY_IS_NOT_NULL(popupWindowPeer);
		});

		// Close windowed popup

		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(popup.IsOpen);
			LOG_OUTPUT("WindowedPopupUIATree: Set popup->IsOpen = false");
			popup.IsOpen = false;
		});

		await popupClosedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(popup.IsOpen);
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupRTL()
	{
		await PopupRTLHelper(true);
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	public async Task PopupRTL()
	{
		await PopupRTLHelper(false);
	}

	private async Task PopupRTLHelper(bool isWindowed)
	{
		Canvas canvas = null;
		Popup popup = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Width='400' HorizontalAlignment='Left' VerticalAlignment='Top' FlowDirection='RightToLeft'> \r\n" +
				" <Canvas x:Name='myCanvas' Translation='50.0,50.0,0.0'>" +
				"  <Popup x:Name='myPopup' HorizontalOffset='100' VerticalOffset='100'>" +
				"    <Rectangle Width='100' Height='100' Fill='Red'/>" +
				"  </Popup>" +
				" </Canvas>" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);

			canvas = (Canvas)rootPanel.FindName("myCanvas");
			VERIFY_IS_NOT_NULL(canvas);

			popup = (Popup)rootPanel.FindName("myPopup");
			VERIFY_IS_NOT_NULL(popup);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			if (isWindowed)
			{
				PopupHelper.SetWindowed(popup);
			}
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison). The master mirrors the 400 wide panel: the canvas (translated by
		// 50,50) is anchored at x = 350, and the 100x100 content ends up at x = 150..250, y = 150..250.
		await VerifyPopupRTLPlacement(popup, isWindowed);

		await RunOnUIThread(() =>
		{
			canvas.CompositeMode = ElementCompositeMode.SourceOver;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "2"). Same placement as before.
		await VerifyPopupRTLPlacement(popup, isWindowed);
	}

	private static async Task VerifyPopupRTLPlacement(Popup popup, bool isWindowed)
	{
		var bounds = await ControlHelper.GetBounds((FrameworkElement)popup.Child);
		LOG_OUTPUT($"Popup child bounds: (X, Y, Width, Height) = ({bounds.X:0}, {bounds.Y:0}, {bounds.Width:0}, {bounds.Height:0})");
		VERIFY_ARE_VERY_CLOSE(bounds.X, 150, 1, "popup child x");
		VERIFY_ARE_VERY_CLOSE(bounds.Y, 150, 1, "popup child y");
		VERIFY_ARE_VERY_CLOSE(bounds.Width, 100, 1, "popup child width");
		VERIFY_ARE_VERY_CLOSE(bounds.Height, 100, 1, "popup child height");

		if (isWindowed)
		{
			// Masters (WindowedPopupRTL, WindowedPopupRTL.2): HwndLeft = 140, HwndTop = 171, 120x120.
			VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));
			PopupHelper.VerifyWindowedPopupRect(popup, 140, 140, 120, 120);
		}
	}

	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopupMoveSize()
	{
		Popup popup = null;
		Rectangle rectangle = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Width='400' HorizontalAlignment='Left' VerticalAlignment='Top'> \r\n" +
				"  <Popup x:Name='myPopup' HorizontalOffset='10' VerticalOffset='10'>" +
				"    <Rectangle x:Name='myRectangle' Width='100' Height='100' Fill='Red'/>" +
				"  </Popup>" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);

			popup = (Popup)rootPanel.FindName("myPopup");
			VERIFY_IS_NOT_NULL(popup);

			rectangle = (Rectangle)rootPanel.FindName("myRectangle");
			VERIFY_IS_NOT_NULL(rectangle);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			PopupHelper.SetWindowed(popup);
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison); master HwndLeft = 0, HwndTop = 31, 120x120.
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));
		PopupHelper.VerifyWindowedPopupRect(popup, 0, 0, 120, 120);

		await RunOnUIThread(() =>
		{
			popup.HorizontalOffset = 20;
			popup.VerticalOffset = 20;

			rectangle.Width = 200;
			rectangle.Height = 200;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "2"); master HwndLeft = 10, HwndTop = 41, 220x220.
		PopupHelper.VerifyWindowedPopupRect(popup, 10, 10, 220, 220);
	}

	// Upstream: Ignore (TODO: Move windowed popups to lifted input).
	[TestMethod]
	[Ignore("Pending windowed popups")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task WindowedPopup3DRequirements()
	{
		StackPanel rootPanel = null;
		Popup popup = null;
		Rectangle rectangle = null;

		await RunOnUIThread(() =>
		{
			rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Width='400' HorizontalAlignment='Left' VerticalAlignment='Top'> \r\n" +
				"  <Popup x:Name='myPopup' HorizontalOffset='10' VerticalOffset='10'>" +
				"    <Rectangle x:Name='myRectangle' Width='100' Height='100' Fill='Red'/>" +
				"  </Popup>" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);

			popup = (Popup)rootPanel.FindName("myPopup");
			VERIFY_IS_NOT_NULL(popup);

			rectangle = (Rectangle)rootPanel.FindName("myRectangle");
			VERIFY_IS_NOT_NULL(rectangle);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// The masters for steps 1, 1B and 2 have a window (HwndLeft/HwndTop = 10,10 and 20,20); the masters for
		// steps 3, 4 and 5 have none: 3D that changes the direction of the x/y axes can't be done in a window.
		await RunOnUIThread(() =>
		{
			popup.ShouldConstrainToRootBounds = false;
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison);
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));
		var step1Rect = PopupHelper.GetWindowedPopupMoveAndResizeRect(popup);

		await RunOnUIThread(() =>
		{
			popup.IsOpen = false;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup.Translation = new Vector3(10, 10, 32);
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "2"); a Z translation does not flip the axes, still a window,
		// 10 px further than step 1.
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));
		var step2Rect = PopupHelper.GetWindowedPopupMoveAndResizeRect(popup);
		VERIFY_ARE_VERY_CLOSE(step2Rect.X - step1Rect.X, 10 * popup.XamlRoot.RasterizationScale, 1, "step 2 x shift");
		VERIFY_ARE_VERY_CLOSE(step2Rect.Y - step1Rect.Y, 10 * popup.XamlRoot.RasterizationScale, 1, "step 2 y shift");

		await RunOnUIThread(() =>
		{
			popup.IsOpen = false;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup.Translation = new Vector3(0, 0, 0);
			var projection = new PlaneProjection();
			projection.RotationY = 5;
			rectangle.Projection = projection;
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "3"); projection on the content: no window.
		VERIFY_IS_FALSE(PopupHelper.IsWindowed(popup));

		await RunOnUIThread(() =>
		{
			popup.IsOpen = false;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			rectangle.Projection = null;
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "1B"); back to a window at the step 1 position.
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));
		var step1BRect = PopupHelper.GetWindowedPopupMoveAndResizeRect(popup);
		VERIFY_ARE_VERY_CLOSE(step1BRect.X, step1Rect.X, 1, "step 1B x");
		VERIFY_ARE_VERY_CLOSE(step1BRect.Y, step1Rect.Y, 1, "step 1B y");

		await RunOnUIThread(() =>
		{
			popup.IsOpen = false;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var compositeTransform3D = new CompositeTransform3D();
			compositeTransform3D.RotationY = 5;
			popup.Transform3D = compositeTransform3D;
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "4"); Transform3D on the popup: no window.
		VERIFY_IS_FALSE(PopupHelper.IsWindowed(popup));

		await RunOnUIThread(() =>
		{
			popup.IsOpen = false;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup.Transform3D = null;
			rootPanel.Rotation = 5;
			rootPanel.RotationAxis = new Vector3(0, 1, 0);
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "5"); 3D rotation on an ancestor: no window.
		VERIFY_IS_FALSE(PopupHelper.IsWindowed(popup));
	}

	[TestMethod]
	[Ignore("Not applicable: runs over process DPI awareness contexts (Unaware, System, PerMonitor, PerMonitorV2, UnawareGdiScaled) via ChangeDPI and InitializeHost, which Uno has no equivalent of. Also ignored upstream (TODO: Move windowed popups to lifted input).")]
	public void WindowedPopupHighDPI()
	{
		// Upstream: with a windowed popup (50x50 transparent red Rectangle, ShouldConstrainToRootBounds = false) inside a
		// 150x150 Canvas at (100, 100) in the root, per DpiAwarenessContext:
		//  - tapping the popup content raises Tapped on the popup child, and not on the Canvas;
		//  - tapping the Canvas raises Tapped on the Canvas, and not on the popup child
		//    (skipped for UnawareGdiScaled because of an island rendering problem).
		// The masters (WindowedPopupHighDPI#metadataSet0-4) pin HwndLeft/HwndTop = 100,100 (125,125 for set 3).
	}

	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task ValidateLightDismissOverlayMode()
	{
		LOG_OUTPUT("Validate that the default is Off and light-dismissible there is no overlay element.");
		await RunOnUIThread(() =>
		{
			var popup = new Popup();
			popup.Child = new Grid();
			popup.IsLightDismissEnabled = true;

			VERIFY_ARE_EQUAL(popup.LightDismissOverlayMode, LightDismissOverlayMode.Off);
			VERIFY_IS_NULL(TestServices.Utilities.GetPopupOverlayElement(popup));
		});

		LOG_OUTPUT("Validate that when set to On and light-dismissible but no child there is no overlay element.");
		await RunOnUIThread(() =>
		{
			var popup = new Popup();
			popup.IsLightDismissEnabled = true;
			popup.LightDismissOverlayMode = LightDismissOverlayMode.On;

			VERIFY_IS_NULL(TestServices.Utilities.GetPopupOverlayElement(popup));
		});

		LOG_OUTPUT("Validate that when set to On but not light-dismissible, there is no overlay element.");
		await RunOnUIThread(() =>
		{
			var popup = new Popup();
			popup.Child = new Grid();
			popup.IsLightDismissEnabled = false;
			popup.LightDismissOverlayMode = LightDismissOverlayMode.On;

			VERIFY_IS_NULL(TestServices.Utilities.GetPopupOverlayElement(popup));
		});

		LOG_OUTPUT("Validate that when set to On and light-dismissible, there is an overlay element.");
		await RunOnUIThread(() =>
		{
			var popup = new Popup();
			popup.Child = new Grid();
			popup.IsLightDismissEnabled = true;
			popup.LightDismissOverlayMode = LightDismissOverlayMode.On;

			VERIFY_IS_NOT_NULL(TestServices.Utilities.GetPopupOverlayElement(popup));
		});

		if (!TestServices.Utilities.IsXBox)
		{
			LOG_OUTPUT("Validate that when set to Auto and not on Xbox, there is no overlay element.");
			await RunOnUIThread(() =>
			{
				var popup = new Popup();
				popup.Child = new Grid();
				popup.IsLightDismissEnabled = true;
				popup.LightDismissOverlayMode = LightDismissOverlayMode.Auto;

				VERIFY_IS_NULL(TestServices.Utilities.GetPopupOverlayElement(popup));
			});
		}
	}

	[TestMethod]
	[Ignore("Not applicable: Xbox only (LightDismissOverlayMode.Auto creates an overlay on Xbox). Ignored upstream. Validates that setting LightDismissOverlayMode to Auto causes an overlay to get created on Xbox.")]
	public void DoesAutoLightDismissOverlayModeCreateOverlayOnXbox()
	{
		// Upstream: new Popup { Child = new Grid(), IsLightDismissEnabled = true, LightDismissOverlayMode = Auto };
		// VERIFY_IS_NOT_NULL(TestServices::Utilities->GetPopupOverlayElement(popup));
	}

	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task ValidateDefaultOverlayBrush()
	{
		await RunOnUIThread(() =>
		{
			var expectedBrush = (SolidColorBrush)Application.Current.Resources["PopupLightDismissOverlayBackground"];

			var popup = new Popup();
			popup.Child = new Grid();
			popup.IsLightDismissEnabled = true;
			popup.LightDismissOverlayMode = LightDismissOverlayMode.On;

			var overlayElement = TestServices.Utilities.GetPopupOverlayElement(popup);
			THROW_IF_NULL_WITH_MSG(overlayElement, "An overlay element should exist for the popup.");

			var overlayRect = overlayElement as Rectangle;
			THROW_IF_NULL_WITH_MSG(overlayRect, "The overlay element should be a rectangle.");

			var overlayBrush = (SolidColorBrush)overlayRect.Fill;
			VERIFY_IS_NOT_NULL(overlayBrush);
			VERIFY_IS_TRUE(overlayBrush.Equals(expectedBrush));
		});
	}

	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task ValidateOverlayTreePlacement()
	{
		Popup testPopup = null;
		Popup otherPopup = null;

		await RunOnUIThread(() =>
		{
			// Set some content to let the test activate successfully.
			var rootGrid = new Grid();
			TestServices.WindowHelper.WindowContent = rootGrid;

			// Open a popup first to make sure the overlay gets inserted behind its
			// popup's child, but in front of other popup's child.
			otherPopup = new Popup();
			otherPopup.Child = new Grid();
			otherPopup.XamlRoot = rootGrid.XamlRoot;
			otherPopup.IsOpen = true;

			testPopup = new Popup();
			testPopup.Child = new Rectangle();
			testPopup.IsLightDismissEnabled = true;
			testPopup.XamlRoot = rootGrid.XamlRoot;
			testPopup.LightDismissOverlayMode = LightDismissOverlayMode.On;

			testPopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		DependencyObject popupRoot = null;
		await RunOnUIThread(() =>
		{
			var overlayElement = TestServices.Utilities.GetPopupOverlayElement(testPopup);
			VERIFY_IS_NOT_NULL(overlayElement);

			// If it has a visual parent, it's in the tree.
			popupRoot = VisualTreeHelper.GetParent(overlayElement);
			VERIFY_IS_NOT_NULL(popupRoot);

			// There should be both the popups' child elements as well as the test popup's
			// overlay element.
			var childCount = VisualTreeHelper.GetChildrenCount(popupRoot);
			VERIFY_ARE_EQUAL(childCount, 3);

			// The overlay element should be behind the test popup's child but in front of the other popup's
			// child so it should be the second child.
			var secondChild = (FrameworkElement)VisualTreeHelper.GetChild(popupRoot, 1);
			VERIFY_IS_TRUE(overlayElement.Equals(secondChild));

			testPopup.IsOpen = false;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// There should just be the child element from the other popup.
			var childCount = VisualTreeHelper.GetChildrenCount(popupRoot);
			VERIFY_ARE_EQUAL(childCount, 1);

			var child = (FrameworkElement)VisualTreeHelper.GetChild(popupRoot, 0);
			VERIFY_IS_TRUE(otherPopup.Child.Equals(child));
		});
	}

	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task CanChangeLightDismissOverlayModeWhileOpen()
	{
		Popup popup = null;

		await RunOnUIThread(() =>
		{
			popup = new Popup();
			popup.Child = new Grid();
			popup.IsLightDismissEnabled = true;
			popup.LightDismissOverlayMode = LightDismissOverlayMode.Off;

			popup.IsOpen = true;

			// Set some content to let the test activate successfully.
			TestServices.WindowHelper.WindowContent = new Grid();
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup.LightDismissOverlayMode = LightDismissOverlayMode.On;

			var overlayElement = TestServices.Utilities.GetPopupOverlayElement(popup);
			VERIFY_IS_NOT_NULL(overlayElement);

			// There should be 2 elements under the popup root.
			var popupRoot = VisualTreeHelper.GetParent(popup.Child);
			var childCount = VisualTreeHelper.GetChildrenCount(popupRoot);
			VERIFY_ARE_EQUAL(childCount, 2);

			// The overlay element should be behind the child, so it should be the first child.
			var firstChild = (FrameworkElement)VisualTreeHelper.GetChild(popupRoot, 0);
			VERIFY_IS_TRUE(overlayElement.Equals(firstChild));
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup.LightDismissOverlayMode = LightDismissOverlayMode.Off;

			var overlayElement = TestServices.Utilities.GetPopupOverlayElement(popup);
			VERIFY_IS_NULL(overlayElement);

			// There should be only 1 element under the popup root.
			var popupRoot = VisualTreeHelper.GetParent(popup.Child);
			var childCount = VisualTreeHelper.GetChildrenCount(popupRoot);
			VERIFY_ARE_EQUAL(childCount, 1);

			// The remaining child should not be the overlay element.
			var child = (FrameworkElement)VisualTreeHelper.GetChild(popupRoot, 0);
			VERIFY_IS_TRUE(popup.Child.Equals(child));
		});
	}

	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task DoesOverlaySizeWithWindow()
	{
		Popup popup = null;

		await RunOnUIThread(() =>
		{
			popup = new Popup();
			popup.Child = new Grid();
			popup.IsLightDismissEnabled = true;
			popup.LightDismissOverlayMode = LightDismissOverlayMode.On;

			popup.IsOpen = true;

			// Set some content to let the test activate successfully.
			var rootGrid = new Grid();
			TestServices.WindowHelper.WindowContent = rootGrid;
		});
		await TestServices.WindowHelper.WaitForIdle();

		var windowBounds = new Rect();
		await RunOnUIThread(() =>
		{
			var overlayElement = TestServices.Utilities.GetPopupOverlayElement(popup);
			THROW_IF_NULL_WITH_MSG(overlayElement, "An overlay element should exist for the popup.");

			// The overlay element should be sized to the window.
			windowBounds = TestServices.WindowHelper.WindowBounds;
			VERIFY_ARE_EQUAL(overlayElement.ActualWidth, windowBounds.Width);
			VERIFY_ARE_EQUAL(overlayElement.ActualHeight, windowBounds.Height);
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Re-size the window so that we can re-check the overlay element's size.
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(windowBounds.Width * 0.5f, windowBounds.Height * 0.5f));
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var overlayElement = TestServices.Utilities.GetPopupOverlayElement(popup);
			THROW_IF_NULL_WITH_MSG(overlayElement, "An overlay element should exist for the popup.");

			// The overlay element should be sized to the window.
			windowBounds = TestServices.WindowHelper.WindowBounds;
			VERIFY_ARE_EQUAL(overlayElement.ActualWidth, windowBounds.Width);
			VERIFY_ARE_EQUAL(overlayElement.ActualHeight, windowBounds.Height);
		});
	}

	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task CanSetChildWhileOpenWithOverlayEnabled()
	{
		Popup popup = null;

		await RunOnUIThread(() =>
		{
			// Set some content to let the test activate successfully.
			var rootGrid = new Grid();
			TestServices.WindowHelper.WindowContent = rootGrid;

			popup = new Popup();
			popup.IsLightDismissEnabled = true;
			popup.LightDismissOverlayMode = LightDismissOverlayMode.On;

			popup.XamlRoot = rootGrid.XamlRoot;
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup.Child = new Grid();

			var overlayElement = TestServices.Utilities.GetPopupOverlayElement(popup);
			VERIFY_IS_NOT_NULL(overlayElement);

			// There should be 2 elements under the popup root.
			var popupRoot = VisualTreeHelper.GetParent(popup.Child);
			var childCount = VisualTreeHelper.GetChildrenCount(popupRoot);
			VERIFY_ARE_EQUAL(childCount, 2);

			// The overlay element should be behind the child, so it should be the first child.
			var firstChild = (FrameworkElement)VisualTreeHelper.GetChild(popupRoot, 0);
			VERIFY_IS_TRUE(overlayElement.Equals(firstChild));
		});
	}

	// Upstream compares the DComp tree. The masters pin the overlay: a window-sized sprite filled with 60% black in
	// the dark theme, 60% white in the light theme and navy (0, 0, 127) in high contrast.
	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task ValidateOverlayDCompTree()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 400));

		Grid root = null;
		Popup popup = null;

		await RunOnUIThread(() =>
		{
			// Use a parented popup so that the requested theme from the root element can
			// propogate to it.
			root = (Grid)XamlReader.Load(
				@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
						Background=""{ThemeResource SystemControlBackgroundAltHighBrush}"" >
					   <Popup x:Name=""popup"" IsLightDismissEnabled=""True"" LightDismissOverlayMode=""On"">
							<Grid/>
						</Popup>
					</Grid>");

			popup = (Popup)root.FindName("popup");

			// Uno: the test app theme is not guaranteed to be Dark, which the first step relies on upstream.
			root.RequestedTheme = ElementTheme.Dark;

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Validate the dark theme of the overlay.");
		await RunOnUIThread(() =>
		{
			VerifyOverlayFill(popup, Windows.UI.Color.FromArgb(0x99, 0x00, 0x00, 0x00));
		});

		LOG_OUTPUT("Validate the light theme of the overlay.");
		await RunOnUIThread(() =>
		{
			root.RequestedTheme = ElementTheme.Light;
		});
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			VerifyOverlayFill(popup, Windows.UI.Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF));
		});

#if false // TODO Uno: needs TestServices.ThemingHelper.HighContrastTheme
		LOG_OUTPUT("Validate the high-contrast theme of the overlay.");
		await RunOnUIThread(() =>
		{
			TestServices.ThemingHelper.HighContrastTheme = HighContrastTheme.Test;
		});
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			VerifyOverlayFill(popup, Windows.UI.Color.FromArgb(0xFF, 0x00, 0x00, 0x7F));
		});
#endif
	}

	private static void VerifyOverlayFill(Popup popup, Windows.UI.Color expectedColor)
	{
		var overlayElement = TestServices.Utilities.GetPopupOverlayElement(popup);
		THROW_IF_NULL_WITH_MSG(overlayElement, "An overlay element should exist for the popup.");

		var overlayRect = overlayElement as Rectangle;
		THROW_IF_NULL_WITH_MSG(overlayRect, "The overlay element should be a rectangle.");

		var fill = overlayRect.Fill as SolidColorBrush;
		THROW_IF_NULL_WITH_MSG(fill, "The overlay should be filled with a SolidColorBrush.");
		VERIFY_ARE_EQUAL(fill.Color, expectedColor);
	}

	// Upstream compares the UIElement tree dump. The masters pin: the root Grid and the Popup fill the 400x400 window,
	// the popup's child Grid is 0x0 and parented to the Popup, and under the popup root there is a window-sized
	// Rectangle overlay filled with #99000000 (dark) / #99FFFFFF (light, RequestedTheme = Light on the root).
	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task ValidateOverlayUIETree()
	{
		Grid root = null;
		Popup popup = null;

		await RunOnUIThread(() =>
		{
			// Use a parented popup so that the requested theme from the root element can
			// propogate to it.
			root = (Grid)XamlReader.Load(
				@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
					Background=""{ThemeResource SystemControlBackgroundAltHighBrush}"" >
				   <Popup x:Name=""popup"" IsOpen=""True"" IsLightDismissEnabled=""True"" LightDismissOverlayMode=""On"">
						<Grid/>
					</Popup>
				</Grid>");
			popup = (Popup)root.FindName("popup");

			// Uno: the test app theme is not guaranteed to be Dark, which the Dark master relies on.
			root.RequestedTheme = ElementTheme.Dark;

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(popup.IsOpen);
			VERIFY_IS_TRUE(popup.IsLightDismissEnabled);
			VERIFY_ARE_EQUAL(popup.LightDismissOverlayMode, LightDismissOverlayMode.On);

			VerifyOverlayUIETree(root, popup, Windows.UI.Color.FromArgb(0x99, 0x00, 0x00, 0x00));
		});

		await RunOnUIThread(() =>
		{
			root.RequestedTheme = ElementTheme.Light;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VerifyOverlayUIETree(root, popup, Windows.UI.Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF));
		});

#if false // TODO Uno: needs TestServices.ThemingHelper.HighContrastTheme (the HC master)
		await RunOnUIThread(() =>
		{
			TestServices.ThemingHelper.HighContrastTheme = HighContrastTheme.Test;
		});
		await TestServices.WindowHelper.WaitForIdle();
#endif
	}

	private static void VerifyOverlayUIETree(Grid root, Popup popup, Windows.UI.Color expectedOverlayColor)
	{
		var windowBounds = TestServices.WindowHelper.WindowBounds;

		// The root Grid and the Popup fill the window (400x400 in the masters).
		VERIFY_ARE_EQUAL(root.ActualWidth, windowBounds.Width);
		VERIFY_ARE_EQUAL(root.ActualHeight, windowBounds.Height);
		VERIFY_ARE_EQUAL(popup.ActualWidth, windowBounds.Width);
		VERIFY_ARE_EQUAL(popup.ActualHeight, windowBounds.Height);

		// The popup's child has no size and is parented to the popup.
		var child = (FrameworkElement)popup.Child;
		VERIFY_ARE_EQUAL(child.ActualWidth, 0d);
		VERIFY_ARE_EQUAL(child.ActualHeight, 0d);
		VERIFY_IS_TRUE(child.Parent == popup);

		// The overlay is a window-sized Rectangle parented to the popup, living with the popup's child under the popup root.
		var overlayElement = TestServices.Utilities.GetPopupOverlayElement(popup);
		THROW_IF_NULL_WITH_MSG(overlayElement, "An overlay element should exist for the popup.");
		var overlayRect = overlayElement as Rectangle;
		THROW_IF_NULL_WITH_MSG(overlayRect, "The overlay element should be a rectangle.");

		VERIFY_ARE_EQUAL(overlayRect.ActualWidth, windowBounds.Width);
		VERIFY_ARE_EQUAL(overlayRect.ActualHeight, windowBounds.Height);
		VERIFY_IS_TRUE(overlayRect.Parent == popup);
		VERIFY_IS_TRUE(VisualTreeHelper.GetParent(overlayRect) == VisualTreeHelper.GetParent(child));

		var fill = overlayRect.Fill as SolidColorBrush;
		THROW_IF_NULL_WITH_MSG(fill, "The overlay should be filled with a SolidColorBrush.");
		VERIFY_ARE_EQUAL(fill.Color, expectedOverlayColor);
	}

	private async Task WindowedPopupOpenAndCloseHelper()
	{
		Popup popup = null;
		StackPanel rootPanel = null;

		var popupOpenedEvent = new Event();
		var popupClosedEvent = new Event();

		var openedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Opened));
		var closedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Closed));

		await RunOnUIThread(() =>
		{
			rootPanel = (StackPanel)XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='LayoutRoot' Background='Red' Width='400' Height='400' > \r\n" +
				"</StackPanel>");

			VERIFY_IS_NOT_NULL(rootPanel);
			TestServices.WindowHelper.WindowContent = rootPanel;

		});
		// The transition from splash screen to 1st Xaml causes a PositionChaged event
		// on the main app window, which would cause any windowed popups to close.
		// Therefore, defer opening the popup until first Xaml frame is drawn.
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup = (Popup)XamlReader.Load(
				"<Popup xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' HorizontalAlignment='Left'> " +
				"  <StackPanel Background='Green' Width='200' Height='200'> \r\n" +
				"    <Button Content='ButtonInPopup' /> \r\n" +
				"  </StackPanel> \r\n" +
				"</Popup>");
			VERIFY_IS_NOT_NULL(popup);
			popup.VerticalOffset = 10;
			popup.HorizontalOffset = -50;
			PopupHelper.SetWindowed(popup);

			openedRegistration.Attach(popup, (s, e) =>
			{
				LOG_OUTPUT("WindowedPopupOpenAndClose: Windowed popup: Open event fired");
				popupOpenedEvent.Set();
			});

			closedRegistration.Attach(popup, (s, e) =>
			{
				LOG_OUTPUT("WindowedPopupOpenAndClose: Windowed popup: Closed event fired");
				popupClosedEvent.Set();
			});

			popup.XamlRoot = rootPanel.XamlRoot;
			LOG_OUTPUT("WindowedPopupOpenAndClose: Set popup->IsOpen = true");
			popup.IsOpen = true;
		});

		await popupOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Upstream: VerifyMockDCompOutput(NoComparison); master HwndLeft = -60, HwndTop = 31, 220x220 (200 + gutters).
		VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));
		PopupHelper.VerifyWindowedPopupRect(popup, -60, 0, 220, 220);

		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(popup.IsOpen);
			LOG_OUTPUT("WindowedPopupOpenAndClose: Set popup->IsOpen = false");
			popup.IsOpen = false;
		});

		await popupClosedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(popup.IsOpen);
		});
	}

	// The masters pin whether the 200x200 red Rectangle is rendered: yes when un-collapsed, and the tree is empty while
	// the parent is collapsed.
	[TestMethod]
	[Ignore("Pending Popup port")]
	public async Task OpenPopupUnderCollapsedParent()
	{
		Canvas rootPanel = null;
		Grid grid = null;
		Popup popup = null;

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Loading XAML");
			rootPanel = (Canvas)XamlReader.Load(
				"<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' HorizontalAlignment='Left'> " +
				"  <Grid x:Name='myGrid' Width='200' Height='200'> \r\n" +
				"    <Popup x:Name='myPopup' IsOpen='False'> \r\n" +
				"      <Rectangle Width='200' Height='200' Fill='Red'/> \r\n" +
				"    </Popup> \r\n" +
				"  </Grid> \r\n" +
				"</Canvas>");
			VERIFY_IS_NOT_NULL(rootPanel);
			grid = (Grid)rootPanel.FindName("myGrid");
			popup = (Popup)rootPanel.FindName("myPopup");

			TestServices.WindowHelper.WindowContent = rootPanel;

			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "Uncollapsed")
		await VerifyCollapsedParentPopupRendering(popup, isRendered: true);

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Collapsing parent");
			grid.Visibility = Visibility.Collapsed;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "Collapsed")
		await VerifyCollapsedParentPopupRendering(popup, isRendered: false);

		await RunOnUIThread(() =>
		{
			// This is the test case that verifies the fix - when un-collapsing the parent, the Popup should now render.
			LOG_OUTPUT("Un-collapsing parent.  Verify the Popup renders");
			grid.Visibility = Visibility.Visible;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "Uncollapsed")
		await VerifyCollapsedParentPopupRendering(popup, isRendered: true);
	}

	private static async Task VerifyCollapsedParentPopupRendering(Popup popup, bool isRendered)
	{
		var child = (FrameworkElement)popup.Child;

		if (isRendered)
		{
			// The master has a single 200x200 red sprite at the origin of the popup's parent.
			var bounds = await ControlHelper.GetBounds(child);
			VERIFY_ARE_VERY_CLOSE(bounds.Width, 200, 1, "popup child width");
			VERIFY_ARE_VERY_CLOSE(bounds.Height, 200, 1, "popup child height");

			var bitmap = await UITestHelper.ScreenShot(child);
			ImageAssert.HasColorAt(bitmap, 100, 100, Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x00, 0x00), tolerance: 5);
		}
		else
		{
			// The master has an empty content visual: nothing of the popup is rendered, and nothing is laid out.
			VERIFY_ARE_EQUAL(child.ActualWidth, 0d);
			VERIFY_ARE_EQUAL(child.ActualHeight, 0d);
		}
	}

	[TestMethod]
	[Ignore("Not applicable: only compares DComp surfaces of a custom XamlCompositionBrushBase rendered through RenderTargetBitmap; ignored upstream ([DCPP-test] Delete TIE and HostBackdrop tests). Ensure a Popup with HostBackdropBrush renders correctly.")]
	public void HostBackdropBrush()
	{
		// Upstream: HostBackdropBrushHelper(isWindowed: false, useLTE: false)
	}

	[TestMethod]
	[Ignore("Not applicable: only compares DComp surfaces of a custom XamlCompositionBrushBase rendered through RenderTargetBitmap; ignored upstream ([DCPP-test] Delete TIE and HostBackdrop tests). Ensure a Windowed Popup with HostBackdropBrush renders correctly.")]
	public void HostBackdropBrushWindowed()
	{
		// Upstream: HostBackdropBrushHelper(isWindowed: true, useLTE: false)
	}

	[TestMethod]
	[Ignore("Not applicable: only compares DComp surfaces of a custom XamlCompositionBrushBase rendered through RenderTargetBitmap, with a LayoutTransitionElement (AddTestLTE) in the PopupRoot; ignored upstream ([DCPP-test] Delete TIE and HostBackdrop tests). Ensure a Popup with HostBackdropBrush and targeted by LTE renders correctly.")]
	public void HostBackdropBrushLTE()
	{
		// Upstream: HostBackdropBrushHelper(isWindowed: false, useLTE: true)
	}

	[TestMethod]
	[Ignore("Not applicable: only compares the DComp tree through a post-tick callback after ApplyElevationEffect (implicit animations); ignored upstream ([DCPP-test] Delete TIE and HostBackdrop tests). Apply elevation effect via Translation property.")]
	public void ElevationTranslate()
	{
		// Upstream: ElevationHelper(animate: true)
	}

	[TestMethod]
	[Ignore("Not applicable: only compares the DComp tree through a post-tick callback after ApplyElevationEffect, with DisableGlobalAnimations; ignored upstream ([DCPP-test] Delete TIE and HostBackdrop tests). Apply elevation effect via Translation property, no animation.")]
	public void ElevationTranslateNoAnim()
	{
		// Upstream: ElevationHelper(animate: false)
	}
}
