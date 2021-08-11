using System;
using System.Linq;
using Windows.UI.Xaml;
using Foundation;
using UIKit;

namespace Uno.UI.Xaml.Input
{
	/// <summary>
	/// A pseudo (discrete) gesture recognizer used to forward pointer events received on a native only view
	/// to its managed logical <see cref="FrameworkElement.Parent"/>.
	/// </summary>
	/// <remarks>
	/// This is used for native views which has a gesture recognizer which request to not forward events to its parents (a.k.a. super views).
	/// It's not the case for all native views, but only few specific views that have a custom pointers handling, like UIScrollView.
	/// </remarks>
	/// <remarks>
	/// This will **NOT** apply any offset on the touches coordinates.
	/// It means that the native view to which is associated this pseudo recognizer is expected to be fully stretched in its logical parent's bounds.
	/// </remarks>
	/// <remarks>
	/// This is a discrete gesture recognizer, that means it won't prevent other gesture recognizers registered on the same view to kick-in.
	/// This recognizer will actually always stay in the <see cref="UIGestureRecognizerState.Possible"/> state
	/// (except in case of <see cref="UIGestureRecognizer.TouchesCancelled"/> where it will go in <see cref="UIGestureRecognizerState.Cancelled"/> as required by Apple's documentation).
	/// </remarks>
	public class PassThroughPseudoGestureRecognizer : UIGestureRecognizer
	{
		private readonly UIView _nativeView;

		private UIElement _target;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nativeView">The native **only** view (i.e. must not inherit from UIElement)</param>
		public PassThroughPseudoGestureRecognizer(UIView nativeView)
		{
			if (nativeView is UIElement)
			{
				throw new ArgumentException("The nativeView must not be a UIElement");
			}

			_nativeView = nativeView;
		}

		/// <inheritdoc />
		public override void TouchesBegan(NSSet touches, UIEvent evt)
		{
			// We wait for the first touches to get the parent so we don't have to track Loaded/UnLoaded
			// Like native dispatch on iOS, we do "implicit captures" the target.
			if (_nativeView.GetParent() is UIElement parent)
			{
				_target = parent;
				_target.TouchesBegan(touches, evt);
			}

			base.TouchesBegan(touches, evt);
		}

		/// <inheritdoc />
		public override void TouchesMoved(NSSet touches, UIEvent evt)
		{
			_target?.TouchesMoved(touches, evt);

			base.TouchesMoved(touches, evt);
		}

		/// <inheritdoc />
		public override void TouchesEnded(NSSet touches, UIEvent evt)
		{
			_target?.TouchesEnded(touches, evt);
			_target = null;

			base.TouchesEnded(touches, evt);
		}

		/// <inheritdoc />
		public override void TouchesCancelled(NSSet touches, UIEvent evt)
		{
			_target?.TouchesCancelled(touches, evt);
			_target = null;

			State = UIGestureRecognizerState.Cancelled; // Only to be a fair player with the OS.

			base.TouchesCancelled(touches, evt);
		}
	}
}
