using System;
using System.Drawing;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
using nint = System.Int32;
using CGSize = System.Drawing.SizeF;
#endif

namespace Windows.UI.Xaml.Controls
{
	[Data.Bindable]
	public partial class WindowView : UIView
	{
		private NSObject _didChangeStatusBarOrientationObserver;
		private NSObject _didChangeStatusBarFrameObserver;
		private bool _disposed;

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			if (!_disposed) {
				_disposed = true;
				RemoveObservers ();
			}
		}

		public override void MovedToSuperview ()
		{
			base.MovedToSuperview ();

			if (Superview != null) {
				AddObservers ();
			} else {
				RemoveObservers ();
			}
		}

		private void AddObservers ()
		{
			RemoveObservers ();
			RotateAccordingToStatusBarOrientationAndSupportedOrientations ();

			Action<NSNotification> action = delegate {
				RotateAccordingToStatusBarOrientationAndSupportedOrientations ();
			};

			_didChangeStatusBarOrientationObserver = NSNotificationCenter.DefaultCenter.AddObserver (UIApplication.DidChangeStatusBarOrientationNotification, action);
			_didChangeStatusBarFrameObserver = NSNotificationCenter.DefaultCenter.AddObserver (UIApplication.DidChangeStatusBarFrameNotification, action);
		}

		private void RemoveObservers ()
		{
			if (_didChangeStatusBarOrientationObserver != null) {
				NSNotificationCenter.DefaultCenter.RemoveObserver (_didChangeStatusBarOrientationObserver);
				_didChangeStatusBarOrientationObserver = null;
			}

			if (_didChangeStatusBarFrameObserver != null) {
				NSNotificationCenter.DefaultCenter.RemoveObserver (_didChangeStatusBarFrameObserver);
				_didChangeStatusBarFrameObserver = null;
			}
		}

		private void RotateAccordingToStatusBarOrientationAndSupportedOrientations ()
		{
			var angle = UIInterfaceOrientationAngleOfOrientation (UIApplication.SharedApplication.StatusBarOrientation);
			var transform = CGAffineTransform.MakeRotation (angle);
			if (!Transform.Equals (transform)) {
				Transform = transform;
			}

			var frame = WindowBounds;
			if (!Frame.Equals (frame)) {
				Frame = frame;
			}
		}

		private static nfloat StatusBarHeight {
			get {
				var orientation = UIApplication.SharedApplication.StatusBarOrientation;
				if (orientation == UIInterfaceOrientation.LandscapeLeft || orientation == UIInterfaceOrientation.LandscapeRight) {
					return UIApplication.SharedApplication.StatusBarFrame.Width;
				}
				return UIApplication.SharedApplication.StatusBarFrame.Height;
			}
		}

		public CGRect WindowBounds {
			get {    
				var orientation = UIApplication.SharedApplication.StatusBarOrientation;
				const float statusBarHeight = 0f; // StatusBarHeight;

				var frame = Window.Bounds;
				frame.X += orientation == UIInterfaceOrientation.LandscapeLeft ? statusBarHeight : 0;
				frame.Y += orientation == UIInterfaceOrientation.Portrait ? statusBarHeight : 0;
				frame.Width -= (orientation == UIInterfaceOrientation.LandscapeLeft || orientation == UIInterfaceOrientation.LandscapeRight) ? statusBarHeight : 0;
				frame.Height -= (orientation == UIInterfaceOrientation.Portrait || orientation == UIInterfaceOrientation.PortraitUpsideDown) ? statusBarHeight : 0;
				return frame;
			}
		}

		private static float UIInterfaceOrientationAngleOfOrientation (UIInterfaceOrientation orientation)
		{
			float angle = 0f;

			switch (orientation) {
			case UIInterfaceOrientation.Portrait:
				break;
			case UIInterfaceOrientation.PortraitUpsideDown:
				angle = (float)Math.PI;
				break;
			case UIInterfaceOrientation.LandscapeLeft:
				angle = -(float)Math.PI * 0.5f;
				break;
			case UIInterfaceOrientation.LandscapeRight:
				angle = (float)Math.PI * 0.5f;
				break;
			}

			return angle;
		}

		private static UIInterfaceOrientationMask UIInterfaceOrientationMaskFromOrientation (UIInterfaceOrientation orientation)
		{
			return (UIInterfaceOrientationMask)(1 << (int)orientation);
		}
	}
}

