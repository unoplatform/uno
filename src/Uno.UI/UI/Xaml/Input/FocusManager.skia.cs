#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Input;

public partial class FocusManager
{
	/// <summary>
	/// When true, FocusNative will not call into JS.
	/// Set by FocusSynchronizer when it takes over focus management,
	/// to avoid duplicate and unresolved focusSemanticElement calls.
	/// The FocusSynchronizer handles focus sync via the GotFocus event
	/// and resolves handles to the nearest semantic DOM element first.
	/// </summary>
	internal static bool SuppressNativeFocus;

	private static void FocusNative(UIElement? control)
	{
		if (OperatingSystem.IsBrowser() && !SuppressNativeFocus && control is not null && (control as Control)?.IsDelegatingFocusToTemplateChild() != true)
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
