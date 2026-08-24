#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using DirectUI;

namespace Microsoft.UI.Xaml.Controls;

public partial class ScrollViewer
{
	private ObservableCollection<float>? _zoomSnapPoints;

	public static bool GetCanContentRenderOutsideBounds(DependencyObject element) =>
		(bool)element.GetValue(CanContentRenderOutsideBoundsProperty);

	public static void SetCanContentRenderOutsideBounds(DependencyObject element, bool canContentRenderOutsideBounds) =>
		element.SetValue(CanContentRenderOutsideBoundsProperty, canContentRenderOutsideBounds);

	public bool CanContentRenderOutsideBounds
	{
		get => (bool)GetValue(CanContentRenderOutsideBoundsProperty);
		set => SetValue(CanContentRenderOutsideBoundsProperty, value);
	}

	public static DependencyProperty CanContentRenderOutsideBoundsProperty
	{
		[DynamicDependency(nameof(GetCanContentRenderOutsideBounds))]
		[DynamicDependency(nameof(SetCanContentRenderOutsideBounds))]
		get;
	} = DependencyProperty.RegisterAttached(
		nameof(CanContentRenderOutsideBounds),
		typeof(bool),
		typeof(ScrollViewer),
		new FrameworkPropertyMetadata(
			false,
			(o, e) => (o as ScrollViewer)?.OnCanContentRenderOutsideBoundsChanged(e.NewValue)));

	public static bool GetIsDeferredScrollingEnabled(DependencyObject element) =>
		(bool)element.GetValue(IsDeferredScrollingEnabledProperty);

	public static void SetIsDeferredScrollingEnabled(DependencyObject element, bool isDeferredScrollingEnabled) =>
		element.SetValue(IsDeferredScrollingEnabledProperty, isDeferredScrollingEnabled);

	public bool IsDeferredScrollingEnabled
	{
		get => (bool)GetValue(IsDeferredScrollingEnabledProperty);
		set => SetValue(IsDeferredScrollingEnabledProperty, value);
	}

	public static DependencyProperty IsDeferredScrollingEnabledProperty
	{
		[DynamicDependency(nameof(GetIsDeferredScrollingEnabled))]
		[DynamicDependency(nameof(SetIsDeferredScrollingEnabled))]
		get;
	} = DependencyProperty.RegisterAttached(
		nameof(IsDeferredScrollingEnabled),
		typeof(bool),
		typeof(ScrollViewer),
		new FrameworkPropertyMetadata(false));

	public static bool GetIsZoomChainingEnabled(DependencyObject element) =>
		(bool)element.GetValue(IsZoomChainingEnabledProperty);

	public static void SetIsZoomChainingEnabled(DependencyObject element, bool isZoomChainingEnabled) =>
		element.SetValue(IsZoomChainingEnabledProperty, isZoomChainingEnabled);

	public bool IsZoomChainingEnabled
	{
		get => (bool)GetValue(IsZoomChainingEnabledProperty);
		set => SetValue(IsZoomChainingEnabledProperty, value);
	}

	public static DependencyProperty IsZoomChainingEnabledProperty
	{
		[DynamicDependency(nameof(GetIsZoomChainingEnabled))]
		[DynamicDependency(nameof(SetIsZoomChainingEnabled))]
		get;
	} = DependencyProperty.RegisterAttached(
		nameof(IsZoomChainingEnabled),
		typeof(bool),
		typeof(ScrollViewer),
		new FrameworkPropertyMetadata(true));

	public static bool GetIsZoomInertiaEnabled(DependencyObject element) =>
		(bool)element.GetValue(IsZoomInertiaEnabledProperty);

	public static void SetIsZoomInertiaEnabled(DependencyObject element, bool isZoomInertiaEnabled) =>
		element.SetValue(IsZoomInertiaEnabledProperty, isZoomInertiaEnabled);

	public bool IsZoomInertiaEnabled
	{
		get => (bool)GetValue(IsZoomInertiaEnabledProperty);
		set => SetValue(IsZoomInertiaEnabledProperty, value);
	}

	public static DependencyProperty IsZoomInertiaEnabledProperty
	{
		[DynamicDependency(nameof(GetIsZoomInertiaEnabled))]
		[DynamicDependency(nameof(SetIsZoomInertiaEnabled))]
		get;
	} = DependencyProperty.RegisterAttached(
		nameof(IsZoomInertiaEnabled),
		typeof(bool),
		typeof(ScrollViewer),
		new FrameworkPropertyMetadata(true));

	public UIElement? LeftHeader
	{
		get => (UIElement?)GetValue(LeftHeaderProperty);
		set => SetValue(LeftHeaderProperty, value);
	}

