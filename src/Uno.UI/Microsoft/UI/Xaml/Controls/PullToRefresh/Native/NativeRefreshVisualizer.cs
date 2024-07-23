#if __ANDROID__ || __APPLE_UIKIT__
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

internal partial class NativeRefreshVisualizer : RefreshVisualizer
{
}
#endif
