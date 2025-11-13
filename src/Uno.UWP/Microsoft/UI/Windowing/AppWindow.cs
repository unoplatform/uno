using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing.Native;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.ViewManagement;
using MUXWindowId = Microsoft.UI.WindowId;

namespace Microsoft.UI.Windowing;

/// <summary>
/// Represents a system-managed container for the content of an app.
/// </summary>
#if HAS_UNO_WINUI
public
#else
internal
#endif
partial class AppWindow
{
	private static readonly ConcurrentDictionary<MUXWindowId, AppWindow> _windowIdMap = new();
	private static ulong _windowIdIterator;

	private INativeAppWindow _nativeAppWindow;

	private AppWindowPresenter _presenter;
	private string _titleCache; // only use this until the _nativeAppWindow is set

	internal AppWindow()
	{
		Id = new(Interlocked.Increment(ref _windowIdIterator));

		TitleBar = new(this);

		_windowIdMap[Id] = this;
		ApplicationView.GetOrCreateForWindowId(Id);
	}

	/// <summary>
	/// Occurs when a property of the window has changed, and the system is in a "steady state" for the time being.
	/// </summary>
	public event TypedEventHandler<AppWindow, AppWindowChangedEventArgs> Changed;

	/// <summary>
	/// Occurs when a window is being closed through a system affordance.
	/// </summary>
	public event TypedEventHandler<AppWindow, AppWindowClosingEventArgs> Closing;

	internal static MUXWindowId MainWindowId { get; } = new(1);

	internal INativeAppWindow NativeAppWindow => _nativeAppWindow;

	/// <summary>
	/// Gets the title bar of the app window.
	/// </summary>
	public AppWindowTitleBar TitleBar { get; }

	/// <summary>
	/// Gets the current size of the window's client area in client coordinates.
	/// </summary>
	public SizeInt32 ClientSize => _nativeAppWindow.ClientSize;

	/// <summary>
	/// Gets the dispatcher queue associated with the app window.
	/// </summary>
	public DispatcherQueue DispatcherQueue => _nativeAppWindow.DispatcherQueue;

	/// <summary>
	/// Gets the identifier for the app window.
	/// </summary>
	public MUXWindowId Id { get; }

	/// <summary>
	/// Gets a value that indicates whether the window is shown.
	/// </summary>
	public bool IsVisible => _nativeAppWindow.IsVisible;

	/// <summary>
	/// Gets the current position of the window in screen coordinates.
	/// </summary>
	public PointInt32 Position => _nativeAppWindow.Position;

	/// <summary>
	/// Gets the currently applied presenter for the app window.
	/// </summary>
	public AppWindowPresenter Presenter => _presenter;

	/// <summary>
	/// Gets the current size of the window in screen coordinates.
	/// </summary>
	public SizeInt32 Size => _nativeAppWindow.Size;

	/// <summary>
	/// Gets or sets the displayed title of the app window.
	/// </summary>
	public string Title
	{
		get => _nativeAppWindow is not null ? _nativeAppWindow.Title : _titleCache;
		set
		{
			if (_nativeAppWindow is not null)
			{
				_nativeAppWindow.Title = value;
			}

			_titleCache = value;
		}
	}

	/// <summary>
	/// Returns the AppWindow with the specified WindowId, if available. Returns null if the WindowId cannot be matched to a valid window.
	/// </summary>
	/// <param name="windowId">The identifier for the AppWindow.</param>
	/// <returns>The AppWindow with the specified WindowId, if available; null if the WindowId cannot be matched to a valid window.</returns>
	public static AppWindow GetFromWindowId(MUXWindowId windowId) =>
		_windowIdMap.TryGetValue(windowId, out var appWindow) ? appWindow : null;

	internal static bool TryGetFromWindowId(MUXWindowId windowId, [NotNullWhen(true)] out AppWindow appWindow) =>
		_windowIdMap.TryGetValue(windowId, out appWindow);

