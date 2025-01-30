#if __ANDROID__ || __IOS__
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

internal partial class NativeRefreshVisualizer : RefreshVisualizer
{
}
#endif
