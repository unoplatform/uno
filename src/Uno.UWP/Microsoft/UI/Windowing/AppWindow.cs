using System.Collections.Concurrent;
using System.Threading;
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

	internal static MUXWindowId MainWindowId { get; } = new(1);

	public MUXWindowId Id { get; }

	public static AppWindow GetFromWindowId(MUXWindowId windowId) => _windowIdMap[windowId];
}
