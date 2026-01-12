using System;
using System.Runtime.InteropServices.JavaScript;
using Windows.UI.ViewManagement;

namespace Uno.WinUI.Runtime.Skia.WebAssembly;

/// <summary>
/// WebAssembly implementation of InputPane functionality for tracking soft keyboard visibility.
/// </summary>
/// <remarks>
/// This implementation uses the Visual Viewport API to detect when the soft keyboard appears
/// on mobile browsers. When the keyboard is shown:
/// 
/// 1. The visualViewport.height decreases to reflect the visible area (excluding keyboard)
/// 2. The TypeScript side (InputPaneExtension.ts) monitors these changes
/// 3. When a significant height change is detected (>100px), it's considered a keyboard event
/// 4. The C# InputPane is updated with the occluded rectangle representing the keyboard area
/// 5. WebAssemblyWindowWrapper.ts uses visualViewport dimensions for window sizing
/// 
/// This ensures that:
/// - The app layout uses only the visible viewport area (above the keyboard)
/// - ScrollViewers can properly scroll content into view
/// - Dialogs and other UI elements fit within the available space
/// - Content is not cut off by the soft keyboard
/// 
/// The implementation is compatible with iOS Safari, Chrome on Android, and other modern mobile browsers
/// that support the Visual Viewport API. For browsers without this API, it falls back to tracking
/// window.innerHeight changes.
/// </remarks>
internal partial class InputPaneExtension : IInputPaneExtension
{
	private static InputPaneExtension? _instance;

	public InputPaneExtension()
	{
		_instance = this;
		NativeMethods.Initialize(this);
	}

	public bool TryShow()
	{
		// In browsers, the keyboard is shown automatically when an input is focused
		// We don't need to explicitly show it
		return true;
	}

	public bool TryHide()
	{
		// In browsers, we can blur the active element to hide the keyboard
		NativeMethods.HideKeyboard();
		return true;
	}

	[JSExport]
	private static void OnKeyboardVisibilityChanged(bool visible, double occludedHeight)
	{
		if (_instance is null)
		{
			return;
		}

		var inputPane = InputPane.GetForCurrentView();
		var windowWrapper = Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.Instance;

		if (visible && occludedHeight > 0)
		{
			// Calculate the occluded rect based on the keyboard height
			var bounds = windowWrapper.Bounds;
			var occludedRect = new Windows.Foundation.Rect(
				0,
				bounds.Height - occludedHeight,
				bounds.Width,
				occludedHeight
			);
			inputPane.OccludedRect = occludedRect;
		}
		else
		{
			// No occlusion when keyboard is hidden
			inputPane.OccludedRect = new Windows.Foundation.Rect(0, 0, 0, 0);
		}
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.InputPaneExtension.initialize")]
		public static partial void Initialize([JSMarshalAs<JSType.Any>] object instance);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.InputPaneExtension.hideKeyboard")]
		public static partial void HideKeyboard();
	}
}
