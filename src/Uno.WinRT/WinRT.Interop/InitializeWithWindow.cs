#nullable enable

using System;
using Uno.Foundation.Extensibility;
using WinRT.Interop.Internal;

namespace WinRT.Interop;

public static partial class InitializeWithWindow
{
	public static void Initialize(object target, IntPtr hwnd)
		=> ApiExtensibility.CreateInstance<IWindowInteropExtension>(target).Initialize(target, hwnd);
}
