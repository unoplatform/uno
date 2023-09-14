using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;

#if HAS_UNO_WINUI
namespace Microsoft.UI.Windowing;
#else
namespace Windows.UI.WindowManagement;
#endif

public partial class AppWindow
{
	private static ulong _windowIdIterator;

	internal AppWindow()
	{
		WindowId = new(Interlocked.Increment(ref _windowIdIterator));
	}

#if HAS_UNO_WINUI
	public
#else
	internal
#endif
	WindowId WindowId
	{ get; }
}
