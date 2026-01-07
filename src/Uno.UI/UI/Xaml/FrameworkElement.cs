#pragma warning disable CS0105 // Ignore duplicate namespaces, to remove when moving to WinUI source tree.

using Uno.Diagnostics.Eventing;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Uno.UI;
using System.Linq;
using Windows.Foundation;
using Uno.UI.Controls;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Uno;
using Uno.Disposables;
using Windows.UI.Core;
using System.ComponentModel;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Data;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Media;

#if __ANDROID__
using View = Android.Views.View;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
using UIKit;
#else
using Color = System.Drawing.Color;
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement : UIElement, IFrameworkElement, IFrameworkElementInternal, ILayoutConstraints, IDependencyObjectParse
#if !UNO_REFERENCE_API
		, ILayouterElement
#endif
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

#if !UNO_REFERENCE_API
		private FrameworkElementLayouter _layouter;

		ILayouter ILayouterElement.Layouter => _layouter;
		Size ILayouterElement.LastAvailableSize => m_previousAvailableSize;
		bool ILayouterElement.IsMeasureDirty => IsMeasureDirty;
		bool ILayouterElement.IsFirstMeasureDoneAndManagedElement => IsFirstMeasureDone;
		bool ILayouterElement.IsMeasureDirtyPathDisabled => IsMeasureDirtyPathDisabled;
#endif

		private bool _defaultStyleApplied;

		private ResourceDictionary _resources;

		private static readonly Uri DefaultBaseUri = new Uri("ms-appx://local");

		private string _baseUriFromParser;
		private Uri _baseUri;

		private protected bool IsDefaultStyleApplied => _defaultStyleApplied;

		/// <summary>
		/// The current user-determined 'active Style'. This will either be the explicitly-set Style, if there is one, or otherwise the resolved implicit Style (either in the view hierarchy or in Application.Resources).
		/// </summary>
		private Style _activeStyle;

		/// <summary>
		/// Cache for the current type key for faster implicit style lookup
		/// </summary>
		private SpecializedResourceDictionary.ResourceKey _thisTypeResourceKey;

		/// <summary>
		/// Sets whether constraint-based optimizations are used to limit redrawing of the entire visual tree on Android. This can be
		/// globally set to false if it is causing visual errors (eg views not updating properly). Note: this can still be overridden by
		/// the <see cref="AreDimensionsConstrained"/> flag set on individual elements.
		/// </summary>
		public static bool UseConstraintOptimizations { get; set; }

		/// <summary>
		/// If manually set, this flag overrides the constraint-based reasoning for optimizing layout calls. This may be useful for
		/// example if there are custom views in the visual hierarchy that do not implement <see cref="ILayoutConstraints"/>.
		/// </summary>
		public bool? AreDimensionsConstrained { get; set; }

		/// <summary>
		/// Indicates that this view can participate in layout optimizations using the simplest logic.
		/// </summary>
		protected virtual bool IsSimpleLayout => false;

		/// <summary>
		/// Flag for whether this FrameworkElement has a Style set by an ItemsControl. This typically happens when the user provides an explicit container
		/// in XAML, but does not set a local style for the container.
		/// </summary>
		internal bool IsStyleSetFromItemsControl { get; set; }

		#region Tag Dependency Property

#if __APPLE_UIKIT__ || __ANDROID__
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


		#region BackgroundSizing Dependency Property (handlers)

		// Actual BackgroundSizing property is define in some elements implementing it:
		// Border, ContentPresenter, Grid, RelativePanel & StackPanel

		internal BackgroundSizing InternalBackgroundSizing { get; set; }

		private protected virtual void OnBackgroundSizingChangedInner(DependencyPropertyChangedEventArgs e)
		{
			InternalBackgroundSizing = (BackgroundSizing)e.NewValue;
			OnBackgroundSizingChangedPartial(e);
#if __SKIA__
			(this as IBorderInfoProvider)?.UpdateBackgroundSizing();
#endif
		}

		partial void OnBackgroundSizingChangedPartial(DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs);

		#endregion

		#region FlowDirection Dependency Property
