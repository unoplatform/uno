// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlyoutIntegrationTests.cpp, commit bac7a9c33

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Uno.UI.DevTools.Input;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls;

// Notes on the port:
// - The upstream Event is an auto-reset event, Uno's Event stays set until Reset() is called.
//   Events that are waited on more than once are reset after each wait.
// - Upstream TestSetup/TestCleanup (InitializeXaml/ShutdownXaml) is replaced by the TestCleanup below.
// - Master-file comparisons (ValidateUIElementTree) are turned into explicit assertions.
// - Windowed state is asserted through the internal test hook Popup.IsWindowed.

[TestClass]
[RequiresFullWindow] // The upstream tests expect window-absolute coordinates.
public class FlyoutIntegrationTests
{
	[TestCleanup]
	public async Task TestCleanup()
	{
		await RunOnUIThread(() =>
		{
#if HAS_UNO
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			TestServices.WindowHelper.WindowContent = null;
		});
	}

	//
	// Test Cases
	//
	[TestMethod]
	[Description("Validates that we can successfully create a Flyout.")]
	public void CanInstantiate()
	{
		try
		{
			var flyout = new Flyout();
		}
		catch (Exception ex)
		{
			Assert.Fail($"Failed to instantiate Flyout: {ex}");
		}
	}

	[TestMethod]
	[Description("Verify the Flyout open and close, projected shadow mode.")]
	[Ignore("Not applicable: needs the WinUI-internal ForceProjectedShadowsOnByDefault runtime feature override (upstream is also ignored: 'We're on drop shadows now').")]
	public async Task CanFlyoutOpenCloseProjectedShadow()
	{
		// RuntimeEnabledFeatureOverride featureUseDropShadows(RuntimeFeatureBehavior::RuntimeEnabledFeature::ForceProjectedShadowsOnByDefault, true);
		// CanFlyoutOpenClose();
		await Task.CompletedTask;
	}

	// Upstream marks this test as ignored ("TODO 36060166: Re-enable after fixing unreliability.").
	[TestMethod]
	[Description("Verify the Flyout open and close, drop shadow mode.")]
	public async Task CanFlyoutOpenCloseDropShadow()
	{
		await CanFlyoutOpenClose();
	}

	private async Task CanFlyoutOpenClose()
	{
		// WUCRenderingScopeGuard wuc(DCompRendering::WUCCompleteSynchronousCompTree);

		Button button = null;
		Flyout flyout = null;

		var flyoutOpenedEvent = new Event();
		var flyoutClosedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left'> " +
				"  <Button x:Name='button' Content='button.flyout' HorizontalAlignment='Center' VerticalAlignment='Center' FontSize='25' > " +
				"    <Button.Flyout> " +
				"      <Flyout Placement='TopEdgeAlignedLeft'> " +
				"        <TextBlock FontSize='30' Text='BUTTON.FLYOUT' Margin='30' Foreground='DarkRed' x:Name='button_flyout_content'/> " +
				"      </Flyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			VERIFY_IS_NOT_NULL(rootPanel);
			TestServices.WindowHelper.WindowContent = rootPanel;

			button = (Button)(rootPanel.FindName("button"));
			VERIFY_IS_NOT_NULL(button);

			flyout = (Flyout)(button.Flyout);
			VERIFY_IS_NOT_NULL(flyout);

			openedRegistration.Attach(flyout, (s, e) =>
			{
				LOG_OUTPUT("PopupOpenClose: Flyout Opened event is fired!");
				flyoutOpenedEvent.Set();
			});

			closedRegistration.Attach(flyout, (s, e) =>
			{
				LOG_OUTPUT("PopupOpenClose: Flyout Closed event is fired!");
				flyoutClosedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Button Tap operation to show the Flyout.");
		TestServices.InputHelper.Tap(button);
		await flyoutOpenedEvent.WaitForDefault();
		flyoutOpenedEvent.Reset();
		await TestServices.WindowHelper.WaitForIdle();
		TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("RootPanel Tap operation to close the Flyout.");
			flyout.Hide();
		});

		await flyoutClosedEvent.WaitForDefault();
		flyoutClosedEvent.Reset();

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Button Tap operation to show the Flyout.");
		TestServices.InputHelper.Tap(button);
		await flyoutOpenedEvent.WaitForDefault();
		flyoutOpenedEvent.Reset();

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Keyboard Escape to close the flyout.");
		await TestServices.KeyboardHelper.Escape();

		await flyoutClosedEvent.WaitForDefault();
		flyoutClosedEvent.Reset();

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Button Tap operation to show the Flyout.");
		TestServices.InputHelper.Tap(button);
		await flyoutOpenedEvent.WaitForDefault();
		flyoutOpenedEvent.Reset();

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Gamepad B to close the flyout.");
		await TestServices.KeyboardHelper.GamepadB();

		await flyoutClosedEvent.WaitForDefault();
	}

	[TestMethod]
	[Description("Verify the Flyout dependency properties.")]
	public async Task VerifyDependencyProperties()
	{
		await RunOnUIThread(() =>
		{
			var rootPanel = new Border();

			var textBlock = new TextBlock();
			textBlock.Text = "Flyout";

			var flyout = new Flyout();
			VERIFY_IS_NOT_NULL(flyout);
			flyout.Content = textBlock;

			VERIFY_IS_NOT_NULL(rootPanel);
			TestServices.WindowHelper.WindowContent = rootPanel;

			rootPanel.SetValue(Flyout.AttachedFlyoutProperty, flyout);
			var attachedFlyout = rootPanel.GetValue(Flyout.AttachedFlyoutProperty);
			VERIFY_ARE_EQUAL(flyout, attachedFlyout);

			flyout.SetValue(Flyout.PlacementProperty, FlyoutPlacementMode.Bottom);
			var placementMode = (FlyoutPlacementMode)(flyout.GetValue(Flyout.PlacementProperty));
			VERIFY_ARE_EQUAL(placementMode, FlyoutPlacementMode.Bottom);

			var button = new Button();
			button.Content = "I'm a button on Flyout!";
			flyout.SetValue(Flyout.ContentProperty, button);
			var content = flyout.GetValue(Flyout.ContentProperty);
			VERIFY_ARE_EQUAL(button, content);

			var styleLocal = new Style();
			flyout.SetValue(Flyout.FlyoutPresenterStyleProperty, styleLocal);
			var style = flyout.GetValue(Flyout.FlyoutPresenterStyleProperty);
			VERIFY_ARE_EQUAL(style, styleLocal);
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Description("Verify the Flyout dependency property default values.")]
	public async Task VerifyDependencyPropertyDefaultValues()
	{
		Button button = null;
		Flyout flyout = null;

		(flyout, button) = await SetupBasicFlyoutTest();
		await FlyoutHelper.OpenFlyout(flyout, button, FlyoutOpenMethod.Programmatic_ShowAt);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var presenter = FlyoutHelper.GetFlyoutPresenter(flyout);
			VERIFY_ARE_EQUAL(true, presenter.IsDefaultShadowEnabled);
		});
	}

	[TestMethod]
	[Description("Verify the Flyout presenter style.")]
	public async Task VerifyFlyoutPresenterStyle()
	{
		var flyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Bottom);
		VERIFY_IS_NOT_NULL(flyout);

