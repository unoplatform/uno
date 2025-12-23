#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.UI;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using AppWindow = Microsoft.UI.Windowing.AppWindow;
using System.Collections.Concurrent;
using Uno.UI;
using Windows.Devices.PointOfService;
using Windows.ApplicationModel.Core;
using System.Diagnostics;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Represents an application window.
/// </summary>
[ContentProperty(Name = nameof(Content))]
public
#if !HAS_UNO_WINUI
sealed
#endif
partial class Window
{
	private static readonly ConcurrentDictionary<AppWindow, Window> _appWindowMap = new();
	private static Window? _current;

	private readonly IWindowImplementation _windowImplementation;

	private bool _initialized;
	private Brush? _background;
	private WindowType _windowType;

	// Window-local storage to detect secondary ALC content
	private object? _secondaryAlcContent;

	private WeakEventHelper.WeakEventCollection? _sizeChangedHandlers;
	private WeakEventHelper.WeakEventCollection? _backgroundChangedHandlers;

	internal Window(WindowType windowType)
	{
#if !__SKIA__
		if (_current is null && CoreApplication.IsFullFledgedApp)
		{
			windowType = WindowType.CoreWindow;
		}
#else
		windowType = WindowType.DesktopXamlSource; // Skia always uses "Desktop" windows.
#endif

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Creating new window (type:{windowType})");
		}

		InitialWindow ??= this;
		_current ??= this; // TODO:MZ: Do we want this?

		AppWindow = new AppWindow();
		_appWindowMap[AppWindow] = this;

		if (
			!NativeWindowFactory.SupportsMultipleWindows

			// When a second ALC is defined with a Window, we allow it
			// even on single-window platforms
			&& Window.ContentHostOverride == null
		)
		{
			if (_current is not null && _current != this)
			{
				throw new InvalidOperationException(
					"Creating secondary windows on this platform is not allowed. " +
					"Ensure you either use Window.Current only, or that you only create a single " +
					"window instance and use it throughout your application.");
			}
		}

		_windowType = windowType;

		_windowImplementation = windowType switch
		{
			WindowType.CoreWindow => new CoreWindowWindow(this),
			WindowType.DesktopXamlSource => new DesktopWindow(this),
			_ => throw new InvalidOperationException("Unsupported window type")
		};

		Compositor = Microsoft.UI.Composition.Compositor.GetSharedCompositor();

		InitializeWindowingFlavor();

		SizeChanged += OnWindowSizeChanged;

		// Eagerly initialize if possible.
		if (Application.Current?.InitializationComplete == true)
		{
			Initialize();
		}

		// We set up the DisplayInformation instance after Initialize so that we have an actual window to bind to.
		global::Windows.Graphics.Display.DisplayInformation.GetOrCreateForWindowId(AppWindow.Id);
	}

	internal INativeWindowWrapper? NativeWrapper => _windowImplementation.NativeWindowWrapper;

	internal static Window GetFromAppWindow(AppWindow appWindow)
	{
		if (!_appWindowMap.TryGetValue(appWindow, out var window))
		{
			throw new InvalidOperationException("Window not found");
		}
		return window;
	}

	partial void InitializeWindowingFlavor();

#pragma warning disable 67
	/// <summary>
	/// Occurs when the window has successfully been activated.
	/// </summary>
	public event WindowActivatedEventHandler? Activated
	{
		add => _windowImplementation.Activated += value;
		remove => _windowImplementation.Activated -= value;
	}

	/// <summary>
	/// Occurs when the app window has first rendered or has changed its rendering size.
	/// </summary>
	public event WindowSizeChangedEventHandler? SizeChanged
	{
		add => _windowImplementation.SizeChanged += value;
		remove => _windowImplementation.SizeChanged -= value;
	}

	/// <summary>
	/// Occurs when the value of the Visible property changes.
	/// </summary>
	public event WindowVisibilityChangedEventHandler? VisibilityChanged
	{
		add => _windowImplementation.VisibilityChanged += value;
		remove => _windowImplementation.VisibilityChanged -= value;
	}

#if HAS_UNO_WINUI
	public
#else
	internal
