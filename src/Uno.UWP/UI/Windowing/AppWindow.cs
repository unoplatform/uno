using System.Threading;
using Windows.UI.ViewManagement;

#if HAS_UNO_WINUI
namespace Microsoft.UI.Windowing;
#else
namespace Windows.UI.WindowManagement;
#endif

public partial class AppWindow
{
	internal AppWindow()
	{
		Id = new(Interlocked.Increment(ref _windowIdIterator));

		_windowIdMap[Id] = this;
		ApplicationView.InitializeForWindowId(Id);
	}

	internal static WindowId MainWindowId { get; } = new WindowId(1);
}
