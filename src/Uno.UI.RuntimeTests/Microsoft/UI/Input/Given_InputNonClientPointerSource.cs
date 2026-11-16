#if HAS_UNO_WINUI || WINDOWS
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Windows.Graphics;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Input;

[TestClass]
[RunsOnUIThread]
public class Given_InputNonClientPointerSource
{
	private const uint WM_NCHITTEST = 0x0084;
	private const nint HTCLIENT = 1;
	private const nint HTCAPTION = 2;

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Passthrough_Overlaps_Caption_Passthrough_Wins()
	{
		var window = new Window();
		window.ExtendsContentIntoTitleBar = true;
		var activated = false;
		try
		{
			window.Activated += (s, e) => activated = true;
			window.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);
			await TestServices.WindowHelper.WaitForIdle();

			var source = InputNonClientPointerSource.GetForWindowId(window.AppWindow.Id);
			source.ClearAllRegionRects();

			// Caption first, then a Passthrough hole punched into it - the order SetTitleBar + TitleBar use.
			source.SetRegionRects(NonClientRegionKind.Caption, [new RectInt32(0, 0, 600, 160)]);
			source.SetRegionRects(NonClientRegionKind.Passthrough, [new RectInt32(100, 60, 150, 60)]);

			var hwnd = GetHwnd(window);
			HitTest(hwnd, 150, 90).Should().Be(HTCLIENT);
			HitTest(hwnd, 400, 90).Should().Be(HTCAPTION);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Passthrough_Overlaps_Caption_On_MacOS_Passthrough_Is_Client()
	{
		var window = new Window { Content = new Grid() };
		window.ExtendsContentIntoTitleBar = true;
		var activated = false;
		try
		{
			window.Activated += (s, e) => activated = true;
			window.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);
			await TestServices.WindowHelper.WaitForIdle();

			var source = InputNonClientPointerSource.GetForWindowId(window.AppWindow.Id);
			source.ClearAllRegionRects();

			// Region rects are physical pixels, the native hit-test callback passes logical points.
			var scale = window.Content.XamlRoot!.RasterizationScale;
			source.SetRegionRects(NonClientRegionKind.Caption, [Scale(new RectInt32(0, 0, 300, 80), scale)]);
			source.SetRegionRects(NonClientRegionKind.Passthrough, [Scale(new RectInt32(50, 30, 75, 30), scale)]);

			Microsoft_UI_Windowing.Given_AppWindowTitleBar.HitTestNonClient(window, 75, 45).Should().Be(0, "Passthrough wins over Caption");
			Microsoft_UI_Windowing.Given_AppWindowTitleBar.HitTestNonClient(window, 200, 45).Should().Be(1, "Caption drags the window");
			Microsoft_UI_Windowing.Given_AppWindowTitleBar.HitTestNonClient(window, 200, 120).Should().Be(0, "outside every region");
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	private static RectInt32 Scale(RectInt32 rect, double scale)
		=> new((int)(rect.X * scale), (int)(rect.Y * scale), (int)(rect.Width * scale), (int)(rect.Height * scale));

	private static nint GetHwnd(Window window)
	{
#if __SKIA__
		return ((Uno.UI.NativeElementHosting.Win32NativeWindow)window.NativeWindow!).Hwnd;
#else
		return WinRT.Interop.WindowNative.GetWindowHandle(window);
#endif
	}

	private static nint HitTest(nint hwnd, int clientX, int clientY)
	{
		var point = new POINT { X = clientX, Y = clientY };
		ClientToScreen(hwnd, ref point);
		var lParam = (nint)(int)(((uint)(ushort)point.Y << 16) | (ushort)point.X);
		return SendMessage(hwnd, WM_NCHITTEST, 0, lParam);
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct POINT
	{
		public int X;
		public int Y;
	}

	[DllImport("user32.dll")]
	private static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

	[DllImport("user32.dll")]
	private static extern nint SendMessage(nint hWnd, uint msg, nuint wParam, nint lParam);

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_TitleBar_Element_Is_RightToLeft_Caption_Rect_Is_Inside_Window()
	{
		var titleBar = new Border { Height = 48, Background = new SolidColorBrush(Microsoft.UI.Colors.Red) };
		var window = new Window { Content = new Grid { FlowDirection = FlowDirection.RightToLeft, Children = { titleBar } } };
		window.ExtendsContentIntoTitleBar = true;
		window.SetTitleBar(titleBar);
		var activated = false;
		try
		{
			window.Activated += (s, e) => activated = true;
			window.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated && titleBar.ActualWidth > 0);

			var source = InputNonClientPointerSource.GetForWindowId(window.AppWindow.Id);
			var expectedHeight = (int)(titleBar.ActualHeight * titleBar.XamlRoot!.RasterizationScale);
			await TestServices.WindowHelper.WaitFor(() => source.GetRegionRects(NonClientRegionKind.Caption).Any(r => r.Height == expectedHeight));

			source.GetRegionRects(NonClientRegionKind.Caption)
				.Should().OnlyContain(r => r.X >= 0 && r.X + r.Width <= window.AppWindow.ClientSize.Width);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_SetTitleBar_Without_Extending_No_Caption_Rect()
	{
		var titleBar = new Border { Height = 48, Background = new SolidColorBrush(Microsoft.UI.Colors.Red) };
		var window = new Window { Content = new Grid { Children = { titleBar } } };
		window.SetTitleBar(titleBar);
		var activated = false;
		try
		{
			window.Activated += (s, e) => activated = true;
			window.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated && titleBar.ActualWidth > 0);

			// Like WinUI, the title bar element only becomes a drag region while the content extends into the title bar.
			var source = InputNonClientPointerSource.GetForWindowId(window.AppWindow.Id);
			var expectedHeight = (int)(titleBar.ActualHeight * titleBar.XamlRoot!.RasterizationScale);
			await TestServices.WindowHelper.WaitForIdle();
			source.GetRegionRects(NonClientRegionKind.Caption).Should().BeEmpty();

			window.ExtendsContentIntoTitleBar = true;
			await TestServices.WindowHelper.WaitFor(() => source.GetRegionRects(NonClientRegionKind.Caption).Any(r => r.Height == expectedHeight));

			window.ExtendsContentIntoTitleBar = false;
			await TestServices.WindowHelper.WaitFor(() => source.GetRegionRects(NonClientRegionKind.Caption).Length == 0);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}
}
#endif
