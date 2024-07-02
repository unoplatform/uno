using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Composition;
using Uno.Extensions;
using Windows.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;
using Uno.UI.Xaml;
#if __ANDROID__
using Android.Views;
using Android.Graphics;
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __IOS__
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using UIKit;
#elif __MACOS__
using View = AppKit.NSView;
using Color = Windows.UI.Color;
#else
using Color = System.Drawing.Color;
using View = Windows.UI.Xaml.UIElement;
#endif
using _Debug = System.Diagnostics.Debug;

using RadialGradientBrush = Microsoft/* UWP don't rename */.UI.Xaml.Media.RadialGradientBrush;
using Uno.UI.Helpers;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

// TODO: Border should be sealed
[ContentProperty(Name = nameof(Child))]
public partial class Border : FrameworkElement
{
	public Border()
	{
#if !UNO_HAS_BORDER_VISUAL
		BorderRenderer = new BorderLayerRenderer(this);
#endif
	}

#if !UNO_HAS_BORDER_VISUAL
	internal BorderLayerRenderer BorderRenderer { get; }
#endif

#if UNO_HAS_BORDER_VISUAL
	private protected override ShapeVisual CreateElementVisual() => Compositor.GetSharedCompositor().CreateBorderVisual();
#endif

	/// <summary>
	/// Support for the C# collection initializer style.
	/// Allows items to be added like this
	/// new Border
	/// {
	///    new Border()
	/// }
	/// </summary>
	/// <param name="view"></param>
	public
#if __IOS__
		new
#endif
		void Add(View view)
	{
		Child = VisualTreeHelper.TryAdaptNative(view);
	}

	protected override bool IsSimpleLayout => true;

	private protected override Thickness GetBorderThickness() => BorderThickness;


	#region Child DependencyProperty

	public UIElement Child
	{
		get => (UIElement)this.GetValue(ChildProperty);
		set => this.SetValue(ChildProperty, value);
	}

	public static DependencyProperty ChildProperty { get; } =
		DependencyProperty.Register(
			"Child",
			typeof(UIElement),
			typeof(Border),
			new FrameworkPropertyMetadata(
				null,
				// Since this is a view, inheritance is handled through the visual tree, rather than via the property. We explicitly
				// disable the property-based propagation here to support the case where the Parent property is overridden to simulate
				// a different inheritance hierarchy, as is done for some controls with native styles.
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
				(s, e) => ((Border)s)?.OnChildChanged((UIElement)e.OldValue, (UIElement)e.NewValue)
			)
		);

	private void OnChildChanged(UIElement oldValue, UIElement newValue)
	{
		ReAttachChildTransitions(oldValue, newValue);

		if (oldValue is not null)
		{
			RemoveChild(oldValue);
		}

		if (newValue is not null)
		{
			AddChild(newValue);
		}
	}

	#endregion

	#region CornerRadius
	private static CornerRadius GetCornerRadiusDefaultValue() => CornerRadius.None;

	[GeneratedDependencyProperty(ChangedCallback = true, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange)]
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

	#region ChildTransitions

	/// <summary>
	/// This is a Transition for a UIElement.
	/// </summary>
	public TransitionCollection ChildTransitions
	{
		get => (TransitionCollection)this.GetValue(ChildTransitionsProperty);
		set => this.SetValue(ChildTransitionsProperty, value);
	}

	// Using a DependencyProperty as the backing store for Transitions.  This enables animation, styling, binding, etc...
	public static DependencyProperty ChildTransitionsProperty { get; } =
		DependencyProperty.Register("ChildTransitions", typeof(TransitionCollection), typeof(Border), new FrameworkPropertyMetadata(null));

	private void ReAttachChildTransitions(UIElement originalChild, UIElement child)
	{
		if (this.ChildTransitions == null)
		{
			return;
		}

		if (!(originalChild is IFrameworkElement oldTargetElement))
		{
			return;
		}

		foreach (var transition in this.ChildTransitions)
		{
			transition.DetachFromElement(oldTargetElement);
		}

		if (!(child is IFrameworkElement targetElement))
		{
			return;
		}

		foreach (var transition in this.ChildTransitions)
		{
			transition.AttachToElement(targetElement);
		}
	}


