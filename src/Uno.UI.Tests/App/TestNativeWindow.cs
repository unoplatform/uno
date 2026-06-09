#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;

namespace UnitTestsApp;

/// <summary>
/// Headless <see cref="INativeWindowWrapper"/> for the unit-test host. The unit-test
/// process has no real Skia host (Win32/X11/...) to register a native window factory,
/// so we register this minimal wrapper that provides a managed-only window backing a
/// <see cref="XamlRoot"/> / visual tree without any OS window or rendering surface.
///
/// It still simulates a visible, sized window: the enhanced-lifecycle tree only goes
/// "live" (rooting + Loaded + ContentPresenter materialization) when the content site is
/// visible and has a size. Without that, elements never Enter the tree and templated
/// content (e.g. DataTemplate named elements) is never materialized.
/// </summary>
internal sealed class TestNativeWindowWrapper : NativeWindowWrapperBase
{
	public TestNativeWindowWrapper(Window window, XamlRoot xamlRoot)
		: base(window, xamlRoot)
	{
		var bounds = new Rect(0, 0, InitialWidth, InitialHeight);
		RasterizationScale = 1.0f;
		SetBoundsAndVisibleBounds(bounds, bounds);
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
