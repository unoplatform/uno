#nullable enable

using System;
using Uno.Foundation.Extensibility;
using WinRT.Interop.Internal;

namespace WinRT.Interop;

public static partial class WindowNative
{
	public static IntPtr GetWindowHandle(object target)
		=> ApiExtensibility.CreateInstance<IWindowInteropExtension>(target).GetWindowHandle(target);
}
