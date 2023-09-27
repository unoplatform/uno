using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MUXWindowId = Microsoft.UI.WindowId;

#if HAS_UNO_WINUI
namespace Microsoft.UI.Windowing;
#else
namespace Windows.UI.WindowManagement;
#endif

partial class AppWindow
{
	private static readonly ConcurrentDictionary<MUXWindowId, AppWindow> _windowIdMap = new();
	private static ulong _windowIdIterator;

#if HAS_UNO_WINUI
	public
#else
	internal
#endif
	MUXWindowId Id
	{ get; }

#if HAS_UNO_WINUI
	public
#else
	internal
#endif
	static AppWindow GetFromWindowId(MUXWindowId windowId) => _windowIdMap[windowId];
}
