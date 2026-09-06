using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Media.Animation;

[ContentProperty(Name = nameof(DefaultNavigationTransitionInfo))]
#if __SKIA__
[global::Uno.NotImplemented]
#endif
public partial class NavigationThemeTransition : Transition
{
}
