using Uno.Diagnostics.Eventing;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using System.Linq;
using Windows.Foundation;
using Uno.UI.Controls;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Uno;
using Uno.Disposables;
using Windows.UI.Core;
using System.ComponentModel;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
#if XAMARIN_ANDROID
using View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using UIKit;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using Color = Windows.UI.Color;
#else
using Color = System.Drawing.Color;
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : UIElement, IFrameworkElement, IFrameworkElementInternal, ILayoutConstraints, IDependencyObjectParse
	{
		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{DDDCCA61-5CB7-4585-95D7-58C5528AABE6}");

			public const int FrameworkElement_MeasureStart = 1;
			public const int FrameworkElement_MeasureStop = 2;
			public const int FrameworkElement_ArrangeStart = 3;
			public const int FrameworkElement_ArrangeStop = 4;
			public const int FrameworkElement_InvalidateMeasure = 5;
		}

#if !NETSTANDARD
		private FrameworkElementLayouter _layouter;
#else
		private readonly static IEventProvider _trace = Tracing.Get(FrameworkElement.TraceProvider.Id);
#endif
		
		private bool _constraintsChanged;
		private bool _suppressIsEnabled;

		private bool _defaultStyleApplied = false;
		private protected bool IsDefaultStyleApplied => _defaultStyleApplied;
		/// <summary>
		/// The current user-determined 'active Style'. This will either be the explicitly-set Style, if there is one, or otherwise the resolved implicit Style (either in the view hierarchy or in Application.Resources).
		/// </summary>
		private Style _activeStyle = null;

		/// <summary>
		/// Sets whether constraint-based optimizations are used to limit redrawing of the entire visual tree on Android. This can be
		/// globally set to false if it is causing visual errors (eg views not updating properly). Note: this can still be overridden by
		/// the <see cref="AreDimensionsConstrained"/> flag set on individual elements.
		/// </summary>
		public static bool UseConstraintOptimizations { get; set; } = false;

		/// <summary>
		/// If manually set, this flag overrides the constraint-based reasoning for optimizing layout calls. This may be useful for
		/// example if there are custom views in the visual hierarchy that do not implement <see cref="ILayoutConstraints"/>.
		/// </summary>
		public bool? AreDimensionsConstrained { get; set; }

		/// <summary>
		/// Indicates that this view can participate in layout optimizations using the simplest logic.
		/// </summary>
		protected virtual bool IsSimpleLayout => false;

		#region Tag Dependency Property

#if __IOS__ || __MACOS__ || __ANDROID__
#pragma warning disable 114 // Error CS0114: 'FrameworkElement.Tag' hides inherited member 'UIView.Tag'
#endif
		public object Tag
		{
			get => GetTagValue();
			set => SetTagValue(value);
		}
#pragma warning restore 114 // Error CS0114: 'FrameworkElement.Tag' hides inherited member 'UIView.Tag'

		[GeneratedDependencyProperty(DefaultValue = null)]
		public static DependencyProperty TagProperty { get; } = CreateTagProperty();

