using System;
using System.Drawing;

#if NET6_0_OR_GREATER
using ObjCRuntime;
#endif

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

namespace Microsoft.UI.Xaml.Controls
{
	[Data.Bindable]
	public partial class WindowView : UIView
	{
		NSObject _didChangeStatusBarOrientationObserver;
		NSObject _didChangeStatusBarFrameObserver;
		bool _disposed;

		public WindowView()
		{

		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!_disposed)
			{
				_disposed = true;
				RemoveObservers();
			}
		}

		public override void MovedToSuperview()
		{
			base.MovedToSuperview();

			if (Superview != null)
			{
				AddObservers();
			}
			else
			{
				RemoveObservers();
			}
		}

		void AddObservers()
		{
			RemoveObservers();
			RotateAccordingToStatusBarOrientationAndSupportedOrientations();

			Action<NSNotification> action = delegate
			{
				RotateAccordingToStatusBarOrientationAndSupportedOrientations();
			};

			_didChangeStatusBarOrientationObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidChangeStatusBarOrientationNotification, action);
			_didChangeStatusBarFrameObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidChangeStatusBarFrameNotification, action);
		}

		void RemoveObservers()
		{
			if (_didChangeStatusBarOrientationObserver != null)
			{
				NSNotificationCenter.DefaultCenter.RemoveObserver(_didChangeStatusBarOrientationObserver);
				_didChangeStatusBarOrientationObserver = null;
			}

			if (_didChangeStatusBarFrameObserver != null)
			{
				NSNotificationCenter.DefaultCenter.RemoveObserver(_didChangeStatusBarFrameObserver);
				_didChangeStatusBarFrameObserver = null;
			}
		}

		void RotateAccordingToStatusBarOrientationAndSupportedOrientations()
		{
			var angle = UIInterfaceOrientationAngleOfOrientation(UIApplication.SharedApplication.StatusBarOrientation);
			var transform = CGAffineTransform.MakeRotation(angle);
			if (!Transform.Equals(transform))
			{
				Transform = transform;
			}

			var frame = WindowBounds;
			if (!Frame.Equals(frame))
			{
				Frame = frame;
			}
		}

		static nfloat StatusBarHeight
		{
			get
			{
				var orientation = UIApplication.SharedApplication.StatusBarOrientation;
				if (orientation == UIInterfaceOrientation.LandscapeLeft || orientation == UIInterfaceOrientation.LandscapeRight)
				{
					return UIApplication.SharedApplication.StatusBarFrame.Width;
				}
				return UIApplication.SharedApplication.StatusBarFrame.Height;
			}
		}

		public CGRect WindowBounds
		{
			get
			{
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

		static float UIInterfaceOrientationAngleOfOrientation(UIInterfaceOrientation orientation)
		{
			float angle = 0f;

			switch (orientation)
			{
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

		static UIInterfaceOrientationMask UIInterfaceOrientationMaskFromOrientation(UIInterfaceOrientation orientation)
		{
			return (UIInterfaceOrientationMask)(1 << (int)orientation);
		}
	}
}

