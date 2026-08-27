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

#if UNO_REFERENCE_API
using View = Microsoft.UI.Xaml.UIElement;
using ViewGroup = Microsoft.UI.Xaml.UIElement;
using System.Buffers;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
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
public partial class ContentPresenter : FrameworkElement
#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
	, ICustomClippingElement
#endif
{

	private View _contentTemplateRoot;

	public ContentPresenter()
	{
		UpdateLastUsedTheme();

		InitializePlatform();
	}

	public BrushTransition BackgroundTransition { get; set; }

	private protected override ContainerVisual CreateElementVisual() => Compositor.GetSharedCompositor().CreateBorderVisual();

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
				options: ContentPropertyOptions,
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
		this.UpdateBackgroundSizing();
		base.OnBackgroundSizingChangedInner(e);
	}

	#endregion

	#region Foreground Dependency Property

	public
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

	#region IsTextScaleFactorEnabled

	public bool IsTextScaleFactorEnabled
	{
		get => (bool)GetValue(IsTextScaleFactorEnabledProperty);
		set => SetValue(IsTextScaleFactorEnabledProperty, value);
	}

	public static DependencyProperty IsTextScaleFactorEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsTextScaleFactorEnabled),
			typeof(bool),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(
				true,
#if __SKIA__
				// AffectsMeasure only needed where Uno's own measure path calls GetScaledFontSize().
				FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure
#else
				FrameworkPropertyMetadataOptions.Inherits
#endif
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
				FrameworkPropertyMetadataOptions.AffectsArrange
			)
		);

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
				FrameworkPropertyMetadataOptions.AffectsArrange
			)
		);

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
		// TODO: https://github.com/unoplatform/uno/issues/16705
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
		this.UpdateBorderThickness();
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
		this.UpdateBorderBrush();
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
		this.UpdateCornerRadius();
	}

	#endregion

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

	private void TrySetDataContextFromContent(object value)
	{
		// Uno specific: WinUI's SetDataContext pushes Content onto the local DataContext unconditionally.
		// Here Content may itself be the result of a binding with no explicit source (no TemplatedParent,
		// no ElementName), which resolves against the inherited DataContext - overwriting that DataContext
		// re-evaluates the binding against the new value and yields garbage.
		if (GetBindingExpression(ContentProperty) is { } expression &&
			!expression.IsExplicitlySourced &&
			!expression.ParentBinding.IsTemplateBinding)
		{
			return;
		}

		if (value == null || value is View)
		{
			this.ClearValue(DataContextProperty, DependencyPropertyValuePrecedences.Local);
		}
		else
		{
			DataContext = value;
		}
	}

	protected virtual void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
	{
		Invalidate(true);

		DataTemplate contentTemplate = null;
		if (newContentTemplate is null)
		{
			var contentTemplateSelector = ContentTemplateSelector;
			if (contentTemplateSelector is not null)
			{
				var content = Content;
				contentTemplate = contentTemplateSelector.SelectTemplate(content, this) ?? contentTemplateSelector.SelectTemplate(content) /*Uno-specific*/;
			}
			SelectedContentTemplate = contentTemplate;
		}
	}

	protected virtual void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
	{
		var contentTemplate = ContentTemplate;
		if (contentTemplate is null)
		{
			if (newContentTemplateSelector is not null)
			{
				var content = Content;
				contentTemplate = newContentTemplateSelector.SelectTemplate(content, this) ?? newContentTemplateSelector.SelectTemplate(content)/*Uno-specific*/;
			}

			SelectedContentTemplate = contentTemplate;
		}
	}

	private View ContentTemplateRoot
	{
		get
		{
			return _contentTemplateRoot;
		}

		set
		{
			var previousValue = _contentTemplateRoot;

			if (previousValue != null)
			{
				UpdateContentTransitions(this.ContentTransitions, null);
			}

			_contentTemplateRoot = value;

			if (_contentTemplateRoot != null)
			{
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

	public Brush Background
	{
		get => (Brush)GetValue(BackgroundProperty);
		set => SetValue(BackgroundProperty, value);
	}

	public static DependencyProperty BackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(Background),
			typeof(Brush),
			typeof(ContentPresenter),
			new FrameworkPropertyMetadata(null, propertyChangedCallback: (s, e) => ((ContentPresenter)s)?.OnBackgroundChanged(e)));

	/// <remarks>
	/// Don't call base, the UpdateBorder() method handles drawing the background.
	/// </remarks>
	private protected virtual void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
	{
		this.UpdateBackground();
		BorderHelper.SetUpBrushTransitionIfAllowed(
			(BorderVisual)this.Visual,
			e.OldValue as Brush,
			e.NewValue as Brush,
			this.BackgroundTransition,
			((DependencyObject)this).GetCurrentHighestValuePrecedence(BackgroundProperty) == DependencyPropertyValuePrecedences.Animations);
	}

	internal override void UpdateThemeBindings(ResourceUpdateReason updateReason)
	{
		base.UpdateThemeBindings(updateReason);
		UpdateLastUsedTheme();
	}

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


	internal string GetTextBlockText()
	{
		if (IsUsingDefaultTemplate && ContentTemplateRoot is TextBlock tb)
		{
			return tb.Text;
		}

		return null;
	}

	private Lazy<INativeElementHostingExtension> _nativeElementHostingExtension;
	private static readonly HashSet<ContentPresenter> _nativeHosts = new();

	private bool _nativeElementAttached;
	private SerialDisposable _frameRenderedDisposable = new();
	private Rect? _lastArrangeRect;

	internal static bool HasNativeElements() => _nativeHosts.Count > 0;

	partial void InitializePlatform()
	{
		_nativeElementHostingExtension = new Lazy<INativeElementHostingExtension>(() =>
		{
			try
			{
				ApiExtensibility.CreateInstance<INativeElementHostingExtension>(this, out var extension);
				return extension;
			}
			catch (Exception e) // this catches weird cases like an enqueued Loaded event on a ContentPresenter that dispatches after the window of that ContentPresenter is closed
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Couldn't create an {nameof(INativeElementHostingExtension)}.", e);
				}
				return null;
			}
		});
	}

	partial void TryRegisterNativeElement(object oldValue, object newValue)
	{
		if (IsNativeHost && IsInLiveTree)
		{
			DetachNativeElement(oldValue);
		}

		if (_nativeElementHostingExtension.Value?.IsNativeElement(newValue) ?? false)
		{
			IsNativeHost = true;
			Visual.SetAsNativeHostVisual(true);

			if (ContentTemplate is not null)
			{
				throw new InvalidOperationException("ContentTemplate cannot be set when the Content is a native element");
			}
			if (ContentTemplateSelector is not null)
			{
				throw new InvalidOperationException("ContentTemplateSelector cannot be set when the Content is a native element");
			}

			if (IsInLiveTree)
			{
				//If in visual tree, attach immediately. If not, don't attach since Enter will attach later.
				AttachNativeElement();
				ArrangeNativeElement();
			}
		}
		else
		{
			IsNativeHost = false;
			Visual.SetAsNativeHostVisual(null);
		}
	}

	partial void AttachNativeElement()
	{
#if DEBUG
		global::System.Diagnostics.Debug.Assert(IsNativeHost && XamlRoot is not null && !_nativeElementAttached);
#endif
		_nativeElementAttached = true;
		_nativeElementHostingExtension.Value!.AttachNativeElement(Content);
		_nativeHosts.Add(this);
		var ct = ((CompositionTarget)Visual.CompositionTarget)!;
		ct.FrameRendered += OnFrameRendered;
		_frameRenderedDisposable.Disposable = Disposable.Create(() => ct.FrameRendered -= OnFrameRendered);
	}

	private void OnFrameRendered()
	{
		if (_nativeElementAttached)
		{
			ArrangeNativeElement();
		}
	}

	partial void DetachNativeElement(object content)
	{
#if DEBUG
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
#endif
		_nativeHosts.Remove(this);
		_lastArrangeRect = null;
		_frameRenderedDisposable.Disposable = null;
		if (_nativeElementAttached)
		{
			_nativeElementAttached = false;
			_nativeElementHostingExtension.Value!.DetachNativeElement(content);
		}
	}

	private Size MeasureNativeElement(Size childMeasuredSize, Size availableSize)
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
		var ret = _nativeElementHostingExtension.Value!.MeasureNativeElement(Content, childMeasuredSize, availableSize);
		if (ret.Width is double.PositiveInfinity or double.NaN)
		{
			ret.Width = 0;
		}
		if (ret.Height is double.PositiveInfinity or double.NaN)
		{
			ret.Height = 0;
		}
		return ret;
	}

	internal static void UpdateNativeHostContentPresentersOpacities()
	{
		foreach (var contentPresenter in _nativeHosts)
		{
			double finalOpacity = 1;
			UIElement parent = contentPresenter;
			while (parent is not null)
			{
				finalOpacity *= parent.Opacity;
				parent = parent.GetParent() as UIElement;
			}

			contentPresenter._nativeElementHostingExtension!.Value.ChangeNativeElementOpacity(contentPresenter.Content, finalOpacity);
		}
	}

	/// <remarks>
	/// <see cref="nativeVisualsInZOrder"/> is read-only and won't be modified.
	/// </remarks>
	internal static void OnNativeHostsRenderOrderChanged(List<Visual> nativeVisualsInZOrder)
	{
		var rentedArray = ArrayPool<(int, ContentPresenter)>.Shared.Rent(_nativeHosts.Count);
		using var _ = new DisposableStruct<(int, ContentPresenter)[]>(static rentedArray => ArrayPool<(int, ContentPresenter)>.Shared.Return(rentedArray, clearArray: true), rentedArray);

		var count = 0;
		foreach (var host in _nativeHosts)
		{
			rentedArray[count++] = (nativeVisualsInZOrder.IndexOf(host.Visual), host);
		}
		new Span<(int, ContentPresenter)>(rentedArray, 0, _nativeHosts.Count).Sort((one, two) => one.Item1 - two.Item1);

		for (var index = 0; index < _nativeHosts.Count; index++)
		{
			var host = rentedArray[index].Item2;
			var order = rentedArray[index].Item1;

			if (host._nativeElementAttached)
			{
				if (order == -1)
				{
					// We're detaching the native element as it's no longer in view, but conceptually, it's still in the tree, so IsNativeHost is still true
					Debug.Assert(host.IsNativeHost);
					host._nativeElementAttached = false;
					host._lastArrangeRect = null;
					host._frameRenderedDisposable.Disposable = null;
					host._nativeElementHostingExtension.Value!.DetachNativeElement(host.Content);
				}
				else
				{
					if (host._nativeElementHostingExtension.Value.SupportsZIndex())
					{
						host._nativeElementHostingExtension.Value.SetZIndex(host.Content, index);
					}
					else
					{
						host.DetachNativeElement(host.Content);
						host.AttachNativeElement();
						host.ArrangeNativeElement();
					}
				}
			}
			else if (order != -1)
			{
				host.AttachNativeElement();
				host.ArrangeNativeElement();
				if (host._nativeElementHostingExtension.Value.SupportsZIndex())
				{
					host._nativeElementHostingExtension.Value.SetZIndex(host.Content, index);
				}
			}
		}
	}

	private void ArrangeNativeElement()
	{
		var arrangeRect = this.GetAbsoluteBoundsRect();
		if (_lastArrangeRect != arrangeRect)
		{
			_lastArrangeRect = arrangeRect;
			_nativeElementHostingExtension.Value!.ArrangeNativeElement(Content, arrangeRect);
		}
	}

	internal object CreateSampleComponent(string text)
	{
		return _nativeElementHostingExtension.Value?.CreateSampleComponent(text);
	}
}
