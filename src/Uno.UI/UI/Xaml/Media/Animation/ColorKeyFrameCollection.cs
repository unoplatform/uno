using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation
{
	partial class ColorKeyFrameCollection : DependencyObjectCollection<ColorKeyFrame>, IList<ColorKeyFrame>, IEnumerable<ColorKeyFrame>
	{
		public ColorKeyFrameCollection() : base(null, false)
		{
		}
	}
}
