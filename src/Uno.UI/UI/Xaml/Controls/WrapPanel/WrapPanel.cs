using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class WrapPanel : Panel
	{
		private Orientation _orientation = Orientation.Horizontal;

		internal override Orientation? PhysicalOrientation
		{
			get
			{
				if (GetTemplatedParent() is ItemsPresenter presenter &&
					presenter.GetTemplatedParent() is GridView gv &&
					gv.GetCurrentHighestValuePrecedence(Control.TemplateProperty) == DependencyPropertyValuePrecedences.DefaultStyle)
				{
					// This is a workaround for our GridView using a WrapPanel instead of an ItemsWrapGrid (which we don't implement).
					// The following is the implementation of ItemsWrapGrid::get_PhysicalOrientation from WinUI.
					if (_orientation is Orientation.Horizontal)
					{
						return Orientation.Vertical;
					}
					else
					{
						return Orientation.Horizontal;
					}
				}
				else
				{
					return _orientation;
				}
			}
		}

		public virtual Orientation Orientation
		{
			get
			{
				return _orientation;
			}
			set
			{
				if (_orientation != value)
				{
					_orientation = value;
					OnOrientationChanged();
				}
			}
		}

		partial void OnOrientationChangedPartial();

		protected virtual void OnOrientationChanged()
		{
			OnOrientationChangedPartial();
		}

		private float? _itemWidth;

		public virtual float? ItemWidth
		{
			get { return _itemWidth; }
			set
			{
				if (_itemWidth != value)
				{
					_itemWidth = value;
					OnItemWidthChanged();
				}
			}
		}

		partial void OnItemWidthChangedPartial();

		protected virtual void OnItemWidthChanged()
		{
			OnItemWidthChangedPartial();
		}

		private float? _itemHeight;

		public virtual float? ItemHeight
		{
			get { return _itemHeight; }
			set
			{
				if (_itemHeight != value)
				{
					_itemHeight = value;
					OnItemHeightChanged();
				}
			}
		}

		partial void OnItemHeightChangedPartial();

		protected virtual void OnItemHeightChanged()
		{
			OnItemHeightChangedPartial();
		}
	}
}
