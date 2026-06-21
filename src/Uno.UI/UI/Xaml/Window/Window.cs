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
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Uno.UI;
using Windows.ApplicationModel.Core;
using Windows.Devices.PointOfService;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Represents an application window.
/// </summary>
[ContentProperty(Name = nameof(Content))]
public partial class Window
{
	private static readonly ConcurrentDictionary<AppWindow, Window> _appWindowMap = new();
	private static Window? _current;

	private readonly IWindowImplementation _windowImplementation;

	private bool _initialized;
	private Brush? _background;
	private WindowType _windowType;

	private WeakEventHelper.WeakEventCollection? _sizeChangedHandlers;
	private WeakEventHelper.WeakEventCollection? _backgroundChangedHandlers;

	internal Window(WindowType windowType, Assembly? callingAssembly = null)
	{
		// The real caller is captured by the public parameterless ctor, which guards
		// Assembly.GetCallingAssembly behind ContentHostOverride because it throws
		// PlatformNotSupportedException on AOT/mobile. Resolving it here would only ever
		// yield Uno.UI (and crash on those platforms), so we rely on what's passed in.
		CaptureOwnerAssemblyLoadContext(callingAssembly);

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

		if (ContentHostOverride is not null)
		{
			// Called only when a secondary ALC is setting content

			InitializeAlcState(callingAssembly);
		}

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
	partial void InitializeAlcState(Assembly? callingAssembly);
	partial void CaptureOwnerAssemblyLoadContext(Assembly? callingAssembly);
	internal partial bool TryGetContentFromSecondaryAlc(out UIElement? content);
	private partial bool TrySetContentFromSecondaryAlc(UIElement? value, ContentControl host, Assembly callingAssembly);

#pragma warning disable 67
	/// <summary>
	/// Occurs when the window has successfully been activated.
	/// </summary>
	public event WindowActivatedEventHandler? Activated
	{
		add
		{
			_windowImplementation.Activated += value;
		}
		remove
		{
			_windowImplementation.Activated -= value;
		}
	}

	/// <summary>
	/// Occurs when the app window has first rendered or has changed its rendering size.
	/// </summary>
	public event WindowSizeChangedEventHandler? SizeChanged
	{
		add
		{
			_windowImplementation.SizeChanged += value;
		}
		remove
		{
			_windowImplementation.SizeChanged -= value;
		}
	}

	/// <summary>
	/// Occurs when the value of the Visible property changes.
	/// </summary>
	public event WindowVisibilityChangedEventHandler? VisibilityChanged
	{
		add
		{
			_windowImplementation.VisibilityChanged += value;
		}
		remove
		{
			_windowImplementation.VisibilityChanged -= value;
		}
	}

	public AppWindow AppWindow
	{ get; }

	/// <summary>
	/// Gets a Rect value containing the height and width of the application window in units of effective (view) pixels.
	/// </summary>
	public Rect Bounds => _alcState is not null ? GetAlcWindowBounds() : _windowImplementation.Bounds;

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
			return TryGetContentFromSecondaryAlc(out var alcContent)
				? alcContent
				: _windowImplementation.Content;
		}

		[MethodImpl(MethodImplOptions.NoInlining)] // No inlining to keep Assembly.GetCallingAssembly accurate
		set
		{
			var host = ContentHostOverride;
			if (host is not null)
			{
				// Capture the caller *before* delegating to keep the stack frame stable for secondary ALC detection
				var callingAssembly = Assembly.GetCallingAssembly();
				if (TrySetContentFromSecondaryAlc(value, host, callingAssembly))
				{
					return;
				}
			}

			_windowImplementation.Content = value;
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

	public global::Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue => global::Microsoft.UI.Dispatching.DispatcherQueue.Main;

	/// <summary>
	/// Gets a value that reports whether the window is visible.
	/// </summary>
	public bool Visible => _alcState is not null ? GetAlcWindowVisible() : _windowImplementation.Visible;

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
		if (_alcState is not null)
		{
			ActivateAlcWindow();
			return;
		}

		_windowImplementation.Activate();
	}