#if !SUPPORTS_RTL
		[NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "__WASM__")]
#endif
		public FlowDirection FlowDirection
		{
			get => GetFlowDirectionValue();
			set => SetFlowDirectionValue(value);
		}

#if !SUPPORTS_RTL
		[NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "__WASM__")]
#endif
		[GeneratedDependencyProperty(DefaultValue = FlowDirection.LeftToRight, Options =
#if SUPPORTS_RTL
			FrameworkPropertyMetadataOptions.AffectsMeasure |
#endif
			FrameworkPropertyMetadataOptions.Inherits)]
		public static DependencyProperty FlowDirectionProperty { get; } = CreateFlowDirectionProperty();

		#endregion
		internal void RaiseSizeChanged(SizeChangedEventArgs args)
		{
#if !__NETSTD_REFERENCE__ && !IS_UNIT_TESTS
			SizeChanged?.Invoke(this, args);
#if !UNO_HAS_ENHANCED_LIFECYCLE
			_renderTransform?.UpdateSize(args.NewSize);
#endif
#endif
		}

#if UNO_HAS_ENHANCED_LIFECYCLE
		internal void UpdateRenderTransformSize(Size newSize)
			=> _renderTransform?.UpdateSize(newSize);
#endif

#if !IS_UNIT_TESTS
		private protected override double GetActualHeight()
		{
			var height = Height;
			if (double.IsNaN(height))
			{
				height = Math.Min(MinHeight, double.PositiveInfinity);
			}

			if (IsMeasureDirty && !HasLayoutStorage)
			{
				height = 0;
			}
			else if (HasLayoutStorage)
			{
				height = RenderSize.Height;
			}
			else
			{
				height = ComputeHeightInMinMaxRange(height);
			}

			return height;
		}

		private protected override double GetActualWidth()
		{
			var width = Width;
			if (double.IsNaN(width))
			{
				width = Math.Min(MinWidth, double.PositiveInfinity);
			}

			if (IsMeasureDirty && !HasLayoutStorage)
			{
				width = 0;
			}
			else if (HasLayoutStorage)
			{
				width = RenderSize.Width;
			}
			else
			{
				width = ComputeWidthInMinMaxRange(width);
			}

			return width;
		}

		private double ComputeWidthInMinMaxRange(double width)
			=> Math.Max(Math.Min(width, MaxWidth), MinWidth);

		private double ComputeHeightInMinMaxRange(double height)
			=> Math.Max(Math.Min(height, MaxHeight), MinHeight);
#endif

		partial void Initialize()
		{
#if !UNO_REFERENCE_API
			_layouter = new FrameworkElementLayouter(this, MeasureOverride, ArrangeOverride);
#endif

			IFrameworkElementHelper.Initialize(this);
		}

		public
#if __ANDROID__
		new
#endif
		Microsoft.UI.Xaml.ResourceDictionary Resources
		{
			get => _resources ??= new ResourceDictionary();
			set
			{
				_resources = value;
				_resources.InvalidateNotFoundCache(true);
			}
		}

		/// <summary>
		/// Tries getting the ResourceDictionary without initializing it.
		/// </summary>
		/// <returns>A ResourceDictionary instance or null</returns>
		internal Microsoft.UI.Xaml.ResourceDictionary TryGetResources()
			=> _resources;

		/// <summary>
		/// Gets the parent of this FrameworkElement in the object tree.
		/// </summary>
		public
#if __ANDROID__
		new
#endif
		DependencyObject Parent =>
			LogicalParentOverride ??
			((IDependencyObjectStoreProvider)this).Store.Parent as DependencyObject;

#if !__NETSTD_REFERENCE__
		internal bool HasParent() => Parent != null;
