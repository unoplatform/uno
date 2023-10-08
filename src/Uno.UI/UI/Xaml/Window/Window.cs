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
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using AppWindow = Microsoft.UI.Windowing.AppWindow;
using System.Collections.Concurrent;
using Uno.UI;

namespace Windows.UI.Xaml;

/// <summary>
/// Represents an application window.
/// </summary>
[ContentProperty(Name = nameof(Content))]
public partial class Window
{
	private static readonly ConcurrentDictionary<AppWindow, Window> _appWindowMap = new();
	private static Window? _current;

	private readonly IWindowImplementation _windowImplementation;

	private Brush? _background;
	private bool _splashScreenDismissed;

	private List<WeakEventHelper.GenericEventHandler> _sizeChangedHandlers = new List<WeakEventHelper.GenericEventHandler>();
	private List<WeakEventHelper.GenericEventHandler>? _backgroundChangedHandlers;

	internal Window(WindowType windowType)
	{
#if WINUI_WINDOWING
		InitialWindow ??= this;
		_current ??= this; // TODO:MZ: Do we want this?
#endif

		// TODO: On non-multiwindow targets, keep CoreWindow-only approach for now #8978!
		if (!NativeWindowFactory.SupportsMultipleWindows)
		{
			windowType = WindowType.CoreWindow;
		}

		AppWindow = new AppWindow();
		_appWindowMap[AppWindow] = this;

		if (windowType is WindowType.CoreWindow)
		{
			WinUICoreServices.Instance.InitCoreWindowContentRoot();
		}

		_windowImplementation = windowType switch
		{
			WindowType.CoreWindow => new CoreWindowWindow(this),
			WindowType.DesktopXamlSource => new DesktopWindow(this),
			_ => throw new InvalidOperationException("Unsupported window type")
		};

		Compositor = Windows.UI.Composition.Compositor.GetSharedCompositor();

		InitializeWindowingFlavor();
		InitPlatform();

#if !HAS_UNO_WINUI
		RaiseCreated();
#endif

		Background = SolidColorBrushHelper.White;

		SizeChanged += OnWindowSizeChanged;
		Closed += OnWindowClosed;

		ApplicationHelper.AddWindow(this);
	}

	private void OnWindowClosed(object sender, object e) => ApplicationHelper.RemoveWindow(this);

	internal static Window GetFromAppWindow(AppWindow appWindow) => _appWindowMap[appWindow];

	partial void InitializeWindowingFlavor();

	partial void InitPlatform();

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
	public Compositor Compositor { get; private set; }

	/// <summary>
	/// Gets or sets the visual root of an application window.
	/// </summary>
	public UIElement? Content
	{
		get => _windowImplementation.Content;
		set
		{
			_windowImplementation.Content = value;
			TryDismissSplashScreen();
		}
	}

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
	public static Window? Current => _current;

#pragma warning disable RS0030 // Current is banned
	/// <summary>
	/// Use this instead of Window.Current throughout this codebase
	/// to prove it is intentional (the property is null throughout Uno.WinUI).
	/// </summary>
	internal static Window? CurrentSafe => Current;
#pragma warning restore RS0030

	internal static Window? IShouldntUseCurrentWindow => CurrentSafe;

	/// <summary>
	/// Use this only as a temporary measure on single-window targets.
	/// </summary>
	internal static Window? InitialWindow { get; private set; }

#if !HAS_UNO_WINUI && !WINUI_WINDOWING
	internal static void InitializeWindowCurrent()
	{
		_current = new Window(WindowType.CoreWindow);
		InitialWindow = _current;
	}
#endif

	/// <summary>
	/// Gets the CoreDispatcher object for the Window, which is generally the CoreDispatcher for the UI thread.
	/// </summary>
	public CoreDispatcher Dispatcher => CoreDispatcher.Main;

#if HAS_UNO_WINUI
	public global::Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; } = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
#else
	internal global::Windows.System.DispatcherQueue DispatcherQueue { get; } = global::Windows.System.DispatcherQueue.GetForCurrentThread();
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
	internal UIElement? RootElement => _windowImplementation.XamlRoot?.VisualTree?.RootElement; //TODO:MZ: Is it ok to change to RootElement instead of PublicRootVisual?

	internal PopupRoot? PopupRoot => _windowImplementation.XamlRoot?.VisualTree?.PopupRoot;

	internal FullWindowMediaRoot? FullWindowMediaRoot => _windowImplementation.XamlRoot?.VisualTree?.FullWindowMediaRoot;

	internal Canvas? FocusVisualLayer => _windowImplementation.XamlRoot?.VisualTree?.FocusVisualRoot;

	public void Activate()
	{
		_windowImplementation.Activate();

		TryDismissSplashScreen();
	}

	public void Close() => _windowImplementation.Close();

	private void TryDismissSplashScreen()
	{
		if (Content != null && !_splashScreenDismissed)
		{
			DismissSplashScreenPlatform();
			_splashScreenDismissed = true;
		}
	}

	partial void DismissSplashScreenPlatform();

	// The parameter name differs between UWP and WinUI.
	// UWP: https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.window.settitlebar?view=winrt-22621
	// WinUI: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.window.settitlebar?view=windows-app-sdk-1.3
	public void SetTitleBar(
#if HAS_UNO_WINUI
		UIElement titleBar
#else
		UIElement value
#endif
		)
	{
	}

	internal Brush? Background
	{
		get => _background;
		set
		{
			_background = value;

			if (_backgroundChangedHandlers != null)
			{
				foreach (var action in _backgroundChangedHandlers)
				{
					action(this, EventArgs.Empty);
				}
			}
		}
	}

	internal IDisposable RegisterBackgroundChangedEvent(EventHandler handler)
		=> WeakEventHelper.RegisterEvent(
			_backgroundChangedHandlers ??= new(),
			handler,
			(h, s, e) =>
				(h as EventHandler)?.Invoke(s, (EventArgs)e)
		);

	/// <summary>
	/// Provides a memory-friendly registration to the <see cref="SizeChanged" /> event.
	/// </summary>
	/// <returns>A disposable instance that will cancel the registration.</returns>
	internal IDisposable RegisterSizeChangedEvent(Windows.UI.Xaml.WindowSizeChangedEventHandler handler)
	{
		return WeakEventHelper.RegisterEvent(
			_sizeChangedHandlers,
			handler,
			(h, s, e) =>
				(h as Windows.UI.Xaml.WindowSizeChangedEventHandler)?.Invoke(s, (WindowSizeChangedEventArgs)e)
		);
	}

	private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
	{
		foreach (var action in _sizeChangedHandlers)
		{
			action(this, e);
		}
	}
}
