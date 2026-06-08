using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Media.Animation;

[ContentProperty(Name = nameof(DefaultNavigationTransitionInfo))]
#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
[global::Uno.NotImplemented]
#endif
public partial class NavigationThemeTransition : Transition
{
}
