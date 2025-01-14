using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ObjCRuntime;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

internal static class NativeTextSelection
{
	/// <summary>
	/// Workaround for https://github.com/unoplatform/uno/issues/9430
	/// </summary>
	[DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSendSuper")]
	static internal extern IntPtr IntPtr_objc_msgSendSuper(IntPtr receiver, IntPtr selector);

	[DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSendSuper")]
	static internal extern void void_objc_msgSendSuper(IntPtr receiver, IntPtr selector, IntPtr arg);

	internal static IntPtr GetSelectedTextRange(IntPtr superHandle)
	{
		return IntPtr_objc_msgSendSuper(superHandle, Selector.GetHandle("selectedTextRange"));
	}

	internal static void SetSelectedTextRange(IntPtr superHandle, IntPtr value)
	{
		void_objc_msgSendSuper(superHandle, Selector.GetHandle("setSelectedTextRange:"), value);
	}
}
