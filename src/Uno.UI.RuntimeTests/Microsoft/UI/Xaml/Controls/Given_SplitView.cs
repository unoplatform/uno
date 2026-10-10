using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Uno.UI.DevTools.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_SplitView
{
	// Kept narrow: the WinUI runtime test host clipped the right edge of a 400px-wide SplitView.
	private const double SplitViewWidth = 300;
	private const double SplitViewHeight = 200;
	private const double TestOpenPaneLength = 150;
	private const double TestCompactPaneLength = 50;

	[TestCleanup]
	public void Cleanup()
	{
		TestServices.WindowHelper.WindowContent = null;
	}

	private sealed class TestSplitView : SplitView
	{
		internal DependencyObject GetPart(string name) => GetTemplateChild(name);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task Update_OpenPaneLength()
	{
		// This test asserts changes to the OpenPaneLength property are reflected in to the column definition's width.
		// We assume here that the control-template is setup with: SplitView\Grid\@ColumnDefinition[0].Width bound to TemplateSettings.OpenPaneLength
		// Should the template change, this test will need to be updated accordingly or voided.

		var sut = new SplitView()
		{
			OpenPaneLength = 100,
			CompactPaneLength = 50
		};
		await UITestHelper.Load(sut, x => x.IsLoaded);

		var rootGrid = sut.FindFirstDescendant<Grid>() ?? throw new InvalidOperationException("failed to find root grid.");
		var columnDefinition = rootGrid.ColumnDefinitions.ElementAtOrDefault(0) ?? throw new InvalidOperationException("root grid doesnt contains any column definition");

		Assert.AreEqual(sut.OpenPaneLength, columnDefinition.Width.Value, "ColumnDefinition Width should be equal to OpenPaneLength");

		sut.OpenPaneLength = 105;
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(sut.OpenPaneLength, columnDefinition.Width.Value, "ColumnDefinition Width should be equal to OpenPaneLength after update");
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Pane_Lengths_Set_TemplateSettings_Follow()
	{
		var sut = new SplitView
		{
			OpenPaneLength = 200,
			CompactPaneLength = 50,
		};

		var settings = sut.TemplateSettings;
		Assert.AreEqual(200, settings.OpenPaneLength);
		Assert.AreEqual(-200, settings.NegativeOpenPaneLength);
		Assert.AreEqual(150, settings.OpenPaneLengthMinusCompactLength);
		Assert.AreEqual(-150, settings.NegativeOpenPaneLengthMinusCompactLength);
		Assert.AreEqual(200, settings.OpenPaneGridLength.Value);
		Assert.AreEqual(50, settings.CompactPaneGridLength.Value);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task Default_Template_Contains_Pinned_Parts()
	{
		var sut = new TestSplitView
		{
			Content = new Border(),
			Pane = new Border(),
			DisplayMode = SplitViewDisplayMode.Inline,
			IsPaneOpen = true,
		};

		await UITestHelper.Load(sut, x => x.IsLoaded);

		Assert.IsNotNull(sut.Template);
		Assert.IsNotNull(sut.GetPart("PaneRoot"));
		Assert.IsNotNull(sut.GetPart("ContentRoot"));
		Assert.IsInstanceOfType<RectangleGeometry>(sut.GetPart("PaneClipRectangle"));
		Assert.IsInstanceOfType<UIElement>(sut.GetPart("LightDismissLayer"));
		Assert.IsNotNull(GetStateGroup(sut, "DisplayModeStates"));
		Assert.IsNotNull(GetStateGroup(sut, "OverlayVisibilityStates"));

		sut.IsPaneOpen = false;
		await UITestHelper.WaitForIdle();
		sut.IsPaneOpen = true;
		await UITestHelper.WaitForIdle();
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Left, false)]
	[DataRow(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Right, false)]
	[DataRow(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Left, false)]
	[DataRow(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Right, false)]
	[DataRow(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Left, false)]
	[DataRow(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Right, false)]
	[DataRow(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Left, true)]
	[DataRow(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Right, true)]
	[DataRow(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Left, true)]
	[DataRow(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Right, true)]
	[DataRow(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Left, true)]
	[DataRow(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Right, true)]
	public async Task When_Pane_Open_PaneClipRectangle_Covers_Pane(SplitViewDisplayMode displayMode, SplitViewPanePlacement placement, bool useControlsResourcesStyle)
	{
		var sut = CreateColoredSplitView(displayMode, placement, useControlsResourcesStyle);
		sut.IsPaneOpen = true;

		await UITestHelper.Load(sut);
		await WaitForPaneAnimationsToSettle(sut);

		var paneClipRectangle = (RectangleGeometry)sut.GetPart("PaneClipRectangle");
		Assert.AreEqual(new Rect(0, 0, TestOpenPaneLength, sut.ActualHeight), paneClipRectangle.Rect);

		var screenshot = await UITestHelper.ScreenShot(sut);
		var paneCenterX = placement == SplitViewPanePlacement.Left
			? TestOpenPaneLength / 2
			: SplitViewWidth - TestOpenPaneLength / 2;
		var contentX = placement == SplitViewPanePlacement.Left
			? SplitViewWidth - 20
			: 20;

		ImageAssert.HasColorAt(screenshot, (float)paneCenterX, (float)(SplitViewHeight / 2), Colors.Red, tolerance: 5);
		ImageAssert.HasColorAt(screenshot, (float)contentX, (float)(SplitViewHeight / 2), Colors.Blue, tolerance: 5);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Left, false)]
	[DataRow(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Left, false)]
	[DataRow(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Right, false)]
	[DataRow(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Right, false)]
	[DataRow(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Left, true)]
	[DataRow(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Left, true)]
	[DataRow(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Right, true)]
	[DataRow(SplitViewDisplayMode.CompactInline, SplitViewPanePlacement.Right, true)]
	public async Task When_Pane_Closed_Compact_Shows_Compact_Strip(SplitViewDisplayMode displayMode, SplitViewPanePlacement placement, bool useControlsResourcesStyle)
	{
		var sut = CreateColoredSplitView(displayMode, placement, useControlsResourcesStyle);

		await UITestHelper.Load(sut);
		await WaitForPaneAnimationsToSettle(sut);

		var screenshot = await UITestHelper.ScreenShot(sut);
		var compactCenterX = placement == SplitViewPanePlacement.Left
			? TestCompactPaneLength / 2
			: SplitViewWidth - TestCompactPaneLength / 2;
		var contentX = placement == SplitViewPanePlacement.Left
			? TestCompactPaneLength + 20
			: SplitViewWidth - TestCompactPaneLength - 20;

		ImageAssert.HasColorAt(screenshot, (float)compactCenterX, (float)(SplitViewHeight / 2), Colors.Red, tolerance: 5);
		ImageAssert.HasColorAt(screenshot, (float)contentX, (float)(SplitViewHeight / 2), Colors.Blue, tolerance: 5);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Open_Overlay_Switches_To_Inline_Content_Moves_Beside_Pane(bool useControlsResourcesStyle)
	{
		// Mirrors the SamplesApp shell: an open Overlay pane that an AdaptiveTrigger later turns Inline.
		var sut = CreateColoredSplitView(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Left, useControlsResourcesStyle);
		sut.IsPaneOpen = true;

		await UITestHelper.Load(sut);

		sut.DisplayMode = SplitViewDisplayMode.Inline;
		await WaitForPaneAnimationsToSettle(sut);
		// The pane is already settled, and on WinUI the idle wait can finish before the next layout pass.
		sut.UpdateLayout();

		var content = (FrameworkElement)sut.Content;
		var contentLeft = content.TransformToVisual(sut).TransformPoint(default).X;
		Assert.AreEqual(TestOpenPaneLength, contentLeft, 0.5);
		Assert.AreEqual(SplitViewWidth - TestOpenPaneLength, content.ActualWidth, 0.5);
	}

	public enum InlineEntry
	{
		LoadedOpen,
		OpenedAfterLoad,
		ClosedThenReopened,
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(InlineEntry.LoadedOpen, false)]
	[DataRow(InlineEntry.LoadedOpen, true)]
	[DataRow(InlineEntry.OpenedAfterLoad, false)]
	[DataRow(InlineEntry.OpenedAfterLoad, true)]
	[DataRow(InlineEntry.ClosedThenReopened, false)]
	[DataRow(InlineEntry.ClosedThenReopened, true)]
	public async Task When_Open_Inline_Switches_To_Overlay_Content_Spans_Full_Width(InlineEntry entry, bool useControlsResourcesStyle)
	{
		// Mirrors narrowing the SamplesApp shell: its AdaptiveTrigger setter drops back to the local Overlay value.
		var sut = CreateColoredSplitView(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Left, useControlsResourcesStyle);
		sut.IsPaneOpen = entry == InlineEntry.LoadedOpen;

		await UITestHelper.Load(sut);

		if (entry == InlineEntry.ClosedThenReopened)
		{
			sut.IsPaneOpen = true;
			await WaitForPaneAnimationsToSettle(sut);
			sut.IsPaneOpen = false;
			await UITestHelper.WaitForIdle();
		}

		if (entry != InlineEntry.LoadedOpen)
		{
			sut.IsPaneOpen = true;
		}

		await WaitForPaneAnimationsToSettle(sut);

		sut.DisplayMode = SplitViewDisplayMode.Overlay;
		await WaitForPaneAnimationsToSettle(sut);
		sut.UpdateLayout();

		var content = (FrameworkElement)sut.Content;
		Assert.AreEqual(0, content.TransformToVisual(sut).TransformPoint(default).X, 0.5);
		Assert.AreEqual(SplitViewWidth, content.ActualWidth, 0.5);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_AdaptiveTrigger_Inline_Deactivates_Content_Spans_Full_Width(bool startOpen)
	{
		// The SamplesApp shell: local Overlay, an AdaptiveTrigger setter switching to Inline on wide windows.
		var root = (Grid)Microsoft.UI.Xaml.Markup.XamlReader.Load($$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				  Width="{{SplitViewWidth}}" Height="{{SplitViewHeight}}">
				<SplitView x:Name="SplitView" DisplayMode="Overlay" OpenPaneLength="{{TestOpenPaneLength}}" IsPaneOpen="{{startOpen}}">
					<SplitView.Pane><Border Background="Red" /></SplitView.Pane>
					<SplitView.Content><Border x:Name="ContentBorder" Background="Blue" /></SplitView.Content>
				</SplitView>
				<VisualStateManager.VisualStateGroups>
					<VisualStateGroup>
						<VisualState x:Name="TabletState">
							<VisualState.StateTriggers>
								<AdaptiveTrigger MinWindowWidth="0" />
							</VisualState.StateTriggers>
							<VisualState.Setters>
								<Setter Target="SplitView.DisplayMode" Value="Inline" />
							</VisualState.Setters>
						</VisualState>
					</VisualStateGroup>
				</VisualStateManager.VisualStateGroups>
			</Grid>
			""");

		// Like the shell's SampleChooserControl, host the state triggers in a UserControl: WinUI doesn't evaluate them on bare window content.
		await UITestHelper.Load(new UserControl { Content = root });

		var sut = (SplitView)root.FindName("SplitView");
		var content = (FrameworkElement)root.FindName("ContentBorder");
		var trigger = (AdaptiveTrigger)VisualStateManager.GetVisualStateGroups(root)[0].States[0].StateTriggers[0];

		Assert.AreEqual(SplitViewDisplayMode.Inline, sut.DisplayMode);

		if (!startOpen)
		{
			sut.IsPaneOpen = true;
		}

		await UITestHelper.WaitForIdle();
		await Task.Delay(500);
		sut.UpdateLayout();
		Assert.AreEqual(TestOpenPaneLength, content.TransformToVisual(sut).TransformPoint(default).X, 0.5, "Inline");

		trigger.MinWindowWidth = 100_000;
		await UITestHelper.WaitFor(() => sut.DisplayMode == SplitViewDisplayMode.Overlay);
		await UITestHelper.WaitForIdle();
		await Task.Delay(500);
		sut.UpdateLayout();

		Assert.IsTrue(sut.IsPaneOpen);
		Assert.AreEqual(0, content.TransformToVisual(sut).TransformPoint(default).X, 0.5, "Overlay");
		Assert.AreEqual(SplitViewWidth, content.ActualWidth, 0.5);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_LightDismissOverlayMode_Changes_OverlayVisibilityStates(bool useControlsResourcesStyle)
	{
		var sut = CreateColoredSplitView(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Left, useControlsResourcesStyle);
		sut.LightDismissOverlayMode = LightDismissOverlayMode.Off;

		await UITestHelper.Load(sut);

		var overlayStates = GetStateGroup(sut, "OverlayVisibilityStates");
		Assert.IsNotNull(overlayStates);
		Assert.AreEqual("OverlayNotVisible", overlayStates.CurrentState?.Name);

		sut.LightDismissOverlayMode = LightDismissOverlayMode.On;
		await UITestHelper.WaitFor(() => overlayStates.CurrentState?.Name == "OverlayVisible");

		sut.LightDismissOverlayMode = LightDismissOverlayMode.Off;
		await UITestHelper.WaitFor(() => overlayStates.CurrentState?.Name == "OverlayNotVisible");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Pane_Toggled_Events_Are_Raised_In_Order()
	{
		var sut = CreateColoredSplitView(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Left);
		await UITestHelper.Load(sut);

		var events = new List<string>();
		sut.PaneOpening += (_, _) => events.Add("PaneOpening");
		sut.PaneOpened += (_, _) => events.Add("PaneOpened");
		sut.PaneClosing += (_, _) => events.Add("PaneClosing");
		sut.PaneClosed += (_, _) => events.Add("PaneClosed");

		sut.IsPaneOpen = true;
		await UITestHelper.WaitFor(() => events.Contains("PaneOpened"));
		CollectionAssert.AreEqual(new[] { "PaneOpening", "PaneOpened" }, events);

		events.Clear();
		sut.IsPaneOpen = false;
		await UITestHelper.WaitFor(() => events.Contains("PaneClosed"));
		CollectionAssert.AreEqual(new[] { "PaneClosing", "PaneClosed" }, events);

		events.Clear();
		sut.DisplayMode = SplitViewDisplayMode.Overlay;
		await UITestHelper.WaitForIdle();
		Assert.AreEqual(0, events.Count, "Changing the display mode of a closed pane must not raise pane events.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task Right_Pane_Inline_Transitions_Are_Present_And_Run()
	{
		var inline = await LoadRightPane(SplitViewDisplayMode.Inline);
		var rootGrid = inline.FindFirstDescendant<Grid>();
		Assert.IsNotNull(rootGrid);

		var displayModeStates = VisualStateManager
			.GetVisualStateGroups(rootGrid)
			.Single(group => group.Name == "DisplayModeStates");
		var transitions = displayModeStates.Transitions
			.Select(transition => (transition.From, transition.To))
			.ToArray();

		CollectionAssert.IsSubsetOf(
			new[]
			{
				("Closed", "OpenInlineRight"),
				("OpenInlineRight", "Closed"),
				("ClosedCompactRight", "OpenInlineRight"),
				("OpenInlineRight", "ClosedCompactRight"),
			},
			transitions);

		await ExerciseRightPane(inline, SplitViewDisplayMode.Inline);

		var compactInline = await LoadRightPane(SplitViewDisplayMode.CompactInline);
		await ExerciseRightPane(compactInline, SplitViewDisplayMode.CompactInline);
	}

#if HAS_UNO
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	[TestMethod]
	[RunsOnUIThread]
	[DataRow(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Left)]
	[DataRow(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Right)]
	[DataRow(SplitViewDisplayMode.CompactOverlay, SplitViewPanePlacement.Left)]
	public async Task When_LightDismissLayer_Clicked_Pane_Closes(SplitViewDisplayMode displayMode, SplitViewPanePlacement placement)
	{
		var sut = CreateColoredSplitView(displayMode, placement);
		sut.IsPaneOpen = true;

		var closingCount = 0;
		var closedCount = 0;
		sut.PaneClosing += (_, _) => closingCount++;
		sut.PaneClosed += (_, _) => closedCount++;

		await UITestHelper.Load(sut);
		await WaitForPaneAnimationsToSettle(sut);

		ClickOutsidePane(sut, placement);

		await UITestHelper.WaitFor(() => !sut.IsPaneOpen);
		await UITestHelper.WaitFor(() => closedCount == 1);
		Assert.AreEqual(1, closingCount);
	}

#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_LightDismiss_Closing_Canceled_Pane_Stays_Open()
	{
		var sut = CreateColoredSplitView(SplitViewDisplayMode.Overlay, SplitViewPanePlacement.Left);
		sut.IsPaneOpen = true;

		var cancel = true;
		var closingCount = 0;
		sut.PaneClosing += (_, args) =>
		{
			closingCount++;
			args.Cancel = cancel;
		};

		await UITestHelper.Load(sut);
		await WaitForPaneAnimationsToSettle(sut);

		ClickOutsidePane(sut, SplitViewPanePlacement.Left);
		await UITestHelper.WaitFor(() => closingCount == 1);
		await UITestHelper.WaitForIdle();
		Assert.IsTrue(sut.IsPaneOpen);

		// A canceled light dismiss must not block the next one.
		cancel = false;
		ClickOutsidePane(sut, SplitViewPanePlacement.Left);
		await UITestHelper.WaitFor(() => !sut.IsPaneOpen);
		Assert.AreEqual(2, closingCount);
	}

#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Inline_Content_Clicked_Pane_Stays_Open()
	{
		var sut = CreateColoredSplitView(SplitViewDisplayMode.Inline, SplitViewPanePlacement.Left);
		sut.IsPaneOpen = true;

		await UITestHelper.Load(sut);
		await WaitForPaneAnimationsToSettle(sut);

		ClickOutsidePane(sut, SplitViewPanePlacement.Left);
		await UITestHelper.WaitForIdle();

		Assert.IsTrue(sut.IsPaneOpen);
	}

	private static void ClickOutsidePane(SplitView sut, SplitViewPanePlacement placement)
	{
		var bounds = sut.GetAbsoluteBounds();
		var x = placement == SplitViewPanePlacement.Left
			? bounds.Right - 20
			: bounds.Left + 20;
		var position = new Point(x, bounds.Top + bounds.Height / 2);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();
		mouse.MoveTo(position);
		mouse.Press(position);
		mouse.Release();
	}
#endif

	// TestSplitView only gets the generic.xaml default style; apps with XamlControlsResources get DefaultSplitViewStyle.
	private static TestSplitView CreateColoredSplitView(SplitViewDisplayMode displayMode, SplitViewPanePlacement placement, bool useControlsResourcesStyle = false)
	{
		TestSplitView splitView = new()
		{
			Width = SplitViewWidth,
			Height = SplitViewHeight,
			OpenPaneLength = TestOpenPaneLength,
			CompactPaneLength = TestCompactPaneLength,
			DisplayMode = displayMode,
			PanePlacement = placement,
			Pane = new Border { Background = new SolidColorBrush(Colors.Red) },
			Content = new Border { Background = new SolidColorBrush(Colors.Blue) },
		};

		if (useControlsResourcesStyle)
		{
			splitView.Style = (Style)Application.Current.Resources["DefaultSplitViewStyle"];
		}

		return splitView;
	}

	private static VisualStateGroup GetStateGroup(SplitView sut, string name)
	{
		var templateRoot = (FrameworkElement)VisualTreeHelper.GetChild(sut, 0);
		return VisualStateManager.GetVisualStateGroups(templateRoot).SingleOrDefault(group => group.Name == name);
	}

	private static async Task WaitForPaneAnimationsToSettle(TestSplitView sut)
	{
		var paneTransform = (CompositeTransform)((UIElement)sut.GetPart("PaneRoot")).RenderTransform;
		var paneClipTransform = (CompositeTransform)((RectangleGeometry)sut.GetPart("PaneClipRectangle")).Transform;

		// Closed compact states shift the clip so that only the compact strip of the pane stays visible.
		var expectedClipTranslateX = sut.IsPaneOpen
			? 0
			: sut.PanePlacement == SplitViewPanePlacement.Left
				? sut.TemplateSettings.NegativeOpenPaneLengthMinusCompactLength
				: sut.TemplateSettings.OpenPaneLengthMinusCompactLength;

		await UITestHelper.WaitFor(() => paneTransform.TranslateX == 0 && paneClipTransform.TranslateX == expectedClipTranslateX);

		await UITestHelper.WaitForIdle();
	}

	private static async Task<TestSplitView> LoadRightPane(SplitViewDisplayMode displayMode)
	{
		var sut = new TestSplitView
		{
			Content = new Border(),
			Pane = new Border(),
			DisplayMode = displayMode,
			PanePlacement = SplitViewPanePlacement.Right,
			IsPaneOpen = false,
			// A derived control only gets the generic.xaml default style; the right inline transitions are in the XamlControlsResources one.
			Style = (Style)Application.Current.Resources["DefaultSplitViewStyle"],
		};

		await UITestHelper.Load(sut, x => x.IsLoaded);
		return sut;
	}

	private static async Task ExerciseRightPane(TestSplitView sut, SplitViewDisplayMode displayMode)
	{
		var paneRoot = sut.GetPart("PaneRoot") as FrameworkElement;
		Assert.IsNotNull(paneRoot);

		sut.IsPaneOpen = true;
		await UITestHelper.WaitFor(() => Grid.GetColumn(paneRoot) == 1 && Grid.GetColumnSpan(paneRoot) == 1);

		sut.IsPaneOpen = false;
		if (displayMode == SplitViewDisplayMode.Inline)
		{
			await UITestHelper.WaitFor(() => paneRoot.Visibility == Visibility.Collapsed);
		}
		else
		{
			await UITestHelper.WaitFor(() => Grid.GetColumnSpan(paneRoot) == 2);
			Assert.AreEqual(HorizontalAlignment.Right, paneRoot.HorizontalAlignment);
		}
	}
}
