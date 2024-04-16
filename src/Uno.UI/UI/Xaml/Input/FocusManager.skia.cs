#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Input;

public partial class FocusManager
{
	private static void FocusNative(UIElement? control)
	{
		if (OperatingSystem.IsBrowser() && control is not null && (control as Control)?.IsDelegatingFocusToTemplateChild() != true)
		{
			NativeMethods.FocusSemanticElement(control.Visual.Handle);
		}

		internal UIElement? UnoFindNextFocusableElement(UIElement current)
		{
			var old = _focusedElement;
			_focusedElement = current;
			try
			{
				return FindNextFocusableElement(FocusNavigationDirection.Next);
			}
			finally
			{
				_focusedElement = old;
			}
		}
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.focusSemanticElement")]
		public static partial void FocusSemanticElement(IntPtr handle);
	}
}
