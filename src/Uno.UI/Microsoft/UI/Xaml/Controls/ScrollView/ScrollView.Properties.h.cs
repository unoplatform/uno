// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls;

public partial class ScrollView : Control
{
	public Visibility ComputedHorizontalScrollBarVisibility
	{
		get { return (Visibility)GetValue(ComputedHorizontalScrollBarVisibilityProperty); }
		set { SetValue(ComputedHorizontalScrollBarVisibilityProperty, value); }
	}

	public static DependencyProperty ComputedHorizontalScrollBarVisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(ComputedHorizontalScrollBarVisibility),
			typeof(Visibility),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(Visibility.Collapsed, propertyChangedCallback: OnComputedHorizontalScrollBarVisibilityPropertyChanged));

	public ScrollingScrollMode ComputedHorizontalScrollMode
	{
		get => (ScrollingScrollMode)GetValue(ComputedHorizontalScrollModeProperty);
		set => SetValue(ComputedHorizontalScrollModeProperty, value);
	}

	public static DependencyProperty ComputedHorizontalScrollModeProperty { get; } =
		DependencyProperty.Register(
			nameof(ComputedHorizontalScrollMode),
			typeof(ScrollingScrollMode),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingScrollMode.Disabled, propertyChangedCallback: OnComputedHorizontalScrollModePropertyChanged));

	public Visibility ComputedVerticalScrollBarVisibility
	{
		get => (Visibility)GetValue(ComputedVerticalScrollBarVisibilityProperty);
		set => SetValue(ComputedVerticalScrollBarVisibilityProperty, value);
	}

	public static DependencyProperty ComputedVerticalScrollBarVisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(ComputedVerticalScrollBarVisibility),
			typeof(Visibility),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(Visibility.Collapsed, propertyChangedCallback: OnComputedVerticalScrollBarVisibilityPropertyChanged));


	public ScrollingScrollMode ComputedVerticalScrollMode
	{
		get => (ScrollingScrollMode)GetValue(ComputedVerticalScrollModeProperty);
		set => SetValue(ComputedVerticalScrollModeProperty, value);
	}

	public static DependencyProperty ComputedVerticalScrollModeProperty { get; } =
		DependencyProperty.Register(
			nameof(ComputedVerticalScrollMode),
			typeof(ScrollingScrollMode),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingScrollMode.Disabled, propertyChangedCallback: OnComputedVerticalScrollModePropertyChanged));

	public UIElement Content
	{
		get => (UIElement)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(
			nameof(Content),
			typeof(UIElement),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(default(UIElement), propertyChangedCallback: OnContentPropertyChanged));

	public ScrollingContentOrientation ContentOrientation
	{
		get => (ScrollingContentOrientation)GetValue(ContentOrientationProperty);
		set => SetValue(ContentOrientationProperty, value);
	}

	public static DependencyProperty ContentOrientationProperty { get; } =
		DependencyProperty.Register(
			nameof(ContentOrientation),
			typeof(ScrollingContentOrientation),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingContentOrientation.Vertical, propertyChangedCallback: OnContentOrientationPropertyChanged));

	public double HorizontalAnchorRatio
	{
		get => (double)GetValue(HorizontalAnchorRatioProperty);
		set => SetValue(HorizontalAnchorRatioProperty, value);
	}

	public static DependencyProperty HorizontalAnchorRatioProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalAnchorRatio),
			typeof(double),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(0.0d, propertyChangedCallback: OnHorizontalAnchorRatioPropertyChanged));

	public ScrollingScrollBarVisibility HorizontalScrollBarVisibility
	{
		get => (ScrollingScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
		set => SetValue(HorizontalScrollBarVisibilityProperty, value);
	}

	public static DependencyProperty HorizontalScrollBarVisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalScrollBarVisibility),
			typeof(ScrollingScrollBarVisibility),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingScrollBarVisibility.Auto, propertyChangedCallback: OnHorizontalScrollBarVisibilityPropertyChanged));

	public ScrollingChainMode HorizontalScrollChainMode
	{
		get => (ScrollingChainMode)GetValue(HorizontalScrollChainModeProperty);
		set => SetValue(HorizontalScrollChainModeProperty, value);
	}

	public static DependencyProperty HorizontalScrollChainModeProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalScrollChainMode),
			typeof(ScrollingChainMode),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingChainMode.Auto, propertyChangedCallback: OnHorizontalScrollChainModePropertyChanged));

	public ScrollingScrollMode HorizontalScrollMode
	{
		get => (ScrollingScrollMode)GetValue(HorizontalScrollModeProperty);
		set => SetValue(HorizontalScrollModeProperty, value);
	}

	public static DependencyProperty HorizontalScrollModeProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalScrollMode),
			typeof(ScrollingScrollMode),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingScrollMode.Auto, propertyChangedCallback: OnHorizontalScrollModePropertyChanged));

	public ScrollingRailMode HorizontalScrollRailMode
	{
		get => (ScrollingRailMode)GetValue(HorizontalScrollRailModeProperty);
		set => SetValue(HorizontalScrollRailModeProperty, value);
	}

	public static DependencyProperty HorizontalScrollRailModeProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalScrollRailMode),
			typeof(ScrollingRailMode),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingRailMode.Enabled, propertyChangedCallback: OnHorizontalScrollRailModePropertyChanged));

	public static DependencyProperty IgnoredInputKindsProperty { get; } =
		DependencyProperty.Register(
			nameof(IgnoredInputKinds),
			typeof(ScrollingInputKinds),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingInputKinds.None, propertyChangedCallback: OnIgnoredInputKindsPropertyChanged));

	public double MaxZoomFactor
	{
		get => (double)GetValue(MaxZoomFactorProperty);
		set => SetValue(MaxZoomFactorProperty, value);
	}

	public static DependencyProperty MaxZoomFactorProperty { get; } =
		DependencyProperty.Register(
			nameof(MaxZoomFactor),
			typeof(double),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(10.0d, propertyChangedCallback: OnMaxZoomFactorPropertyChanged));

	public double MinZoomFactor
	{
		get => (double)GetValue(MinZoomFactorProperty);
		set => SetValue(MinZoomFactorProperty, value);
	}

	public static DependencyProperty MinZoomFactorProperty { get; } =
		DependencyProperty.Register(
			nameof(MinZoomFactor),
			typeof(double),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(0.1d, propertyChangedCallback: OnMinZoomFactorPropertyChanged));

	// In WinUI code, there is ScrollViewProperties::ScrollPresenter()
	// which gets the value from the DP.
	// And also, there is ScrollView::ScrollPresenter() which only reads a field.
	// Since we only have a single ScrollView, there is only one ScrollPresenter property that we can have.
	// So, we make the DP one internal and rename suffix it with "Internal"
	// And leave the other one public.
	// From testing in WinUI, the property that's publicly exposed is the one that reads the field.
	// So, we should be good.
	// To confirm the behavior, use the following code in a WinUI app:
	//
	/*
		var x = new ScrollView();
		var old1 = x.ScrollPresenter; // This will be null.
		var old2 = x.GetValue(ScrollView.ScrollPresenterProperty); // This will be null.

		x.SetValue(ScrollView.ScrollPresenterProperty, new ScrollPresenter());

		var new1 = x.ScrollPresenter; // This will be null
		var new2 = x.GetValue(ScrollView.ScrollPresenterProperty); // This will be non-null
	 */
	//
	// The above explains that the publicly exposed ScrollPresenter property reads from the field rather than the DP
	internal ScrollPresenter ScrollPresenterInternal
	{
		get => (ScrollPresenter)GetValue(ScrollPresenterProperty);
		set => SetValue(ScrollPresenterProperty, value);
	}

	public static DependencyProperty ScrollPresenterProperty { get; } =
		DependencyProperty.Register(
			nameof(ScrollPresenter),
			typeof(ScrollPresenter),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(default(ScrollPresenter), propertyChangedCallback: OnScrollPresenterPropertyChanged));

	public double VerticalAnchorRatio
	{
		get => (double)GetValue(VerticalAnchorRatioProperty);
		set => SetValue(VerticalAnchorRatioProperty, value);
	}

	public static DependencyProperty VerticalAnchorRatioProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalAnchorRatio),
			typeof(double),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(0.0d, propertyChangedCallback: OnVerticalAnchorRatioPropertyChanged));

	public ScrollingScrollBarVisibility VerticalScrollBarVisibility
	{
		get => (ScrollingScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
		set => SetValue(VerticalScrollBarVisibilityProperty, value);
	}

	public static DependencyProperty VerticalScrollBarVisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalScrollBarVisibility),
			typeof(ScrollingScrollBarVisibility),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingScrollBarVisibility.Auto, propertyChangedCallback: OnVerticalScrollBarVisibilityPropertyChanged));

	public ScrollingChainMode VerticalScrollChainMode
	{
		get => (ScrollingChainMode)GetValue(VerticalScrollChainModeProperty);
		set => SetValue(VerticalScrollChainModeProperty, value);
	}

	public static DependencyProperty VerticalScrollChainModeProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalScrollChainMode),
			typeof(ScrollingChainMode),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingChainMode.Auto, propertyChangedCallback: OnVerticalScrollChainModePropertyChanged));

	public ScrollingScrollMode VerticalScrollMode
	{
		get => (ScrollingScrollMode)GetValue(VerticalScrollModeProperty);
		set => SetValue(VerticalScrollModeProperty, value);
	}

	public static DependencyProperty VerticalScrollModeProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalScrollMode),
			typeof(ScrollingScrollMode),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingScrollMode.Auto, propertyChangedCallback: OnVerticalScrollModePropertyChanged));

	public ScrollingRailMode VerticalScrollRailMode
	{
		get => (ScrollingRailMode)GetValue(VerticalScrollRailModeProperty);
		set => SetValue(VerticalScrollRailModeProperty, value);
	}

	public static DependencyProperty VerticalScrollRailModeProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalScrollRailMode),
			typeof(ScrollingRailMode),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingRailMode.Enabled, propertyChangedCallback: OnVerticalScrollRailModePropertyChanged));

	public ScrollingChainMode ZoomChainMode
	{
		get => (ScrollingChainMode)GetValue(ZoomChainModeProperty);
		set => SetValue(ZoomChainModeProperty, value);
	}

	public static DependencyProperty ZoomChainModeProperty { get; } =
		DependencyProperty.Register(
			nameof(ZoomChainMode),
			typeof(ScrollingChainMode),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingChainMode.Auto, propertyChangedCallback: OnZoomChainModePropertyChanged));

	public ScrollingZoomMode ZoomMode
	{
		get => (ScrollingZoomMode)GetValue(ZoomModeProperty);
		set => SetValue(ZoomModeProperty, value);
	}

	public static DependencyProperty ZoomModeProperty { get; } =
		DependencyProperty.Register(
			nameof(ZoomMode),
			typeof(ScrollingZoomMode),
			typeof(ScrollView),
			new FrameworkPropertyMetadata(ScrollingZoomMode.Disabled, propertyChangedCallback: OnZoomModePropertyChanged));
}
