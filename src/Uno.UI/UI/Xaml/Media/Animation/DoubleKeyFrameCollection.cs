using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	/// <summary>
	/// Represents a collection of DoubleKeyFrame objects.
	/// </summary>
	public partial class DoubleKeyFrameCollection : DependencyObjectCollection<DoubleKeyFrame>, IList<DoubleKeyFrame>, IEnumerable<DoubleKeyFrame>
	{
		public DoubleKeyFrameCollection()
			: base(null, false)
		{

		}

		internal DoubleKeyFrameCollection(DependencyObject owner, bool isAutoPropertyInheritanceEnabled)
			: base(owner, isAutoPropertyInheritanceEnabled: isAutoPropertyInheritanceEnabled)
		{

		}

		// CTimingCollection: the animation is the timing owner of its key frames, which can't be added or cleared while
		// its storyboard runs, and whose removal is then ignored.
		private protected override void OnModifying() => (this.GetParent() as Timeline)?.CheckCanBeModified();

		private protected override bool CanRemoveItems() => (this.GetParent() as Timeline)?.CanBeModified() ?? true;
	}
}
