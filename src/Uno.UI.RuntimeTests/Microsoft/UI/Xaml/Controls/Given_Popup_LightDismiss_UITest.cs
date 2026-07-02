#if HAS_INPUT_INJECTOR || WINAPPSDK
#nullable enable

using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Migrated from SamplesApp.UITests PopupTests.UnoSamples_Popup_LightDismiss_Tests and
// Windows_UI_Input.PointersTests.HitTest_LightDismiss: real pointer-tap hit-testing for the
// light-dismiss layer (Popup / Flyout / ComboBox dropdown).
//
// Reconstructed inline rather than loaded via RunAsync: a Popup/Flyout's Child is reparented to a
// PopupPanel under the root's PopupRoot layer (a sibling of the window content), so elements inside
// an (open) popup are NOT reachable by descending from WindowHelper.WindowContent — the App.Marked/
// App.Query shim used by RunAsync-based tests can't find them. Holding direct C# references to the
// popups and their content sidesteps that entirely, and lets each scenario use small, deliberately
// non-overlapping coordinates instead of depending on the original sample's StackPanel/font layout.
[TestClass]
[RunsOnUIThread]
public class Given_Popup_LightDismiss_UITest
{
	private const RuntimeTestPlatforms InjectionUnsupported =
		RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm;

	private static Point GetCenter(FrameworkElement element)
		=> element.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
			.TransformPoint(new Point(element.ActualWidth / 2, element.ActualHeight / 2));

