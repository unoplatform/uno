#if NET46 || __MACOS__
#pragma warning disable CS0067
#endif

using System;
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

		internal bool ShouldCapturePointer { get; set; } = true;

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
			if (ShouldCapturePointer)
			{
				CapturePointer(args.Pointer);
			}
			StartDrag(args.GetCurrentPoint(this).Position);
		}

		protected override void OnPointerCanceled(PointerRoutedEventArgs args)
		{
			base.OnPointerCanceled(args);
			args.Handled = true;
			if (ShouldCapturePointer)
			{
				ReleasePointerCapture(args.Pointer);
			}
			CompleteDrag(args.GetCurrentPoint(this).Position);
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);
			args.Handled = true;
			if (ShouldCapturePointer)
			{
				ReleasePointerCapture(args.Pointer);
			}
			CompleteDrag(args.GetCurrentPoint(this).Position);
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs args)
		{
			base.OnPointerMoved(args);
			args.Handled = true;
			if (ShouldCapturePointer && IsPointerCaptured)
			{
				DeltaDrag(args.GetCurrentPoint(this).Position);
			}
		}
	}
}
