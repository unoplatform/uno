using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class NavigationView
{
#if !__SKIA__ // Uno workaround: ThemeShadow support check; Skia always supports ThemeShadow (WinUI applies it unconditionally).
	private bool IsThemeShadowSupported() => ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.ThemeShadow, Uno.UI");
#endif
}
