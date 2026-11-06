#if HAS_UNO
#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

using Uno.UI.Xaml.Controls;
using Uno.UI.DevTools.Input;
using Windows.Foundation;
using Windows.Graphics;

using Windows.UI.Input.Preview.Injection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Input;


namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives;

// Windows have to be real: the desktop hosts have windowed popups.
[TestClass]
[RunsOnUIThread]
[RequiresFullWindow]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
public class Given_Popup_Windowed
{
	private const int ContentSize = 200;

	private static async Task<IWindowedPopupSite> WaitForWindow(Popup popup)
	{
		var site = popup.WindowedPopupSite;
		Assert.IsNotNull(site, "The popup has no window.");

		await UITestHelper.WaitFor(() => site.IsVisible, timeoutMS: 3000);

		return site;
	}

	private static Popup CreatePopup(FrameworkElement child, XamlRoot xamlRoot, double x, double y) => new()
	{
		Child = child,
		XamlRoot = xamlRoot,
		HorizontalOffset = x,
		VerticalOffset = y,
		ShouldConstrainToRootBounds = false,
	};

	private static Border CreateContent() => new()
	{
		Width = ContentSize,
		Height = ContentSize,
		Background = new SolidColorBrush(Colors.Red),
	};

	private static RectInt32 GetClientScreenRect(XamlRoot xamlRoot)
	{
		var appWindow = TestServices.WindowHelper.CurrentTestWindow!.AppWindow;
		var offset = Popup.GetScreenOffsetForTesting(xamlRoot);
		var size = appWindow.ClientSize;
		return new RectInt32(offset.X, offset.Y, size.Width, size.Height);
	}

