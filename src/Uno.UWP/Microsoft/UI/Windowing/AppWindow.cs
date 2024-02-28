using System;
using System.Collections.Concurrent;
using System.Threading;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Microsoft.UI.Windowing.Native;
using Windows.UI.ViewManagement;
using MUXWindowId = Microsoft.UI.WindowId;

namespace Microsoft.UI.Windowing;

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

	internal AppWindow()
	{
		Id = new(Interlocked.Increment(ref _windowIdIterator));

		_windowIdMap[Id] = this;
		ApplicationView.GetOrCreateForWindowId(Id);
		DisplayInformation.GetOrCreateForWindowId(Id);
	}

	public event TypedEventHandler<AppWindow, AppWindowChangedEventArgs> Changed;

	public string Title
	{
		get => _nativeAppWindow.Title;
		set => _nativeAppWindow.Title = value;
	}

	internal void SetNativeWindow(INativeAppWindow nativeAppWindow)
	{
		if (nativeAppWindow is null)
		{
			throw new ArgumentNullException(nameof(nativeAppWindow));
		}

		_nativeAppWindow = nativeAppWindow;
		SetPresenter(AppWindowPresenterKind.Default);
	}

	public event TypedEventHandler<AppWindow, AppWindowClosingEventArgs> Closing;

	internal static MUXWindowId MainWindowId { get; } = new(1);

	public MUXWindowId Id { get; }

	public AppWindowPresenter Presenter => _presenter;

	public static AppWindow GetFromWindowId(MUXWindowId windowId)
	{
		if (!_windowIdMap.TryGetValue(windowId, out var appWindow))
		{
			throw new InvalidOperationException("Window not found");
		}

		return appWindow;
	}

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
