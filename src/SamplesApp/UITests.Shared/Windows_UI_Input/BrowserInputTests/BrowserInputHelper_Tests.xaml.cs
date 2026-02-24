using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Input.BrowserInputTests
{
#if __SKIA__
	[Sample("Keyboard", Name = "BrowserInputHelper",
		Description = "Manual tests for BrowserInputHelper: browser zoom toggle and keyboard lock API (WASM Skia only).",
		IsManualTest = true,
		IgnoreInSnapshotTests = true)]
#endif
	public sealed partial class BrowserInputHelper_Tests : Page
	{
#if __SKIA__
		private static readonly Type _helperType = Type.GetType(
			"Uno.UI.Runtime.Skia.BrowserInputHelper, Uno.WinUI.Runtime.Skia.WebAssembly.Browser");

		private static readonly PropertyInfo _zoomProperty = _helperType?.GetProperty(
			"IsBrowserZoomEnabled", BindingFlags.Public | BindingFlags.Static);

		private static readonly MethodInfo _lockMethod = _helperType?.GetMethod(
			"LockKeysAsync", BindingFlags.Public | BindingFlags.Static);

		private static readonly MethodInfo _unlockMethod = _helperType?.GetMethod(
			"UnlockKeys", BindingFlags.Public | BindingFlags.Static);

		private static readonly PropertyInfo _lockSupportedProperty = _helperType?.GetProperty(
			"IsKeyboardLockSupported", BindingFlags.Public | BindingFlags.Static);
#endif

		public BrowserInputHelper_Tests()
		{
			this.InitializeComponent();
#if __SKIA__
			if (_helperType is null)
			{
				PlatformStatus.Text = "BrowserInputHelper not available. This sample only works on WASM Skia (Uno.WinUI.Runtime.Skia.WebAssembly.Browser).";
			}
			else
			{
				var lockSupported = _lockSupportedProperty?.GetValue(null) as bool? ?? false;
				PlatformStatus.Text = lockSupported
					? "BrowserInputHelper loaded. Keyboard Lock API is supported."
					: "BrowserInputHelper loaded. Keyboard Lock API is NOT supported (requires HTTPS + Chromium).";
			}

			KeyDown += OnKeyDown;
			KeyUp += OnKeyUp;
#endif
		}

#if __SKIA__
		private void OnKeyDown(object sender, KeyRoutedEventArgs e)
		{
			AppendLog($"[KeyDown] Key={e.Key} OriginalKey={e.OriginalKey}");
		}

		private void OnKeyUp(object sender, KeyRoutedEventArgs e)
		{
			AppendLog($"[KeyUp] Key={e.Key} OriginalKey={e.OriginalKey}");
		}

		private void AppendLog(string message)
		{
			var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
			EventLog.Text += $"[{timestamp}] {message}\n";

			// Auto-scroll to bottom
			LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
		}

		// --- Browser Zoom ---

		private void DisableZoom_Click(object sender, RoutedEventArgs e)
		{
			if (_zoomProperty is null)
			{
				AppendLog("ERROR: IsBrowserZoomEnabled property not found.");
				return;
			}

			_zoomProperty.SetValue(null, false);
			ZoomStatus.Text = "Zoom: DISABLED";
			AppendLog("Browser zoom disabled. Ctrl+Wheel should NOT zoom the page.");
		}

		private void EnableZoom_Click(object sender, RoutedEventArgs e)
		{
			if (_zoomProperty is null)
			{
				AppendLog("ERROR: IsBrowserZoomEnabled property not found.");
				return;
			}

			_zoomProperty.SetValue(null, true);
			ZoomStatus.Text = "Zoom: enabled";
			AppendLog("Browser zoom enabled. Ctrl+Wheel should zoom the page.");
		}

		// --- Keyboard Lock - Presets ---

		private async void LockEscape_Click(object sender, RoutedEventArgs e)
		{
			await LockKeysAsync("Escape");
			LockStatus.Text = "Lock: Escape";
			AppendLog("Locked: Escape. Press Escape — it should reach the app, not exit fullscreen.");
		}

		private async void LockFKeys_Click(object sender, RoutedEventArgs e)
		{
			await LockKeysAsync("F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12");
			LockStatus.Text = "Lock: F1-F12";
			AppendLog("Locked: F1-F12. Function keys should reach the app, not trigger browser actions.");
		}

		private async void LockAllKeys_Click(object sender, RoutedEventArgs e)
		{
			await LockKeysAsync();
			LockStatus.Text = "Lock: ALL keys";
			AppendLog("Locked: ALL keys. All system keys should reach the app.");
		}

		private void UnlockKeys_Click(object sender, RoutedEventArgs e)
		{
			if (_unlockMethod is null)
			{
				AppendLog("ERROR: UnlockKeys method not found.");
				return;
			}

			_unlockMethod.Invoke(null, null);
			LockStatus.Text = "Lock: none";
			AppendLog("Unlocked all keys. Default browser key handling restored.");
		}

		// --- Keyboard Lock - Custom ---

		private async void LockCustomKeys_Click(object sender, RoutedEventArgs e)
		{
			var text = CustomKeysInput.Text?.Trim();
			if (string.IsNullOrEmpty(text))
			{
				AppendLog("Enter at least one key code (e.g. KeyW,F5,Escape).");
				return;
			}

			var keys = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			await LockKeysAsync(keys);
			LockStatus.Text = $"Lock: {string.Join(", ", keys)}";
			AppendLog($"Locked: {string.Join(", ", keys)}");
		}

		// --- Log ---

		private void ClearLog_Click(object sender, RoutedEventArgs e)
		{
			EventLog.Text = string.Empty;
		}

		// --- Helpers ---

		private async Task LockKeysAsync(params string[] keyCodes)
		{
			if (_lockMethod is null)
			{
				AppendLog("ERROR: LockKeysAsync method not found.");
				return;
			}

			var lockSupported = _lockSupportedProperty?.GetValue(null) as bool? ?? false;
			AppendLog($"IsKeyboardLockSupported: {lockSupported}");

			try
			{
				var task = (Task)_lockMethod.Invoke(null, new object[] { keyCodes });
				await task;
			}
			catch (Exception ex)
			{
				AppendLog($"LockKeysAsync failed: {ex.InnerException?.Message ?? ex.Message}");
			}
		}
#else
		private void DisableZoom_Click(object sender, RoutedEventArgs e) { }
		private void EnableZoom_Click(object sender, RoutedEventArgs e) { }
		private void LockEscape_Click(object sender, RoutedEventArgs e) { }
		private void LockFKeys_Click(object sender, RoutedEventArgs e) { }
		private void LockAllKeys_Click(object sender, RoutedEventArgs e) { }
		private void UnlockKeys_Click(object sender, RoutedEventArgs e) { }
		private void LockCustomKeys_Click(object sender, RoutedEventArgs e) { }
		private void ClearLog_Click(object sender, RoutedEventArgs e) { }
#endif
	}
}