	public static DependencyProperty LeftHeaderProperty { get; } =
		DependencyProperty.Register(
			nameof(LeftHeader),
			typeof(UIElement),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(default(UIElement)));

	public bool ReduceViewportForCoreInputViewOcclusions
	{
		get => (bool)GetValue(ReduceViewportForCoreInputViewOcclusionsProperty);
		set => SetValue(ReduceViewportForCoreInputViewOcclusionsProperty, value);
	}

	public static DependencyProperty ReduceViewportForCoreInputViewOcclusionsProperty { get; } =
		DependencyProperty.Register(
			nameof(ReduceViewportForCoreInputViewOcclusions),
			typeof(bool),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(false));

	public UIElement? TopHeader
	{
		get => (UIElement?)GetValue(TopHeaderProperty);
		set => SetValue(TopHeaderProperty, value);
	}

	public static DependencyProperty TopHeaderProperty { get; } =
		DependencyProperty.Register(
			nameof(TopHeader),
			typeof(UIElement),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(default(UIElement)));

	public UIElement? TopLeftHeader
	{
		get => (UIElement?)GetValue(TopLeftHeaderProperty);
		set => SetValue(TopLeftHeaderProperty, value);
	}

	public static DependencyProperty TopLeftHeaderProperty { get; } =
		DependencyProperty.Register(
			nameof(TopLeftHeader),
			typeof(UIElement),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(default(UIElement)));

	public IList<float> ZoomSnapPoints
	{
		get
		{
			if (_zoomSnapPoints is null)
			{
				_zoomSnapPoints = new ObservableCollection<float>();
				_zoomSnapPoints.CollectionChanged += (_, _) => OnSnapPointsChanged(DMMotionTypes.Zoom);
				SetValue(ZoomSnapPointsProperty, _zoomSnapPoints);
			}

			return _zoomSnapPoints;
		}
	}

	public static DependencyProperty ZoomSnapPointsProperty { get; } =
		DependencyProperty.Register(
			nameof(ZoomSnapPoints),
			typeof(IList<float>),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(default(IList<float>)));

	public SnapPointsType ZoomSnapPointsType
	{
		get => (SnapPointsType)GetValue(ZoomSnapPointsTypeProperty);
		set => SetValue(ZoomSnapPointsTypeProperty, value);
	}

	public static DependencyProperty ZoomSnapPointsTypeProperty { get; } =
		DependencyProperty.Register(
			nameof(ZoomSnapPointsType),
			typeof(SnapPointsType),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(
				defaultValue: SnapPointsType.Optional,
				propertyChangedCallback: (o, _) =>
				{
					var scrollViewer = (ScrollViewer)o;
					scrollViewer.OnSnapPointsAffectingPropertyChanged(
						DMMotionTypes.Zoom,
						scrollViewer.IsInDirectManipulation);
				}));

	// Hooks the WinUI port's template-part wiring on top of the cross-platform
	// OnApplyTemplate. See ScrollViewer.partial.mux.cs for the implementation.
	partial void OnApplyTemplatePartial() => OnApplyTemplate_MuxPartial();

	// Updates the zoom factor value. Equivalent of ScrollToHorizontalOffset
	// and ScrollToVerticalOffset for the ZoomFactor dependency property.
	public void ZoomToFactor(float factor) =>
		ZoomToFactorInternal(factor, delayAndFlushViewChanged: true, out _);

	public void InvalidateScrollInfo() => ((IScrollOwner)this).InvalidateScrollInfoImpl();

	// MUX Reference ScrollViewer_Partial.cpp:OnPropertyChanged2 ZoomMode case.
	// When the ZoomMode property changes, both the manipulability and the
	// primary-content extent push to DM need to refresh.
	partial void OnZoomModeChangedPartial(ZoomMode zoomMode)
	{
		OnManipulatabilityAffectingPropertyChanged(
			pIsInLiveTree: null,
			isCachedPropertyChanged: true,
			isContentChanged: false,
			isAffectingConfigurations: true,
			isAffectingTouchConfiguration: false);

		// When the zoom factor changes from static to manipulatable or vice-versa,
		// a new content size may have to be pushed to DirectManipulation.
		OnPrimaryContentAffectingPropertyChanged(
			boundsChanged: true,
			horizontalAlignmentChanged: false,
			verticalAlignmentChanged: false,
			zoomFactorBoundaryChanged: false);

		if (_presenter is ScrollContentPresenter scp)
		{
			switch (zoomMode)
			{
				case ZoomMode.Disabled:
					scp.OnMinZoomFactorChanged(1f);
					scp.OnMaxZoomFactorChanged(1f);
					break;
				case ZoomMode.Enabled:
					scp.OnMinZoomFactorChanged(MinZoomFactor);
					scp.OnMaxZoomFactorChanged(MaxZoomFactor);
					break;
			}
		}
	}

}
