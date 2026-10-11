#if HAS_UNO_WINUI || WINDOWS
using System;
using System.Reflection;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Windowing;

[TestClass]
[RunsOnUIThread]
public class Given_AppWindowTitleBar
{
	// MacOSNonClientPointerAction
	private const int Client = 0;
	private const int Drag = 1;

	// MacOSWindowWrapper.GetCaptionButtonsState bits
	private const int CloseShown = 1 << 0;
	private const int CloseEnabled = 1 << 1;
	private const int MinimizeShown = 1 << 2;
	private const int MinimizeEnabled = 1 << 3;
	private const int ZoomShown = 1 << 4;
	private const int ZoomEnabled = 1 << 5;
	private const int AllShown = CloseShown | MinimizeShown | ZoomShown;

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Extended_On_MacOS_Traffic_Lights_Reserve_LeftInset()
	{
		var window = new Window { Content = new Grid() };
		var titleBar = window.AppWindow.TitleBar;
		try
		{
			await ActivateAsync(window);
			titleBar.LeftInset.Should().Be(0, "the traffic lights are outside the content until it extends into the title bar");

			window.ExtendsContentIntoTitleBar = true;
			await TestServices.WindowHelper.WaitFor(() => titleBar.LeftInset > 0);

			titleBar.RightInset.Should().Be(0);
			// Physical pixels, like WinUI: the three traffic lights plus their margins, never the whole bar.
			var scale = window.Content.XamlRoot!.RasterizationScale;
			(titleBar.LeftInset / scale).Should().BeInRange(40, 120);

			window.ExtendsContentIntoTitleBar = false;
			await TestServices.WindowHelper.WaitFor(() => titleBar.LeftInset == 0);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Extended_On_MacOS_Collapsed_Height_Hides_Traffic_Lights()
	{
		var window = new Window { Content = new Grid() };
		var titleBar = window.AppWindow.TitleBar;
		try
		{
			window.ExtendsContentIntoTitleBar = true;
			await ActivateAsync(window);
			await TestServices.WindowHelper.WaitFor(() => titleBar.LeftInset > 0);

			titleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
			await TestServices.WindowHelper.WaitFor(() => titleBar.LeftInset == 0);

			titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
			await TestServices.WindowHelper.WaitFor(() => titleBar.LeftInset > 0);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Extended_Without_SetTitleBar_On_MacOS_Title_Bar_Strip_Drags()
	{
		var window = new Window { Content = new Grid() };
		var titleBar = window.AppWindow.TitleBar;
		try
		{
			window.ExtendsContentIntoTitleBar = true;
			titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
			await ActivateAsync(window);

			// WinUI keeps a default drag region the height of the title bar when SetTitleBar is not called.
			await TestServices.WindowHelper.WaitFor(() => HitTestNonClient(window, 200, 24) == Drag);
			HitTestNonClient(window, 200, 60).Should().Be(Client);

			window.ExtendsContentIntoTitleBar = false;
			await TestServices.WindowHelper.WaitFor(() => HitTestNonClient(window, 200, 24) == Client);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Extended_On_MacOS_No_Xaml_Caption_Buttons()
	{
		var window = new Window { Content = new Grid() };
		try
		{
			window.ExtendsContentIntoTitleBar = true;
			await ActivateAsync(window);
			await TestServices.WindowHelper.WaitFor(() => window.AppWindow.TitleBar.LeftInset > 0);

			// The native traffic lights are kept, so WindowChrome must not register its own Win32-style buttons.
			var source = InputNonClientPointerSource.GetForWindowId(window.AppWindow.Id);
			source.GetRegionRects(NonClientRegionKind.Close).Should().BeEmpty();
			source.GetRegionRects(NonClientRegionKind.Minimize).Should().BeEmpty();
			source.GetRegionRects(NonClientRegionKind.Maximize).Should().BeEmpty();
			AppWindowTitleBar.IsCustomizationSupported().Should().BeTrue();

#if HAS_UNO
			// WindowChrome's shared template still holds the Win32-style buttons, but they stay collapsed.
			DependencyObject element = window.Content;
			while (element is not null and not Uno.UI.Xaml.Controls.WindowChrome)
			{
				element = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
			}

			element.Should().NotBeNull();
			((Uno.UI.Xaml.Controls.WindowChrome)element).CaptionVisibility.Should().Be(Visibility.Collapsed);
#endif
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Presenter_Caption_Options_Change_On_MacOS_Native_Buttons_Are_Disabled()
	{
		var window = new Window { Content = new Grid() };
		var presenter = (OverlappedPresenter)window.AppWindow.Presenter;
		try
		{
			await ActivateAsync(window);
			GetCaptionButtonsState(window).Should().Be(AllShown | CloseEnabled | MinimizeEnabled | ZoomEnabled);

			// macOS convention: unavailable caption buttons are greyed out, not hidden.
			presenter.IsMinimizable = false;
			await TestServices.WindowHelper.WaitFor(() => GetCaptionButtonsState(window) == (AllShown | CloseEnabled | ZoomEnabled));

			presenter.IsMaximizable = false;
			await TestServices.WindowHelper.WaitFor(() => GetCaptionButtonsState(window) == (AllShown | CloseEnabled));

			presenter.IsMinimizable = true;
			presenter.IsMaximizable = true;
			await TestServices.WindowHelper.WaitFor(() => GetCaptionButtonsState(window) == (AllShown | CloseEnabled | MinimizeEnabled | ZoomEnabled));

			presenter.IsResizable = false;
			await TestServices.WindowHelper.WaitFor(() => GetCaptionButtonsState(window) == (AllShown | CloseEnabled | MinimizeEnabled));

			presenter.IsResizable = true;
			await TestServices.WindowHelper.WaitFor(() => GetCaptionButtonsState(window) == (AllShown | CloseEnabled | MinimizeEnabled | ZoomEnabled));
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_TitleBar_Hidden_On_MacOS_Traffic_Lights_Are_Hidden_And_Custom_Caption_Drags()
	{
		var content = new Grid();
		var window = new Window { Content = content };
		var presenter = (OverlappedPresenter)window.AppWindow.Presenter;
		var titleBar = window.AppWindow.TitleBar;
		try
		{
			window.ExtendsContentIntoTitleBar = true;
			await ActivateAsync(window);
			await TestServices.WindowHelper.WaitFor(() => titleBar.LeftInset > 0);

			presenter.SetBorderAndTitleBar(true, false);
			await TestServices.WindowHelper.WaitFor(() => titleBar.LeftInset == 0 && (GetCaptionButtonsState(window) & AllShown) == 0);

			// The app draws its own title bar: its Caption region drags the native window.
			var scale = content.XamlRoot!.RasterizationScale;
			var source = InputNonClientPointerSource.GetForWindowId(window.AppWindow.Id);
			source.SetRegionRects(NonClientRegionKind.Caption, [new((int)(100 * scale), 0, (int)(200 * scale), (int)(40 * scale))]);
			HitTestNonClient(window, 150, 20).Should().Be(Drag);
			HitTestNonClient(window, 50, 20).Should().Be(Client);

			presenter.SetBorderAndTitleBar(true, true);
			await TestServices.WindowHelper.WaitFor(() => titleBar.LeftInset > 0 && (GetCaptionButtonsState(window) & AllShown) == AllShown);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_TitleBar_Hidden_On_MacOS_Content_Extends_Under_It()
	{
		var content = new Grid();
		var window = new Window { Content = content };
		var presenter = (OverlappedPresenter)window.AppWindow.Presenter;
		try
		{
			await ActivateAsync(window);
			await TestServices.WindowHelper.WaitFor(() => content.ActualHeight > 0);
			var heightWithTitleBar = content.ActualHeight;

			presenter.SetBorderAndTitleBar(true, false);
			await TestServices.WindowHelper.WaitFor(() => content.ActualHeight > heightWithTitleBar);

			presenter.SetBorderAndTitleBar(true, true);
			await TestServices.WindowHelper.WaitFor(() => content.ActualHeight == heightWithTitleBar);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_TitleBar_Hidden_On_MacOS_Presenter_Commands_Work()
	{
		// What custom XAML caption buttons call once the native traffic lights are hidden.
		var window = new Window { Content = new Grid() };
		var presenter = (OverlappedPresenter)window.AppWindow.Presenter;
		try
		{
			presenter.SetBorderAndTitleBar(true, false);
			await ActivateAsync(window);

			presenter.Maximize();
			await TestServices.WindowHelper.WaitFor(() => presenter.State == OverlappedPresenterState.Maximized);
			presenter.Restore();
			await TestServices.WindowHelper.WaitFor(() => presenter.State == OverlappedPresenterState.Restored);

			presenter.Minimize();
			await TestServices.WindowHelper.WaitFor(() => presenter.State == OverlappedPresenterState.Minimized);
			// CI VMs block the main thread for seconds on the Dock animations.
			presenter.Restore();
			await TestServices.WindowHelper.WaitFor(() => presenter.State == OverlappedPresenterState.Restored, timeoutMS: 30000);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
		}

#if HAS_UNO
		var nativeWindow = window.NativeWindow!;
		window.Close();
		await TestServices.WindowHelper.WaitFor(() => GetNativeHandle(nativeWindow) == 0);
#endif
	}

	[TestMethod]
	public void When_Colors_Set_They_Are_Kept()
	{
		// Shared title bar code sets these unconditionally; they must not throw on Uno targets that don't apply them.
		var titleBar = TestServices.WindowHelper.CurrentTestWindow.AppWindow.TitleBar;
		var color = global::Windows.UI.Color.FromArgb(255, 10, 20, 30);
		try
		{
			titleBar.ButtonForegroundColor = color;
			titleBar.ButtonBackgroundColor = color;
			titleBar.ButtonHoverBackgroundColor = color;
			titleBar.ButtonInactiveForegroundColor = color;

			titleBar.ButtonForegroundColor.Should().Be(color);
			titleBar.ButtonBackgroundColor.Should().Be(color);
			titleBar.ButtonHoverBackgroundColor.Should().Be(color);
			titleBar.ButtonInactiveForegroundColor.Should().Be(color);
		}
		finally
		{
			titleBar.ButtonForegroundColor = null;
			titleBar.ButtonBackgroundColor = null;
			titleBar.ButtonHoverBackgroundColor = null;
			titleBar.ButtonInactiveForegroundColor = null;
		}
	}

	private static async Task ActivateAsync(Window window)
	{
		var activated = false;
		window.Activated += (s, e) => activated = true;
		window.Activate();
		await TestServices.WindowHelper.WaitFor(() => activated);
		await TestServices.WindowHelper.WaitForIdle();
	}

	private static int GetCaptionButtonsState(Window window)
	{
#if HAS_UNO
		var wrapper = window.AppWindow.NativeAppWindow;
		var method = wrapper.GetType().GetMethod("GetCaptionButtonsState", BindingFlags.Instance | BindingFlags.NonPublic);
		method.Should().NotBeNull();
		return (int)method!.Invoke(wrapper, [])!;
#else
		throw new NotSupportedException();
#endif
	}

	// MacOSWindowNative.Handle is reset once the NSWindow is actually closed.
	private static nint GetNativeHandle(object nativeWindow)
		=> (nint)nativeWindow.GetType().GetProperty("Handle", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(nativeWindow)!;

	// What the native left-mouse-down hit-test callback gets for a content point in logical pixels.
	internal static int HitTestNonClient(Window window, double x, double y)
	{
#if HAS_UNO
		var wrapper = window.AppWindow.NativeAppWindow;
		var method = wrapper.GetType().GetMethod("HitTestNonClient", BindingFlags.Instance | BindingFlags.NonPublic);
		method.Should().NotBeNull();
		return Convert.ToInt32(method!.Invoke(wrapper, [x, y]));
#else
		throw new NotSupportedException();
#endif
	}
}
#endif
