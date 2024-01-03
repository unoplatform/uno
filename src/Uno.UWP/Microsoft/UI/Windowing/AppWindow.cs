using System;
using System.Collections.Concurrent;
using System.Threading;
using Windows.Foundation;
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

	internal AppWindow()
	{
		Id = new(Interlocked.Increment(ref _windowIdIterator));

		_windowIdMap[Id] = this;
		ApplicationView.InitializeForWindowId(Id);
	}

	public event TypedEventHandler<AppWindow, AppWindowClosingEventArgs> Closing;

	internal static MUXWindowId MainWindowId { get; } = new(1);

	public MUXWindowId Id { get; }

	public static AppWindow GetFromWindowId(MUXWindowId windowId)
	{
		if (!_windowIdMap.TryGetValue(windowId, out var appWindow))
		{
			throw new InvalidOperationException("Window not found");
		}

		return appWindow;
	}

	internal void RaiseClosing(AppWindowClosingEventArgs args) => Closing?.Invoke(this, args);
}
