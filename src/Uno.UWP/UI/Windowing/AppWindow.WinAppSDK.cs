using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if HAS_UNO_WINUI
namespace Microsoft.UI.Windowing;
#else
namespace Windows.UI.WindowManagement;
#endif

partial class AppWindow
{
	private static readonly Dictionary<WindowId, AppWindow> _appWindowIdMap = new();
	private static ulong _windowIdIterator;

#if HAS_UNO_WINUI
	public
#else
	internal
#endif
	WindowId WindowId
	{ get; }

#if HAS_UNO_WINUI
	public
#else
	internal
#endif
	static AppWindow GetFromWindowId(WindowId windowId) => _appWindowIdMap[windowId];
}
