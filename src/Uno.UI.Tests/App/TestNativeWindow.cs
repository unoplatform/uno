#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace UnitTestsApp;

/// <summary>
/// Headless <see cref="INativeWindowWrapper"/> for the unit-test host. The unit-test
/// process has no real Skia host (Win32/X11/...) to register a native window factory,
/// so we register this minimal wrapper that provides a managed-only window backing a
/// <see cref="XamlRoot"/> / visual tree without any OS window or rendering surface.
/// </summary>
internal sealed class TestNativeWindowWrapper : NativeWindowWrapperBase
{
	public TestNativeWindowWrapper(Window window, XamlRoot xamlRoot)
		: base(window, xamlRoot)
	{
	}

	public override object? NativeWindow => null;
}

/// <summary>
/// Registers <see cref="TestNativeWindowWrapper"/> as the native window factory for the
/// unit-test host, mirroring what the real Skia runtime hosts configure on startup.
/// </summary>
internal sealed class TestNativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	public bool SupportsMultipleWindows => true;

	public bool SupportsClosingCancellation => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
		=> new TestNativeWindowWrapper(window, xamlRoot);
}
