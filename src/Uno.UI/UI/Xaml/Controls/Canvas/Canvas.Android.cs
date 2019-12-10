using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls
{
	public partial class Canvas
	{
		/// <summary>
		/// Order of children determined by Canvas.ZIndex, keyed by position in Children
		/// </summary>
		private Dictionary<int, int> _zSortedChildren;

		partial void InitializePartial()
		{
			// Set this in order for GetChildDrawingOrder() to be called
			ChildrenDrawingOrderEnabled = true;
		}

		partial void MeasureOverridePartial()
		{
			_zSortedChildren = Children
				.Select((view, childrenIndex) => (view, childrenIndex))
				.OrderBy(tpl => tpl.view is DependencyObject obj ? Canvas.GetZIndex(obj) : 0)
				.Select((tpl, orderedIndex) => (tpl.childrenIndex, orderedIndex))
				.ToDictionary(keySelector: tpl => tpl.childrenIndex, elementSelector: tpl => tpl.orderedIndex);
		}

		protected override int GetChildDrawingOrder(int childCount, int i)
		{
			return _zSortedChildren?[i] ?? i;
		}
	}
}
