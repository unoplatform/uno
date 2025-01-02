using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives;

internal enum SnapPointApplicableRangeType
{
	Mandatory = 0,
	Optional = 1,
}


public enum ScrollSnapPointsAlignment
{
	Near = 0,
	Center = 1,
	Far = 2,
}


public partial interface IScrollControllerPanningInfo
{
	bool IsRailEnabled { get; }
	Orientation PanOrientation { get; }
	UIElement PanningElementAncestor { get; }

	void SetPanningElementExpressionAnimationSources(
		CompositionPropertySet propertySet,
		string minOffsetPropertyName,
		string maxOffsetPropertyName,
		string offsetPropertyName,
		string multiplierPropertyName);

	event TypedEventHandler<IScrollControllerPanningInfo, object> Changed;
	event TypedEventHandler<IScrollControllerPanningInfo, ScrollControllerPanRequestedEventArgs> PanRequested;
}


public partial interface IScrollController
{
	IScrollControllerPanningInfo PanningInfo { get; }
	bool CanScroll { get; }
	bool IsScrollingWithMouse { get; }
	void SetIsScrollable(bool isScrollable);
	void SetValues(double minOffset, double maxOffset, double offset, double viewportLength);
	CompositionAnimation GetScrollAnimation(
		int correlationId,
		Vector2 startPosition,
		Vector2 endPosition,
		CompositionAnimation defaultAnimation);
	void NotifyRequestedScrollCompleted(int correlationId);
	event TypedEventHandler<IScrollController, object> CanScrollChanged;
	event TypedEventHandler<IScrollController, object> IsScrollingWithMouseChanged;
	event TypedEventHandler<IScrollController, ScrollControllerScrollToRequestedEventArgs> ScrollToRequested;
	event TypedEventHandler<IScrollController, ScrollControllerScrollByRequestedEventArgs> ScrollByRequested;
	event TypedEventHandler<IScrollController, ScrollControllerAddScrollVelocityRequestedEventArgs> AddScrollVelocityRequested;
}


