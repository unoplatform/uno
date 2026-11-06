// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PopupTests.cpp, commit bac7a9c33

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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
// - Upstream compares MockDComp / UIElement dump masters (HasAssociatedMasterFile, VerifyMockDCompOutput). Those
//   dumps are replaced by explicit assertions on what the masters pin: bounds of the popup content relative to
//   its ancestors, the z-order of the open popups, whether a popup is open and whether it is windowed.
// - Windowed state is asserted through the Uno test hooks PopupHelper.IsWindowed (Popup.IsWindowed) and
//   PopupHelper.VerifyWindowedPopupRect (Popup.GetWindowedPopupMoveAndResizeRect). See PopupIntegrationTests
//   for the conventions: HWND rects in the masters were recorded in a WPF host whose XAML island sits 31 px below
//   the origin of the HWND coordinate space, and include a 10 px drop shadow gutter on every side.
// - The tests are laid out relative to the content root, as SetWindowSizeOverride is a no-op in Uno.
// - VisualTreeHelper.GetOpenPopupsForXamlRoot lists the most recently opened popup first.
// - Pure DComp checks (WindowBackgroundTreatment effect brush, device lost, SystemBackdropPlacementVisual
//   rounded corner clip, surfaces of RasterizationScale) cannot be observed from the public tree. They are
//   noted in comments where the test still ports the rest of the scenario.
[TestClass]
[RequiresFullWindow] // The upstream tests expect window-absolute coordinates.
public class PopupTests
{
	private const int E_UNEXPECTED = unchecked((int)0x8000FFFF);

	[TestCleanup]
	public async Task TestCleanup()
	{
		await RunOnUIThread(() =>
		{
			if (TestServices.WindowHelper.XamlRoot is { } xamlRoot)
			{
				VisualTreeHelper.CloseAllPopups(xamlRoot);
			}

			TestServices.WindowHelper.WindowContent = null;
		});
	}

	private static async Task<Popup> MakeRTLPopupInLTRTree()
	{
		// Grid1[compnode]-Grid2-Canvas3[RTL]-Popup[RTL]-Rectangle[RTL]
		Grid grid1 = null;
		Grid grid2 = null;
		Canvas canvas3 = null;
		Popup popup = null;
		Rectangle rectangle = null;

		await RunOnUIThread(() =>
		{
			rectangle = new Rectangle();
			rectangle.FlowDirection = FlowDirection.RightToLeft;
			rectangle.Width = 100;
			rectangle.Height = 100;
			rectangle.Fill = new SolidColorBrush(Colors.Green);

			popup = new Popup();
			popup.FlowDirection = FlowDirection.RightToLeft;
			popup.Width = 100;
			popup.Height = 100;
			popup.Child = rectangle;
			Canvas.SetLeft(popup, 50.0);
			Canvas.SetTop(popup, 50.0);

			canvas3 = new Canvas();
			canvas3.FlowDirection = FlowDirection.RightToLeft;
			canvas3.Width = 200;
			canvas3.Height = 200;
			canvas3.Children.Add(popup);

			grid2 = new Grid();
			grid2.FlowDirection = FlowDirection.LeftToRight;
			grid2.Width = 200;
			grid2.Height = 200;
			grid2.Children.Add(canvas3);

			grid1 = new Grid();
			grid1.FlowDirection = FlowDirection.LeftToRight;
			grid1.Width = 200;
			grid1.Height = 200;
			grid1.CompositeMode = ElementCompositeMode.SourceOver;
			grid1.Children.Add(grid2);

			TestServices.WindowHelper.WindowContent = grid1;
		});

		// Let the tree render before opening the popup. Otherwise, the popup could be incorrectly detected as a parentless popup.
		await TestServices.WindowHelper.WaitForIdle();

		return popup;
	}

	private static async Task<Rect> GetBoundsRelativeTo(FrameworkElement element, UIElement ancestor)
	{
		Rect rect = default;

		await RunOnUIThread(() =>
		{
			var transform = element.TransformToVisual(ancestor);
			var topLeft = transform.TransformPoint(new Point(0, 0));
			var bottomRight = transform.TransformPoint(new Point(element.ActualWidth, element.ActualHeight));

			rect = new Rect(
				Math.Min(topLeft.X, bottomRight.X),
				Math.Min(topLeft.Y, bottomRight.Y),
				Math.Abs(topLeft.X - bottomRight.X),
				Math.Abs(topLeft.Y - bottomRight.Y));
		});

		return rect;
	}

	private static void VerifyBounds(Rect actual, double x, double y, double width, double height)
	{
		LOG_OUTPUT($"Bounds: (X, Y, Width, Height) = ({actual.X:0.##}, {actual.Y:0.##}, {actual.Width:0.##}, {actual.Height:0.##}), expected ({x:0.##}, {y:0.##}, {width:0.##}, {height:0.##})");

		VERIFY_ARE_VERY_CLOSE(actual.X, x, 1, "left");
		VERIFY_ARE_VERY_CLOSE(actual.Y, y, 1, "top");
		VERIFY_ARE_VERY_CLOSE(actual.Width, width, 1, "width");
		VERIFY_ARE_VERY_CLOSE(actual.Height, height, 1, "height");
	}

	// Verifies the open popups of the XamlRoot, most recently opened first.
	private static async Task VerifyOpenPopups(XamlRoot xamlRoot, params Popup[] expectedTopmostFirst)
	{
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot);

