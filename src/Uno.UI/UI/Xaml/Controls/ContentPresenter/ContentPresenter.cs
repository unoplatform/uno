using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using Uno.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using Microsoft.UI.Composition;
using Uno.UI.Controls;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;

using Point = Windows.Foundation.Point;
using Rect = Windows.Foundation.Rect;

#if __ANDROID__
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif __APPLE_UIKIT__
using UIKit;
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif UNO_REFERENCE_API || IS_UNIT_TESTS
using View = Microsoft.UI.Xaml.UIElement;
using ViewGroup = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Declares a Content presenter
/// </summary>
/// <remarks>
/// The content presenter is used for compatibility with WPF concepts,
/// but the ContentSource property is not available, because there are ControlTemplates for now.
/// </remarks>
[ContentProperty(Name = nameof(Content))]
public partial class ContentPresenter : FrameworkElement, IFrameworkTemplatePoolAware
#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
	, ICustomClippingElement
#endif
{
#if !UNO_HAS_BORDER_VISUAL
	private readonly BorderLayerRenderer _borderRenderer;
#endif

	private bool _firstLoadResetDone;
	private View _contentTemplateRoot;
	private bool _appliedTemplate;

	/// <summary>
	/// Will be set to either the result of ContentTemplateSelector or to ContentTemplate, depending on which is used
	/// </summary>
	private DataTemplate _dataTemplateUsedLastUpdate;

	public ContentPresenter()
	{
#if !UNO_HAS_BORDER_VISUAL
		_borderRenderer = new BorderLayerRenderer(this);
#endif
		UpdateLastUsedTheme();

		InitializePlatform();
	}

#if __ANDROID__ || __APPLE_UIKIT__ || IS_UNIT_TESTS || __WASM__ || __NETSTD_REFERENCE__
	[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
#endif
	public BrushTransition BackgroundTransition { get; set; }

#if UNO_HAS_BORDER_VISUAL
	private protected override ContainerVisual CreateElementVisual() => Compositor.GetSharedCompositor().CreateBorderVisual();
#endif

	partial void InitializePlatform();

	/// <summary>
	/// Indicates if the content should inherit templated parent from the presenter, or its templated parent.
	/// </summary>
	/// <remarks>Clear this flag to let the control nested directly under this ContentPresenter to inherit the correct templated parent</remarks>
	internal bool SynchronizeContentWithOuterTemplatedParent { get; set; } = true;

	/// <summary>
	/// Flag indicating whether the content presenter uses implicit text block to render its content.
	/// </summary>
	internal bool IsUsingDefaultTemplate { get; private set; }

	/// <summary>
	/// Determines if the current ContentPresenter is hosting a native control.
	/// </summary>
	/// <remarks>This is used to alter the propagation of the templated parent.</remarks>
	internal bool IsNativeHost { get; set; }

	internal DataTemplate SelectedContentTemplate => _dataTemplateUsedLastUpdate;

	protected override bool IsSimpleLayout => true;

	#region Content DependencyProperty

	public object Content
	{
		get { return (object)GetValue(ContentProperty); }
		set { SetValue(ContentProperty, value); }
	}

	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(
			nameof(Content),
			typeof(object),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure,
				propertyChangedCallback: (s, e) => ((ContentPresenter)s)?.OnContentChanged(e.OldValue, e.NewValue)
			)
		);

	#endregion

	#region ContentTemplate DependencyProperty

	public DataTemplate ContentTemplate
	{
		get { return (DataTemplate)GetValue(ContentTemplateProperty); }
		set { SetValue(ContentTemplateProperty, value); }
	}

	// Using a DependencyProperty as the backing store for ContentTemplate.  This enables animation, styling, binding, etc...
	public static DependencyProperty ContentTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(ContentTemplate),
			typeof(DataTemplate),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((ContentPresenter)s)?.OnContentTemplateChanged(e.OldValue as DataTemplate, e.NewValue as DataTemplate)
			)
		);
	#endregion

	#region ContentTemplateSelector DependencyProperty

	public DataTemplateSelector ContentTemplateSelector
	{
		get { return (DataTemplateSelector)GetValue(ContentTemplateSelectorProperty); }
		set { SetValue(ContentTemplateSelectorProperty, value); }
	}

	public static DependencyProperty ContentTemplateSelectorProperty { get; } =
		DependencyProperty.Register(
			"ContentTemplateSelector",
			typeof(DataTemplateSelector),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				null,
				(s, e) => ((ContentPresenter)s)?.OnContentTemplateSelectorChanged(e.OldValue as DataTemplateSelector, e.NewValue as DataTemplateSelector)
			)
		);
	#endregion

	#region Transitions Dependency Property

	public TransitionCollection ContentTransitions
	{
		get { return (TransitionCollection)this.GetValue(ContentTransitionsProperty); }
		set { this.SetValue(ContentTransitionsProperty, value); }
	}

	public static DependencyProperty ContentTransitionsProperty { get; } =
		DependencyProperty.Register("ContentTransitions", typeof(TransitionCollection), typeof(ContentPresenter), new FrameworkPropertyMetadata(null, OnContentTransitionsChanged));

	private static void OnContentTransitionsChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		var control = dependencyObject as ContentPresenter;

		if (control != null)
		{
			var oldValue = (TransitionCollection)args.OldValue;
			var newValue = (TransitionCollection)args.NewValue;

			control.UpdateContentTransitions(oldValue, newValue);
		}
	}

	#endregion

	#region BackgroundSizing DepedencyProperty
	[GeneratedDependencyProperty(DefaultValue = default(BackgroundSizing), ChangedCallback = true)]
	public static DependencyProperty BackgroundSizingProperty { get; } = CreateBackgroundSizingProperty();

	public BackgroundSizing BackgroundSizing
	{
		get => GetBackgroundSizingValue();
		set => SetBackgroundSizingValue(value);
	}
	private void OnBackgroundSizingChanged(DependencyPropertyChangedEventArgs e)
	{
#if UNO_HAS_BORDER_VISUAL
		this.UpdateBackgroundSizing();
#else
		UpdateBorder();
#endif
		base.OnBackgroundSizingChangedInner(e);
	}

	#endregion

	#region Foreground Dependency Property

	public