	internal static void SkipMainWindowId()
	{
		// In case of Uno Islands we currently have no "main window",
		// so we must avoid assigning the first created secondary window
		// Id = 1, otherwise it would be considered as the main window.
		if (!CoreApplication.IsFullFledgedApp && _windowIdIterator == 0)
		{
			_windowIdIterator++;
		}
	}

	/// <summary>
	/// Sets the icon for the window.
	/// </summary>
	public void SetIcon(string iconPath)
	{
		// If the path is relative, construct the absolute path based on the current directory
		if (!Path.IsPathRooted(iconPath))
		{
			iconPath = Path.Combine(Package.Current.InstalledPath, iconPath);
		}

		_nativeAppWindow.SetIcon(iconPath);
	}

	/// <summary>
	/// Shows the window and activates it.
	/// </summary>
	public void Show() => Show(true);

	/// <summary>
	/// Shows the window with an option to activate it or not.
	/// </summary>
	/// <param name="activateWindow">Shows the window with an option to activate it or not.</param>
	public void Show(bool activateWindow) => _nativeAppWindow.Show(activateWindow);

	/// <summary>
	/// Moves the window to the specified point in screen coordinates.
	/// </summary>
	/// <param name="position">The point to move the window to in screen coordinates.</param>
	public void Move(PointInt32 position) => _nativeAppWindow.Move(position);

	/// <summary>
	/// Resizes the window to the specified size.
	/// </summary>
	/// <param name="size">The height and width of the window in screen coordinates.</param>
	public void Resize(SizeInt32 size) => _nativeAppWindow.Resize(size);

	/// <summary>
	/// Applies the specified presenter to the window.
	/// </summary>
	/// <param name="appWindowPresenter">The presenter to apply to the window.</param>
	public void SetPresenter(AppWindowPresenter appWindowPresenter)
	{
		if (_presenter == appWindowPresenter)
		{
			return;
		}

		if (_presenter is not null)
		{
			_presenter.SetOwner(null);
		}

		appWindowPresenter.SetOwner(this);
		_presenter = appWindowPresenter;
		_nativeAppWindow.SetPresenter(_presenter);
		Changed?.Invoke(this, new AppWindowChangedEventArgs() { DidPresenterChange = true });
	}

	/// <summary>
	/// Applies the specified presenter kind to the window.
	/// </summary>
	/// <param name="appWindowPresenterKind">The presenter kind to apply to the window.</param>
	/// <exception cref="NotSupportedException">Thrown when an unsupported presenter is requested.</exception>
	/// <exception cref="InvalidOperationException">Thrown when invalid param value is provided.</exception>
	public void SetPresenter(AppWindowPresenterKind appWindowPresenterKind)
	{
		switch (appWindowPresenterKind)
		{
			case AppWindowPresenterKind.CompactOverlay:
				throw new NotSupportedException("CompactOverlay presenter is not yet supported for non-Windows targets.");
			case AppWindowPresenterKind.FullScreen:
				SetPresenter(FullScreenPresenter.Create());
				break;
			case AppWindowPresenterKind.Overlapped:
			case AppWindowPresenterKind.Default:
				SetPresenter(OverlappedPresenter.Create());
				break;
			default:
				throw new InvalidOperationException("Invalid presenter kind");
		}
	}

	internal void RaiseClosing(AppWindowClosingEventArgs args) => Closing?.Invoke(this, args);

	internal void OnAppWindowChanged(AppWindowChangedEventArgs args) => Changed?.Invoke(this, args);

	internal void SetNativeWindow(INativeAppWindow nativeAppWindow)
	{
		if (nativeAppWindow is null)
		{
			throw new ArgumentNullException(nameof(nativeAppWindow));
		}

		_nativeAppWindow = nativeAppWindow;

		if (string.IsNullOrWhiteSpace(_nativeAppWindow.Title) && !string.IsNullOrWhiteSpace(_titleCache))
		{
			_nativeAppWindow.Title = _titleCache;
		}
		else
		{
			_titleCache = _nativeAppWindow.Title;
		}

		SetPresenter(AppWindowPresenterKind.Default);
	}
}