	public void Close()
	{
		if (_alcState is not null)
		{
			CloseAlcWindow();
			return;
		}

		_windowImplementation.Close();
	}

	// The parameter name differs between UWP and WinUI.
	// UWP: https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.window.settitlebar?view=winrt-22621
	// WinUI: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.window.settitlebar?view=windows-app-sdk-1.3
	public void SetTitleBar(UIElement? titleBar) => _windowImplementation.SetTitleBar(titleBar);

	public bool ExtendsContentIntoTitleBar
	{
		get => AppWindow.TitleBar.ExtendsContentIntoTitleBar;
		set => AppWindow.TitleBar.ExtendsContentIntoTitleBar = value;
	}

	internal bool HasSupportedSystemBackdrop
	{
		get
		{
#if __SKIA__
			return IsBackdropSupported(_systemBackdrop);
#else
			return false;
#endif
		}
	}

#if __SKIA__
	private Microsoft.UI.Xaml.Media.SystemBackdrop? _systemBackdrop;
	private readonly Dictionary<DependencyObject, Brush?> _savedBackdropBackgrounds = new();
	private readonly HashSet<FrameworkElement> _backdropLoadedSubscriptions = new();
	private bool _backdropBackgroundsApplied;

	/// <summary>
	/// Gets or sets the system backdrop used to render materials like Mica and Acrylic.
	/// </summary>
	public Microsoft.UI.Xaml.Media.SystemBackdrop? SystemBackdrop
	{
		get => _systemBackdrop;
		set
		{
			if (value is not null && !IsBackdropImplemented(value))
			{
				this.LogWarn()?.Warn($"{nameof(SystemBackdrop)} currently supports only {nameof(Media.MicaBackdrop)} and {nameof(Media.DesktopAcrylicBackdrop)} on Skia. '{value.GetType().Name}' will not be applied natively.");
			}

			_systemBackdrop = value;
			NativeWrapper?.SetSystemBackdrop(value);
			UpdateRootVisualBackgroundForBackdrop(value);
		}
	}

	private void UpdateRootVisualBackgroundForBackdrop(Media.SystemBackdrop? backdrop)
	{
		// Always tear down first so subscriptions and saved backgrounds are released even when the
		// root has already been torn down (RootElement is null), avoiding stale strong references.
		if (backdrop is null || !IsBackdropSupported(backdrop))
		{
			ClearBackdropLoadedSubscriptions();
			RestoreSavedBackdropBackgrounds();
			return;
		}

		if (RootElement is null)
		{
			return;
		}

		ApplyTransparentBackgroundsForBackdrop();
	}

	private void ApplyTransparentBackgroundsForBackdrop()
	{
		RestoreSavedBackdropBackgrounds();

		if (RootElement is not { } root)
		{
			return;
		}

		var transparent = new Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
		var pending = new Stack<DependencyObject>();
		var visited = new HashSet<DependencyObject>();
		pending.Push(root);

		while (pending.Count > 0)
		{
			var current = pending.Pop();
			if (!visited.Add(current))
			{
				continue;
			}

			if (TryGetOpaqueBackground(current, out var originalBrush))
			{
				_savedBackdropBackgrounds[current] = originalBrush;
				SetElementBackground(current, transparent);
			}

			// Listen for new descendants being loaded (for example after Frame.Navigate) so we can
			// re-walk and transparentize their backgrounds as well, and for them being unloaded so we
			// can drop the subscription and release the element instead of retaining it indefinitely.
			if (current is FrameworkElement fe && _backdropLoadedSubscriptions.Add(fe))
			{
				fe.Loaded += OnBackdropElementLoaded;
				fe.Unloaded += OnBackdropElementUnloaded;
			}

			for (var i = Media.VisualTreeHelper.GetChildrenCount(current) - 1; i >= 0; i--)
			{
				pending.Push(Media.VisualTreeHelper.GetChild(current, i));
			}
		}

		_backdropBackgroundsApplied = true;
	}

