#nullable enable

using System;
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
	internal static void ForceLoaded(this FrameworkElement? element)
	{
		if (element is null)
		{
			return;
		}

		// Rooting the element below requires the test App (its HostView is the live tree).
		// The App is a process-wide singleton created lazily by EnsureApplication(); many
		// test classes call it in setup, but some (e.g. Given_Grid, Given_Binding) rely on it
		// already existing. Test execution order is not stable across runners (dotnet test vs
		// Visual Studio), so a class that runs before any EnsureApplication caller would
		// otherwise find no App, skip the rooting below, and silently fail to materialize its
		// templated/bound content -- making those tests order-dependent. Create it here when
		// absent. Guarded on null so repeated ForceLoaded calls within a single test don't
		// trip EnsureApplication's HostView reset and unload already-loaded siblings.
		if (Application.Current is not UnitTestsApp.App)
		{
			UnitTestsApp.App.EnsureApplication();
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

		// Entering the live tree queues a loaded event "on the next tick" (normally pumped by
		// the render loop, which the unit-test host doesn't have). Flush it synchronously so the
		// pending tick does not leak into a later test and fire while that test's tree is active,
		// which otherwise makes loaded/binding/materialization behavior order-dependent.
		var eventManager = global::Uno.UI.Xaml.Core.CoreServices.Instance.EventManager;
		if (eventManager.ShouldRaiseLoadedEvent)
		{
			eventManager.RaiseLoadedEvent();
		}
	}

	// Fail fast if these private members are renamed/removed: a silent null would skip the
	// loading phase (and the m_firedLoadingEvent guard) while still marking elements loaded,
	// producing misleading passes instead of a loud breakage.
	private static readonly MethodInfo s_onFwEltLoading = typeof(FrameworkElement)
		.GetMethod("OnFwEltLoading", BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new MissingMethodException(nameof(FrameworkElement), "OnFwEltLoading");

	private static readonly FieldInfo s_firedLoadingEvent = typeof(FrameworkElement)
		.GetField("m_firedLoadingEvent", BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new MissingFieldException(nameof(FrameworkElement), "m_firedLoadingEvent");

	private static void ForceLoadedRecursive(UIElement element)
	{
		// FrameworkElement.OnFwEltLoading wraps the OnLoading + OnLoadingPartial pair
		// and raises the Loading event. RaiseLoaded then drives OnFwEltLoaded and
		// updates IsLoaded. Together they reproduce what the legacy mock did via the
		// ad-hoc EnterTree() helper.
		if (element is FrameworkElement fe)
		{
			s_onFwEltLoading.Invoke(fe, null);

			// WinUI raises Loading exactly once (RaiseLoadingEventIfNeeded sets m_firedLoadingEvent).
			// We raise it directly here, bypassing the measure pass, so set the guard too --
			// otherwise a later real measure pass (e.g. a layout manager leaked from another test in
			// the full suite) re-raises Loading and re-runs Bindings.Update(), double-evaluating
			// x:Bind functions.
			s_firedLoadingEvent.SetValue(fe, true);
		}

		// Under the enhanced Skia lifecycle a Control's template is applied during the measure
		// pass, which ForceLoaded intentionally does not run. Apply it explicitly so templated
		// content (ContentPresenter children, x:Bind targets, named elements) materializes and
		// can be loaded, without populating layout-derived values. EnsureTemplate is idempotent.
		if (element is Microsoft.UI.Xaml.Controls.Control control)
		{
			control.EnsureTemplate();
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
