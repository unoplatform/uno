using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Windows.UI.Xaml.Media
{
	public partial class PathFigureCollection : DependencyObjectCollection<PathFigure>
	{
		public PathFigureCollection()
		{
		}

		private protected override void OnCollectionChanged()
		{
			base.OnCollectionChanged();
			this.InvalidateMeasure();
		}
	}
}
