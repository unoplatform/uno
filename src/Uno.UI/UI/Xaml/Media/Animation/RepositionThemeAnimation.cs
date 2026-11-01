#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

// TODO Uno: RepositionThemeAnimation (a CDynamicTimeline in WinUI) is not implemented. Like the other unported theme
// animations it begins as an empty, zero-length timeline, so that a Storyboard containing it still completes: visual
// state setters aren't applied until a transition storyboard completes.
#if __SKIA__
[global::Uno.NotImplemented]
#endif
public partial class RepositionThemeAnimation : Timeline
{
}