			VERIFY_ARE_EQUAL(expectedTopmostFirst.Length, popups.Count);
			for (var i = 0; i < expectedTopmostFirst.Length; i++)
			{
				VERIFY_IS_TRUE(ReferenceEquals(expectedTopmostFirst[i], popups[i]), $"Open popup at index {i} (0 is topmost) is not the expected one.");
			}
		});
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: windowed popups are a desktop concept
	public async Task WindowedPopupHasForcedBackground()
	{
		Popup popup = null;
		Popup windowedPopup = null;

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("  > Opening popups");

			Canvas root = new Canvas();
			TestServices.WindowHelper.WindowContent = root;

			Rectangle rectangle = new Rectangle();
			rectangle.Width = 10;
			rectangle.Height = 10;
			rectangle.Fill = new SolidColorBrush(Colors.Red);

			Canvas canvas = new Canvas();
			canvas.Width = 300;
			canvas.Height = 300;
			canvas.Background = new SolidColorBrush(Colors.Transparent);
			canvas.Children.Add(rectangle);

			// Upstream relies on the single root of its test window. Uno's test window is an island-like root: it has to be told.
			popup = new Popup();
			popup.XamlRoot = root.XamlRoot;
			popup.Child = canvas;
			popup.IsOpen = true;

			Rectangle rectangle2 = new Rectangle();
			rectangle2.Width = 12;
			rectangle2.Height = 10;
			rectangle2.Fill = new SolidColorBrush(Colors.Red);

			Canvas canvas2 = new Canvas();
			canvas2.Width = 302;
			canvas2.Height = 300;
			canvas2.Background = new SolidColorBrush(Colors.Transparent);
			canvas2.Children.Add(rectangle2);

			windowedPopup = new Popup();
			windowedPopup.XamlRoot = root.XamlRoot;
			PopupHelper.SetWindowed(windowedPopup);
			windowedPopup.Child = canvas2;
			windowedPopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Upstream: VerifyMockDCompOutput(NoComparison). The master pins:
		// - the inline popup is not windowed, its content is 300x300 with a 10x10 red rectangle.
		// - the windowed popup lives in its own window, its content is 302x300 with a 12x10 red rectangle,
		//   and gets a WindowBackgroundTreatment EffectBrush (the forced background, DComp only).
		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(PopupHelper.IsWindowed(popup));
			VERIFY_IS_TRUE(PopupHelper.IsWindowed(windowedPopup));

			var canvas = (Canvas)popup.Child;
			VERIFY_ARE_EQUAL(300.0, canvas.ActualWidth);
			VERIFY_ARE_EQUAL(300.0, canvas.ActualHeight);

			var canvas2 = (Canvas)windowedPopup.Child;
			VERIFY_ARE_EQUAL(302.0, canvas2.ActualWidth);
			VERIFY_ARE_EQUAL(300.0, canvas2.ActualHeight);
		});
		await PopupHelper.VerifyWindowedPopupRect(windowedPopup, -10, -2, 322, 320);

		// Upstream: Device lost (ResetDeviceAndVisuals) and 2.0 zoom scale (SetWindowSizeOverrideWithScale) steps, each
		// followed by VerifyMockDCompOutput. They re-verify the same trees (the second master adds a 2x scale transform
		// on the root and on the windowed popup's window). Not ported: needs D3D device loss and a window scale override.
		// TODO Uno: needs TestServices.WindowHelper.ResetDeviceAndVisuals / SetWindowSizeOverrideWithScale
	}

	[TestMethod]
	public async Task ParentedPopup_ValidateRequestedThemePropagation()
	{
		Popup popup = await MakeRTLPopupInLTRTree();

		await RunOnUIThread(() =>
		{
			var parent = (FrameworkElement)(popup.Parent);
			parent.RequestedTheme = ElementTheme.Light;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup.IsOpen = true;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var child = (FrameworkElement)(popup.Child);
			VERIFY_IS_TRUE(child.ActualTheme == ElementTheme.Light);
		});
	}

	[TestMethod]
	public async Task ParentedPopup_RTLSubtreeInLTRTreeWUC()
	{
		Popup popup = await MakeRTLPopupInLTRTree();

		await RunOnUIThread(() =>
		{
			popup.IsOpen = true;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Upstream: VerifyMockDCompOutput(NoComparison). The master pins a 100x100 popup content at offset (50, 50)
		// in Canvas3 (200x200, in turn at the center of the 400x300 window), mirrored by the RTL transform of Canvas3.
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(popup.IsOpen);
			VERIFY_IS_FALSE(PopupHelper.IsWindowed(popup));
		});

		var grid1 = (UIElement)TestServices.WindowHelper.WindowContent;
		var rectangle = (FrameworkElement)popup.Child;
		VerifyBounds(await GetBoundsRelativeTo(rectangle, grid1), 50, 50, 100, 100);
	}

	[TestMethod]
	public async Task ParentedPopup_RTLSubtreeInLTRTreeWUC_AnimatedOffset()
	{
		Popup popup = await MakeRTLPopupInLTRTree();
		Storyboard sb = null;

		await RunOnUIThread(() =>
		{
			popup.IsOpen = true;

			DoubleAnimation da = MakeDoubleAnimation(popup, "(Microsoft.UI.Xaml.Controls:Canvas.Left)");
			da.From = 75.0;
			da.To = 75.0;

			sb = new Storyboard();
			sb.Children.Add(da);
			sb.Begin();
		});

		await TestServices.WindowHelper.SynchronouslyTickUIThread(2);
		await TestServices.WindowHelper.WaitForIdle();

		// Upstream: VerifyMockDCompOutput(NoComparison). The master pins the popup content at an animated
		// offset of (75, 50) in Canvas3 (200x200, RTL), i.e. mirrored to x = 200 - 75 - 100 = 25.
		var grid1 = (UIElement)TestServices.WindowHelper.WindowContent;
		var rectangle = (FrameworkElement)popup.Child;
		VerifyBounds(await GetBoundsRelativeTo(rectangle, grid1), 25, 50, 100, 100);

		await RunOnUIThread(() =>
		{
			sb.Stop();
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Ignore("Uno has no hand-off visual: ElementCompositionPreview gives the element's own visual, whose Offset is the layout offset under the RTL mirroring, not a visual composed above it.")]
	public async Task ParentedPopup_RTLSubtreeInLTRTreeWUC_HandoffVisual()
	{
		Popup popup = await MakeRTLPopupInLTRTree();
		global::Microsoft.UI.Composition.Visual handOffVisual = null;

		await RunOnUIThread(() =>
		{
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			handOffVisual = ElementCompositionPreview.GetElementVisual(popup);
			Vector3 newOffset = new Vector3(12, 34, 0);
			handOffVisual.Offset = newOffset;
		});

		await TestServices.WindowHelper.SynchronouslyTickUIThread(2);

		// Upstream: VerifyMockDCompOutput(NoComparison). The master pins the popup content at the hand-off visual
		// offset of (12, 34) in Canvas3 (200x200, RTL), i.e. mirrored to x = 200 - 12 - 100 = 88.
		var grid1 = (UIElement)TestServices.WindowHelper.WindowContent;
		var rectangle = (FrameworkElement)popup.Child;
		VerifyBounds(await GetBoundsRelativeTo(rectangle, grid1), 88, 34, 100, 100);
	}

	private static Popup MakePopup(UIElement child)
	{
		Canvas canvas = new Canvas();
		canvas.Width = 10;
		canvas.Height = 10;
		if (child != null)
		{
			canvas.Children.Add(child);
		}

		Popup popup;
		popup = new Popup();
		popup.XamlRoot = TestServices.WindowHelper.XamlRoot; // Uno: as a desktop host, a parentless popup needs one.
		popup.Child = canvas;
		return popup;
	}

	private static DoubleAnimation MakeDoubleAnimation(DependencyObject target, string targetProperty)
	{
		var da = new DoubleAnimation();
		da.From = 0.0;
		da.To = 100.0;
		da.Duration = new Duration(TimeSpan.FromTicks(10000000L));
		Storyboard.SetTarget(da, target);
		Storyboard.SetTargetProperty(da, targetProperty);

		return da;
	}

	[TestMethod]
	public async Task ResetNestedPopups()
	{
		Canvas root = null;
		Popup r1 = null;  // Rooted
		Popup r1_1 = null;
		Popup r1_1_1 = null;
		Popup r1_2 = null;
		Popup u2 = null;  // Unrooted
		Popup u3 = null;
		Popup u3_1 = null;
		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Creating popup tree.");

			r1_1_1 = MakePopup(null);
			r1_1 = MakePopup(r1_1_1);
			r1_2 = MakePopup(null);
			Canvas canvas = new Canvas();
			canvas.Children.Add(r1_1);
			canvas.Children.Add(r1_2);
			r1 = MakePopup(canvas);

			u2 = MakePopup(null);

			u3_1 = MakePopup(null);
			u3 = MakePopup(u3_1);

			root = new Canvas();
			root.Children.Add(r1);
			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			{
				var xamlRoot = root.XamlRoot;
				if (xamlRoot != null)
				{
					// UAP will return a null content root and does not need this to be set
					u2.XamlRoot = xamlRoot;
					u3.XamlRoot = xamlRoot;
				}
			}

			LOG_OUTPUT("> Open nested popups in a messy order. Verify that we don't crash when resetting the visual tree.");
			LOG_OUTPUT("> Rooted popups - open parent, then child, then grandparent, then sibling.");
			LOG_OUTPUT("> Unrooted popups - open child, then parent.");
			r1_1.IsOpen = true;
			r1_1_1.IsOpen = true;
			u2.IsOpen = true;
			r1.IsOpen = true;
			u3_1.IsOpen = true;
			u3.IsOpen = true;
			r1_2.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("> Ending test, which closes all nested popups and all rooted popups via TestCleanupWrapper. Don't crash.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: windowed popups are a desktop concept
	public async Task WindowedPopupsMoveWithAncestorChain()
	{
		Canvas popupAncestor = null;
		Canvas root = null;
		Canvas popupContent = null;
		Canvas popupContent2 = null;
		Popup childPopup = null;
		Popup grandchildPopup = null;
		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Creating windowed popup tree.");

			popupContent = new Canvas();
			popupContent.Width = 100;
			popupContent.Height = 100;
			popupContent.Background = new SolidColorBrush(Colors.Green);

			grandchildPopup = MakePopup(popupContent);
			grandchildPopup.ShouldConstrainToRootBounds = false;
			grandchildPopup.VerticalOffset = 50;
			Canvas.SetLeft(grandchildPopup, 50);

			Canvas childCanvas = new Canvas();
			childCanvas.CompositeMode = ElementCompositeMode.SourceOver;
			childCanvas.Children.Add(grandchildPopup);
			Canvas.SetLeft(childCanvas, 100);
			Canvas.SetTop(childCanvas, 100);

			popupContent2 = new Canvas();
			popupContent2.Width = 100;
			popupContent2.Height = 100;
			popupContent2.Background = new SolidColorBrush(Colors.Green);

			childPopup = MakePopup(popupContent2);
			childPopup.ShouldConstrainToRootBounds = false;
			childPopup.VerticalOffset = 100;
			Canvas.SetLeft(childPopup, 100);

			popupAncestor = new Canvas();
			popupAncestor.Children.Add(childPopup);
			popupAncestor.Children.Add(childCanvas);

			root = new Canvas();
			root.Children.Add(popupAncestor);
			TestServices.WindowHelper.WindowContent = root;

			childPopup.IsOpen = true;
			grandchildPopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Upstream: VerifyMockDCompOutput(NoComparison, "Initial"). The master pins two windowed popups whose
		// windows (HwndLeft, HwndTop) sit at (100, 100) and (150, 150), i.e. at the position of their content.
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(PopupHelper.IsWindowed(childPopup));
			VERIFY_IS_TRUE(PopupHelper.IsWindowed(grandchildPopup));
		});
		VerifyBounds(await GetBoundsRelativeTo(popupContent2, root), 100, 100, 100, 100);
		VerifyBounds(await GetBoundsRelativeTo(popupContent, root), 150, 150, 100, 100);

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Moving windowed popup ancestor. The windowed popup is expected to move to follow the ancestor.");
			Canvas.SetLeft(popupAncestor, 100);
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Upstream: VerifyMockDCompOutput(NoComparison, "ParentMoved"). The windows moved by 100 to (200, 100) and (250, 150).
		VerifyBounds(await GetBoundsRelativeTo(popupContent2, root), 200, 100, 100, 100);
		VerifyBounds(await GetBoundsRelativeTo(popupContent, root), 250, 150, 100, 100);
	}

	[TestMethod]
	public async Task RasterizationScale()
	{
		Popup belowRasterizationScale = null;
		Popup hasRasterizationScale = null;   // Note: Popup's own RasterizationScale is ignored, just like how a ScaleTransform on a Popup will still leave blurry content
		Popup aboveRasterizationScale = null;

		Popup hasRasterizationScale_parentless = null;
		Popup aboveRasterizationScale_parentless = null;

		Canvas root = null;
		var ellipses = new List<Ellipse>();
		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Creating popup tree.");

			{
				Ellipse ellipse = new Ellipse();
				ellipse.Width = 100;
				ellipse.Height = 100;
				ellipse.RasterizationScale = 2;
				ellipse.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 0, 26));
				ellipses.Add(ellipse);

				aboveRasterizationScale = MakePopup(ellipse);
			}

			{
				Ellipse ellipse = new Ellipse();
				ellipse.Width = 100;
				ellipse.Height = 100;
				ellipse.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 0, 51));
				ellipses.Add(ellipse);

				hasRasterizationScale = MakePopup(ellipse);
				hasRasterizationScale.RasterizationScale = 2;
			}

			{
				Ellipse ellipse = new Ellipse();
				ellipse.Width = 100;
				ellipse.Height = 100;
				ellipse.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 0, 77));
				ellipses.Add(ellipse);

				belowRasterizationScale = MakePopup(ellipse);
			}
			Canvas rasterizationScaleCanvas = new Canvas();
			rasterizationScaleCanvas.RasterizationScale = 2;
			rasterizationScaleCanvas.Children.Add(belowRasterizationScale);

			root = new Canvas();
			root.Children.Add(aboveRasterizationScale);
			root.Children.Add(hasRasterizationScale);
			root.Children.Add(rasterizationScaleCanvas);

			{
				Ellipse ellipse = new Ellipse();
				ellipse.Width = 100;
				ellipse.Height = 100;
				ellipse.RasterizationScale = 2;
				ellipse.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 0, 102));
				ellipses.Add(ellipse);

				aboveRasterizationScale_parentless = MakePopup(ellipse);
			}

			{
				Ellipse ellipse = new Ellipse();
				ellipse.Width = 100;
				ellipse.Height = 100;
				ellipse.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 0, 128));
				ellipses.Add(ellipse);

				hasRasterizationScale_parentless = MakePopup(ellipse);
				hasRasterizationScale_parentless.RasterizationScale = 2;
			}

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			{
				var xamlRoot = root.XamlRoot;
				if (xamlRoot != null)
				{
					// UAP will return a null content root and does not need this to be set
					aboveRasterizationScale_parentless.XamlRoot = xamlRoot;
					hasRasterizationScale_parentless.XamlRoot = xamlRoot;
				}
			}

			aboveRasterizationScale.IsOpen = true;
			hasRasterizationScale.IsOpen = true;
			belowRasterizationScale.IsOpen = true;

			aboveRasterizationScale_parentless.IsOpen = true;
			hasRasterizationScale_parentless.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Upstream: VerifyMockDCompOutput(ReferencedOnly). The master pins five 100x100 ellipses, stacked in the order
		// they opened (the color of the later popups is stacked on top: rgb(0,0,26) ... rgb(0,0,128)),
		// all at the top-left corner of the window, each in its own popup. The masked surfaces (the rasterization
		// scale of each ellipse, 5 master PNGs) are DComp surfaces and cannot be observed from the public tree.
		await VerifyOpenPopups(
			root.XamlRoot,
			hasRasterizationScale_parentless,
			aboveRasterizationScale_parentless,
			belowRasterizationScale,
			hasRasterizationScale,
			aboveRasterizationScale);

		foreach (var ellipse in ellipses)
		{
			VerifyBounds(await GetBoundsRelativeTo(ellipse, root), 0, 0, 100, 100);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: windowed popups are a desktop concept
	public async Task InlinePopupInWindowedPopup()
	{
		Popup windowedPopup = null;
		Rectangle windowedPopupContent = null;
		Popup inlineRedPopup = null;
		Popup inlineBluePopup = null;
		Ellipse redEllipse = null;
		Ellipse blueEllipse = null;

		Canvas root = null;
		Canvas windowedPopupCanvas = null;
		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Creating popup tree.");

			blueEllipse = new Ellipse();
			blueEllipse.Width = 100;
			blueEllipse.Height = 100;
			blueEllipse.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 0, 255));
			inlineBluePopup = MakePopup(blueEllipse);

			redEllipse = new Ellipse();
			redEllipse.Width = 100;
			redEllipse.Height = 100;
			redEllipse.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 255, 0, 0));
			inlineRedPopup = MakePopup(redEllipse);

			windowedPopupContent = new Rectangle();
			windowedPopupContent.Width = 50;
			windowedPopupContent.Height = 50;
			windowedPopupContent.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 0, 0));

			windowedPopup = MakePopup(null);
			windowedPopup.HorizontalOffset = 100;
			windowedPopup.VerticalOffset = 100;
			windowedPopup.ShouldConstrainToRootBounds = false;

			windowedPopupCanvas = (Canvas)(windowedPopup.Child);
			windowedPopupCanvas.Children.Add(inlineBluePopup);
			windowedPopupCanvas.Children.Add(windowedPopupContent);
			windowedPopupCanvas.Children.Add(inlineRedPopup);

			root = new Canvas();
			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("> Opening inline popups inside windowed popup. The one opened second should be on top (blue covers red).");
		await RunOnUIThread(() =>
		{
			{
				var xamlRoot = root.XamlRoot;
				if (xamlRoot != null)
				{
					windowedPopup.XamlRoot = xamlRoot;
				}
			}

			windowedPopup.IsOpen = true;

			// The second child opens first.
			inlineRedPopup.IsOpen = true;

			// The first child opens second. Note that this covers inlinePopup2 (i.e. blue covers red) because it opened later. Their positions in the tree has no effect.
			inlineBluePopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "Nested1"). The master pins: the main window has one popup
		// placeholder per popup (3), and the windowed popup's window (HwndLeft=90, HwndTop=121, 120x120) holds the
		// black 50x50 rectangle, then the red, then the blue 100x100 ellipse, all at the origin of the windowed popup's
		// canvas (offset 100, 100). The inline popups render inside the windowed popup's window, not in the main window.
		await VerifyNested1();

		LOG_OUTPUT("> Close inner inline popup and reopen.");
		await RunOnUIThread(() =>
		{
			inlineBluePopup.IsOpen = false;
		});
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			inlineBluePopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "Nested1")
		await VerifyNested1();

		LOG_OUTPUT("> Close outer windowed popup and reopen. The inner inline popup closed when its outer windowed popup closed and will not reopen automatically.");
		await RunOnUIThread(() =>
		{
			windowedPopup.IsOpen = false;
		});
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			windowedPopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "Nested2"). The master pins one popup placeholder and a 30x30
		// window holding only the black rectangle.
		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(inlineBluePopup.IsOpen);
			VERIFY_IS_FALSE(inlineRedPopup.IsOpen);
		});
		await VerifyOpenPopups(root.XamlRoot, windowedPopup);
		await PopupHelper.VerifyWindowedPopupRect(windowedPopup, 90, 98, 30, 30);

		LOG_OUTPUT("> Reopen inner inline popup.");
		await RunOnUIThread(() =>
		{
			inlineBluePopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "Nested3"). The master pins two popup placeholders and a
		// 120x120 window holding the black rectangle and the blue ellipse.
		await VerifyOpenPopups(root.XamlRoot, inlineBluePopup, windowedPopup);
		await PopupHelper.VerifyWindowedPopupRect(windowedPopup, 90, 98, 120, 120);
		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(inlineRedPopup.IsOpen);
		});
		VerifyBounds(await GetBoundsRelativeTo(blueEllipse, windowedPopupCanvas), 0, 0, 100, 100);

		LOG_OUTPUT("> Close outer windowed popup, reparent inner inline popup to not be nested, and reopen.");
		await RunOnUIThread(() =>
		{
			windowedPopup.IsOpen = false;
			inlineBluePopup.IsOpen = false;

			windowedPopupCanvas = (Canvas)(windowedPopup.Child);
			windowedPopupCanvas.Children.Clear();
			root.Children.Add(inlineBluePopup);

			inlineBluePopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "NotNested"). The master pins a single inline popup in the
		// main window holding the blue 100x100 ellipse at the origin.
		await VerifyOpenPopups(root.XamlRoot, inlineBluePopup);
		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(windowedPopup.IsOpen);
			VERIFY_IS_FALSE(PopupHelper.IsWindowed(inlineBluePopup));
		});
		VerifyBounds(await GetBoundsRelativeTo(blueEllipse, root), 0, 0, 100, 100);

		LOG_OUTPUT("> Close inline popup, reparent to windowed popup, and reopen.");
		await RunOnUIThread(() =>
		{
			inlineBluePopup.IsOpen = false;

			root.Children.RemoveAt(root.Children.Count - 1);
			windowedPopupCanvas.Children.Add(inlineBluePopup);
			windowedPopupCanvas.Children.Add(windowedPopupContent);
			windowedPopupCanvas.Children.Add(inlineRedPopup);

			windowedPopup.IsOpen = true;
			inlineBluePopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "Nested4"). The master pins two popup placeholders and a
		// 120x120 window holding the black rectangle and the blue ellipse.
		await VerifyOpenPopups(root.XamlRoot, inlineBluePopup, windowedPopup);
		await PopupHelper.VerifyWindowedPopupRect(windowedPopup, 90, 98, 120, 120);
		VerifyBounds(await GetBoundsRelativeTo(blueEllipse, windowedPopupCanvas), 0, 0, 100, 100);

		LOG_OUTPUT("> Reopen second inline popup. It should cover the first inline popup which opened earlier (red covers blue).");
		await RunOnUIThread(() =>
		{
			inlineRedPopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "Nested5"). The master pins three popup placeholders and a
		// 120x120 window holding the black rectangle, then the blue, then the red ellipse.
		await VerifyOpenPopups(root.XamlRoot, inlineRedPopup, inlineBluePopup, windowedPopup);
		await PopupHelper.VerifyWindowedPopupRect(windowedPopup, 90, 98, 120, 120);
		VerifyBounds(await GetBoundsRelativeTo(redEllipse, windowedPopupCanvas), 0, 0, 100, 100);
		VerifyBounds(await GetBoundsRelativeTo(blueEllipse, windowedPopupCanvas), 0, 0, 100, 100);

		LOG_OUTPUT("> Close outer windowed popup. Reopen an inner inline popup while the outer windowed popup is still closed.");
		// The inner popup will still render in this case, as an inline popup. It'll behave like a parentless popup and will
		// not inherit any transform above the Popup itself.
		await RunOnUIThread(() =>
		{
			windowedPopup.IsOpen = false;  // Implicitly closes nested popups
			inlineRedPopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "Nested6"). The master pins a single inline popup in the main
		// window holding the red 100x100 ellipse at the origin, and no windowed popup window.
		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(windowedPopup.IsOpen);
			VERIFY_IS_FALSE(inlineBluePopup.IsOpen);
			VERIFY_IS_TRUE(inlineRedPopup.IsOpen);
			VERIFY_IS_FALSE(PopupHelper.IsWindowed(inlineRedPopup));
		});
		await VerifyOpenPopups(root.XamlRoot, inlineRedPopup);
		VerifyBounds(await GetBoundsRelativeTo(redEllipse, root), 0, 0, 100, 100);

		LOG_OUTPUT("> Open the outer windowed popup while the inner non-windowed popup is open.");
		// Xaml will not attempt to reparent the nested popup inside its windowed parent. The nested popup will continue to
		// live in the main tree.
		await RunOnUIThread(() =>
		{
			windowedPopup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();
		// Upstream: VerifyMockDCompOutput(NoComparison, "Nested7"). The master pins one popup placeholder and the red
		// ellipse still in the main window (now offset by the windowed popup's canvas, at 100, 100), and a 120x120
		// window holding only the black rectangle.
		await VerifyOpenPopups(root.XamlRoot, windowedPopup, inlineRedPopup);
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(PopupHelper.IsWindowed(windowedPopup));
			VERIFY_IS_FALSE(PopupHelper.IsWindowed(inlineRedPopup));
		});
		await PopupHelper.VerifyWindowedPopupRect(windowedPopup, 90, 98, 120, 120);
		VerifyBounds(await GetBoundsRelativeTo(redEllipse, root), 100, 100, 100, 100);

		async Task VerifyNested1()
		{
			await VerifyOpenPopups(root.XamlRoot, inlineBluePopup, inlineRedPopup, windowedPopup);
			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(PopupHelper.IsWindowed(windowedPopup));
				VERIFY_IS_FALSE(PopupHelper.IsWindowed(inlineRedPopup));
				VERIFY_IS_FALSE(PopupHelper.IsWindowed(inlineBluePopup));
			});
			await PopupHelper.VerifyWindowedPopupRect(windowedPopup, 90, 98, 120, 120);
			VerifyBounds(await GetBoundsRelativeTo(windowedPopupContent, windowedPopupCanvas), 0, 0, 50, 50);
			VerifyBounds(await GetBoundsRelativeTo(redEllipse, windowedPopupCanvas), 0, 0, 100, 100);
			VerifyBounds(await GetBoundsRelativeTo(blueEllipse, windowedPopupCanvas), 0, 0, 100, 100);
		}
	}

	[TestMethod]
	public async Task InlinePopupTabOrder()
	{
		await PopupTabOrder(false);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: windowed popups are a desktop concept
	public async Task WindowedPopupTabOrder()
	{
		await PopupTabOrder(true);
	}

	private async Task PopupTabOrder(bool isWindowed)
	{
		StackPanel stackPanel = null;
		Button button1 = null;
		Popup popup = null;
		Button button2 = null;
		Button button3 = null;

		await RunOnUIThread(() =>
		{
			stackPanel = new StackPanel();
			stackPanel.HorizontalAlignment = HorizontalAlignment.Left;
			TestServices.WindowHelper.WindowContent = stackPanel;

			button1 = new Button();
			button1.Width = 200;
			button1.Height = 50;
			button1.Background = new SolidColorBrush(Colors.Red);
			stackPanel.Children.Add(button1);

			popup = new Popup();
			popup.Width = 200;
			popup.Height = 50;
			if (isWindowed)
			{
				popup.ShouldConstrainToRootBounds = false;
			}
			stackPanel.Children.Add(popup);

			button2 = new Button();
			button2.Width = 200;
			button2.Height = 50;
			button2.Background = new SolidColorBrush(Colors.Yellow);
			button2.Foreground = new SolidColorBrush(Colors.Black);
			button2.Content = isWindowed ? "Windowed" : "Inline";
			popup.Child = button2;
			popup.IsOpen = true;

			button3 = new Button();
			button3.Width = 200;
			button3.Height = 50;
			button3.Background = new SolidColorBrush(Colors.Green);
			stackPanel.Children.Add(button3);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(isWindowed, PopupHelper.IsWindowed(popup));
			button2.Focus(FocusState.Programmatic);
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("> Initial focus should be on button2.");
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button2));
		});

		LOG_OUTPUT("> Pressing Tab should move focus to button1.");
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button1));
		});

		LOG_OUTPUT("> Pressing Tab again should move focus to button3.");
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button3));
		});

		LOG_OUTPUT("> Pressing Tab should move focus to button1. This is a problem - button2 can never be focused again.");
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button1));
		});

		LOG_OUTPUT("> Pressing Tab again should move focus to button3.");
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button3));
		});

		LOG_OUTPUT("> Pressing Shift+Tab should move focus back to button1. This is a problem - button2 can never be focused again.");
		await TestServices.KeyboardHelper.ShiftTab();
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button1));
		});

		LOG_OUTPUT("> Pressing Shift+Tab once more should move focus back to button3.");
		await TestServices.KeyboardHelper.ShiftTab();
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button3));
		});

		LOG_OUTPUT("> Pressing Shift+Tab should move focus back to button1.");
		await TestServices.KeyboardHelper.ShiftTab();
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button1));
		});

		LOG_OUTPUT("> Pressing Shift+Tab once more should move focus back to button3.");
		await TestServices.KeyboardHelper.ShiftTab();
		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button3));
		});
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: windowed popups are a desktop concept
	[Ignore("Popup.SystemBackdrop isn't implemented: a DWM backdrop covers the whole HWND, shadow gutter included, and Win32 has no ContentExternalBackdropLink to fit it to the presenter.")]
	public async Task SystemBackdropRoundedCorners()
	{
		Canvas root = null;
		Popup popupBorder0 = null;
		Popup popupBorder3 = null;
		Popup popupBorderUneven = null;
		Popup popupSquareBorder0 = null;
		Popup popupSquareBorder3 = null;
		Popup popupSquareBorderUneven = null;
		Grid changingGrid = null;

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Creating popups.");
			DesktopAcrylicBackdrop backdrop = new DesktopAcrylicBackdrop();

			{
				Border border0 = new Border();
				border0.CornerRadius = new CornerRadius(8);

				Grid grid = new Grid();
				grid.Width = 150;
				grid.Height = 150;
				grid.Children.Add(border0);

				popupBorder0 = new Popup();
				popupBorder0.ShouldConstrainToRootBounds = false;
				popupBorder0.SystemBackdrop = backdrop;
				popupBorder0.Child = grid;
			}

			{
				Border border3 = new Border();
				border3.CornerRadius = new CornerRadius(8);
				border3.BorderThickness = new Thickness(3, 3, 3, 3);
				border3.BorderBrush = new SolidColorBrush(Colors.Green);

				changingGrid = new Grid();
				changingGrid.Width = 150;
				changingGrid.Height = 150;
				changingGrid.Children.Add(border3);

				popupBorder3 = new Popup();
				popupBorder3.ShouldConstrainToRootBounds = false;
				popupBorder3.SystemBackdrop = backdrop;
				popupBorder3.Child = changingGrid;
			}

			{
				Border borderUneven = new Border();
				borderUneven.CornerRadius = new CornerRadius(8);
				borderUneven.BorderThickness = new Thickness(3, 3, 3, 2);
				borderUneven.BorderBrush = new SolidColorBrush(Colors.Green);

				Grid grid = new Grid();
				grid.Width = 150;
				grid.Height = 150;
				grid.Children.Add(borderUneven);

				popupBorderUneven = new Popup();
				popupBorderUneven.ShouldConstrainToRootBounds = false;
				popupBorderUneven.SystemBackdrop = backdrop;
				popupBorderUneven.Child = grid;
			}

			{
				Border squareBorder0 = new Border();

				Grid grid = new Grid();
				grid.Width = 150;
				grid.Height = 150;
				grid.Children.Add(squareBorder0);

				popupSquareBorder0 = new Popup();
				popupSquareBorder0.ShouldConstrainToRootBounds = false;
				popupSquareBorder0.SystemBackdrop = backdrop;
				popupSquareBorder0.Child = grid;
			}

			{
				Border squareBorder3 = new Border();
				squareBorder3.BorderThickness = new Thickness(3, 3, 3, 3);
				squareBorder3.BorderBrush = new SolidColorBrush(Colors.Green);

				Grid grid = new Grid();
				grid.Width = 150;
				grid.Height = 150;
				grid.Children.Add(squareBorder3);

				popupSquareBorder3 = new Popup();
				popupSquareBorder3.ShouldConstrainToRootBounds = false;
				popupSquareBorder3.SystemBackdrop = backdrop;
				popupSquareBorder3.Child = grid;
			}

			{
				Border squareBorderUneven = new Border();
				squareBorderUneven.BorderThickness = new Thickness(3, 3, 3, 2);
				squareBorderUneven.BorderBrush = new SolidColorBrush(Colors.Green);

				Grid grid = new Grid();
				grid.Width = 150;
				grid.Height = 150;
				grid.Children.Add(squareBorderUneven);

				popupSquareBorderUneven = new Popup();
				popupSquareBorderUneven.ShouldConstrainToRootBounds = false;
				popupSquareBorderUneven.SystemBackdrop = backdrop;
				popupSquareBorderUneven.Child = grid;
			}

			root = new Canvas();
			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Opening popups.");

			popupBorder0.XamlRoot = root.XamlRoot;
			popupBorder0.IsOpen = true;

			popupBorder3.XamlRoot = root.XamlRoot;
			popupBorder3.IsOpen = true;

			popupBorderUneven.XamlRoot = root.XamlRoot;
			popupBorderUneven.IsOpen = true;

			popupSquareBorder0.XamlRoot = root.XamlRoot;
			popupSquareBorder0.IsOpen = true;

			popupSquareBorder3.XamlRoot = root.XamlRoot;
			popupSquareBorder3.IsOpen = true;

			popupSquareBorderUneven.XamlRoot = root.XamlRoot;
			popupSquareBorderUneven.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Upstream verifies, for each popup, the SystemBackdropPlacementVisual under the windowed popup's animation root:
		// - its size is the size of the popup child (150x150), and
		// - its rounded corner clip follows the child's Border: (0, 0, 150, 150) radius 8 for the rounded border
		//   without border thickness, (3, 3, 147, 147) radius 6.5 for 3px borders (radius minus half the
		//   thickness), the outer radius for uneven borders, and radius 0 for the square borders.
		// The visual and its clip are DComp only. The window of each popup, which holds the backdrop, is
		// observable and is verified below. The clip is not.
		// TODO Uno: needs a test hook for the SystemBackdropPlacementVisual size, offset and RoundedCornerClip
		var allPopups = new[]
		{
			popupBorder0,
			popupBorder3,
			popupBorderUneven,
			popupSquareBorder0,
			popupSquareBorder3,
			popupSquareBorderUneven,
		};

		await RunOnUIThread(() =>
		{
			foreach (var popup in allPopups)
			{
				VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));

				var child = (FrameworkElement)popup.Child;
				VERIFY_ARE_EQUAL(150.0, child.ActualWidth);
				VERIFY_ARE_EQUAL(150.0, child.ActualHeight);
			}
		});
		foreach (var popup in allPopups)
		{
			await PopupHelper.VerifyWindowedPopupRect(popup, -10, -2, 170, 170);
		}

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Updating a popup child's size.");
			changingGrid.Width = 300;
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("> Verifying that the backdrop size updated too.");
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(300.0, changingGrid.ActualWidth);
			VERIFY_ARE_EQUAL(150.0, changingGrid.ActualHeight);
		});
		await PopupHelper.VerifyWindowedPopupRect(popupBorder3, -10, -2, 320, 170);

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Updating a popup's position.");
			popupBorder0.VerticalOffset = 300;
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("> Verifying that the backdrop position updated too.");
		await PopupHelper.VerifyWindowedPopupRect(popupBorder0, -10, 298, 170, 170);
	}

	[TestMethod]
	[Ignore("Not applicable: pins D3D device loss recovery (wh->ResetDeviceOnly) and the DComp visual tree of the windowed popup (WindowedPopup_AnimationRootVisual, PublicRootVisual, comp node visuals). Upstream: Device lost. Don't crash. Then the popup child's visual is 150x150.")]
	public void WindowedPopupDeviceLost()
	{
		// Upstream: creates a windowed popup (ShouldConstrainToRootBounds = false) with a 150x150 green Border,
		// opens it, resets the D3D device ("Device lost. Don't crash."), then walks the DComp visual tree down to the
		// popup child's visual and verifies its size is 150x150.
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: windowed popups are a desktop concept
	public async Task WindowedPopupAncestorScale()
	{
		Canvas root = null;
		Canvas clickTarget = null;
		Popup popup = null;
		Border border = null;

		var pointerPressedRegistration = CreateSafeEventRegistration<UIElement, PointerEventHandler>(nameof(UIElement.PointerPressed));
		var pointerPressedEvent = new Event();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Creating popup.");

			border = new Border();
			border.Width = 150;
			border.Height = 150;
			border.Background = new SolidColorBrush(Colors.Green);

			pointerPressedRegistration.Attach(
				border,
				(s, args) =>
				{
					LOG_OUTPUT("> Hit Test succeeded. PointerPressed event received.");
					pointerPressedEvent.Set();
				});

			popup = new Popup();
			popup.ShouldConstrainToRootBounds = false;
			popup.Child = border;

			root = new Canvas();
			TestServices.WindowHelper.WindowContent = root;

			ScaleTransform scale = new ScaleTransform();
			scale.ScaleX = 2;
			scale.ScaleY = 0.5;

			Grid doubleGrid = new Grid();
			doubleGrid.RenderTransform = scale;
			doubleGrid.Children.Add(popup);
			root.Children.Add(doubleGrid);

			// The popup is 150x150, under 2x / 0.5x it becomes 300x75.
			// Inject the click at (250,50), which misses the untransformed popup but hits the transformed one.
			clickTarget = new Canvas();
			clickTarget.Width = 100;
			clickTarget.Height = 100;
			Canvas.SetLeft(clickTarget, 200);
			root.Children.Add(clickTarget);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Opening popup.");

			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Upstream: verifies the popup's transform matrix is a (2, 0.5) scale, and that the popup content's visual
		// is 150x150 (the scale is applied by the transform, the content size does not change).
		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(PopupHelper.IsWindowed(popup));
			VERIFY_ARE_EQUAL(150.0, border.ActualWidth);
			VERIFY_ARE_EQUAL(150.0, border.ActualHeight);
		});
		VerifyBounds(await GetBoundsRelativeTo(border, root), 0, 0, 300, 75);

		LOG_OUTPUT("> Tapping on popup");
		TestServices.InputHelper.Tap(clickTarget);

		await pointerPressedEvent.WaitForDefault();
	}

	[TestMethod]
	public async Task NoXamlRoot()
	{
		Canvas root = null;
		Popup parentlessPopup = null;
		Popup parentlessWindowedPopup = null;

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Creating popup.");

			parentlessPopup = new Popup();
			parentlessPopup.Child = new Border();

			parentlessWindowedPopup = new Popup();
			parentlessWindowedPopup.ShouldConstrainToRootBounds = false;
			parentlessWindowedPopup.Child = new Border();

			root = new Canvas();
			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Opening popup.");

			int hr = 0;

			try
			{
				parentlessPopup.IsOpen = true;
			}
			catch (Exception e)
			{
				hr = e.HResult;
			}

			VERIFY_ARE_EQUAL(hr, E_UNEXPECTED);

			try
			{
				parentlessWindowedPopup.IsOpen = true;
			}
			catch (Exception e)
			{
				hr = e.HResult;
			}

			VERIFY_ARE_EQUAL(hr, E_UNEXPECTED);
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: needs a secondary window with a XAML island
	[Description("Closes the window when it has a popup open to confirm it doesn't crash.")]
	public async Task CloseWindowWithPopupOpen()
	{
		Window window1 = null;

		TextBox tb1 = null;
		var tb1ContextMenuOpeningRegistration = CreateSafeEventRegistration<TextBox, ContextMenuOpeningEventHandler>(nameof(TextBox.ContextMenuOpening));
		var tb1ContextMenuOpeningEvent = new Event();

		try
		{
			await RunOnUIThread(() =>
			{
				var rootPanel = (StackPanel)(global::Microsoft.UI.Xaml.Markup.XamlReader.Load(
					"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
					"  <Button x:Name='btn1' Width='150' Height='50' Content='Button1'/>" +
					"  <TextBox x:Name='tb1' Width='150' Height='50' Text='Some text'/>" +
					"</StackPanel>"));
				tb1 = (TextBox)(rootPanel.FindName("tb1"));

				window1 = new Window();
				window1.Title = "Window1";
				window1.Content = rootPanel;
				window1.Activate();
			});
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Setting focus to tb1 in window1");
			await FocusTestHelper.EnsureFocus(tb1, FocusState.Pointer);
			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(tb1.XamlRoot).Equals(tb1));

				// Select the text and copy it to the clipboard. This is to ensure there is a
				// need to show the context menu both for the ability to copy and to paste.
				tb1.SelectAll();
				tb1.CopySelectionToClipboard();

				tb1ContextMenuOpeningRegistration.Attach(tb1,
					(s, e) =>
					{
						LOG_OUTPUT("TextBox ContextMenuOpening");
						tb1ContextMenuOpeningEvent.Set();
					});
			});
			await TestServices.WindowHelper.WaitForIdle();

			LOG_OUTPUT("Open the context menu using Shift+F10.");
			await TestServices.KeyboardHelper.PressKeySequence("$d$_shift#$d$_f10#$u$_f10#$u$_shift", tb1);

			await TestServices.WindowHelper.WaitForIdle();
			await tb1ContextMenuOpeningEvent.WaitForDefault();

			LOG_OUTPUT("Closing the window.");
			await RunOnUIThread(() =>
			{
				window1.Close();
				window1 = null;
			});
			await TestServices.WindowHelper.WaitForIdle();
			LOG_OUTPUT("Window closed.");
		}
		finally
		{
			if (window1 is not null)
			{
				await RunOnUIThread(() => window1.Close());
			}
		}
	}
}
