#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Represents a collection of PointKeyFrame objects.
/// </summary>
public partial class PointKeyFrameCollection : DependencyObjectCollection<PointKeyFrame>, IList<PointKeyFrame>, IEnumerable<PointKeyFrame>
{
	public PointKeyFrameCollection()
		: base(null, false)
	{
	}

	internal PointKeyFrameCollection(DependencyObject owner, bool isAutoPropertyInheritanceEnabled)
		: base(owner, isAutoPropertyInheritanceEnabled: isAutoPropertyInheritanceEnabled)
	{
	}

	// CTimingCollection: the animation is the timing owner of its key frames, which can't be added or cleared while
	// its storyboard runs, and whose removal is then ignored.
	private protected override void OnModifying() => (this.GetParent() as Timeline)?.CheckCanBeModified();

	private protected override bool CanRemoveItems() => (this.GetParent() as Timeline)?.CanBeModified() ?? true;
}
