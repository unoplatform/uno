#if HAS_UNO

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

using static Private.Infrastructure.TestServices;
using Uno.UI.RuntimeTests;

namespace Windows.UI.Tests.Enterprise
{
	[TestClass]
	[RequiresFullWindow]
	public class SplitViewIntegrationTests : BaseDxamlTestClass
	{
		[ClassInitialize]
		public static void ClassSetup()
		{
			CommonTestSetupHelper.CommonTestClassSetup();
		}

		[ClassCleanup]
		public static void TestCleanup()
		{
			TestServices.WindowHelper.ShutdownXaml();
		}

		//
		// Test Cases
		//

		// Validates that we can successfully create a SplitView.
		[TestMethod]
		public async Task CanInstantiate()
		{
			SplitView splitView = null;

			await RunOnUIThread(() =>
			{
				splitView = new SplitView();
			});

			VERIFY_IS_TRUE(splitView != null);
		}

		// Validates that we can successfully add/remove a SplitView from the live tree.
		[TestMethod]
		public async Task CanEnterAndLeaveLiveTree()
		{
			SplitView splitView = null;

			await RunOnUIThread(() =>
			{
				splitView = new SplitView();
				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				TestServices.WindowHelper.WindowContent = null;
			});
			await TestServices.WindowHelper.WaitForIdle();
		}

