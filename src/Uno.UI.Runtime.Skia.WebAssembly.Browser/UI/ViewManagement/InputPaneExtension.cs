#nullable enable

using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser;

internal class BrowserInputPaneExtension : IInputPaneExtension
{
	public bool TryShow()
	{
		// The browser manages keyboard display automatically when the invisible
		// text input receives focus (via BrowserInvisibleTextBoxViewExtension).
		return false;
	}

	public bool TryHide()
	{
		// Blur the active element to dismiss the soft keyboard.
		if (NativeMethods.BlurActiveElement())
		{
			return true;
		}

		return false;
	}

	private static partial class NativeMethods
	{
		[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.Uno.UI.Runtime.Skia.InputPaneExtension.blurActiveElement")]
		public static partial bool BlurActiveElement();
	}
}