#endif

		public global::System.Uri BaseUri
		{
			get
			{
				if (_baseUri is null)
				{
					_baseUri = _baseUriFromParser is null ? DefaultBaseUri : new Uri(_baseUriFromParser);
				}

				return _baseUri;
			}
		}

		/// <summary>
		/// Allows to override the publicly-visible <see cref="Parent"/> without modifying DP propagation.
		/// </summary>
		internal DependencyObject LogicalParentOverride { get; set; }

		/// <summary>
		/// Provides the managed visual parent of the element. This property can be overriden for specific
		/// scenarios, for example in case of SelectorItem, where actual parent is null, but visual parent
		/// is the list.
		/// </summary>
		internal virtual UIElement VisualParent => ((IDependencyObjectStoreProvider)this).Store.Parent as UIElement;

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
#if __ANDROID__ || __APPLE_UIKIT__
			var child = this.FindFirstChild();
			return child is not null && child is not UIElement
				? MeasureElement(child, availableSize)
				: new Size(0, 0);
#else
			return default;
#endif
		}

		/// <summary>
		/// Provides the behavior for the "Arrange" pass of layout. Classes can override this method to define their own "Arrange" pass behavior.
		/// </summary>
		/// <param name="finalSize">The final area within the parent that this object should use to arrange itself and its children. </param>
		/// <returns>The actual size that is used after the element is arranged in layout.</returns>
		protected virtual Size ArrangeOverride(Size finalSize)
		{
#if __ANDROID__ || __APPLE_UIKIT__
			var child = this.FindFirstChild();
			if (child is not null && child is not UIElement)
			{
				ArrangeElement(child, new Rect(0, 0, finalSize.Width, finalSize.Height));
			}
#endif
			return finalSize;
		}

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
#if UNO_REFERENCE_API
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
#if UNO_REFERENCE_API
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
#if UNO_REFERENCE_API
			return view.DesiredSize;
#else
			return (_layouter as ILayouter).GetDesiredSize(view);