#if __ANDROID__
	new
#endif
	Brush Foreground
	{
		get { return (Brush)this.GetValue(ForegroundProperty); }
		set { this.SetValue(ForegroundProperty, value); }
	}

	public static DependencyProperty ForegroundProperty { get; } =
		DependencyProperty.Register(
			"Foreground",
			typeof(Brush),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				SolidColorBrushHelper.Black,
				FrameworkPropertyMetadataOptions.Inherits,
				propertyChangedCallback: (s, e) => ((ContentPresenter)s)?.OnForegroundColorChanged(e.OldValue as Brush, e.NewValue as Brush)
			)
		);

	#endregion

	#region FontWeight

	public FontWeight FontWeight
	{
		get { return (FontWeight)this.GetValue(FontWeightProperty); }
		set { this.SetValue(FontWeightProperty, value); }
	}

	public static DependencyProperty FontWeightProperty { get; } =
		DependencyProperty.Register(
			nameof(FontWeight),
			typeof(FontWeight),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				FontWeights.Normal,
				FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((ContentPresenter)s)?.OnFontWeightChanged((FontWeight)e.OldValue, (FontWeight)e.NewValue)
			)
		);

	#endregion

	#region FontSize

	public double FontSize
	{
		get { return (double)this.GetValue(FontSizeProperty); }
		set { this.SetValue(FontSizeProperty, value); }
	}

	public static DependencyProperty FontSizeProperty { get; } =
		DependencyProperty.Register(
			nameof(FontSize),
			typeof(double),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				14.0,
				FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((ContentPresenter)s)?.OnFontSizeChanged((double)e.OldValue, (double)e.NewValue)
			)
		);

	#endregion

	#region FontFamily

	public FontFamily FontFamily
	{
		get { return (FontFamily)this.GetValue(FontFamilyProperty); }
		set { this.SetValue(FontFamilyProperty, value); }
	}

	public static DependencyProperty FontFamilyProperty { get; } =
		DependencyProperty.Register(
			nameof(FontFamily),
			typeof(FontFamily),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				FontFamily.Default,
				FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((ContentPresenter)s)?.OnFontFamilyChanged(e.OldValue as FontFamily, e.NewValue as FontFamily)
			)
		);
	#endregion

	#region FontStyle

	public FontStyle FontStyle
	{
		get { return (FontStyle)this.GetValue(FontStyleProperty); }
		set { this.SetValue(FontStyleProperty, value); }
	}

	public static DependencyProperty FontStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(FontStyle),
			typeof(FontStyle),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				FontStyle.Normal,
				FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((ContentPresenter)s)?.OnFontStyleChanged((FontStyle)e.OldValue, (FontStyle)e.NewValue)
			)
		);
	#endregion

	#region FontStretch

	public FontStretch FontStretch
	{
		get => GetFontStretchValue();
		set => SetFontStretchValue(value);
	}

	[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnFontStretchChanged), DefaultValue = FontStretch.Normal, Options = FrameworkPropertyMetadataOptions.Inherits)]
	public static DependencyProperty FontStretchProperty { get; } = CreateFontStretchProperty();
	#endregion

	#region TextWrapping Dependency Property

	public TextWrapping TextWrapping
	{
		get { return (TextWrapping)this.GetValue(TextWrappingProperty); }
		set { this.SetValue(TextWrappingProperty, value); }
	}

	public static DependencyProperty TextWrappingProperty { get; } =
		DependencyProperty.Register(
			"TextWrapping",
			typeof(TextWrapping),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				defaultValue: TextWrapping.NoWrap,
				propertyChangedCallback: (s, e) => ((ContentPresenter)s).OnTextWrappingChanged()
			)
		);

	private void OnTextWrappingChanged()
	{
		OnTextWrappingChangedPartial();
	}

	partial void OnTextWrappingChangedPartial();

	#endregion

	#region MaxLines Dependency Property

	public int MaxLines
	{
		get { return (int)this.GetValue(MaxLinesProperty); }
		set { this.SetValue(MaxLinesProperty, value); }
	}

	public static DependencyProperty MaxLinesProperty { get; } =
		DependencyProperty.Register(
			nameof(MaxLines),
			typeof(int),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				defaultValue: 0,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure,
				propertyChangedCallback: (s, e) => ((ContentPresenter)s).OnMaxLinesChanged()
			)
		);

	private void OnMaxLinesChanged()
	{
		OnMaxLinesChangedPartial();
	}

	partial void OnMaxLinesChangedPartial();

	#endregion

	#region TextTrimming Dependency Property

	public TextTrimming TextTrimming
	{
		get { return (TextTrimming)this.GetValue(TextTrimmingProperty); }
		set { this.SetValue(TextTrimmingProperty, value); }
	}

	public static DependencyProperty TextTrimmingProperty { get; } =
		DependencyProperty.Register(
			"TextTrimming",
			typeof(TextTrimming),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				defaultValue: TextTrimming.None,
				propertyChangedCallback: (s, e) => ((ContentPresenter)s).OnTextTrimmingChanged()
			)
		);

	private void OnTextTrimmingChanged()
	{
		OnTextTrimmingChangedPartial();
	}

	partial void OnTextTrimmingChangedPartial();

	#endregion

	#region TextAlignment Dependency Property

	public
