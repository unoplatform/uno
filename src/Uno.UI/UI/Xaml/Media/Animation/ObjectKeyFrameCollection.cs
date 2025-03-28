using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public sealed partial class ObjectKeyFrameCollection : DependencyObjectCollection<ObjectKeyFrame>, IList<ObjectKeyFrame>, IEnumerable<ObjectKeyFrame>
	{
		internal ObjectKeyFrameCollection(DependencyObject owner, bool isAutoPropertyInheritanceEnabled)
			: base(parent: owner, isAutoPropertyInheritanceEnabled: isAutoPropertyInheritanceEnabled)
		{
		}
	}
}
