using CoreAnimation;
using CoreGraphics;
using Foundation;
using Uno.Extensions;
using Uno.UI.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Logging;
using Uno;
using Windows.Devices.Input;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml.Media;
using Uno.UI.Extensions;
using Uno.UI;
using AppKit;

namespace Windows.UI.Xaml
{
	public partial class UIElement : BindableNSView
	{
#if DEBUG
		/// <summary>
		/// Provides the ability to disable clipping for an object provided by the selector.
		/// </summary>
		public static Func<object, bool> CanClipSelector { get; set; }
#endif

		internal bool IsPointerCaptured { get; set; }

		#region Logs
		private static readonly ILogger _log = typeof(UIElement).Log();
		private static readonly Action LogRegisterPointerCanceledNotImplemented = Actions.CreateOnce(() => _log.Error("Unable to RegisterPointerCanceled on UIElement for iOS. Not Implemented."));
		private static readonly Action LogUnregisterPointerCanceledNotImplemented = Actions.CreateOnce(() => _log.Error("Unable to UnregisterPointerCanceled on UIElement for iOS. Not Implemented."));
		private static readonly Action LogRegisterPointerExitedNotImplemented = Actions.CreateOnce(() => _log.Error("Unable to RegisterPointerExited on UIElement for iOS. Not Implemented."));
		private static readonly Action LogUnRegisterPointerExitedNotImplemented = Actions.CreateOnce(() => _log.Error("Unable to UnregisterPointerExited on UIElement for iOS. Not Implemented."));
		private static readonly Action LogRegisterPointerPressedNotImplemented = Actions.CreateOnce(() => _log.Error("Unable to RegisterPointerPressed on UIElement for iOS. Not Implemented."));
		private static readonly Action LogUnRegisterPointerPressedNotImplemented = Actions.CreateOnce(() => _log.Error("Unable to UnRegisterPointerPressed on UIElement for iOS. Not Implemented."));
		private static readonly Action LogRegisterPointerReleasedNotImplemented = Actions.CreateOnce(() => _log.Error("Unable to RegisterPointerReleased on UIElement for iOS. Not Implemented."));
		private static readonly Action LogUnRegisterPointerReleasedNotImplemented = Actions.CreateOnce(() => _log.Error("Unable to UnregisterPointerReleased on UIElement for iOS. Not Implemented."));
		#endregion

		public UIElement()
		{
			InitializePointers();
		}

		partial void InitializeCapture();

		partial void EnsureClip(Rect rect)
		{
			if (rect.IsEmpty
				|| double.IsPositiveInfinity(rect.X)
				|| double.IsPositiveInfinity(rect.Y)
				|| double.IsPositiveInfinity(rect.Width)
				|| double.IsPositiveInfinity(rect.Height)
			)
			{
				this.Layer.Mask = null;
				return;
			}
			this.WantsLayer = true;
			this.Layer.Mask = new CAShapeLayer
			{
				Path = CGPath.FromRect(ToCGRect(rect))
			};
		}

		partial void OnOpacityChanged(DependencyPropertyChangedEventArgs args)
		{
			// Don't update the internal value if the value is being animated.
			// The value is being animated by the platform itself.
			if (!(args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation))
			{
				AlphaValue = IsRenderingSuspended ? 0 : (nfloat)Opacity;
			}
		}

		protected virtual void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			var newVisibility = (Visibility)newValue;

			if (base.Hidden != newVisibility.IsHidden())
			{
				base.Hidden = newVisibility.IsHidden();
				base.NeedsLayout = true;			

				if (newVisibility == Visibility.Visible)
				{
					// This recursively invalidates the layout of all subviews
					// to ensure LayoutSubviews is called and views get updated.
					// Failing to do this can cause some views to remain collapsed.
					SetSubviewsNeedLayout();
				}
			}
		}

