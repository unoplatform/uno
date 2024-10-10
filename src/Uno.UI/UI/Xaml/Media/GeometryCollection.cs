using System.Collections;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Media
{
	public partial class GeometryCollection : DependencyObjectCollection<Geometry>
	{
		private protected override void OnCollectionChanged()
		{
			base.OnCollectionChanged();
			this.InvalidateMeasure();
		}
	}
}
