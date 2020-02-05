#if NET461 || __MACOS__
#pragma warning disable CS0067
#endif

using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public sealed partial class Thumb : Control
	{
		public event DragCompletedEventHandler DragCompleted;
		public event DragDeltaEventHandler DragDelta;
		public event DragStartedEventHandler DragStarted;

		#region IsDragging DependencyProperty

		public bool IsDragging
		{
			get { return (bool)GetValue(IsDraggingProperty); }
			set { SetValue(IsDraggingProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsDragging.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsDraggingProperty =
			DependencyProperty.Register("IsDragging", typeof(bool), typeof(Thumb), new PropertyMetadata(false, (s, e) => ((Thumb)s)?.OnIsDraggingChanged(e)));

		private void OnIsDraggingChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		public Thumb()
		{
			// Call Initialise to allow platform-specific code execution 
			Initialize();
		}

		// 0b0[All][Mouse][Pen]
		// this assume that PointerDeviceType.Touch = 0 / Pen = 1 / Mouse = 2
		private int _disableCapturePointers = 0b0000; 

		/// <summary>
		/// Disable capture of all pointer kind
		/// </summary>
		internal void DisablePointersTracking()
			=> _disableCapturePointers = 0b0100;

		/// <summary>
		/// Disable capture for mouse
		/// </summary>
		internal void DisableMouseTracking()
			=> _disableCapturePointers |= (int)PointerDeviceType.Mouse;

		private bool ShouldCapture(PointerDeviceType type)
			=> (_disableCapturePointers & (0b0100 | (int)type)) == 0;

		/// <summary>
		/// Initializes necessary platform-specific components
		/// </summary>
		partial void Initialize();

		public void CancelDrag() { }

		private Point _startLocation;

		internal void StartDrag(Point location)
		{
			_startLocation = location;

			IsDragging = true;
			DragStarted?.Invoke(this, new DragStartedEventArgs(this, 0, 0));
		}

		internal void DeltaDrag(Point location)
		{
			DragDelta?.Invoke(this, new DragDeltaEventArgs(this, location.X - _startLocation.X, location.Y - _startLocation.Y));
		}

		internal void CompleteDrag(Point location)
		{
			IsDragging = false;
			DragCompleted?.Invoke(this, new DragCompletedEventArgs(this, location.X - _startLocation.X, location.Y - _startLocation.Y, false));
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);

			if (!ShouldCapture(args.Pointer.PointerDeviceType))
			{
				return;
			}

			var point = args.GetCurrentPoint(this);
			if (!point.Properties.IsLeftButtonPressed
				|| !CapturePointer(args.Pointer))
			{
				return;
			}

			// Note: We don't handle event as UWP does not, and otherwise it will prevent parent control to update its visual state
			StartDrag(point.Position);
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs args)
		{
			base.OnPointerMoved(args);

			if (IsCaptured(args.Pointer))
			{
				// Note: We don't handle event as UWP does not, and otherwise it will prevent parent control to update its visual state
				DeltaDrag(args.GetCurrentPoint(this).Position);
			}
		}

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
		{
			base.OnPointerCaptureLost(args);

			// Note: We don't handle event as UWP does not, and otherwise it will prevent parent control to update its visual state
			CompleteDrag(args.GetCurrentPoint(this).Position);
		}
	}
}
