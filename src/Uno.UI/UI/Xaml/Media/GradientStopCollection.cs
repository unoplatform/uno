using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media
{
	public sealed partial class GradientStopCollection : DependencyObjectCollection<GradientStop>, IList<GradientStop>, IEnumerable<GradientStop>
	{
		private protected override void OnCollectionChanged()
		{
			base.OnCollectionChanged();
			this.InvalidateArrange();
		}
	}
}
