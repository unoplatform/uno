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
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
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