public partial class ScrollPresenter :
	 FrameworkElement,
	 IScrollAnchorProvider
{
	public new Brush Background
	{
		get => (Brush)GetValue(BackgroundProperty);
		set => SetValue(BackgroundProperty, value);
	}

	public UIElement Content
	{
		get => (UIElement)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public ScrollingContentOrientation ContentOrientation
	{
		get => (ScrollingContentOrientation)GetValue(ContentOrientationProperty);
		set => SetValue(ContentOrientationProperty, value);
	}

	public ScrollingChainMode HorizontalScrollChainMode
	{
		get => (ScrollingChainMode)GetValue(HorizontalScrollChainModeProperty);
		set => SetValue(HorizontalScrollChainModeProperty, value);
	}

	public ScrollingChainMode VerticalScrollChainMode
	{
		get => (ScrollingChainMode)GetValue(VerticalScrollChainModeProperty);
		set => SetValue(VerticalScrollChainModeProperty, value);
	}

	public ScrollingRailMode HorizontalScrollRailMode
	{
		get => (ScrollingRailMode)GetValue(HorizontalScrollRailModeProperty);
		set => SetValue(HorizontalScrollRailModeProperty, value);
	}

	public ScrollingRailMode VerticalScrollRailMode
	{
		get => (ScrollingRailMode)GetValue(VerticalScrollRailModeProperty);
		set => SetValue(VerticalScrollRailModeProperty, value);
	}

	public ScrollingScrollMode HorizontalScrollMode
	{
		get => (ScrollingScrollMode)GetValue(HorizontalScrollModeProperty);
		set => SetValue(HorizontalScrollModeProperty, value);
	}

	public ScrollingScrollMode VerticalScrollMode
	{
		get => (ScrollingScrollMode)GetValue(VerticalScrollModeProperty);
		set => SetValue(VerticalScrollModeProperty, value);
	}

	public ScrollingScrollMode ComputedHorizontalScrollMode
	{
		get => (ScrollingScrollMode)GetValue(ComputedHorizontalScrollModeProperty);
		set => SetValue(ComputedHorizontalScrollModeProperty, value);
	}

	public ScrollingScrollMode ComputedVerticalScrollMode
	{
		get => (ScrollingScrollMode)GetValue(ComputedVerticalScrollModeProperty);
		set => SetValue(ComputedVerticalScrollModeProperty, value);
	}

	public ScrollingChainMode ZoomChainMode
	{
		get => (ScrollingChainMode)GetValue(ZoomChainModeProperty);
		set => SetValue(ZoomChainModeProperty, value);
	}

	public ScrollingZoomMode ZoomMode
	{
		get => (ScrollingZoomMode)GetValue(ZoomModeProperty);
		set => SetValue(ZoomModeProperty, value);
	}

	public double MinZoomFactor
	{
		get => (double)GetValue(MinZoomFactorProperty);
		set => SetValue(MinZoomFactorProperty, value);
	}

	public double MaxZoomFactor
	{
		get => (double)GetValue(MaxZoomFactorProperty);
		set => SetValue(MaxZoomFactorProperty, value);
	}



	//     MU_XC_NAMESPACE.ScrollingInteractionState State { get; }

	//     IScrollController HorizontalScrollController { get; set; }

	//     IScrollController VerticalScrollController { get; set; }

	public double HorizontalAnchorRatio
	{
		get => (double)GetValue(HorizontalAnchorRatioProperty);
		set => SetValue(HorizontalAnchorRatioProperty, value);
	}

	public double VerticalAnchorRatio
	{
		get => (double)GetValue(VerticalAnchorRatioProperty);
		set => SetValue(VerticalAnchorRatioProperty, value);
	}

	//     Windows.Foundation.Collections.IVector<ScrollSnapPointBase> HorizontalSnapPoints { get; }
	//     Windows.Foundation.Collections.IVector<ScrollSnapPointBase> VerticalSnapPoints { get; }
	//     Windows.Foundation.Collections.IVector<ZoomSnapPointBase> ZoomSnapPoints { get; }

	public event TypedEventHandler<ScrollPresenter, object> ExtentChanged;
	public event TypedEventHandler<ScrollPresenter, object> StateChanged;
	public event TypedEventHandler<ScrollPresenter, object> ViewChanged;
	public event TypedEventHandler<ScrollPresenter, ScrollingScrollAnimationStartingEventArgs> ScrollAnimationStarting;
	public event TypedEventHandler<ScrollPresenter, ScrollingZoomAnimationStartingEventArgs> ZoomAnimationStarting;
	public event TypedEventHandler<ScrollPresenter, ScrollingScrollCompletedEventArgs> ScrollCompleted;
	public event TypedEventHandler<ScrollPresenter, ScrollingZoomCompletedEventArgs> ZoomCompleted;
	public event TypedEventHandler<ScrollPresenter, ScrollingBringingIntoViewEventArgs> BringingIntoView;
	public event TypedEventHandler<ScrollPresenter, ScrollingAnchorRequestedEventArgs> AnchorRequested;

	public new static DependencyProperty BackgroundProperty { get; } = DependencyProperty.Register(
		nameof(Background),
		typeof(Brush),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnBackgroundPropertyChanged(s, e)));

	public static DependencyProperty ContentProperty { get; } = DependencyProperty.Register(
		nameof(Content),
		typeof(UIElement),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnContentPropertyChanged(s, e)));

	public static DependencyProperty ContentOrientationProperty { get; } = DependencyProperty.Register(
		nameof(ContentOrientation),
		typeof(ScrollingContentOrientation),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultContentOrientation, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnContentOrientationPropertyChanged(s, e)));

	public static DependencyProperty HorizontalScrollChainModeProperty { get; } = DependencyProperty.Register(
		nameof(HorizontalScrollChainMode),
		typeof(ScrollingChainMode),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultHorizontalScrollChainMode, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnHorizontalScrollChainModePropertyChanged(s, e)));

	public static DependencyProperty VerticalScrollChainModeProperty { get; } = DependencyProperty.Register(
		nameof(VerticalScrollChainMode),
		typeof(ScrollingChainMode),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultVerticalScrollChainMode, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnVerticalScrollChainModePropertyChanged(s, e)));

	public static DependencyProperty HorizontalScrollRailModeProperty { get; } = DependencyProperty.Register(
		nameof(HorizontalScrollRailMode),
		typeof(ScrollingRailMode),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultHorizontalScrollRailMode, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnHorizontalScrollRailModePropertyChanged(s, e)));

	public static DependencyProperty VerticalScrollRailModeProperty { get; } = DependencyProperty.Register(
		nameof(VerticalScrollRailMode),
		typeof(ScrollingRailMode),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultVerticalScrollRailMode, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnVerticalScrollRailModePropertyChanged(s, e)));

	public static DependencyProperty HorizontalScrollModeProperty { get; } = DependencyProperty.Register(
		nameof(HorizontalScrollMode),
		typeof(ScrollingScrollMode),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultHorizontalScrollMode, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnHorizontalScrollModePropertyChanged(s, e)));

	public static DependencyProperty VerticalScrollModeProperty { get; } = DependencyProperty.Register(
		nameof(VerticalScrollMode),
		typeof(ScrollingScrollMode),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultVerticalScrollMode, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnVerticalScrollModePropertyChanged(s, e)));

	public static DependencyProperty ComputedHorizontalScrollModeProperty { get; } = DependencyProperty.Register(
		nameof(ComputedHorizontalScrollMode),
		typeof(ScrollingScrollMode),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultComputedHorizontalScrollMode, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnComputedHorizontalScrollModePropertyChanged(s, e)));

	public static DependencyProperty ComputedVerticalScrollModeProperty { get; } = DependencyProperty.Register(
		nameof(ComputedVerticalScrollMode),
		typeof(ScrollingScrollMode),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultComputedVerticalScrollMode, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnComputedVerticalScrollModePropertyChanged(s, e)));

	public static DependencyProperty ZoomChainModeProperty { get; } = DependencyProperty.Register(
		nameof(ZoomChainMode),
		typeof(ScrollingChainMode),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultZoomChainMode, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnZoomChainModePropertyChanged(s, e)));

	public static DependencyProperty ZoomModeProperty { get; } = DependencyProperty.Register(
		nameof(ZoomMode),
		typeof(ScrollingZoomMode),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultZoomMode, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnZoomModePropertyChanged(s, e)));

	public static DependencyProperty IgnoredInputKindsProperty { get; } = DependencyProperty.Register(
		nameof(IgnoredInputKinds),
		typeof(ScrollingInputKinds),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultIgnoredInputKinds, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnIgnoredInputKindsPropertyChanged(s, e)));

	public static DependencyProperty MinZoomFactorProperty { get; } = DependencyProperty.Register(
		nameof(MinZoomFactor),
		typeof(double),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultMinZoomFactor, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnMinZoomFactorPropertyChanged(s, e)));

	public static DependencyProperty MaxZoomFactorProperty { get; } = DependencyProperty.Register(
		nameof(MaxZoomFactor),
		typeof(double),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultMaxZoomFactor, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnMaxZoomFactorPropertyChanged(s, e)));

	public static DependencyProperty HorizontalAnchorRatioProperty { get; } = DependencyProperty.Register(
		nameof(HorizontalAnchorRatio),
		typeof(double),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultAnchorRatio, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnHorizontalAnchorRatioPropertyChanged(s, e)));

	public static DependencyProperty VerticalAnchorRatioProperty { get; } = DependencyProperty.Register(
		nameof(VerticalAnchorRatio),
		typeof(double),
		typeof(ScrollPresenter),
		new FrameworkPropertyMetadata(defaultValue: s_defaultAnchorRatio, propertyChangedCallback: (s, e) => (s as ScrollPresenter)?.OnVerticalAnchorRatioPropertyChanged(s, e)));

	void OnBackgroundPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnComputedHorizontalScrollModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnComputedVerticalScrollModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnContentPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnContentOrientationPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnHorizontalAnchorRatioPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;

		var value = (double)args.NewValue;
		var coercedValue = value;
		ScrollPresenter.ValidateAnchorRatio(coercedValue);
		//if (std::memcmp(&value, &coercedValue, sizeof(value)) != 0) // use memcmp to avoid tripping over nan
		//{
		//	sender.SetValue(args.Property(), box_value<double>(coercedValue));
		//	return;
		//}

		owner.OnPropertyChanged(args);
	}

	void OnHorizontalScrollChainModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnHorizontalScrollModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnHorizontalScrollRailModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnIgnoredInputKindsPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnMaxZoomFactorPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;

		var value = (double)args.NewValue;
		var coercedValue = value;
		ScrollPresenter.ValidateZoomFactoryBoundary(coercedValue);
		//if (std::memcmp(&value, &coercedValue, sizeof(value)) != 0) // use memcmp to avoid tripping over nan
		//{
		//	sender.SetValue(args.Property(), box_value<double>(coercedValue));
		//	return;
		//}

		owner.OnPropertyChanged(args);
	}

	void OnMinZoomFactorPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;

		var value = (double)args.NewValue;
		var coercedValue = value;
		ScrollPresenter.ValidateZoomFactoryBoundary(coercedValue);
		//if (std::memcmp(&value, &coercedValue, sizeof(value)) != 0) // use memcmp to avoid tripping over nan
		//{
		//	sender.SetValue(args.Property(), box_value<double>(coercedValue));
		//	return;
		//}

		owner.OnPropertyChanged(args);
	}

	void OnVerticalAnchorRatioPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;

		var value = (double)args.NewValue;
		var coercedValue = value;
		ScrollPresenter.ValidateAnchorRatio(coercedValue);
		//if (std::memcmp(&value, &coercedValue, sizeof(value)) != 0) // use memcmp to avoid tripping over nan
		//{
		//	sender.SetValue(args.Property(), box_value<double>(coercedValue));
		//	return;
		//}

		owner.OnPropertyChanged(args);
	}

	void OnVerticalScrollChainModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnVerticalScrollModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnVerticalScrollRailModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnZoomChainModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

	void OnZoomModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ScrollPresenter)sender;
		owner.OnPropertyChanged(args);
	}

}
