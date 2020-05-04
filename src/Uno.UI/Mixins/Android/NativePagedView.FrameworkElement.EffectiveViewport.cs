#define TEMPLATED
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI;
using _This = Windows.UI.Xaml.Controls.NativePagedView;

namespace Windows.UI.Xaml.Controls
{
#if !TEMPLATED
	// Internal interface used to allow communication between the real FrameworkElement
	// and presenters that are only implementing the IFrameworkElement interface (cf. FrameworkElementMixins.tt).
	// It must not be used anywhere else out of the file.
	internal interface IFrameworkElement_EffectiveViewport
	{
		IDisposable RequestViewportUpdates(IFrameworkElement_EffectiveViewport child = null);
		void OnParentViewportChanged(UIElement parent, Rect viewport, bool isInitial = false);
	}
#endif

	partial class NativePagedView : IFrameworkElement_EffectiveViewport
	{
		private static RoutedEventHandler ReconfigureViewportPropagationOnLoad = (snd, e) => ((_This)snd).ReconfigureViewportPropagation();
		private event TypedEventHandler<_This, EffectiveViewportChangedEventArgs> _effectiveViewportChanged;
		private int _childrenInterestedInViewportUpdates;
		private IDisposable _parentViewportUpdatesSubscription;
		private Rect _parentViewport = Rect.Empty;
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
					var parent = Parent;
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
					// We are already subscribed, the parent won't send any update (and our _parentViewport is expected to be up-to-date).
					// But if this "reconfigure" was made for a new child (child != null), we have to initialize its own _parentViewport.
					child?.OnParentViewportChanged(this, GetEffectiveViewport(), isInitial: true);
				}
			}
			else
			{
				if (_parentViewportUpdatesSubscription != null)
				{
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
			UIElement parent, // We propagate the parent to avoid costly lookup and useless casting
			Rect viewport, // Be aware tht it might be empty ([+∞,+∞,-∞,-∞]) if not clipped
			bool isInitial) // Indicates that this update is only intended to initiate the _parentViewport
		{
			if (!IsEffectiveViewportEnabled)
			{
				// We do not keep the _parentViewport up-to-date if not needed.
				// It's expected to the root parent to update its children when propagation activated. 
				return;
			}

			var viewportInLocalCoordinates = viewport.IsEmpty
				? viewport
				: GetTransform(this, parent).Transform(viewport);
			if (viewportInLocalCoordinates == _parentViewport)
			{
				return;
			}

			_parentViewport = viewportInLocalCoordinates;
			PropagateEffectiveViewportChange(isInitial);
		}

		private protected sealed override void OnViewportUpdated(Rect viewport) // a.k.a. OnLayoutUpdated
		{
			// Always keep it up-to-date, so if effective viewport is enable later, we will have a valid value.
			_localViewport = viewport;

			// Even if the viewport didn't changed, the LayoutSlot might have changed!
			PropagateEffectiveViewportChange();
		}

		private Rect GetEffectiveViewport()
		{
			Rect viewport;
			if (_localViewport.IsEmpty)
			{
				// The local element does not clips its children (the common case),
				// so we only propagate the parent viewport (adjusted in the local coordinate space)
				viewport = _parentViewport;
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
				viewport = new Rect(
					x: _localViewport.X + _parentViewport.X.FiniteOrDefault(0),
					y: _localViewport.Y + _parentViewport.Y.FiniteOrDefault(0),
					width: Math.Min(_localViewport.Width, _parentViewport.Width.FiniteOrDefault(double.PositiveInfinity)),
					height: Math.Min(_localViewport.Height, _parentViewport.Height.FiniteOrDefault(double.PositiveInfinity)));

				// This element is also acting as scroller, so we also have to apply the local scroll offsets.
				// Note: Those offsets should probably be part of the _localViewport (Frame vs. Bounds),
				//		 but for now we supports only the internal controls that are able to set the internal ScrollOffsets property.
				if (IsScrollPort)
				{
					viewport.X += ScrollOffsets.X;
					viewport.Y += ScrollOffsets.Y;
				}
			}

			return viewport;
		}

		private void PropagateEffectiveViewportChange(bool isInitial = false)
		{
			if (!IsEffectiveViewportEnabled)
			{
				return;
			}

			var viewport = GetEffectiveViewport();
			var slot = LayoutSlot;

			var isViewportUpdate = _lastEffectiveViewport != viewport;
			var isSlotUpdate = _lastEffectiveSlot != LayoutSlot;

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
						childFwElt.OnParentViewportChanged(this, viewport);
					}
				}
			}
		}

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
	}
}