#endif
	AppWindow AppWindow
	{ get; }

	/// <summary>
	/// Gets a Rect value containing the height and width of the application window in units of effective (view) pixels.
	/// </summary>
	public Rect Bounds => _windowImplementation.Bounds;

	/// <summary>
	/// Gets the Compositor for this window.
	/// </summary>
	public Microsoft.UI.Composition.Compositor Compositor { get; private set; }

	/// <summary>
	/// Gets or sets the visual root of an application window.
	/// </summary>
	public UIElement? Content
	{
		get
		{
			if (IsContentFromSecondaryAlc(_secondaryAlcContent))
			{
				return ContentHostOverride?.Content as UIElement;
			}

			return _windowImplementation.Content;
		}

		set
		{
			if (IsContentFromSecondaryAlc(value) && ContentHostOverride is { } host)
			{
				// We're in a secondary ALC, redirect to the host override
				host.Content = value;
				_secondaryAlcContent = value;
			}
			else
			{
				// We're in the default ALC, set content normally
				_windowImplementation.Content = value;
			}
		}
	}

	/// <summary>
	/// Gets or sets an internal <c>static</c> content host override for scenarios like secondary AssemblyLoadContext (ALC) hosting.
	/// </summary>
	/// <remarks>
	/// Global effect: This property is <c>static</c> and affects all <see cref="Window"/> instances in the application.
	/// Usage: When set, <see cref="Window.Content"/> from secondary ALCs will redirect to this <see cref="ContentControl"/>.
	/// </remarks>
	internal static ContentControl? ContentHostOverride { get; set; }

	/// <summary>
	/// Gets the window of the current thread.
	/// </summary>
	public CoreWindow? CoreWindow => _windowImplementation.CoreWindow;

#pragma warning disable RS0030 // CoreWindow is banned
	/// <summary>
	/// Use this instead of Window.Current throughout this codebase
	/// to prove it is intentional (the property is null throughout Uno.WinUI).
	/// </summary>
	internal CoreWindow? CoreWindowSafe => CoreWindow;
#pragma warning restore RS0030

	/// <summary>
	/// Gets the window of the current thread.		
	/// </summary>
	public static Window? Current
	{
		get
		{
			if (_current is null && CoreApplication.IsFullFledgedApp)
			{
				EnsureWindowCurrent();
			}

			return _current;
		}
	}

#pragma warning disable RS0030 // Current is banned
	/// <summary>
	/// Use this instead of Window.Current throughout this codebase
	/// to prove it is intentional (the property is null throughout Uno.WinUI).
	/// </summary>
	internal static Window? CurrentSafe => _current;
#pragma warning restore RS0030

	/// <summary>
	/// Use this only as a temporary measure on single-window targets.
	/// </summary>
	internal static Window? InitialWindow { get; private set; }

	internal object? NativeWindow => _windowImplementation.NativeWindow;

	/// <summary>
	/// This is run when Application.Current is set and the UI framework is ready to construct
	/// visual elements (this is important for eample for Andorid where trying to construct
	/// UI during Application ctor will fail).
	/// </summary>
	internal void Initialize()
	{
		if (Application.Current?.InitializationComplete != true)
		{
			throw new InvalidOperationException("Application was not yet initialized.");
		}

		if (_initialized)
		{
			return;
		}

		_initialized = true;

		if (_windowType is WindowType.CoreWindow)
		{
			WinUICoreServices.Instance.InitCoreWindowContentRoot();
#if __WASM__ || __ANDROID__ || __IOS__ // We normally call SetHost from the NativeWindowWrapper on DesktopXamlSource targets, but for WASM we put it here.
			WinUICoreServices.Instance.MainVisualTree!.ContentRoot.SetHost(this);
#endif
		}

		_windowImplementation.Initialize();

#if !HAS_UNO_WINUI
		RaiseCreated();
#endif
	}

	internal static void EnsureWindowCurrent()
	{
		if (_current is not null)
		{
			return;
		}

		_current = new Window(WindowType.CoreWindow);
		InitialWindow = _current;
	}

	/// <summary>
	/// Gets the CoreDispatcher object for the Window, which is generally the CoreDispatcher for the UI thread.
	/// </summary>
	public CoreDispatcher Dispatcher => CoreDispatcher.Main;

