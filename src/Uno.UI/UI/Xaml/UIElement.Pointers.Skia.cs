#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Uno.Foundation.Extensibility;
using Windows.UI.Core;
using Windows.Foundation;
using System.Threading;
using System.Numerics;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		private class PointerManager
		{
			public PointerManager()
			{
				Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
				Window.Current.CoreWindow.PointerEntered += CoreWindow_PointerEntered;
				Window.Current.CoreWindow.PointerExited += CoreWindow_PointerExited;
				Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
				Window.Current.CoreWindow.PointerReleased += CoreWindow_PointerReleased;
				Window.Current.CoreWindow.PointerWheelChanged += CoreWindow_PointerWheelChanged;
			}

			private void CoreWindow_PointerWheelChanged(CoreWindow sender, PointerEventArgs args)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerWheelChanged ({args.CurrentPoint.Position})");
				}

				PropagateEvent(args, e =>
				{
					var pointer = new Pointer(args.CurrentPoint.PointerId, PointerDeviceType.Mouse, false, isInRange: true);
					var pointerArgs = new PointerRoutedEventArgs(args, e);
					e.OnNativePointerWheel(pointerArgs);
				});
			}

			private void CoreWindow_PointerEntered(CoreWindow sender, PointerEventArgs args)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerEntered ({args.CurrentPoint.Position})");
				}
			}

			private void CoreWindow_PointerExited(CoreWindow sender, PointerEventArgs args)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerExited ({args.CurrentPoint.Position})");
				}


			}

			private void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerReleased ({args.CurrentPoint.Position})");
				}

				var pointer = new Pointer(args.CurrentPoint.PointerId, PointerDeviceType.Mouse, false, isInRange: true);
				if (UIElement.PointerCapture.TryGet(pointer, out var capture))
				{
					foreach(var target in capture.Targets.ToArray())
					{
						var pointerArgs = new PointerRoutedEventArgs(args, target.Element);
						target.Element.OnNativePointerUp(pointerArgs);
					}
				}
				else
				{
					PropagateEvent(args, e =>
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"PointerManager.Released [{e}/{e.GetHashCode():X8}");
						}

						var pointerArgs = new PointerRoutedEventArgs(args, e);

						TraverseAncestors(e, e => e.OnNativePointerUp(pointerArgs));
					});
				}
			}

			private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerPressed ({args.CurrentPoint.Position})");
				}

				PropagateEvent(args, e =>
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"PointerManager.Pressed [{e}/{e.GetHashCode():X8}]");
					}

					var pointer = new Pointer(args.CurrentPoint.PointerId, PointerDeviceType.Mouse, false, isInRange: true);
					var pointerArgs = new PointerRoutedEventArgs(args, e);

					TraverseAncestors(e, e => e.OnNativePointerDown(pointerArgs));
				});
			}

			private void TraverseAncestors(UIElement element, Func<UIElement, bool> action)
			{
				if (!action(element))
				{
					foreach (var parent in element.GetParents().OfType<UIElement>())
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"TraverseAncestors for [{element}/{element.GetHashCode():X8}] = {parent}/{parent.GetHashCode():X8}");
						}

						if (action(parent))
						{
							return;
						}
					}
				}
			}

			private void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"CoreWindow_PointerMoved ({args.CurrentPoint.Position})");
				}

				var pointer = new Pointer(args.CurrentPoint.PointerId, PointerDeviceType.Mouse, false, isInRange: true);

				if (UIElement.PointerCapture.TryGet(pointer, out var capture))
				{
					foreach (var target in capture.Targets)
					{
						var pointerArgs = new PointerRoutedEventArgs(args, target.Element);

						target.Element.OnNativePointerMove(pointerArgs);
					}
				}
				else
				{
					PropagateEvent(args, e =>
					{
						var pointerArgs = new PointerRoutedEventArgs(args, e);
						e.OnNativePointerMove(pointerArgs);
					});
				}
			}

			private void PropagateEvent(PointerEventArgs args, Action<UIElement> raiseEvent)
			{
				if(Window.Current.RootElement is UIElement root)
				{
					PropagageEventRecursive(args, new Point(0, 0), root, Matrix3x2.Identity, raiseEvent);
				}
			}

			private bool PropagageEventRecursive(PointerEventArgs args, Point root, UIElement element, Matrix3x2 currentTransform, Action<UIElement> raiseEvent)
			{
				bool raised = false;
				var elementRect = element.LayoutSlotWithMarginsAndAlignments;
				elementRect.X += root.X;
				elementRect.Y += root.Y;

				var position = args.CurrentPoint.Position;
				var pointer = new Pointer(args.CurrentPoint.PointerId, PointerDeviceType.Mouse, false, isInRange: true);
				var pointerArgs = new PointerRoutedEventArgs(args, element);

				if (element.RenderTransform != null)
				{
					currentTransform *= element.RenderTransform.MatrixCore;
					elementRect = currentTransform.Transform(elementRect);
				}

				if (elementRect.Contains(position))
				{
					foreach (var e in element.GetChildren().Reverse().ToArray())
					{
						if(PropagageEventRecursive(args, elementRect.Location, e, currentTransform, raiseEvent))
						{
							return true;
						}
					}

					var isHitTestVisible =
						element.GetValue(HitTestVisibilityProperty) is HitTestVisibility hitTestVisibility
						&& hitTestVisibility == HitTestVisibility.Visible;

					if (isHitTestVisible)
					{
						if (!element.IsOver(pointer))
						{
							element.OnNativePointerEnter(pointerArgs);
						}

						raiseEvent(element);
						return true;
					}
				}
				else
				{
					bool RecursePointerExited(UIElement e)
					{
						if (e.IsOver(pointer))
						{
							foreach(var child in e.GetChildren().Reverse().ToArray())
							{
								if (RecursePointerExited(child))
								{
									return true;
								}
							}

							e.OnNativePointerExited(pointerArgs);
							return true;
						}

						return false;
					}

					RecursePointerExited(element);
				}

				return raised;
			}
		}

		// TODO Should be per CoreWindow
		private static PointerManager _pointerManager;

		partial void InitializePointersPartial()
		{
			if (_pointerManager == null)
			{
				_pointerManager = new PointerManager();
			}
		}

		partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{

		}


		#region HitTestVisibility
		internal void UpdateHitTest()
		{
			this.CoerceValue(HitTestVisibilityProperty);
		}

		private enum HitTestVisibility
		{
			/// <summary>
			/// The element and its children can't be targeted by hit-testing.
			/// </summary>
			/// <remarks>
			/// This occurs when IsHitTestVisible="False", IsEnabled="False", or Visibility="Collapsed".
			/// </remarks>
			Collapsed,

			/// <summary>
			/// The element can't be targeted by hit-testing.
			/// </summary>
			/// <remarks>
			/// This usually occurs if an element doesn't have a Background/Fill.
			/// </remarks>
			Invisible,

			/// <summary>
			/// The element can be targeted by hit-testing.
			/// </summary>
			Visible,
		}

		/// <summary>
		/// Represents the final calculated hit-test visibility of the element.
		/// </summary>
		/// <remarks>
		/// This property should never be directly set, and its value should always be calculated through coercion (see <see cref="CoerceHitTestVisibility(DependencyObject, object, bool)"/>.
		/// </remarks>
		private static readonly DependencyProperty HitTestVisibilityProperty =
			DependencyProperty.Register(
				"HitTestVisibility",
				typeof(HitTestVisibility),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					HitTestVisibility.Visible,
					FrameworkPropertyMetadataOptions.Inherits,
					coerceValueCallback: (s, e) => CoerceHitTestVisibility(s, e),
					propertyChangedCallback: (s, e) => OnHitTestVisibilityChanged(s, e)
				)
			);

		/// <summary>
		/// This calculates the final hit-test visibility of an element.
		/// </summary>
		/// <returns></returns>
		private static object CoerceHitTestVisibility(DependencyObject dependencyObject, object baseValue)
		{
			var element = (UIElement)dependencyObject;

			// The HitTestVisibilityProperty is never set directly. This means that baseValue is always the result of the parent's CoerceHitTestVisibility.
			var baseHitTestVisibility = (HitTestVisibility)baseValue;

			// If the parent is collapsed, we should be collapsed as well. This takes priority over everything else, even if we would be visible otherwise.
			if (baseHitTestVisibility == HitTestVisibility.Collapsed)
			{
				return HitTestVisibility.Collapsed;
			}

			// If we're not locally hit-test visible, visible, or enabled, we should be collapsed. Our children will be collapsed as well.
			if (!element.IsHitTestVisible || element.Visibility != Visibility.Visible || !element.IsEnabledOverride())
			{
				return HitTestVisibility.Collapsed;
			}

			// If we're not hit (usually means we don't have a Background/Fill), we're invisible. Our children will be visible or not, depending on their state.
			if (!element.IsViewHit())
			{
				return HitTestVisibility.Invisible;
			}

			// If we're not collapsed or invisible, we can be targeted by hit-testing. This means that we can be the source of pointer events.
			return HitTestVisibility.Visible;
		}

		private static void OnHitTestVisibilityChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
		}
		#endregion

	}
}