#endif
		}

		partial void OnLoadingPartial()
		{
			this.StoreTryEnableHardReferences();

			if (RequestedTheme is not ElementTheme.Default)
			{
				SyncRootRequestedTheme();
			}

			// Apply active style and default style when we enter the visual tree, if they haven't been applied already.
			ApplyStyles();

			// This is replicating the UpdateAllThemeReferences call in Enter in WinUI.
			// Updates theme references to account for new ancestor theme dictionaries.
			this.UpdateResourceBindings();
		}

		partial void OnUnloadedPartial()
		{
			this.StoreDisableHardReferences();
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
			try
			{
				ApplyStyles();
			}
			finally
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

		public static DependencyProperty StyleProperty { get; } =
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
			OnStyleChanged(oldActiveStyle, _activeStyle, DependencyPropertyValuePrecedences.ExplicitOrImplicitStyle);
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

		internal Style GetActiveStyle() => _activeStyle;

		private SpecializedResourceDictionary.ResourceKey ThisTypeResourceKey
		{
			get
			{
				if (_thisTypeResourceKey.IsEmpty)
				{
					_thisTypeResourceKey = this.GetType();
				}

				return _thisTypeResourceKey;
			}
		}

		private protected Style ResolveImplicitStyle() => this.StoreGetImplicitStyle(ThisTypeResourceKey);

		#region Requested theme dependency property

		// TODO Uno: ActualTheme should always be initialized with Application.Current.RequestedTheme,
		// and should trigger ActualThemeChanged, when the element enters the visual tree, where some
		// higher-level element has explicitly changed its RequestedTheme. This may could start working
		// automatically when the RequestedTheme property supports inheritance.

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
				new FrameworkPropertyMetadata(
					ElementTheme.Default,
					(o, e) => ((FrameworkElement)o).OnRequestedThemeChanged((ElementTheme)e.OldValue, (ElementTheme)e.NewValue)));

		private void OnRequestedThemeChanged(ElementTheme oldValue, ElementTheme newValue)
		{
			SyncRootRequestedTheme();

			if (ActualThemeChanged != null)
			{
				var actualThemeChanged =
					// 1. Previously was default, and new explicit value differs from application theme
					(oldValue == ElementTheme.Default && Application.Current?.ActualElementTheme != newValue) ||
					// 2. Previously was explicit, and new ActualTheme is different
					(oldValue != ElementTheme.Default && oldValue != ActualTheme);

				if (actualThemeChanged)
				{
					ActualThemeChanged?.Invoke(this, null);
				}
			}
		}

		private void SyncRootRequestedTheme()
		{
			if (XamlRoot?.Content == this) // Some elements like TextBox set RequestedTheme in their Focused style, so only listen to changes to root view
			{
				Application.Current.SyncRequestedThemeFromXamlRoot(XamlRoot);
			}
		}


		#endregion

		/// <summary>
		/// Gets or sets a value that determines the light-dark
		/// preference for the overall theme of an app.
		/// </summary>
		/// <remarks>
		/// This is always either Dark or Light. By default the color matches Application.Current.RequestedTheme.
		/// When the FrameworkElement.RequestedTheme has non-default value, it has precedence.
		/// When the value changes ActualThemeChanged event is triggered.
		/// </remarks>
		public ElementTheme ActualTheme => RequestedTheme == ElementTheme.Default ?
			(Application.Current?.ActualElementTheme ?? ElementTheme.Light) :
			RequestedTheme;

		/// <summary>
		/// Occurs when the ActualTheme property value has changed.
		/// </summary>
		public event TypedEventHandler<FrameworkElement, object> ActualThemeChanged;

		[GeneratedDependencyProperty]
		public static DependencyProperty FocusVisualSecondaryThicknessProperty { get; } = CreateFocusVisualSecondaryThicknessProperty();

		public Thickness FocusVisualSecondaryThickness
		{
			get => GetFocusVisualSecondaryThicknessValue();
			set => SetFocusVisualSecondaryThicknessValue(value);
		}

		private static Thickness GetFocusVisualSecondaryThicknessDefaultValue() => new Thickness(1);

		[GeneratedDependencyProperty(DefaultValue = default(Brush))]
		public static DependencyProperty FocusVisualSecondaryBrushProperty { get; } = CreateFocusVisualSecondaryBrushProperty();

		public Brush FocusVisualSecondaryBrush
		{
			get => GetFocusVisualSecondaryBrushValue();
			set => SetFocusVisualSecondaryBrushValue(value);
		}

		[GeneratedDependencyProperty]
		public static DependencyProperty FocusVisualPrimaryThicknessProperty { get; } = CreateFocusVisualPrimaryThicknessProperty();

		public Thickness FocusVisualPrimaryThickness
		{
			get => GetFocusVisualPrimaryThicknessValue();
			set => SetFocusVisualPrimaryThicknessValue(value);
		}

		private static Thickness GetFocusVisualPrimaryThicknessDefaultValue() => new Thickness(2);

		public Brush FocusVisualPrimaryBrush
		{
			get => GetFocusVisualPrimaryBrushValue();
			set => SetFocusVisualPrimaryBrushValue(value);
		}

		[GeneratedDependencyProperty(DefaultValue = default(Brush))]
		public static DependencyProperty FocusVisualPrimaryBrushProperty { get; } = CreateFocusVisualPrimaryBrushProperty();

		public Thickness FocusVisualMargin
		{
			get => GetFocusVisualMarginValue();
			set => SetFocusVisualMarginValue(value);
		}

		private static Thickness GetFocusVisualMarginDefaultValue() => Thickness.Empty;

		[GeneratedDependencyProperty]
		public static DependencyProperty FocusVisualMarginProperty { get; } = CreateFocusVisualMarginProperty();

		/// <summary>
		/// Gets or sets whether a disabled control can receive focus.
		/// </summary>
		public bool AllowFocusWhenDisabled
		{
			get => GetAllowFocusWhenDisabledValue();
			set => SetAllowFocusWhenDisabledValue(value);
		}

		/// <summary>
		/// Identifies the AllowFocusWhenDisabled  dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = false, Options = FrameworkPropertyMetadataOptions.Inherits)]
		public static DependencyProperty AllowFocusWhenDisabledProperty { get; } = CreateAllowFocusWhenDisabledProperty();

		/// <summary>
		/// Gets or sets a value that indicates whether the element automatically gets focus when the user interacts with it.
		/// </summary>
		public bool AllowFocusOnInteraction
		{
			get => GetAllowFocusOnInteractionValue();
			set => SetAllowFocusOnInteractionValue(value);
		}

		/// <summary>
		/// Identifies for the AllowFocusOnInteraction dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = true, Options = FrameworkPropertyMetadataOptions.Inherits)]
		public static DependencyProperty AllowFocusOnInteractionProperty { get; } = CreateAllowFocusOnInteractionProperty();

		internal virtual