	#endregion

	#region Padding DependencyProperty
	private static Thickness GetPaddingDefaultValue() => Thickness.Empty;

	[GeneratedDependencyProperty(ChangedCallback = true, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange)]
	public static DependencyProperty PaddingProperty { get; } = CreatePaddingProperty();

	public Thickness Padding
	{
		get => GetPaddingValue();
		set => SetPaddingValue(value);
	}

	private void OnPaddingChanged(Thickness oldValue, Thickness newValue)
	{
#if UNO_HAS_BORDER_VISUAL
		// TODO: https://github.com/unoplatform/uno/issues/16705
#else
		UpdateBorder();
#endif
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

	#region BorderThickness DependencyProperty
	private static Thickness GetBorderThicknessDefaultValue() => Thickness.Empty;

	[GeneratedDependencyProperty(ChangedCallback = true, Options = FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange)]
	public static DependencyProperty BorderThicknessProperty { get; } = CreateBorderThicknessProperty();

	public Thickness BorderThickness
	{
		get => GetBorderThicknessValue();
		set => SetBorderThicknessValue(value);
	}

	private void OnBorderThicknessChanged(Thickness oldValue, Thickness newValue)
	{
#if UNO_HAS_BORDER_VISUAL
		this.UpdateBorderThickness();
#else
		UpdateBorder();
#endif
	}

	#endregion

	#region BorderBrush Dependency Property

	private Action _borderBrushChanged;

#if __ANDROID__
	//This field is never accessed. It just exists to create a reference, because the DP causes issues with ImageBrush of the backing bitmap being prematurely garbage-collected. (Bug with ConditionalWeakTable? https://bugzilla.xamarin.com/show_bug.cgi?id=21620)
	private Brush _borderBrushStrongReference;
#endif

	public Brush BorderBrush
	{
		get => GetBorderBrushValue();
		set
		{
			SetBorderBrushValue(value);

#if __ANDROID__
			_borderBrushStrongReference = value;
#endif
		}
	}

	private static Brush GetBorderBrushDefaultValue() => SolidColorBrushHelper.Transparent;

	[GeneratedDependencyProperty(ChangedCallback = true, Options = FrameworkPropertyMetadataOptions.ValueInheritsDataContext)]
	public static DependencyProperty BorderBrushProperty { get; } = CreateBorderBrushProperty();

	private void OnBorderBrushChanged(Brush oldValue, Brush newValue)
	{
		Brush.SetupBrushChanged(oldValue, newValue, ref _borderBrushChanged, _borderBrushChanged ?? (() =>
		{
#if UNO_HAS_BORDER_VISUAL
			this.UpdateBorderBrush();
#else
			UpdateBorder();
#endif
		}));
#if __WASM__
		if (((oldValue is null) ^ (newValue is null)) && BorderThickness != default)
		{
			// The transition from null to non-null (and vice-versa) affects child arrange on Wasm when non-zero BorderThickness is specified.
			Child?.InvalidateArrange();
		}
#endif
	}

	#endregion

	protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
	{
#if UNO_HAS_BORDER_VISUAL
		this.UpdateBackground();
#else
		UpdateBorder();
#endif
		OnBackgroundChangedPartial();
	}

	partial void OnBackgroundChangedPartial();

	internal override bool CanHaveChildren() => true;

	internal override bool IsViewHit() => IsViewHitImpl(this);

	internal static bool IsViewHitImpl(FrameworkElement element)
	{
		_Debug.Assert(element is Panel
			|| element is Border
			|| element is ContentPresenter
		);

		return element.Background != null;
	}

#if !UNO_HAS_BORDER_VISUAL
	private void UpdateBorder() => BorderRenderer.Update();
#endif
}
