#if NET461 || __MACOS__
#pragma warning disable CS0067
#endif

using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Input;
using Uno.Extensions;

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

		internal bool IgnoreTouchInput { get; set; }

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

			DefaultStyleKey = typeof(Thumb);
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

		// Note: We don't use the Manipulation events as we want to handle the PointerPressed event,
		//		 however there is probably no good reason to not use the GestureRecognizer.
		private Point _startLocation, _lastLocation;

		internal void StartDrag(PointerRoutedEventArgs args)
		{
			// Note: Position MUST be absolute as the element might move
			var absoluteLocation = args.GetCurrentPoint(null).Position;

			_startLocation = _lastLocation = absoluteLocation;

			IsDragging = true;

			var handler = DragStarted;
			if (Parent is UIElement elt && handler != null)
			{
				var locationRelativeToParent = args.GetCurrentPoint(elt).Position;
				DragStarted?.Invoke(this, new DragStartedEventArgs(this, locationRelativeToParent.X, locationRelativeToParent.Y));
			}
		}

		internal void DeltaDrag(PointerRoutedEventArgs args)
		{
			// Note: Position MUST be absolute as the element might have moved
			var absoluteLocation = args.GetCurrentPoint(null).Position;
			var deltaX = absoluteLocation.X - _lastLocation.X;
			var deltaY = absoluteLocation.Y - _lastLocation.Y;
			var totalX = absoluteLocation.X - _startLocation.X;
			var totalY = absoluteLocation.Y - _startLocation.Y;

			_lastLocation = absoluteLocation;

			DragDelta?.Invoke(this, new DragDeltaEventArgs(this, deltaX, deltaY, totalX, totalY));
		}

		internal void CompleteDrag(PointerRoutedEventArgs args)
		{
			IsDragging = false;

			// Note: Position MUST be absolute as the element might have moved
			var absoluteLocation = args.GetCurrentPoint(null).Position;
			var deltaX = absoluteLocation.X - _lastLocation.X;
			var deltaY = absoluteLocation.Y - _lastLocation.Y;
			var totalX = absoluteLocation.X - _startLocation.X;
			var totalY = absoluteLocation.Y - _startLocation.Y;

			DragCompleted?.Invoke(this, new DragCompletedEventArgs(this, deltaX, deltaY, totalX, totalY, false));
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);

			if (!ShouldCapture(args.Pointer.PointerDeviceType))
			{
				return;
			}

			var point = args.GetCurrentPoint(null);
			if (!point.Properties.IsLeftButtonPressed
				|| !CapturePointer(args.Pointer))
			{
				return;
			}

			// Note: WinUI handles only the PointerPressed event.
			// Note2: Handling this event causes an issue in parent controls (like ToggleSwitch) that has a pressed visual state.
			//		  In order to fix that, parents controls are expected to also listen for handled events too, and update their visual state in case.
			args.Handled = true;
			StartDrag(args);
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs args)
		{
			base.OnPointerMoved(args);

			if (IsCaptured(args.Pointer))
			{
				// Note: We don't handle event as UWP does not, and otherwise it will prevent parent control to update its visual state
				DeltaDrag(args);
			}
		}

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
		{
			base.OnPointerCaptureLost(args);

			// Note: We don't handle event as UWP does not, and otherwise it will prevent parent control to update its visual state
			CompleteDrag(args);
		}
	}
}
