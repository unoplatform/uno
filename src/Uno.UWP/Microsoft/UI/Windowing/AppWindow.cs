using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Windows.Foundation;
using Microsoft.UI.Windowing.Native;
using Windows.UI.ViewManagement;
using MUXWindowId = Microsoft.UI.WindowId;
using Windows.Graphics;
using Microsoft.UI.Dispatching;

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

	public event TypedEventHandler<AppWindow, AppWindowChangedEventArgs> Changed;

	/// <summary>
	/// Gets the title bar of the app window.
	/// </summary>
	public AppWindowTitleBar TitleBar { get; } = new AppWindowTitleBar();

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

	public AppWindowTitleBar TitleBar { get; }

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

	public event TypedEventHandler<AppWindow, AppWindowClosingEventArgs> Closing;

	internal static MUXWindowId MainWindowId { get; } = new(1);

	public static AppWindow GetFromWindowId(MUXWindowId windowId)
	{
		if (!_windowIdMap.TryGetValue(windowId, out var appWindow))
		{
			throw new InvalidOperationException("Window not found");
		}

		return appWindow;
	}

	internal static bool TryGetFromWindowId(MUXWindowId windowId, [NotNullWhen(true)] out AppWindow appWindow)
		=> _windowIdMap.TryGetValue(windowId, out appWindow);

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
}
