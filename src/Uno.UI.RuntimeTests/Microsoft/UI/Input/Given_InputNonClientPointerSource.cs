#if HAS_UNO_WINUI || WINDOWS
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
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
}
#endif
