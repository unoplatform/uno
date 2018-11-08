using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public abstract partial class VirtualizingPanelLayout : DependencyObject
	{
		public ListViewBase XamlParent => throw new NotImplementedException();


		private void OnOrientationChanged(Orientation newValue)
		{
			throw new NotImplementedException();
		}

		private IndexPath GetFirstVisibleIndexPath()
		{
			throw new NotImplementedException();
		}

		private IndexPath GetLastVisibleIndexPath()
		{
			throw new NotImplementedException();
		}

		private IEnumerable<float> GetSnapPointsInner(SnapPointsAlignment alignment)
		{
			throw new NotImplementedException();
		}

		private float AdjustOffsetForSnapPointsAlignment(float offset) => throw new NotImplementedException();
	}
}
