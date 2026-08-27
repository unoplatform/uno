using Microsoft.UI.Xaml;

#if HAS_UNO
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Windows.Devices.Input;

#if HAS_UNO_WINUI
using GestureSettings = Microsoft.UI.Input.GestureSettings;
#else
using GestureSettings = Windows.UI.Input.GestureSettings;
#endif
#endif

namespace Uno.UI.Toolkit
{
	/// <summary>
	/// Opt-in behaviors for elements which pan their own content through <see cref="UIElement.ManipulationMode"/>.
	/// </summary>
	public static class ManipulationExtensions
	{
		/// <summary>
		/// When set on an element which pans its content with <see cref="Microsoft.UI.Xaml.Input.ManipulationModes.TranslateInertia"/>, the
		/// press which stops the inertia is consumed as a "stop the momentum" gesture: it raises no
		/// Tapped/DoubleTapped/RightTapped on the element, its ancestors nor its descendants, so a first tap only
		/// stops the coasting content and a second one acts on the pointed item.
		/// </summary>
		/// <remarks>
		/// This is not the WinUI behavior: there, gesture recognition is per-element and independent of the inertia
		/// of an ancestor, so the pointed item is activated by the very press which stops the momentum. It matches
		/// what the OS does for its own scrollable surfaces though, which is what touch users expect on a
		/// touch-only target - hence an opt-in. No-op on WinAppSDK.
		/// </remarks>
		public static DependencyProperty IsTapToStopInertiaEnabledProperty { get; } =
			DependencyProperty.RegisterAttached(
				"IsTapToStopInertiaEnabled",
				typeof(bool),
				typeof(ManipulationExtensions),
				new PropertyMetadata(false, OnIsTapToStopInertiaEnabledChanged));

		public static void SetIsTapToStopInertiaEnabled(this UIElement element, bool isTapToStopInertiaEnabled)
			=> element.SetValue(IsTapToStopInertiaEnabledProperty, isTapToStopInertiaEnabled);

		public static bool GetIsTapToStopInertiaEnabled(this UIElement element)
			=> (bool)element.GetValue(IsTapToStopInertiaEnabledProperty);

#if !HAS_UNO
		private static void OnIsTapToStopInertiaEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
		}
#else
		private const GestureSettings Taps = GestureSettings.Tap
			| GestureSettings.DoubleTap
			| GestureSettings.RightTap
			| GestureSettings.Hold
			| GestureSettings.HoldWithMouse;

		private static readonly DependencyProperty SubscriptionProperty = DependencyProperty.RegisterAttached(
			"Subscription",
			typeof(IDisposable),
			typeof(ManipulationExtensions),
			new PropertyMetadata(default(IDisposable)));

		private static void OnIsTapToStopInertiaEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			if (sender is not UIElement element)
			{
				return;
			}

			(element.GetValue(SubscriptionProperty) as IDisposable)?.Dispose();
			element.SetValue(SubscriptionProperty, args.NewValue is true ? new InertiaStopTracker(element) : null);
		}

		/// <summary>
		/// Mutes the tap gestures of the whole pointer path of the press which stops the inertia of its element.
		/// </summary>
		/// <remarks>
		/// The muting has to be done in two steps because of the order in which the pointer events reach the
		/// gesture recognizers: the ancestors of the original source are visited from the closest to the root, each
		/// updating its own gestures before invoking its handlers, and the original source updates its own gestures
		/// last of all. So on the press only the elements from the original source up to the tracked one have a
		/// gesture to mute, and the rest - the ancestors and the original source - is muted on the release, which
		/// still runs before their own gestures are recognized.
		/// </remarks>
		private sealed class InertiaStopTracker : IDisposable
		{
			private readonly UIElement _element;
			private readonly SerialDisposable _subscriptions = new();
			private readonly HashSet<PointerIdentifier> _pendingPointers = new();

			public InertiaStopTracker(UIElement element)
			{
				_element = element;

				var pressed = new PointerEventHandler(OnPointerPressed);
				var released = new PointerEventHandler(OnPointerReleased);

				element.AddHandler(UIElement.PointerPressedEvent, pressed, handledEventsToo: true);
				element.AddHandler(UIElement.PointerReleasedEvent, released, handledEventsToo: true);
				element.AddHandler(UIElement.PointerCanceledEvent, released, handledEventsToo: true);

				_subscriptions.Disposable = Disposable.Create(() =>
				{
					element.RemoveHandler(UIElement.PointerPressedEvent, pressed);
					element.RemoveHandler(UIElement.PointerReleasedEvent, released);
					element.RemoveHandler(UIElement.PointerCanceledEvent, released);
				});
			}

			private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
			{
				// An element updates its own gestures before invoking its handlers, so the recognizer has already
				// told us whether this press is what aborted the coasting manipulation.
				if (!_element.LastPointerDownStoppedInertia)
				{
					return;
				}

				var pointer = args.Pointer.UniqueId;
				_pendingPointers.Add(pointer);

				// Only the elements from the original source up to this one have a gesture at this point.
				MutePath(args, pointer, upTo: _element);
			}

			private void OnPointerReleased(object sender, PointerRoutedEventArgs args)
			{
				var pointer = args.Pointer.UniqueId;
				if (_pendingPointers.Remove(pointer))
				{
					MutePath(args, pointer, upTo: null);
				}
			}

			private static void MutePath(PointerRoutedEventArgs args, PointerIdentifier pointer, UIElement upTo)
			{
				DependencyObject current = args.OriginalSource as UIElement;
				while (current is not null)
				{
					if (current is UIElement element)
					{
						element.PreventGestures(pointer, Taps);

						if (element == upTo)
						{
							return;
						}
					}

					current = (current as FrameworkElement)?.Parent ?? Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
				}
			}

			public void Dispose() => _subscriptions.Dispose();
		}
#endif
	}
}