	private static Popup CreateLightDismissPopup(double horizontalOffset, double verticalOffset)
		=> new()
		{
			HorizontalOffset = horizontalOffset,
			VerticalOffset = verticalOffset,
			IsLightDismissEnabled = true,
			Child = new Border
			{
				Width = 75,
				Height = 50,
				Background = new SolidColorBrush(Colors.Gray),
			},
		};

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, InjectionUnsupported)]
	public async Task When_MultiplePopups_Dismissed_ForwardOrder()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var popup1 = CreateLightDismissPopup(150, 150);
		var popup2 = CreateLightDismissPopup(250, 150);
		var popup3 = CreateLightDismissPopup(150, 250);
		var popup4 = CreateLightDismissPopup(250, 250);
		var outsideAnchor = new Border { Width = 10, Height = 10, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };

		try
		{
			TestServices.WindowHelper.WindowContent = outsideAnchor;
			await TestServices.WindowHelper.WaitForLoaded(outsideAnchor);

			var xamlRoot = TestServices.WindowHelper.XamlRoot;
			popup1.XamlRoot = popup2.XamlRoot = popup3.XamlRoot = popup4.XamlRoot = xamlRoot;

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task TapOutside()
			{
				mouse.Press(GetCenter(outsideAnchor));
				mouse.Release();
				await TestServices.WindowHelper.WaitForIdle();
			}

			Assert.IsFalse(popup1.IsOpen);
			Assert.IsFalse(popup2.IsOpen);
			Assert.IsFalse(popup3.IsOpen);
			Assert.IsFalse(popup4.IsOpen);

			// Open all 4 dismissible popups; popup4 (opened last) is topmost.
			popup1.IsOpen = true;
			popup2.IsOpen = true;
			popup3.IsOpen = true;
			popup4.IsOpen = true;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(popup1.IsOpen);
			Assert.IsTrue(popup2.IsOpen);
			Assert.IsTrue(popup3.IsOpen);
			Assert.IsTrue(popup4.IsOpen);

			// Each outside tap dismisses only the topmost (most-recently-opened) popup.
			await TapOutside();
			Assert.IsTrue(popup1.IsOpen);
			Assert.IsTrue(popup2.IsOpen);
			Assert.IsTrue(popup3.IsOpen);
			Assert.IsFalse(popup4.IsOpen);

			await TapOutside();
			Assert.IsTrue(popup1.IsOpen);
			Assert.IsTrue(popup2.IsOpen);
			Assert.IsFalse(popup3.IsOpen);
			Assert.IsFalse(popup4.IsOpen);

			await TapOutside();
			Assert.IsTrue(popup1.IsOpen);
			Assert.IsFalse(popup2.IsOpen);
			Assert.IsFalse(popup3.IsOpen);
			Assert.IsFalse(popup4.IsOpen);

			await TapOutside();
			Assert.IsFalse(popup1.IsOpen);
			Assert.IsFalse(popup2.IsOpen);
			Assert.IsFalse(popup3.IsOpen);
			Assert.IsFalse(popup4.IsOpen);
		}
		finally
		{
			popup1.IsOpen = false;
			popup2.IsOpen = false;
			popup3.IsOpen = false;
			popup4.IsOpen = false;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, InjectionUnsupported)]
	public async Task When_MultiplePopups_Dismissed_ReverseOrder()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var popup1 = CreateLightDismissPopup(150, 150);
		var popup2 = CreateLightDismissPopup(250, 150);
		var popup3 = CreateLightDismissPopup(150, 250);
		var popup4 = CreateLightDismissPopup(250, 250);
		var outsideAnchor = new Border { Width = 10, Height = 10, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };

		try
		{
			TestServices.WindowHelper.WindowContent = outsideAnchor;
			await TestServices.WindowHelper.WaitForLoaded(outsideAnchor);

			var xamlRoot = TestServices.WindowHelper.XamlRoot;
			popup1.XamlRoot = popup2.XamlRoot = popup3.XamlRoot = popup4.XamlRoot = xamlRoot;

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task TapOutside()
			{
				mouse.Press(GetCenter(outsideAnchor));
				mouse.Release();
				await TestServices.WindowHelper.WaitForIdle();
			}

			// Open in reverse order (4, 3, 2, 1); popup1 (opened last) is topmost.
			popup4.IsOpen = true;
			popup3.IsOpen = true;
			popup2.IsOpen = true;
			popup1.IsOpen = true;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(popup1.IsOpen);
			Assert.IsTrue(popup2.IsOpen);
			Assert.IsTrue(popup3.IsOpen);
			Assert.IsTrue(popup4.IsOpen);

			await TapOutside();
			Assert.IsFalse(popup1.IsOpen);
			Assert.IsTrue(popup2.IsOpen);
			Assert.IsTrue(popup3.IsOpen);
			Assert.IsTrue(popup4.IsOpen);

			await TapOutside();
			Assert.IsFalse(popup1.IsOpen);
			Assert.IsFalse(popup2.IsOpen);
			Assert.IsTrue(popup3.IsOpen);
			Assert.IsTrue(popup4.IsOpen);

			await TapOutside();
			Assert.IsFalse(popup1.IsOpen);
			Assert.IsFalse(popup2.IsOpen);
			Assert.IsFalse(popup3.IsOpen);
			Assert.IsTrue(popup4.IsOpen);

			await TapOutside();
			Assert.IsFalse(popup1.IsOpen);
			Assert.IsFalse(popup2.IsOpen);
			Assert.IsFalse(popup3.IsOpen);
			Assert.IsFalse(popup4.IsOpen);
		}
		finally
		{
			popup1.IsOpen = false;
			popup2.IsOpen = false;
			popup3.IsOpen = false;
			popup4.IsOpen = false;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, InjectionUnsupported)]
	public async Task When_MultiplePopups_WithOneNonDismissablePopup()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var popup1 = CreateLightDismissPopup(150, 150);
		var popup2 = CreateLightDismissPopup(250, 150);
		var popup3 = CreateLightDismissPopup(150, 250);
		var popup4 = CreateLightDismissPopup(250, 250);

		var closeButton = new Button { Content = "Close", Width = 120, Height = 40 };
		var popup5 = new Popup
		{
			HorizontalOffset = 150,
			VerticalOffset = 350,
			IsLightDismissEnabled = false,
			Child = closeButton,
		};
		closeButton.Click += (_, _) => popup5.IsOpen = false;

		var outsideAnchor = new Border { Width = 10, Height = 10, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };

		try
		{
			TestServices.WindowHelper.WindowContent = outsideAnchor;
			await TestServices.WindowHelper.WaitForLoaded(outsideAnchor);

			var xamlRoot = TestServices.WindowHelper.XamlRoot;
			popup1.XamlRoot = popup2.XamlRoot = popup3.XamlRoot = popup4.XamlRoot = popup5.XamlRoot = xamlRoot;

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task Tap(Point point)
			{
				mouse.Press(point);
				mouse.Release();
				await TestServices.WindowHelper.WaitForIdle();
			}

			Assert.IsFalse(popup1.IsOpen);
			Assert.IsFalse(popup5.IsOpen);

			popup1.IsOpen = true;
			popup2.IsOpen = true;
			popup3.IsOpen = true;
			popup4.IsOpen = true;
			popup5.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(closeButton);

			Assert.IsTrue(popup1.IsOpen);
			Assert.IsTrue(popup2.IsOpen);
			Assert.IsTrue(popup3.IsOpen);
			Assert.IsTrue(popup4.IsOpen);
			Assert.IsTrue(popup5.IsOpen);

			// Tapping the button inside the non-dismissable popup closes only that popup (its own Click handler).
			await Tap(GetCenter(closeButton));
			Assert.IsTrue(popup1.IsOpen);
			Assert.IsTrue(popup2.IsOpen);
			Assert.IsTrue(popup3.IsOpen);
			Assert.IsTrue(popup4.IsOpen);
			Assert.IsFalse(popup5.IsOpen);

			// The remaining dismissible popups still close one at a time from the top, even though
			// the (now closed) non-dismissable popup5 had been the topmost.
			var outsidePoint = GetCenter(outsideAnchor);

			await Tap(outsidePoint);
			Assert.IsTrue(popup1.IsOpen);
			Assert.IsTrue(popup2.IsOpen);
			Assert.IsTrue(popup3.IsOpen);
			Assert.IsFalse(popup4.IsOpen);

			await Tap(outsidePoint);
			Assert.IsTrue(popup1.IsOpen);
			Assert.IsTrue(popup2.IsOpen);
			Assert.IsFalse(popup3.IsOpen);

			await Tap(outsidePoint);
			Assert.IsTrue(popup1.IsOpen);
			Assert.IsFalse(popup2.IsOpen);

			await Tap(outsidePoint);
			Assert.IsFalse(popup1.IsOpen);
		}
		finally
		{
			popup1.IsOpen = false;
			popup2.IsOpen = false;
			popup3.IsOpen = false;
			popup4.IsOpen = false;
			popup5.IsOpen = false;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, InjectionUnsupported)]
	public async Task When_Dismissible_Popup_OutsideTap_DismissesWithoutClickThrough()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var contentPressed = false;
		var popupContent = new Border { Width = 220, Height = 140, Background = new SolidColorBrush(Colors.DarkGreen) };
		popupContent.PointerPressed += (_, _) => contentPressed = true;

		var popup = new Popup { Child = popupContent, IsLightDismissEnabled = true };

		var actionClickCount = 0;
		var actionButton = new Button { Content = "Action", Width = 150, Height = 40 };
		actionButton.Click += (_, _) => actionClickCount++;

		var canvas = new Canvas();
		Canvas.SetLeft(actionButton, 0);
		Canvas.SetTop(actionButton, 200);
		canvas.Children.Add(actionButton);

		try
		{
			TestServices.WindowHelper.WindowContent = canvas;
			await TestServices.WindowHelper.WaitForLoaded(actionButton);

			popup.XamlRoot = TestServices.WindowHelper.XamlRoot;

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task Tap(Point point)
			{
				mouse.Press(point);
				mouse.Release();
				await TestServices.WindowHelper.WaitForIdle();
			}

			Assert.IsFalse(popup.IsOpen);

			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(popupContent);
			Assert.IsTrue(popup.IsOpen);

			// Tapping the popup's own content should not dismiss it.
			await Tap(GetCenter(popupContent));
			Assert.IsTrue(contentPressed);
			Assert.IsTrue(popup.IsOpen);

			// Tapping outside the (dismissible) popup closes it, and the tap is swallowed by the
			// light-dismiss layer — the underlying button's Click never fires.
			await Tap(GetCenter(actionButton));
			Assert.IsFalse(popup.IsOpen);
			Assert.AreEqual(0, actionClickCount);
		}
		finally
		{
			popup.IsOpen = false;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, InjectionUnsupported)]
	public async Task When_Undismissible_Popup_OutsideTap_ClicksThroughWithoutDismissing()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var contentPressed = false;
		var popupContent = new Border { Width = 220, Height = 140, Background = new SolidColorBrush(Colors.DarkGreen) };
		popupContent.PointerPressed += (_, _) => contentPressed = true;

		var popup = new Popup { Child = popupContent, IsLightDismissEnabled = false };

		var actionClickCount = 0;
		var actionButton = new Button { Content = "Action", Width = 150, Height = 40 };
		actionButton.Click += (_, _) => actionClickCount++;

		var canvas = new Canvas();
		Canvas.SetLeft(actionButton, 0);
		Canvas.SetTop(actionButton, 200);
		canvas.Children.Add(actionButton);

		try
		{
			TestServices.WindowHelper.WindowContent = canvas;
			await TestServices.WindowHelper.WaitForLoaded(actionButton);

			popup.XamlRoot = TestServices.WindowHelper.XamlRoot;

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task Tap(Point point)
			{
				mouse.Press(point);
				mouse.Release();
				await TestServices.WindowHelper.WaitForIdle();
			}

			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(popupContent);
			Assert.IsTrue(popup.IsOpen);

			await Tap(GetCenter(popupContent));
			Assert.IsTrue(contentPressed);
			Assert.IsTrue(popup.IsOpen);

			// A non-dismissable popup doesn't occupy the light-dismiss hit-test layer, so the tap
			// falls through to the button underneath: its Click fires, and the popup stays open.
			await Tap(GetCenter(actionButton));
			Assert.AreEqual(1, actionClickCount);
			Assert.IsTrue(popup.IsOpen);

			// Dismiss it explicitly (mirrors the sample's "Reset" button).
			popup.IsOpen = false;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsFalse(popup.IsOpen);
		}
		finally
		{
			popup.IsOpen = false;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, InjectionUnsupported)]
	public async Task When_Flyout_ContentTap_DoesNotDismiss_TransparentAreaTap_Dismisses()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var contentPressed = false;
		var opaqueContent = new Border
		{
			Width = 200,
			Height = 60,
			Background = new SolidColorBrush(Colors.Red),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
		};
		opaqueContent.PointerPressed += (_, _) => contentPressed = true;

		// A larger, otherwise-empty (hit-test-transparent) Grid hosting the opaque content in its
		// top-left corner: tapping the uncovered area falls through to the flyout's dismiss layer.
		var flyoutRoot = new Grid { Width = 200, Height = 200, Children = { opaqueContent } };

		// Chromeless FlyoutPresenter: the default style paints an opaque background over the whole
		// presenter area, which would defeat the "hit-transparent" part of this test.
		var chromelessStyle = (Style)XamlReader.Load(
			"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
			"       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
			"       TargetType='FlyoutPresenter'>" +
			"  <Setter Property='Padding' Value='0' />" +
			"  <Setter Property='Template'>" +
			"    <Setter.Value>" +
			"      <ControlTemplate TargetType='FlyoutPresenter'>" +
			"        <ContentPresenter Content='{TemplateBinding Content}' />" +
			"      </ControlTemplate>" +
			"    </Setter.Value>" +
			"  </Setter>" +
			"</Style>");

		var flyout = new Flyout
		{
			Content = flyoutRoot,
			FlyoutPresenterStyle = chromelessStyle,
			Placement = FlyoutPlacementMode.Bottom,
		};

		var owner = new Button { Content = "Owner", Width = 100, Height = 40 };

		try
		{
			TestServices.WindowHelper.WindowContent = owner;
			await TestServices.WindowHelper.WaitForLoaded(owner);

			flyout.ShowAt(owner);
			await TestServices.WindowHelper.WaitForLoaded(flyoutRoot);
			Assert.IsTrue(flyout.IsOpen);

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task Tap(Point point)
			{
				mouse.Press(point);
				mouse.Release();
				await TestServices.WindowHelper.WaitForIdle();
			}

			// Tapping the flyout's own (opaque) content should not dismiss it.
			await Tap(GetCenter(opaqueContent));
			Assert.IsTrue(contentPressed);
			Assert.IsTrue(flyout.IsOpen);

			// Tapping the flyout's hit-test-transparent area (bottom-right corner, uncovered by
			// opaqueContent) falls through to the light-dismiss layer and closes it.
			var transparentPoint = flyoutRoot.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
				.TransformPoint(new Point(flyoutRoot.ActualWidth - 10, flyoutRoot.ActualHeight - 10));
			await Tap(transparentPoint);
			Assert.IsFalse(flyout.IsOpen);
		}
		finally
		{
			flyout.Hide();
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, InjectionUnsupported)]
	public async Task When_ComboBox_OutsideTap_ClosesDropdownWithoutClickThrough()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		var comboBox = new ComboBox { ItemsSource = new[] { "One", "Two", "Three" }, Width = 150 };

		var actionClickCount = 0;
		var actionButton = new Button { Content = "Action", Width = 150, Height = 40 };
		actionButton.Click += (_, _) => actionClickCount++;

		var canvas = new Canvas();
		Canvas.SetLeft(comboBox, 0);
		Canvas.SetTop(comboBox, 0);
		Canvas.SetLeft(actionButton, 0);
		Canvas.SetTop(actionButton, 200);
		canvas.Children.Add(comboBox);
		canvas.Children.Add(actionButton);

		try
		{
			TestServices.WindowHelper.WindowContent = canvas;
			await TestServices.WindowHelper.WaitForLoaded(comboBox);
			await TestServices.WindowHelper.WaitForLoaded(actionButton);

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task Tap(FrameworkElement element)
			{
				mouse.Press(GetCenter(element));
				mouse.Release();
				await TestServices.WindowHelper.WaitForIdle();
			}

			Assert.IsFalse(comboBox.IsDropDownOpen);

			await Tap(comboBox);
			Assert.IsTrue(comboBox.IsDropDownOpen);

			// Tapping outside the dropdown dismisses it (the ComboBox's dropdown popup is
			// light-dismiss-enabled), swallowing the tap so the underlying button's Click never fires.
			await Tap(actionButton);
			Assert.IsFalse(comboBox.IsDropDownOpen);
			Assert.AreEqual(0, actionClickCount);
		}
		finally
		{
			comboBox.IsDropDownOpen = false;
			TestServices.WindowHelper.WindowContent = null;
		}
	}
}
#endif
