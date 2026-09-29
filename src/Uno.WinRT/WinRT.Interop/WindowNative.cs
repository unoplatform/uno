#nullable enable

using System;
using Uno.Foundation.Extensibility;

namespace WinRT.Interop;

public static partial class WindowNative
{
	public static IntPtr GetWindowHandle(object target)
		=> ApiExtensibility.CreateInstance<IWindowInteropExtension>(target).GetWindowHandle(target);
}