	private void RestoreSavedBackdropBackgrounds()
	{
		if (!_backdropBackgroundsApplied)
		{
			return;
		}

		foreach (var (element, original) in _savedBackdropBackgrounds)
		{
			SetElementBackground(element, original);
		}

		_savedBackdropBackgrounds.Clear();
		_backdropBackgroundsApplied = false;
	}

	private void ClearBackdropLoadedSubscriptions()
	{
		foreach (var element in _backdropLoadedSubscriptions)
		{
			element.Loaded -= OnBackdropElementLoaded;
			element.Unloaded -= OnBackdropElementUnloaded;
		}

		_backdropLoadedSubscriptions.Clear();
	}

	private void OnBackdropElementLoaded(object sender, RoutedEventArgs e)
	{
		if (_systemBackdrop is null || !IsBackdropSupported(_systemBackdrop))
		{
			return;
		}

		// Re-walk so descendants newly added under the loaded element (for example a page
		// inserted by Frame.Navigate) are picked up and transparentized.
		ApplyTransparentBackgroundsForBackdrop();
	}

	private void OnBackdropElementUnloaded(object sender, RoutedEventArgs e)
	{
		// Drop the subscription when an element leaves the tree so it can be collected and we stop
		// re-walking from stale elements. Restore its original background first so that, if it is
		// re-inserted later, the re-walk captures the real value instead of our transparent override.
		if (sender is FrameworkElement fe && _backdropLoadedSubscriptions.Remove(fe))
		{
			fe.Loaded -= OnBackdropElementLoaded;
			fe.Unloaded -= OnBackdropElementUnloaded;

			if (_savedBackdropBackgrounds.Remove(fe, out var original))
			{
				SetElementBackground(fe, original);
			}
		}
	}

	private static bool TryGetOpaqueBackground(DependencyObject element, out Brush? originalBrush)
	{
		// Panel/Border/ContentPresenter derive from FrameworkElement while Control (Page, UserControl,
		// ContentControl, NavigationView, ...) is a separate branch, so these cases never overlap.
		switch (element)
		{
			case Controls.Panel panel when panel.Background is Media.SolidColorBrush panelBrush && panelBrush.Color.A > 0:
				originalBrush = panel.Background;
				return true;
			case Controls.Border border when border.Background is Media.SolidColorBrush borderBrush && borderBrush.Color.A > 0:
				originalBrush = border.Background;
				return true;
			case Controls.ContentPresenter presenter when presenter.Background is Media.SolidColorBrush presenterBrush && presenterBrush.Color.A > 0:
				originalBrush = presenter.Background;
				return true;
			case Controls.Control control when control.Background is Media.SolidColorBrush controlBrush && controlBrush.Color.A > 0:
				originalBrush = control.Background;
				return true;
			default:
				originalBrush = null;
				return false;
		}
	}

	private static void SetElementBackground(DependencyObject element, Brush? brush)
	{
		switch (element)
		{
			case Controls.Panel panel:
				panel.Background = brush;
				break;
			case Controls.Border border:
				border.Background = brush;
				break;
			case Controls.ContentPresenter presenter:
				presenter.Background = brush;
				break;
			case Controls.Control control:
				control.Background = brush;
				break;
		}
	}

	private static bool IsBackdropImplemented(Media.SystemBackdrop? backdrop) => backdrop switch
	{
		null => true,
		Media.MicaBackdrop => true,
		Media.DesktopAcrylicBackdrop => true,
		_ => false,
	};

	private static bool IsBackdropSupported(Media.SystemBackdrop? backdrop) => backdrop switch
	{
		Media.MicaBackdrop => Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported(),
		Media.DesktopAcrylicBackdrop => Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported(),
		_ => false,
	};
#endif

	internal Brush? Background
	{
		get => _background;
		set
		{
			_background = value;

			_backgroundChangedHandlers?.Invoke(this, EventArgs.Empty);
		}
	}

	internal void NotifyContentLoaded()
	{
		_windowImplementation.NotifyContentLoaded();

#if __SKIA__
		// Re-apply pending system backdrop now that the root visual is available.
		if (_systemBackdrop is not null)
		{
			UpdateRootVisualBackgroundForBackdrop(_systemBackdrop);
		}
#endif
	}

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

}
