#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Hosting;

namespace Microsoft.UI.Xaml.Input;

public partial class FocusManager
{
	private void FocusNative(UIElement? control)
	{
		// Resign native first responder so keyboard events return to the managed layer and are properly processed by the focused element.
		if (_contentRoot.XamlRoot is { } xamlRoot)
		{
			XamlRootMap.GetHostForRoot(xamlRoot)?.ResignNativeFocus();
		}

		if (OperatingSystem.IsBrowser() && control is not null && (control as Control)?.IsDelegatingFocusToTemplateChild() != true)
		{
			NativeMethods.FocusSemanticElement(control.Visual.Handle);
		}
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.focusSemanticElement")]
		public static partial void FocusSemanticElement(IntPtr handle);
	}
}
