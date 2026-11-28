#if HAS_UNO
#nullable enable

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Graphics.Display;

namespace Uno.UI.RuntimeTests.Tests.Windows_Graphics;

/// <summary>
/// The Win32 host reads <c>UNO_DISPLAY_SCALE_OVERRIDE</c> once at startup, so this test checks a process started
/// with it, and is inconclusive otherwise.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_DisplayInformation_Win32ScaleOverride
{
	private const string OverrideVariable = "UNO_DISPLAY_SCALE_OVERRIDE";

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Started_With_Override_Then_Content_Is_Drawn_At_Its_Scale()
	{
		var scale = GetOverrideOrInconclusive();

		const double width = 100, height = 50;
		var border = new Border
		{
			Width = width,
			Height = height,
			Margin = new Thickness(40, 30, 0, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
		};
		var root = new Grid
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Children = { border },
		};

		await UITestHelper.Load(root);

		var xamlRoot = border.XamlRoot!;
		var window = TestServices.WindowHelper.CurrentTestWindow;

		xamlRoot.RasterizationScale.Should().BeApproximately(scale, 1e-6);

		var displayInformation = DisplayInformation.GetOrCreateForWindowId(window.AppWindow.Id);
		displayInformation.RawPixelsPerViewPixel.Should().BeApproximately(scale, 1e-6);
		displayInformation.LogicalDpi.Should().BeApproximately((float)(96 * scale), 1e-3f);
		((int)displayInformation.ResolutionScale).Should().Be((int)Math.Round(scale * 100));

		// The client area Windows reports in pixels is Uno's logical size at the override's scale.
		var hwnd = ((Uno.UI.NativeElementHosting.Win32NativeWindow)window.NativeWindow!).Hwnd;
		GetClientRect(hwnd, out var clientRect).Should().BeTrue();
		((double)clientRect.Right).Should().BeApproximately(xamlRoot.Size.Width * scale, 1);
		((double)clientRect.Bottom).Should().BeApproximately(xamlRoot.Size.Height * scale, 1);

		// What Windows shows of the window has the Border at its logical bounds times the override, in pixels.
		var origin = border.TransformToVisual(null).TransformPoint(default);
		var (left, top) = (origin.X, origin.Y);

		int[] pixels = [];
		await TestServices.WindowHelper.WaitFor(() =>
		{
			pixels = CaptureClient(hwnd, clientRect.Right, clientRect.Bottom);
			return IsRed(PixelAt(pixels, clientRect.Right, left + width / 2, top + height / 2, scale));
		}, message: "the Border should be drawn at its logical center times the override");

		IsRed(PixelAt(pixels, clientRect.Right, left + 3, top + 3, scale)).Should().BeTrue("the Border's top-left corner is drawn there");
		IsRed(PixelAt(pixels, clientRect.Right, left + width - 3, top + height - 3, scale)).Should().BeTrue("the Border's bottom-right corner is drawn there");
		IsRed(PixelAt(pixels, clientRect.Right, left - 3, top + height / 2, scale)).Should().BeFalse("nothing red is left of the Border");
		IsRed(PixelAt(pixels, clientRect.Right, left + width + 3, top + height / 2, scale)).Should().BeFalse("nothing red is right of the Border");
		IsRed(PixelAt(pixels, clientRect.Right, left + width / 2, top + height + 3, scale)).Should().BeFalse("nothing red is below the Border");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Started_With_Override_Then_Window_Keeps_Its_Size_At_Another_Dpi()
	{
		var scale = GetOverrideOrInconclusive();

		var window = TestServices.WindowHelper.CurrentTestWindow;
		var hwnd = ((Uno.UI.NativeElementHosting.Win32NativeWindow)window.NativeWindow!).Hwnd;
		await TestServices.WindowHelper.WaitForIdle();
		GetWindowRect(hwnd, out var before).Should().BeTrue();
		var otherDpi = GetDpiForWindow(hwnd) == 96 ? 144u : 96u;

		// Windows asks this before moving the window to a monitor with another DPI, and sizes a window dragged
		// there from the answer. It passes the linearly scaled size, which the window replaces with its own.
		var size = new SIZE { cx = (before.Right - before.Left) * 3 / 2, cy = (before.Bottom - before.Top) * 3 / 2 };
		var handled = SendMessage(hwnd, WM_GETDPISCALEDSIZE, otherDpi, ref size);
		handled.Should().NotBe(0, "the window should give its size at another DPI");
		size.cx.Should().Be(before.Right - before.Left);
		size.cy.Should().Be(before.Bottom - before.Top);

		// The rectangle that comes with WM_DPICHANGED is sized and placed for that DPI: the window stays as it is.
		var suggested = new RECT
		{
			Left = before.Left + 40,
			Top = before.Top + 40,
			Right = before.Left + 40 + size.cx * 3 / 2,
			Bottom = before.Top + 40 + size.cy * 3 / 2,
		};
		SendMessage(hwnd, WM_DPICHANGED, (otherDpi << 16) | otherDpi, ref suggested);
		await TestServices.WindowHelper.WaitForIdle();
		GetWindowRect(hwnd, out var after).Should().BeTrue();
		after.Should().Be(before);
		window.Content!.XamlRoot!.RasterizationScale.Should().BeApproximately(scale, 1e-6);
	}

	private static double GetOverrideOrInconclusive()
	{
		if (!double.TryParse(Environment.GetEnvironmentVariable(OverrideVariable), NumberStyles.Float, CultureInfo.InvariantCulture, out var scale)
			|| !double.IsFinite(scale)
			|| scale <= 0)
		{
			Assert.Inconclusive($"Needs the test process started with {OverrideVariable} set to a positive number.");
		}

		return scale;
	}

	private static int PixelAt(int[] pixels, int stride, double logicalX, double logicalY, double scale)
		=> pixels[(int)(logicalY * scale) * stride + (int)(logicalX * scale)];

	// BGRA, as a 32-bit DIB stores it.
	private static bool IsRed(int pixel)
		=> ((pixel >> 16) & 0xFF) > 200 && ((pixel >> 8) & 0xFF) < 60 && (pixel & 0xFF) < 60;

	private static int[] CaptureClient(nint hwnd, int width, int height)
	{
		var info = new BITMAPINFOHEADER
		{
			biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
			biWidth = width,
			biHeight = -height, // top-down
			biPlanes = 1,
			biBitCount = 32,
		};

		var dc = CreateCompatibleDC(0);
		var bitmap = CreateDIBSection(dc, ref info, 0, out var bits, 0, 0);
		var previous = SelectObject(dc, bitmap);
		try
		{
			PrintWindow(hwnd, dc, PW_CLIENTONLY | PW_RENDERFULLCONTENT).Should().BeTrue();
			var pixels = new int[width * height];
			Marshal.Copy(bits, pixels, 0, pixels.Length);
			return pixels;
		}
		finally
		{
			SelectObject(dc, previous);
			DeleteObject(bitmap);
			DeleteDC(dc);
		}
	}

	private const uint PW_CLIENTONLY = 1;
	private const uint PW_RENDERFULLCONTENT = 2;

	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct BITMAPINFOHEADER
	{
		public uint biSize;
		public int biWidth;
		public int biHeight;
		public ushort biPlanes;
		public ushort biBitCount;
		public uint biCompression;
		public uint biSizeImage;
		public int biXPelsPerMeter;
		public int biYPelsPerMeter;
		public uint biClrUsed;
		public uint biClrImportant;
	}

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetClientRect(nint hWnd, out RECT lpRect);

	private const uint WM_DPICHANGED = 0x02E0;
	private const uint WM_GETDPISCALEDSIZE = 0x02E4;

	[StructLayout(LayoutKind.Sequential)]
	private struct SIZE
	{
		public int cx;
		public int cy;
	}

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint hwnd);

	[DllImport("user32.dll", EntryPoint = "SendMessageW")]
	private static extern nint SendMessage(nint hWnd, uint msg, nuint wParam, ref SIZE lParam);

	[DllImport("user32.dll", EntryPoint = "SendMessageW")]
	private static extern nint SendMessage(nint hWnd, uint msg, nuint wParam, ref RECT lParam);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool PrintWindow(nint hwnd, nint hdcBlt, uint nFlags);

	[DllImport("gdi32.dll")]
	private static extern nint CreateCompatibleDC(nint hdc);

	[DllImport("gdi32.dll")]
	private static extern nint CreateDIBSection(nint hdc, ref BITMAPINFOHEADER pbmi, uint usage, out nint ppvBits, nint hSection, uint offset);

	[DllImport("gdi32.dll")]
	private static extern nint SelectObject(nint hdc, nint h);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DeleteObject(nint ho);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DeleteDC(nint hdc);
}
#endif
