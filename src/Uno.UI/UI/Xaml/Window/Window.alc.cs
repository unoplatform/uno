#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.UI.Xaml.Controls;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml;

partial class Window
{
	private static readonly ConditionalWeakTable<object, SecondaryAlcMarker> _secondaryAlcMarkers = new();

	private bool _isWindowFromSecondaryAlc;

	// Window-local storage to detect secondary ALC content
	private object? _secondaryAlcContent;

	// Lazily allocated ALC window state - null when not in ALC mode
	private AlcWindowState? _alcState;

	partial void InitializeAlcState()
		=> _isWindowFromSecondaryAlc = IsAssemblyFromSecondaryAlc(GetType().Assembly);

	/// <summary>
	/// Encapsulates all ALC window lifecycle state to avoid memory overhead when ALC is not used.
	/// </summary>
	private sealed class AlcWindowState
	{
		public bool IsClosed;
		public bool IsVisible;

		// Subscription handlers for cleanup
		public SizeChangedEventHandler? HostSizeChangedHandler;
		public RoutedEventHandler? HostLoadedHandler;
		public RoutedEventHandler? HostUnloadedHandler;

	}

	private partial bool TryGetContentFromSecondaryAlc(out UIElement? content)
	{
		if (IsSecondaryAlcContent(_secondaryAlcContent))
		{
			content = ContentHostOverride?.Content as UIElement;
			return true;
		}

		content = default;
		return false;
	}

	private partial bool TrySetContentFromSecondaryAlc(UIElement? value, ContentControl host, Assembly callingAssembly)
	{
		if (!ShouldRedirectToContentHost(value, callingAssembly))
		{
			return false;
		}

#if DEBUG
		if (callingAssembly.FullName?.StartsWith("System.", StringComparison.Ordinal) == true)
		{
			System.Diagnostics.Debug.WriteLine("Window.Content was set via reflection or framework code; secondary ALC detection may be inaccurate.");
		}
#endif

		InitializeAlcWindowMode();

		UnmarkSecondaryAlcContent(_secondaryAlcContent);
		host.Content = value;
		_secondaryAlcContent = value;
		MarkContentAsSecondaryAlc(value);
		return true;
	}

	/// <summary>
	/// Initialize ALC mode when content is first set from a secondary ALC.
	/// </summary>
	private void InitializeAlcWindowMode()
	{
		if (_alcState is not null)
		{
			return;
		}

		_alcState = new AlcWindowState();
		ApplicationHelper.RemoveWindow(this); // Don't block app closure

		// Close the native window if it was created - ALC windows don't need a native window
		// since they render into ContentHostOverride. This also unregisters from platform
		// window tracking (e.g., X11XamlRootHost) so the app can exit when the main window closes.
		_windowImplementation.NativeWindowWrapper?.Close();

		SubscribeToContentHostEvents();
	}

	/// <summary>
	/// Subscribe to ContentHostOverride for event forwarding.
	/// </summary>
	private void SubscribeToContentHostEvents()
	{
		var host = ContentHostOverride;
		var state = _alcState;
		if (host is null || state is null)
		{
			return;
		}

		state.HostSizeChangedHandler = OnContentHostSizeChanged;
		host.SizeChanged += state.HostSizeChangedHandler;

		state.HostLoadedHandler = OnContentHostLoaded;
		state.HostUnloadedHandler = OnContentHostUnloaded;
		host.Loaded += state.HostLoadedHandler;
		host.Unloaded += state.HostUnloadedHandler;

		state.IsVisible = host.IsLoaded;
	}

	/// <summary>
	/// Forward SizeChanged from host.
	/// </summary>
	private void OnContentHostSizeChanged(object sender, SizeChangedEventArgs e)
	{
		var state = _alcState;
		if (state is null || state.IsClosed)
		{
			return;
		}

		_windowImplementation.RaiseSizeChanged(new WindowSizeChangedEventArgs(e.NewSize));
	}

	/// <summary>
	/// Forward visibility from host Loaded event.
	/// </summary>
	private void OnContentHostLoaded(object sender, RoutedEventArgs e)
	{
		var state = _alcState;
		if (state is null || state.IsClosed || state.IsVisible)
		{
			return;
		}

		state.IsVisible = true;
		_windowImplementation.RaiseVisibilityChanged(new VisibilityChangedEventArgs { Visible = true });
	}

	/// <summary>
	/// Forward visibility from host Unloaded event.
	/// </summary>
	private void OnContentHostUnloaded(object sender, RoutedEventArgs e)
	{
		var state = _alcState;
		if (state is null || state.IsClosed || !state.IsVisible)
		{
			return;
		}

		state.IsVisible = false;
		_windowImplementation.RaiseVisibilityChanged(new VisibilityChangedEventArgs { Visible = false });
	}

