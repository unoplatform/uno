#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

#if __SKIA__
[global::Uno.NotImplemented]
#endif
public partial class RepositionThemeAnimation : Timeline
{
	// TODO Uno: RepositionThemeAnimation (a CDynamicTimeline in WinUI) is not implemented. It begins as an empty, zero-length
	// timeline, so that a Storyboard containing it still completes: visual state setters aren't applied until a transition
	// storyboard completes.
	internal override void OnBegin()
	{
	}

	internal override void GetNaturalDuration(out DurationType pDurationType, out float pDurationValue)
	{
		pDurationType = DurationType.TimeSpan;
		pDurationValue = 0;
	}
}