#if __ANDROID__
			new
#endif
			bool HasFocus()
		{
			var focusManager = VisualTree.GetFocusManagerForElement(this);
			return focusManager?.FocusedElement == this;
		}

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
		/// <remarks>
		/// The default app-wide style is always applied (using the lower priority ImplicitStyle) so that setters that are not
		/// set by a tree-provided style are still applied. (e.g. a tree-provided implicit style may only change the Foreground of a Button,
		/// and the Template property still needs to be applied for the template to work).
		/// </remarks>
		private protected void ApplyDefaultStyle()
		{
			if (_defaultStyleApplied)
			{
				return;
			}
			_defaultStyleApplied = true;
			((IDependencyObjectStoreProvider)this).Store.SetLastUsedTheme(Application.Current?.RequestedThemeForResources);

			var style = Style.GetDefaultStyleForInstance(this, GetDefaultStyleKey());

			OnStyleChanged(null, style, DependencyPropertyValuePrecedences.DefaultStyle);
#if DEBUG
			AppliedDefaultStyle = style;
#endif
		}

		/// <summary>
		/// This returns <see cref="Control.DefaultStyleKey"/> for Control subclasses, and null for all other types.
		/// </summary>
		private protected virtual Type GetDefaultStyleKey() => null;

		protected virtual void OnApplyTemplate()
		{
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
		{
			if (FeatureConfiguration.FrameworkElement.UseLegacyHitTest)
			{
				return Background != null;
			}

			return false;
		}

		/// <summary>
		/// The list of available children render phases, if this
		/// control is the root element of a DataTemplate.
		/// </summary>
		internal int[] DataTemplateRenderPhases { get; set; }

		internal bool GoToElementState(string stateName, bool useTransitions) => GoToElementStateCore(stateName, useTransitions);

		protected virtual bool GoToElementStateCore(string stateName, bool useTransitions) => false;

		#region LayoutUpdated

		private event EventHandler<object> _layoutUpdated;

		public event EventHandler<object> LayoutUpdated
		{
			add
			{
#if UNO_HAS_ENHANCED_LIFECYCLE
				var isFirstSubscriber = _layoutUpdated is null;
#endif

				_layoutUpdated += value;

#if UNO_HAS_ENHANCED_LIFECYCLE
				if (isFirstSubscriber)
				{
					Uno.UI.Extensions.DependencyObjectExtensions.GetContext(this).EventManager.AddLayoutUpdatedEventHandler(this);
				}
#endif
			}
			remove
			{
#if UNO_HAS_ENHANCED_LIFECYCLE
				var hadSubscribers = _layoutUpdated is not null;
#endif

				_layoutUpdated -= value;

#if UNO_HAS_ENHANCED_LIFECYCLE
				if (hadSubscribers && _layoutUpdated is null)
				{
					Uno.UI.Extensions.DependencyObjectExtensions.GetContext(this).EventManager.RemoveLayoutUpdatedEventHandler(this);
				}
#endif
			}
		}

		// This shouldn't be virtual on enhanced lifecycle as it won't be called if there is no real subscriber to LayoutUpdated.
		internal
#if !UNO_HAS_ENHANCED_LIFECYCLE
			virtual
#endif
			void OnLayoutUpdated()
		{
			_layoutUpdated?.Invoke(null, null);
		}

		#endregion

		private protected virtual Thickness GetBorderThickness() => Thickness.Empty;

		private protected Size MeasureFirstChild(Size availableSize)
		{
			var child = this.FindFirstChild();
			return child != null ? MeasureElement(child, availableSize) : new Size(0, 0);
		}

		private protected Size ArrangeFirstChild(Size finalSize)
		{
			var child = this.FindFirstChild();
			if (child != null)
			{
				ArrangeElement(child, new Rect(0, 0, finalSize.Width, finalSize.Height));
			}

			return finalSize;
		}

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
					Uno.UI.Dispatching.UIAsyncOperation action = null;
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
					action = (Uno.UI.Dispatching.UIAsyncOperation)presenterRoot.Dispatcher.RunAnimation(ApplyPhase);
#elif __APPLE_UIKIT__
					action = (Uno.UI.Dispatching.UIAsyncOperation)presenterRoot.Dispatcher.RunAsync(CoreDispatcherPriority.High, ApplyPhase);
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
		internal virtual void UpdateThemeBindings(ResourceUpdateReason updateReason)
		{
			TryGetResources()?.UpdateThemeBindings(updateReason);
			(this as IDependencyObjectStoreProvider).Store.UpdateResourceBindings(updateReason);

			if (updateReason == ResourceUpdateReason.ThemeResource)
			{
				// Trigger ActualThemeChanged if relevant
				if (ActualThemeChanged != null && RequestedTheme == ElementTheme.Default)
				{
					try
					{
						ActualThemeChanged?.Invoke(this, null);
					}
					catch (Exception e)
					{
						this.LogError()?.Error("An exception was thrown during theme binding updates in response to theme changes.", e);
					}
				}
			}
		}

		#region AutomationPeer
#if !__APPLE_UIKIT__ && !__ANDROID__ // This code is generated in FrameworkElementMixins
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
#endif

		#endregion

#if !UNO_REFERENCE_API
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

#if __ANDROID__
			protected override void MeasureChild(View view, int widthSpec, int heightSpec) => view.Measure(widthSpec, heightSpec);
#endif

			protected override Size MeasureOverride(Size availableSize) => _measureOverrideHandler(availableSize);
		}
