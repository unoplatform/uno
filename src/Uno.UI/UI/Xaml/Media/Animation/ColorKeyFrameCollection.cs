using System.Collections.Generic;

namespace Windows.UI.Xaml.Media.Animation
{
	partial class ColorKeyFrameCollection : DependencyObjectCollection<ColorKeyFrame>, IList<ColorKeyFrame>, IEnumerable<ColorKeyFrame>
	{
		public ColorKeyFrameCollection() : base(null, false)
		{
		}

		internal ColorKeyFrameCollection(DependencyObject owner, bool isAutoPropertyInheritanceEnabled) : base(owner,  isAutoPropertyInheritanceEnabled)
		{
		}
	}
}
