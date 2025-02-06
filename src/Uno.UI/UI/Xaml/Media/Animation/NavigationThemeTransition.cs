using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Media.Animation;

[ContentProperty(Name = nameof(DefaultNavigationTransitionInfo))]
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
[global::Uno.NotImplemented]
#endif
public partial class NavigationThemeTransition : Transition
{
}
