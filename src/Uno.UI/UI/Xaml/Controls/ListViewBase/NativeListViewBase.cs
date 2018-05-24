#if !NET46 && !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Uno;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeListViewBase : IFrameworkElement, DependencyObject, IScrollSnapPointsInfo
	{
		public bool AreHorizontalSnapPointsRegular => ((IScrollSnapPointsInfo)NativeLayout).AreHorizontalSnapPointsRegular;

		public bool AreVerticalSnapPointsRegular => ((IScrollSnapPointsInfo)NativeLayout).AreVerticalSnapPointsRegular;

		internal ListViewBase XamlParent { get; set; }

		[NotImplemented]
		public event EventHandler<object> HorizontalSnapPointsChanged
		{
			add
			{
				((IScrollSnapPointsInfo)NativeLayout).HorizontalSnapPointsChanged += value;
			}

			remove
			{
				((IScrollSnapPointsInfo)NativeLayout).HorizontalSnapPointsChanged -= value;
			}
		}

		[NotImplemented]
		public event EventHandler<object> VerticalSnapPointsChanged
		{
			add
			{
				((IScrollSnapPointsInfo)NativeLayout).VerticalSnapPointsChanged += value;
			}

			remove
			{
				((IScrollSnapPointsInfo)NativeLayout).VerticalSnapPointsChanged -= value;
			}
		}

		public IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
		{
			return ((IScrollSnapPointsInfo)NativeLayout).GetIrregularSnapPoints(orientation, alignment);
		}

		public float GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset)
		{
			return ((IScrollSnapPointsInfo)NativeLayout).GetRegularSnapPoints(orientation, alignment, out offset);
		}
	}
}

#endif