#if __ANDROID__
		new
#endif
		TextAlignment TextAlignment
	{
		get { return (TextAlignment)this.GetValue(TextAlignmentProperty); }
		set { this.SetValue(TextAlignmentProperty, value); }
	}

	public static DependencyProperty TextAlignmentProperty { get; } =
		DependencyProperty.Register(
			"TextAlignment",
			typeof(TextAlignment),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				defaultValue: TextAlignment.Left,
				propertyChangedCallback: (s, e) => ((ContentPresenter)s).OnTextAlignmentChanged()
			)
		);

	private void OnTextAlignmentChanged()
	{
		OnTextAlignmentChangedPartial();
	}

	partial void OnTextAlignmentChangedPartial();

	#endregion

	#region HorizontalContentAlignment DependencyProperty

	public HorizontalAlignment HorizontalContentAlignment
	{
		get => (HorizontalAlignment)this.GetValue(HorizontalContentAlignmentProperty);
		set => this.SetValue(HorizontalContentAlignmentProperty, value);
	}

	public static DependencyProperty HorizontalContentAlignmentProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalContentAlignment),
			typeof(HorizontalAlignment),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				HorizontalAlignment.Stretch,
				FrameworkPropertyMetadataOptions.AffectsArrange,
				(s, e) => ((ContentPresenter)s)?.OnHorizontalContentAlignmentChanged((HorizontalAlignment)e.OldValue, (HorizontalAlignment)e.NewValue)
			)
		);

	protected virtual void OnHorizontalContentAlignmentChanged(HorizontalAlignment oldHorizontalContentAlignment, HorizontalAlignment newHorizontalContentAlignment)
	{
		OnHorizontalContentAlignmentChangedPartial(oldHorizontalContentAlignment, newHorizontalContentAlignment);
	}

	partial void OnHorizontalContentAlignmentChangedPartial(HorizontalAlignment oldHorizontalContentAlignment, HorizontalAlignment newHorizontalContentAlignment);

	#endregion

	#region VerticalContentAlignment DependencyProperty

	public VerticalAlignment VerticalContentAlignment
	{
		get => (VerticalAlignment)this.GetValue(VerticalContentAlignmentProperty);
		set => this.SetValue(VerticalContentAlignmentProperty, value);
	}

	public static DependencyProperty VerticalContentAlignmentProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalContentAlignment),
			typeof(VerticalAlignment),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				VerticalAlignment.Stretch,
				FrameworkPropertyMetadataOptions.AffectsArrange,
				(s, e) => ((ContentPresenter)s)?.OnVerticalContentAlignmentChanged((VerticalAlignment)e.OldValue, (VerticalAlignment)e.NewValue)
			)
		);

	protected virtual void OnVerticalContentAlignmentChanged(VerticalAlignment oldVerticalContentAlignment, VerticalAlignment newVerticalContentAlignment)
	{
		OnVerticalContentAlignmentChangedPartial(oldVerticalContentAlignment, newVerticalContentAlignment);
	}

	partial void OnVerticalContentAlignmentChangedPartial(VerticalAlignment oldVerticalContentAlignment, VerticalAlignment newVerticalContentAlignment);

	#endregion

	#region Padding DependencyProperty

	public Thickness Padding
	{
		get { return (Thickness)GetValue(PaddingProperty); }
		set { SetValue(PaddingProperty, value); }
	}

	public static DependencyProperty PaddingProperty { get; } =
		DependencyProperty.Register(
			"Padding",
			typeof(Thickness),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				(Thickness)Thickness.Empty,
				FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((ContentPresenter)s)?.OnPaddingChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
			)
		);

	private void OnPaddingChanged(Thickness oldValue, Thickness newValue)
	{
#if UNO_HAS_BORDER_VISUAL
		// TODO: https://github.com/unoplatform/uno/issues/16705
#else
		UpdateBorder();
#endif
	}

	#endregion

	#region BorderThickness DependencyProperty

	public Thickness BorderThickness
	{
		get { return (Thickness)GetValue(BorderThicknessProperty); }
		set { SetValue(BorderThicknessProperty, value); }
	}

	public static DependencyProperty BorderThicknessProperty { get; } =
		DependencyProperty.Register(
			nameof(BorderThickness),
			typeof(Thickness),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				(Thickness)Thickness.Empty,
				FrameworkPropertyMetadataOptions.AffectsMeasure,
				(s, e) => ((ContentPresenter)s)?.OnBorderThicknessChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
			)
		);

	private void OnBorderThicknessChanged(Thickness oldValue, Thickness newValue)
	{
#if UNO_HAS_BORDER_VISUAL
		this.UpdateBorderThickness();
#else
		UpdateBorder();
#endif
	}

	#endregion

	#region BorderBrush DependencyProperty

	public Brush BorderBrush
	{
		get { return (Brush)GetValue(BorderBrushProperty); }
		set { SetValue(BorderBrushProperty, value); }
	}

	public static DependencyProperty BorderBrushProperty { get; } =
		DependencyProperty.Register(
			"BorderBrush",
			typeof(Brush),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				null,
				(s, e) => ((ContentPresenter)s)?.OnBorderBrushChanged((Brush)e.OldValue, (Brush)e.NewValue)
			)
		);

	private void OnBorderBrushChanged(Brush oldValue, Brush newValue)
	{
#if __WASM__
		if (((oldValue is null) ^ (newValue is null)) && BorderThickness != default)
		{
			// The transition from null to non-null (and vice-versa) affects child arrange on Wasm when non-zero BorderThickness is specified.
			(Content as UIElement)?.InvalidateArrange();
		}
#endif
#if UNO_HAS_BORDER_VISUAL
		this.UpdateBorderBrush();
#else
		UpdateBorder();
#endif
	}


	#endregion

	#region CornerRadius DependencyProperty
	private static CornerRadius GetCornerRadiusDefaultValue() => CornerRadius.None;

	[GeneratedDependencyProperty(ChangedCallback = true)]
	public static DependencyProperty CornerRadiusProperty { get; } = CreateCornerRadiusProperty();

	public CornerRadius CornerRadius
	{
		get => GetCornerRadiusValue();
		set => SetCornerRadiusValue(value);
	}

	private void OnCornerRadiusChanged(CornerRadius oldValue, CornerRadius newValue)
	{
#if UNO_HAS_BORDER_VISUAL
		this.UpdateCornerRadius();
#else
		UpdateBorder();
#endif
	}

	#endregion

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		// Applying the template will not delete existing visuals. This will be done conditionally
		// when the template is invalidated.
		// Uno specific: since we don't call this early enough, we have to comment out the condition
		// if (GetChildren().Count == 0)
		{
			ContentControl pTemplatedParent = GetTemplatedParent() as ContentControl;

			// Only ContentControl has the two properties below.  Other parents would just fail to bind since they don't have these
			// two content related properties.
			if (pTemplatedParent != null
#if ANDROID || __APPLE_UIKIT__
				&& this is not NativeCommandBarPresenter // Uno specific: NativeCommandBarPresenter breaks if you inherit from the TP
#endif
				)
			{
				// bool needsRefresh = false;
				DependencyProperty pdpTarget;

				// By default Content and ContentTemplate are template are bound.
				// If no template binding exists already then hook them up now
				// pdpTarget = GetPropertyByIndexInline(KnownPropertyIndex::ContentPresenter_SelectedContentTemplate);
				// IFCEXPECT(pdpTarget);
				// if (IsPropertyDefault(pdpTarget) && !IsPropertyTemplateBound(pdpTarget))
				// {
				// 	const CDependencyProperty* pdpSource = pTemplatedParent->GetPropertyByIndexInline(KnownPropertyIndex::ContentControl_SelectedContentTemplate);
				// 	IFCEXPECT(pdpSource);
				//
				// 	IFC(SetTemplateBinding(pdpTarget, pdpSource));
				// 	needsRefresh = true;
				// }

				// UNO Specific: SelectedContentTemplate is not implemented, we hook ContentTemplateSelector instead
				pdpTarget = ContentPresenter.ContentTemplateSelectorProperty;
				global::System.Diagnostics.Debug.Assert(pdpTarget is { });
				var store = ((IDependencyObjectStoreProvider)this).Store;
				if (store.GetCurrentHighestValuePrecedence(pdpTarget) == DependencyPropertyValuePrecedences.DefaultValue &&
					!store.IsPropertyTemplateBound(pdpTarget))
				{
					DependencyProperty pdpSource = ContentControl.ContentTemplateSelectorProperty;
					global::System.Diagnostics.Debug.Assert(pdpSource is { });

					store.SetTemplateBinding(pdpTarget, pdpSource);
					// needsRefresh = true;
				}

				pdpTarget = ContentPresenter.ContentTemplateProperty;
				global::System.Diagnostics.Debug.Assert(pdpTarget is { });
				if (store.GetCurrentHighestValuePrecedence(pdpTarget) == DependencyPropertyValuePrecedences.DefaultValue &&
					!store.IsPropertyTemplateBound(pdpTarget))
				{
					DependencyProperty pdpSource = ContentControl.ContentTemplateProperty;
					global::System.Diagnostics.Debug.Assert(pdpSource is { });

					store.SetTemplateBinding(pdpTarget, pdpSource);
					// needsRefresh = true;
				}

				pdpTarget = ContentPresenter.ContentProperty;
				global::System.Diagnostics.Debug.Assert(pdpTarget is { });
				if (store.GetCurrentHighestValuePrecedence(pdpTarget) == DependencyPropertyValuePrecedences.DefaultValue &&
					!store.IsPropertyTemplateBound(pdpTarget))
				{
					DependencyProperty pdpSource = ContentControl.ContentProperty;
					global::System.Diagnostics.Debug.Assert(pdpSource is { });

					store.SetTemplateBinding(pdpTarget, pdpSource);
					// needsRefresh = true;
				}

				// Uno specific: uno bindings don't work this way
				// Setting up the binding doesn't get you the values.  We need to call refresh to get the latest value
				// for m_pContentTemplate, SelectedContentTemplate and/or m_pContent for the tests below.
				// if (needsRefresh)
				// {
				// 	IFC(pTemplatedParent->RefreshTemplateBindings(TemplateBindingsRefreshType::All));
				// }
			}
		}
	}

	protected virtual void OnForegroundColorChanged(Brush oldValue, Brush newValue)
	{
		OnForegroundColorChangedPartial(oldValue, newValue);
	}

	partial void OnForegroundColorChangedPartial(Brush oldValue, Brush newValue);

	protected virtual void OnFontWeightChanged(FontWeight oldValue, FontWeight newValue)
	{
		OnFontWeightChangedPartial(oldValue, newValue);
	}

	partial void OnFontWeightChangedPartial(FontWeight oldValue, FontWeight newValue);

	protected virtual void OnFontFamilyChanged(FontFamily oldValue, FontFamily newValue)
	{
		OnFontFamilyChangedPartial(oldValue, newValue);
	}

	partial void OnFontFamilyChangedPartial(FontFamily oldValue, FontFamily newValue);

	protected virtual void OnFontSizeChanged(double oldValue, double newValue)
	{
		OnFontSizeChangedPartial(oldValue, newValue);
	}

	partial void OnFontSizeChangedPartial(double oldValue, double newValue);

	protected virtual void OnFontStyleChanged(FontStyle oldValue, FontStyle newValue)
	{
		OnFontStyleChangedPartial(oldValue, newValue);
	}

	partial void OnFontStyleChangedPartial(FontStyle oldValue, FontStyle newValue);

	private protected virtual void OnFontStretchChanged(FontStretch oldValue, FontStretch newValue)
	{
		OnFontStretchChangedPartial(oldValue, newValue);
	}

	partial void OnFontStretchChangedPartial(FontStretch oldValue, FontStretch newValue);

	protected virtual void OnContentChanged(object oldValue, object newValue)
	{
		if (oldValue is View || newValue is View)
		{
			// Make sure not to reuse the previous Content as a ContentTemplateRoot (i.e., in case there's no data template)
			// If setting Content to a new View, recreate the template
			ContentTemplateRoot = null;
		}

		// We need to overrides the local value of DataContext with Content's value here.
		// But if the content value is the result of a binding without explicit sources: TemplatedParent, ElementName...
		// Then we can't do so, because updating the DC will cause the binding to evaluate again, and yield invalid value.
		if (GetBindingExpression(ContentProperty) is not { } expression ||
			expression.IsExplicitlySourced ||
			expression.ParentBinding.IsTemplateBinding)
		{
			TrySetDataContextFromContent(newValue);
		}

		TryRegisterNativeElement(oldValue, newValue);

		SetUpdateTemplate();
	}

	private void TrySetDataContextFromContent(object value)
	{
		if (value == null)
		{
			this.ClearValue(DataContextProperty, DependencyPropertyValuePrecedences.Local);
		}
		else
		{
			if (!(value is View))
			{
				// If the content is not a view, we apply the content as the
				// DataContext of the materialized content.
				DataContext = value;
			}
			else
			{
				// Restore DataContext propagation if the content is a view
				this.ClearValue(DataContextProperty, DependencyPropertyValuePrecedences.Local);
			}
		}
	}

	protected virtual void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
	{
		if (ContentTemplateRoot != null)
		{
			ContentTemplateRoot = null;
		}

		SetUpdateTemplate();
	}

	protected virtual void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
	{
		SetUpdateTemplate();
	}

	partial void UnregisterContentTemplateRoot();

	public View ContentTemplateRoot
	{
		get
		{
			return _contentTemplateRoot;
		}

		protected set
		{
			var previousValue = _contentTemplateRoot;

			if (previousValue != null)
			{
				CleanupView(previousValue);

				UnregisterContentTemplateRoot();

				UpdateContentTransitions(this.ContentTransitions, null);
			}

			_contentTemplateRoot = value;

			if (_contentTemplateRoot != null)
			{
				RegisterContentTemplateRoot();

				UpdateContentTransitions(null, this.ContentTransitions);
			}
		}
	}

	private void UpdateContentTransitions(TransitionCollection oldValue, TransitionCollection newValue)
	{
		var contentRoot = this.ContentTemplateRoot as IFrameworkElement;

		if (contentRoot == null)
		{
			return;
		}

		if (oldValue != null)
		{
			foreach (var item in oldValue)
			{
				item.DetachFromElement(contentRoot);
			}
		}

		if (newValue != null)
		{
			foreach (var item in newValue)
			{
				item.AttachToElement(contentRoot);
			}
		}
	}

	/// <summary>
	/// Cleanup the view from its binding references
	/// </summary>
	/// <param name="previousValue"></param>
	private void CleanupView(View previousValue)
	{
		if (!(previousValue is IFrameworkElement) && previousValue is DependencyObject dependencyObject)
		{
			dependencyObject.SetParent(null);
		}
	}

