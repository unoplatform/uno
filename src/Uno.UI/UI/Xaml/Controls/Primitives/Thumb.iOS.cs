#if __IOS__
using System;
using CoreGraphics;
using Foundation;
using UIKit;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Xaml.Controls.Primitives
{
	public sealed partial class Thumb : Control
	{
		private CGPoint _startlocation;

		partial void Initialize()
		{
			// Add the gesture recognizer necessary for touch event captures on iOS
			AddGestureRecognizer(new ThumbGestureRecognizer(this));
		}

		public override void TouchesEnded(NSSet touches, UIEvent evt)
		{
			try
			{
				var touch = (touches.AnyObject as UITouch);
				var location = touch.LocationInView(Superview);
				CompleteDrag(location);

				base.TouchesEnded(touches, evt);
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		internal void CompleteDrag(CGPoint location)
		{
			IsDragging = false;
			DragCompleted?.Invoke(this, new DragCompletedEventArgs(location.X - _startlocation.X, location.Y - _startlocation.Y, false));
		}

		public override void TouchesBegan(NSSet touches, UIEvent evt)
		{
			try
			{
				var touch = (touches.AnyObject as UITouch);
				var location = touch.LocationInView(Superview);
				StartDrag(location);

				base.TouchesBegan(touches, evt);
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		internal void StartDrag(CGPoint location)
		{
			_startlocation = location;

			IsDragging = true;
			DragStarted?.Invoke(this, new DragStartedEventArgs(0, 0));
		}

		public override void TouchesMoved(NSSet touches, UIEvent evt)
		{
			try
			{
				var touch = (touches.AnyObject as UITouch);
				var location = touch.LocationInView(Superview);
				DeltaDrag(location);

				base.TouchesMoved(touches, evt);
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		internal void DeltaDrag(CGPoint location)
		{
			DragDelta?.Invoke(this, new DragDeltaEventArgs(location.X - _startlocation.X, location.Y - _startlocation.Y));
		}

		/// <summary>
		/// This GestureRecognizer is used to eat the touch events sent to the Thumb. Without such a system,
		/// the events can be captured by a ScrollView if wrapped around a Thumb.
		/// 
		/// This GestureRecognizer forwards the Touches events to the relevant Touches events in SliderContainer
		/// while preventing capture by other views.
		/// </summary>
		private class ThumbGestureRecognizer : UIKit.UIGestureRecognizer
		{
			private Thumb _thumb;

			public ThumbGestureRecognizer(Thumb thumb)
			{
				this._thumb = thumb;
			}

			public override void TouchesBegan(NSSet touches, UIEvent evt)
			{
				try
				{
					base.TouchesBegan(touches, evt);
					State = UIGestureRecognizerState.Began;

					this._thumb.TouchesBegan(touches, evt);
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}

			public override void TouchesMoved(NSSet touches, UIEvent evt)
			{
				try
				{
					base.TouchesMoved(touches, evt);
					State = UIGestureRecognizerState.Changed;

					this._thumb.TouchesMoved(touches, evt);
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}

			public override void TouchesEnded(NSSet touches, UIEvent evt)
			{
				try
				{
					base.TouchesEnded(touches, evt);
					State = UIGestureRecognizerState.Ended;

					this._thumb.TouchesEnded(touches, evt);
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}

			public override void TouchesCancelled(NSSet touches, UIEvent evt)
			{
				try
				{
					base.TouchesCancelled(touches, evt);
					State = UIGestureRecognizerState.Cancelled;

					this._thumb.TouchesCancelled(touches, evt);
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledException(e);
				}
			}
		}
	}
}
#endif