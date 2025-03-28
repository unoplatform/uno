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
		/// Draw order of children as determined by Canvas.ZIndex
		/// </summary>
		private int[] _drawOrders;

		partial void InitializePartial()
		{
			// Set this in order for GetChildDrawingOrder() to be called
			ChildrenDrawingOrderEnabled = true;
		}

		partial void MeasureOverridePartial()
		{
			// Sorting is only needed when Children count is above 1
			if (Children.Count > 1)
			{
				if (_drawOrders?.Length != Children.Count)
				{
					_drawOrders = new int[Children.Count];
				}

				var sorted = Children
					.Select((view, childrenIndex) => (view, childrenIndex))
					.OrderBy(tpl => tpl.view is UIElement obj ? Canvas.GetZIndex(obj) : 0); // Note: this has to be a stable sort

				var drawOrder = 0;
				foreach (var tpl in sorted)
				{
					_drawOrders[tpl.childrenIndex] = drawOrder;
					drawOrder++;
				}
			}
			else
			{
				_drawOrders = null;
			}
		}

		protected override int GetChildDrawingOrder(int childCount, int i)
		{
			return _drawOrders?.Length == childCount ? _drawOrders[i] : i;
		}
	}
}