		// Validates that the SplitView control Overlay and CompactOverlay modes support light dismiss,
		// whereas Inline and CompactInline modes do not.
		[TestMethod]
		public async Task ValidateLightDismissBehavior_Overlay_Left_LTR()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Left, FlowDirection.LeftToRight, shouldLightDismiss: true);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_Overlay_Right_LTR()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Right, FlowDirection.LeftToRight, shouldLightDismiss: true);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_CompactOverlay_Left_LTR()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Left, FlowDirection.LeftToRight, shouldLightDismiss: true);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_CompactOverlay_Right_LTR()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Right, FlowDirection.LeftToRight, shouldLightDismiss: true);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_Inline_Left_LTR()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Left, FlowDirection.LeftToRight, shouldLightDismiss: false);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_Inline_Right_LTR()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Right, FlowDirection.LeftToRight, shouldLightDismiss: false);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_CompactInline_Left_LTR()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Left, FlowDirection.LeftToRight, shouldLightDismiss: false);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_CompactInline_Right_LTR()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Right, FlowDirection.LeftToRight, shouldLightDismiss: false);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_Overlay_Left_RTL()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Left, FlowDirection.RightToLeft, shouldLightDismiss: true);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_Overlay_Right_RTL()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Right, FlowDirection.RightToLeft, shouldLightDismiss: true);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_CompactOverlay_Left_RTL()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Left, FlowDirection.RightToLeft, shouldLightDismiss: true);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_CompactOverlay_Right_RTL()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Right, FlowDirection.RightToLeft, shouldLightDismiss: true);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_Inline_Left_RTL()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Left, FlowDirection.RightToLeft, shouldLightDismiss: false);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_Inline_Right_RTL()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Right, FlowDirection.RightToLeft, shouldLightDismiss: false);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_CompactInline_Left_RTL()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Left, FlowDirection.RightToLeft, shouldLightDismiss: false);
		}

		[TestMethod]
		public async Task ValidateLightDismissBehavior_CompactInline_Right_RTL()
		{
			await ValidateLightDismissBehaviorWorker(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Right, FlowDirection.RightToLeft, shouldLightDismiss: false);
		}

		private async Task ValidateLightDismissBehaviorWorker(
			SplitViewDisplayMode displayMode,
			SplitViewPanePlacement placement,
			FlowDirection flowDirection,
			bool shouldLightDismiss)
		{
			var expectedIsOpenValue = !shouldLightDismiss;

			FrameworkElement rootPanel = null;
			SplitView splitView = null;
			FrameworkElement contentElement = null;
			FrameworkElement outsideElement = null;

			await RunOnUIThread(() =>
			{
				rootPanel = (FrameworkElement)XamlReader.Load(
					"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"    <SplitView x:Name='splitView' Width='400' Height='400' OpenPaneLength='100'>" +
					"        <SplitView.Pane>" +
					"            <Border Background='Orange'>" +
					"                <Button/>" +
					"            </Border>" +
					"        </SplitView.Pane>" +
					"        <Rectangle x:Name='contentElement' Fill='Purple'/>" +
					"    </SplitView>" +
					"    <Border Width='400' Height='50' Background='GreenYellow'>" +
					"        <Button x:Name='outsideElement'/>" +
					"    </Border>" +
					"</StackPanel>");

				rootPanel.FlowDirection = flowDirection;

				splitView = (SplitView)rootPanel.FindName("splitView");
				contentElement = (FrameworkElement)rootPanel.FindName("contentElement");
				outsideElement = (FrameworkElement)rootPanel.FindName("outsideElement");

				splitView.DisplayMode = displayMode;
				splitView.PanePlacement = placement;

				TestServices.WindowHelper.WindowContent = rootPanel;
			});
			await TestServices.WindowHelper.WaitForIdle();

			// Test tapping outside of pane area, but within the splitview control.
			{
				await RunOnUIThread(() => { splitView.IsPaneOpen = true; });
				await TestServices.WindowHelper.WaitForIdle();

				TestServices.InputHelper.Tap(contentElement);
				await TestServices.WindowHelper.WaitForIdle();

				bool isOpen = false;
				await RunOnUIThread(() => { isOpen = splitView.IsPaneOpen; });
				VERIFY_ARE_EQUAL(isOpen, expectedIsOpenValue);
			}

			// Test tapping outside of splitview control.
			{
				await RunOnUIThread(() => { splitView.IsPaneOpen = true; });
				await TestServices.WindowHelper.WaitForIdle();

				TestServices.InputHelper.Tap(outsideElement);
				await TestServices.WindowHelper.WaitForIdle();

				bool isOpen = false;
				await RunOnUIThread(() => { isOpen = splitView.IsPaneOpen; });
				VERIFY_ARE_EQUAL(isOpen, expectedIsOpenValue);
			}

			// TODO Uno: Original C++ test also verifies:
			// - ESC key press (TestServices::KeyboardHelper->Escape())
			// - Gamepad B button (TestServices::KeyboardHelper->GamepadB())
			// - Control size change
			// - Back button press (TestServices::Utilities->InjectBackButtonPress())
			// - Window resize (TestServices::WindowHelper->SetDesktopWindowSize())
			// These require keyboard/gamepad input injection and window resize helpers
			// that are not yet available in Uno Platform runtime tests.
		}

		// Verifies that app authors can set the compact length of the pane.
		[TestMethod]
		public async Task CanSetCompactPaneLengthProperty()
		{
			SplitView splitView = null;

			const double splitViewWidth = 400.0;
			const double compactPaneLength = 85.0;
			const double expectedContentAreaWidth = splitViewWidth - compactPaneLength;

			await RunOnUIThread(() =>
			{
				splitView = new SplitView();

				splitView.DisplayMode = SplitViewDisplayMode.CompactOverlay;
				splitView.Width = splitViewWidth;
				splitView.CompactPaneLength = compactPaneLength;

				splitView.Content = new Rectangle();

				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(((FrameworkElement)splitView.Content).ActualWidth, expectedContentAreaWidth);
			});
		}

		// Validates that we're not excessively re-laying out our content when transitioning between states.
		[TestMethod]
		public async Task ValidateElementResizeCountForTransitions_Overlay_Left()
		{
			await ValidateElementResizeCountForTransitionsWorker(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Left, 0, 0, 1, 0);
		}

		[TestMethod]
		public async Task ValidateElementResizeCountForTransitions_Overlay_Right()
		{
			await ValidateElementResizeCountForTransitionsWorker(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Right, 0, 0, 1, 0);
		}

		[TestMethod]
		public async Task ValidateElementResizeCountForTransitions_Inline_Left()
		{
			await ValidateElementResizeCountForTransitionsWorker(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Left, 1, 1, 1, 0);
		}

		[TestMethod]
		public async Task ValidateElementResizeCountForTransitions_Inline_Right()
		{
			await ValidateElementResizeCountForTransitionsWorker(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Right, 1, 1, 1, 0);
		}

		[TestMethod]
		public async Task ValidateElementResizeCountForTransitions_CompactOverlay_Left()
		{
			await ValidateElementResizeCountForTransitionsWorker(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Left, 0, 0, 0, 0);
		}

		[TestMethod]
		public async Task ValidateElementResizeCountForTransitions_CompactOverlay_Right()
		{
			await ValidateElementResizeCountForTransitionsWorker(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Right, 0, 0, 0, 0);
		}

		[TestMethod]
		public async Task ValidateElementResizeCountForTransitions_CompactInline_Left()
		{
			await ValidateElementResizeCountForTransitionsWorker(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Left, 1, 1, 0, 0);
		}

		[TestMethod]
		public async Task ValidateElementResizeCountForTransitions_CompactInline_Right()
		{
			await ValidateElementResizeCountForTransitionsWorker(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Right, 1, 1, 0, 0);
		}

		private async Task ValidateElementResizeCountForTransitionsWorker(
			SplitViewDisplayMode displayMode,
			SplitViewPanePlacement placement,
			int expectedContentAreaClosedToOpenCount,
			int expectedContentAreaOpenToClosedCount,
			int expectedPaneAreaClosedToOpenCount,
			int expectedPaneAreaOpenToClosedCount)
		{
			SplitView splitView = null;

			int contentAreaSizeChangedCount = 0;
			int paneAreaSizeChangedCount = 0;

			await RunOnUIThread(() =>
			{
				splitView = (SplitView)XamlReader.Load(
					"<SplitView xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"    <SplitView.Pane>" +
					"        <Rectangle x:Name='paneElement' Fill='Orange'/>" +
					"    </SplitView.Pane>" +
					"    <Rectangle x:Name='contentElement' Fill='Purple'/>" +
					"</SplitView>");

				splitView.DisplayMode = displayMode;
				splitView.PanePlacement = placement;

				var contentElement = (FrameworkElement)splitView.FindName("contentElement");
				var paneElement = (FrameworkElement)splitView.FindName("paneElement");

				contentElement.SizeChanged += (s, e) => contentAreaSizeChangedCount++;
				paneElement.SizeChanged += (s, e) => paneAreaSizeChangedCount++;

				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			// Reset to account for initial layout.
			contentAreaSizeChangedCount = 0;
			paneAreaSizeChangedCount = 0;

			// Go from closed to opened.
			await RunOnUIThread(() => { splitView.IsPaneOpen = true; });
			await TestServices.WindowHelper.WaitForIdle();

			VERIFY_ARE_EQUAL(contentAreaSizeChangedCount, expectedContentAreaClosedToOpenCount);
			VERIFY_ARE_EQUAL(paneAreaSizeChangedCount, expectedPaneAreaClosedToOpenCount);

			// Reset for next transition.
			contentAreaSizeChangedCount = 0;
			paneAreaSizeChangedCount = 0;

			// Go from opened to closed.
			await RunOnUIThread(() => { splitView.IsPaneOpen = false; });
			await TestServices.WindowHelper.WaitForIdle();

			VERIFY_ARE_EQUAL(contentAreaSizeChangedCount, expectedContentAreaOpenToClosedCount);
			VERIFY_ARE_EQUAL(paneAreaSizeChangedCount, expectedPaneAreaOpenToClosedCount);
		}

		// Verifies that the opening, opened, closing, closed events fire when changing IsPaneOpen.
		[TestMethod]
		public async Task DoesFireEventsWhenOpeningOrClosingPane()
		{
			SplitView splitView = null;
			string eventOrder = "";

			var openingEvent = new Event();
			var openedEvent = new Event();
			var closingEvent = new Event();
			var closedEvent = new Event();

			await RunOnUIThread(() =>
			{
				splitView = new SplitView();

				splitView.PaneOpening += (s, e) =>
				{
					eventOrder += "[Opening]";
					openingEvent.Set();
				};

				splitView.PaneOpened += (s, e) =>
				{
					eventOrder += "[Opened]";
					openedEvent.Set();
				};

				splitView.PaneClosing += (s, e) =>
				{
					eventOrder += "[Closing]";
					closingEvent.Set();
				};

				splitView.PaneClosed += (s, e) =>
				{
					eventOrder += "[Closed]";
					closedEvent.Set();
				};

				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Open the pane and validate the Opening & Opened events fire.");
			await RunOnUIThread(() =>
			{
				splitView.IsPaneOpen = true;
			});
			await TestServices.WindowHelper.WaitForIdle();
			await openingEvent.WaitForDefault();
			await openedEvent.WaitForDefault();

			LOG_OUTPUT("Close the pane by using a size-change to trigger a light dismiss and validate that Closing & Closed events fire.");
			await RunOnUIThread(() =>
			{
				splitView.Width = splitView.ActualWidth * 0.80;
			});
			await TestServices.WindowHelper.WaitForIdle();
			await closingEvent.WaitForDefault();
			await closedEvent.WaitForDefault();

			VERIFY_ARE_EQUAL(eventOrder, "[Opening][Opened][Closing][Closed]");
		}

		// Verifies that app authors can cancel a pane that is closing.
		[TestMethod]
		public async Task CanCancelPaneClosing()
		{
			SplitView splitView = null;

			var closingEvent = new Event();

			await RunOnUIThread(() =>
			{
				splitView = new SplitView();
				splitView.IsPaneOpen = true;

				splitView.PaneClosing += (s, e) =>
				{
					e.Cancel = true;
					closingEvent.Set();
				};

				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			// Change the size of the control to trigger a light dismiss.
			await RunOnUIThread(() =>
			{
				splitView.Width = splitView.ActualWidth * 0.80;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await closingEvent.WaitForDefault();

			// Our closing handler should have canceled it.
			await RunOnUIThread(() => { VERIFY_IS_TRUE(splitView.IsPaneOpen); });
		}

		// Verifies that SplitView theme resources are available.
		[TestMethod]
		public async Task DoSplitViewThemeResourcesExist()
		{
			await RunOnUIThread(() =>
			{
				var openPaneLengthThemeResource = Application.Current.Resources["SplitViewOpenPaneThemeLength"];
				var compactPaneLengthThemeResource = Application.Current.Resources["SplitViewCompactPaneThemeLength"];

				VERIFY_IS_TRUE(openPaneLengthThemeResource != null);
				VERIFY_IS_TRUE(compactPaneLengthThemeResource != null);

				VERIFY_ARE_EQUAL((double)openPaneLengthThemeResource, 320.0);
				VERIFY_ARE_EQUAL((double)compactPaneLengthThemeResource, 48.0);
			});
		}

		// Verifies that focus can be restored & lost to a hyperlink.
		[TestMethod]
		public async Task DoesFocusWorkWithHyperlink()
		{
			SplitView splitView = null;
			Microsoft.UI.Xaml.Documents.Hyperlink hyperlink = null;
			Button paneButton = null;

			var openedEvent = new Event();
			var closedEvent = new Event();

			await RunOnUIThread(() =>
			{
				var root = (Grid)XamlReader.Load(
					"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"    <SplitView x:Name='splitView' PaneBackground='Orange'>" +
					"        <SplitView.Pane>" +
					"            <Button x:Name='paneButton' Content='Some button'/>" +
					"        </SplitView.Pane>" +
					"        <Rectangle Fill='Purple'/>" +
					"    </SplitView>" +
					"    <TextBlock Width='100' Height='25' HorizontalAlignment='Center' VerticalAlignment='Center'>" +
					"        <Hyperlink x:Name='hyperlink'>Some Hyperlink!</Hyperlink>" +
					"    </TextBlock>" +
					"</Grid>");

				splitView = (SplitView)root.FindName("splitView");
				hyperlink = (Microsoft.UI.Xaml.Documents.Hyperlink)root.FindName("hyperlink");
				paneButton = (Button)root.FindName("paneButton");

				splitView.PaneOpened += (s, e) => openedEvent.Set();
				splitView.PaneClosed += (s, e) => closedEvent.Set();

				TestServices.WindowHelper.WindowContent = root;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				hyperlink.Focus(FocusState.Keyboard);
			});
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Open the pane to steal focus from the hyperlink.");
			await RunOnUIThread(() => { splitView.IsPaneOpen = true; });
			await openedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// Focus should be on the pane button now.
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(paneButton));

				LOG_OUTPUT("Close the pane to restore focus back to the hyperlink.");
				splitView.IsPaneOpen = false;
			});
			await closedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			// Focus should be back on the hyperlink now.
			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(hyperlink));
			});
		}

		// Verifies that there is no crash when you try to focus the content area
		// of an inline splitview after it opens.
		[TestMethod]
		public async Task CanFocusInlineSplitViewContentAreaAfterOpening()
		{
			SplitView splitView = null;
			FrameworkElement contentElement = null;
			Button contentButton = null;

			await RunOnUIThread(() =>
			{
				splitView = (SplitView)XamlReader.Load(
					"<SplitView xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
					"           DisplayMode='Inline'>" +
					"    <SplitView.Pane>" +
					"        <Rectangle Fill='Orange'/>" +
					"    </SplitView.Pane>" +
					"    <Grid x:Name='ContentElement' Background='Purple'>" +
					"        <Button x:Name='ContentButton' VerticalAlignment='Top'/>" +
					"    </Grid>" +
					"</SplitView>");

				contentElement = (FrameworkElement)splitView.FindName("ContentElement");
				contentButton = (Button)splitView.FindName("ContentButton");

				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() => { contentButton.Focus(FocusState.Keyboard); });
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() => { splitView.IsPaneOpen = true; });
			await TestServices.WindowHelper.WaitForIdle();

			// Tap on the content element to move focus out of the pane.
			TestServices.InputHelper.Tap(contentElement);
			await TestServices.WindowHelper.WaitForIdle();
		}

		// Verifies that the OpenPaneLength property supports being set to 'Auto'.
		[TestMethod]
		public async Task CanSetOpenPaneLengthToAuto()
		{
			FrameworkElement contentElement = null;

			const double splitViewWidth = 500;
			const double paneElementWidth = 200;
			const double expectedContentElementWidth = splitViewWidth - paneElementWidth;

			await RunOnUIThread(() =>
			{
				var splitView = (SplitView)XamlReader.Load(
					"<SplitView xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
					"           IsPaneOpen='True' DisplayMode='Inline' OpenPaneLength='Auto'>" +
					"    <SplitView.Pane>" +
					"        <Rectangle x:Name='PaneElement' Fill='Orange'/>" +
					"    </SplitView.Pane>" +
					"    <Rectangle x:Name='ContentElement' Fill='Purple'/>" +
					"</SplitView>");

				var paneElement = (FrameworkElement)splitView.FindName("PaneElement");
				contentElement = (FrameworkElement)splitView.FindName("ContentElement");

				splitView.Width = splitViewWidth;
				paneElement.Width = paneElementWidth;

				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(contentElement.ActualWidth, expectedContentElementWidth);
			});
		}

		// Verifies that a SplitView measured with infinite width is still tappable and doesn't crash.
		[TestMethod]
		public async Task CanTouchSplitViewWithInfiniteWidth()
		{
			Button button = null;

			var clickEvent = new Event();

			await RunOnUIThread(() =>
			{
				var root = (FrameworkElement)XamlReader.Load(
					"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
					"      Background='{ThemeResource ApplicationPageBackgroundThemeBrush}'>" +
					"    <Grid.ColumnDefinitions>" +
					"        <ColumnDefinition Width='48'/>" +
					"        <ColumnDefinition Width='Auto'/>" +
					"    </Grid.ColumnDefinitions>" +
					"    <SplitView DisplayMode='CompactOverlay' Grid.ColumnSpan='2'>" +
					"        <SplitView.Pane>" +
					"            <Grid Background='Orange'>" +
					"                <Button x:Name='TapTarget' Content='button'/>" +
					"            </Grid>" +
					"        </SplitView.Pane>" +
					"    </SplitView>" +
					"</Grid>");

				button = (Button)root.FindName("TapTarget");
				button.Click += (s, e) => clickEvent.Set();

				TestServices.WindowHelper.WindowContent = root;
			});
			await TestServices.WindowHelper.WaitForIdle();

			TestServices.InputHelper.Tap(button);
			await clickEvent.WaitForDefault();
		}

		// Verifies that the SplitView fires the Pane closing event when closed programmatically.
		[TestMethod]
		public async Task DoesFirePaneClosingEventWhenClosedProgrammatically()
		{
			SplitView splitView = null;

			var closingEvent = new Event();

			await RunOnUIThread(() =>
			{
				splitView = new SplitView();
				splitView.IsPaneOpen = true;

				splitView.PaneClosing += (s, e) => closingEvent.Set();

				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			// Close the SplitView programmatically.
			await RunOnUIThread(() =>
			{
				splitView.IsPaneOpen = false;
			});
			await closingEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();
		}

		// Validates that SplitViews in various configurations still allow their pane content to be interacted with.
		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_Overlay_Left_LTR()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Left, FlowDirection.LeftToRight);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_Overlay_Right_LTR()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Right, FlowDirection.LeftToRight);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_CompactOverlay_Left_LTR()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Left, FlowDirection.LeftToRight);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_CompactOverlay_Right_LTR()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Right, FlowDirection.LeftToRight);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_Inline_Left_LTR()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Left, FlowDirection.LeftToRight);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_Inline_Right_LTR()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Right, FlowDirection.LeftToRight);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_CompactInline_Left_LTR()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Left, FlowDirection.LeftToRight);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_CompactInline_Right_LTR()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Right, FlowDirection.LeftToRight);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_Overlay_Left_RTL()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Left, FlowDirection.RightToLeft);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_Overlay_Right_RTL()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Right, FlowDirection.RightToLeft);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_CompactOverlay_Left_RTL()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Left, FlowDirection.RightToLeft);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_CompactOverlay_Right_RTL()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Right, FlowDirection.RightToLeft);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_Inline_Left_RTL()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Left, FlowDirection.RightToLeft);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_Inline_Right_RTL()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Right, FlowDirection.RightToLeft);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_CompactInline_Left_RTL()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Left, FlowDirection.RightToLeft);
		}

		[TestMethod]
		public async Task CanInteractWithItemsInOpenPane_CompactInline_Right_RTL()
		{
			await CanInteractWithItemsInOpenPaneWorker(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Right, FlowDirection.RightToLeft);
		}

		private async Task CanInteractWithItemsInOpenPaneWorker(
			SplitViewDisplayMode displayMode,
			SplitViewPanePlacement placement,
			FlowDirection flowDirection)
		{
			SplitView splitView = null;
			Button tapTarget = null;

			var clickEvent = new Event();

			await RunOnUIThread(() =>
			{
				var root = (FrameworkElement)XamlReader.Load(
					"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"    <SplitView x:Name='splitView' Width='400' Height='400' OpenPaneLength='100'>" +
					"        <SplitView.Pane>" +
					"            <Border Background='Orange'>" +
					"                <Button x:Name='TapTarget'/>" +
					"            </Border>" +
					"        </SplitView.Pane>" +
					"        <Rectangle x:Name='contentElement' Fill='Purple'/>" +
					"    </SplitView>" +
					"</StackPanel>");

				root.FlowDirection = flowDirection;

				splitView = (SplitView)root.FindName("splitView");
				tapTarget = (Button)root.FindName("TapTarget");
				tapTarget.Click += (s, e) => clickEvent.Set();

				splitView.DisplayMode = displayMode;
				splitView.PanePlacement = placement;

				TestServices.WindowHelper.WindowContent = root;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				splitView.IsPaneOpen = true;
			});
			await TestServices.WindowHelper.WaitForIdle();

			// Validate that we can tap an item in the open pane.
			TestServices.InputHelper.Tap(tapTarget);
			await clickEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();
		}

		// Validates that SplitView.OpenPaneLength can be set correctly from a XAML string (e.g. VSM Setter).
		[TestMethod]
		public async Task CanSetOpenPaneLengthFromXamlString()
		{
			Control root = null;
			SplitView splitView = null;

			await RunOnUIThread(() =>
			{
				root = (Control)XamlReader.Load(
					"<UserControl xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"    <Border x:Name='MyBorder' Background='Pink'>" +
					"        <VisualStateManager.VisualStateGroups>" +
					"            <VisualStateGroup x:Name='Group1'>" +
					"                <VisualState x:Name='State1'>" +
					"                    <VisualState.Setters>" +
					"                        <Setter Target='splitView.OpenPaneLength' Value='200' />" +
					"                    </VisualState.Setters>" +
					"                </VisualState>" +
					"            </VisualStateGroup>" +
					"        </VisualStateManager.VisualStateGroups>" +
					"        <SplitView x:Name='splitView'" +
					"            PaneBackground='{ThemeResource SystemControlBackgroundAccentBrush}'" +
					"            DisplayMode='Inline'" +
					"            IsPaneOpen='True'" +
					"            OpenPaneLength='100' />" +
					"    </Border>" +
					"</UserControl>");

				TestServices.WindowHelper.WindowContent = root;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				splitView = (SplitView)root.FindName("splitView");
				VERIFY_ARE_EQUAL(100.0, splitView.OpenPaneLength);

				VisualStateManager.GoToState(root, "State1", true);
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(200.0, splitView.OpenPaneLength);
			});
		}

		// Verifies that a Splitview that is opened by default allows their controls to be interacted with.
		[TestMethod]
		public async Task CanInteractWithPaneContentIfOpenedByDefault()
		{
			SplitView splitView = null;
			FrameworkElement paneElement = null;

			await RunOnUIThread(() =>
			{
				var root = (FrameworkElement)XamlReader.Load(
					"<SplitView x:Name='splitView' IsPaneOpen='True' Width='300' Height='80'" +
					"           xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"    <SplitView.Pane>" +
					"        <Rectangle x:Name='paneElement' Fill='Purple'/>" +
					"    </SplitView.Pane>" +
					"</SplitView>");

				splitView = (SplitView)root.FindName("splitView");
				paneElement = (FrameworkElement)root.FindName("paneElement");

				TestServices.WindowHelper.WindowContent = root;
			});
			await TestServices.WindowHelper.WaitForIdle();

			// Test tapping an element inside the pane
			TestServices.InputHelper.Tap(paneElement);
			await TestServices.WindowHelper.WaitForIdle();

			bool isOpen = false;
			await RunOnUIThread(() => { isOpen = splitView.IsPaneOpen; });
			VERIFY_ARE_EQUAL(isOpen, true);
		}

		// Validates that apps can change the OpenPaneLength property and have it take effect.
		[TestMethod]
		public async Task CanChangeOpenPaneLength()
		{
			SplitView splitView = null;
			Rectangle refElement = null;

			await RunOnUIThread(() =>
			{
				splitView = new SplitView();
				splitView.DisplayMode = SplitViewDisplayMode.CompactInline;
				splitView.IsPaneOpen = true;

				refElement = new Rectangle();
				refElement.HorizontalAlignment = HorizontalAlignment.Left;

				splitView.Content = refElement;

				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			double expectedXOffset = 100;
			await RunOnUIThread(() =>
			{
				splitView.OpenPaneLength = expectedXOffset;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var offset = refElement.TransformToVisual(splitView).TransformPoint(new Point(0, 0));
				VERIFY_ARE_EQUAL(offset.X, expectedXOffset);
			});
		}

		// Validates that apps can change the CompactPaneLength property and have it take effect.
		[TestMethod]
		public async Task CanChangeCompactPaneLength()
		{
			SplitView splitView = null;
			Rectangle refElement = null;

			await RunOnUIThread(() =>
			{
				splitView = new SplitView();
				splitView.DisplayMode = SplitViewDisplayMode.CompactInline;

				refElement = new Rectangle();
				refElement.HorizontalAlignment = HorizontalAlignment.Left;

				splitView.Content = refElement;

				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			double expectedXOffset = 100;
			await RunOnUIThread(() =>
			{
				splitView.CompactPaneLength = expectedXOffset;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var offset = refElement.TransformToVisual(splitView).TransformPoint(new Point(0, 0));
				VERIFY_ARE_EQUAL(offset.X, expectedXOffset);
			});
		}

		// Validates the ActualWidth of SplitView's Content and Pane content in various configurations.
		[TestMethod]
		public async Task ValidateFootprint()
		{
			const double expectedClosedOverlayContentWidth = 400;
			const double expectedClosedOverlayPaneWidth = 0;

			const double expectedOpenOverlayContentWidth = 400;
			const double expectedOpenOverlayPaneWidth = 320;

			const double expectedClosedCompactOverlayContentWidth = 400 - 48; // WindowWidth - CompactPaneLength
			const double expectedClosedCompactOverlayPaneWidth = 320;

			const double expectedOpenCompactOverlayContentWidth = 400 - 48; // WindowWidth - CompactPaneLength
			const double expectedOpenCompactOverlayPaneWidth = 320;

			const double expectedClosedInlineContentWidth = 400;
			const double expectedClosedInlinePaneWidth = 0;

			const double expectedOpenInlineContentWidth = 400 - 320; // WindowWidth - OpenPaneLength
			const double expectedOpenInlinePaneWidth = 320;

			const double expectedClosedCompactInlineContentWidth = 400 - 48; // WindowWidth - CompactPaneLength
			const double expectedClosedCompactInlinePaneWidth = 320;

			const double expectedOpenCompactInlineContentWidth = 400 - 320; // WindowWidth - OpenPaneLength
			const double expectedOpenCompactInlinePaneWidth = 320;

			SplitView closedOverlaySplitView = null;
			SplitView openOverlaySplitView = null;
			SplitView closedCompactOverlaySplitView = null;
			SplitView openCompactOverlaySplitView = null;

			SplitView closedInlineSplitView = null;
			SplitView openInlineSplitView = null;
			SplitView closedCompactInlineSplitView = null;
			SplitView openCompactInlineSplitView = null;

			// TODO Uno: Original C++ test calls TestServices::WindowHelper->SetWindowSizeOverride(wf::Size(400, 600));
			// Using explicit Width on root panel instead.

			await RunOnUIThread(() =>
			{
				var rootPanel = (StackPanel)XamlReader.Load(
					"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
					"            Width='400'>" +
					"    <SplitView x:Name='closedOverlaySplitView' Height='75' IsPaneOpen='False' DisplayMode='Overlay'>" +
					"        <SplitView.Pane><Rectangle/></SplitView.Pane>" +
					"        <Rectangle/>" +
					"    </SplitView>" +
					"    <SplitView x:Name='openOverlaySplitView' Height='75' IsPaneOpen='True' DisplayMode='Overlay'>" +
					"        <SplitView.Pane><Rectangle/></SplitView.Pane>" +
					"        <Rectangle/>" +
					"    </SplitView>" +
					"    <SplitView x:Name='closedCompactOverlaySplitView' Height='75' IsPaneOpen='False' DisplayMode='CompactOverlay'>" +
					"        <SplitView.Pane><Rectangle/></SplitView.Pane>" +
					"        <Rectangle/>" +
					"    </SplitView>" +
					"    <SplitView x:Name='openCompactOverlaySplitView' Height='75' IsPaneOpen='True' DisplayMode='CompactOverlay'>" +
					"        <SplitView.Pane><Rectangle/></SplitView.Pane>" +
					"        <Rectangle/>" +
					"    </SplitView>" +
					"    <SplitView x:Name='closedInlineSplitView' Height='75' IsPaneOpen='False' DisplayMode='Inline'>" +
					"        <SplitView.Pane><Rectangle/></SplitView.Pane>" +
					"        <Rectangle/>" +
					"    </SplitView>" +
					"    <SplitView x:Name='openInlineSplitView' Height='75' IsPaneOpen='True' DisplayMode='Inline'>" +
					"        <SplitView.Pane><Rectangle/></SplitView.Pane>" +
					"        <Rectangle/>" +
					"    </SplitView>" +
					"    <SplitView x:Name='closedCompactInlineSplitView' Height='75' IsPaneOpen='False' DisplayMode='CompactInline'>" +
					"        <SplitView.Pane><Rectangle/></SplitView.Pane>" +
					"        <Rectangle/>" +
					"    </SplitView>" +
					"    <SplitView x:Name='openCompactInlineSplitView' Height='75' IsPaneOpen='True' DisplayMode='CompactInline'>" +
					"        <SplitView.Pane><Rectangle/></SplitView.Pane>" +
					"        <Rectangle/>" +
					"    </SplitView>" +
					"</StackPanel>");

				closedOverlaySplitView = (SplitView)rootPanel.FindName("closedOverlaySplitView");
				openOverlaySplitView = (SplitView)rootPanel.FindName("openOverlaySplitView");
				closedCompactOverlaySplitView = (SplitView)rootPanel.FindName("closedCompactOverlaySplitView");
				openCompactOverlaySplitView = (SplitView)rootPanel.FindName("openCompactOverlaySplitView");
				closedInlineSplitView = (SplitView)rootPanel.FindName("closedInlineSplitView");
				openInlineSplitView = (SplitView)rootPanel.FindName("openInlineSplitView");
				closedCompactInlineSplitView = (SplitView)rootPanel.FindName("closedCompactInlineSplitView");
				openCompactInlineSplitView = (SplitView)rootPanel.FindName("openCompactInlineSplitView");

				TestServices.WindowHelper.WindowContent = rootPanel;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(expectedClosedOverlayContentWidth, ((FrameworkElement)closedOverlaySplitView.Content).ActualWidth);
				VERIFY_ARE_EQUAL(expectedClosedOverlayPaneWidth, ((FrameworkElement)closedOverlaySplitView.Pane).ActualWidth);

				VERIFY_ARE_EQUAL(expectedOpenOverlayContentWidth, ((FrameworkElement)openOverlaySplitView.Content).ActualWidth);
				VERIFY_ARE_EQUAL(expectedOpenOverlayPaneWidth, ((FrameworkElement)openOverlaySplitView.Pane).ActualWidth);

				VERIFY_ARE_EQUAL(expectedClosedCompactOverlayContentWidth, ((FrameworkElement)closedCompactOverlaySplitView.Content).ActualWidth);
				VERIFY_ARE_EQUAL(expectedClosedCompactOverlayPaneWidth, ((FrameworkElement)closedCompactOverlaySplitView.Pane).ActualWidth);

				VERIFY_ARE_EQUAL(expectedOpenCompactOverlayContentWidth, ((FrameworkElement)openCompactOverlaySplitView.Content).ActualWidth);
				VERIFY_ARE_EQUAL(expectedOpenCompactOverlayPaneWidth, ((FrameworkElement)openCompactOverlaySplitView.Pane).ActualWidth);

				VERIFY_ARE_EQUAL(expectedClosedInlineContentWidth, ((FrameworkElement)closedInlineSplitView.Content).ActualWidth);
				VERIFY_ARE_EQUAL(expectedClosedInlinePaneWidth, ((FrameworkElement)closedInlineSplitView.Pane).ActualWidth);

				VERIFY_ARE_EQUAL(expectedOpenInlineContentWidth, ((FrameworkElement)openInlineSplitView.Content).ActualWidth);
				VERIFY_ARE_EQUAL(expectedOpenInlinePaneWidth, ((FrameworkElement)openInlineSplitView.Pane).ActualWidth);

				VERIFY_ARE_EQUAL(expectedClosedCompactInlineContentWidth, ((FrameworkElement)closedCompactInlineSplitView.Content).ActualWidth);
				VERIFY_ARE_EQUAL(expectedClosedCompactInlinePaneWidth, ((FrameworkElement)closedCompactInlineSplitView.Pane).ActualWidth);

				VERIFY_ARE_EQUAL(expectedOpenCompactInlineContentWidth, ((FrameworkElement)openCompactInlineSplitView.Content).ActualWidth);
				VERIFY_ARE_EQUAL(expectedOpenCompactInlinePaneWidth, ((FrameworkElement)openCompactInlineSplitView.Pane).ActualWidth);
			});
		}

		// When a SplitView is opened when nothing has focus, it should correctly focus its Pane.
		// Regression coverage for MSFT:2849754.
		[TestMethod]
		public async Task OpenSplitViewWithNoElementsFocused()
		{
			SplitView splitView = null;
			Button button = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = (Grid)XamlReader.Load(
					"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"    <SplitView x:Name='splitView' IsTabStop='False'>" +
					"        <SplitView.Pane>" +
					"            <StackPanel>" +
					"                <Button x:Name='button' Content='Button' />" +
					"            </StackPanel>" +
					"        </SplitView.Pane>" +
					"    </SplitView>" +
					"</Grid>");

				splitView = (SplitView)rootPanel.FindName("splitView");
				button = (Button)rootPanel.FindName("button");

				TestServices.WindowHelper.WindowContent = rootPanel;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot) == null);
				splitView.IsPaneOpen = true;
			});
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button));
				VERIFY_ARE_NOT_EQUAL(FocusState.Unfocused, button.FocusState);
			});
		}

		// Validates that the Opened event is not re-fired if changing display modes while open.
		[TestMethod]
		public async Task DoesNotFireOpenedOrClosedEventOnDisplayModeChange()
		{
			SplitView splitView = null;

			await RunOnUIThread(() =>
			{
				splitView = new SplitView();
				splitView.DisplayMode = SplitViewDisplayMode.Overlay;
				splitView.IsPaneOpen = true;

				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			var openedEvent = new Event();
			splitView.PaneOpened += (s, e) => openedEvent.Set();

			LOG_OUTPUT("Change the SplitView's display mode while open and validate that the PaneOpened event is not fired.");
			await RunOnUIThread(() =>
			{
				splitView.DisplayMode = SplitViewDisplayMode.CompactOverlay;
			});
			await TestServices.WindowHelper.WaitForIdle();

			VERIFY_IS_FALSE(openedEvent.HasFired());

			LOG_OUTPUT("Close the pane and verify that the PaneClosed event does not re-fire with display mode changes.");
			await RunOnUIThread(() =>
			{
				splitView.IsPaneOpen = false;
			});
			await TestServices.WindowHelper.WaitForIdle();

			var closedEvent = new Event();
			splitView.PaneClosed += (s, e) => closedEvent.Set();

			LOG_OUTPUT("Change the SplitView's display mode while closed and validate that the PaneClosed event is not fired.");
			await RunOnUIThread(() =>
			{
				splitView.DisplayMode = SplitViewDisplayMode.Overlay;
			});
			await TestServices.WindowHelper.WaitForIdle();

			VERIFY_IS_FALSE(closedEvent.HasFired());
		}

		// Verifies that the opened & closed events fire even when SplitView is re-templated
		// to exclude the DisplayModesStates state group.
		[TestMethod]
		public async Task DoesFireOpenedAndClosedEventsWhenRetemplated()
		{
			SplitView splitView = null;

			var openedEvent = new Event();
			var closedEvent = new Event();

			await RunOnUIThread(() =>
			{
				splitView = new SplitView();

				splitView.Template = (ControlTemplate)XamlReader.Load(
					"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
					"                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
					"                 TargetType='SplitView'>" +
					"    <Grid Background='{TemplateBinding Background}'/>" +
					"</ControlTemplate>");

				splitView.PaneOpened += (s, e) => openedEvent.Set();
				splitView.PaneClosed += (s, e) => closedEvent.Set();

				TestServices.WindowHelper.WindowContent = splitView;
			});
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Set IsPaneOpen=true and validate that the PaneOpened event fires.");
			await RunOnUIThread(() =>
			{
				splitView.IsPaneOpen = true;
			});
			await TestServices.WindowHelper.WaitForIdle();
			await openedEvent.WaitForDefault();

			LOG_OUTPUT("Set IsPaneOpen=false and validate that the PaneClosed event fires.");
			await RunOnUIThread(() =>
			{
				splitView.IsPaneOpen = false;
			});
			await TestServices.WindowHelper.WaitForIdle();
			await closedEvent.WaitForDefault();
		}

		// TODO Uno: CanDragFromPane test is not ported.
		// It requires drag-and-drop input injection support (InputHelper.DragBetweenElements)
		// which is not yet available in Uno Platform runtime tests.
		// Original test verifies dragging items from the SplitView pane to content and external ListViews.

		// TODO Uno: ValidateUIElementTree test is not ported.
		// It requires ControlHelper.ValidateUIElementTree which produces visual tree snapshots
		// for comparison against master files. This infrastructure is not available in Uno.

		// TODO Uno: VerifyKeyboardFocusBehavior test is not ported.
		// It requires KeyboardHelper.Tab/ShiftTab for keyboard focus navigation.
		// Original test validates focus sequence: [C][P1][P2][P3][P1][P2][P3][P2][P1][P3][P2][P1][C]

		// TODO Uno: VerifyGamepadFocusBehavior test is not ported.
		// It requires CommonInputHelper.Up/Down/Left/Right for gamepad navigation.
		// Original test validates focus trapping behavior differs between Overlay and Inline modes.

		// TODO Uno: CanNotShiftTabOutOfPaneWhenContentsIsListView test is not ported.
		// It requires KeyboardHelper.ShiftTab/Tab and TreeHelper.IsAncestorOf.
		// Original test validates focus stays within overlay pane when shift-tabbing with ListView content.

		// TODO Uno: ValidateLightDismissOverlayMode test is not ported.
		// It requires ControlHelper.IsInVisualState helper which is not available.
		// Original test validates OverlayVisibilityStates visual states for Auto/On/Off modes.

		// TODO Uno: IsAutoLightDismissOverlayModeVisibleOnXbox test is not ported.
		// It is Xbox-specific and requires ControlHelper.IsInVisualState.
		// Original test was also marked as Ignored in WinUI.

		// TODO Uno: ValidateOverlayBrush test is not ported.
		// It requires TreeHelper.GetVisualChildByName to find the LightDismissLayer element.
		// Original test validates the overlay rectangle uses SplitViewLightDismissOverlayBackground brush.

		// TODO Uno: ValidateOverlayUIETree test is not ported.
		// It requires ControlHelper.ValidateUIElementTree for visual tree snapshot validation.
	}
}

#endif