#if HAS_UNO_WINUI
	public global::Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue => global::Microsoft.UI.Dispatching.DispatcherQueue.Main;
#else
	internal global::Windows.System.DispatcherQueue DispatcherQueue => global::Windows.System.DispatcherQueue.Main;
#endif

	/// <summary>
	/// Gets a value that reports whether the window is visible.
	/// </summary>
	public bool Visible => _windowImplementation.Visible;

	/// <summary>
	/// This is the real root of the **managed** visual tree.
	/// This means its the root panel which contains the <see cref="Content"/>
	/// but also the PopupRoot, the DragRoot and all other internal UI elements.
	/// On platforms like iOS and Android, we might still have few native controls above this.
	/// </summary>
	/// <remarks>This element is flagged with IsVisualTreeRoot.</remarks>
	internal UIElement? RootElement => _windowImplementation.XamlRoot?.VisualTree?.RootElement;

	internal PopupRoot? PopupRoot => _windowImplementation.XamlRoot?.VisualTree?.PopupRoot;

	internal FullWindowMediaRoot? FullWindowMediaRoot => _windowImplementation.XamlRoot?.VisualTree?.FullWindowMediaRoot;

	internal Canvas? FocusVisualLayer => _windowImplementation.XamlRoot?.VisualTree?.FocusVisualRoot;

	public void Activate()
	{
		if (IsContentHostedInSecondaryAlc())
		{
			// Don't activate - we're hosted in another window
			return;
		}

		_windowImplementation.Activate();
	}

	public void Close() => _windowImplementation.Close();

	// The parameter name differs between UWP and WinUI.
	// UWP: https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.window.settitlebar?view=winrt-22621
	// WinUI: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.window.settitlebar?view=windows-app-sdk-1.3
	public void SetTitleBar(UIElement? titleBar) => _windowImplementation.SetTitleBar(titleBar);

	public bool ExtendsContentIntoTitleBar
	{
		get => AppWindow.TitleBar.ExtendsContentIntoTitleBar;
		set => AppWindow.TitleBar.ExtendsContentIntoTitleBar = value;
	}

	internal Brush? Background
	{
		get => _background;
		set
		{
			_background = value;

			_backgroundChangedHandlers?.Invoke(this, EventArgs.Empty);
		}
	}

	internal void NotifyContentLoaded() => _windowImplementation.NotifyContentLoaded();

	internal IDisposable RegisterBackgroundChangedEvent(EventHandler handler)
		=> WeakEventHelper.RegisterEvent(
			_backgroundChangedHandlers ??= new(),
			handler,
			(h, s, e) =>
				(h as EventHandler)?.Invoke(s, (EventArgs)e!)
		);

	/// <summary>
	/// Provides a memory-friendly registration to the <see cref="SizeChanged" /> event.
	/// </summary>
	/// <returns>A disposable instance that will cancel the registration.</returns>
	internal IDisposable RegisterSizeChangedEvent(Microsoft.UI.Xaml.WindowSizeChangedEventHandler handler)
	{
		return WeakEventHelper.RegisterEvent(
			_sizeChangedHandlers ??= new(),
			handler,
			(h, s, e) =>
				(h as Microsoft.UI.Xaml.WindowSizeChangedEventHandler)?.Invoke(s, (WindowSizeChangedEventArgs)e!)
		);
	}

	private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e) => _sizeChangedHandlers?.Invoke(this, e);

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
			System.Runtime.Loader.AssemblyLoadContext.Default,
			System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(value.GetType().Assembly));
	}

	/// <summary>
	/// Checks if the content in ContentHostOverride is from a secondary ALC.
	/// </summary>
	private bool IsContentHostedInSecondaryAlc()
	{
		var content = ContentHostOverride?.Content;
		return content is not null
			&& ContentHostOverride is not null
			&& !ReferenceEquals(
				System.Runtime.Loader.AssemblyLoadContext.Default,
				System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(content.GetType().Assembly));
	}
}
