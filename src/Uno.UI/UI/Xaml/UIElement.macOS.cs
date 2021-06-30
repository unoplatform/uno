using Uno.UI.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.System;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Extensions;
using AppKit;
using CoreAnimation;
using CoreGraphics;

namespace Windows.UI.Xaml
{
	public partial class UIElement : BindableNSView
	{
		public UIElement()
		{
			Initialize();
			InitializePointers();
		}

		/// <summary>
		/// Determines if InvalidateMeasure has been called
		/// </summary>
		internal bool IsMeasureDirty { get; private protected set; }

		/// <summary>
		/// Determines if InvalidateArrange has been called
		/// </summary>
		internal bool IsArrangeDirty { get; private protected set; }

		internal bool ClippingIsSetByCornerRadius { get; set; } = false;

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
			get => base.Hidden;
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

		/// <inheritdoc />
		public override bool AcceptsFirstResponder()
			=> true; // This is required to receive the KeyDown / KeyUp. Note: Key events are then bubble in managed.

		private protected override void OnNativeKeyDown(NSEvent evt)
		{
			var args = new KeyRoutedEventArgs(this, VirtualKeyHelper.FromKeyCode(evt.KeyCode))
			{
				CanBubbleNatively = false // Only the first responder gets the event
			};

			RaiseEvent(KeyDownEvent, args);

			base.OnNativeKeyDown(evt);
		}

		private bool TryGetParentUIElementForTransformToVisual(out UIElement parentElement, ref double offsetX, ref double offsetY)
		{
			var parent = this.GetVisualTreeParent();
			switch (parent)
			{
				// First we try the direct parent, if it's from the known type we won't even have to adjust offsets

				case UIElement elt:
					parentElement = elt;
					return true;

				case null:
					parentElement = null;
					return false;

				case NSView view:
					do
					{
						parent = parent?.GetVisualTreeParent();

						switch (parent)
						{
							case UIElement eltParent:
								// We found a UIElement in the parent hierarchy, we compute the X/Y offset between the
								// first parent 'view' and this 'elt', and return it.

								var offset = view?.ConvertPointToView(default, eltParent) ?? default;

								parentElement = eltParent;
								offsetX += offset.X;
								offsetY += offset.Y;
								return true;

							case null:
								// We reached the top of the window without any UIElement in the hierarchy,
								// so we adjust offsets using the X/Y position of the original 'view' in the window.

								offset = view.ConvertRectToView(default, null).Location;

								parentElement = null;
								offsetX += offset.X;
								offsetY += offset.Y;
								return false;
						}
					} while (true);
			}
		}

		private protected override void OnNativeKeyUp(NSEvent evt)
		{
			var args = new KeyRoutedEventArgs(this, VirtualKeyHelper.FromKeyCode(evt.KeyCode))
			{
				CanBubbleNatively = false // Only the first responder gets the event
			};

			RaiseEvent(KeyUpEvent, args);

			base.OnNativeKeyUp(evt);
		}

		partial void ApplyNativeClip(Rect rect)
		{
			if (rect.IsEmpty
				|| double.IsPositiveInfinity(rect.X)
				|| double.IsPositiveInfinity(rect.Y)
				|| double.IsPositiveInfinity(rect.Width)
				|| double.IsPositiveInfinity(rect.Height)
			)
			{
				if (!ClippingIsSetByCornerRadius)
				{
					if (Layer != null)
					{
						this.Layer.Mask = null;
					}
				}
				return;
			}

			WantsLayer = true;
			if (Layer != null)
			{
				this.Layer.Mask = new CAShapeLayer
				{
					Path = CGPath.FromRect(rect.ToCGRect())
				};
			}
		}
	}
}
