using System;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public sealed partial class Thumb : Control
	{
		private Point _startLocation;

		internal void StartDrag(Point location)
		{
			_startLocation = location;

			IsDragging = true;
			DragStarted?.Invoke(this, new DragStartedEventArgs(0, 0));
		}

		internal void DeltaDrag(Point location)
		{
			DragDelta?.Invoke(this, new DragDeltaEventArgs(location.X - _startLocation.X, location.Y - _startLocation.Y));
		}

		internal void CompleteDrag(Point location)
		{
			IsDragging = false;
			DragCompleted?.Invoke(this, new DragCompletedEventArgs(location.X - _startLocation.X, location.Y - _startLocation.Y, false));
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);
			args.Handled = true;
			CapturePointer(args.Pointer);
			StartDrag(args.GetCurrentPoint());
		}

		protected override void OnPointerCanceled(PointerRoutedEventArgs args)
		{
			base.OnPointerCanceled(args);
			args.Handled = true;
			ReleasePointerCapture(args.Pointer);
			CompleteDrag(args.GetCurrentPoint());
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);
			args.Handled = true;
			ReleasePointerCapture(args.Pointer);
			CompleteDrag(args.GetCurrentPoint());
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs args)
		{
			base.OnPointerMoved(args);
			args.Handled = true;
			if (IsPointerCaptured)
			{
				DeltaDrag(args.GetCurrentPoint());
			}
		}
	}
}