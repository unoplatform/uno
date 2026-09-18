using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ObjCRuntime;
using UIKit;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

internal static class NativeTextSelection
{
	/// <summary>
	/// Workaround for https://github.com/unoplatform/uno/issues/9430
	/// </summary>
	[DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSendSuper")]
#if NET11_0_OR_GREATER
	private static extern unsafe IntPtr IntPtr_objc_msgSendSuper(ObjCSuper* super, IntPtr selector);
#else // NET11_0_OR_GREATER
	private static extern IntPtr IntPtr_objc_msgSendSuper(IntPtr receiver, IntPtr selector);
#endif // NET11_0_OR_GREATER


	[DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSendSuper")]
#if NET11_0_OR_GREATER
	private static extern unsafe void void_objc_msgSendSuper(ObjCSuper* super, IntPtr selector, IntPtr arg);
#else // NET11_0_OR_GREATER
	private static extern void void_objc_msgSendSuper(IntPtr receiver, IntPtr selector, IntPtr arg);
#endif // NET11_0_OR_GREATER

	internal static unsafe IntPtr GetSelectedTextRange(UIView view)
	{
		var selector = Selector.GetHandle("selectedTextRange");
#if NET11_0_OR_GREATER
		var super = new ObjCSuper(view);
		return IntPtr_objc_msgSendSuper(&super, selector);
#else // NET11_0_OR_GREATER
		return IntPtr_objc_msgSendSuper(view.SuperHandle, selector);
#endif // NET11_0_OR_GREATER
	}

	internal static unsafe void SetSelectedTextRange(UIView view, IntPtr value)
	{
		var selector = Selector.GetHandle("setSelectedTextRange:");
#if NET11_0_OR_GREATER
		var super = new ObjCSuper(view);
		void_objc_msgSendSuper(&super, selector, value);
#else // NET11_0_OR_GREATER
		void_objc_msgSendSuper(view.SuperHandle, selector, value);
#endif // NET11_0_OR_GREATER
	}
}
