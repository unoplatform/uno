#if __ANDROID__
using System;
using Android.Views;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Xaml.Controls.Primitives
{
	public sealed partial class Thumb : Control
	{
		private double _startX;
		private double _startY;
		
		public override bool DispatchTouchEvent(MotionEvent e)
		{
			var array = new int[2];
			this.GetLocationOnScreen(array);

			var actualX = array[0] + e.GetX();
			var actualY = array[1] + e.GetY();

			if (e.Action == MotionEventActions.Down)
			{
				StartDrag(actualX, actualY);

				return true;
			}
			else if (e.Action == MotionEventActions.Move)
			{
				DeltaDrag(actualX, actualY);
				return true;
			}
			else if (e.Action == MotionEventActions.Up)
			{
				CompleteDrag(actualX, actualY);

				return true;
			}

			return base.DispatchTouchEvent(e);
		}

		internal void StartDrag(Point location)
		{
			var physicalLocation = location.LogicalToPhysicalPixels();
			StartDrag(physicalLocation.X, physicalLocation.Y);
		}

		private void StartDrag(double x, double y)
		{

			_startX = x;
			_startY = y;

			IsDragging = true;
			DragStarted?.Invoke(this, new DragStartedEventArgs(0, 0));
		}

		internal void CompleteDrag(Point location)
		{
			var physicalLocation = location.LogicalToPhysicalPixels();
			CompleteDrag(physicalLocation.X, physicalLocation.Y);
		}

		private void CompleteDrag(double x, double y)
		{
			IsDragging = false;
			DragCompleted?.Invoke(
					this,
					new DragCompletedEventArgs(
						ViewHelper.PhysicalToLogicalPixels(x - _startX),
						ViewHelper.PhysicalToLogicalPixels(y - _startY)
						, false
					)
				);
		}

		internal void DeltaDrag(Point location)
		{
			var physicalLocation = location.LogicalToPhysicalPixels();
			DeltaDrag(physicalLocation.X, physicalLocation.Y);
		}

		private void DeltaDrag(double x, double y)
		{

			DragDelta?.Invoke(
				this,
				new DragDeltaEventArgs(
					ViewHelper.PhysicalToLogicalPixels(x - _startX),
					ViewHelper.PhysicalToLogicalPixels(y - _startY)
				)
			);
		}
	}
}
#endif