		var target = await FlyoutHelper.CreateTarget(
			300 /*width*/, 300 /*height*/,
			new Thickness(100),
			HorizontalAlignment.Center,
			VerticalAlignment.Center);
		VERIFY_IS_NOT_NULL(target);

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			VERIFY_IS_NOT_NULL(rootPanel);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("VerifyFlyoutPresenterStyle: Execute the ShowAt.");
		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var presenter = FlyoutHelper.GetFlyoutPresenter(flyout);
			VERIFY_IS_NOT_NULL(presenter);
			var tag = presenter.GetValue(FlyoutPresenter.TagProperty);
			VERIFY_IS_NOT_NULL(tag);
			VERIFY_ARE_EQUAL("presenter_style", tag.ToString());
		});

		LOG_OUTPUT("VerifyFlyoutPresenterStyle: Execute the Hide.");
		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Verify the AttachedFlyout can show and hide.")]
	public async Task CanAttachedFlyoutShowHide()
	{
		var flyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Right);
		VERIFY_IS_NOT_NULL(flyout);

		var target = await FlyoutHelper.CreateTarget(
			300 /*width*/, 300 /*height*/,
			new Thickness(100),
			HorizontalAlignment.Left,
			VerticalAlignment.Center);
		VERIFY_IS_NOT_NULL(target);

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			VERIFY_IS_NOT_NULL(rootPanel);

			TestServices.WindowHelper.WindowContent = rootPanel;

			var attachedFlyout = Flyout.GetAttachedFlyout(target);
			VERIFY_IS_NULL(attachedFlyout);

			Flyout.SetAttachedFlyout(target, flyout);

			attachedFlyout = Flyout.GetAttachedFlyout(target);
			VERIFY_IS_NOT_NULL(attachedFlyout);
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("CanAttachedFlyoutShowHide: Execute ShowAttachedFlyout.");
		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAttachedFlyout);

		LOG_OUTPUT("CanAttachedFlyoutShowHide: Execute the Hide.");
		await FlyoutHelper.HideFlyout(flyout);
	}

	// Upstream compares the open flyout's UIElement tree against master files
	// (Controls_Flyout_FlyoutIntegrationTests_ValidateUIElementTree.{Dark,Light,HC}[.Windowed|.Unwindowed].master.xml).
	// Uno has no master-file comparison, so ValidateUIETreeWorker asserts what those masters pin explicitly.
	// Brush/color values that depend on the WinUI theme resources version are intentionally not pinned.
	[TestMethod]
	[Description("Validates the UI element tree of an open Flyout")]
	public async Task ValidateUIElementTree()
	{
		await ValidateUIETreeWorker(Theme.Dark, "Dark");
		await ValidateUIETreeWorker(Theme.Light, "Light");
#if false // TODO Uno: needs a high-contrast mode switch (ControlHelper.ValidateUIElementTreeForHighContrast) and the HC master
		await ValidateUIETreeWorker(Theme.HighContrast, "HC");
#endif
	}

	private async Task ValidateUIETreeWorker(Theme theme, string variation)
	{
		Grid rootPanel = null;
		Flyout flyout = null;
		TextBlock flyoutContent = null;
		Button flyoutTarget = null;

		// PopupHelper::AreWindowedPopupsEnabled() picks the ".Windowed"/".Unwindowed" master. Both masters
		// pin ShouldConstrainToRootBounds=True and the popup living in the PopupRoot, which is asserted below.

		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

		var flyoutOpenedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));

		await RunOnUIThread(() =>
		{
			rootPanel = new Grid();

			flyout = new Flyout();
			flyout.Placement = FlyoutPlacementMode.Top;
			flyout.LightDismissOverlayMode = LightDismissOverlayMode.Off;

			flyoutContent = new TextBlock();
			flyoutContent.Text = "Flyout Content";
			flyout.Content = flyoutContent;

			flyoutTarget = new Button();
			flyoutTarget.IsEnabled = false;    // Disabled so it is not Focusable and IsPressed is set to False to ensure same dumps for Desktop and Phone.
			flyoutTarget.Content = "Flyout Target";
			flyoutTarget.HorizontalAlignment = HorizontalAlignment.Center;
			flyoutTarget.VerticalAlignment = VerticalAlignment.Center;
			rootPanel.Children.Add(flyoutTarget);

			openedRegistration.Attach(flyout, (s, e) =>
			{
				LOG_OUTPUT("Flyout Opened event is fired!");
				flyoutOpenedEvent.Set();
			});

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		if (theme == Theme.Light)
		{
			await RunOnUIThread(() =>
			{
				rootPanel.RequestedTheme = ElementTheme.Light;
				rootPanel.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
			});
		}

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			flyout.ShowAt(flyoutTarget);
		});
		await flyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// What the master files pin, as explicit assertions.
		var targetBounds = await ControlHelper.GetBounds(flyoutTarget);
		await RunOnUIThread(() =>
		{
			// Target: a disabled Button that is not pressed.
			VERIFY_IS_FALSE(flyoutTarget.IsEnabled);
			VERIFY_IS_FALSE(flyoutTarget.IsPressed);
			VERIFY_ARE_EQUAL("Flyout Target", flyoutTarget.Content);

			// The flyout lives in a single popup in the PopupRoot, with a FlyoutPresenter as its child.
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot);
			VERIFY_ARE_EQUAL(1, popups.Count);
			var popup = popups[0];
			var presenter = popup.Child as FlyoutPresenter;
			VERIFY_IS_NOT_NULL(presenter);

			VERIFY_IS_TRUE(popup.IsOpen);
			VERIFY_IS_TRUE(popup.IsLightDismissEnabled);
			VERIFY_ARE_EQUAL(LightDismissOverlayMode.Off, popup.LightDismissOverlayMode);
			VERIFY_IS_TRUE(popup.ShouldConstrainToRootBounds);
			VERIFY_IS_TRUE(Microsoft.UI.Xaml.Automation.AutomationProperties.GetIsDialog(popup));
			VERIFY_ARE_EQUAL(1.0, popup.Opacity);
			VERIFY_ARE_EQUAL(FlowDirection.LeftToRight, popup.FlowDirection);
			VERIFY_ARE_EQUAL(0.0, popup.ActualWidth);
			VERIFY_ARE_EQUAL(0.0, popup.ActualHeight);
#if HAS_UNO
			// Not windowed: the popup is hosted in the PopupRoot (ShouldConstrainToRootBounds=True).
			VERIFY_IS_FALSE(popup.IsWindowed);
#endif

			// Popup placement for Placement=Top: horizontally centered on the target, directly above it with a 4px gap.
			VERIFY_ARE_VERY_CLOSE(targetBounds.X + (targetBounds.Width / 2) - (presenter.ActualWidth / 2), popup.HorizontalOffset, 1.0);
			VERIFY_ARE_VERY_CLOSE(targetBounds.Y - 4 - presenter.ActualHeight, popup.VerticalOffset, 1.0);

			// FlyoutPresenter defaults.
			VERIFY_ARE_EQUAL(popup, presenter.Parent);
			VERIFY_IS_FALSE(presenter.IsTabStop);
			VERIFY_IS_TRUE(presenter.AllowFocusOnInteraction);
			VERIFY_IS_FALSE(presenter.AllowFocusWhenDisabled);
			VERIFY_ARE_EQUAL(HorizontalAlignment.Stretch, presenter.HorizontalContentAlignment);
			VERIFY_ARE_EQUAL(VerticalAlignment.Stretch, presenter.VerticalContentAlignment);
			VERIFY_ARE_EQUAL(96.0, presenter.MinWidth);
			VERIFY_ARE_EQUAL(40.0, presenter.MinHeight);
			VERIFY_ARE_EQUAL(456.0, presenter.MaxWidth);
			VERIFY_ARE_EQUAL(new Thickness(1), presenter.BorderThickness);
			VERIFY_ARE_EQUAL(new CornerRadius(8), presenter.CornerRadius);
			VERIFY_ARE_EQUAL(new Thickness(16, 15, 16, 17), presenter.Padding);
			VERIFY_ARE_EQUAL(flyoutContent, presenter.Content);
			VERIFY_ARE_EQUAL(ScrollBarVisibility.Auto, ScrollViewer.GetHorizontalScrollBarVisibility(presenter));
			VERIFY_ARE_EQUAL(ScrollMode.Auto, ScrollViewer.GetHorizontalScrollMode(presenter));
			VERIFY_ARE_EQUAL(ScrollBarVisibility.Auto, ScrollViewer.GetVerticalScrollBarVisibility(presenter));
			VERIFY_ARE_EQUAL(ScrollMode.Auto, ScrollViewer.GetVerticalScrollMode(presenter));
			VERIFY_ARE_EQUAL(ZoomMode.Disabled, ScrollViewer.GetZoomMode(presenter));
			VERIFY_IS_NOT_NULL(presenter.Background);
			VERIFY_IS_NOT_NULL(presenter.BorderBrush);

			// The presenter's template: Border (shadowed) > ScrollViewer > ... > ContentPresenter > the flyout content.
			var border = VisualTreeHelper.GetChild(presenter, 0) as Border;
			VERIFY_IS_NOT_NULL(border);
			VERIFY_IS_NOT_NULL(border.Shadow);
			VERIFY_ARE_EQUAL(new Thickness(1), border.BorderThickness);
			VERIFY_ARE_EQUAL(new CornerRadius(8), border.CornerRadius);
			var scrollViewer = border.Child as ScrollViewer;
			VERIFY_IS_NOT_NULL(scrollViewer);
			VERIFY_ARE_EQUAL("ScrollViewer", scrollViewer.Name);
			VERIFY_ARE_EQUAL(new Thickness(0), scrollViewer.Padding);
			VERIFY_ARE_EQUAL(0.0, scrollViewer.ScrollableWidth);
			VERIFY_ARE_EQUAL(0.0, scrollViewer.ScrollableHeight);

			var scrollContentPresenter = TreeHelper.GetVisualChildByName(scrollViewer, "ScrollContentPresenter");
			VERIFY_IS_NOT_NULL(scrollContentPresenter);
			var contentPresenter = TreeHelper.GetVisualChildByType<ContentPresenter>(scrollContentPresenter);
			VERIFY_IS_NOT_NULL(contentPresenter);
			VERIFY_ARE_EQUAL(new Thickness(16, 15, 16, 17), contentPresenter.Margin);
			VERIFY_ARE_EQUAL(HorizontalAlignment.Stretch, contentPresenter.HorizontalAlignment);
			VERIFY_ARE_EQUAL(VerticalAlignment.Stretch, contentPresenter.VerticalAlignment);
			VERIFY_ARE_EQUAL(flyoutContent, contentPresenter.Content);
			VERIFY_ARE_EQUAL("Flyout Content", flyoutContent.Text);
			VERIFY_ARE_EQUAL(scrollContentPresenter, VisualTreeHelper.GetParent(contentPresenter));
			VERIFY_ARE_EQUAL(contentPresenter, VisualTreeHelper.GetParent(flyoutContent));

			// Both scroll bars are present in the ScrollViewer template and collapsed (nothing to scroll).
			var verticalScrollBar = TreeHelper.GetVisualChildByName(scrollViewer, "VerticalScrollBar") as ScrollBar;
			var horizontalScrollBar = TreeHelper.GetVisualChildByName(scrollViewer, "HorizontalScrollBar") as ScrollBar;
			VERIFY_IS_NOT_NULL(verticalScrollBar);
			VERIFY_IS_NOT_NULL(horizontalScrollBar);
			VERIFY_ARE_EQUAL(Orientation.Vertical, verticalScrollBar.Orientation);
			VERIFY_ARE_EQUAL(Orientation.Horizontal, horizontalScrollBar.Orientation);
			VERIFY_ARE_EQUAL(0.0, verticalScrollBar.Maximum);
			VERIFY_ARE_EQUAL(0.0, horizontalScrollBar.Maximum);
			VERIFY_IS_FALSE(verticalScrollBar.IsTabStop);
			VERIFY_IS_FALSE(horizontalScrollBar.IsTabStop);

			// Requested theme: Light is propagated from the target's ancestors to the popup and the presenter.
			if (theme == Theme.Light)
			{
				VERIFY_ARE_EQUAL(ElementTheme.Light, popup.RequestedTheme);
				VERIFY_ARE_EQUAL(ElementTheme.Light, presenter.RequestedTheme);
			}
			else
			{
				VERIFY_ARE_EQUAL(ElementTheme.Default, popup.RequestedTheme);
			}
		});

		await RunOnUIThread(() =>
		{
			flyout.Hide();
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Description("Certain properties, including FlowDirection and RequestedTheme should get forwarded from the target to the presenter")]
	public async Task VerifyTargetPropertiesForwardedToPresenter()
	{
		Button button = null;
		Flyout flyout = null;

		string dataContext = "DataContext";
		FlowDirection flowDirection = FlowDirection.RightToLeft;
		string language = "fr-FR";
		bool isTextScaleFactorEnabled = false;
		ElementTheme requestedTheme = ElementTheme.Light;

		(flyout, button) = await SetupBasicFlyoutTest();

		await RunOnUIThread(() =>
		{
			var rootPanel = (Panel)(button.Parent);
			rootPanel.DataContext = dataContext;
			rootPanel.FlowDirection = flowDirection;
			rootPanel.Language = language;
			button.IsTextScaleFactorEnabled = isTextScaleFactorEnabled;
			rootPanel.RequestedTheme = requestedTheme;
			rootPanel.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(flyout, button, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var presenter = FlyoutHelper.GetFlyoutPresenter(flyout);
			VERIFY_ARE_EQUAL(dataContext, presenter.DataContext as string);
			VERIFY_ARE_EQUAL(flowDirection, presenter.FlowDirection);
			VERIFY_ARE_EQUAL(language, presenter.Language);
			VERIFY_ARE_EQUAL(isTextScaleFactorEnabled, presenter.IsTextScaleFactorEnabled);
			VERIFY_ARE_EQUAL(requestedTheme, presenter.RequestedTheme);
		});

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("When the target is in a Popup, verify that the theme gets propagated to the presenter")]
	public async Task VerifyThemeAppliedToPresenterWhenTargetIsInPopup()
	{
		Button button = null;
		Flyout flyout = null;
		Popup popup = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      Width='400' Height='200'> " +
				"  <StackPanel RequestedTheme='Light' Background='White' >" +
				"    <Popup x:Name='popup'>" +
				"      <Button x:Name='button' Content='button' > " +
				"        <Button.Flyout> " +
				"          <Flyout x:Name='flyout'> " +
				"            <TextBlock Text='flyout' /> " +
				"          </Flyout> " +
				"        </Button.Flyout> " +
				"      </Button> " +
				"    </Popup>" +
				"  </StackPanel>" +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;

			popup = (Popup)(rootPanel.FindName("popup"));
			button = (Button)(rootPanel.FindName("button"));
			flyout = (Flyout)(button.Flyout);

			// We need to open the popup after WindowContent is set.  If we set IsOpen="true" in the Popup's Xaml, the Popup is opened
			// during parse time, but the island doesn't exist yet so Xaml doesn't know what PopupRoot to use.
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(flyout, button, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var presenter = FlyoutHelper.GetFlyoutPresenter(flyout);
			VERIFY_ARE_EQUAL(ElementTheme.Light, presenter.RequestedTheme);
		});

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Validates that an open flyout is still light dismiss when full.")]
	public async Task ValidateIsLightDismissWhenFull()
	{
		Button button = null;
		Flyout flyout = null;

		(flyout, button) = await SetupBasicFlyoutTest();

		var flyoutOpenedEvent = new Event();
		var flyoutClosedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		await RunOnUIThread(() =>
		{
			flyout.Placement = FlyoutPlacementMode.Full;

			openedRegistration.Attach(flyout, (s, e) =>
			{
				flyoutOpenedEvent.Set();
			});

			closedRegistration.Attach(flyout, (s, e) =>
			{
				flyoutClosedEvent.Set();
			});
		});

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Show the flyout.");
			flyout.ShowAt(button);
		});

		await flyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Dismiss flyout by tapping outside of it.");
		Point targetPoint = default;
		var presenterBounds = await ControlHelper.GetBounds(FlyoutHelper.GetFlyoutPresenter(flyout));
		await RunOnUIThread(() =>
		{
			// We tap to the left of the flyout, half-way down.
			targetPoint = new Point(0, presenterBounds.Height / 2);
		});
		TestServices.InputHelper.Tap(targetPoint);

		LOG_OUTPUT("Waiting for flyout to close...");
		await flyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	private async Task<(Flyout flyout, Button button)> SetupBasicFlyoutTest()
	{
		Flyout flyout = null;
		Button button = null;

		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>(nameof(Grid.Loaded));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' Width='400' Height='200' VerticalAlignment='Top' HorizontalAlignment='Left'> " +
				"  <Button x:Name='button' Content='button.flyout' HorizontalAlignment='Left' FontSize='25' > " +
				"    <Button.Flyout> " +
				"      <Flyout Placement='Top'> " +
				"        <TextBlock FontSize='30' Text='BUTTON.FLYOUT' Margin='30' Foreground='DarkRed' x:Name='button_flyout_content'/> " +
				"      </Flyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			VERIFY_IS_NOT_NULL(rootPanel);
			loadedRegistration.Attach(rootPanel, (s, e) => { loadedEvent.Set(); });
			TestServices.WindowHelper.WindowContent = rootPanel;

			button = (Button)(rootPanel.FindName("button"));
			VERIFY_IS_NOT_NULL(button);

			flyout = (Flyout)(button.Flyout);
			VERIFY_IS_NOT_NULL(flyout);
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		return (flyout, button);
	}

	private enum UnloadBehavior
	{
		remainOpen = 0,
		shouldDismiss = 1
	}

	private static async Task RunDismissOnUnloadTest(FlyoutPlacementMode mode, UnloadBehavior expected)
	{
		Flyout flyout = null;
		Panel rootPanel = null;
		ContentPresenter flyoutTarget = null;

		var flyoutOpenedEvent = new Event();
		var flyoutClosedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		await RunOnUIThread(() =>
		{
			rootPanel = new Grid();
			TestServices.WindowHelper.WindowContent = rootPanel;

			flyoutTarget = new ContentPresenter();
			flyoutTarget.Content = "Flyout target";
			flyoutTarget.HorizontalAlignment = HorizontalAlignment.Center;
			flyoutTarget.VerticalAlignment = VerticalAlignment.Center;
			rootPanel.Children.Add(flyoutTarget);

			var flyoutContent = new TextBlock();
			flyoutContent.Text = "I'm a flyout!";
			flyoutContent.FontSize = 20;

			flyout = new Flyout();
			flyout.Placement = mode;
			flyout.Content = flyoutContent;
			Flyout.SetAttachedFlyout(flyoutTarget, flyout);

			openedRegistration.Attach(flyout, (s, e) =>
			{
				flyoutOpenedEvent.Set();
			});

			closedRegistration.Attach(flyout, (s, e) =>
			{
				flyoutClosedEvent.Set();
			});
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Show the flyout.");
			flyout.ShowAt(flyoutTarget);
		});

		await flyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Dismiss flyout by unloading its placement target");
		await RunOnUIThread(() =>
		{
			rootPanel.Children.Clear();
		});

		if (expected == UnloadBehavior.shouldDismiss)
		{
			await flyoutClosedEvent.WaitForDefault();
		}
		await TestServices.WindowHelper.WaitForIdle();

		var flyoutClosedEventFired = flyoutClosedEvent.HasFired();
		if (expected == UnloadBehavior.shouldDismiss)
		{
			VERIFY_IS_TRUE(flyoutClosedEventFired);
		}
		else
		{
			VERIFY_IS_FALSE(flyoutClosedEventFired);
		}

		// This is cleaning up the remaining flyout after test.
		if (expected == UnloadBehavior.remainOpen)
		{
			await RunOnUIThread(() =>
			{
				flyout.Hide();
			});
		}
	}

	// Runs on all Win10 and later builds. The converged (UAP) behavior for
	// fullscreen flyouts yields an "overlay" flyout that isn't its own page
	// with its own AppBar.

	// This isn't a valid test for Phone 8.1 because fullscreen flyouts in
	// Phone 8.1 could have their own AppBar, which would cause the original
	// page's AppBar to unload, which could then result in immediately
	// dismissing the Flyout (because its placement target (the AppBar) unloaded).
	[TestMethod]
	[Description("Validates that an open flyout is hidden when its placement target is unloaded")]
	public async Task ValidateDismissOnPlacementTargetUnloaded()
	{
		await RunDismissOnUnloadTest(FlyoutPlacementMode.Full, UnloadBehavior.shouldDismiss);
		await RunDismissOnUnloadTest(FlyoutPlacementMode.Top, UnloadBehavior.shouldDismiss);
		await RunDismissOnUnloadTest(FlyoutPlacementMode.Bottom, UnloadBehavior.shouldDismiss);
		await RunDismissOnUnloadTest(FlyoutPlacementMode.Left, UnloadBehavior.shouldDismiss);
		await RunDismissOnUnloadTest(FlyoutPlacementMode.Right, UnloadBehavior.shouldDismiss);
	}

	[TestMethod]
	[Description("Verify the Flyout property change at the Opening event.")]
	public async Task VerifyFlyoutPropertiesAtOpening()
	{
		Flyout flyout = null;
		Button button = null;
		Button buttonFlyout = null;
		Style styleLocal = null;
		SolidColorBrush blue = null;

		var flyoutOpeningEvent = new Event();
		var openingRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opening));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' Width='400' Height='200' VerticalAlignment='Top' HorizontalAlignment='Left'> " +
				"  <Button x:Name='button' Content='button.flyout' HorizontalAlignment='Left' FontSize='25' > " +
				"    <Button.Flyout> " +
				"      <Flyout Placement='Bottom'> " +
				"        <TextBlock FontSize='30' Text='Before_ContentChangedAtOpening' /> " +
				"      </Flyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;

			button = (Button)(rootPanel.FindName("button"));
			flyout = (Flyout)(button.Flyout);

			openingRegistration.Attach(
				flyout,
				(s, e) =>
				{
					LOG_OUTPUT("VerifyFlyoutContentChange: Flyout Opening event is fired!");

					buttonFlyout = new Button();
					buttonFlyout.Content = "Content_Changed!";

					// Replace the Content with new value.
					flyout.Content = buttonFlyout;

					// Replace the FlyoutPresenterStyle with new value.
					blue = new SolidColorBrush(Microsoft.UI.Colors.Blue);
					styleLocal = new Style(typeof(FlyoutPresenter));
					styleLocal.Setters.Add(new Setter(Control.BackgroundProperty, blue));
					flyout.FlyoutPresenterStyle = styleLocal;

					flyoutOpeningEvent.Set();
				});
		});
		await TestServices.WindowHelper.WaitForIdle();

		TestServices.InputHelper.Tap(button);
		await flyoutOpeningEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(flyout.Content, buttonFlyout);
			VERIFY_ARE_EQUAL(flyout.FlyoutPresenterStyle, styleLocal);
		});

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Validates that the contents of a flyout have loaded and unloaded properly raised once and only once on open and close.")]
	public async Task ValidateLoadedAndUnloadedRaisedAtCorrectTimes()
	{
		Flyout flyout = null;
		Button button = null;
		FrameworkElement flyoutContent = null;

		var flyoutOpenedEvent = new Event();
		var flyoutClosedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		var contentLoadedEventCount = 0;
		var contentUnloadedEventCount = 0;
		var contentLoadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>(nameof(FrameworkElement.Loaded));
		var contentUnloadedRegistration = CreateSafeEventRegistration<FrameworkElement, RoutedEventHandler>(nameof(FrameworkElement.Unloaded));

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' Width='400' Height='200' VerticalAlignment='Top' HorizontalAlignment='Left'> " +
				"  <Button x:Name='button' Content='button.flyout' HorizontalAlignment='Left' FontSize='25' > " +
				"    <Button.Flyout> " +
				"      <Flyout Placement='Bottom'> " +
				"        <TextBlock FontSize='30' Text='Before_ContentChangedAtOpening' /> " +
				"      </Flyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;

			button = (Button)(rootPanel.FindName("button"));
			flyout = (Flyout)(button.Flyout);
			flyoutContent = (FrameworkElement)(flyout.Content);

			openedRegistration.Attach(
				flyout,
				(s, e) =>
				{
					flyoutOpenedEvent.Set();
				});

			closedRegistration.Attach(
				flyout,
				(s, e) =>
				{
					flyoutClosedEvent.Set();
				});

			contentLoadedRegistration.Attach(
				flyoutContent,
				(s, e) =>
				{
					contentLoadedEventCount++;
				});

			contentUnloadedRegistration.Attach(
				flyoutContent,
				(s, e) =>
				{
					contentUnloadedEventCount++;
				});
		});
		await TestServices.WindowHelper.WaitForIdle();

		TestServices.InputHelper.Tap(button);
		await flyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(1, contentLoadedEventCount);
			VERIFY_ARE_EQUAL(0, contentUnloadedEventCount);
		});

		await FlyoutHelper.HideFlyout(flyout);
		await flyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(1, contentLoadedEventCount);
			VERIFY_ARE_EQUAL(1, contentUnloadedEventCount);
		});
	}

	[TestMethod]
	[Description("Verify the nested Popups open and close.")]
	public async Task CanPopupNestedInFlyoutOpenClose()
	{
		Button button = null;
		Flyout flyout = null;
		FlyoutPresenter flyoutPresenter = null;
		ComboBox comboBox = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
				"  x:Name='root' Background='SlateBlue' Width='400' Height='500' VerticalAlignment='Top' HorizontalAlignment='Left'>" +
				"  <Button x:Name='button' Content='button.flyout' VerticalAlignment='Top' HorizontalAlignment='Left' FontSize='25' Margin='50'>" +
				"    <Button.Flyout>" +
				"      <Flyout Placement='Top'>" +
				"          <ComboBox x:Name='comboBox' Margin='0,0,0,250'>" +
				"            <ComboBoxItem Content='TEST'/>" +
				"            <ComboBoxItem Content='TEST'/>" +
				"          </ComboBox>" +
				"      </Flyout>" +
				"    </Button.Flyout>" +
				"  </Button>" +
				"</Grid>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
			button = (Button)(rootPanel.FindName("button"));
			flyout = (Flyout)(button.Flyout);
			// TODO Uno: FindName doesn't reach a flyout's content when the root hasn't entered the tree.
			comboBox = (ComboBox)flyout.Content;
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open the Flyout hosting the ComboBox.");
		await FlyoutHelper.OpenFlyout(flyout, button, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Validate that there is one popup open.");
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(button.XamlRoot);
			VERIFY_ARE_EQUAL(popups.Count, 1);

			flyoutPresenter = FlyoutHelper.GetFlyoutPresenter(flyout);
			VERIFY_IS_NOT_NULL(flyoutPresenter);
		});

		LOG_OUTPUT("Click the open flyout.");
		TestServices.InputHelper.LeftMouseClick(flyoutPresenter);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Open the ComboBox");
		await Microsoft.UI.Xaml.Tests.Common.ComboBoxHelper.OpenComboBox(comboBox, Microsoft.UI.Xaml.Tests.Common.ComboBoxHelper.OpenMethod.Programmatic);

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Validate that there are two popups open.");
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(button.XamlRoot);
			VERIFY_ARE_EQUAL(popups.Count, 2);
		});

		await Microsoft.UI.Xaml.Tests.Common.ComboBoxHelper.CloseComboBox(comboBox);
		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Validates that setting FlowDirection to RTL causes a flyout positioned on the left to appear on the right, and vice versa.")]
	public async Task ValidateRTLReversesHorizontalPlacementModes()
	{
		var flyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Left);

		var target = await FlyoutHelper.CreateTarget(
			200 /*width*/, 200 /*height*/,
			new Thickness(10),
			HorizontalAlignment.Center,
			VerticalAlignment.Center);

		Point leftFlyoutPosition = default;
		Point rightFlyoutPosition = default;

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			leftFlyoutPosition = FlyoutHelper.GetFlyoutPresenter(flyout).TransformToVisual(null).TransformPoint(new Point(0, 0));
		});

		await FlyoutHelper.HideFlyout(flyout);

		await RunOnUIThread(() =>
		{
			flyout.Placement = FlyoutPlacementMode.Right;
		});

		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			rightFlyoutPosition = FlyoutHelper.GetFlyoutPresenter(flyout).TransformToVisual(null).TransformPoint(new Point(0, 0));

			VERIFY_IS_LESS_THAN(leftFlyoutPosition.X, rightFlyoutPosition.X);
			VERIFY_ARE_EQUAL(leftFlyoutPosition.Y, rightFlyoutPosition.Y);
		});

		await FlyoutHelper.HideFlyout(flyout);

		await RunOnUIThread(() =>
		{
			flyout.Placement = FlyoutPlacementMode.Left;
			target.FlowDirection = FlowDirection.RightToLeft;
		});

		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var flyoutPresenter = FlyoutHelper.GetFlyoutPresenter(flyout);
			var flyoutPosition = flyoutPresenter.TransformToVisual(null).TransformPoint(new Point((float)flyoutPresenter.ActualWidth, 0));

			VERIFY_ARE_VERY_CLOSE(rightFlyoutPosition.X, flyoutPosition.X, 0.01, "X"); // Uno: layout rounding works with doubles.
			VERIFY_ARE_EQUAL(rightFlyoutPosition.Y, flyoutPosition.Y);
		});

		await FlyoutHelper.HideFlyout(flyout);

		await RunOnUIThread(() =>
		{
			flyout.Placement = FlyoutPlacementMode.Right;
		});

		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var flyoutPresenter = FlyoutHelper.GetFlyoutPresenter(flyout);
			var flyoutPosition = flyoutPresenter.TransformToVisual(null).TransformPoint(new Point((float)flyoutPresenter.ActualWidth, 0));

			VERIFY_ARE_EQUAL(leftFlyoutPosition.X, flyoutPosition.X);
			VERIFY_ARE_EQUAL(leftFlyoutPosition.Y, flyoutPosition.Y);
		});

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Validates that a Flyout with no completely open positions to any side of its target is placed where the most open space exists.")]
	public async Task ValidatePlacementWithNoOpenPositions()
	{
		var leftFlyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Left);
		var rightFlyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Right);
		var topFlyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Top);
		var bottomFlyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Bottom);
		Rect windowBounds = default;

		await RunOnUIThread(() =>
		{
			windowBounds = TestServices.WindowHelper.WindowBounds;
		});

		var leftFlyoutTarget = await FlyoutHelper.CreateTarget(
			windowBounds.Width - 10, windowBounds.Height - 10,
			new Thickness(0),
			HorizontalAlignment.Right,
			VerticalAlignment.Center);
		var rightFlyoutTarget = await FlyoutHelper.CreateTarget(
			windowBounds.Width - 10, windowBounds.Height - 10,
			new Thickness(0),
			HorizontalAlignment.Left,
			VerticalAlignment.Center);
		var topFlyoutTarget = await FlyoutHelper.CreateTarget(
			windowBounds.Width - 10, windowBounds.Height - 10,
			new Thickness(0),
			HorizontalAlignment.Center,
			VerticalAlignment.Bottom);
		var bottomFlyoutTarget = await FlyoutHelper.CreateTarget(
			windowBounds.Width - 10, windowBounds.Height - 10,
			new Thickness(0),
			HorizontalAlignment.Center,
			VerticalAlignment.Top);

		Point leftFlyoutPosition = default;
		Point topFlyoutPosition = default;

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();

			rootPanel.Children.Add(leftFlyoutTarget);
			rootPanel.Children.Add(rightFlyoutTarget);
			rootPanel.Children.Add(topFlyoutTarget);
			rootPanel.Children.Add(bottomFlyoutTarget);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(leftFlyout, leftFlyoutTarget, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var flyoutPresenter = FlyoutHelper.GetFlyoutPresenter(leftFlyout);
			leftFlyoutPosition = flyoutPresenter.TransformToVisual(null).TransformPoint(new Point(0, 0));
		});

		await FlyoutHelper.HideFlyout(leftFlyout);
		await FlyoutHelper.OpenFlyout(rightFlyout, rightFlyoutTarget, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var flyoutPresenter = FlyoutHelper.GetFlyoutPresenter(rightFlyout);
			var rightFlyoutPosition = flyoutPresenter.TransformToVisual(null).TransformPoint(new Point((float)flyoutPresenter.ActualWidth, 0));

			VERIFY_IS_LESS_THAN(leftFlyoutPosition.X, rightFlyoutPosition.X);
			VERIFY_ARE_EQUAL(leftFlyoutPosition.Y, rightFlyoutPosition.Y);
		});

		await FlyoutHelper.HideFlyout(rightFlyout);
		await FlyoutHelper.OpenFlyout(topFlyout, topFlyoutTarget, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var flyoutPresenter = FlyoutHelper.GetFlyoutPresenter(topFlyout);
			topFlyoutPosition = flyoutPresenter.TransformToVisual(null).TransformPoint(new Point(0, 0));
		});

		await FlyoutHelper.HideFlyout(topFlyout);
		await FlyoutHelper.OpenFlyout(bottomFlyout, bottomFlyoutTarget, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var flyoutPresenter = FlyoutHelper.GetFlyoutPresenter(bottomFlyout);
			var bottomFlyoutPosition = flyoutPresenter.TransformToVisual(null).TransformPoint(new Point(0, (float)flyoutPresenter.ActualHeight));

			VERIFY_ARE_EQUAL(topFlyoutPosition.X, bottomFlyoutPosition.X);
			VERIFY_IS_LESS_THAN(topFlyoutPosition.Y, bottomFlyoutPosition.Y);
		});

		await FlyoutHelper.HideFlyout(bottomFlyout);
	}

	[TestMethod]
	[Description("Validates that a Flyout can be open from within another Flyout's presenter.")]
	public async Task CanOpenChildFlyout()
	{
		Button parentButton = null;
		Flyout parentFlyout = null;

		Button childButton = null;
		Flyout childFlyout = null;

		await RunOnUIThread(() =>
		{
			var rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Microsoft.UI.Colors.Orange);
			rect.Width = 100;
			rect.Height = 100;

			childFlyout = new Flyout();
			childFlyout.Content = rect;

			childButton = new Button();
			childButton.Content = "child button";

			parentFlyout = new Flyout();
			parentFlyout.Content = childButton;

			parentButton = new Button();
			parentButton.Content = "button";

			var root = new Grid();
			root.Children.Add(parentButton);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// First open our parent.
		await FlyoutHelper.OpenFlyout(parentFlyout, parentButton, FlyoutOpenMethod.Programmatic_ShowAt);

		// Now try to open the child flyout.
		await FlyoutHelper.OpenFlyout(childFlyout, childButton, FlyoutOpenMethod.Programmatic_ShowAt);

		// Validate that there are now have 2 flyout popups open.
		await RunOnUIThread(() =>
		{
			var openPopups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(openPopups.Count, 2);
		});

		Event childFlyoutClosedEvent = new();
		var childFlyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));
		childFlyoutClosedRegistration.Attach(childFlyout, (s, e) => { childFlyoutClosedEvent.Set(); });

		// Hiding the parent flyout should also close the child flyout.
		await FlyoutHelper.HideFlyout(parentFlyout);
		await childFlyoutClosedEvent.WaitForDefault();

		// There should be no open popup's now.
		await RunOnUIThread(() =>
		{
			var openPopups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(openPopups.Count, 0);
		});
	}

	[TestMethod]
	[Description("Validates that opening a flyout using press-and-hold only opens it once.")]
	public async Task ValidatePressAndHoldOnlyOpensOnce()
	{
		var flyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Left);
		var target = await FlyoutHelper.CreateTarget(
			200 /*width*/, 200 /*height*/,
			new Thickness(10),
			HorizontalAlignment.Center,
			VerticalAlignment.Center);

		var flyoutOpenedEvent = new Event();
		var flyoutOpenedCount = 0u;
		var flyoutOpenedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Opened));
		var flyoutClosedEvent = new Event();
		var flyoutClosedCount = 0u;
		var flyoutClosedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Closed));
		var holdingRegistration = CreateSafeEventRegistration<UIElement, HoldingEventHandler>(nameof(UIElement.Holding));

		flyoutOpenedCount = 0;
		flyoutClosedCount = 0;

		await RunOnUIThread(() =>
		{
			flyoutOpenedRegistration.Attach(flyout, (s, e) =>
			{
				flyoutOpenedEvent.Set();
				flyoutOpenedCount++;
			});

			flyoutClosedRegistration.Attach(flyout, (s, e) =>
			{
				flyoutClosedEvent.Set();
				flyoutClosedCount++;
			});

			holdingRegistration.Attach(target, (s, e) =>
			{
				var showOptions = new FlyoutShowOptions();
				showOptions.Position = new Point(0, 0);
				flyout.ShowAt(target, showOptions);
			});

			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await HoldWithTouch(target);

		await flyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			flyout.Hide();
		});

		await flyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		VERIFY_ARE_EQUAL(1u, flyoutOpenedCount);
		VERIFY_ARE_EQUAL(1u, flyoutClosedCount);
	}

	private static async Task HoldWithTouch(FrameworkElement target)
	{
		Point center = default;
		await RunOnUIThread(() =>
		{
			center = target.GetAbsoluteCenter();
		});

		await RunOnUIThread(async () =>
		{
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();
			await finger.Hold(center, target);
		});
	}

	[TestMethod]
	[Description("Validates that a flyout can be shown at the window's edge at various global scale factors.")]
	public async Task ValidateShowAtWindowEdge()
	{
		await ValidateShowAtWindowEdge(1.0f  /*scaleFactor*/, new Point() /*offset*/);
		await ValidateShowAtWindowEdge(1.25f /*scaleFactor*/, new Point() /*offset*/);
		await ValidateShowAtWindowEdge(1.5f  /*scaleFactor*/, new Point() /*offset*/);
		await ValidateShowAtWindowEdge(1.75f /*scaleFactor*/, new Point() /*offset*/);
		await ValidateShowAtWindowEdge(2.25f /*scaleFactor*/, new Point() /*offset*/);
	}

	[TestMethod]
	[Description("Validates that attempts to show a flyout beyond the window's edge at various global scale factors do not throw errors.")]
	public async Task ValidateShowBeyondWindowEdge()
	{
		await ValidateShowAtWindowEdge(1.0f  /*scaleFactor*/, new Point(-600, -600) /*offset*/);
		await ValidateShowAtWindowEdge(1.5f  /*scaleFactor*/, new Point(100, 100) /*offset*/);
	}

	private async Task ValidateShowAtWindowEdge(float scaleFactor, Point offset)
	{
		// There is no SetWindowSizeOverrideWithScale in Uno's TestServices: the window size override is a no-op there,
		// and the scale goes through the root scale test override.
		Size size = new Size(421, 421);
		TestServices.WindowHelper.SetWindowSizeOverride(size);
		TestServices.WindowHelper.SetTestScaling(scaleFactor);

		try
		{
			Rect windowBounds = default;

			await RunOnUIThread(() =>
			{
				windowBounds = TestServices.WindowHelper.WindowBounds;
				LOG_OUTPUT("WindowBounds: %f,%f,%f,%f.", windowBounds.X, windowBounds.Y, windowBounds.Width, windowBounds.Height);
			});

			var flyout = await FlyoutHelper.CreateDefaultFlyout(
				FlyoutPlacementMode.Right);

			var target = await FlyoutHelper.CreateTarget(
				windowBounds.Width /*width*/, windowBounds.Height /*height*/,
				new Thickness(0) /*margin*/,
				HorizontalAlignment.Right,
				VerticalAlignment.Bottom);

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>(nameof(Grid.Loaded));
			var flyoutOpenedEvent = new Event();
			var flyoutOpenedCount = 0u;
			var flyoutOpenedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Opened));
			var flyoutClosedEvent = new Event();
			var flyoutClosedCount = 0u;
			var flyoutClosedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Closed));

			flyoutOpenedCount = 0;
			flyoutClosedCount = 0;

			await RunOnUIThread(() =>
			{
				flyoutOpenedRegistration.Attach(flyout, (s, e) =>
				{
					LOG_OUTPUT("Flyout.Opened event raised.");
					flyoutOpenedEvent.Set();
					flyoutOpenedCount++;
				});

				flyoutClosedRegistration.Attach(flyout, (s, e) =>
				{
					LOG_OUTPUT("Flyout.Closed event raised.");
					flyoutClosedEvent.Set();
					flyoutClosedCount++;
				});

				var rootPanel = new Grid();
				rootPanel.Children.Add(target);

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
				LOG_OUTPUT("Showing flyout at position %f,%f.", windowBounds.Width, windowBounds.Height);
				var showOptions = new FlyoutShowOptions();
				showOptions.Position = new Point(windowBounds.Width + offset.X, windowBounds.Height + offset.Y);
				flyout.ShowAt(target, showOptions);
			});

			LOG_OUTPUT("Waiting for Opened event...");
			await flyoutOpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Hiding flyout.");
				flyout.Hide();
			});

			LOG_OUTPUT("Waiting for Closed event...");
			await flyoutClosedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Verifying event counts.");
				VERIFY_ARE_EQUAL(1u, flyoutOpenedCount);
				VERIFY_ARE_EQUAL(1u, flyoutClosedCount);
			});
		}
		finally
		{
			TestServices.WindowHelper.UnsetTestScaling();
		}
	}

	// Upstream is ignored too ("Enable ValidateFlyoutCanOpenLeftAndUpOutsideWindowBoundaries").
	// The windowed popup (ShouldConstrainToRootBounds=false) is expected to land outside of the app window.
	[TestMethod]
	[Description("Validates a flyout that wants to open with a placement of Top such that it will appear above the top of the window can do so.")]
	[Ignore("TODO Uno: needs desktop window resize/move helpers (SetDesktopWindowSize, MoveDesktopWindow, MonitorBounds), the window handle (GetUIAWindowHandle) and the popup's OS window bounds (Popup_GetWindow / popup.IsWindowed)")]
	public async Task ValidateFlyoutCanOpenLeftAndUpOutsideWindowBoundaries()
	{
#if false // TODO Uno: needs desktop window resize/move helpers, GetUIAWindowHandle, MonitorBounds and the popup's OS window bounds
		try
		{
			// We'll set the window size to something small to allow the popup to open up.
			TestServices.WindowHelper.SetDesktopWindowSize(500, 400);
			await TestServices.WindowHelper.WaitForIdle();

			HWND currentHwnd = TestServices.WindowHelper.GetUIAWindowHandle();

			RECT windowRect = {};
			VERIFY_IS_TRUE(GetWindowRect(currentHwnd, &windowRect));

			// We'll also move the window to the bottom-right of the screen in order to ensure that the popup can open left and up.
			var monitorBounds = TestServices.WindowHelper.MonitorBounds;
			int windowPositionX = (int)(monitorBounds.Width - (windowRect.right - windowRect.left));
			int windowPositionY = (int)(monitorBounds.Height - (windowRect.bottom - windowRect.top));
			TestServices.WindowHelper.MoveDesktopWindow(windowPositionX, windowPositionY);
			await TestServices.WindowHelper.WaitForIdle();

			Flyout flyout = null;

			await RunOnUIThread(() =>
			{
				flyout = new Flyout();
				flyout.ShouldConstrainToRootBounds = false;

				var rectangle = new Rectangle();
				rectangle.Width = 100;
				rectangle.Height = 100;
				rectangle.Fill = new SolidColorBrush(Microsoft.UI.Colors.Red);

				flyout.Content = rectangle;
			});

			Button target = null;

			await RunOnUIThread(() =>
			{
				target = new Button();
				target.HorizontalAlignment = HorizontalAlignment.Left;
				target.VerticalAlignment = VerticalAlignment.Top;
			});

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>(nameof(Grid.Loaded));
			var flyoutOpenedEvent = new Event();
			var flyoutOpenedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Opened));
			var flyoutClosedEvent = new Event();
			var flyoutClosedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Closed));

			await RunOnUIThread(() =>
			{
				flyoutOpenedRegistration.Attach(flyout, (s, e) =>
				{
					LOG_OUTPUT("Flyout.Opened event raised.");
					flyoutOpenedEvent.Set();
				});

				flyoutClosedRegistration.Attach(flyout, (s, e) =>
				{
					LOG_OUTPUT("Flyout.Closed event raised.");
					flyoutClosedEvent.Set();
				});

				var rootPanel = new Grid();
				rootPanel.Children.Add(target);

				loadedRegistration.Attach(rootPanel, (s, e) =>
				{
					LOG_OUTPUT("Grid.Loaded event raised.");
					loadedEvent.Set();
				});

				TestServices.WindowHelper.WindowContent = rootPanel;
			});

			await loadedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				flyout.Placement = FlyoutPlacementMode.Left;
				flyout.ShowAt(target);
			});

			await flyoutOpenedEvent.WaitForDefault();
			flyoutOpenedEvent.Reset();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var flyoutPopup = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot)[0];
				HWND flyoutHwnd = TestServices.WindowHelper.Popup_GetWindow(flyoutPopup);

				RECT flyoutRect = {};
				VERIFY_IS_TRUE(GetWindowRect(flyoutHwnd, &flyoutRect));

				LOG_OUTPUT("Opening left. Window x-position = %d; popup x-position = %d", windowPositionX, flyoutRect.left);
				VERIFY_IS_LESS_THAN(flyoutRect.left, windowPositionX);

				flyout.Hide();
			});

			await flyoutClosedEvent.WaitForDefault();
			flyoutClosedEvent.Reset();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				flyout.Placement = FlyoutPlacementMode.Top;
				flyout.ShowAt(target);
			});

			await flyoutOpenedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var flyoutPopup = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot)[0];
				HWND flyoutHwnd = TestServices.WindowHelper.Popup_GetWindow(flyoutPopup);

				RECT flyoutRect = {};
				VERIFY_IS_TRUE(GetWindowRect(flyoutHwnd, &flyoutRect));

				LOG_OUTPUT("Opening top. Window y-position = %d; popup y-position = %d", windowPositionY, flyoutRect.top);
				VERIFY_IS_LESS_THAN(flyoutRect.top, windowPositionY);

				flyout.Hide();
			});

			await flyoutClosedEvent.WaitForDefault();
			await TestServices.WindowHelper.WaitForIdle();
		}
		finally
		{
			TestServices.WindowHelper.MaximizeDesktopWindow();
			await TestServices.WindowHelper.WaitForIdle();

			TestServices.WindowHelper.ResetWindowContentAndWaitForIdle();
		}