#if UNO_HAS_ENHANCED_LIFECYCLE
	internal override void EnterImpl(EnterParams @params, int depth)
	{
		base.EnterImpl(@params, depth);

		if (ResetDataContextOnFirstLoad() || ContentTemplateRoot == null)
		{
			SetUpdateTemplate();
		}

#if !UNO_HAS_BORDER_VISUAL
		UpdateBorder();
#endif

		// We do this in Enter not Loaded since Loaded is a lot more tricky
		// (e.g. you can have Unloaded without Loaded, you can have multiple loaded events without unloaded in between, etc.)
		if (IsNativeHost)
		{
			AttachNativeElement();
		}
	}

	internal override void LeaveImpl(LeaveParams @params)
	{
		base.LeaveImpl(@params);

		if (IsNativeHost)
		{
			DetachNativeElement(Content);
		}
	}
#endif

	private protected override void OnLoaded()
	{
		base.OnLoaded();


		// WinUI has some special handling for ContentPresenter and ContentControl where even though they aren't Controls,
		// they use OnApplyTemplate. As a workaround for now, we just call OnApplyTemplate here.
		if (!_appliedTemplate)
		{
			_appliedTemplate = true;
			OnApplyTemplate();
		}


#if !UNO_HAS_ENHANCED_LIFECYCLE
		if (ResetDataContextOnFirstLoad() || ContentTemplateRoot == null)
		{
			SetUpdateTemplate();
		}
#endif

#if !UNO_HAS_ENHANCED_LIFECYCLE
		UpdateBorder();

		if (IsNativeHost)
		{
			AttachNativeElement();
		}
#endif
	}

	private protected override void OnUnloaded()
	{
		base.OnUnloaded();

#if !UNO_HAS_ENHANCED_LIFECYCLE
		if (IsNativeHost)
		{
			DetachNativeElement(Content);
		}
#endif
	}

	private bool ResetDataContextOnFirstLoad()
	{
		if (!_firstLoadResetDone)
		{
			_firstLoadResetDone = true;

			// This test avoids the ContentPresenter from resetting
			// the DataContext to null (or the inherited value) and then back to
			// the content and have two-way bindings propagating the null value
			// back to the source.
			if (!ReferenceEquals(DataContext, Content) &&
				this.GetCurrentHighestValuePrecedence(DataContextProperty) != DependencyPropertyValuePrecedences.Inheritance)
			{
				// On first load UWP clears the local value of a ContentPresenter.
				// The reason for this behavior is unknown.
				this.ClearValue(DataContextProperty, DependencyPropertyValuePrecedences.Local);

				TrySetDataContextFromContent(Content);
			}

			return true;
		}

		return false;
	}

	void IFrameworkTemplatePoolAware.OnTemplateRecycled()
	{
		// This needs to be cleared on recycle, to prevent
		// SetUpdateTemplate from being skipped in OnLoaded.
		_firstLoadResetDone = false;
	}

	protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
	{
		base.OnVisibilityChanged(oldValue, newValue);

		if (oldValue == Visibility.Collapsed && newValue == Visibility.Visible)
		{
			SetUpdateTemplate();
		}
	}

	public void UpdateContentTemplateRoot()
	{
		if (Visibility == Visibility.Collapsed)
		{
			return;
		}

		//If ContentTemplateRoot is null, it must be updated even if the templates haven't changed
		if (ContentTemplateRoot == null)
		{
			_dataTemplateUsedLastUpdate = null;
		}

		//ContentTemplate/ContentTemplateSelector will only be applied to a control with no Template, normally the innermost element
		var dataTemplate = this.ResolveContentTemplate();

		void OnCurrentTemplateUpdated()
		{
			// Force re-materialization on template update
			_dataTemplateUsedLastUpdate = null;
			SetUpdateTemplate();
		}

		// Subscribe to template updates so presenter can refresh when factory changes (when feature is activated)
		if (TemplateManager.IsDataTemplateDynamicUpdateEnabled)
		{
			Uno.UI.TemplateUpdateSubscription.Attach(this, dataTemplate, OnCurrentTemplateUpdated);
		}

		// Only apply the template if it has changed
		if (!object.Equals(dataTemplate, _dataTemplateUsedLastUpdate))
		{
			_dataTemplateUsedLastUpdate = dataTemplate;
			ContentTemplateRoot = dataTemplate?.LoadContentCached(this) ?? Content as View;
			if (ContentTemplateRoot != null)
			{
				IsUsingDefaultTemplate = false;
			}
		}

		if (Content != null
			&& !(Content is View)
			&& ContentTemplateRoot == null
		)
		{
			// Use basic default root for non-View Content if no template is supplied
			SetContentTemplateRootToPlaceholder();
		}

		if (ContentTemplateRoot == null && Content is View contentView && dataTemplate == null)
		{
			// No template and Content is a View, set it directly as root
			ContentTemplateRoot = contentView as View;
		}

		IsUsingDefaultTemplate = ContentTemplateRoot is ImplicitTextBlock;
	}

	private void SetContentTemplateRootToPlaceholder()
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().DebugFormat("No ContentTemplate was specified for {0} and content is not a UIView, defaulting to TextBlock.", GetType().Name);
		}

		if (!IsNativeHost)
		{
			var textBlock = new ImplicitTextBlock(this);
			textBlock.SetTemplatedParent(this);

			TemplateBind(TextBlock.TextProperty, nameof(Content));
			TemplateBind(TextBlock.HorizontalAlignmentProperty, nameof(HorizontalContentAlignment));
			TemplateBind(TextBlock.VerticalAlignmentProperty, nameof(VerticalContentAlignment));
			TemplateBind(TextBlock.TextWrappingProperty, nameof(TextWrapping));
			TemplateBind(TextBlock.MaxLinesProperty, nameof(MaxLines));
			TemplateBind(TextBlock.TextAlignmentProperty, nameof(TextAlignment));

			void TemplateBind(DependencyProperty property, string path) =>
				textBlock.SetBinding(property, new Binding(path)
				{
					RelativeSource = RelativeSource.TemplatedParent
				});

			ContentTemplateRoot = textBlock;
			IsUsingDefaultTemplate = true;
		}
	}

	partial void RegisterContentTemplateRoot();

	/// <remarks>
	/// Don't call base, the UpdateBorder() method handles drawing the background.
	/// </remarks>
	protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
	{
#if UNO_HAS_BORDER_VISUAL
		this.UpdateBackground();
		BorderHelper.SetUpBrushTransitionIfAllowed(
			(BorderVisual)this.Visual,
			e.OldValue as Brush,
			e.NewValue as Brush,
			this.BackgroundTransition,
			((IDependencyObjectStoreProvider)this).Store.GetCurrentHighestValuePrecedence(BackgroundProperty) == DependencyPropertyValuePrecedences.Animations);
#else
		UpdateBorder();
#endif
	}

	internal override void UpdateThemeBindings(ResourceUpdateReason updateReason)
	{
		base.UpdateThemeBindings(updateReason);
		UpdateLastUsedTheme();
	}

