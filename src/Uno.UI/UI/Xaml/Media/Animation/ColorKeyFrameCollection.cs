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

		// CTimingCollection::CheckCanBeModified: key frames can't change while the storyboard of their animation runs.
		private protected override void OnModifying() => (this.GetParent() as Timeline)?.CheckCanBeModified();
	}
}
