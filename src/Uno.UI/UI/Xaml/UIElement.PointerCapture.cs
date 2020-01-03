using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		/*
		 *	This partial file contains only the helper classes for the pointer capture handling which is located in the "Pointers" file.
		 *	Those classes are in UIElement only to get access to the partial native capture APIs.
		 *	
		 *	This file does not implements any partial API, but is using:
		 *		partial void CapturePointerNative(Pointer pointer);
		 *		partial void ReleasePointerNative(Pointer pointer);
		 */

		[Flags]
		private enum PointerCaptureKind : byte
		{
			None = 0,

			Explicit = 1,
			Implicit = 2,

			Any = Explicit | Implicit,
		}

		private class PointerCapture
		{
			private static readonly IDictionary<Pointer, PointerCapture> _actives = new Dictionary<Pointer, PointerCapture>(EqualityComparer<Pointer>.Default);

			/// <summary>
			/// Current currently active pointer capture for the given pointer, or creates a new one.
			/// </summary>
			/// <param name="pointer">The pointer to capture</param>
			public static PointerCapture GetOrCreate(Pointer pointer)
				=> _actives.TryGetValue(pointer, out var capture)
					? capture
					: new PointerCapture(pointer); // The capture will be added to the _actives only when a target is added to it.

			public static bool TryGet(Pointer pointer, out PointerCapture capture)
				=> _actives.TryGetValue(pointer, out capture);

			public static bool Any(out IEnumerable<PointerCapture> cloneOfAllCaptures)
			{
				if (_actives.Any())
				{
					cloneOfAllCaptures = _actives.Values.ToList();
					return true;
				}
				else
				{
					cloneOfAllCaptures = default;
					return false;
				}
			}

			private UIElement _nativeCaptureElement;
			private readonly Dictionary<UIElement, PointerCaptureTarget> _targets = new Dictionary<UIElement, PointerCaptureTarget>(2);

			private PointerCapture(Pointer pointer)
			{
				Pointer = pointer;
			}

			/// <summary>
			/// The captured pointer
			/// </summary>
			public Pointer Pointer { get; }

			/// <summary>
			/// Gets the <see cref="PointerRoutedEventArgs.FrameId"/> of the last args that has been handled by this capture
			/// </summary>
			public long MostRecentDispatchedEventFrameId { get; private set; }

			public IEnumerable<PointerCaptureTarget> Targets => _targets.Values;

			public bool IsTarget(UIElement element, PointerCaptureKind kinds)
				=> _targets.TryGetValue(element, out var target)
					&& (target.Kind & kinds) != PointerCaptureKind.None;

			public bool TryAddTarget(UIElement element, PointerCaptureKind kind, PointerRoutedEventArgs relatedArgs = null)
			{
				global::System.Diagnostics.Debug.Assert(
					kind == PointerCaptureKind.Explicit || kind == PointerCaptureKind.Implicit,
					"The initial capture kind must be Explicit **OR** Implicit.");

				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"{element}: Capturing ({kind}) pointer {Pointer}");
				}

				if (_targets.TryGetValue(element, out var target))
				{
					// Validate if the requested kind is not already handled
					if (target.Kind.HasFlag(kind))
					{
						return false;
					}
				}
				else
				{
					target = new PointerCaptureTarget(element);
					_targets.Add(element, target);

					// If the capture is made while raising an event (usually captures are made in PointerPressed handlers)
					// we re-use the current event args (if they match) to init the target.LastDispatched property.
					// Note:  we don't check the sender as we may capture on another element but the frame ID is still correct.
					if (relatedArgs?.Pointer == Pointer)
					{
						Update(target, relatedArgs);
					}
				}

				// Add the new king to the target
				target.Kind |= kind;

				// If we added an explicit capture, we update the _localExplicitCaptures of the target element
				if (kind == PointerCaptureKind.Explicit)
				{
					element._localExplicitCaptures.Add(Pointer);
				}

				// Make sure that this capture is effective
				EnsureEffectiveCaptureState();

				return true;
			}

			/// <summary>
			/// Removes a UIElement from the targets of this capture.
			/// DO NOT USE directly, use instead the Release method on the UIElement in order to properly raise the PointerCaptureLost event.
			/// </summary>
			public PointerCaptureKind RemoveTarget(UIElement element, PointerCaptureKind kinds, out PointerRoutedEventArgs lastDispatched)
			{
				if (_targets.TryGetValue(element, out var target))
				{
					// Validate if any of the requested kinds is handled
					if ((target.Kind & kinds) == 0)
					{
						lastDispatched = default;
						return PointerCaptureKind.None;
					}
				}
				else
				{
					lastDispatched = default;
					return PointerCaptureKind.None;
				}

				var removed = target.Kind & kinds;
				lastDispatched = target.LastDispatched;

				RemoveCore(target, kinds);

				return removed;
			}

			private void Clear()
			{
				foreach (var target in _targets.Values.ToList())
				{
					RemoveCore(target, PointerCaptureKind.Any);
				}
			}

			private void RemoveCore(PointerCaptureTarget target, PointerCaptureKind kinds)
			{
				global::System.Diagnostics.Debug.Assert(
					kinds != PointerCaptureKind.None,
					"The capture kind must be set to release pointer captures.");

				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"{target.Element}: Releasing ({kinds}) capture of pointer {Pointer}");
				}

				// If we remove an explicit capture, we update the _localExplicitCaptures of the target element
				if (kinds.HasFlag(PointerCaptureKind.Explicit)
					&& target.Kind.HasFlag(PointerCaptureKind.Explicit))
				{
					target.Element._localExplicitCaptures.Remove(Pointer);
				}

				target.Kind &= ~kinds;

				// The element is no longer listing for event, remove it.
				if (target.Kind == PointerCaptureKind.None)
				{
					_targets.Remove(target.Element);
				}

				// Validate / update the state of this capture
				EnsureEffectiveCaptureState();
			}

			/// <summary>
			/// Validate if the provided routed event args are relevant for the given element according to the active captures
			/// </summary>
			/// <param name="element">The target element for which the args are validated</param>
			/// <param name="args">The pending pointer event args that is under test</param>
			/// <param name="autoRelease">A flag that allows to automatically release any pending out-dated capture (for PointerDown only)</param>
			/// <returns>A boolean which indicates if the args are valid or not for the given element</returns>
			public bool ValidateAndUpdate(UIElement element, PointerRoutedEventArgs args, bool autoRelease)
			{
				if ((autoRelease && MostRecentDispatchedEventFrameId < args.FrameId)
					|| !_nativeCaptureElement.IsHitTestVisibleCoalesced)
				{
					// If 'autoRelease' we want to release any previous capture that was not release properly no matter the reason.
					// BUT we don't want to release a capture that was made by a child control (so LastDispatchedEventFrameId should already be equals to current FrameId).
					// We also do not allow a control that is not loaded to keep a capture (they should all have been release on unload).
					// ** This is an IMPORTANT safety catch to prevent the application to become unresponsive **
					Clear();

					return false;
				}
				else if (_targets.TryGetValue(element, out var target))
				{
					Update(target, args);

					return true;
				}
				else
				{
					// We should dispatch the event only if the control which has captured the pointer has already dispatched the event
					// (Which actually means that the current control is a parent of the control which has captured the pointer)
					// Remarks: This is not enough to determine parent-child relationship when we dispatch multiple events base on the same native event,
					//			(as they will all have the same FrameId), however in that case we dispatch events layer per layer
					//			instead of bubbling a single event before raising the next one, so we are safe.
					//			The only limitation would be when mixing native vs. managed bubbling, but this check only prevents
					//			the leaf of the tree to raise the event, so we cannot mix bubbling mode in that case.
					return MostRecentDispatchedEventFrameId >= args.FrameId;
				}
			}

			private void Update(PointerCaptureTarget target, PointerRoutedEventArgs args)
			{
				target.LastDispatched = args;
				if (MostRecentDispatchedEventFrameId < args.FrameId)
				{
					MostRecentDispatchedEventFrameId = args.FrameId;
				}
			}

			private void EnsureEffectiveCaptureState()
			{
				if (_targets.Any())
				{
					// We have some target, self enable us

					if (_actives.TryGetValue(Pointer, out var capture))
					{
						if (capture != this)
						{
							throw new InvalidOperationException("There is already another active capture.");
						}
					}
					else
					{
						// This is what makes this capture active
						_actives.Add(Pointer, this);
					}

					if (_nativeCaptureElement == null)
					{
						_nativeCaptureElement = _targets.Single().Key;

						try
						{
							_nativeCaptureElement.CapturePointerNative(Pointer);
						}
						catch (Exception e)
						{
							this.Log().Error($"Failed to capture natively pointer {Pointer}.", e);
						}
					}
				}
				else
				{
					// We no longer have any target, cleanup

					if (_nativeCaptureElement != null)
					{
						try
						{
							_nativeCaptureElement.ReleasePointerNative(Pointer);
						}
						catch (Exception e)
						{
							this.Log().Error($"Failed to release native capture of {Pointer}", e);
						}

						_nativeCaptureElement = null;
					}

					if (_actives.TryGetValue(Pointer, out var capture) && capture == this)
					{
						// This is what makes this capture inactive
						_actives.Remove(Pointer);
					}
				}
			}
		}

		private class PointerCaptureTarget
		{
			public PointerCaptureTarget(UIElement element)
			{
				Element = element;
			}

			/// <summary>
			/// The target element to which event args should be forwarded
			/// </summary>
			public UIElement Element { get; }

			/// <summary>
			/// Gets tha current capture kind that was enabled on the target
			/// </summary>
			public PointerCaptureKind Kind { get; set; }

			/// <summary>
			/// Determines if the <see cref="Element"/> is in the native bubbling tree.
			/// If so we could rely on standard events bubbling to reach it.
			/// Otherwise this means that we have to bubble the event in managed only.
			///
			/// This makes sense only for platform that has "implicit capture"
			/// (i.e. all pointers events are sent to the element on which the pointer pressed
			/// occured at the beginning of the gesture). This is the case on iOS and Android.
			/// </summary>
			public bool? IsInNativeBubblingTree { get; set; }

			/// <summary>
			/// Gets the last event dispatched by the <see cref="Element"/>.
			/// In case of native bubbling (cf. <see cref="IsInNativeBubblingTree"/>),
			/// this helps to determine that an event was already dispatched by the Owner:
			/// if a UIElement is receiving and event with the same timestamp, it means that the element
			/// is a parent of the Owner and we are only bubbling the routed event, so this element can
			/// raise the event (if the opposite, it means that the element is a child, so it has to mute the event).
			/// </summary>
			public PointerRoutedEventArgs LastDispatched { get; set; }
		}
	}
}