#if __ANDROID__
	// Support for the C# collection initializer style.
	public void Add(View view)
	{
		Content = view;
	}

	public IEnumerator GetEnumerator()
	{
		if (Content != null)
		{
			return new[] { Content }.GetEnumerator();
		}
		else
		{
			return Enumerable.Empty<object>().GetEnumerator();
		}
	}
#endif

	protected override Size ArrangeOverride(Size finalSize)
	{
		var child = this.FindFirstChild();

		if (child != null)
		{
			var padding = Padding;
			var borderThickness = BorderThickness;

			var innerRect = new Rect(
				padding.Left + borderThickness.Left,
				padding.Top + borderThickness.Top,
				finalSize.Width - padding.Left - padding.Right - borderThickness.Left - borderThickness.Right,
				finalSize.Height - padding.Top - padding.Bottom - borderThickness.Top - borderThickness.Bottom
			);

			var availableSize = new Size(innerRect.Width, innerRect.Height);

			// Using GetElementDesiredSize to properly handle native controls.
			var desiredSize = GetElementDesiredSize(child);

			var contentWidth = HorizontalContentAlignment == HorizontalAlignment.Stretch ?
				availableSize.Width : desiredSize.Width;
			var contentHeight = VerticalContentAlignment == VerticalAlignment.Stretch ?
				availableSize.Height : desiredSize.Height;
			var contentSize = new Size(contentWidth, contentHeight);

			var offset = CalculateContentOffset(availableSize, contentSize);

			var arrangeRect = new Rect(
				innerRect.X + offset.X,
				innerRect.Y + offset.Y,
				contentSize.Width,
				contentSize.Height);

			if (child is UIElement childAsUIElement)
			{
				childAsUIElement.EnsureLayoutStorage();
			}
			ArrangeElement(child, arrangeRect);
		}

		return finalSize;
	}

	private Point CalculateContentOffset(Size availableSize, Size contentSize)
	{
		var horizontalAlignment = HorizontalContentAlignment;
		var verticalAlignment = VerticalContentAlignment;

		if (horizontalAlignment == HorizontalAlignment.Stretch &&
			contentSize.Width > availableSize.Width)
		{
			horizontalAlignment = HorizontalAlignment.Left;
		}

		if (verticalAlignment == VerticalAlignment.Stretch &&
			contentSize.Height > availableSize.Height)
		{
			verticalAlignment = VerticalAlignment.Top;
		}

		double offsetX;
		if (horizontalAlignment == HorizontalAlignment.Center ||
			horizontalAlignment == HorizontalAlignment.Stretch)
		{
			offsetX = (availableSize.Width - contentSize.Width) / 2;
		}
		else if (horizontalAlignment == HorizontalAlignment.Right)
		{
			offsetX = availableSize.Width - contentSize.Width;
		}
		else
		{
			offsetX = 0;
		}

		double offsetY;
		if (verticalAlignment == VerticalAlignment.Center ||
			verticalAlignment == VerticalAlignment.Stretch)
		{
			offsetY = (availableSize.Height - contentSize.Height) / 2;
		}
		else if (verticalAlignment == VerticalAlignment.Bottom)
		{
			offsetY = availableSize.Height - contentSize.Height;
		}
		else
		{
			offsetY = 0;
		}

		return new Point(offsetX, offsetY);
	}

	protected override Size MeasureOverride(Size size)
	{
		var padding = Padding;
		var borderThickness = BorderThickness;

		var child = this.FindFirstChild();
		Size measuredSize = default;
		if (child is not null)
		{
			measuredSize = MeasureElement(child,
				new Size(
					size.Width - padding.Left - padding.Right - borderThickness.Left - borderThickness.Right,
					size.Height - padding.Top - padding.Bottom - borderThickness.Top - borderThickness.Bottom
				));
			if (child is UIElement childAsUIElement)
			{
				childAsUIElement.EnsureLayoutStorage();
			}
		}

#if UNO_SUPPORTS_NATIVEHOST
		if (IsNativeHost)
		{
			measuredSize = MeasureNativeElement(measuredSize, size);
		}
#endif

		return new Size(
			measuredSize.Width + padding.Left + padding.Right + borderThickness.Left + borderThickness.Right,
			measuredSize.Height + padding.Top + padding.Bottom + borderThickness.Top + borderThickness.Bottom
		);
	}

	private protected override Thickness GetBorderThickness() => BorderThickness;

	internal override bool CanHaveChildren() => true;

	internal override bool IsViewHit() => Border.IsViewHitImpl(this);

	/// <summary>
	/// Registers the provided native element in the native shell
	/// </summary>
	partial void TryRegisterNativeElement(object oldValue, object newValue);

	/// <summary>
	/// Attaches the current native element in the native shell
	/// </summary>
	partial void AttachNativeElement();

	/// <summary>
	/// Detaches the current native element from the native shell
	/// </summary>
	partial void DetachNativeElement(object content);

#if !UNO_HAS_BORDER_VISUAL
	private void UpdateBorder() => _borderRenderer.Update();
#endif

	private void SetUpdateTemplate()
	{
		UpdateContentTemplateRoot();
		SetUpdateTemplatePartial();
	}

	partial void SetUpdateTemplatePartial();

	internal string GetTextBlockText()
	{
		if (IsUsingDefaultTemplate && ContentTemplateRoot is ImplicitTextBlock tb)
		{
			return tb.Text;
		}

		return null;
	}
}