#endif

		private protected virtual FrameworkTemplate/*?*/ GetTemplate()
		{
			return null;
		}

		// WinUI overrides this in CalendarViewBaseItemChrome, ListViewBaseItemChrome, and MediaPlayerElement.
		private protected virtual bool HasTemplateChild()
		{
			return GetFirstChild() is not null;
		}

		internal UIElement GetFirstChildNoAddRef() => GetFirstChild();

		internal virtual UIElement/*?*/ GetFirstChild()
		{
#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
			if (GetChildren() is { Count: > 0 } children)
			{
				return children[0] as UIElement;
			}
#elif XAMARIN
			if (this is IShadowChildrenProvider { ChildrenShadow: { Count: > 0 } childrenShadow })
			{
				return childrenShadow[0] as UIElement;
			}
#endif

			if (VisualTreeHelper.GetChildrenCount(this) > 0)
			{
				return VisualTreeHelper.GetChild(this, 0) as UIElement;
			}

			return null;
		}

		internal DependencyObject GetTemplatedParent()
		{
			return (this as IDependencyObjectStoreProvider)?.Store.GetTemplatedParent2();
		}
		internal void SetTemplatedParent(DependencyObject tp)
		{
			(this as IDependencyObjectStoreProvider)?.Store.SetTemplatedParent2(tp);
		}
	}
}
