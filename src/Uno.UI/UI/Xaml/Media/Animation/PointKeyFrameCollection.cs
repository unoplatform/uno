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

	// CTimingCollection::CheckCanBeModified: key frames can't change while the storyboard of their animation runs.
	private protected override void OnModifying() => (this.GetParent() as Timeline)?.CheckCanBeModified();
}