#endregion

		#region EffectiveViewPort
		private static RoutedEventHandler ReconfigureViewportPropagationOnLoad = (snd, e) => ((FrameworkElement)snd).ReconfigureViewportPropagation();
		private event TypedEventHandler<FrameworkElement, EffectiveViewportChangedEventArgs> _effectiveViewportChanged;
		private int _childrenInterestedInViewportUpdates;
		private IDisposable _parentViewportUpdatesSubscription;
		private Rect _parentViewport = Rect.Empty;
		private Rect _localViewport = Rect.Empty; // i.e. the applied clipping, Empty if not clipped
		private Rect _lastEffectiveSlot = new Rect();
		private Rect _lastEffectiveViewport = new Rect();

		public event TypedEventHandler<FrameworkElement, EffectiveViewportChangedEventArgs> EffectiveViewportChanged
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
		private void ReconfigureViewportPropagation(FrameworkElement child = null)
		{
			if (IsLoaded && IsEffectiveViewportEnabled)
			{
				if (_parentViewportUpdatesSubscription == null)
				{
					var parent = Parent;
// -- BEGIN -- WORKAROUND CASE FOR IFrameworkElement, can safely be removed when we strip out the IFrameworkElement
					while (parent is IFrameworkElement pseudoFwElt && !(parent is FrameworkElement))
					{
						parent = pseudoFwElt.Parent;
					}
// -- END -- WORKAROUND CASE FOR IFrameworkElement

					if (parent is FrameworkElement parentFwElt)
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
		private IDisposable RequestViewportUpdates(FrameworkElement child)
		{
			//global::System.Diagnostics.Debug.Assert(Uno.UI.Extensions.DependencyObjectExtensions.GetChildren(this).Contains(child));

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
		private void OnParentViewportChanged(
			UIElement parent, // We propagate the parent to avoid costly lookup and useless casting
			Rect viewport, // Be aware tht it might be empty ([+∞,+∞,-∞,-∞]) if not clipped
			bool isInitial = false) // Indicates that this update is only intended to initiate the _parentViewport
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

// -- BEGIN -- WORKAROUND CASE FOR IFrameworkElement, can safely be removed when we strip out the IFrameworkElement
				IEnumerable<DependencyObject> GetNestedChildren(DependencyObject c)
					=>  c is FrameworkElement
							? new[] {c} :
						c is IFrameworkElement pseudoFwElt
							? Uno.UI.Extensions.DependencyObjectExtensions.GetChildren(pseudoFwElt).SelectMany(GetNestedChildren)
							: Enumerable.Empty<DependencyObject>();
				children = children.SelectMany(GetNestedChildren);
// -- END -- WORKAROUND CASE FOR IFrameworkElement

				foreach (var child in children)
				{
					if (child is FrameworkElement childFwElt)
					{
						childFwElt.OnParentViewportChanged(this, viewport);
					}
				}
			}
		}

		// This is the public API for the effective viewport invalidation
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
		#endregion

		partial void Initialize()
		{
#if !NETSTANDARD2_0
			_layouter = new FrameworkElementLayouter(this, MeasureOverride, ArrangeOverride);
#endif
			Resources = new Windows.UI.Xaml.ResourceDictionary();

			IFrameworkElementHelper.Initialize(this);

			Loaded += ReconfigureViewportPropagationOnLoad;
			Unloaded += ReconfigureViewportPropagationOnLoad;
		}

		public
#if __ANDROID__
		new
#endif
		Windows.UI.Xaml.ResourceDictionary Resources
		{
			get; set;
		}

		/// <summary>
		/// Gets the parent of this FrameworkElement in the object tree.
		/// </summary>
		public
#if __ANDROID__
		new
#endif
		DependencyObject Parent => ((IDependencyObjectStoreProvider)this).Store.Parent as DependencyObject;

		private bool _isParsing;
		/// <summary>
		/// True if the element is in the process of being parsed from Xaml.
		/// </summary>
		/// <remarks>This property shouldn't be set from user code. It's public to allow being set from generated code.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public bool IsParsing
		{
			get => _isParsing;
			set
			{
				if (!value)
				{
					throw new InvalidOperationException($"{nameof(IsParsing)} should never be set from user code.");
				}

				_isParsing = value;
				if (_isParsing)
				{
					ResourceResolver.PushSourceToScope(this);
				}
			}
		}

		/// <summary>
		/// Provides the behavior for the "Measure" pass of the layout cycle. Classes can override this method to define their own "Measure" pass behavior.
		/// </summary>
		/// <param name="availableSize">The available size that this object can give to child objects. Infinity can be specified as a value to indicate that the object will size to whatever content is available.</param>
		/// <returns>The size that this object determines it needs during layout, based on its calculations of the allocated sizes for child objects or based on other considerations such as a fixed container size.</returns>
		protected virtual Size MeasureOverride(Size availableSize)
		{
#if !NETSTANDARD2_0
			LastAvailableSize = availableSize;
#endif

			var child = this.FindFirstChild();
			return child != null ? MeasureElement(child, availableSize) : new Size(0, 0);
		}

		/// <summary>
		/// Provides the behavior for the "Arrange" pass of layout. Classes can override this method to define their own "Arrange" pass behavior.
		/// </summary>
		/// <param name="finalSize">The final area within the parent that this object should use to arrange itself and its children. </param>
		/// <returns>The actual size that is used after the element is arranged in layout.</returns>
		protected virtual Size ArrangeOverride(Size finalSize)
		{
			var child = this.FindFirstChild();

			if (child != null)
			{
#if NETSTANDARD
				child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
#else
				ArrangeElement(child, new Rect(0, 0, finalSize.Width, finalSize.Height));
#endif
				return finalSize;
			}
			else
			{
				return finalSize;
			}
		}

#if !NETSTANDARD
		/// <summary>
		/// Updates the DesiredSize of a UIElement. Typically, objects that implement custom layout for their
		/// layout children call this method from their own MeasureOverride implementations to form a recursive layout update.
		/// </summary>
		/// <param name="availableSize">
		/// The available space that a parent can allocate to a child object. A child object can request a larger
		/// space than what is available; the provided size might be accommodated if scrolling or other resize behavior is
		/// possible in that particular container.
		/// </param>
		/// <returns>The measured size.</returns>
		/// <remarks>
		/// Under Uno.UI, this method should not be called during the normal layouting phase. Instead, use the
		/// <see cref="MeasureElement(View, Size)"/> methods, which handles native view properly.
		/// </remarks>
		public override void Measure(Size availableSize)
		{
			if (double.IsNaN(availableSize.Width) || double.IsNaN(availableSize.Height))
			{
				throw new InvalidOperationException($"Cannot measure [{GetType()}] with NaN");
			}

			_layouter.Measure(availableSize);
			OnMeasurePartial(availableSize);
		}

		/// <summary>
		/// Positions child objects and determines a size for a UIElement. Parent objects that implement custom layout
		/// for their child elements should call this method from their layout override implementations to form a recursive layout update.
		/// </summary>
		/// <param name="finalRect">The final size that the parent computes for the child in layout, provided as a <see cref="Windows.Foundation.Rect"/> value.</param>
		public override void Arrange(Rect finalRect)
		{
			_layouter.Arrange(finalRect);
			_layouter.ArrangeChild(this, finalRect);
		}
#endif

		partial void OnMeasurePartial(Size slotSize);

		/// <summary>
		/// Measures an native element, in the same way <see cref="Measure"/> would do.
		/// </summary>
		/// <param name="view">The view to be measured.</param>
		/// <param name="availableSize">
		/// The available space that a parent can allocate to a child object. A child object can request a larger
		/// space than what is available; the provided size might be accommodated if scrolling or other resize behavior is
		/// possible in that particular container.
		/// </param>
		/// <returns>The measured size - INCLUDES THE MARGIN</returns>
		protected Size MeasureElement(View view, Size availableSize)
		{
#if NETSTANDARD
			view.Measure(availableSize);
			return view.DesiredSize;
#else
			return _layouter.MeasureElement(view, availableSize);
#endif
		}

		/// <summary>
		/// Positions an object inside the current element and determines a size for a UIElement. Parent objects that implement custom layout
		/// for their child elements should call this method from their layout override implementations to form a recursive layout update.
		/// </summary>
		/// <param name="finalRect">The final size that the parent computes for the child in layout, provided as a <see cref="Windows.Foundation.Rect"/> value.</param>
		protected void ArrangeElement(View view, Rect finalRect)
		{
#if __WASM__
			var adjust = GetBorderThickness();

			// HTML moves the origin along with the border thickness.
			// Adjust the child based on this element's border thickness.
			var rect = new Rect(finalRect.X - adjust.Left, finalRect.Y - adjust.Top, finalRect.Width, finalRect.Height);

			view.Arrange(rect);
#elif NETSTANDARD
			view.Arrange(finalRect);
#else
			_layouter.ArrangeElement(view, finalRect);
#endif
		}

		/// <summary>
		/// Provides the desired size, computed during a previous call to <see cref="Measure"/> or <see cref="MeasureElement(View, Size)"/>.
		/// </summary>
		protected Size GetElementDesiredSize(View view)
		{
#if NETSTANDARD
			return view.DesiredSize;
#else
			return (_layouter as ILayouter).GetDesiredSize(view);
#endif
		}

		partial void OnLoadingPartial()
		{
			// Apply active style and default style when we enter the visual tree, if they haven't been applied already.
			ApplyStyles();
		}

		private protected void ApplyStyles()
		{
			ApplyStyle();
			ApplyDefaultStyle();
		}

		/// <summary>
		/// Called when the element has completed being parsed from Xaml.
		/// </summary>
		/// <remarks>This method shouldn't be called from user code. It's public to allow being called from generated code.</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void CreationComplete()
		{
			if (!IsParsing)
			{
				throw new InvalidOperationException($"Called without matching {nameof(IsParsing)} call. This method should never be called from user code.");
			}
#if !HAS_EXPENSIVE_TRYFINALLY
			try
#endif
			{
				ApplyStyles();
			}
#if !HAS_EXPENSIVE_TRYFINALLY
			finally
#endif
			{
				_isParsing = false;
				ResourceResolver.PopSourceFromScope();
			}
		}

#region Style DependencyProperty

		public Style Style
		{
			get => (Style)GetValue(StyleProperty);
			set => SetValue(StyleProperty, value);
		}

		public static DependencyProperty StyleProperty { get ; } =
			DependencyProperty.Register(
				nameof(Style),
				typeof(Style),
				typeof(FrameworkElement),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
					propertyChangedCallback: (s, e) => ((FrameworkElement)s)?.OnStyleChanged((Style)e.OldValue, (Style)e.NewValue)
				)
			);

#endregion

		private void OnStyleChanged(Style oldStyle, Style newStyle)
		{
			if (!IsParsing) // Style will be applied once element has completed parsing
			{
				ApplyStyle();
			}
		}

		/// <summary>
		/// Update and apply the current 'active Style'.
		/// </summary>
		private void ApplyStyle()
		{
			var oldActiveStyle = _activeStyle;
			UpdateActiveStyle();
			OnStyleChanged(oldActiveStyle, _activeStyle, DependencyPropertyValuePrecedences.ExplicitStyle);
		}

		/// <summary>
		/// Update the current 'active Style'. This will be the <see cref="Style"/> if it's set, or the implicit Style otherwise.
		/// </summary>
		private void UpdateActiveStyle()
		{
			if (this.IsDependencyPropertySet(StyleProperty))
			{
				_activeStyle = Style;
			}
			else
			{
				_activeStyle = ResolveImplicitStyle();
			}
		}

		private Style ResolveImplicitStyle() => (this as IDependencyObjectStoreProvider).Store.GetImplicitStyle();

		#region Requested theme dependency property

		public ElementTheme RequestedTheme
		{
			get => (ElementTheme)GetValue(RequestedThemeProperty);
			set => SetValue(RequestedThemeProperty, value);
		}

		public static DependencyProperty RequestedThemeProperty { get; } =
			DependencyProperty.Register(
				nameof(RequestedTheme),
				typeof(ElementTheme),
				typeof(FrameworkElement),
				new PropertyMetadata(
					ElementTheme.Default,
					(o, e) => ((FrameworkElement)o).OnRequestedThemeChanged((ElementTheme)e.OldValue, (ElementTheme)e.NewValue)));

		private void OnRequestedThemeChanged(ElementTheme oldValue, ElementTheme newValue)
		{
			if (IsWindowRoot) // Some elements like TextBox set RequestedTheme in their Focused style, so only listen to changes to root view
			{
				// This is an ultra-naive implementation... but nonetheless enables the common use case of overriding the system theme for
				// the entire visual tree (since Application.RequestedTheme cannot be set after launch)
				Application.Current.SetExplicitRequestedTheme(Uno.UI.Extensions.ElementThemeExtensions.ToApplicationThemeOrDefault(newValue));
			}
		}


		#endregion

		public ElementTheme ActualTheme => IsWindowRoot ?
			Application.Current?.ActualElementTheme ?? ElementTheme.Default
			: ElementTheme.Default;

		/// <summary>
		/// Replace previous style with new style, at nominated precedence. This method is called separately for the user-determined
		/// 'active style' and for the baked-in 'default style'.
		/// </summary>
		private void OnStyleChanged(Style oldStyle, Style newStyle, DependencyPropertyValuePrecedences precedence)
		{
			if (oldStyle == newStyle)
			{
				// Nothing to do
				return;
			}

			oldStyle?.ClearInvalidProperties(this, newStyle, precedence);

			newStyle?.ApplyTo(this, precedence);
		}

		/// <summary>
		/// Apply the default style for this element, if one is defined.
		/// </summary>
		private protected void ApplyDefaultStyle()
		{
			if (_defaultStyleApplied)
			{
				return;
			}
			_defaultStyleApplied = true;

			var style = Style.GetDefaultStyleForType(GetDefaultStyleKey());

			// Although this is the default style, we use the ImplicitStyle enum value (which is otherwise unused) to ensure that it takes precedence
			//over inherited property values. UWP's precedence system is simpler than WPF's, from which the enum is derived.
			OnStyleChanged(null, style, DependencyPropertyValuePrecedences.ImplicitStyle);
		}

		/// <summary>
		/// This returns <see cref="Control.DefaultStyleKey"/> for Control subclasses, and null for all other types.
		/// </summary>
		private protected virtual Type GetDefaultStyleKey() => null;

		protected virtual void OnApplyTemplate()
		{
		}

		partial void OnGenericPropertyUpdatedPartial(DependencyPropertyChangedEventArgs args)
		{
			_constraintsChanged = true;
		}

		/// <summary>
		/// Provides the ability to disable <see cref="IsEnabled"/> value changes, e.g. in the context of ICommand CanExecute.
		/// </summary>
		/// <param name="suppress">If true, <see cref="IsEnabled"/> will always be false</param>
		private protected void SuppressIsEnabled(bool suppress)
		{
			_suppressIsEnabled = suppress;
			this.CoerceValue(IsEnabledProperty);
		}

		private object CoerceIsEnabled(object baseValue)
		{
			return _suppressIsEnabled ? false : baseValue;
		}

		/// <summary>
		/// Determines whether a measure/arrange invalidation on this element requires elements higher in the tree to be invalidated,
		/// by determining recursively whether this element's dimensions are already constrained.
		/// </summary>
		/// <returns>True if a request should be elevated, false if only this view needs to be rearranged.</returns>
		private bool ShouldPropagateLayoutRequest()
		{
			if (!UseConstraintOptimizations && !AreDimensionsConstrained.HasValue)
			{
				return true;
			}

			if (_constraintsChanged)
			{
				return true;
			}
			if (!IsLoaded)
			{
				//If the control isn't loaded, propagating the request won't do anything anyway
				return true;
			}

			if (AreDimensionsConstrained.HasValue)
			{
				return !AreDimensionsConstrained.Value;
			}

			var iswidthConstrained = IsWidthConstrained(null);
			var isHeightConstrained = IsHeightConstrained(null);
			return !(iswidthConstrained && isHeightConstrained);
		}

		bool ILayoutConstraints.IsWidthConstrained(View requester) => IsWidthConstrained(requester);
		private bool IsWidthConstrained(View requester)
		{
			return IsWidthConstrainedInner(requester) ??
				(Parent as ILayoutConstraints)?.IsWidthConstrained(this) ??
				//If the top level view itself is making the request, propagate it
				(requester != null && IsTopLevelXamlView());
		}

		protected virtual bool? IsWidthConstrainedInner(View requester)
		{
			if (!IsSimpleLayout)
			{
				//In the base case (eg for non-framework custom panels) assume that we have to relayout
				return false;
			}
			return this.IsWidthConstrainedSimple();
		}

		bool ILayoutConstraints.IsHeightConstrained(View requester) => IsHeightConstrained(requester);
		private bool IsHeightConstrained(View requester)
		{
			return IsHeightConstrainedInner(requester) ?? (Parent as ILayoutConstraints)?.IsHeightConstrained(this) ?? IsTopLevelXamlView();
		}

		protected virtual bool? IsHeightConstrainedInner(View requester)
		{
			if (!IsSimpleLayout)
			{
				//In the base case (eg for non-framework custom panels) assume that we have to relayout
				return false;
			}
			return this.IsHeightConstrainedSimple();
		}

		internal override bool IsViewHit()
			=> Background != null;

		/// <summary>
		/// The list of available children render phases, if this
		/// control is the root element of a DataTemplate.
		/// </summary>
		internal int[] DataTemplateRenderPhases { get; set; }

		internal bool GoToElementState(string stateName, bool useTransitions) => GoToElementStateCore(stateName, useTransitions);

		protected virtual bool GoToElementStateCore(string stateName, bool useTransitions) => false;

		public event EventHandler<object> LayoutUpdated;

		internal virtual void OnLayoutUpdated()
		{
			LayoutUpdated?.Invoke(this, new RoutedEventArgs(this));
		}

		private protected virtual Thickness GetBorderThickness() => Thickness.Empty;

#if XAMARIN
		private static FrameworkElement FindPhaseEnabledRoot(ContentControl content)
		{
			if (content.TemplatedRoot is FrameworkElement root)
			{
				var presenter = root.FindFirstChild<ContentPresenter>();

				if (presenter?.ContentTemplateRoot is FrameworkElement presenterRoot
					&& presenterRoot.DataTemplateRenderPhases != null)
				{
					return presenterRoot;
				}
			}

			return null;
		}

		/// <summary>
		/// Initializes the provided control for phased binding, if supported.
		/// </summary>
		/// <param name="content"></param>
		internal static void InitializePhaseBinding(ContentControl content)
		{
			var presenterRoot = FindPhaseEnabledRoot(content);

			if (presenterRoot != null)
			{
				// Phase zero is always visible
				presenterRoot.ApplyBindingPhase(0);
			}
		}

		/// <summary>
		/// Registers the provided item template instance for phase binding
		/// </summary>
		/// <param name="content">The content control the phase-render</param>
		/// <param name="registerForRecycled">An action that will be executed when the provided view will be recycled.</param>
		internal static void RegisterPhaseBinding(ContentControl content, Action<Action> registerForRecycled)
		{
			var presenterRoot = FindPhaseEnabledRoot(content);

			if (presenterRoot != null)
			{
				// Phase zero is always visible
				presenterRoot.ApplyBindingPhase(0);

				var startPhaseIndex = presenterRoot.DataTemplateRenderPhases[0] == 0 ? 1 : 0;

				// Schedule all the phases at once
				for (int i = startPhaseIndex; i < presenterRoot.DataTemplateRenderPhases.Length; i++)
				{
					UIAsyncOperation action = null;
					var phaseCapture = i;

					async void ApplyPhase()
					{
						// Yield immediately so we requeue on the normal dispatcher.
						await Task.Yield();

						if (!action.IsCancelled)
						{
							presenterRoot.ApplyBindingPhase(presenterRoot.DataTemplateRenderPhases[phaseCapture]);

							// Reset the action so we can avoid canceling it.
							action = null;
						}
					}

#if __ANDROID__
					// Schedule on the animation dispatcher so the callback appears faster.
					action = presenterRoot.Dispatcher.RunAnimation(ApplyPhase);
#elif __IOS__ || __MACOS__
					action = presenterRoot.Dispatcher.RunAsync(CoreDispatcherPriority.High, ApplyPhase);
#endif

					registerForRecycled(
						() =>
						{
							// If the view is recycled, don't process the other phases.
							action?.Cancel();

							// Reset to the original so the next datacontext assignment only
							// impacts the least of the tree.
							presenterRoot.ApplyBindingPhase(0);
						}
					);
				}
			}
		}
#endif

		/// <summary>
		/// Update ThemeResource references. 
		/// </summary>
		internal virtual void UpdateThemeBindings()
		{
			Resources?.UpdateThemeBindings();
			(this as IDependencyObjectStoreProvider).Store.UpdateResourceBindings(isThemeChangedUpdate: true);
		}

		/// <summary>
		/// Set correct default foreground for the current theme.
		/// </summary>
		/// <param name="foregroundProperty">The appropriate property for the calling instance.</param>
		private protected void SetDefaultForeground(DependencyProperty foregroundProperty)
		{
			(this).SetValue(foregroundProperty,
							Application.Current == null || Application.Current.RequestedTheme == ApplicationTheme.Light
								? SolidColorBrushHelper.Black
								: SolidColorBrushHelper.White, DependencyPropertyValuePrecedences.DefaultValue);
		}

#region AutomationPeer
#if !__IOS__ && !__ANDROID__ && !__MACOS__ // This code is generated in FrameworkElementMixins
		private AutomationPeer _automationPeer;

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			if (AutomationProperties.GetName(this) is string name && !string.IsNullOrEmpty(name))
			{
				return new FrameworkElementAutomationPeer(this);
			}

			return null;
		}

		public virtual string GetAccessibilityInnerText()
		{
			return null;
		}

		public AutomationPeer GetAutomationPeer()
		{
			if (_automationPeer == null)
			{
				_automationPeer = OnCreateAutomationPeer();
			}

			return _automationPeer;
		}