	[TestMethod]
	public async Task When_Offset_Past_The_Window_Then_Window_Extends_Past_Owner()
	{
		var root = new Grid();
		await UITestHelper.Load(root);

		var xamlRoot = root.XamlRoot!;
		var content = CreateContent();

		// Half of it past the right edge of the window.
		var popup = CreatePopup(content, xamlRoot, xamlRoot.Size.Width - ContentSize / 2.0, 20);
		try
		{
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(content);
			var site = await WaitForWindow(popup);

			Assert.IsTrue(popup.IsWindowed);

			var client = GetClientScreenRect(xamlRoot);
			var screen = site.ScreenBounds;

			Assert.IsTrue(screen.X + screen.Width > client.X + client.Width, $"The popup's window {screen} doesn't extend past the client area {client}.");
			Assert.IsTrue(screen.X < client.X + client.Width, $"The popup's window {screen} should start inside the client area {client}.");

			// Physical pixels, and the gutters of a popup with a shadow.
			var scale = xamlRoot.RasterizationScale;
			Assert.AreEqual((ContentSize + 20) * scale, screen.Width, 1);
			Assert.AreEqual((ContentSize + 20) * scale, screen.Height, 1);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Pressed_Out_Of_The_Window_Then_Content_Is_Hit()
	{
		var root = new Grid();
		await UITestHelper.Load(root);

		var xamlRoot = root.XamlRoot!;
		var pressed = 0;
		var content = CreateContent();
		content.PointerPressed += (_, _) => pressed++;

		var popup = CreatePopup(content, xamlRoot, xamlRoot.Size.Width - ContentSize / 2.0, 20);
		try
		{
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(content);
			await WaitForWindow(popup);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available on this platform.");
			using var mouse = injector.GetMouse();

			// Past the window's right edge.
			mouse.MoveTo(new Point(xamlRoot.Size.Width + ContentSize / 4.0, 20 + ContentSize / 2.0));
			mouse.Press();
			mouse.Release();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(1, pressed);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Dragging_Slider_Then_Value_Follows(bool constrained)
	{
		var root = new Grid();
		await UITestHelper.Load(root);

		var xamlRoot = root.XamlRoot!;
		var slider = new Slider { Width = ContentSize, Minimum = 0, Maximum = 100 };
		var popup = CreatePopup(slider, xamlRoot, 40, 40);
		popup.ShouldConstrainToRootBounds = constrained;

		try
		{
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(slider);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(!constrained, popup.IsWindowed);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available on this platform.");
			using var mouse = injector.GetMouse();

			var bounds = slider.TransformToVisual(null).TransformBounds(new Rect(0, 0, slider.ActualWidth, slider.ActualHeight));
			var y = bounds.Y + bounds.Height / 2;
			mouse.MoveTo(new Point(bounds.X + 8, y));
			mouse.Press();
			mouse.MoveTo(new Point(bounds.X + bounds.Width / 2, y));
			mouse.MoveTo(new Point(bounds.Right + 50, y));
			mouse.Release();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(100, slider.Value, 1);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Past_The_Window_Then_Effective_Viewport_Is_Not_Limited_By_It()
	{
		var root = new Grid();
		await UITestHelper.Load(root);

		var xamlRoot = root.XamlRoot!;
		var inner = new Border { Width = 50, Height = 50 };
		var content = new Grid { Width = ContentSize, Height = ContentSize };
		content.Children.Add(inner);

		Rect? viewport = null;
		inner.EffectiveViewportChanged += (_, e) => viewport = e.EffectiveViewport;

		var popup = CreatePopup(content, xamlRoot, xamlRoot.Size.Width + 100, 20);
		try
		{
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(inner);
			await UITestHelper.WaitFor(() => viewport is not null);

			Assert.IsTrue(viewport!.Value.Width > 0 && viewport.Value.Height > 0, $"The viewport {viewport} shouldn't be empty past the window.");
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Pressed_Outside_Then_Light_Dismisses()
	{
		var root = new Grid() { Background = new SolidColorBrush(Colors.Blue) };
		await UITestHelper.Load(root);

		var xamlRoot = root.XamlRoot!;
		var content = CreateContent();
		var popup = CreatePopup(content, xamlRoot, xamlRoot.Size.Width - ContentSize / 2.0, 20);
		popup.IsLightDismissEnabled = true;

		try
		{
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(content);
			await WaitForWindow(popup);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available on this platform.");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(new Point(10, 300));
			mouse.Press();
			mouse.Release();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(popup.IsOpen);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_Open_Then_Keyboard_Stays_On_Owner()
	{
		var button = new Button { Content = "Owner" };
		var root = new Grid();
		root.Children.Add(button);
		await UITestHelper.Load(root);

		var xamlRoot = root.XamlRoot!;
		button.Focus(FocusState.Programmatic);

		var inner = new Button { Content = "In the popup" };
		var keyDownCount = 0;
		inner.KeyDown += (_, _) => keyDownCount++;

		var popup = CreatePopup(inner, xamlRoot, 10, 10);
		try
		{
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(inner);
			var site = await WaitForWindow(popup);

			Assert.IsFalse(site.HasFocus, "The popup's window must not take the keyboard.");

			// Focus moves inside XAML, and the owner's keyboard source keeps feeding it.
			inner.Focus(FocusState.Keyboard);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(inner, FocusManager.GetFocusedElement(xamlRoot));
			Assert.IsFalse(site.HasFocus, "The popup's window must not take the keyboard.");

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Pointer injection not available on this platform.");
			injector.InjectKeyboardInput(new[] { new InjectedInputKeyboardInfo { VirtualKey = (ushort)global::Windows.System.VirtualKey.A } });
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(1, keyDownCount);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_MenuFlyout_Then_Placed_By_Monitor_Not_By_Window()
	{
		var target = new Border { Width = 50, Height = 50, Background = new SolidColorBrush(Colors.Green), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
		var root = new Grid();
		root.Children.Add(target);
		await UITestHelper.Load(root);

		var xamlRoot = root.XamlRoot!;
		var flyout = new MenuFlyout();
		for (var i = 0; i < 3; i++)
		{
			flyout.Items.Add(new MenuFlyoutItem { Text = $"A rather long menu item number {i} to make it wide" });
		}

		TestServices.WindowHelper.SetWindowSizeOverride(new Size(420, 420));
		try
		{
			await TestServices.WindowHelper.WaitForIdle();
			root.UpdateLayout();

			// The right edge of the window: inside it, the menu can't fit.
			flyout.ShowAt(root, new FlyoutShowOptions { Position = new Point(xamlRoot.Size.Width - 5, 5) });
			await TestServices.WindowHelper.WaitForIdle();

			var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).Single();
			var presenter = (FrameworkElement)popup.Child;
			await TestServices.WindowHelper.WaitForLoaded(presenter);
			await WaitForWindow(popup);

			Assert.IsTrue(popup.IsWindowed);

			var bounds = presenter.TransformToVisual(null).TransformBounds(new Rect(0, 0, presenter.ActualWidth, presenter.ActualHeight));
			Assert.AreEqual(xamlRoot.Size.Width - 5, bounds.X, 1, "A windowed menu stays at its position when its monitor has the room.");
			Assert.IsTrue(bounds.Right > xamlRoot.Size.Width, $"A windowed menu at {bounds} should leave the {xamlRoot.Size.Width} wide window.");
		}
		finally
		{
			flyout.Hide();
			TestServices.WindowHelper.ResetWindowSizeOverride();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Owner_Moves_Then_Window_Follows()
	{
		var root = new Grid();
		await UITestHelper.Load(root);

		var xamlRoot = root.XamlRoot!;
		var content = CreateContent();
		var popup = CreatePopup(content, xamlRoot, 10, 10);
		var appWindow = TestServices.WindowHelper.CurrentTestWindow!.AppWindow;
		var originalPosition = appWindow.Position;

		TestServices.WindowHelper.SetWindowSizeOverride(new Size(420, 420));
		try
		{
			appWindow.Move(new PointInt32(50, 50));
			await TestServices.WindowHelper.WaitForIdle();

			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(content);
			var site = await WaitForWindow(popup);

			var before = site.ScreenBounds;
			appWindow.Move(new PointInt32(appWindow.Position.X + 30, appWindow.Position.Y + 20));
			await UITestHelper.WaitFor(() => site.ScreenBounds.X != before.X, timeoutMS: 3000);

			var after = site.ScreenBounds;
			Assert.AreEqual(before.X + 30, after.X);
			Assert.AreEqual(before.Y + 20, after.Y);
			Assert.IsTrue(popup.IsOpen, "Moving the owner doesn't close a windowed popup: PopupAnchor_ParentIsland.");
		}
		finally
		{
			popup.IsOpen = false;
			appWindow.Move(originalPosition);
			TestServices.WindowHelper.ResetWindowSizeOverride();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Reopened_In_Another_Window_Then_New_Window_Is_Owned_By_It()
	{
		var root = new Grid();
		await UITestHelper.Load(root);

		var content = CreateContent();
		var popup = CreatePopup(content, root.XamlRoot!, 10, 10);
		Window? secondary = null;
		try
		{
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(content);
			var firstSite = await WaitForWindow(popup);
			popup.IsOpen = false;
			await TestServices.WindowHelper.WaitForIdle();

			secondary = new Window();
			var secondaryRoot = new Grid();
			secondary.Content = secondaryRoot;
			secondary.Activate();
			await TestServices.WindowHelper.WaitForLoaded(secondaryRoot);

			popup.XamlRoot = secondaryRoot.XamlRoot;
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(content, timeoutMS: 5000);
			var secondSite = await WaitForWindow(popup);

			Assert.AreNotSame(firstSite, secondSite);
			Assert.IsTrue(firstSite.IsClosed, "The window of the previous root should be gone.");
			Assert.IsFalse(secondSite.IsClosed);
		}
		finally
		{
			popup.IsOpen = false;
			secondary?.Close();
		}
	}

	[TestMethod]
	public async Task When_Tree_Reset_Closes_Open_Flyout_Then_Other_Flyouts_Still_Open()
	{
		var secondary = new Window();
		var secondaryButton = new Button { Content = "Target" };
		secondary.Content = secondaryButton;
		secondary.Activate();
		await TestServices.WindowHelper.WaitForLoaded(secondaryButton);

		var flyout = new Flyout { Content = new Border { Width = 50, Height = 50 } };
		var closedCount = 0;
		flyout.Closed += (_, _) => closedCount++;
		var opened = false;
		flyout.Opened += (_, _) => opened = true;
		flyout.ShowAt(secondaryButton);
		await UITestHelper.WaitFor(() => opened, timeoutMS: 3000);

		// What a window closing does. Going through Close alone, the window deactivating usually light-dismisses the flyout first.
		secondaryButton.XamlRoot!.VisualTree.PopupRoot!.CloseAllPopupsForTreeReset();
		secondary.Close();
		await UITestHelper.WaitFor(() => closedCount > 0, timeoutMS: 3000);
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(1, closedCount);

		var button = new Button { Content = "Target" };
		await UITestHelper.Load(button);

		var other = new Flyout { Content = new Border { Width = 50, Height = 50 } };
		var otherOpened = false;
		other.Opened += (_, _) => otherOpened = true;
		other.ShowAt(button);
		try
		{
			await UITestHelper.WaitFor(() => otherOpened, timeoutMS: 3000);
		}
		finally
		{
			other.Hide();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Window_Creation_Fails_Then_Popup_Stays_Open_In_Window()
	{
		var real = Popup.WindowedPopupExtensionForTesting!;
		Popup.WindowedPopupExtensionForTesting = new FailingExtension(real);
		try
		{
			var target = new Button { Content = "Target" };
			await UITestHelper.Load(target);

			var content = CreateContent();
			var popup = CreatePopup(content, target.XamlRoot!, 10, 10);
			popup.PlacementTarget = target;
			popup.DesiredPlacement = PopupPlacementMode.Bottom;
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(content);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(popup.IsOpen, "The window failing must not close the popup.");
			Assert.IsNull(popup.WindowedPopupSite);
			Assert.IsFalse(popup.IsWindowed, "Placement must not make it windowed again while it's open.");

			popup.IsOpen = false;
			await TestServices.WindowHelper.WaitForIdle();
		}
		finally
		{
			Popup.WindowedPopupExtensionForTesting = real;
		}
	}

	// A host's site that fails to set up its graphics tells its content it closed, then gives up.
	private sealed class FailingExtension(IWindowedPopupExtension inner) : IWindowedPopupExtension
	{
		public bool IsRightHanded => inner.IsRightHanded;

		public bool IsSupported(XamlRoot xamlRoot) => inner.IsSupported(xamlRoot);

		public IWindowedPopupSite? TryCreateSite(XamlRoot xamlRoot, IWindowedPopupContent content)
		{
			content.OnSiteClosed();
			return null;
		}

		public PointInt32 GetScreenOffset(XamlRoot xamlRoot) => inner.GetScreenOffset(xamlRoot);

		public bool TryGetMonitorBounds(PointInt32 screenPoint, out RectInt32 monitor, out RectInt32 workArea)
			=> inner.TryGetMonitorBounds(screenPoint, out monitor, out workArea);
	}

	[TestMethod]
	public async Task When_Owner_Closes_Then_Window_Is_Destroyed()
	{
		var secondary = new Window();
		var secondaryRoot = new Grid();
		secondary.Content = secondaryRoot;
		secondary.Activate();
		await TestServices.WindowHelper.WaitForLoaded(secondaryRoot);

		var content = CreateContent();
		var popup = CreatePopup(content, secondaryRoot.XamlRoot!, 10, 10);
		popup.IsOpen = true;
		await TestServices.WindowHelper.WaitForLoaded(content);
		var site = await WaitForWindow(popup);

		secondary.Close();
		await UITestHelper.WaitFor(() => site.IsClosed, timeoutMS: 3000);

		// The popup closes with its root (CPopupRoot::CloseAllPopupsForTreeReset).
		await UITestHelper.WaitFor(() => !popup.IsOpen, timeoutMS: 3000);
	}

	[TestMethod]
	public async Task When_Closed_Then_Window_Is_Hidden_And_Reused()
	{
		var root = new Grid();
		await UITestHelper.Load(root);

		var content = CreateContent();
		var popup = CreatePopup(content, root.XamlRoot!, 10, 10);
		try
		{
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(content);
			var site = await WaitForWindow(popup);

			popup.IsOpen = false;
			Assert.IsFalse(site.IsVisible);

			popup.IsOpen = true;
			await WaitForWindow(popup);
			Assert.AreSame(site, popup.WindowedPopupSite);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_RightToLeft_Then_Window_Holds_Flipped_Content()
	{
		var root = new Grid();
		await UITestHelper.Load(root);

		var xamlRoot = root.XamlRoot!;
		var content = CreateContent();
		var popup = CreatePopup(content, xamlRoot, xamlRoot.Size.Width / 2, 10);
		popup.FlowDirection = FlowDirection.RightToLeft;

		try
		{
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(content);
			var site = await WaitForWindow(popup);

			// The origin of an RTL popup is its right edge, while a window is positioned by its left one.
			var scale = xamlRoot.RasterizationScale;
			var client = GetClientScreenRect(xamlRoot);
			var bounds = content.TransformToVisual(null).TransformBounds(new Rect(0, 0, ContentSize, ContentSize));
			Assert.AreEqual(xamlRoot.Size.Width / 2 - ContentSize, bounds.X, 1, "The content should be flipped around the popup's origin.");
			Assert.AreEqual(client.X + (bounds.X - 10) * scale, site.ScreenBounds.X, 1.5, "Left gutter");
			Assert.AreEqual((ContentSize + 20) * scale, site.ScreenBounds.Width, 1);
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	[TestMethod]
	public async Task When_ToolTip_Has_Window_With_Small_Gutters()
	{
		var target = new Button { Content = "Target", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		var toolTip = new ToolTip { Content = "ToolTip text" };
		ToolTipService.SetToolTip(target, toolTip);

		var root = new Grid();
		root.Children.Add(target);
		await UITestHelper.Load(root);

		try
		{
			toolTip.IsOpen = true;
			await UITestHelper.WaitFor(() => toolTip.Popup.IsWindowed && toolTip.Popup.WindowedPopupSite is { IsVisible: true });

			var scale = root.XamlRoot!.RasterizationScale;
			var screen = toolTip.Popup.WindowedPopupSite!.ScreenBounds;

			// MUX Reference ThemeShadow.cpp, GetInsetsForWindowedPopup: DropShadowDepthClass::Small.
			var ceilGutters = (Math.Ceiling(4 * scale) + Math.Ceiling(4 * scale)) / scale;
			Assert.AreEqual((toolTip.ActualWidth + ceilGutters) * scale, screen.Width, 2);
		}
		finally
		{
			toolTip.IsOpen = false;
		}
	}
}
#endif