#endif
		await Task.CompletedTask;
	}

	[TestMethod]
	[Description("Validates that opening a child Flyout while one of its sibling flyouts is open, closes the sibling and opens the child instead.")]
	public async Task CanOpenChildFlyoutWithOpenSibling()
	{
		Button parentButton = null;
		Flyout parentFlyout = null;

		Button siblingButton = null;
		Flyout siblingFlyout = null;

		Button childButton = null;
		Flyout childFlyout = null;

		await RunOnUIThread(() =>
		{
			var rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Microsoft.UI.Colors.Purple);
			rect.Width = 100;
			rect.Height = 100;

			siblingFlyout = new Flyout();
			siblingFlyout.Content = rect;

			siblingButton = new Button();
			siblingButton.Content = "sibling button";

			rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Microsoft.UI.Colors.Orange);
			rect.Width = 100;
			rect.Height = 100;

			childFlyout = new Flyout();
			childFlyout.Content = rect;

			childButton = new Button();
			childButton.Content = "child button";

			var flyoutRoot = new StackPanel();
			flyoutRoot.Children.Add(siblingButton);
			flyoutRoot.Children.Add(childButton);

			parentFlyout = new Flyout();
			parentFlyout.Content = flyoutRoot;

			parentButton = new Button();
			parentButton.Content = "button";

			var root = new Grid();
			root.Children.Add(parentButton);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Open the parent and sibling flyout to set up the test.
		await FlyoutHelper.OpenFlyout(parentFlyout, parentButton, FlyoutOpenMethod.Programmatic_ShowAt);
		await FlyoutHelper.OpenFlyout(siblingFlyout, siblingButton, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			if (popups.Count != 2)
			{
				throw new InvalidOperationException("There should be 2 popups open, one for each flyout.");
			}
		});

		Event siblingFlyoutClosedEvent = new();
		var siblingFlyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));
		siblingFlyoutClosedRegistration.Attach(siblingFlyout, (s, e) => { siblingFlyoutClosedEvent.Set(); });

		// Try to open the child flyout.
		await FlyoutHelper.OpenFlyout(childFlyout, childButton, FlyoutOpenMethod.Programmatic_ShowAt);

		// Opening our child flyout should have caused its sibling to close.
		await siblingFlyoutClosedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(popups.Count, 2);
		});

		// Hiding the parent flyout will hide our child flyout as well, so no need
		// to do it explicitly.
		await FlyoutHelper.HideFlyout(parentFlyout);
	}

	[TestMethod]
	[Description("Validates that a child flyout can be re-opened with a new placement target.")]
	public async Task CanReopenChildFlyoutWithNewPlacementTarget()
	{
		Button parentButton = null;
		Flyout parentFlyout = null;

		Button placement1 = null;
		Button placement2 = null;

		Flyout childFlyout = null;

		await RunOnUIThread(() =>
		{
			var rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Microsoft.UI.Colors.Orange);
			rect.Width = 100;
			rect.Height = 100;

			childFlyout = new Flyout();
			childFlyout.Content = rect;

			placement1 = new Button();
			placement1.Content = "placement 1";

			placement2 = new Button();
			placement2.Content = "placement 2";

			var flyoutRoot = new StackPanel();
			flyoutRoot.Children.Add(placement1);
			flyoutRoot.Children.Add(placement2);

			parentFlyout = new Flyout();
			parentFlyout.Content = flyoutRoot;

			parentButton = new Button();
			parentButton.Content = "button";

			var root = new Grid();
			root.Children.Add(parentButton);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Open the parent and the child to set up the test.
		await FlyoutHelper.OpenFlyout(parentFlyout, parentButton, FlyoutOpenMethod.Programmatic_ShowAt);
		await FlyoutHelper.OpenFlyout(childFlyout, placement1, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			if (popups.Count != 2)
			{
				throw new InvalidOperationException("There should be 2 popups open, one for each flyout.");
			}
		});

		Event childFlyoutClosedEvent = new();
		var childFlyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));
		childFlyoutClosedRegistration.Attach(childFlyout, (s, e) => { childFlyoutClosedEvent.Set(); });

		Event childFlyoutOpeningEvent = new();
		var childFlyoutOpeningRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opening));
		childFlyoutOpeningRegistration.Attach(childFlyout, (s, e) => { childFlyoutOpeningEvent.Set(); });

		Event childFlyoutOpenedEvent = new();
		var childFlyoutOpenedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		childFlyoutOpenedRegistration.Attach(childFlyout, (s, e) => { childFlyoutOpenedEvent.Set(); });

		// Re-open the child with a different placement target.
		await FlyoutHelper.OpenFlyout(childFlyout, placement2, FlyoutOpenMethod.Programmatic_ShowAt);

		// Opening our child flyout with a new placement target should have caused it
		// to close and then re-open.
		await childFlyoutClosedEvent.WaitForDefault();
		await childFlyoutOpeningEvent.WaitForDefault();
		await childFlyoutOpenedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(popups.Count, 2);
		});

		// Hiding the parent flyout will hide our child flyout as well, so no need
		// to do it explicitly.
		await FlyoutHelper.HideFlyout(parentFlyout);
	}

	[TestMethod]
	[Description("Validates that a Flyout targeting an item in a ListView can be open from within another Flyout's presenter.")]
	public async Task CanOpenChildFlyoutTargetingItemInListView()
	{
		Button parentButton = null;
		Flyout parentFlyout = null;

		FrameworkElement childTarget = null;
		Flyout childFlyout = null;

		ListView listView = null;

		await RunOnUIThread(() =>
		{
			var rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Microsoft.UI.Colors.Orange);
			rect.Width = 100;
			rect.Height = 100;

			childFlyout = new Flyout();
			childFlyout.Content = rect;

			listView = new ListView();
			listView.Items.Add("Item");

			parentFlyout = new Flyout();
			parentFlyout.Content = listView;

			parentButton = new Button();
			parentButton.Content = "button";

			var root = new Grid();
			root.Children.Add(parentButton);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// First open our parent.
		await FlyoutHelper.OpenFlyout(parentFlyout, parentButton, FlyoutOpenMethod.Programmatic_ShowAt);

		// Do this after the hosting flyout has opened so that the containers will have been realized.
		await RunOnUIThread(() =>
		{
			childTarget = (FrameworkElement)(listView.ContainerFromIndex(0));
		});

		// Now try to open the child flyout.
		await FlyoutHelper.OpenFlyout(childFlyout, childTarget, FlyoutOpenMethod.Programmatic_ShowAt);

		// Validate that there are now have 2 flyout popups open.
		await RunOnUIThread(() =>
		{
			var openPopups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(openPopups.Count, 2);
		});

		Event childFlyoutClosedEvent = new();
		var childFlyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));
		childFlyoutClosedRegistration.Attach(childFlyout, (s, e) => { childFlyoutClosedEvent.Set(); });

		// Hiding the parent flyout should also close the child flyout.
		await FlyoutHelper.HideFlyout(parentFlyout);
		await childFlyoutClosedEvent.WaitForDefault();

		// There should be no open popup's now.
		await RunOnUIThread(() =>
		{
			var openPopups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(openPopups.Count, 0);
		});
	}

	[TestMethod]
	[Description("Validates that opening a new root flyout (not the child of an already open flyout) closes an already open flyout chain.")]
	public async Task DoesFlyoutChainCloseWhenOpeningNewRootFlyout()
	{
		Button parentButton = null;
		Flyout parentFlyout = null;

		Button childButton = null;
		Flyout childFlyout = null;

		Flyout otherFlyout = null;

		await RunOnUIThread(() =>
		{
			var rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Microsoft.UI.Colors.Purple);
			rect.Width = 100;
			rect.Height = 100;

			childFlyout = new Flyout();
			childFlyout.Content = rect;

			childButton = new Button();
			childButton.Content = "child button";

			parentFlyout = new Flyout();
			parentFlyout.Content = childButton;

			parentButton = new Button();
			parentButton.Content = "button";

			rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Microsoft.UI.Colors.Orange);
			rect.Width = 100;
			rect.Height = 100;

			otherFlyout = new Flyout();
			otherFlyout.Content = rect;

			var root = new StackPanel();
			root.Children.Add(parentButton);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Open the parent and the child to set up the test.
		await FlyoutHelper.OpenFlyout(parentFlyout, parentButton, FlyoutOpenMethod.Programmatic_ShowAt);
		await FlyoutHelper.OpenFlyout(childFlyout, childButton, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			if (popups.Count != 2)
			{
				throw new InvalidOperationException("There should be 2 popups open, one for each flyout.");
			}
		});

		Event childFlyoutClosedEvent = new();
		var childFlyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));
		childFlyoutClosedRegistration.Attach(childFlyout, (s, e) => { childFlyoutClosedEvent.Set(); });

		Event parentFlyoutClosedEvent = new();
		var parentFlyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));
		parentFlyoutClosedRegistration.Attach(parentFlyout, (s, e) => { parentFlyoutClosedEvent.Set(); });

		// Open a new root flyout.
		await FlyoutHelper.OpenFlyout(otherFlyout, parentButton, FlyoutOpenMethod.Programmatic_ShowAt);

		// Opening a new root flyout should close the previous chain.
		await childFlyoutClosedEvent.WaitForDefault();
		await parentFlyoutClosedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(popups.Count, 1);
		});

		await FlyoutHelper.HideFlyout(otherFlyout);
	}

	[TestMethod]
	[Description("Validates that a child flyout unloads when its placement target unloads.")]
	public async Task DoesChildFlyoutCloseWhenPlacementTargetUnloads()
	{
		Button parentButton = null;
		Flyout parentFlyout = null;
		Grid parentFlyoutRoot = null;

		Button childButton = null;
		Flyout childFlyout = null;

		await RunOnUIThread(() =>
		{
			var rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Microsoft.UI.Colors.Orange);
			rect.Width = 100;
			rect.Height = 100;

			childFlyout = new Flyout();
			childFlyout.Content = rect;

			childButton = new Button();
			childButton.Content = "child button";

			parentFlyoutRoot = new Grid();
			parentFlyoutRoot.Children.Add(childButton);

			parentFlyout = new Flyout();
			parentFlyout.Content = parentFlyoutRoot;

			parentButton = new Button();
			parentButton.Content = "button";

			var root = new Grid();
			root.Children.Add(parentButton);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Open the parent and child to setup the test.
		await FlyoutHelper.OpenFlyout(parentFlyout, parentButton, FlyoutOpenMethod.Programmatic_ShowAt);
		await FlyoutHelper.OpenFlyout(childFlyout, childButton, FlyoutOpenMethod.Programmatic_ShowAt);

		// Validate that there are now have 2 flyout popups open.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			if (popups.Count != 2)
			{
				throw new InvalidOperationException("There should be 2 popups open, one for each flyout.");
			}
		});

		Event childFlyoutClosedEvent = new();
		var childFlyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));
		childFlyoutClosedRegistration.Attach(childFlyout, (s, e) => { childFlyoutClosedEvent.Set(); });

		// Remove the child's placement target from the tree, which should close the child flyout.
		await RunOnUIThread(() =>
		{
			parentFlyoutRoot.Children.Clear();
		});
		await childFlyoutClosedEvent.WaitForDefault();

		// There should be 1 open popup now, the parent's.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(popups.Count, 1);
		});

		await FlyoutHelper.HideFlyout(parentFlyout);
	}

	[TestMethod]
	[Description("Validates that a child MenuFlyout can be opened from within another flyout.")]
	public async Task CanOpenChildMenuFlyout()
	{
		Button parentButton = null;
		Flyout parentFlyout = null;

		Button menuFlyoutButton = null;
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = new MenuFlyout();

			for (int i = 0; i < 5; ++i)
			{
				var item = new MenuFlyoutItem();
				item.Text = "menu item";
				menuFlyout.Items.Add(item);
			}

			menuFlyoutButton = new Button();
			menuFlyoutButton.Content = "menu flyout button";

			parentFlyout = new Flyout();
			parentFlyout.Content = menuFlyoutButton;

			parentButton = new Button();
			parentButton.Content = "button";

			var root = new Grid();
			root.Children.Add(parentButton);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Open the flyout to setup the test.
		await FlyoutHelper.OpenFlyout(parentFlyout, parentButton, FlyoutOpenMethod.Programmatic_ShowAt);

		// Validate that there are now have 1 flyout popup open.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			if (popups.Count != 1)
			{
				throw new InvalidOperationException("There should be 1 popup open.");
			}
		});

		// Try to open the menu flyout.
		await FlyoutHelper.OpenFlyout(menuFlyout, menuFlyoutButton, FlyoutOpenMethod.Programmatic_ShowAt);

		// Validate that there are now have 2 flyout popups open.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(popups.Count, 2);
		});

		// Hiding the parent flyout should close the child MenuFlyout as well.
		await FlyoutHelper.HideFlyout(parentFlyout);
	}

	[TestMethod]
	[Description("Validates that a child Date/TimePickerFlyout can be opened from within another flyout.")]
	public async Task CanOpenChildDateTimePickerFlyout()
	{
		await CanOpenDateTimePickerWorker<DatePicker>();
		await CanOpenDateTimePickerWorker<TimePicker>();
	}

	private async Task CanOpenDateTimePickerWorker<TDateTimePicker>()
		where TDateTimePicker : FrameworkElement, new()
	{
		Button parentButton = null;
		Flyout parentFlyout = null;

		TDateTimePicker dateTimePicker = null;

		await RunOnUIThread(() =>
		{
			dateTimePicker = new TDateTimePicker();

			parentFlyout = new Flyout();
			parentFlyout.Content = dateTimePicker;

			parentButton = new Button();
			parentButton.Content = "button";

			var root = new Grid();
			root.Children.Add(parentButton);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Open the flyout to setup the test.
		await FlyoutHelper.OpenFlyout(parentFlyout, parentButton, FlyoutOpenMethod.Programmatic_ShowAt);

		// Validate that there are now have 1 flyout popup open.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			if (popups.Count != 1)
			{
				throw new InvalidOperationException("There should be 1 popup open.");
			}
		});

		// Try to open the DatePicker from within the flyout.
		await DateTimePickerHelper.OpenDateTimePicker(dateTimePicker);

		// Validate that there are now have 2 flyout popups open.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(popups.Count, 2);
		});

		// Hiding the parent flyout should close the child DatePickerFlyout as well.
		await FlyoutHelper.HideFlyout(parentFlyout);
	}

	[TestMethod]
	[Description("Validates that a child flyout containing a CalendarView can be opened from within another flyout.")]
	public async Task CanOpenChildCalenderViewFlyout()
	{
		Button parentButton = null;
		Flyout parentFlyout = null;

		CalendarDatePicker calendarDatePicker = null;

		await RunOnUIThread(() =>
		{
			calendarDatePicker = new CalendarDatePicker();

			parentFlyout = new Flyout();
			parentFlyout.Content = calendarDatePicker;

			parentButton = new Button();
			parentButton.Content = "button";

			var root = new Grid();
			root.Children.Add(parentButton);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Open the flyout to setup the test.
		await FlyoutHelper.OpenFlyout(parentFlyout, parentButton, FlyoutOpenMethod.Programmatic_ShowAt);

		// Validate that there are now have 1 flyout popup open.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			if (popups.Count != 1)
			{
				throw new InvalidOperationException("There should be 1 popup open.");
			}

			// Try to open the DatePicker from within the flyout.
			calendarDatePicker.IsCalendarOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Validate that there are now have 2 flyout popups open.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(popups.Count, 2);
		});

		// Hiding the parent flyout should close the child CalendarView flyout as well.
		await FlyoutHelper.HideFlyout(parentFlyout);
	}

	[TestMethod]
	[Description("Validates that right-clicking will dismiss an open chain of flyouts and invoke the handler on the next hit-testable element.")]
	public async Task DoesChainRightClickWithChildFlyouts()
	{
		Button target = null;
		Flyout parentFlyout = null;

		Button childButton1 = null;
		Flyout childFlyout1 = null;

		Button childButton2 = null;
		Flyout childFlyout2 = null;

		var targetRightTappedRegistration = CreateSafeEventRegistration<Button, RightTappedEventHandler>(nameof(Button.RightTapped));
		bool wasTargetRightTapped = false;

		await RunOnUIThread(() =>
		{
			var rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Microsoft.UI.Colors.Orange);
			rect.Width = 100;
			rect.Height = 100;

			childFlyout2 = new Flyout();
			childFlyout2.Placement = FlyoutPlacementMode.Bottom;
			childFlyout2.Content = rect;

			childButton2 = new Button();
			childButton2.Content = "child button 2";

			childFlyout1 = new Flyout();
			childFlyout1.Placement = FlyoutPlacementMode.Bottom;
			childFlyout1.Content = childButton2;

			childButton1 = new Button();
			childButton1.Content = "child button 1";

			parentFlyout = new Flyout();
			parentFlyout.Placement = FlyoutPlacementMode.Bottom;
			parentFlyout.Content = childButton1;

			target = new Button();
			target.VerticalAlignment = VerticalAlignment.Top;
			target.Content = "button";
			target.Height = 100;

			targetRightTappedRegistration.Attach(target, (s, e) => { wasTargetRightTapped = true; });

			var root = new Grid();
			root.Children.Add(target);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Open our flyouts to setup the test.
		await FlyoutHelper.OpenFlyout(parentFlyout, target, FlyoutOpenMethod.Programmatic_ShowAt);
		await FlyoutHelper.OpenFlyout(childFlyout1, childButton1, FlyoutOpenMethod.Programmatic_ShowAt);
		await FlyoutHelper.OpenFlyout(childFlyout2, childButton2, FlyoutOpenMethod.Programmatic_ShowAt);

		// Validate that there are now have 3 flyout popups open.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(target.XamlRoot);
			if (popups.Count != 3)
			{
				throw new InvalidOperationException("There should be 3 popups open.");
			}
		});

		// Inject right-click.
		var targetCenter = await ControlHelper.GetCenterOfElement(target);
		TestServices.InputHelper.ClickMouseButton(MouseButton.Right, targetCenter);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// There should be no popups open now becaue the flyouts should have closed.
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(target.XamlRoot);
			if (popups.Count != 0)
			{
				throw new InvalidOperationException("The flyouts should have closed.");
			}

			// The right-click should have been chained, so the target's handler should have
			// been invoked.
			VERIFY_IS_TRUE(wasTargetRightTapped);
		});
	}

	[TestMethod]
	[Description("Validates that it is possible to cancel closing a flyout.")]
	public async Task CanCancelClosing()
	{
		Button button = null;
		Flyout flyout = null;

		await RunOnUIThread(() =>
		{
			// Don't put a focusable control in the popup because we want to
			// test dismissing the popup via its key handlers, so it needs to
			// get focus.
			var textBlock = new TextBlock();
			textBlock.Text = "You can cancel closing me!";

			flyout = new Flyout();
			flyout.Content = textBlock;

			button = new Button();
			button.Content = "button";

			var root = new Grid();
			root.Children.Add(button);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Open the flyout to setup the test.
		await FlyoutHelper.OpenFlyout(flyout, button, FlyoutOpenMethod.Programmatic_ShowAt);

		// Validate that there is 1 flyout popup open.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(button.XamlRoot);
			if (popups.Count != 1)
			{
				throw new InvalidOperationException("There should be 1 popup open.");
			}
		});

		Event closingEvent = new();
		var closingRegistration = CreateSafeEventRegistration<Flyout, TypedEventHandler<FlyoutBase, FlyoutBaseClosingEventArgs>>(nameof(Flyout.Closing));
		closingRegistration.Attach(
			flyout,
			(s, args) =>
			{
				args.Cancel = true;
				closingEvent.Set();
			});

		// A list of the various scenarios that could cause a flyout to close; we want
		// to make sure that each raises a Closing event which can be canceled.
		List<(string, Func<Task>)> closingScenarios = new();
		closingScenarios.Add(("Programmatic", async () => { await RunOnUIThread(() => { flyout.Hide(); }); }));
		closingScenarios.Add(("Touch light-dismiss", () => { TestServices.InputHelper.Tap(button); return Task.CompletedTask; }));
		closingScenarios.Add(("Keyboard cancel", async () => { await CommonInputHelper.Cancel(InputDevice.Keyboard); }));
		closingScenarios.Add(("Gamepad cancel", async () => { await CommonInputHelper.Cancel(InputDevice.Gamepad); }));

		foreach (var scenario in closingScenarios)
		{
			LOG_OUTPUT("Try to dismiss the flyout: %s", scenario.Item1);
			await scenario.Item2();
			await TestServices.WindowHelper.WaitForIdle();
			await closingEvent.WaitForDefault();
			closingEvent.Reset();

			// Validate that there is still 1 flyout popup open.
			await RunOnUIThread(() =>
			{
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(button.XamlRoot);
				VERIFY_ARE_EQUAL(popups.Count, 1);
			});
		}

		// Detach the Closing event registration before trying to hide the flyout to
		// make sure we properly cleanup.
		closingRegistration.Detach();
		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Validates that it is possible to cancel closing a child flyout.")]
	public async Task CanCancelChildClosing()
	{
		Button parentButton = null;
		Flyout parentFlyout = null;

		Button childButton1 = null;
		Flyout childFlyout1 = null;

		Button childButton2 = null;
		Flyout childFlyout2 = null;

		await RunOnUIThread(() =>
		{
			var rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Microsoft.UI.Colors.Orange);
			rect.Width = 100;
			rect.Height = 100;

			childFlyout2 = new Flyout();
			childFlyout2.Content = rect;

			childButton2 = new Button();
			childButton2.Content = "child button 2";

			childFlyout1 = new Flyout();
			childFlyout1.Content = childButton2;

			childButton1 = new Button();
			childButton1.Content = "child button 1";

			parentFlyout = new Flyout();
			parentFlyout.Content = childButton1;

			parentButton = new Button();
			parentButton.Content = "button";

			var root = new Grid();
			root.Children.Add(parentButton);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Open our flyouts to setup the test.
		await FlyoutHelper.OpenFlyout(parentFlyout, parentButton, FlyoutOpenMethod.Programmatic_ShowAt);
		await FlyoutHelper.OpenFlyout(childFlyout1, childButton1, FlyoutOpenMethod.Programmatic_ShowAt);
		await FlyoutHelper.OpenFlyout(childFlyout2, childButton2, FlyoutOpenMethod.Programmatic_ShowAt);

		// Validate that there are now have 3 flyout popups open.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			if (popups.Count != 3)
			{
				throw new InvalidOperationException("There should be 3 popups open.");
			}
		});

		Event closingEvent = new();
		var closingRegistration = CreateSafeEventRegistration<Flyout, TypedEventHandler<FlyoutBase, FlyoutBaseClosingEventArgs>>(nameof(Flyout.Closing));
		closingRegistration.Attach(
			childFlyout1,
			(s, args) =>
			{
				args.Cancel = true;
				closingEvent.Set();
			});

		// Hiding the parent flyout, but the child flyouts should remain
		// open because we cancel closing them.
		await FlyoutHelper.HideFlyout(parentFlyout);

		// Validate that the child flyouts are still open.
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(parentButton.XamlRoot);
			VERIFY_ARE_EQUAL(popups.Count, 2);
		});

		// Detach the Closing event registration before trying to hide the flyout to
		// make sure we properly cleanup.  This should also close any child flyouts
		// to this flyout.
		closingRegistration.Detach();
		await FlyoutHelper.HideFlyout(childFlyout1);
	}

	[TestMethod]
	[Description("Validates the behavior of the LightDismissOverlayMode property.")]
	public async Task ValidateLightDismissOverlayMode()
	{
		Flyout flyout = null;
		Button target = null;

		await RunOnUIThread(() =>
		{
			flyout = new Flyout();
			target = new Button();

			TestServices.WindowHelper.WindowContent = target;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		LOG_OUTPUT("Validate that the default is Auto and the flyout's popup is set to Off (or On if on Xbox)");
		{
			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(flyout.LightDismissOverlayMode, LightDismissOverlayMode.Auto);
			});

			await ValidateFlyoutPopupLightDismissOverlayMode(
				TestServices.Utilities.IsXBox ? LightDismissOverlayMode.On : LightDismissOverlayMode.Off);
		}

		LOG_OUTPUT("Validate that when set to On the flyout's popup is also set to On.");
		{
			await RunOnUIThread(() =>
			{
				flyout.LightDismissOverlayMode = LightDismissOverlayMode.On;
			});

			await ValidateFlyoutPopupLightDismissOverlayMode(LightDismissOverlayMode.On);
		}

		LOG_OUTPUT("Validate that when set to Off the flyout's popup is also set to Off.");
		{
			await RunOnUIThread(() =>
			{
				flyout.LightDismissOverlayMode = LightDismissOverlayMode.Off;
			});

			await ValidateFlyoutPopupLightDismissOverlayMode(LightDismissOverlayMode.Off);
		}

		await FlyoutHelper.HideFlyout(flyout);
	}

	// Upstream is ignored: "Validates that setting LightDismissOverlayMode to Auto on Xbox causes the popup overlay to get configured to On."
	[TestMethod]
	[Description("Validates that setting LightDismissOverlayMode to Auto on Xbox causes the popup overlay to get configured to On.")]
	[Ignore("Not applicable: Uno doesn't run on Xbox and upstream ignores this test too.")]
	public async Task DoesAutoLightDismissOverlayModeCreateOverlayOnXbox()
	{
		Flyout flyout = null;
		Button target = null;

		await RunOnUIThread(() =>
		{
			flyout = new Flyout();
			flyout.LightDismissOverlayMode = LightDismissOverlayMode.Auto;

			target = new Button();
			TestServices.WindowHelper.WindowContent = target;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await ValidateFlyoutPopupLightDismissOverlayMode(LightDismissOverlayMode.On);

		await FlyoutHelper.HideFlyout(flyout);
	}

	private static async Task ValidateFlyoutPopupLightDismissOverlayMode(LightDismissOverlayMode expectedMode)
	{
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot);
			if (popups.Count != 1)
			{
				throw new InvalidOperationException("Expected exactly one open Popup.");
			}

			var flyoutPopup = popups[0];
			VERIFY_ARE_EQUAL(flyoutPopup.LightDismissOverlayMode, expectedMode);

			if (expectedMode == LightDismissOverlayMode.On)
			{
				var overlayElement = TestServices.Utilities.GetPopupOverlayElement(flyoutPopup);
				VERIFY_IS_NOT_NULL(overlayElement);
			}
		});
	}

	[TestMethod]
	[Description("Validates that the brush used for the Overlay matches the 'FlyoutLightDismissOverlayBackground' resource.")]
	public async Task ValidateOverlayBrush()
	{
		await FlyoutHelper.ValidateOverlayBrush<Flyout>("FlyoutLightDismissOverlayBackground");
	}

	[TestMethod]
	[Description("Validates that designating an element as the light-dismiss pass-through element in XAML allows pointer events to route to it through the light-dismiss layer.")]
	public async Task ValidatePointerEventsCanBeRoutedThroughLightDismissLayerSetInXaml()
	{
		Button buttonPassThrough = null;
		Button buttonNonPassThrough = null;
		Button flyoutTarget = null;

		var loadedEvent = new Event();
		var loadedEventRegistration = CreateSafeEventRegistration<StackPanel, RoutedEventHandler>(nameof(StackPanel.Loaded));

		await RunOnUIThread(() =>
		{
			var root = (StackPanel)(XamlReader.Load(
				"<StackPanel Margin='100' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
				"  <Button x:Name='buttonPassThrough'>Pass-through element</Button>" +
				"  <Button x:Name='buttonNonPassThrough'>Non-pass-through element</Button>" +
				"  <Button x:Name='flyoutTarget' Content='Click to open flyout'>" +
				"    <Button.Flyout>" +
				"      <Flyout Placement='Bottom' OverlayInputPassThroughElement='{Binding ElementName=buttonPassThrough}'/>" +
				"    </Button.Flyout>" +
				"  </Button>" +
				"</StackPanel>"));

			buttonPassThrough = (Button)(root.FindName("buttonPassThrough"));
			buttonNonPassThrough = (Button)(root.FindName("buttonNonPassThrough"));
			flyoutTarget = (Button)(root.FindName("flyoutTarget"));

			loadedEventRegistration.Attach(root, (s, e) => { loadedEvent.Set(); });
			TestServices.WindowHelper.WindowContent = root;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await ValidatePointerEventsCanBeRoutedThroughLightDismissLayer(
			buttonPassThrough,
			buttonNonPassThrough,
			flyoutTarget);
	}

	// Upstream is ignored: "[DCPPTest] Xaml tests are failing because Xaml no longer applies the plateau scale".
	[TestMethod]
	[Description("Validates that designating an element as the light-dismiss pass-through element in XAML allows pointer events to route to it through the light-dismiss layer in HiDpi.")]
	public async Task ValidatePointerEventsCanBeRoutedThroughLightDismissLayerHiDpi()
	{
		// TestServices::WindowHelper->ResetWindowContentAndScaleWaitForIdle(2.0f);
		TestServices.WindowHelper.SetTestScaling(2.0f);
		try
		{
			await TestServices.WindowHelper.WaitForIdle();
			await ValidatePointerEventsCanBeRoutedThroughLightDismissLayerSetInXaml();
		}
		finally
		{
			TestServices.WindowHelper.UnsetTestScaling();
		}
	}

	[TestMethod]
	[Description("Validates that designating an element as the light-dismiss pass-through element in code-behind allows pointer events to route to it through the light-dismiss layer.")]
	public async Task ValidatePointerEventsCanBeRoutedThroughLightDismissLayerManuallySet()
	{
		Button buttonPassThrough = null;
		Button buttonNonPassThrough = null;
		Button flyoutTarget = null;

		var loadedEvent = new Event();
		var loadedEventRegistration = CreateSafeEventRegistration<StackPanel, RoutedEventHandler>(nameof(StackPanel.Loaded));

		await RunOnUIThread(() =>
		{
			buttonPassThrough = new Button();
			buttonPassThrough.Content = "Pass-through element";

			buttonNonPassThrough = new Button();
			buttonNonPassThrough.Content = "Non-pass-through element";

			flyoutTarget = new Button();
			flyoutTarget.Content = "Click to open flyout";

			var flyout = new Flyout();
			flyout.Placement = FlyoutPlacementMode.Bottom;
			flyout.OverlayInputPassThroughElement = buttonPassThrough;
			flyoutTarget.Flyout = flyout;

			var root = new StackPanel();
			root.Margin = new Thickness(10, 10, 10, 10);
			root.Children.Add(buttonPassThrough);
			root.Children.Add(buttonNonPassThrough);
			root.Children.Add(flyoutTarget);

			loadedEventRegistration.Attach(root, (s, e) => { loadedEvent.Set(); });
			TestServices.WindowHelper.WindowContent = root;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await ValidatePointerEventsCanBeRoutedThroughLightDismissLayer(
			buttonPassThrough,
			buttonNonPassThrough,
			flyoutTarget);
	}

	private async Task ValidatePointerEventsCanBeRoutedThroughLightDismissLayer(
		Button buttonPassThrough,
		Button buttonNonPassThrough,
		Button flyoutTarget)
	{
		FlyoutBase flyout = null;
		VisualStateGroup commonStates = null;

		var pointerOverStateEvent = new Event();
		var passThroughClickEvent = new Event();
		var nonPassThroughClickEvent = new Event();
		var closedEvent = new Event();
		var currentStateChangedRegistration = CreateSafeEventRegistration<VisualStateGroup, VisualStateChangedEventHandler>(nameof(VisualStateGroup.CurrentStateChanged));
		var passThroughClickEventRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>(nameof(Button.Click));
		var nonPassThroughClickEventRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>(nameof(Button.Click));
		var closedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Closed));

		await RunOnUIThread(() =>
		{
			flyout = flyoutTarget.Flyout;
			commonStates = GetVisualStateGroup(buttonPassThrough, "CommonStates");
		});

		await RunOnUIThread(() =>
		{
			currentStateChangedRegistration.Attach(
				commonStates,
				(sender, e) =>
				{
					if (e.NewState.Name == "PointerOver")
					{
						pointerOverStateEvent.Set();
					}
				});

			passThroughClickEventRegistration.Attach(buttonPassThrough, (s, e) => { passThroughClickEvent.Set(); });
			nonPassThroughClickEventRegistration.Attach(buttonNonPassThrough, (s, e) => { nonPassThroughClickEvent.Set(); });
			closedRegistration.Attach(flyout, (s, e) => { closedEvent.Set(); });
		});

		LOG_OUTPUT("Open the flyout by clicking the button.");
		await FlyoutHelper.OpenFlyout(flyout, flyoutTarget, FlyoutOpenMethod.Mouse);

		LOG_OUTPUT("We should be able to put the pass-through element into the pointer-over state by mousing over it, even with the flyout open.");
		TestServices.InputHelper.MoveMouse(buttonPassThrough);

		await pointerOverStateEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("That shouldn't close the flyout, though.");
			VERIFY_ARE_EQUAL(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot).Count);
		});

		LOG_OUTPUT("And clicking it should raise the click event while closing it.");
		TestServices.InputHelper.LeftMouseClick(buttonPassThrough);

		await passThroughClickEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await closedEvent.WaitForDefault();
		closedEvent.Reset();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot).Count);
		});

		LOG_OUTPUT("Now open the flyout again.");
		await FlyoutHelper.OpenFlyout(flyout, flyoutTarget, FlyoutOpenMethod.Mouse);

		LOG_OUTPUT("Mousing over the non-pass-through element with the flyout open should not change its visual state.");
		TestServices.InputHelper.MoveMouse(buttonNonPassThrough);
		await TestServices.WindowHelper.WaitForIdle();

		VERIFY_IS_TRUE(await ControlHelper.IsInVisualState(buttonNonPassThrough, "CommonStates", "Normal"));

		LOG_OUTPUT("And clicking it should not raise the click event.");
		TestServices.InputHelper.LeftMouseClick(buttonNonPassThrough);

		await TestServices.WindowHelper.WaitForIdle();
		VERIFY_IS_FALSE(nonPassThroughClickEvent.HasFired());

		await closedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	private static VisualStateGroup GetVisualStateGroup(FrameworkElement control, string groupName)
	{
		var visualStateGroups = VisualStateManager.GetVisualStateGroups(control);
		if ((visualStateGroups == null || visualStateGroups.Count == 0) && VisualTreeHelper.GetChildrenCount(control) > 0)
		{
			visualStateGroups = VisualStateManager.GetVisualStateGroups((FrameworkElement)VisualTreeHelper.GetChild(control, 0));
		}

		if (visualStateGroups == null)
		{
			return null;
		}

		foreach (var visualStateGroup in visualStateGroups)
		{
			if (visualStateGroup.Name == groupName)
			{
				return visualStateGroup;
			}
		}

		return null;
	}

	[TestMethod]
	[Description("Validates that OnPresenterLoaded being raised without ShowAt having first been called does not crash the app.")]
	[Ignore("Not applicable: Uno runs OnPresenterUnloaded from Popup.Closed, so swapping Popup.Child cannot reach OnPresenterLoaded without a placement target, and Unloaded is synchronous within that setter.")]
	public async Task ValidatePresenterLoadedWithoutShowAtDoesNotCrash()
	{
		Button flyoutTarget = null;
		FlyoutBase flyout = null;
		FlyoutPresenter flyoutPresenter = null;
		Popup flyoutPopup = null;

		var rootLoadedEvent = new Event();
		var flyoutOpenedEvent = new Event();
		var presenterUnloadedEvent = new Event();
		var presenterLoadedEvent = new Event();
		var rootLoadedEventRegistration = CreateSafeEventRegistration<StackPanel, RoutedEventHandler>(nameof(StackPanel.Loaded));
		var flyoutOpenedEventRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Opened));
		var presenterUnloadedEventRegistration = CreateSafeEventRegistration<FlyoutPresenter, RoutedEventHandler>(nameof(FlyoutPresenter.Unloaded));
		var presenterLoadedEventRegistration = CreateSafeEventRegistration<FlyoutPresenter, RoutedEventHandler>(nameof(FlyoutPresenter.Loaded));

		await RunOnUIThread(() =>
		{
			var root = (StackPanel)(XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
				"  <Button x:Name='FlyoutTarget' Content='Click to open flyout'>" +
				"    <Button.Flyout>" +
				"      <Flyout ShouldConstrainToRootBounds='True' />" +
				"    </Button.Flyout>" +
				"  </Button>" +
				"</StackPanel>"));

			flyoutTarget = (Button)(root.FindName("FlyoutTarget"));
			flyout = flyoutTarget.Flyout;

			// In very specific timing circumstances, we can incur the occurrence where
			// FlyoutBase::OnPresenterLoaded() is called after FlyoutBase::OnPresenterUnloaded()
			// without ShowAt() having ever been called.  We want to ensure that we don't crash
			// with a null placement target in this circumstance.
			flyoutOpenedEventRegistration.Attach(flyout,
				(s, e) =>
				{
					flyoutPopup = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot)[0];
					flyoutPresenter = (FlyoutPresenter)(flyoutPopup.Child);

					presenterUnloadedEventRegistration.Attach(flyoutPresenter,
						(s2, e2) =>
						{
							flyoutPopup.Child = flyoutPresenter;
							flyoutPopup.IsOpen = true;
							presenterUnloadedEvent.Set();
							presenterUnloadedEventRegistration.Detach();
						});

					presenterLoadedEventRegistration.Attach(flyoutPresenter,
						(s2, e2) =>
						{
							presenterLoadedEvent.Set();
							presenterLoadedEventRegistration.Detach();
						});

					flyoutPopup.Child = null;
					flyoutOpenedEvent.Set();

					flyoutOpenedEventRegistration.Detach();
				});

			rootLoadedEventRegistration.Attach(root, (s, e) => { rootLoadedEvent.Set(); });
			TestServices.WindowHelper.WindowContent = root;
		});

		await rootLoadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			flyout.ShowAt(flyoutTarget);
		});

		await flyoutOpenedEvent.WaitForDefault();
		await presenterUnloadedEvent.WaitForDefault();
		await presenterLoadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Validates that the position of the flyout is as set in onOpening method for the first time.")]
	public async Task ValidateFlyoutTakesThePositionItsSetToTheFirstTime()
	{
		Rectangle rectangle = null;
		Flyout flyout = null;
		Grid flyoutGrid = null;

		var loadedEvent = new Event();
		var loadedEventRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>(nameof(Grid.Loaded));
		var openingRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opening));

		Point rightFlyoutPosition = default;
		Point rightRectanglePosition = default;

		await RunOnUIThread(() =>
		{
			var root = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
				"  <Rectangle x:Name='Rectangle' Width = '100' Height = '40' Fill = 'Gray'> " +
				"      <Rectangle.ContextFlyout>" +
				"          <Flyout x:Name='Flyout' Opening = 'Flyout_Opening'>" +
				"              <Grid x:Name='FlyoutGrid' Width = '300' Height = '300' />" +
				"          </Flyout>" +
				"      </Rectangle.ContextFlyout>" +
				"  </Rectangle>" +
				"</Grid>"));

			rectangle = (Rectangle)(root.FindName("Rectangle"));
			// TODO Uno: FindName can't reach flyouts until the namescope work lands (uno--findname-misses-resource-and-flyout-names-in-xamlreader).
			flyout = (Flyout)rectangle.ContextFlyout;
			flyoutGrid = (Grid)flyout.Content;

			loadedEventRegistration.Attach(root, (s, e) => { loadedEvent.Set(); });
			TestServices.WindowHelper.WindowContent = root;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		openingRegistration.Attach(
			flyout,
			(s, e) =>
			{
				LOG_OUTPUT("===========================================================================================");
				LOG_OUTPUT("VerifyFlyoutContentChange: Flyout Opening event is fired!");
				LOG_OUTPUT("===========================================================================================");
				flyout.SetValue(Flyout.PlacementProperty, FlyoutPlacementMode.Right);
			});

		// Inject right-click.
		var rectangleCenter = await ControlHelper.GetCenterOfElement(rectangle);
		TestServices.InputHelper.ClickMouseButton(MouseButton.Right, rectangleCenter);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			rightFlyoutPosition = FlyoutHelper.GetFlyoutPresenter(flyout).TransformToVisual(null).TransformPoint(new Point(0, 0));
			rightRectanglePosition = rectangle.TransformToVisual(null).TransformPoint(new Point(0, 0));

			VERIFY_IS_LESS_THAN(rightRectanglePosition.X, rightFlyoutPosition.X);
		});

		// Not doing that causes a memory leak
		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Validates if a flyout is opened from another flyout, we can right-click on the original flyout to open a third flyout without that right-click hiding the original flyout.")]
	public async Task ValidateRightClickCanHideOneFlyoutWithoutHidingAllFlyouts()
	{
		Rectangle rectangle = null;
		Flyout flyout = null;
		Rectangle innerRectangle1 = null;
		Rectangle innerRectangle2 = null;
		Flyout innerFlyout1 = null;
		Flyout innerFlyout2 = null;

		var loadedEvent = new Event();
		var loadedEventRegistration = CreateSafeEventRegistration<Grid, RoutedEventHandler>(nameof(Grid.Loaded));
		var mainFlyoutOpenedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var mainFlyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));
		var innerFlyout1OpenedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var innerFlyout1ClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));
		var innerFlyout2OpenedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var innerFlyout2ClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		bool mainFlyoutIsOpen = false;
		bool innerFlyout1IsOpen = false;
		bool innerFlyout2IsOpen = false;

		await RunOnUIThread(() =>
		{
			var root = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
				"    <Rectangle x:Name='Rectangle' Width='100' Height='40' Fill='Gray'>" +
				"        <Rectangle.ContextFlyout>" +
				"            <Flyout x:Name='Flyout'>" +
				"                <StackPanel>" +
				"                    <Rectangle x:Name='InnerRectangle1' Width='100' Height='40' Fill='Red'>" +
				"                        <Rectangle.ContextFlyout>" +
				"                            <Flyout x:Name='InnerFlyout1'>" +
				"                                <Rectangle Width='100' Height='40' Fill='Yellow' />" +
				"                            </Flyout>" +
				"                        </Rectangle.ContextFlyout>" +
				"                    </Rectangle>" +
				"                    <Rectangle x:Name='InnerRectangle2' Width='100' Height='40' Fill='Blue'>" +
				"                        <Rectangle.ContextFlyout>" +
				"                            <Flyout x:Name='InnerFlyout2'>" +
				"                                <Rectangle Width='100' Height='40' Fill='Yellow' />" +
				"                            </Flyout>" +
				"                        </Rectangle.ContextFlyout>" +
				"                    </Rectangle>" +
				"                </StackPanel>" +
				"            </Flyout>" +
				"        </Rectangle.ContextFlyout>" +
				"    </Rectangle>" +
				"</Grid>"));

			rectangle = (Rectangle)(root.FindName("Rectangle"));
			// TODO Uno: FindName can't reach flyouts until the namescope work lands (uno--findname-misses-resource-and-flyout-names-in-xamlreader).
			flyout = (Flyout)rectangle.ContextFlyout;
			var flyoutPanel = (StackPanel)flyout.Content;
			innerRectangle1 = (Rectangle)flyoutPanel.Children[0];
			innerRectangle2 = (Rectangle)flyoutPanel.Children[1];
			innerFlyout1 = (Flyout)innerRectangle1.ContextFlyout;
			innerFlyout2 = (Flyout)innerRectangle2.ContextFlyout;

			mainFlyoutOpenedRegistration.Attach(flyout, (s, e) => { mainFlyoutIsOpen = true; });
			mainFlyoutClosedRegistration.Attach(flyout, (s, e) => { mainFlyoutIsOpen = false; });
			innerFlyout1OpenedRegistration.Attach(innerFlyout1, (s, e) => { innerFlyout1IsOpen = true; });
			innerFlyout1ClosedRegistration.Attach(innerFlyout1, (s, e) => { innerFlyout1IsOpen = false; });
			innerFlyout2OpenedRegistration.Attach(innerFlyout2, (s, e) => { innerFlyout2IsOpen = true; });
			innerFlyout2ClosedRegistration.Attach(innerFlyout2, (s, e) => { innerFlyout2IsOpen = false; });

			loadedEventRegistration.Attach(root, (s, e) => { loadedEvent.Set(); });
			TestServices.WindowHelper.WindowContent = root;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Inject a right-click on the main rectangle to open the main flyout.");
		TestServices.InputHelper.ClickMouseButton(MouseButton.Right, await ControlHelper.GetCenterOfElement(rectangle));
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Inject a right-click on the main rectangle to open the first flyout.");
			VERIFY_IS_TRUE(mainFlyoutIsOpen);
			VERIFY_IS_FALSE(innerFlyout1IsOpen);
			VERIFY_IS_FALSE(innerFlyout2IsOpen);
		});

		LOG_OUTPUT("Inject a right-click on the first inner rectangle to open the second inner flyout.");
		TestServices.InputHelper.ClickMouseButton(MouseButton.Right, await ControlHelper.GetCenterOfElement(innerRectangle1));
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("The main and second inner flyouts should now be open.");
			VERIFY_IS_TRUE(mainFlyoutIsOpen);
			VERIFY_IS_TRUE(innerFlyout1IsOpen);
			VERIFY_IS_FALSE(innerFlyout2IsOpen);
		});

		LOG_OUTPUT("Inject a right-click on the second inner rectangle to open the first inner flyout.");
		TestServices.InputHelper.ClickMouseButton(MouseButton.Right, await ControlHelper.GetCenterOfElement(innerRectangle2));
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("The main and first inner flyouts should now be open.");
			VERIFY_IS_TRUE(mainFlyoutIsOpen);
			VERIFY_IS_FALSE(innerFlyout1IsOpen);
			VERIFY_IS_TRUE(innerFlyout2IsOpen);
		});

		LOG_OUTPUT("Close the main flyout to ensure that everything is cleaned up properly.");
		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Validates that a flyout closes when it loses focus.")]
	public async Task ValidateLosingFocusClosesFlyout()
	{
		TextBox textBox = null;
		Button flyoutButton = null;
		Flyout flyout = null;

		var loadedEvent = new Event();
		var loadedEventRegistration = CreateSafeEventRegistration<StackPanel, RoutedEventHandler>(nameof(StackPanel.Loaded));
		var flyoutClosedEvent = new Event();
		var flyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		await RunOnUIThread(() =>
		{
			var root = (StackPanel)(XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
				"    <TextBox x:Name='TextBox' />" +
				"    <Button x:Name='FlyoutButton'>" +
				"        <Button.Flyout>" +
				"            <Flyout x:Name='Flyout' />" +
				"        </Button.Flyout>" +
				"    </Button>" +
				"</StackPanel>"));

			textBox = (TextBox)(root.FindName("TextBox"));
			flyoutButton = (Button)(root.FindName("FlyoutButton"));
			// TODO Uno: FindName can't reach flyouts until the namescope work lands (uno--findname-misses-resource-and-flyout-names-in-xamlreader).
			flyout = (Flyout)flyoutButton.Flyout;

			flyoutClosedRegistration.Attach(flyout, (s, e) => { flyoutClosedEvent.Set(); });

			loadedEventRegistration.Attach(root, (s, e) => { loadedEvent.Set(); });
			TestServices.WindowHelper.WindowContent = root;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Click on the textBox to set last input device type to pointer.");
		TestServices.InputHelper.LeftMouseClick(textBox);

		LOG_OUTPUT("First, open the flyout.");
		await FlyoutHelper.OpenFlyout(flyout, flyoutButton, FlyoutOpenMethod.Programmatic_ShowAt);

		LOG_OUTPUT("Now give focus to the text box.");
		await ControlHelper.EnsureFocused(textBox);

		LOG_OUTPUT("The flyout should have closed...");
		await flyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("...and the TextBox should still have the same focus as before.");
			VERIFY_IS_TRUE(textBox.Equals(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot)));
			VERIFY_ARE_EQUAL(FocusState.Pointer, textBox.FocusState);
		});
	}

	[TestMethod]
	[Description("Verify that focus moves into a Flyout when opened with UIA when AllowFocusOnInteraction=false on the target.")]
	public async Task VerifyFlyoutTakesFocusWithAllowFocusOnInteractionFalseWithUIA()
	{
		// We want to test the scenario where:
		// We have a button with AllowFocusOnInteraction=false (default for AppBarButton)
		// The Button has a flyout
		// The user uses UIA (e.g. Narrator) to move focus to the button and invoke it to open the flyout.
		// Focus should move into the Flyout.
		// See MSFT:[January PreProd Blocking] [RS4 Inbox/Store][Screen Reader - Photos - More->Set as] "Set as" Menu items are not accessible using Narrator scan mode.

		Button dummyButton = null;
		Button flyoutButton = null;
		Button buttonInFlyout = null;
		Flyout flyout = null;

		Event loadedEvent = new();
		var loadedEventRegistration = CreateSafeEventRegistration<StackPanel, RoutedEventHandler>(nameof(StackPanel.Loaded));

		Event clickEvent = new();
		var passThroughClickEventRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>(nameof(Button.Click));

		Event openedEvent = new();
		var openedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Opened));

		Event buttonInFlyoutGotFocus = new();
		var buttonInFlyoutGotFocusRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>(nameof(Button.GotFocus));

		Event flyoutButtonGotFocus = new();
		var flyoutButtonGotFocusRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>(nameof(Button.GotFocus));

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)(XamlReader.Load(
				"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" >
					<Button x:Name="dummyButton" Content="Dummy Button" />
					<Button x:Name="flyoutButton" Content="Flyout Button" AllowFocusOnInteraction="False" >
						<Button.Flyout>
							<Flyout x:Name="flyout">
								<Button x:Name="buttonInFlyout" Content="Button in Flyout" />
							</Flyout>
						</Button.Flyout>
					</Button>
				</StackPanel>
				"""));

			flyoutButton = (Button)(rootPanel.FindName("flyoutButton"));
			dummyButton = (Button)(rootPanel.FindName("dummyButton"));
			// TODO Uno: FindName can't reach flyouts until the namescope work lands (uno--findname-misses-resource-and-flyout-names-in-xamlreader).
			flyout = (Flyout)flyoutButton.Flyout;
			buttonInFlyout = (Button)flyout.Content;

			loadedEventRegistration.Attach(rootPanel, (s, e) => { loadedEvent.Set(); });
			passThroughClickEventRegistration.Attach(dummyButton, (s, e) => { clickEvent.Set(); });
			openedRegistration.Attach(flyout, (s, e) => { openedEvent.Set(); });
			flyoutButtonGotFocusRegistration.Attach(flyoutButton, (s, e) => { flyoutButtonGotFocus.Set(); });
			buttonInFlyoutGotFocusRegistration.Attach(buttonInFlyout, (s, e) => { buttonInFlyoutGotFocus.Set(); });

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Waiting for Loaded event");
		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Tapping dummy button");
		TestServices.InputHelper.Tap(dummyButton);
		LOG_OUTPUT("Waiting for Click event");
		await clickEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Setting focus to flyoutButton using UIA");
		ButtonAutomationPeer buttonAp = null;
		await RunOnUIThread(() =>
		{
			buttonAp = (ButtonAutomationPeer)(FrameworkElementAutomationPeer.FromElement(flyoutButton));
			buttonAp.SetFocus();
		});
		LOG_OUTPUT("Waiting for Button to get focus");
		await flyoutButtonGotFocus.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Invoking flyoutButton using UIA");
		await RunOnUIThread(() =>
		{
			buttonAp.Invoke();
		});

		LOG_OUTPUT("Waiting for Flyout to open");
		await openedEvent.WaitForDefault();

		LOG_OUTPUT("Waiting for buttonInFlyout to get focus");
		await buttonInFlyoutGotFocus.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var focusedElement = FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
			LOG_OUTPUT("FocusedElement: %s", focusedElement.ToString());
			if (focusedElement is FrameworkElement fe)
			{
				LOG_OUTPUT("FocusedElement Name: %s", fe.Name.ToString());
			}
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(buttonInFlyout));
		});

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Validates that each value of FlyoutPlacementMode has the effect that we expect it to.")]
	[RequiresScaling(1f)]
	public async Task ValidateFlyoutPlacementMode()
	{
		Button button;
		Flyout flyout;

		(button, flyout) = await SetupFlyoutPlacementModeTest();

		// Note:
		// The target is 100x100 and centered in a 300x300 panel.
		// The flyout is 50x50.
		// There is also a 4px padding between the target and the flyout

		// The PlacementMode and the expected bounds:
		List<(FlyoutPlacementMode, Rect)> placementsAndBounds = new()
		{
			(FlyoutPlacementMode.Top,                    new Rect(125, 46, 50, 50)),
			(FlyoutPlacementMode.TopEdgeAlignedLeft,     new Rect(100, 46, 50, 50)),
			(FlyoutPlacementMode.TopEdgeAlignedRight,    new Rect(150, 46, 50, 50)),
			(FlyoutPlacementMode.Bottom,                 new Rect(125, 204, 50, 50)),
			(FlyoutPlacementMode.BottomEdgeAlignedLeft,  new Rect(100, 204, 50, 50)),
			(FlyoutPlacementMode.BottomEdgeAlignedRight, new Rect(150, 204, 50, 50)),
			(FlyoutPlacementMode.Left,                   new Rect(46, 125, 50, 50)),
			(FlyoutPlacementMode.LeftEdgeAlignedTop,     new Rect(46, 100, 50, 50)),
			(FlyoutPlacementMode.LeftEdgeAlignedBottom,  new Rect(46, 150, 50, 50)),
			(FlyoutPlacementMode.Right,                  new Rect(204, 125, 50, 50)),
			(FlyoutPlacementMode.RightEdgeAlignedTop,    new Rect(204, 100, 50, 50)),
			(FlyoutPlacementMode.RightEdgeAlignedBottom, new Rect(204, 150, 50, 50))
		};

		foreach (var placementAndBounds in placementsAndBounds)
		{
			var placement = placementAndBounds.Item1;
			var expectedBounds = placementAndBounds.Item2;

			LOG_OUTPUT("Testing with Placement=%s", placement.ToString());
			await RunOnUIThread(() =>
			{
				flyout.Placement = placement;
			});

			await FlyoutHelper.OpenFlyout(flyout, button, FlyoutOpenMethod.Programmatic_ShowAt);
			await TestServices.WindowHelper.WaitForIdle();

			var presenter = FlyoutHelper.GetFlyoutPresenter(flyout);
			var bounds = await ControlHelper.GetBounds(presenter);

			LOG_OUTPUT("Expected Bounds: %f,%f,%f,%f", expectedBounds.X, expectedBounds.Y, expectedBounds.Width, expectedBounds.Height);
			LOG_OUTPUT("Actual Bounds: %f,%f,%f,%f", bounds.X, bounds.Y, bounds.Width, bounds.Height);
			VERIFY_IS_TRUE(bounds.Equals(expectedBounds));

			await FlyoutHelper.HideFlyout(flyout);
		}
	}

	[TestMethod]
	[Description("Validates that each value of FlyoutPlacementMode when opened at a set point has the effect that we expect it to.")]
	[RequiresScaling(1f)]
	public async Task ValidateFlyoutPlacementModeAtPoint()
	{
		Button button;
		Flyout flyout;

		(button, flyout) = await SetupFlyoutPlacementModeTest();

		// Note:
		// The target is 100x100 and centered in a 300x300 panel.
		// The flyout is 50x50.
		// The flyout is shown at (50,25) within the target.

		// The PlacementMode and the expected bounds:
		List<(FlyoutPlacementMode, Rect)> placementsAndBounds = new()
		{
			(FlyoutPlacementMode.Top,                    new Rect(125, 75, 50, 50)),
			(FlyoutPlacementMode.TopEdgeAlignedLeft,     new Rect(150, 75, 50, 50)),
			(FlyoutPlacementMode.TopEdgeAlignedRight,    new Rect(100, 75, 50, 50)),
			(FlyoutPlacementMode.Bottom,                 new Rect(125, 125, 50, 50)),
			(FlyoutPlacementMode.BottomEdgeAlignedLeft,  new Rect(150, 125, 50, 50)),
			(FlyoutPlacementMode.BottomEdgeAlignedRight, new Rect(100, 125, 50, 50)),
			(FlyoutPlacementMode.Left,                   new Rect(100, 100, 50, 50)),
			(FlyoutPlacementMode.LeftEdgeAlignedTop,     new Rect(100, 125, 50, 50)),
			(FlyoutPlacementMode.LeftEdgeAlignedBottom,  new Rect(100, 75, 50, 50)),
			(FlyoutPlacementMode.Right,                  new Rect(150, 100, 50, 50)),
			(FlyoutPlacementMode.RightEdgeAlignedTop,    new Rect(150, 125, 50, 50)),
			(FlyoutPlacementMode.RightEdgeAlignedBottom, new Rect(150, 75, 50, 50))
		};

		FlyoutShowOptions showOptions = null;

		await RunOnUIThread(() =>
		{
			showOptions = new FlyoutShowOptions();
			showOptions.Position = new Point(50, 25);
		});

		foreach (var placementAndBounds in placementsAndBounds)
		{
			var placement = placementAndBounds.Item1;
			var expectedBounds = placementAndBounds.Item2;

			LOG_OUTPUT("Testing with Placement=%s", placement.ToString());
			await RunOnUIThread(() =>
			{
				flyout.Placement = placement;
			});

			await FlyoutHelper.ShowFlyoutWithOptions(flyout, button, showOptions);

			var presenter = FlyoutHelper.GetFlyoutPresenter(flyout);
			var bounds = await ControlHelper.GetBounds(presenter);

			LOG_OUTPUT("Expected Bounds: %f,%f,%f,%f", expectedBounds.X, expectedBounds.Y, expectedBounds.Width, expectedBounds.Height);
			LOG_OUTPUT("Actual Bounds: %f,%f,%f,%f", bounds.X, bounds.Y, bounds.Width, bounds.Height);
			VERIFY_IS_TRUE(bounds.Equals(expectedBounds));

			await FlyoutHelper.HideFlyout(flyout);
		}
	}

	[TestMethod]
	[Description("Validates that each value of FlyoutPlacementMode when opened at a set point with an exclusion rect has the effect that we expect it to.")]
	[RequiresScaling(1f)]
	public async Task ValidateFlyoutPlacementModeAtPointWithExclusionRect()
	{
		Button button;
		Flyout flyout;

		(button, flyout) = await SetupFlyoutPlacementModeTest();

		// Note:
		// The target is 100x100 and centered in a 300x300 panel.
		// The panel has a 120x120 exclusion rect centered at (-10,-10)
		// The flyout is 50x50.
		// The flyout is shown at (50,25) within the target.

		// The PlacementMode and the expected bounds:
		List<(FlyoutPlacementMode, Rect)> placementsAndBounds = new()
		{
			(FlyoutPlacementMode.Top,                    new Rect(125, 40, 50, 50)),
			(FlyoutPlacementMode.TopEdgeAlignedLeft,     new Rect(150, 40, 50, 50)),
			(FlyoutPlacementMode.TopEdgeAlignedRight,    new Rect(100, 40, 50, 50)),
			(FlyoutPlacementMode.Bottom,                 new Rect(125, 210, 50, 50)),
			(FlyoutPlacementMode.BottomEdgeAlignedLeft,  new Rect(150, 210, 50, 50)),
			(FlyoutPlacementMode.BottomEdgeAlignedRight, new Rect(100, 210, 50, 50)),
			(FlyoutPlacementMode.Left,                   new Rect(40, 100, 50, 50)),
			(FlyoutPlacementMode.LeftEdgeAlignedTop,     new Rect(40, 125, 50, 50)),
			(FlyoutPlacementMode.LeftEdgeAlignedBottom,  new Rect(40, 75, 50, 50)),
			(FlyoutPlacementMode.Right,                  new Rect(210, 100, 50, 50)),
			(FlyoutPlacementMode.RightEdgeAlignedTop,    new Rect(210, 125, 50, 50)),
			(FlyoutPlacementMode.RightEdgeAlignedBottom, new Rect(210, 75, 50, 50))
		};

		FlyoutShowOptions showOptions = null;

		await RunOnUIThread(() =>
		{
			showOptions = new FlyoutShowOptions();
			showOptions.Position = new Point(50, 25);
			showOptions.ExclusionRect = new Rect(-10, -10, 120, 120);
		});

		foreach (var placementAndBounds in placementsAndBounds)
		{
			var placement = placementAndBounds.Item1;
			var expectedBounds = placementAndBounds.Item2;

			LOG_OUTPUT("Testing with Placement=%s", placement.ToString());
			await RunOnUIThread(() =>
			{
				flyout.Placement = placement;
			});

			await FlyoutHelper.ShowFlyoutWithOptions(flyout, button, showOptions);

			var presenter = FlyoutHelper.GetFlyoutPresenter(flyout);
			var bounds = await ControlHelper.GetBounds(presenter);

			LOG_OUTPUT("Expected Bounds: %f,%f,%f,%f", expectedBounds.X, expectedBounds.Y, expectedBounds.Width, expectedBounds.Height);
			LOG_OUTPUT("Actual Bounds: %f,%f,%f,%f", bounds.X, bounds.Y, bounds.Width, bounds.Height);
			VERIFY_IS_TRUE(bounds.Equals(expectedBounds));

			await FlyoutHelper.HideFlyout(flyout);
		}
	}

	[TestMethod]
	[Description("Validates that FlyoutShowMode can be set from XAML.")]
	public async Task ValidateFlyoutShowMode()
	{
		Flyout flyout = null;
		Button targetButton = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)(XamlReader.Load(
				"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<StackPanel.Resources>
						<Flyout x:Name="flyout" ShowMode="Transient">
							<StackPanel>
								<TextBox />
							</StackPanel>
						</Flyout>
					</StackPanel.Resources>
					<Button Content="Flyout target" x:Name="targetButton" />
				</StackPanel>
				"""));

			TestServices.WindowHelper.WindowContent = rootPanel;

			// TODO Uno: FindName can't reach flyouts until the namescope work lands (uno--findname-misses-resource-and-flyout-names-in-xamlreader).
			flyout = rootPanel.Resources.Values.OfType<Flyout>().First();
			targetButton = (Button)(rootPanel.FindName("targetButton"));
		});

		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(flyout, targetButton, FlyoutOpenMethod.Programmatic_ShowAt);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(FlyoutShowMode.Transient, flyout.ShowMode);
		});

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Validates that a flyout opened as transient does not take keyboard focus when it opens.")]
	public async Task ValidateTransientFlyoutDoesNotTakeFocus()
	{
		Flyout flyout = null;
		Button focusButton = null;
		Button targetButton = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)(XamlReader.Load(
				"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<StackPanel.Resources>
						<Flyout x:Name="flyout">
							<StackPanel>
								<TextBox />
							</StackPanel>
						</Flyout>
					</StackPanel.Resources>
					<Button Content="Button for focus" x:Name="focusButton" />
					<Button Content="Flyout target" x:Name="targetButton" />
				</StackPanel>
				"""));

			TestServices.WindowHelper.WindowContent = rootPanel;

			// TODO Uno: FindName can't reach flyouts until the namescope work lands (uno--findname-misses-resource-and-flyout-names-in-xamlreader).
			flyout = rootPanel.Resources.Values.OfType<Flyout>().First();
			focusButton = (Button)(rootPanel.FindName("focusButton"));
			targetButton = (Button)(rootPanel.FindName("targetButton"));
		});

		await TestServices.WindowHelper.WaitForIdle();

		await ControlHelper.EnsureFocused(focusButton);

		FlyoutShowOptions showOptions = null;
		await RunOnUIThread(() =>
		{
			showOptions = new FlyoutShowOptions();
			showOptions.ShowMode = FlyoutShowMode.Transient;
		});

		await FlyoutHelper.ShowFlyoutWithOptions(flyout, targetButton, showOptions);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_NOT_EQUAL(FocusState.Unfocused, focusButton.FocusState);
		});

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Validates that a flyout opened as transient allows input to pass through its light-dismiss layer.")]
	public async Task ValidateTransientFlyoutLightDismissLayerAllowsInputToPassThrough()
	{
		Flyout flyout = null;
		Button clickHandlerButton = null;
		Button targetButton = null;

		var clickEvent = new Event();
		var flyoutClosedEvent = new Event();
		var clickRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>(nameof(Button.Click));
		var flyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)(XamlReader.Load(
				"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<StackPanel.Resources>
						<Flyout x:Name="flyout" Placement="Bottom">
							<StackPanel>
								<TextBox />
							</StackPanel>
						</Flyout>
					</StackPanel.Resources>
					<Button Content="Button with click handler" x:Name="clickHandlerButton" />
					<Button Content="Flyout target" x:Name="targetButton" />
				</StackPanel>
				"""));

			TestServices.WindowHelper.WindowContent = rootPanel;

			// TODO Uno: FindName can't reach flyouts until the namescope work lands (uno--findname-misses-resource-and-flyout-names-in-xamlreader).
			flyout = rootPanel.Resources.Values.OfType<Flyout>().First();
			clickHandlerButton = (Button)(rootPanel.FindName("clickHandlerButton"));
			targetButton = (Button)(rootPanel.FindName("targetButton"));

			flyoutClosedRegistration.Attach(flyout, (s, e) => { flyoutClosedEvent.Set(); });
			clickRegistration.Attach(clickHandlerButton, (s, e) => { clickEvent.Set(); });
		});

		await TestServices.WindowHelper.WaitForIdle();

		FlyoutShowOptions showOptions = null;
		await RunOnUIThread(() =>
		{
			showOptions = new FlyoutShowOptions();
			showOptions.ShowMode = FlyoutShowMode.Transient;
		});

		await FlyoutHelper.ShowFlyoutWithOptions(flyout, targetButton, showOptions);
		await TestServices.WindowHelper.WaitForIdle();

		TestServices.InputHelper.LeftMouseClick(clickHandlerButton);

		await clickEvent.WaitForDefault();
		await flyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Description("Validates that FlyoutShowMode=TransientWithDismissOnPointerMoveAway dismisses properly.")]
	public async Task ValidateFlyoutTransientWithDismissOnPointerMoveAway()
	{
		Flyout flyout = null;
		Button targetButton = null;

		var flyoutOpenedEvent = new Event();
		var flyoutClosedEvent = new Event();
		var flyoutOpenedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var flyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)(XamlReader.Load(
				"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Background="Green">
					<Button Content="Flyout target" x:Name="targetButton">
						<Button.Flyout>
							<Flyout x:Name="flyout" ShowMode="TransientWithDismissOnPointerMoveAway">
								<StackPanel>
									<Button Content="Button in Flyout" />
								</StackPanel>
							</Flyout>
						</Button.Flyout>
					</Button>
				</StackPanel>
				"""));

			TestServices.WindowHelper.WindowContent = rootPanel;

			targetButton = (Button)(rootPanel.FindName("targetButton"));
			// TODO Uno: FindName can't reach flyouts until the namescope work lands (uno--findname-misses-resource-and-flyout-names-in-xamlreader).
			flyout = (Flyout)targetButton.Flyout;

			flyoutOpenedRegistration.Attach(flyout, (s, e) =>
			{
				LOG_OUTPUT("Opened raised.");
				flyoutOpenedEvent.Set();
			});

			flyoutClosedRegistration.Attach(flyout, (s, e) =>
			{
				LOG_OUTPUT("Closed raised.");
				flyoutClosedEvent.Set();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Opening the flyout...");
		TestServices.InputHelper.LeftMouseClick(targetButton);
		await TestServices.WindowHelper.WaitForIdle();
		await flyoutOpenedEvent.WaitForDefault();
		flyoutOpenedEvent.Reset();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(FlyoutShowMode.TransientWithDismissOnPointerMoveAway, flyout.ShowMode);
			VERIFY_ARE_EQUAL(flyout.IsOpen, true);
		});

		LOG_OUTPUT("Moving the mouse away, which should close it.");
		TestServices.InputHelper.MoveMouse(new Point(300, 500));
		await TestServices.WindowHelper.WaitForIdle();
		LOG_OUTPUT("Waiting for close...");
		await flyoutClosedEvent.WaitForDefault();
		flyoutClosedEvent.Reset();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(flyout.IsOpen, false);
		});

		// Repeat!
		LOG_OUTPUT("Repeating : Opening the flyout again...");
		TestServices.InputHelper.LeftMouseClick(targetButton);
		await TestServices.WindowHelper.WaitForIdle();
		await flyoutOpenedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(FlyoutShowMode.TransientWithDismissOnPointerMoveAway, flyout.ShowMode);
			VERIFY_ARE_EQUAL(flyout.IsOpen, true);
		});

		LOG_OUTPUT("Moving the mouse away, which should close it.");
		TestServices.InputHelper.MoveMouse(new Point(300, 500));
		await TestServices.WindowHelper.WaitForIdle();
		LOG_OUTPUT("Waiting for close...");
		await flyoutClosedEvent.WaitForDefault();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(flyout.IsOpen, false);
		});
	}

	[TestMethod]
	[Description("Validates that opening a ContextFlyout with the right mouse button causes it to be placed at the mouse cursor, not just on the UI element it's attached to.")]
	public async Task ValidateContextFlyoutPlacedAtMouse()
	{
		Rectangle rectangle = null;
		Flyout flyout = null;

		var flyoutOpenedEvent = new Event();
		var flyoutClosedEvent = new Event();
		var flyoutOpenedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var flyoutClosedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));
		var pointerPressedRegistration = CreateSafeEventRegistration<Rectangle, PointerEventHandler>(nameof(Rectangle.PointerPressed));
		var pointerReleasedRegistration = CreateSafeEventRegistration<Rectangle, PointerEventHandler>(nameof(Rectangle.PointerReleased));

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)(XamlReader.Load(
				"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<Rectangle Width="100" Height="100" Fill="Red" x:Name="rectangle">
						<Rectangle.ContextFlyout>
							<Flyout x:Name="flyout" Placement="Bottom">
								<StackPanel>
									<TextBox />
								</StackPanel>
							</Flyout>
						</Rectangle.ContextFlyout>
					</Rectangle>
				</StackPanel>
				"""));

			TestServices.WindowHelper.WindowContent = rootPanel;

			rectangle = (Rectangle)(rootPanel.FindName("rectangle"));
			// TODO Uno: FindName can't reach flyouts until the namescope work lands (uno--findname-misses-resource-and-flyout-names-in-xamlreader).
			flyout = (Flyout)rectangle.ContextFlyout;

			flyoutOpenedRegistration.Attach(flyout, (s, e) =>
			{
				LOG_OUTPUT("Opened raised.");
				flyoutOpenedEvent.Set();
			});

			flyoutClosedRegistration.Attach(flyout, (s, e) =>
			{
				LOG_OUTPUT("Closed raised.");
				flyoutClosedEvent.Set();
			});

			pointerPressedRegistration.Attach(rectangle, (s, e) =>
			{
				LOG_OUTPUT("PointerPressed raised.");
			});

			pointerReleasedRegistration.Attach(rectangle, (s, e) =>
			{
				LOG_OUTPUT("PointerReleased raised.");
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		Point point1 = new Point(0, 0);
		Point point2 = new Point(25, 25);

		var rectangleCenter = await ControlHelper.GetCenterOfElement(rectangle);

		LOG_OUTPUT("Right click over Rectangle at position %f, %f from center.", point1.X, point1.Y);
		TestServices.InputHelper.ClickMouseButton(MouseButton.Right, new Point(rectangleCenter.X + (int)point1.X, rectangleCenter.Y + (int)point1.Y));
		LOG_OUTPUT("Waiting for Flyout.Opened event.");
		await flyoutOpenedEvent.WaitForDefault();
		flyoutOpenedEvent.Reset();
		await TestServices.WindowHelper.WaitForIdle();

		FlyoutPresenter presenter = null;
		Rect bounds1 = default;
		Rect bounds2 = default;

		presenter = FlyoutHelper.GetFlyoutPresenter(flyout);
		bounds1 = await ControlHelper.GetBounds(presenter);

		LOG_OUTPUT("Hiding flyout.");
		await FlyoutHelper.HideFlyout(flyout);
		LOG_OUTPUT("Waiting for Flyout.Closed event.");
		await flyoutClosedEvent.WaitForDefault();
		flyoutClosedEvent.Reset();
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Right click over Rectangle at position %f, %f from center.", point2.X, point2.Y);
		TestServices.InputHelper.ClickMouseButton(MouseButton.Right, new Point(rectangleCenter.X + (int)point2.X, rectangleCenter.Y + (int)point2.Y));

		LOG_OUTPUT("Waiting for Flyout.Opened event.");
		await flyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		bounds2 = await ControlHelper.GetBounds(presenter);

		LOG_OUTPUT("Hiding flyout.");
		await FlyoutHelper.HideFlyout(flyout);
		LOG_OUTPUT("Waiting for Flyout.Closed event.");
		await flyoutClosedEvent.WaitForDefault();

		LOG_OUTPUT("Verifying relative flyout positions X: %f, %f", bounds1.X + point2.X, bounds2.X + point1.X);
		LOG_OUTPUT("Verifying relative flyout positions Y: %f, %f", bounds1.Y + point2.Y, bounds2.Y + point1.Y);
		VERIFY_IS_LESS_THAN_OR_EQUAL(bounds1.X + point2.X, bounds2.X + point1.X + 1);
		VERIFY_IS_LESS_THAN_OR_EQUAL(bounds1.Y + point2.Y, bounds2.Y + point1.Y + 1);
	}

	[TestMethod]
	[Description("Validates that InputDevicePrefersPrimaryCommands correctly reports whether or not the flyout was opened with an input device that wants more primary commands.")]
	public async Task VerifyInputDevicePrefersPrimaryCommands()
	{
		Flyout flyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Bottom);
		Button target = null;

		await RunOnUIThread(() =>
		{
			target = new Button();
			target.Content = "Click for flyout";
			target.Flyout = flyout;
			target.ContextFlyout = flyout;

			var rootPanel = new Grid();
			rootPanel.Children.Add(target);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		async Task verifyOpenInputDevice(FlyoutOpenMethod openMethod, bool expectedInputDevicePrefersPrimaryCommands)
		{
			var openingRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opening));

			await RunOnUIThread(() =>
			{
				openingRegistration.Attach(
					flyout,
					(s, e) =>
					{
						LOG_OUTPUT("Inside FlyoutBase.Opening. Expecting OpenInputDevice to be %s.", expectedInputDevicePrefersPrimaryCommands ? "true" : "false");
						VERIFY_ARE_EQUAL(expectedInputDevicePrefersPrimaryCommands, flyout.InputDevicePrefersPrimaryCommands);
					});
			});

			LOG_OUTPUT("Opening flyout using %s.", openMethod.ToString());
			if (openMethod == FlyoutOpenMethod.Pen)
			{
				// FlyoutHelper.OpenFlyout does not implement Pen in Uno.
				await FlyoutHelper.OpenFlyoutWithPenHold(flyout, target);
			}
			else
			{
				await FlyoutHelper.OpenFlyout(flyout, target, openMethod);
			}

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Flyout opened. Expecting InputDevicePrefersPrimaryCommands to be %s.", expectedInputDevicePrefersPrimaryCommands ? "true" : "false");
				VERIFY_ARE_EQUAL(expectedInputDevicePrefersPrimaryCommands, flyout.InputDevicePrefersPrimaryCommands);
			});

			LOG_OUTPUT("Closing flyout.");
			await FlyoutHelper.HideFlyout(flyout);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Flyout closed. Expecting InputDevicePrefersPrimaryCommands to be false.");
				VERIFY_IS_FALSE(flyout.InputDevicePrefersPrimaryCommands);

				openingRegistration.Detach();
			});
		}

		await verifyOpenInputDevice(FlyoutOpenMethod.Mouse, false);
		await verifyOpenInputDevice(FlyoutOpenMethod.Touch, true);
		await verifyOpenInputDevice(FlyoutOpenMethod.Pen, true);
		await verifyOpenInputDevice(FlyoutOpenMethod.Keyboard, false);
		await verifyOpenInputDevice(FlyoutOpenMethod.Gamepad, false);
	}

	[TestMethod]
	[Description("Validates that a hyperlink in a flyout taking focus does not close that flyout.")]
	public async Task ValidateHyperlinkTakingFocusDoesNotCloseFlyout()
	{
		var target = await FlyoutHelper.CreateTarget(
			300 /*width*/, 300 /*height*/,
			new Thickness(100),
			HorizontalAlignment.Center,
			VerticalAlignment.Center);

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			VERIFY_IS_NOT_NULL(rootPanel);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		Flyout flyout = null;
		Hyperlink hyperlinkFocusTarget = null;

		await RunOnUIThread(() =>
		{
			var richTextBlock = (RichTextBlock)(XamlReader.Load(
				"""
				<RichTextBlock xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<Paragraph>
						<Hyperlink x:Name="HyperlinkFocusTarget" NavigateUri="http://www.bing.com">Link text</Hyperlink>
					</Paragraph>
				</RichTextBlock>
				"""));

			// TODO Uno: FindName doesn't reach the inlines of a RichTextBlock that hasn't entered the tree.
			hyperlinkFocusTarget = (Hyperlink)((Paragraph)richTextBlock.Blocks[0]).Inlines[0];

			flyout = new Flyout();
			flyout.Content = richTextBlock;
		});

		LOG_OUTPUT("Open the flyout. The hyperlink should take focus and the flyout should stay open.");
		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Ensuring the flyout is open...");
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(target.XamlRoot);
			VERIFY_ARE_EQUAL(popups.Count, 1);

			LOG_OUTPUT("Ensuring the hyperlink is the focused element...");
			VERIFY_IS_TRUE(hyperlinkFocusTarget.Equals(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot)));
		});

		await FlyoutHelper.HideFlyout(flyout);
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Description("Validates the flyout will respond to a theme change when open.")]
	public async Task ValidateThemeChangeWhileOpen()
	{
		// Verify that if a theme change comes in while the flyout is showing, it will be updated.

		// WUCRenderingScopeGuard wuc(DCompRendering::WUCCompleteSynchronousCompTree);

		Button button = null;
		Flyout flyout = null;
		Grid rootPanel = null;
		var flyoutOpenedEvent = new Event();
		var flyoutClosedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		await RunOnUIThread(() =>
		{
			rootPanel = (Grid)(XamlReader.Load(
				"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"      x:Name='root' Background='SlateBlue' Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left'> " +
				"  <Button x:Name='button' Content='button.flyout' HorizontalAlignment='Center' VerticalAlignment='Center' FontSize='25' > " +
				"    <Button.Flyout> " +
				"      <Flyout Placement='TopEdgeAlignedLeft'> " +
				"        <TextBlock FontSize='30' Text='BUTTON.FLYOUT' Margin='30' Foreground='DarkRed' x:Name='button_flyout_content'/> " +
				"      </Flyout> " +
				"    </Button.Flyout> " +
				"  </Button> " +
				"</Grid>"));

			VERIFY_IS_NOT_NULL(rootPanel);
			TestServices.WindowHelper.WindowContent = rootPanel;

			button = (Button)(rootPanel.FindName("button"));
			VERIFY_IS_NOT_NULL(button);

			flyout = (Flyout)(button.Flyout);
			VERIFY_IS_NOT_NULL(flyout);

			openedRegistration.Attach(flyout, (s, e) =>
			{
				LOG_OUTPUT("PopupOpenClose: Flyout Opened event is fired!");
				flyoutOpenedEvent.Set();
			});

			closedRegistration.Attach(flyout, (s, e) =>
			{
				LOG_OUTPUT("PopupOpenClose: Flyout Closed event is fired!");
				flyoutClosedEvent.Set();
			});

			// Ensure we start in the light theme.
			LOG_OUTPUT("Ensure Light Theme");
			rootPanel.RequestedTheme = ElementTheme.Light;
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Button Tap operation to show the Flyout.");
		TestServices.InputHelper.Tap(button);
		await flyoutOpenedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		// Change the theme
		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Change To Dark Theme");
			rootPanel.RequestedTheme = ElementTheme.Dark;
		});
		await TestServices.WindowHelper.WaitForIdle();
		TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);

		// Close the flyout
		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Close the Flyout");
			flyout.Hide();
		});
		await flyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Description("Validates that calling Hide() when opening a flyout cancels the opening.")]
	public async Task ValidateHideInOpeningCancelsOpening()
	{
		var target = await FlyoutHelper.CreateTarget(
			300 /*width*/, 300 /*height*/,
			new Thickness(100),
			HorizontalAlignment.Center,
			VerticalAlignment.Center);

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		Flyout flyout = null;
		var flyoutOpeningEvent = new Event();
		var openingRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opening));

		await RunOnUIThread(() =>
		{
			flyout = new Flyout();

			openingRegistration.Attach(
				flyout,
				(s, e) =>
				{
					flyout.Hide();
					flyoutOpeningEvent.Set();
				});

			LOG_OUTPUT("Open the flyout. It should not actually open because we call Hide() in the Opening event.");
			flyout.ShowAt(target);
		});

		await flyoutOpeningEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Ensuring the flyout is not open...");
			VERIFY_ARE_EQUAL(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(target.XamlRoot).Count);
		});
	}

	[TestMethod]
	[Description("Verify that ShowAt can be called multiple times in a row with different targets.")]
	public async Task ValidateImmediateRetargeting()
	{
		Flyout flyout = null;
		Button button1 = null;
		Button button2 = null;

		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<StackPanel, RoutedEventHandler>(nameof(StackPanel.Loaded));

		var flyoutOpenedEvent = new Event();
		var flyoutClosedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		var flyoutOpenedEventCount = 0;
		var flyoutClosedEventCount = 0;

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Loading XAML.");
			var rootPanel = (StackPanel)(XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
				"  x:Name='root' Background='SlateBlue' Height='200' VerticalAlignment='Top' HorizontalAlignment='Left' Orientation='Horizontal'>" +
				"  <Button x:Name='button1' Content='button1' Margin='10' FontSize='25' VerticalAlignment='Bottom'/>" +
				"  <Button x:Name='button2' Content='button2' Margin='10' FontSize='25' VerticalAlignment='Bottom'/>" +
				"</StackPanel>"));

			VERIFY_IS_NOT_NULL(rootPanel);
			loadedRegistration.Attach(rootPanel, (s, e) =>
			{
				LOG_OUTPUT("StackPanel.Loaded event raised.");
				loadedEvent.Set();
			});

			LOG_OUTPUT("Setting WindowContent.");
			TestServices.WindowHelper.WindowContent = rootPanel;

			button1 = (Button)(rootPanel.FindName("button1"));
			button2 = (Button)(rootPanel.FindName("button2"));

			VERIFY_IS_NOT_NULL(button1);
			VERIFY_IS_NOT_NULL(button2);

			LOG_OUTPUT("Setting up Flyout.Content.");
			var flyoutContent = new TextBlock();
			flyoutContent.Text = "FlyoutContent";
			flyoutContent.FontSize = 20;

			LOG_OUTPUT("Setting up Flyout.");
			flyout = new Flyout();
			flyout.Content = flyoutContent;

			openedRegistration.Attach(
				flyout,
				(s, e) =>
				{
					LOG_OUTPUT("Flyout.Opened event raised.");
					flyoutOpenedEventCount++;
					flyoutOpenedEvent.Set();
				});

			closedRegistration.Attach(
				flyout,
				(s, e) =>
				{
					LOG_OUTPUT("Flyout.Closed event raised.");
					flyoutClosedEventCount++;
					flyoutClosedEvent.Set();
				});
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Opening flyout targeting button1.");
			flyout.ShowAt(button1);
			LOG_OUTPUT("Opening flyout targeting button2.");
			flyout.ShowAt(button2);
		});

		LOG_OUTPUT("Waiting for Opened event...");
		await flyoutOpenedEvent.WaitForDefault();
		LOG_OUTPUT("Waiting for Closed event...");
		await flyoutClosedEvent.WaitForDefault();
		flyoutClosedEvent.Reset();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Verifying event counts.");
			VERIFY_ARE_EQUAL(1, flyoutOpenedEventCount);
			VERIFY_ARE_EQUAL(1, flyoutClosedEventCount);
		});

		LOG_OUTPUT("Closing Flyout.");
		await FlyoutHelper.HideFlyout(flyout);

		LOG_OUTPUT("Waiting for Closed event...");
		await flyoutClosedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Verifying event counts.");
			VERIFY_ARE_EQUAL(1, flyoutOpenedEventCount);
			VERIFY_ARE_EQUAL(2, flyoutClosedEventCount);
		});
	}

	[TestMethod]
	[Description("Verify that a ContextFlyout on a TextBlock can be shown.")]
	public async Task VerifyContextFlyoutOnTextBlock()
	{
		TextBlock textBlockWithContextFlyout = null;
		TextBlock textBlockWithoutContextFlyout = null;
		TextBlock textBlockWithTextSelection = null;
		Flyout flyout1 = null;
		FlyoutBase defaultTextBlockContextFlyout = null;

		var loadedEvent = new Event();
		var loadedEventRegistration = CreateSafeEventRegistration<StackPanel, RoutedEventHandler>(nameof(StackPanel.Loaded));

		Event flyout1OpenedEvent = new();
		var flyout1OpenedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Opened));

		Event defaultFlyoutOpenedEvent = new();
		var defaultFlyoutOpenedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Opened));

		await RunOnUIThread(() =>
		{
			var root = (StackPanel)(XamlReader.Load(
				"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
				"    <TextBlock x:Name='textBlockWithContextFlyout' Text='TextBlock' >" +
				"        <TextBlock.ContextFlyout>" +
				"            <Flyout x:Name='flyout1' />" +
				"        </TextBlock.ContextFlyout>" +
				"    </TextBlock>" +
				"    <TextBlock x:Name='textBlockWithoutContextFlyout' Text='TextBlock' />" +
				"    <TextBlock x:Name='textBlockWithTextSelection' Text='TextBlock' IsTextSelectionEnabled='True' />" +
				"</StackPanel>"));

			textBlockWithContextFlyout = (TextBlock)(root.FindName("textBlockWithContextFlyout"));
			textBlockWithoutContextFlyout = (TextBlock)(root.FindName("textBlockWithoutContextFlyout"));
			textBlockWithTextSelection = (TextBlock)(root.FindName("textBlockWithTextSelection"));

			// TODO Uno: FindName can't reach flyouts until the namescope work lands (uno--findname-misses-resource-and-flyout-names-in-xamlreader).
			flyout1 = (Flyout)textBlockWithContextFlyout.ContextFlyout;
			defaultTextBlockContextFlyout = textBlockWithoutContextFlyout.ContextFlyout;

			VERIFY_ARE_EQUAL(defaultTextBlockContextFlyout, textBlockWithTextSelection.ContextFlyout, "TextBlocks should share same default ContextFlyout");

			flyout1OpenedRegistration.Attach(flyout1, (s, e) => { flyout1OpenedEvent.Set(); });
			defaultFlyoutOpenedRegistration.Attach(defaultTextBlockContextFlyout, (s, e) => { defaultFlyoutOpenedEvent.Set(); });

			loadedEventRegistration.Attach(root, (s, e) => { loadedEvent.Set(); });
			TestServices.WindowHelper.WindowContent = root;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();


		// Right clicking on a TextBlock with an explict ContextFlyout should show the flyout:
		LOG_OUTPUT("Right-click the TextBlock with a ContextFlyout");
		TestServices.InputHelper.ClickMouseButton(MouseButton.Right, await ControlHelper.GetCenterOfElement(textBlockWithContextFlyout));
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Waiting for Flyout to open");
		await flyout1OpenedEvent.WaitForDefault();

		LOG_OUTPUT("Close the flyout.");
		await FlyoutHelper.HideFlyout(flyout1);


		// Right-clicking on a TextBlock without an explict ContextFlyout should not show a flyout:
		LOG_OUTPUT("Right-click the TextBlock with no explict ContextFlyout");
		TestServices.InputHelper.ClickMouseButton(MouseButton.Right, await ControlHelper.GetCenterOfElement(textBlockWithoutContextFlyout));
		await TestServices.WindowHelper.WaitForIdle();

		VERIFY_IS_FALSE(defaultFlyoutOpenedEvent.HasFired(), "Default flyout should not open");


		// A TextBlock with IsTextSelectionEnabled=True should show the default flyout:
		LOG_OUTPUT("Right-click the TextBlock with Text Selection enabled");
		TestServices.InputHelper.ClickMouseButton(MouseButton.Right, await ControlHelper.GetCenterOfElement(textBlockWithTextSelection));
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Waiting for Flyout to open");
		await defaultFlyoutOpenedEvent.WaitForDefault();

		LOG_OUTPUT("Close the flyout.");
		await FlyoutHelper.HideFlyout(defaultTextBlockContextFlyout);
	}

	[TestMethod]
	[Description("Verify that a flyout opened near the left side of the screen with FlyoutPlacementMode = LeftEdgeAlignedTop is nudged back into view.")]
	public async Task VerifyFlyoutOpenedOffTheLeftSideOfTheScreenIsKeptInView()
	{
		Flyout flyout = null;

		var openedEvent = new Event();
		var closedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Opened));
		var closedRegistration = CreateSafeEventRegistration<Flyout, EventHandler<object>>(nameof(Flyout.Closed));

		await RunOnUIThread(() =>
		{
			flyout = new Flyout();
			flyout.Placement = FlyoutPlacementMode.LeftEdgeAlignedTop;

			var rect = new Rectangle();
			rect.Fill = new SolidColorBrush(Microsoft.UI.Colors.Orange);
			rect.Width = 100;
			rect.Height = 100;
			flyout.Content = rect;

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

			var rootPanel = new Grid();
			TestServices.WindowHelper.WindowContent = rootPanel;

			flyout.XamlRoot = rootPanel.XamlRoot;
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Show the flyout.");

		await RunOnUIThread(() =>
		{
			var showOptions = new FlyoutShowOptions();
			showOptions.Position = new Point(20.0f, 20.0f);
			flyout.ShowAt(null, showOptions);
		});

		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		FrameworkElement presenter = null;
		await RunOnUIThread(() =>
		{
			IReadOnlyList<Popup> popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(
				TestServices.WindowHelper.WindowContent.XamlRoot);

			LOG_OUTPUT("There should be one popup for the flyout.");
			VERIFY_ARE_EQUAL(1, popups.Count);

			var flyoutPopup = popups[0];
			presenter = (FrameworkElement)(flyoutPopup.Child);
		});

		var flyoutBounds = await ControlHelper.GetBounds(presenter);
		LOG_OUTPUT("Flyout bounds: left=%.0f top=%.0f width=%.0f height=%.0f", flyoutBounds.Left, flyoutBounds.Top, flyoutBounds.Width, flyoutBounds.Height);
		LOG_OUTPUT("The bounds' left position should be zero.");
		VERIFY_IS_TRUE(Math.Abs(flyoutBounds.Left) < 0.001f);

		LOG_OUTPUT("Close the flyout.");
		await FlyoutHelper.HideFlyout(flyout);

		await closedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	private async Task<(Button button, Flyout flyout)> SetupFlyoutPlacementModeTest()
	{
		Button button = null;
		Flyout flyout = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"""
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
							Background="SlateBlue" Width="300" Height="300" VerticalAlignment="Top" HorizontalAlignment="Left">
						<Grid.Resources>
							<Flyout x:Name="flyout">
								<Flyout.FlyoutPresenterStyle>
									<Style TargetType="FlyoutPresenter">
										<Setter Property="BorderThickness" Value="0,0,0,0" />
										<Setter Property="Padding" Value="0,0,0,0" />
										<Setter Property="MinWidth" Value="0" />
										<Setter Property="MinHeight" Value="0" />
									</Style>
								</Flyout.FlyoutPresenterStyle>
								<Border Background="Green" Width="50" Height="50">
									<TextBlock Text="Flyout" />
								</Border>
							</Flyout>
						</Grid.Resources>
						<Rectangle Width="120" Height="120" Fill="Yellow" HorizontalAlignment="Center" VerticalAlignment="Center" />
						<Button x:Name="button" Content="button" Width="100" Height="100" HorizontalAlignment="Center" VerticalAlignment="Center" />
					</Grid>
				"""));

			TestServices.WindowHelper.WindowContent = rootPanel;

			button = (Button)(rootPanel.FindName("button"));
			// TODO Uno: FindName can't reach flyouts until the namescope work lands (uno--findname-misses-resource-and-flyout-names-in-xamlreader).
			flyout = rootPanel.Resources.Values.OfType<Flyout>().First();
		});

		await TestServices.WindowHelper.WaitForIdle();

		return (button, flyout);
	}

	// PerfOptIn=true is set via Data:PerfOptIn upstream (WindowHelper::InitializeXamlCore reads it during TestSetup and sets the override).
	// Uno has no PerfOptIn override, so this runs the same body as the Off variant.
	[TestMethod]
	[Description("Verify that keyboard accelerators on a custom default TextBlock ContextFlyout work with PerfOptIn enabled.")]
	public async Task VerifyAppResourceOverrideContextFlyoutAcceleratorsPerfOptInOn()
	{
		await VerifyAppResourceOverrideContextFlyoutAcceleratorsHelper();
	}

	// PerfOptIn=false is set via Data:PerfOptIn upstream.
	[TestMethod]
	[Description("Verify that keyboard accelerators on a custom default TextBlock ContextFlyout work with PerfOptIn disabled.")]
	public async Task VerifyAppResourceOverrideContextFlyoutAcceleratorsPerfOptInOff()
	{
		await VerifyAppResourceOverrideContextFlyoutAcceleratorsHelper();
	}

	private async Task VerifyAppResourceOverrideContextFlyoutAcceleratorsHelper()
	{
		StackPanel root = null;
		TextBlock textBlock = null;
		Button focusButton = null;
		MenuFlyout customFlyout = null;
		MenuFlyoutItem flyoutItem = null;
		KeyboardAccelerator accelerator = null;

		Event clickEvent = new();
		var clickRegistration = CreateSafeEventRegistration<MenuFlyoutItem, RoutedEventHandler>(nameof(MenuFlyoutItem.Click));

		try
		{
			await RunOnUIThread(() =>
			{
				// Create a MenuFlyout with two items. The keyboard accelerator (Ctrl+2) is on the
				// SECOND item, not the first. This is important: when the flyout opens, the first
				// item gets focus and ProcessLocalAccelerators checks accelerators on the focused
				// element. By putting the accelerator on the second item, we ensure it can only be
				// found via the global live accelerator collection (AddToLiveKeyboardAccelerators),
				// which exposes the visualTree=nullptr / wrong-ContentRoot bug.
				customFlyout = new MenuFlyout();

				var dummyItem = new MenuFlyoutItem();
				dummyItem.Text = "Dummy First Item";
				customFlyout.Items.Add(dummyItem);

				flyoutItem = new MenuFlyoutItem();
				flyoutItem.Text = "Custom Action";

				accelerator = new KeyboardAccelerator();
				accelerator.Key = global::Windows.System.VirtualKey.Number2;
				accelerator.Modifiers = global::Windows.System.VirtualKeyModifiers.Control;
				flyoutItem.KeyboardAccelerators.Add(accelerator);

				customFlyout.Items.Add(flyoutItem);

				// Override the default TextControlCommandBarContextFlyout in Application.Resources.
				Application.Current.Resources["TextControlCommandBarContextFlyout"] = customFlyout;

				root = (StackPanel)(XamlReader.Load(
					"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"    <TextBlock x:Name='textBlock' Text='Hello World' IsTextSelectionEnabled='True' />" +
					"    <Button x:Name='focusButton' Content='FocusButton' />" +
					"</StackPanel>"));

				textBlock = (TextBlock)(root.FindName("textBlock"));
				focusButton = (Button)(root.FindName("focusButton"));

				// Verify the TextBlock picks up our custom flyout as its default ContextFlyout.
				var contextFlyout = textBlock.ContextFlyout;
				VERIFY_IS_NOT_NULL(contextFlyout, "TextBlock should have a ContextFlyout");
				VERIFY_ARE_EQUAL(customFlyout, contextFlyout, "TextBlock should use our custom flyout from Application.Resources");

				clickRegistration.Attach(flyoutItem, (s, e) =>
				{
					LOG_OUTPUT("MenuFlyoutItem Click event fired!");

					clickEvent.Set();
				});

				TestServices.WindowHelper.WindowContent = root;
			});

			await TestServices.WindowHelper.WaitForIdle();

			// Focus the button so keyboard input is routed into the XAML tree.
			await RunOnUIThread(() =>
			{
				focusButton.Focus(FocusState.Keyboard);
			});
			await TestServices.WindowHelper.WaitForIdle();

			// Before opening the flyout, the accelerator does NOT fire. This is a known limitation
			// (https://github.com/microsoft/microsoft-ui-xaml/issues/11025):
			// CUIElement::EnterImpl passes visualTree=nullptr when entering the
			// ContextFlyout, so accelerators are registered on the vestigial CoreWindow ContentRoot
			// instead of the island's ContentRoot where keyboard input is actually processed.
			// See docs/design-notes/context-flyout.md ("The visualTree=nullptr Problem").
			LOG_OUTPUT("==> Press accelerator sequence before flyout has been opened: Ctrl + 2 (expect no invocation)");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_2#$u$_2#$u$_ctrlscan");
			await TestServices.WindowHelper.WaitForIdle();
#if HAS_UNO
			// Flyout.Enter recovers the visual tree from the owner to fix microsoft-ui-xaml#11025, so this upstream limitation doesn't apply.
			clickEvent.Reset();
#else
			VERIFY_IS_FALSE(clickEvent.HasFired(), "Accelerator should not fire when flyout has never been opened - accelerators are on the wrong ContentRoot (known limitation)");
#endif

			// Right click on textBlock to open the ContextFlyout, then try the accelerator while
			// the flyout is open. The accelerator is on the second MenuFlyoutItem, which does NOT
			// have focus (the first item gets focus). ProcessLocalAccelerators won't find it, and
			// the global collection has it on the wrong ContentRoot, so it still doesn't fire.
			LOG_OUTPUT("==> Right-click the TextBlock to open the ContextFlyout.");
			TestServices.InputHelper.ClickMouseButton(MouseButton.Right, await ControlHelper.GetCenterOfElement(textBlock));
			await TestServices.WindowHelper.WaitForIdle();

			clickEvent.Reset();
			LOG_OUTPUT("==> Press accelerator sequence while flyout is open: Ctrl + 2 (expect no invocation)");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrlscan#$d$_2#$u$_2#$u$_ctrlscan");
			await TestServices.WindowHelper.WaitForIdle();
#if !HAS_UNO
			VERIFY_IS_FALSE(clickEvent.HasFired(), "Accelerator should not fire even with flyout open - it's on the second item (not focused) and global registration is on wrong ContentRoot");
#endif
		}
		finally
		{
			// Clean up: remove the override from Application.Resources.
			await RunOnUIThread(() =>
			{
				Application.Current.Resources.Remove("TextControlCommandBarContextFlyout");
			});
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	private enum Theme
	{
		Dark = 0,
		Light = 1,
		HighContrast = 2
	}
}
