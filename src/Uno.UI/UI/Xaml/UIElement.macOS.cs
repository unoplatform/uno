using Uno.UI.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.System;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Extensions;
using AppKit;
using CoreAnimation;
using CoreGraphics;
using ObjCRuntime;

namespace Windows.UI.Xaml
{
	public partial class UIElement : BindableNSView
	{
		public UIElement()
		{
			Initialize();
			InitializePointers();

			UpdateHitTest();
		}

		internal bool IsMeasureDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => false; // Not implemented on macOS yet
		}

		internal bool IsArrangeDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => false; // Not implemented on macOS yet
		}

		internal bool ClippingIsSetByCornerRadius { get; set; }

		partial void OnOpacityChanged(DependencyPropertyChangedEventArgs args)
		{
			// Don't update the internal value if the value is being animated.
			// The value is being animated by the platform itself.
			if (!(args.NewPrecedence == DependencyPropertyValuePrecedences.Animations && args.BypassesPropagation))
			{
				AlphaValue = IsRenderingSuspended ? 0 : (nfloat)Opacity;
			}
		}

		partial void OnIsHitTestVisibleChangedPartial(bool oldValue, bool newValue)
		{
			UpdateHitTest();
		}

		partial void OnVisibilityChangedPartial(Visibility oldValue, Visibility newValue)
		{
			UpdateHitTest();

			var isNewVisibilityHidden = newValue.IsHidden();

			if (base.Hidden == isNewVisibilityHidden)
			{
				return;
			}

			base.Hidden = isNewVisibilityHidden;
			InvalidateMeasure();

			if (isNewVisibilityHidden)
			{
				return;
			}

			// This recursively invalidates the layout of all subviews
			// to ensure LayoutSubviews is called and views get updated.
			// Failing to do this can cause some views to remain collapsed.
			SetSubviewsNeedLayout();
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

		internal global::Windows.Foundation.Point GetPosition(Point position, global::Windows.UI.Xaml.UIElement relativeTo)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public override bool AcceptsFirstResponder()
			=> true; // This is required to receive the KeyDown / KeyUp. Note: Key events are then bubble in managed.

		private protected override void OnNativeKeyDown(NSEvent evt)
		{
			var args = new KeyRoutedEventArgs(this, VirtualKeyHelper.FromKeyCode(evt.KeyCode), VirtualKeyHelper.FromFlagsToVirtualModifiers(evt.ModifierFlags))
			{
				CanBubbleNatively = false // Only the first responder gets the event
			};

			RaiseEvent(KeyDownEvent, args);

			base.OnNativeKeyDown(evt);
		}

		private NSEventModifierMask _lastFlags;

		private protected override void OnNativeFlagsChanged(NSEvent evt)
		{
			var newFlags = evt.ModifierFlags;
			var modifiers = VirtualKeyHelper.FromFlagsToVirtualModifiers(newFlags);

			var flags = Enum.GetValues<NSEventModifierMask>();
			foreach (var flag in flags)
			{
				var key = VirtualKeyHelper.FromFlagsToKey(flag);
				if (key == null)
				{
					continue;
				}

				var raiseKeyDown = CheckFlagKeyDown(flag, newFlags);
				var raiseKeyUp = CheckFlagKeyUp(flag, newFlags);

				if (raiseKeyDown || raiseKeyUp)
				{
					var args = new KeyRoutedEventArgs(this, key.Value, modifiers)
					{
						CanBubbleNatively = false // Only the first responder gets the event
					};

					if (raiseKeyDown)
					{
						RaiseEvent(KeyDownEvent, args);
					}

					if (raiseKeyUp)
					{
						RaiseEvent(KeyUpEvent, args);
					}
				}
			}

			_lastFlags = newFlags;
		}

		private bool CheckFlagKeyUp(NSEventModifierMask flag, NSEventModifierMask newMask) => _lastFlags.HasFlag(flag) && !newMask.HasFlag(flag);

		private bool CheckFlagKeyDown(NSEventModifierMask flag, NSEventModifierMask newMask) => !_lastFlags.HasFlag(flag) && newMask.HasFlag(flag);

		private bool TryGetParentUIElementForTransformToVisual(out UIElement parentElement, ref Matrix3x2 matrix)
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
								matrix.M31 += (float)offset.X;
								matrix.M32 += (float)offset.Y;
								return true;

							case null:
								// We reached the top of the window without any UIElement in the hierarchy,
								// so we adjust offsets using the X/Y position of the original 'view' in the window.

								offset = view.ConvertRectToView(default, null).Location;

								parentElement = null;
								matrix.M31 += (float)offset.X;
								matrix.M32 += (float)offset.Y;
								return false;
						}
					} while (true);
			}
		}

		private protected override void OnNativeKeyUp(NSEvent evt)
		{
			var args = new KeyRoutedEventArgs(this, VirtualKeyHelper.FromKeyCode(evt.KeyCode), VirtualKeyHelper.FromFlagsToVirtualModifiers(evt.ModifierFlags))
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
					var emptyClipLayer = Layer;
					if (emptyClipLayer != null)
					{
						emptyClipLayer.Mask = null;
					}
				}
				return;
			}

			WantsLayer = true;
			var layer = Layer;
			if (layer != null)
			{
				layer.Mask = new CAShapeLayer
				{
					Path = CGPath.FromRect(rect.ToCGRect())
				};
			}
		}
	}
}
