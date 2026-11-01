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

		// CTimingCollection::CheckCanBeModified: key frames can't change while the storyboard of their animation runs.
		private protected override void OnModifying() => (this.GetParent() as Timeline)?.CheckCanBeModified();
	}
}
