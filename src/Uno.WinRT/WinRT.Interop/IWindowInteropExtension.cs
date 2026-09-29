#nullable enable

using System;

namespace WinRT.Interop;

/// <summary>
/// Provided by the XAML layer, which owns <c>Window</c>, to back <see cref="WindowNative"/> and <see cref="InitializeWithWindow"/>.
/// </summary>
internal interface IWindowInteropExtension
{
	IntPtr GetWindowHandle(object target);

	void Initialize(object target, IntPtr hwnd);
}
