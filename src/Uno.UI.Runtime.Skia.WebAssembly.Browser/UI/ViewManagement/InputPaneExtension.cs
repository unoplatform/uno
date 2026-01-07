using System;
using System.Runtime.InteropServices.JavaScript;
using Windows.UI.ViewManagement;

namespace Uno.WinUI.Runtime.Skia.WebAssembly;

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
