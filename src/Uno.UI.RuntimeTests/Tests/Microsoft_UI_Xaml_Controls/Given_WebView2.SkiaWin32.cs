#if __SKIA__
#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.NativeElementHosting;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml;
using Windows.Graphics.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public sealed class Given_WebView2_SkiaWin32
{
	private const string ReloadHarnessHtml = """
		<!doctype html>
		<html lang="en">
		<head>
			<meta charset="utf-8" />
			<title>Uno WebView2 Reload Harness</title>
			<style>
				html, body { margin: 0; width: 100%; height: 100%; }
				body {
					font-family: "Segoe UI", Arial, sans-serif;
					background: linear-gradient(130deg, #0B4F6C 0%, #25708A 45%, #9AD1D4 100%);
					color: #FFFFFF;
					display: flex;
					align-items: center;
					justify-content: center;
				}
				.card {
					background: rgba(0, 0, 0, 0.25);
					border-radius: 12px;
					padding: 20px 24px;
					max-width: 680px;
				}
				h1 { margin: 0 0 8px 0; font-size: 34px; font-weight: 700; }
				p { margin: 0; font-size: 20px; line-height: 1.4; }
			</style>
		</head>
		<body>
			<div class="card">
				<h1>Uno WebView2 Reload Probe</h1>
				<p id="payload">Deterministic local page for reload verification.</p>
			</div>
		</body>
		</html>
		""";
	private static readonly string DefaultArtifactsPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "artifacts", "runtime-tests", "webview2"));
	private static readonly string DomReadyScript = """
		(() => {
			const payload = document.getElementById("payload")?.textContent ?? "";
			return document.readyState === "complete"
				&& document.title === "Uno WebView2 Reload Harness"
				&& payload.includes("Deterministic local page");
		})();
		""";
	private static readonly string DomStateScript = """
		(() => ({
			readyState: document.readyState || "",
			title: document.title || "",
			textLength: ((document.body?.innerText) ?? "").replace(/\s+/g, " ").trim().length
		}))();
		""";

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_Reloaded_Rapidly_Then_WebContent_Is_Not_Blank()
	{
		await TestHelper.RetryAssert(
			async () =>
			{
				Grid? root = null;
				WebView2? webView = null;

				try
				{
					const int loadTimeoutMs = 5000;

					root = new Grid
					{
						Background = new SolidColorBrush(Microsoft.UI.Colors.Black)
					};

					webView = new WebView2
					{
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch,
						Margin = new Thickness(32)
					};

					root.Children.Add(webView);
					TestServices.WindowHelper.WindowContent = root;

					await TestServices.WindowHelper.WaitForLoaded(root, timeoutMS: loadTimeoutMs);
					await TestServices.WindowHelper.WaitForLoaded(webView, timeoutMS: loadTimeoutMs);
					await webView.EnsureCoreWebView2Async();

					var completedCount = 0;
					var lastNavigationSucceeded = false;
					webView.NavigationCompleted += (_, args) =>
					{
						completedCount++;
						lastNavigationSucceeded = args.IsSuccess;
					};

					webView.NavigateToString(ReloadHarnessHtml);
					await TestServices.WindowHelper.WaitFor(() => completedCount >= 1 && lastNavigationSucceeded, timeoutMS: 60000);
					await WaitForDomReadyAsync(webView, timeout: TimeSpan.FromSeconds(60));
					await CaptureNonBlankScreenshotAsync(webView, "before-reload", TimeSpan.FromSeconds(30));

					const int reloadCount = 40;
					for (var i = 0; i < reloadCount; i++)
					{
						webView.Reload();
						await Task.Delay(30);
					}

					await TestServices.WindowHelper.WaitFor(() => completedCount >= 2 && lastNavigationSucceeded, timeoutMS: 60000);
					await WaitForDomReadyAsync(webView, timeout: TimeSpan.FromSeconds(60));
					await CaptureNonBlankScreenshotAsync(webView, "after-reload", TimeSpan.FromSeconds(30));
				}
				finally
				{
					root?.Children.Remove(webView);
					webView?.Dispose();
					await TestServices.WindowHelper.WaitForIdle();
				}
			},
			count: 2);
	}

	[TestMethod]
	public async Task When_Disposed_Before_Load_Then_EnsureCoreWebView2Async_FailsFast()
	{
		var webView = new WebView2();
		webView.Dispose();

		await AssertEnsureCoreWebView2FailsFastAfterDisposeAsync(webView);
	}

	[TestMethod]
	public async Task When_Disposed_After_Initialization_Then_EnsureCoreWebView2Async_RemainsTerminal()
	{
		const int loadTimeoutMs = 5000;
		var initialRoot = new Grid();
		var reloadedRoot = new Grid();
		var webView = new WebView2();
		initialRoot.Children.Add(webView);

		try
		{
			TestServices.WindowHelper.WindowContent = initialRoot;
			await TestServices.WindowHelper.WaitForLoaded(initialRoot, timeoutMS: loadTimeoutMs);
			await TestServices.WindowHelper.WaitForLoaded(webView, timeoutMS: loadTimeoutMs);
			await webView.EnsureCoreWebView2Async();

			webView.Dispose();

			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();

			reloadedRoot.Children.Add(webView);
			TestServices.WindowHelper.WindowContent = reloadedRoot;
			await TestServices.WindowHelper.WaitForLoaded(reloadedRoot, timeoutMS: loadTimeoutMs);
			await TestServices.WindowHelper.WaitForLoaded(webView, timeoutMS: loadTimeoutMs);

			await AssertEnsureCoreWebView2FailsFastAfterDisposeAsync(webView);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public void When_SuccessArtifacts_Are_Not_Explicitly_Enabled_Then_OutputRoot_Is_Null()
	{
		using var _ = new EnvironmentVariableScope("UNO_WEBVIEW2_RUNTIME_TEST_ARTIFACTS", null);
		Assert.IsNull(GetSuccessfulScreenshotOutputRoot());
	}

	[TestMethod]
	public void When_SuccessArtifacts_Are_Explicitly_Enabled_Then_OutputRoot_Uses_Environment_Value()
	{
		const string outputRoot = @"C:\temp\uno-webview2-artifacts";
		using var _ = new EnvironmentVariableScope("UNO_WEBVIEW2_RUNTIME_TEST_ARTIFACTS", outputRoot);
		Assert.AreEqual(outputRoot, GetSuccessfulScreenshotOutputRoot());
	}

	private static async Task CaptureNonBlankScreenshotAsync(WebView2 webView, string phase, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;
		WebViewVisualSummary lastSummary = default;
		ScreenCaptureBuffer lastCapture = default;
		var hasCapture = false;

		while (DateTime.UtcNow < deadline)
		{
			await UITestHelper.WaitForRender(frameCount: 2, timeoutMS: 1000);

			lastCapture = CaptureClientAreaPixels();
			hasCapture = true;

			var captureRegion = GetWebViewCaptureRegion(webView, lastCapture.Width, lastCapture.Height);
			lastSummary = AnalyzeWebViewVisual(lastCapture, captureRegion);

			if (lastSummary.IsNonBlank)
			{
				var outputRoot = GetSuccessfulScreenshotOutputRoot();
				if (outputRoot is not null)
				{
					await SaveScreenshotAsync(lastCapture, phase, lastSummary, outputRoot);
				}

				return;
			}

			await Task.Delay(150);
		}

		if (hasCapture)
		{
			await SaveScreenshotAsync(lastCapture, phase + "-timeout", lastSummary, GetFailureScreenshotOutputRoot());
		}

		Assert.Fail($"WebView2 content remained blank in phase '{phase}'. Last summary: {lastSummary}");
	}

	private static async Task AssertEnsureCoreWebView2FailsFastAfterDisposeAsync(WebView2 webView)
	{
		var ensureTask = webView.EnsureCoreWebView2Async().AsTask();
		var completedTask = await Task.WhenAny(ensureTask, Task.Delay(TimeSpan.FromSeconds(5)));

		Assert.AreSame(ensureTask, completedTask, "EnsureCoreWebView2Async() should fail quickly after disposal instead of hanging.");
		try
		{
			await ensureTask;
			Assert.Fail("EnsureCoreWebView2Async() should throw ObjectDisposedException after disposal.");
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private static unsafe ScreenCaptureBuffer CaptureClientAreaPixels()
	{
		var testWindow = TestServices.WindowHelper.CurrentTestWindow;
		if (testWindow is null)
		{
			Assert.Fail("Unable to capture screenshot because CurrentTestWindow is null.");
			return default;
		}

		var hwnd = GetNativeHostWindowHandle(testWindow);
		if (hwnd == HWND.Null)
		{
			Assert.Fail("Unable to capture screenshot because the test window has no Win32 handle.");
		}

		if (!PInvoke.GetClientRect(hwnd, out var clientRect))
		{
			Assert.Fail("GetClientRect failed for the current test window.");
		}

		var width = Math.Max(1, clientRect.right - clientRect.left);
		var height = Math.Max(1, clientRect.bottom - clientRect.top);
		var pixels = new byte[width * height * 4];
		// In this project configuration, CsWin32 generates ClientToScreen(HWND, ref System.Drawing.Point).
		// Keep this explicit to match the generated interop signature and avoid marshaling/type drift.
		var captureOrigin = new Point(0, 0);
		if (!PInvoke.ClientToScreen(hwnd, ref captureOrigin))
		{
			Assert.Fail("ClientToScreen failed for the current test window.");
		}

		var sourceDc = default(HDC);
		var memoryDc = default(HDC);
		var hBitmap = default(HBITMAP);
		var oldBitmap = default(HGDIOBJ);

		try
		{
			// Capture from the desktop DC and crop to the app client area; this includes native child HWND content.
			sourceDc = PInvoke.GetDC(HWND.Null);
			if (sourceDc == default)
			{
				Assert.Fail($"{nameof(PInvoke.GetDC)} failed for the test window.");
			}

			memoryDc = PInvoke.CreateCompatibleDC(sourceDc);
			if (memoryDc == default)
			{
				Assert.Fail($"{nameof(PInvoke.CreateCompatibleDC)} failed for the test window.");
			}

			var bitmapInfo = new BITMAPINFO
			{
				bmiHeader = new BITMAPINFOHEADER
				{
					biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
					biWidth = width,
					biHeight = -height, // Negative height creates top-down bitmap rows.
					biPlanes = 1,
					biBitCount = 32,
					biCompression = 0 // BI_RGB (uncompressed)
				}
			};

			void* bits;
			hBitmap = PInvoke.CreateDIBSection(sourceDc, &bitmapInfo, DIB_USAGE.DIB_RGB_COLORS, &bits, HANDLE.Null, 0);
			if (hBitmap == default || bits is null)
			{
				Assert.Fail($"{nameof(PInvoke.CreateDIBSection)} failed for the test window.");
			}

			oldBitmap = PInvoke.SelectObject(memoryDc, (HGDIOBJ)hBitmap);
			if (oldBitmap == default)
			{
				Assert.Fail($"{nameof(PInvoke.SelectObject)} failed for the test window.");
			}

			if (!PInvoke.BitBlt(memoryDc, 0, 0, width, height, sourceDc, captureOrigin.X, captureOrigin.Y, ROP_CODE.SRCCOPY))
			{
				Assert.Fail($"{nameof(PInvoke.BitBlt)} failed for the test window.");
			}

			Marshal.Copy((IntPtr)bits, pixels, 0, pixels.Length);

			// Some device contexts leave alpha undefined; force opaque pixels for deterministic comparisons.
			for (var i = 3; i < pixels.Length; i += 4)
			{
				pixels[i] = 0xFF;
			}

			return new ScreenCaptureBuffer(width, height, pixels);
		}
		finally
		{
			if (memoryDc != default && oldBitmap != default)
			{
				PInvoke.SelectObject(memoryDc, oldBitmap);
			}

			if (hBitmap != default)
			{
				PInvoke.DeleteObject((HGDIOBJ)hBitmap);
			}

			if (memoryDc != default)
			{
				PInvoke.DeleteDC(memoryDc);
			}

			if (sourceDc != default)
			{
				PInvoke.ReleaseDC(HWND.Null, sourceDc);
			}
		}
	}

	private static HWND GetNativeHostWindowHandle(Window testWindow)
	{
		// Desktop capture must use the native host HWND. On Uno, WindowNative.GetWindowHandle()
		// returns the managed AppWindow identifier instead of a Win32 handle.
		if (testWindow.GetNativeWindow() is Win32NativeWindow nativeWindow &&
			nativeWindow.Hwnd != IntPtr.Zero)
		{
			return (HWND)nativeWindow.Hwnd;
		}

		Assert.Fail("Unable to capture screenshot because the test window does not expose a native Win32 host handle.");
		return HWND.Null;
	}

	private static Rectangle GetWebViewCaptureRegion(WebView2 webView, int pixelWidth, int pixelHeight)
	{
		var webViewBounds = webView.GetAbsoluteBounds();
		var scale = webView.XamlRoot?.RasterizationScale ?? 1d;

		var left = (int)Math.Floor(webViewBounds.X * scale);
		var top = (int)Math.Floor(webViewBounds.Y * scale);
		var width = (int)Math.Ceiling(webViewBounds.Width * scale);
		var height = (int)Math.Ceiling(webViewBounds.Height * scale);

		const int inset = 4;
		left += inset;
		top += inset;
		width = Math.Max(1, width - (2 * inset));
		height = Math.Max(1, height - (2 * inset));

		left = Math.Clamp(left, 0, Math.Max(0, pixelWidth - 1));
		top = Math.Clamp(top, 0, Math.Max(0, pixelHeight - 1));
		width = Math.Clamp(width, 1, Math.Max(1, pixelWidth - left));
		height = Math.Clamp(height, 1, Math.Max(1, pixelHeight - top));

		return new Rectangle(left, top, width, height);
	}

	private static WebViewVisualSummary AnalyzeWebViewVisual(ScreenCaptureBuffer capture, Rectangle captureRegion)
	{
		var left = captureRegion.Left;
		var top = captureRegion.Top;
		var right = captureRegion.Right - 1;
		var bottom = captureRegion.Bottom - 1;

		const int sampleColumns = 20;
		const int sampleRows = 12;

		var sampleCount = 0;
		var whiteCount = 0;
		var blackCount = 0;
		var buckets = new HashSet<int>();

		for (var row = 0; row < sampleRows; row++)
		{
			for (var col = 0; col < sampleColumns; col++)
			{
				var x = left + (int)Math.Round((right - left) * (col / (double)(sampleColumns - 1)));
				var y = top + (int)Math.Round((bottom - top) * (row / (double)(sampleRows - 1)));

				x = Math.Clamp(x, 0, capture.Width - 1);
				y = Math.Clamp(y, 0, capture.Height - 1);

				var offset = ((y * capture.Width) + x) * 4;
				var b = capture.Pixels[offset + 0];
				var g = capture.Pixels[offset + 1];
				var r = capture.Pixels[offset + 2];
				sampleCount++;

				if (r > 245 && g > 245 && b > 245)
				{
					whiteCount++;
				}

				if (r < 10 && g < 10 && b < 10)
				{
					blackCount++;
				}

				var bucket =
					((r >> 5) << 6) |
					((g >> 5) << 3) |
					(b >> 5);
				buckets.Add(bucket);
			}
		}

		var uniqueColorBucketCount = buckets.Count;
		var dominantMonochromeRatio = sampleCount == 0
			? 1d
			: Math.Max(whiteCount, blackCount) / (double)sampleCount;
		var isNonBlank = uniqueColorBucketCount >= 6 && dominantMonochromeRatio < 0.95;

		return new WebViewVisualSummary(
			sampleCount,
			uniqueColorBucketCount,
			whiteCount,
			blackCount,
			isNonBlank,
			captureRegion.Left,
			captureRegion.Top,
			captureRegion.Width,
			captureRegion.Height);
	}

	private static async Task WaitForDomReadyAsync(WebView2 webView, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;
		string? lastState = null;

		while (DateTime.UtcNow < deadline)
		{
			var isReadyJson = await webView.ExecuteScriptAsync(DomReadyScript);
			if (TryReadBooleanResult(isReadyJson, out var isReady) && isReady)
			{
				return;
			}

			lastState = await webView.ExecuteScriptAsync(DomStateScript);
			await Task.Delay(200);
		}

		Assert.Fail($"WebView2 DOM did not report a loaded page before timeout. Last script payload: {lastState ?? "<null>"}");
	}

	private static bool TryReadBooleanResult(string? scriptResult, out bool value)
	{
		value = false;
		if (string.IsNullOrWhiteSpace(scriptResult))
		{
			return false;
		}

		if (bool.TryParse(scriptResult, out value))
		{
			return true;
		}

		try
		{
			using var json = JsonDocument.Parse(scriptResult);
			switch (json.RootElement.ValueKind)
			{
				case JsonValueKind.True:
					value = true;
					return true;
				case JsonValueKind.False:
					value = false;
					return true;
				case JsonValueKind.String:
					return bool.TryParse(json.RootElement.GetString(), out value);
				default:
					return false;
			}
		}
		catch (JsonException)
		{
			return false;
		}
	}

	private static string? GetSuccessfulScreenshotOutputRoot()
	{
		var outputRoot = Environment.GetEnvironmentVariable("UNO_WEBVIEW2_RUNTIME_TEST_ARTIFACTS");
		return string.IsNullOrWhiteSpace(outputRoot) ? null : outputRoot;
	}

	private static string GetFailureScreenshotOutputRoot() =>
		GetSuccessfulScreenshotOutputRoot() ?? DefaultArtifactsPath;

	private static async Task SaveScreenshotAsync(ScreenCaptureBuffer capture, string phase, WebViewVisualSummary summary, string outputRoot)
	{
		Directory.CreateDirectory(outputRoot);

		var fileName =
			$"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{phase}-samples{summary.SampleCount}" +
			$"-unique{summary.UniqueColorBuckets}-white{summary.WhiteCount}-black{summary.BlackCount}.png";
		var fullPath = Path.Combine(outputRoot, fileName);

		using var outputFile = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
		using var randomAccessStream = outputFile.AsRandomAccessStream();
		var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, randomAccessStream);
		encoder.SetPixelData(
			BitmapPixelFormat.Bgra8,
			BitmapAlphaMode.Premultiplied,
			(uint)capture.Width,
			(uint)capture.Height,
			96d,
			96d,
			capture.Pixels);
		await encoder.FlushAsync();
		await randomAccessStream.FlushAsync();
	}

	private readonly record struct ScreenCaptureBuffer(int Width, int Height, byte[] Pixels);

	private readonly record struct WebViewVisualSummary(
		int SampleCount,
		int UniqueColorBuckets,
		int WhiteCount,
		int BlackCount,
		bool IsNonBlank,
		int CaptureRegionLeft,
		int CaptureRegionTop,
		int CaptureRegionWidth,
		int CaptureRegionHeight)
	{
		public override string ToString()
			=> $"samples={SampleCount}, uniqueBuckets={UniqueColorBuckets}, white={WhiteCount}, black={BlackCount}, nonBlank={IsNonBlank}, region=({CaptureRegionLeft},{CaptureRegionTop},{CaptureRegionWidth},{CaptureRegionHeight})";
	}

	private sealed class EnvironmentVariableScope : IDisposable
	{
		private readonly string _name;
		private readonly string? _previousValue;

		public EnvironmentVariableScope(string name, string? value)
		{
			_name = name;
			_previousValue = Environment.GetEnvironmentVariable(name);
			Environment.SetEnvironmentVariable(name, value);
		}

		public void Dispose() => Environment.SetEnvironmentVariable(_name, _previousValue);
	}
}
#endif
