// SnapHelper is only available in API 24+
#if !MONOANDROID6_0 && !MONOANDROID7_0
using System;
using System.Collections.Generic;
using System.Text;
using AndroidX.RecyclerView.Widget;
using Android.Views;

using Uno.Extensions;
using Windows.UI.Xaml.Controls.Primitives;
using static AndroidX.RecyclerView.Widget.RecyclerView;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeListViewBase
	{
		partial void InitializeSnapHelper()
		{
			var helper = new SnapPointsSnapHelper(this);
			helper.AttachToRecyclerView(this);

			UseNativeSnapping = true;
		}

		private class SnapPointsSnapHelper : SnapHelper
		{
			private readonly NativeListViewBase _owner;

			private int _velocitySign;

			public SnapPointsSnapHelper(NativeListViewBase owner)
			{
				_owner = owner;
			}
			/// <summary>
			/// Called immediately on drag release, or once fling has 'found' the view for target position, to determine the remaining 
			/// scroll offset to apply. This will determine where the target view is positioned in the viewport. It will only be called 
			/// if <see cref="FindSnapView(LayoutManager)"/> or <see cref="FindTargetSnapPosition(LayoutManager, int, int)"/> has 
			/// previously returned a valid view/position.
			/// </summary>
			public override int[] CalculateDistanceToFinalSnap(LayoutManager layoutManager, View targetView)
			{
				var layout = layoutManager as VirtualizingPanelLayout;

				var snapTo = layout.GetSnapTo(_velocitySign, layout.ContentOffset);

				if (!snapTo.HasValue)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning("No snap point found.");
					}

					return new[] { 0, 0 };
				}

				return GetSnapToAsRemainingDistance(layout, snapTo.Value);
			}

			private int[] GetSnapToAsRemainingDistance(VirtualizingPanelLayout layout, float snapTo)
			{
				var diff = layout.GetSnapToAsRemainingDistance(snapTo);

				if (layout.ScrollOrientation == Orientation.Vertical)
				{
					return new[] { 0, (int)diff };
				}
				else
				{
					return new[] { (int)diff, 0 };
				}
			}

			/// <summary>
			/// Called when drag is released (ie no fling) to find an existing materialized view to snap to. Here we simply set the 
			/// velocity sign to 0 and return an arbitrary view; the actual snap logic is in <see cref="CalculateDistanceToFinalSnap(LayoutManager, View)"/>.
			/// </summary>
			public override View FindSnapView(LayoutManager layoutManager)
			{
				if ((layoutManager as VirtualizingPanelLayout).SnapPointsType != SnapPointsType.MandatorySingle)
				{
					return null;
				}

				_velocitySign = 0;
				return layoutManager.GetChildAt(0);
			}

			/// <summary>
			/// Called when fling begins to find an adapter position to snap to. Here we simply set the velocity sign correctly and return 
			/// an arbitrary position; the actual snap logic is in <see cref="CalculateDistanceToFinalSnap(LayoutManager, View)"/>.
			/// </summary>
			public override int FindTargetSnapPosition(LayoutManager layoutManager, int velocityX, int velocityY)
			{
				var layout = layoutManager as VirtualizingPanelLayout;
				if (layout.SnapPointsType != SnapPointsType.MandatorySingle)
				{
					return RecyclerView.NoPosition;
				}

				_velocitySign = Math.Sign(layout.ScrollOrientation == Orientation.Vertical ? velocityY : velocityX);
				return (layoutManager as VirtualizingPanelLayout).GetFirstVisibleDisplayPosition();
			}
		}
	}
}

#endif