#elif __MACOS__
		private AutomationPeer _automationPeer;

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return null;
		}

		public virtual string GetAccessibilityInnerText()
		{
			return null;
		}
		public AutomationPeer GetAutomationPeer()
		{
			if (_automationPeer == null)
			{
				_automationPeer = OnCreateAutomationPeer();
			}

			return _automationPeer;
		}
#endif

#endregion

#if !NETSTANDARD
		private class FrameworkElementLayouter : Layouter
		{
			private readonly MeasureOverrideHandler _measureOverrideHandler;
			private readonly ArrangeOverrideHandler _arrangeOverrideHandler;

			public delegate Size ArrangeOverrideHandler(Size finalSize);
			public delegate Size MeasureOverrideHandler(Size availableSize);

			public FrameworkElementLayouter(IFrameworkElement element, MeasureOverrideHandler measureOverrideHandler, ArrangeOverrideHandler arrangeOverrigeHandler) : base(element)
			{
				_measureOverrideHandler = measureOverrideHandler;
				_arrangeOverrideHandler = arrangeOverrigeHandler;
			}

			public Size MeasureElement(View element, Size availableSize) => MeasureChild(element, availableSize);

			public void ArrangeElement(View element, Rect finalRect) => ArrangeChild(element, finalRect);

			protected override string Name => Panel.Name;

			protected override Size ArrangeOverride(Size finalSize) => _arrangeOverrideHandler(finalSize);

#if XAMARIN_ANDROID
			protected override void MeasureChild(View view, int widthSpec, int heightSpec) => view.Measure(widthSpec, heightSpec);
#endif

			protected override Size MeasureOverride(Size availableSize) => _measureOverrideHandler(availableSize);
		}
#endif
	}
}
