#if __ANDROID__
using Windows.Foundation;
namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
		internal void SetVisibleBounds(Rect windowBounds)
		{
			VisibleBounds = windowBounds;
			VisibleBoundsChanged?.Invoke(this, null);
		}
	}
}
#endif