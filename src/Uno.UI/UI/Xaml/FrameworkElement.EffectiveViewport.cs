//#define TRACE_EFFECTIVE_VIEWPORT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Extensions;
using _This = Windows.UI.Xaml.FrameworkElement;
#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml
{
#if !TEMPLATED
	// Internal interface used to allow communication between the real FrameworkElement
	// and presenters that are only implementing the IFrameworkElement interface (cf. FrameworkElementMixins.tt).
	// It must not be used anywhere else out of the file.
	internal interface IFrameworkElement_EffectiveViewport
	{
		void InitializeEffectiveViewport();
		IDisposable RequestViewportUpdates(IFrameworkElement_EffectiveViewport child = null);
		void OnParentViewportChanged(IFrameworkElement_EffectiveViewport parent, Rect viewport, bool isInitial = false);
		void OnLayoutUpdated();
	}
#endif

	partial class FrameworkElement : IFrameworkElement_EffectiveViewport
	{
		private static RoutedEventHandler ReconfigureViewportPropagationOnLoad = (snd, e) => ((_This)snd).ReconfigureViewportPropagation();
		private event TypedEventHandler<_This, EffectiveViewportChangedEventArgs> _effectiveViewportChanged;
		private int _childrenInterestedInViewportUpdates;
		private IDisposable _parentViewportUpdatesSubscription;
		private Rect _parentViewport = Rect.Empty; // WARNING: Stored in parent's coordinates on iOS, use GetParentViewport()
		private Rect _localViewport = Rect.Empty; // i.e. the applied clipping, Empty if not clipped
		private Rect _lastEffectiveSlot = new Rect();
		private Rect _lastEffectiveViewport = new Rect();

		public event TypedEventHandler<_This, EffectiveViewportChangedEventArgs> EffectiveViewportChanged
		{
			add
			{
				_effectiveViewportChanged += value;
				ReconfigureViewportPropagation();
			}
			remove
			{
				_effectiveViewportChanged -= value;
				ReconfigureViewportPropagation();
			}
		}

		// ctor (invoked by IFrameworkElement.Initialize())
		void IFrameworkElement_EffectiveViewport.InitializeEffectiveViewport()
		{
			Loaded += ReconfigureViewportPropagationOnLoad;
			Unloaded += ReconfigureViewportPropagationOnLoad;
		}

		/// <summary>
		/// Indicates if the effective viewport should/will be propagated to/by this element
		/// </summary>
		private bool IsEffectiveViewportEnabled => _childrenInterestedInViewportUpdates > 0 || _effectiveViewportChanged != null;

		/// <summary>
		/// Make sure to request or disable effective viewport changes from the parent
		/// </summary>
		private void ReconfigureViewportPropagation(IFrameworkElement_EffectiveViewport child = null)
		{
			if (IsLoaded && IsEffectiveViewportEnabled)
			{
				if (_parentViewportUpdatesSubscription == null)
				{
					TRACE_EFFECTIVE_VIEWPORT("Enabling effective viewport propagation.");

					var parent = this.GetVisualTreeParent();
					if (parent is IFrameworkElement_EffectiveViewport parentFwElt)
					{
						_parentViewportUpdatesSubscription = parentFwElt.RequestViewportUpdates(this);
					}
					else
					{
						// We are the root of the visual tree (maybe just temporarily),
						// we update the effective viewport in order to initialize the _parentViewport of children.
						PropagateEffectiveViewportChange(isInitial: true);
					}
				}
				else
				{
					TRACE_EFFECTIVE_VIEWPORT("New child requested viewport propagation which has already been enabled, forwarding current viewport to it.");

					// We are already subscribed, the parent won't send any update (and our _parentViewport is expected to be up-to-date).
					// But if this "reconfigure" was made for a new child (child != null), we have to initialize its own _parentViewport.

					var slot = LayoutInformation.GetLayoutSlot(this); // a.k.a. the implicit viewport to use if none was defined by any parent (usually only few elements at the top of the tree)
					var parentViewport = GetParentViewport();
					var viewport = GetEffectiveViewport(parentViewport, slot);

					child?.OnParentViewportChanged(this, viewport, isInitial: true);
				}
			}
			else
			{
				if (_parentViewportUpdatesSubscription != null)
				{
					TRACE_EFFECTIVE_VIEWPORT("Disabling effective viewport propagation.");

					_parentViewportUpdatesSubscription.Dispose();
					_parentViewportUpdatesSubscription = null;

					_parentViewport = Rect.Empty;
				}
			}
		}

		/// <summary>
		/// Used by a child of this element, in order to subscribe to viewport updates
		/// (so the OnParentViewportChanged will be invoked on this given child)
		/// </summary>
		IDisposable IFrameworkElement_EffectiveViewport.RequestViewportUpdates(IFrameworkElement_EffectiveViewport child)
		{
			global::System.Diagnostics.Debug.Assert(Uno.UI.Extensions.DependencyObjectExtensions.GetChildren(this).OfType<IFrameworkElement_EffectiveViewport>().Contains(child));

			_childrenInterestedInViewportUpdates++;
			ReconfigureViewportPropagation(child);

			return Disposable.Create(() =>
			{
				_childrenInterestedInViewportUpdates--;
				ReconfigureViewportPropagation();
			});
		}

		/// <summary>
		/// Used by a parent element to propagate down the viewport change
		/// </summary>
		void IFrameworkElement_EffectiveViewport.OnParentViewportChanged(
			IFrameworkElement_EffectiveViewport parent, // We propagate the parent to avoid costly lookup
			Rect viewport, // Be aware tht it might be empty ([+∞,+∞,-∞,-∞]) if not clipped
			bool isInitial) // Indicates that this update is only intended to initiate the _parentViewport
		{
			if (!IsEffectiveViewportEnabled)
			{
				// We do not keep the _parentViewport up-to-date if not needed.
				// It's expected to the root parent to update its children when propagation activated. 
				return;
			}

#if !__IOS__ // cf. GetParentViewport
			viewport = ParentToLocalCoordinates(parent, viewport);
#endif

			if (!isInitial && viewport == _parentViewport)
			{
				return;
			}

			_parentViewport = viewport;
			PropagateEffectiveViewportChange(isInitial);
		}

#if IS_NATIVE_ELEMENT
		// Native elements cannot be clipped (using Uno), so the _localViewport will always be an empty rect, and we only react to LayoutSlot updates
		void IFrameworkElement_EffectiveViewport.OnLayoutUpdated()
			=> PropagateEffectiveViewportChange();
#else
		void IFrameworkElement_EffectiveViewport.OnLayoutUpdated() { }  // Nothing to do here: this won't be invoked for real FrameworkElement, instead we receive OnViewportUpdated

		private protected sealed override void OnViewportUpdated(Rect viewport) // a.k.a. OnLayoutUpdated / OnClippingApplied
		{
			// Always keep it up-to-date, so if effective viewport is enable later, we will have a valid value.
			_localViewport = viewport;

			// Even if the viewport didn't changed, the LayoutSlot might have changed!
			PropagateEffectiveViewportChange();
		}
#endif

		private Rect GetEffectiveViewport(Rect parentViewport, Rect slot)
		{
			Rect viewport;
			if (_localViewport.IsEmpty)
			{
				// The local element does not clips its children (the common case),
				// so we only propagate the parent viewport (adjusted in the local coordinate space)
				viewport = parentViewport;
			}
			else
			{
				// The local element is clipping its children, so it defines the "effective" viewport for it and its children.
				// We however still have to consider the offsets applied by the parent (i.e. _parentViewport X and Y)
				// and constraint the viewport to the parent's viewport size (i.e. _parentViewport Width and Height).
				// Note: At this point as the _parentViewport is defined in local coordinate space,
				//		 _parentViewport.X and Y are usually negative.
				//		 If there isn't any parent that clipped us, then it will be empty [+∞,+∞,-∞,-∞],
				//		 in that case make sure to ignore it (we assume that it's possible to be clipped only on one direction).

				double x, y, width, height;
				var parentWidth = parentViewport.Width.FiniteOrDefault(slot.Width);
				if (_localViewport.Width < parentWidth)
				{
					// The local element is clipping vertically
					x = _localViewport.X;
					width = _localViewport.Width;
				}
				else
				{
					x = _localViewport.X + parentViewport.X.FiniteOrDefault(0);
					width = Math.Min(_localViewport.Width, parentWidth);
				}

				var parentHeight = parentViewport.Height.FiniteOrDefault(slot.Height);
				if (_localViewport.Height < parentHeight)
				{
					// The local element is clipping vertically
					y = _localViewport.Y;
					height = _localViewport.Height;
				}
				else
				{
					y = _localViewport.Y + parentViewport.Y.FiniteOrDefault(0);
					height = Math.Min(_localViewport.Height, parentHeight);
				}

				viewport = new Rect(x, y, width, height);

#if !IS_NATIVE_ELEMENT // Only true-UIElement can register as scroller.
				// This element is also acting as scroller, so we also have to apply the local scroll offsets.
				// Note: Those offsets should probably be part of the _localViewport (Frame vs. Bounds),
				//		 but for now we supports only the internal controls that are able to set the internal ScrollOffsets property.
				if (IsScrollPort)
				{
					viewport.X += ScrollOffsets.X;
					viewport.Y += ScrollOffsets.Y;
				}
#endif
			}

			return viewport;
		}

		private Rect GetParentViewport()
#if __IOS__
			// On iOS the Arrange is "async": When arranged an element only arrange its direct children (set their Frame)
			// which then waits for their 'LayoutSubView' to arrange their own children.
			//
			// The issue is that after having arrange its children, the element will also apply its clipping,
			// which will cause an update of the EffectiveViewport and more specifically invoke the OnParentViewportChanged
			// on all children (direct and sub children), including children that might not be arranged yet.
			//
			// If we convert the viewport into local element coordinate space at this time (like on other platforms),
			// we won't have a valid LayoutSlot (and LayoutSlotWithMarginsAndAlignment used in GetTransform).
			//
			// To avoid this we store the parentViewport in parent coordinates on iOS, and reconvert it every time.
			=> ParentToLocalCoordinates(this.GetParent(), _parentViewport);
#else
			=> _parentViewport;
#endif

		private Rect ParentToLocalCoordinates(object parent, Rect viewport)
#if IS_NATIVE_ELEMENT // No RenderTransform on native elements
			=> viewport;
#else
			=> !viewport.IsEmpty && parent is UIElement parentElt
				? GetTransform(this, parentElt).Inverse().Transform(viewport)
				: viewport;
#endif

		private void PropagateEffectiveViewportChange(
			bool isInitial = false
#if TRACE_EFFECTIVE_VIEWPORT
			, [CallerMemberName] string caller = null) {
#else
			)
		{
			const string caller = "--unavailable--";
#endif
			if (!IsEffectiveViewportEnabled)
			{
				TRACE_EFFECTIVE_VIEWPORT($"Effective viewport disabled, stop propagation (reason:{caller} | isInitial:{isInitial}).");
				return;
			}

			var slot = LayoutInformation.GetLayoutSlot(this); // a.k.a. the implicit viewport to use if none was defined by any parent (usually only few elements at the top of the tree)
			var parentViewport = GetParentViewport();
			var viewport = GetEffectiveViewport(parentViewport, slot);

			var isViewportUpdate = _lastEffectiveViewport != viewport;
			var isSlotUpdate = _lastEffectiveSlot != slot;

			TRACE_EFFECTIVE_VIEWPORT($"viewport: {viewport.ToDebugString()} (updated: {isViewportUpdate}) "
				+ $"| slot: {slot.ToDebugString()} (updated: {isSlotUpdate}) "
				+ $"| isInitial: {isInitial} "
				+ $"| local: {_localViewport.ToDebugString()} "
				+ $"| parent: {parentViewport.ToDebugString()} "
#if IS_NATIVE_ELEMENT
				+ $"| scroll: --none--(native) "
#else
				+ $"| scroll: {(IsScrollPort ? $"{ScrollOffsets.ToDebugString()}" : "--none--")} "
#endif
				+ $"| reason: {caller} "
				+ $"| children: {_childrenInterestedInViewportUpdates}");

			_lastEffectiveViewport = viewport;
			_lastEffectiveSlot = slot;

			if (!isInitial && (isViewportUpdate || isSlotUpdate))
			{
				// Note: Here the viewport might have some infinite values (notably if we don't have any parent that clipped us).
				//		 In that case we fallback to the LayoutSlot as we should not raise the event with infinite values.
				_effectiveViewportChanged?.Invoke(this, new EffectiveViewportChangedEventArgs(viewport.FiniteOrDefault(slot)));
			}

			if (_childrenInterestedInViewportUpdates > 0
				&& (isViewportUpdate || isInitial)) // If isLayoutSlot update, then children element are also going to be arranged
			{
				var children = Uno.UI.Extensions.DependencyObjectExtensions.GetChildren(this);
				foreach (var child in children)
				{
					if (child is IFrameworkElement_EffectiveViewport childFwElt)
					{
						childFwElt.OnParentViewportChanged(this, viewport, isInitial: isInitial);
					}
				}
			}
		}

#if !IS_NATIVE_ELEMENT
		[NotImplemented] // Supported only for internal elements, cf. comment below
		protected void InvalidateViewport()
		{
			if (!IsScrollPort)
			{
				throw new InvalidOperationException("InvalidateViewport can only be called on elements that have been registered as scroll ports.");
			}

			// Here we should use the clipping to determine the actual view port for external controls,
			// but for now the clipping we support only internal controls that can set the ScrollOffsets property on UIElement.
			PropagateEffectiveViewportChange();
		}
#endif

		[Conditional("TRACE_EFFECTIVE_VIEWPORT")]
		private void TRACE_EFFECTIVE_VIEWPORT(string text)
		{
#if TRACE_EFFECTIVE_VIEWPORT
			Debug.Write($"{this.GetDebugIdentifier()} {text}\r\n");
#endif
		}
	}
}
