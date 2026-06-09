using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Uno.UI.Tests.Helpers;

/// <summary>
/// Test helpers that replace the in-memory mock APIs that lived in
/// FrameworkElement.unittests.cs / Window.unittests.cs before #17399. They use
/// only InternalsVisibleTo from Uno.UI to drive the real Skia objects rather
/// than fake substitutes.
/// </summary>
internal static class TestExtensions
{
	/// <summary>
	/// Replicates the legacy mock `FrameworkElement.ForceLoaded()`: marks the
	/// element and all descendants as loaded by directly raising Loaded on
	/// each, since the unit-test process has no real window or render loop to
	/// drive that lifecycle.
	/// </summary>
	internal static void ForceLoaded(this FrameworkElement element)
	{
		if (element is null)
		{
			return;
		}

		// On real Skia (UNO_HAS_ENHANCED_LIFECYCLE), entering the live visual tree is what
		// materializes ContentPresenter/Control template content and applies x:Bind once.
		// The test host runs a simulated visible window (see TestNativeWindowWrapper), so
		// rooting the element in HostView makes it Enter the live tree -- materializing
		// templated content and its named elements -- without a measure pass that would
		// re-apply bindings or populate layout state.
		var hostView = (Application.Current as UnitTestsApp.App)?.HostView;
		if (hostView is not null && element.Parent is null && element != hostView)
		{
			hostView.Children.Add(element);
		}

		ForceLoadedRecursive(element);
	}

	private static readonly MethodInfo s_onFwEltLoading = typeof(FrameworkElement)
		.GetMethod("OnFwEltLoading", BindingFlags.Instance | BindingFlags.NonPublic);

	private static void ForceLoadedRecursive(UIElement element)
	{
		// FrameworkElement.OnFwEltLoading wraps the OnLoading + OnLoadingPartial pair
		// and raises the Loading event. RaiseLoaded then drives OnFwEltLoaded and
		// updates IsLoaded. Together they reproduce what the legacy mock did via the
		// ad-hoc EnterTree() helper.
		if (element is FrameworkElement fe)
		{
			s_onFwEltLoading?.Invoke(fe, null);
		}

		element.RaiseLoaded();

		var count = VisualTreeHelper.GetChildrenCount(element);
		for (var i = 0; i < count; i++)
		{
			if (VisualTreeHelper.GetChild(element, i) is UIElement child)
			{
				ForceLoadedRecursive(child);
			}
		}
	}

	/// <summary>
	/// Replicates the legacy mock `Window.SetWindowSize`: sets the visible
	/// bounds on the visual tree and invalidates layout, which is what the
	/// adaptive-trigger / size-change tests need.
	/// </summary>
	internal static void SetWindowSize(this Window window, Size size)
	{
		// Drive the simulated window's actual size: XamlRoot.Bounds (read by AdaptiveTrigger
		// and others) is backed by the window/XamlIsland size, not by VisibleBoundsOverride.
		// Updating the wrapper bounds raises SizeChanged -> XamlRoot.Changed so size-dependent
		// triggers re-evaluate (matches the legacy mock's RaiseNativeSizeChanged path).
		var bounds = new Rect(0, 0, size.Width, size.Height);
		if (window.NativeWrapper is Uno.UI.Xaml.Controls.NativeWindowWrapperBase nativeWrapper)
		{
			nativeWrapper.SetBoundsAndVisibleBounds(bounds, bounds);
		}

		var xamlRoot = window.Content?.XamlRoot ?? Window.InitialWindow?.RootElement?.XamlRoot;
		if (xamlRoot?.VisualTree is { } visualTree)
		{
			visualTree.VisibleBoundsOverride = bounds;
		}

		if (window.Content is FrameworkElement root)
		{
			root.InvalidateMeasure();
			root.InvalidateArrange();
			root.UpdateLayout();
		}
	}
}
