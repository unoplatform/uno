using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public sealed partial class ObjectKeyFrameCollection : DependencyObjectCollection<ObjectKeyFrame>, IList<ObjectKeyFrame>, IEnumerable<ObjectKeyFrame>
	{
		public ObjectKeyFrameCollection()
			: base(null, false)
		{
		}

		internal ObjectKeyFrameCollection(DependencyObject owner, bool isAutoPropertyInheritanceEnabled)
			: base(parent: owner, isAutoPropertyInheritanceEnabled: isAutoPropertyInheritanceEnabled)
		{
		}

		// CTimingCollection: the animation is the timing owner of its key frames, which can't be added or cleared while
		// its storyboard runs, and whose removal is then ignored.
		private protected override void OnModifying() => (this.GetParent() as Timeline)?.CheckCanBeModified();

		private protected override bool CanRemoveItems() => (this.GetParent() as Timeline)?.CanBeModified() ?? true;
	}
}