	/// <summary>
	/// Get visibility for ALC window based on ContentHostOverride state.
	/// </summary>
	private bool GetAlcWindowVisible()
	{
		var state = _alcState;
		if (state is null || state.IsClosed)
		{
			return false;
		}

		var host = ContentHostOverride;
		return host is not null && host.IsLoaded && host.Visibility == Visibility.Visible;
	}

	/// <summary>
	/// Get bounds for ALC window based on ContentHostOverride dimensions.
	/// </summary>
	private Rect GetAlcWindowBounds()
	{
		var state = _alcState;
		if (state is null || state.IsClosed)
		{
			return default;
		}

		var host = ContentHostOverride;
		return host is null ? default : new Rect(0, 0, host.ActualWidth, host.ActualHeight);
	}

	/// <summary>
	/// Activate ALC window - raises Activated event.
	/// </summary>
	private void ActivateAlcWindow()
	{
		var state = _alcState;
		if (state is null || state.IsClosed)
		{
			throw new InvalidOperationException("Cannot activate a closed window.");
		}

		_windowImplementation.RaiseActivated(new WindowActivatedEventArgs(CoreWindowActivationState.CodeActivated));
	}

	/// <summary>
	/// Close ALC window - cleanup and raise event.
	/// </summary>
	private bool CloseAlcWindow()
	{
		var state = _alcState;
		if (state is null || state.IsClosed)
		{
			return false;
		}

		var closedArgs = new WindowEventArgs();
		_windowImplementation.RaiseClosed(closedArgs);
		if (closedArgs.Handled)
		{
			return false; // Handled, cancel close
		}

		state.IsClosed = true;

		// Clear content from host
		var host = ContentHostOverride;
		if (host is not null && ReferenceEquals(host.Content, _secondaryAlcContent))
		{
			host.Content = null;
		}

		// Raise visibility changed if was visible
		if (state.IsVisible)
		{
			state.IsVisible = false;
			_windowImplementation.RaiseVisibilityChanged(new VisibilityChangedEventArgs { Visible = false });
		}

		// Cleanup subscriptions
		UnsubscribeFromContentHostEvents();
		UnmarkSecondaryAlcContent(_secondaryAlcContent);
		_secondaryAlcContent = null;

		return true;
	}

	/// <summary>
	/// Unsubscribe from ContentHostOverride events.
	/// </summary>
	private void UnsubscribeFromContentHostEvents()
	{
		var host = ContentHostOverride;
		var state = _alcState;
		if (host is null || state is null)
		{
			return;
		}

		if (state.HostSizeChangedHandler is not null)
		{
			host.SizeChanged -= state.HostSizeChangedHandler;
		}

		if (state.HostLoadedHandler is not null)
		{
			host.Loaded -= state.HostLoadedHandler;
		}

		if (state.HostUnloadedHandler is not null)
		{
			host.Unloaded -= state.HostUnloadedHandler;
		}
	}

	/// <summary>
	/// Gets whether this window is operating in ALC mode.
	/// </summary>
	internal bool IsAlcWindow => _alcState is not null;

	/// <summary>
	/// Checks if the given content element is from a secondary AssemblyLoadContext.
	/// When value is null, returns false to allow clearing content.
	/// </summary>
	private bool IsContentFromSecondaryAlc(object? value)
	{
		// Explicitly handle null: a null assignment should clear content, not be detected as secondary ALC
		if (value == null)
		{
			return false;
		}

		return !ReferenceEquals(
			AssemblyLoadContext.Default,
			AssemblyLoadContext.GetLoadContext(value.GetType().Assembly));
	}

	private bool IsSecondaryAlcContent(object? value)
		=> IsContentFromSecondaryAlc(value) || IsContentMarkedAsSecondaryAlc(value);

	private static bool IsContentMarkedAsSecondaryAlc(object? value)
		=> value is not null && _secondaryAlcMarkers.TryGetValue(value, out _);

	private static void MarkContentAsSecondaryAlc(object? value)
	{
		if (value is null)
		{
			return;
		}

		_secondaryAlcMarkers.GetValue(value, static _ => new SecondaryAlcMarker());
	}

	private static void UnmarkSecondaryAlcContent(object? value)
	{
		if (value is null)
		{
			return;
		}

		_secondaryAlcMarkers.Remove(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool ShouldRedirectToContentHost(object? value, Assembly callingAssembly)
	{
		if (IsContentFromSecondaryAlc(value))
		{
			return true;
		}

		if (IsAssemblyFromSecondaryAlc(callingAssembly))
		{
			return true;
		}

		return _isWindowFromSecondaryAlc;
	}

	private static bool IsAssemblyFromSecondaryAlc(Assembly assembly)
	{
		var loadContext = AssemblyLoadContext.GetLoadContext(assembly);
		return loadContext is not null && !ReferenceEquals(loadContext, AssemblyLoadContext.Default);
	}

	private sealed class SecondaryAlcMarker
	{
	}
}
