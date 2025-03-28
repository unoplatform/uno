namespace Windows.UI.Xaml.Media.Animation;

#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
[global::Uno.NotImplemented]
#endif
public partial class RepositionThemeAnimation : Timeline, ITimeline
{
	// RepositionThemeAnimation is currently not implemented.
	// But we need to call OnCompleted() so that whenever RepositionThemeAnimation is
	// included in a Storyboard, we want to allow the Storyboard to fire Completed event
	// This is important especially when RepositionThemeAnimation is part of a transition Storyboard.
	// Visual state setters aren't executed unless the transition is completed.
	void ITimeline.Begin() => OnCompleted();
}