		public override bool Hidden
		{
			get
			{
				return base.Hidden;
			}
			set
			{
				// Only set the Visility property, the Hidden property is updated 
				// in the property changed handler as there are actions associated with 
				// the change.
				Visibility = value ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		public void SetSubviewsNeedLayout()
		{
			base.NeedsLayout = true;

			if (this is Controls.Panel p)
			{
				// This section is here because of the enumerator type returned by Children,
				// to avoid allocating during the enumeration.
				foreach (var view in p.Children)
				{
					(view as IFrameworkElement)?.SetSubviewsNeedLayout();
				}
			}
			else
			{
				foreach (var view in this.GetChildren())
				{
					(view as IFrameworkElement)?.SetSubviewsNeedLayout();
				}
			}
		}

		internal Windows.Foundation.Point GetPosition(Point position, global::Windows.UI.Xaml.UIElement relativeTo)
		{
#if __IOS__
			return relativeTo.ConvertPointToCoordinateSpace(position, relativeTo);
#elif __MACOS__
			throw new NotImplementedException();
#endif
		}

		private CGRect ToCGRect(Rect rect)
		{
			return new CGRect
			(
				(nfloat)(rect.X),
				(nfloat)(rect.Y),
				(nfloat)(rect.Width),
				(nfloat)(rect.Height)
			);
		}

		public GeneralTransform TransformToVisual(UIElement visual)
		{
			// If visual is null, we transform the element to the window
			if (visual == null)
			{
				visual = Xaml.Window.Current.Content;
			}

			var unit = new CGRect(0, 0, 1, 1);
			var transformed = visual.ConvertRectFromView(unit, this);

			// TODO: UWP returns a MatrixTransform here. For now TransformToVisual doesn't support rotations, scalings, etc.
			return new TranslateTransform { X = transformed.X, Y = transformed.Y };
		}


		/// <summary>
		/// Gets the parent view for the <paramref name="owner"/> which clips its content.
		/// </summary>
		/// <returns>A tuple of the clipping parent, and the view that let to this parent.</returns>
		private static (NSView child, NSView clippingParent) GetClippingParent(NSView owner)
		{
			(NSView child, NSView clippingParent) GetClippingParent(NSView child, NSView parent)
			{
				if (parent is FrameworkElement pfe)
				{
					if (!pfe.ClipChildrenToBounds)
					{
						return GetClippingParent(pfe, pfe.Superview);
					}
					else
					{
						return (child, parent);
					}
				}
				else
				{
					return (child, parent);
				}
			}


			if (owner.Superview is FrameworkElement sfe && !sfe.ClipChildrenToBounds)
			{
				return GetClippingParent(owner, owner.Superview);
			}

			return (owner, owner.Superview);
		}

		/// <summary>
		/// Gets the origin point of the <paramref name="view"/> in the clippingParent's 
		/// coordinate system.
		/// </summary>
		/// <param name="view">The view to get the point from</param>
		/// <param name="parentView">The view for which to get the adjusted coordinates from</param>
		/// <returns></returns>
		private static CGPoint ConvertOriginPointToView(NSView view, NSView parentView)
		{
			var value = CGPoint.Empty;
			var current = view;

			do
			{
				if (current is FrameworkElement fr)
				{
					value.X += (nfloat)fr.AppliedFrame.X;
					value.Y += (nfloat)fr.AppliedFrame.Y;
				}
				else
				{
					value.X += current.Frame.X;
					value.Y += current.Frame.Y;
				}

				current = current.Superview;

			} while (current != null && current != parentView);

			return value;
		}

#region DoubleTapped event
		private void RegisterDoubleTapped(DoubleTappedEventHandler handler)
		{
			LogRegisterPointerCanceledNotImplemented();
		}

		private void UnregisterDoubleTapped(DoubleTappedEventHandler handler)
		{
			LogRegisterPointerCanceledNotImplemented();
		}
#endregion

#region PointerCanceled event
		private void RegisterPointerCanceled(PointerEventHandler handler)
		{
			LogRegisterPointerCanceledNotImplemented();
		}

		private void UnregisterPointerCanceled(PointerEventHandler handler)
		{
			LogUnregisterPointerCanceledNotImplemented();
		}
#endregion

#region PointerExited event
		private void RegisterPointerExited(PointerEventHandler handler)
		{
			LogRegisterPointerExitedNotImplemented();
		}

		private void UnregisterPointerExited(PointerEventHandler handler)
		{
			LogUnRegisterPointerExitedNotImplemented();
		}
#endregion

#region PointerPressed event
		private void RegisterPointerPressed(PointerEventHandler handler)
		{
			LogRegisterPointerPressedNotImplemented();
		}

		private void UnregisterPointerPressed(PointerEventHandler handler)
		{
			LogUnRegisterPointerPressedNotImplemented();
		}
#endregion

#region PointerReleased event
		private void RegisterPointerReleased(PointerEventHandler handler)
		{
			LogRegisterPointerReleasedNotImplemented();
		}

		private void UnregisterPointerReleased(PointerEventHandler handler)
		{
			LogUnRegisterPointerReleasedNotImplemented();
		}
#endregion

#region Tapped event
		private void RegisterTapped(TappedEventHandler handler)
		{
			LogRegisterPointerReleasedNotImplemented();
		}

		private void UnregisterTapped(TappedEventHandler handler)
		{
			LogUnRegisterPointerReleasedNotImplemented();
		}
#endregion

		internal void RaiseTapped(TappedRoutedEventArgs args) => LogUnRegisterPointerReleasedNotImplemented();

		public override void MouseDown(NSEvent evt)
		{
			try
			{
				var pointerEventIsHandledInManaged = false;

				if (evt.IsTouchInView(this))
				{
					IsPointerPressed = true;
					IsPointerOver = true;

					// evt.AllTouches raises a invalid selector exception
					var args = new PointerRoutedEventArgs(null, evt)
					{
						CanBubbleNatively = true,
						OriginalSource = this
					};

					pointerEventIsHandledInManaged = RaiseEvent(PointerEnteredEvent, args);

					args.Handled = false; // reset for "pressed" event

					pointerEventIsHandledInManaged = RaiseEvent(PointerPressedEvent, args) || pointerEventIsHandledInManaged;
				}

				if (!pointerEventIsHandledInManaged)
				{
					// Bubble up the event natively
					base.MouseDown(evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void MouseUp(NSEvent evt)
		{
			try
			{
				var wasPointerOver = IsPointerOver;
				IsPointerOver = evt.IsTouchInView(this);

				// Call entered/exited one last time
				// evt.AllTouches raises a invalid selector exception
				var args = new PointerRoutedEventArgs(null, evt)
				{
					CanBubbleNatively = true,
					OriginalSource = this
				};

				var pointerEventIsHandledInManaged = false;

				if (!wasPointerOver && IsPointerOver)
				{
					pointerEventIsHandledInManaged = RaiseEvent(PointerEnteredEvent, args);
				}
				else if (wasPointerOver && !IsPointerOver)
				{
					pointerEventIsHandledInManaged = RaiseEvent(PointerExitedEvent, args);
				}

				if (IsPointerCaptured || IsPointerOver)
				{
					args.Handled = false; // reset as unhandled
					pointerEventIsHandledInManaged = RaiseEvent(PointerReleasedEvent, args) || pointerEventIsHandledInManaged;
				}

				if (IsPointerCaptured)
				{
					args.Handled = false; // reset as unhandled
					pointerEventIsHandledInManaged = RaiseEvent(PointerCaptureLostEvent, args) || pointerEventIsHandledInManaged;
				}

				if (!pointerEventIsHandledInManaged)
				{
					// Bubble up the event natively
					base.MouseUp(evt);
				}

				IsPointerPressed = false;
				IsPointerOver = false;
				_pointCaptures.Clear();
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void MouseDragged(NSEvent evt)
		{
			try
			{
				var wasPointerOver = IsPointerOver;
				IsPointerOver = evt.IsTouchInView(this);

				var pointerEventIsHandledInManaged = false;

				// evt.AllTouches raises a invalid selector exception
				var args = new PointerRoutedEventArgs(null, evt)
				{
					CanBubbleNatively = true,
					OriginalSource = this
				};

				if (IsPointerCaptured || IsPointerOver)
				{
					pointerEventIsHandledInManaged = RaiseEvent(PointerMovedEvent, args);
				}

				if (!wasPointerOver && IsPointerOver)
				{
					args.Handled = false; // reset as unhandled
					pointerEventIsHandledInManaged = RaiseEvent(PointerEnteredEvent, args) || pointerEventIsHandledInManaged;
				}
				else if (wasPointerOver && !IsPointerOver)
				{
					args.Handled = false; // reset as unhandled
					pointerEventIsHandledInManaged = RaiseEvent(PointerExitedEvent, args) || pointerEventIsHandledInManaged;
				}

				if (!pointerEventIsHandledInManaged)
				{
					// Bubble up the event natively
					base.MouseDragged(evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}
	}
}
