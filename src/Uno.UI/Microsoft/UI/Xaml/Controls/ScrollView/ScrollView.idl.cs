using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public enum ScrollingScrollBarVisibility
{
	Auto = 0,
	Visible = 1,
	Hidden = 2,
}

[ContentProperty(Name = "Content")]
public partial class ScrollView : Control
{
	public event TypedEventHandler<ScrollView, object> ExtentChanged;
	public event TypedEventHandler<ScrollView, object> StateChanged;
	public event TypedEventHandler<ScrollView, object> ViewChanged;
	public event TypedEventHandler<ScrollView, ScrollingScrollAnimationStartingEventArgs> ScrollAnimationStarting;
	public event TypedEventHandler<ScrollView, ScrollingZoomAnimationStartingEventArgs> ZoomAnimationStarting;
	public event TypedEventHandler<ScrollView, ScrollingScrollCompletedEventArgs> ScrollCompleted;
	public event TypedEventHandler<ScrollView, ScrollingZoomCompletedEventArgs> ZoomCompleted;
	public event TypedEventHandler<ScrollView, ScrollingBringingIntoViewEventArgs> BringingIntoView;
	public event TypedEventHandler<ScrollView, ScrollingAnchorRequestedEventArgs> AnchorRequested;
}
