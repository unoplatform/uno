using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation
{
	partial class ColorKeyFrameCollection : DependencyObjectCollection<ColorKeyFrame>, IList<ColorKeyFrame>, IEnumerable<ColorKeyFrame>
	{
		public ColorKeyFrameCollection() : base(null, false)
		{
		}

		internal ColorKeyFrameCollection(DependencyObject owner, bool isAutoPropertyInheritanceEnabled)
			: base(owner, isAutoPropertyInheritanceEnabled: isAutoPropertyInheritanceEnabled)
		{
		}

		// CTimingCollection: the animation is the timing owner of its key frames, which can't be added or cleared while
		// its storyboard runs, and whose removal is then ignored.
		private protected override void OnModifying() => (this.GetParent() as Timeline)?.CheckCanBeModified();

		private protected override bool CanRemoveItems() => (this.GetParent() as Timeline)?.CanBeModified() ?? true;
	}
}
