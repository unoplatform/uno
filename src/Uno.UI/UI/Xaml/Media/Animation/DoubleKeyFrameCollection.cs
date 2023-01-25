using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
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
	}
}
