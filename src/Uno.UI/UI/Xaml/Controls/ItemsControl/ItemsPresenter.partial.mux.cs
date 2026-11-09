// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsPresenter_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsPresenter
{
	public ItemsPresenter()
	{
		Initialize();
	}

	// ~ItemsPresenter: nothing to release in managed code.

	// Supports the IManipulationDataProvider interface: see the interface list of the class.

	private void Initialize()
	{
		// The header and footer are created by CreateHeaderAndFooter.

		// Subscribes to load/unload events.
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		// KnownPropertyIndex::ItemsPresenter_Padding in native and has PROP_AFFECT_MEASURE flag
		if (args.Property == HeaderProperty)
		{
			if (HeaderContentControl is { } header)
			{
				header.Content = args.NewValue;
			}
			InvalidateMeasure();
		}
		else if (args.Property == HeaderTemplateProperty)
		{
			if (HeaderContentControl is { } header)
			{
				header.ContentTemplate = (DataTemplate?)args.NewValue;
			}
			InvalidateMeasure();
		}
		else if (args.Property == HeaderTransitionsProperty)
		{
			if (HeaderContentControl is { } header)
			{
				header.ContentTransitions = (TransitionCollection)args.NewValue;
			}
			InvalidateMeasure();
		}
		// The footer is loaded only when it enters the viewport.
		else if (args.Property == FooterProperty)
		{
			if (m_bCanLoadFooter)
			{
				if (FooterContentControl is { } footer)
				{
					footer.Content = args.NewValue;
				}
				InvalidateMeasure();
			}
		}
		else if (args.Property == FooterTemplateProperty)
		{
			if (m_bCanLoadFooter)
			{
				if (FooterContentControl is { } footer)
				{
					footer.ContentTemplate = (DataTemplate?)args.NewValue;
				}
				InvalidateMeasure();
			}
		}
		else if (args.Property == FooterTransitionsProperty)
		{
			if (FooterContentControl is { } footer)
			{
				footer.ContentTransitions = (TransitionCollection)args.NewValue;
			}
			InvalidateMeasure();
		}
		// ItemsPresenter_ItemsPanel: the panel is set through SetItemsPanel, which notifies NotifySnapPointsInfoPanelChanged.
	}

	// Subscribes to ScrollViewer events in order to load footer when it enters the viewport.
	private void OnLoaded(object sender, RoutedEventArgs args)
	{
		ScrollViewer? spScrollViewer = null;

		// In some circumstances, Unloaded doesn't get raised. We need to make sure we don't subscribe twice
		// to the same event.
		if (m_wrScrollViewer is not null)
		{
			if (m_wrScrollViewer.TryGetTarget(out var spOldScrollViewer))
			{
				spOldScrollViewer.ViewChanged -= OnScrollViewChanged;
			}

			m_wrScrollViewer = null;
		}

		var spParentAsDO = VisualTreeHelper.GetParent(this);

		if (spParentAsDO is ScrollContentPresenter spParentAsSCP)
		{
			spScrollViewer = spParentAsSCP.ScrollOwner as ScrollViewer;

			if (spScrollViewer is not null)
			{
				m_wrScrollViewer = new WeakReference<ScrollViewer>(spScrollViewer);
				spScrollViewer.ViewChanged += OnScrollViewChanged;

				DelayLoadFooter();
			}
		}

		// If we can't find a ScrollViewer, we won't delay load footer.
		if (spScrollViewer is null)
		{
			LoadFooter(false /* updateLayout */);
		}
	}

	// Unsubscribes from ScrollViewer events.
	private void OnUnloaded(object sender, RoutedEventArgs args)
	{
		if (m_wrScrollViewer is not null && m_wrScrollViewer.TryGetTarget(out var spScrollViewer))
		{
			spScrollViewer.ViewChanged -= OnScrollViewChanged;
		}

		m_wrScrollViewer = null;
	}

	// Called when the ScrollViewer's viewport changes.
	private void OnScrollViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
	{
		DelayLoadFooter();
	}

	// Loads the footer if it's in the viewport.
	// The current viewport is retrieved from the ScrollViewer unless a future viewport is specified.
	// Future viewports are used to pre-load the footer before a scroll operation.
	internal void DelayLoadFooter()
	{
		DelayLoadFooter(null, false /* updateLayout */);
	}

	internal void DelayLoadFooter(Rect? pFutureViewportRect, bool updateLayout)
	{
		// TODO Uno: Loaded comes before the first arrange with a panel (WinUI has one by then), and a footer rect of 0 would
		// always look close enough. The arrange calls back here.
		if (!m_bCanLoadFooter && m_hasArrangedPanel)
		{
			ScrollViewer? spScrollViewer = null;
			m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

			if (spScrollViewer is not null)
			{
				bool isHorizontal = false;
				double viewportOffset = 0;
				double viewportSize = 0;
				double footerOffset = 0;
				double delayLoadOffset = FooterDelayLoadOffset;
				float zoomFactor = 0.0f;

				isHorizontal = IsHorizontal();

				// Retrieves offset values depending of the panel orientation.
				if (isHorizontal)
				{
					if (pFutureViewportRect is { } futureViewportRect)
					{
						viewportOffset = futureViewportRect.X;
						viewportSize = futureViewportRect.Width;
					}
					else
					{
						viewportOffset = spScrollViewer.HorizontalOffset;
						viewportSize = spScrollViewer.ViewportWidth;
					}

					footerOffset = m_lastFooterArrangeRect.X;
				}
				else
				{
					if (pFutureViewportRect is { } futureViewportRect)
					{
						viewportOffset = futureViewportRect.Y;
						viewportSize = futureViewportRect.Height;
					}
					else
					{
						viewportOffset = spScrollViewer.VerticalOffset;
						viewportSize = spScrollViewer.ViewportHeight;
					}

					footerOffset = m_lastFooterArrangeRect.Y;
				}

				// Adjusts the viewport to account for the ScrollViewer zoom.
				zoomFactor = spScrollViewer.ZoomFactor;
				viewportOffset /= zoomFactor;
				viewportSize /= zoomFactor;
				delayLoadOffset /= zoomFactor;

				// Check that the footer is in the viewport.
				if ((viewportOffset + viewportSize + delayLoadOffset) > footerOffset)
				{
					LoadFooter(updateLayout);
				}
			}
		}
	}

	// Force footer to load if loading is being delayed.
	internal void LoadFooter(bool updateLayout)
	{
		if (!m_bCanLoadFooter)
		{
			ScrollViewer? spScrollViewer = null;
			m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

			var spFooter = Footer;
			var spFooterTemplate = FooterTemplate;

			if (FooterContentControl is { } footerContentControl)
			{
				footerContentControl.Content = spFooter;
				footerContentControl.ContentTemplate = spFooterTemplate;
			}

			m_bCanLoadFooter = true;

			// This is called when we use ScrollIntoView to scroll to footer.
			if (updateLayout)
			{
				FooterContentControl?.UpdateLayout();
			}

			// We don't need this event anymore.
			if (spScrollViewer is not null)
			{
				spScrollViewer.ViewChanged -= OnScrollViewChanged;
			}

			Loaded -= OnLoaded;
			Unloaded -= OnUnloaded;
		}
	}

	internal override bool WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(Orientation orientation)
	{
		// get_Panel failing means the default behavior.
		if (Panel is not { } spPanel)
		{
			return true;
		}

		return WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(orientation, spPanel);
	}

	private bool WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(Orientation orientation, UIElement spPanel)
	{
		return (spPanel as FrameworkElement)?.WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(orientation) ?? true;
	}

	// Measures header, footer and the inner panel
	protected override Size MeasureOverride(Size availableSize)
	{
		Size correctedAvailableSize = default;
		Size desiredSize = default;
		Size headerDesiredSize = default;
		Size footerDesiredSize = default;
		bool isHorizontal = false;
		Thickness padding = default;

		// Ensure panel and header is initialized and hooked up.
		var spPanel = Panel;

		var hasScrollInfo = IsScrollInfoPanel(spPanel);

		var spHeaderContent = Header;
		var spHeaderTemplate = HeaderTemplate;

		var spFooterContent = Footer;
		var spFooterTemplate = FooterTemplate;

		if (spPanel is not null)
		{
			if (HeaderContentControl is { } header)
			{
				if (spHeaderContent is null && spHeaderTemplate is null)
				{
					header.Visibility = Visibility.Collapsed;
				}
				else
				{
					header.Visibility = Visibility.Visible;
				}
			}

			// Footers are not supported for scroll info panels (i.e. VirtualizingStackPanel), so we
			// hide them.
			if (FooterContentControl is { } footer)
			{
				if ((spFooterContent is null && spFooterTemplate is null) || hasScrollInfo)
				{
					footer.Visibility = Visibility.Collapsed;
				}
				else
				{
					footer.Visibility = Visibility.Visible;
				}
			}

			// The header and footer are inserted around the panel by SetItemsPanel.
		}
		else
		{
			return desiredSize;
		}

		padding = PaddingInternal;
		isHorizontal = IsHorizontal();

		// Correct available size for non-virtualizing direction padding.
		DecreaseSizeForPaddingInNonVirtualizingDimension(ref availableSize);

		correctedAvailableSize = availableSize;

		// Measure header with infinite size in virtualizing direction.
		// We need to ensure header is a child of ItemsPresenter
		if (isHorizontal)
		{
			correctedAvailableSize.Width = double.PositiveInfinity;
		}
		else
		{
			correctedAvailableSize.Height = double.PositiveInfinity;
		}

		// Measure header
		HeaderContentControl?.Measure(correctedAvailableSize);
		headerDesiredSize = GetHeaderSize();

		// Measure footer
		FooterContentControl?.Measure(correctedAvailableSize);
		footerDesiredSize = GetFooterSize();

		// Measure panel

		// regarding the check for IModernCollectionBasePanel:
		// these new style virtualizing panels do not think in logical units
		// and want to be treated just like regular panels. Ofcourse they
		// do not want to be limited in the virtualizing direction, so I want to use
		// the correctedAvailableSize for it.

		if (!hasScrollInfo)
		{
			var hasScrollOwner = false;

			if (spPanel is ModernCollectionBasePanel spModernPanel)
			{
				// the panels will not have yet been laid out at this point so have no clue about their offset
				// Without having implemented nested virtualization completely, we have a need to understand more
				// about our situation than measure will provide. If we do not pass in the location, we will have
				// do another layoutpass to actually adjust perfectly. So the below communication increases perf
				// and fixes issues around the first tick not being perfect.
				Size tempSize = availableSize;
				Point panelOrigin = default;

				// Correct final size for non-virtualizing direction padding.
				DecreaseSizeForPaddingInNonVirtualizingDimension(ref tempSize);

				// Calculate arrange location for header and inner panel
				CalculateArrangeRect(tempSize, spPanel, out _, out var panelRect, out _);

				// the above call was correct because we already have a desiredsize for the header.
				var margin = Margin;
				panelOrigin.X = (float)panelRect.X + (float)margin.Left;
				panelOrigin.Y = (float)panelRect.Y + (float)margin.Top;
				spModernPanel.SetOriginFromItemsPresenter(panelOrigin);
			}

#if !HAS_UNO // TODO Uno: ScrollContentPresenter does not make the ItemsPresenter its scroll owner
			hasScrollOwner = m_ScrollData.ScrollOwner is not null;
#endif

			if (hasScrollOwner || spPanel is ModernCollectionBasePanel)
			{
				spPanel.Measure(correctedAvailableSize);
			}
			else
			{
				spPanel.Measure(availableSize);
			}
		}
		else
		{
			if (spPanel is ModernCollectionBasePanel)
			{
				spPanel.Measure(correctedAvailableSize);
			}
			else
			{
				spPanel.Measure(availableSize);
			}
		}

		desiredSize = spPanel.DesiredSize;

		if (isHorizontal)
		{
			// don't accumulate header and footer size for ScrollInfoPanels
			if (!hasScrollInfo)
			{
				desiredSize.Width +=
					headerDesiredSize.Width +
					footerDesiredSize.Width +
					(float)(padding.Left + padding.Right);
			}
			desiredSize.Height = Math.Max(desiredSize.Height, Math.Max(headerDesiredSize.Height, footerDesiredSize.Height));
		}
		else
		{
			// don't accumulate header and footer size for ScrollInfoPanels
			if (!hasScrollInfo)
			{
				desiredSize.Height +=
					headerDesiredSize.Height +
					footerDesiredSize.Height +
					(float)(padding.Top + padding.Bottom);
			}
			desiredSize.Width = Math.Max(desiredSize.Width, Math.Max(headerDesiredSize.Width, footerDesiredSize.Width));
		}

		var returnValue = desiredSize;

		// Correct return size for non-virtualizing direction padding.
		IncreaseSizeForPaddingInNonVirtualizingDimension(ref returnValue);

#if !HAS_UNO // TODO Uno: scroll data is only maintained for IScrollInfo panels
		var availableDimension = isHorizontal ? availableSize.Width : availableSize.Height;
		var roundedAvailableDimension = (float)LayoutRound(availableDimension);

		UpdateScrollData(isHorizontal, roundedAvailableDimension);
#endif

		return returnValue;
	}

	// Arranges header and inner panel
	protected override Size ArrangeOverride(Size finalSize)
	{
		var returnValue = finalSize;

		var spPanel = Panel;
		if (spPanel is null)
		{
			return returnValue;
		}

		// Correct final size for non-virtualizing direction padding.
		DecreaseSizeForPaddingInNonVirtualizingDimension(ref finalSize);

		// Calculate arrange location for header and inner panel
		CalculateArrangeRect(finalSize, spPanel, out var headerRect, out var panelRect, out var footerRect);

		// Arrange the header, panel and footer.
		HeaderContentControl?.Arrange(headerRect);
		spPanel.Arrange(panelRect);
		FooterContentControl?.Arrange(footerRect);

		var isHorizontal = IsHorizontal();

		var notifySnapPointsChanges =
			isHorizontal ?
				headerRect.X != m_lastHeaderArrangeRect.X || headerRect.Width != m_lastHeaderArrangeRect.Width ||
				footerRect.X != m_lastFooterArrangeRect.X || footerRect.Width != m_lastFooterArrangeRect.Width ||
				panelRect.X != m_lastPanelArrangeRect.X || panelRect.Width != m_lastPanelArrangeRect.Width :
			/* isVertical */
				headerRect.Y != m_lastHeaderArrangeRect.Y || headerRect.Height != m_lastHeaderArrangeRect.Height ||
				footerRect.Y != m_lastFooterArrangeRect.Y || footerRect.Height != m_lastFooterArrangeRect.Height ||
				panelRect.Y != m_lastPanelArrangeRect.Y || panelRect.Height != m_lastPanelArrangeRect.Height;

		m_lastHeaderArrangeRect = headerRect;
		m_lastFooterArrangeRect = footerRect;
		m_lastPanelArrangeRect = panelRect;
		m_hasArrangedPanel = true;
		DelayLoadFooter();

		returnValue = finalSize;

		// Correct final size for non-virtualizing direction padding.
		IncreaseSizeForPaddingInNonVirtualizingDimension(ref returnValue);

		if (notifySnapPointsChanges)
		{
			// Snap point might have changed, which might require to raise an event.
			NotifySnapPointsChanges();
		}

		return returnValue;
	}

#if !HAS_UNO // TODO Uno: the panel is set through SetItemsPanel, not by applying a template
	// This method enforces which Panel can be inside which control
	protected override void OnApplyTemplate()
	{
		var spTemplatedParent = GetTemplatedParent();
		if (spTemplatedParent is null)
		{
			return;
		}

		var indexItemsControl = spTemplatedParent.GetType();

		var spIPanel = Panel;
		if (spIPanel is null)
		{
			return;
		}

		var indexPanel = spIPanel.GetType();
		var failed = false;

		if (indexPanel == typeof(ItemsStackPanel))
		{
			// enable KnownTypeIndex::ItemsStackPanel for ListBox (P2 scenario)
			if (indexItemsControl == typeof(ListBox))
			{
				// OK
			}
			else
			{
				// We are not supporting ItemsWrapGrid for ListBox right now since WrapGrid isn't supported.
				// This is because we don't have the dev/test resources to invest to ensure this scenario works and support it,
				// but we may revisit this decision at some point.
				// Cut new panels from ComboBox and FlipView
				if (indexItemsControl != typeof(ItemsControl) && indexItemsControl != typeof(ListView) && indexItemsControl != typeof(GridView))
				{
					failed = true;
				}
			}
		}
		else if (indexPanel == typeof(ItemsWrapGrid) || indexPanel == typeof(WrapGrid))
		{
			if (indexItemsControl != typeof(ItemsControl) && indexItemsControl != typeof(ListView) && indexItemsControl != typeof(GridView))
			{
				failed = true;
			}
		}
		else if (indexPanel == typeof(VariableSizedWrapGrid))
		{
			if (indexItemsControl != typeof(ItemsControl) && indexItemsControl != typeof(GridView))
			{
				failed = true;
			}
		}
		else if (indexPanel == typeof(CarouselPanel))
		{
			if (indexItemsControl != typeof(ComboBox))
			{
				failed = true;
			}
		}
		else if (indexPanel == typeof(CalendarPanel))
		{
			if (indexItemsControl != typeof(CalendarView))
			{
				failed = true;
			}
		}

		if (failed)
		{
			throw new InvalidOperationException("ERROR_INCORRECT_PANEL_FOR_CONTROL");
		}

		if (spTemplatedParent is FrameworkElement spItemsControlAsFE)
		{
			var spItemsControlTemplatedParent = spItemsControlAsFE.GetTemplatedParent();
			if (spItemsControlTemplatedParent is GroupItem && spIPanel is VirtualizingPanel)
			{
				// We don't support virtualizing panels inside GroupItems.
				throw new InvalidOperationException("ERROR_INCORRECT_PANEL_FOR_GROUPITEM");
			}
		}
	}
#endif

	// Itemspresenter is the lynchpin in deciding whether we want to be in a non clipping subtree.
	// Basically, when he decided to not want infinity, even though we were in a scrolling configuration,
	// we want the measures underneath it to not constrain the desiredsize to the availablesize. That will allow
	// wrapping uielements (like textblock) to size correctly, and still allow containers that want to be bigger
	// to look good.
	// Itemspresenter will set the value on himself (used during layout) and on the panel. Listview will set it on the
	// items (so that individual measure on them will work).
	internal bool EvaluateAndSetNonClippingBehavior(bool isNotBeingPassedInfinity)
	{
		var result = false;

		if (Panel is ItemsStackPanel spIsp)
		{
			// we only want the behavior in an isp at the moment

			// first set it to ourselves
			IsNonClippingSubtree = isNotBeingPassedInfinity;

			// in the case of a modernpanel, lets have this itemspresenter not clip to the available size
			// in overconstrained scenarios
			// we're just passing through the setting to the panel here in an effort to make itemspresenter behave like a
			// 'dumb' passthrough. The real logic was performed when measuring the scrollcontentpresenter

			// we pass it along - the reason we want these individual elements also to have the correct value is because
			// they could potentially be measured independently
			spIsp.IsNonClippingSubtree = isNotBeingPassedInfinity;

			result = isNotBeingPassedInfinity;
		}

		return result;
	}

	// Calculates arrange location for header, footer and inner panel
	private void CalculateArrangeRect(
		Size availableSize,
		UIElement spPanel,
		out Rect pHeaderArrangeRect,
		out Rect pPanelArrangeRect,
		out Rect pFooterArrangeRect)
	{
		pHeaderArrangeRect = default;
		pPanelArrangeRect = default;
		pFooterArrangeRect = default;

		Size headerDesiredSize = default;
		Size panelDesiredSize = default;
		Size footerDesiredSize = default;

		Size startingSize = default;
		Size sizeOfFirstVisibleChild = default;

		Thickness padding = default;

		double offset = 0.0;
		double innerPanelOffset = 0.0;
		double offsetForTrailingPadding = 0;
		double firstVisibleItemSize = 0;

		bool isHorizontal = false;
		bool isPixelBasedPanel = false;

		// a virtualizing panel thinks in logical units and is expected to be arranged
		// at the size of the itemspresenter (so it can fill it up completely)
		// this is not the case for normal elements (pixel based).
		// Below you will see we subtract the rect.Y and X positions from the final size:
		// Given that we have a padding of 100 and a pixelsbased panel with width of 2000, the
		// measured size of the total will be 2100. That will be the finalsize that we get to arrange with.
		// If we were to arrange the panel with a width of 2100, we would give it more size than it originally
		// desired. Thus we calculate back to 2000 and arrange with that.
		// This flag also lets us _only_ do this to pixel based panels
		isPixelBasedPanel = spPanel is ModernCollectionBasePanel || spPanel is StackPanel;

		// current logic is to only decrease padding size in the non virtualizing dimension
		// however, in pixel based scenarios we need more correction
		// This flag lets us do an extra correction in virtualizing direction
		isHorizontal = IsHorizontal();

#if !HAS_UNO // TODO Uno: the offsets come from the IScrollInfo implementation
		if (isHorizontal)
		{
			offset = HorizontalOffset;
		}
		else
		{
			offset = VerticalOffset;
		}
#endif

		innerPanelOffset = GetInnerPanelOffset(isHorizontal);

		headerDesiredSize = GetHeaderSize();
		panelDesiredSize = spPanel.DesiredSize;
		footerDesiredSize = GetFooterSize();

		// Allow header as much as possible in their non-virtualizing dimension
		// so that HorizontalAlignment can be used properly on them.
		pHeaderArrangeRect.Height = isHorizontal ? Math.Max(headerDesiredSize.Height, availableSize.Height) : headerDesiredSize.Height;
		pHeaderArrangeRect.Width = isHorizontal ? headerDesiredSize.Width : Math.Max(headerDesiredSize.Width, availableSize.Width);

		// Allow footer as much as possible in their non-virtualizing dimension
		// so that HorizontalAlignment can be used properly on them.
		pFooterArrangeRect.Height = isHorizontal ? Math.Max(footerDesiredSize.Height, availableSize.Height) : footerDesiredSize.Height;
		pFooterArrangeRect.Width = isHorizontal ? footerDesiredSize.Width : Math.Max(footerDesiredSize.Width, availableSize.Width);

		padding = PaddingInternal;

		if (isHorizontal)
		{
			pHeaderArrangeRect.X = (float)padding.Left;
			pHeaderArrangeRect.Y = (float)padding.Top;

			pPanelArrangeRect.X = pHeaderArrangeRect.X + headerDesiredSize.Width;
			pPanelArrangeRect.Y = pHeaderArrangeRect.Y;
		}
		else
		{
			pHeaderArrangeRect.Y = (float)padding.Top;
			pHeaderArrangeRect.X = (float)padding.Left;

			pPanelArrangeRect.Y = pHeaderArrangeRect.Y + headerDesiredSize.Height;
			pPanelArrangeRect.X = pHeaderArrangeRect.X;
		}

		// Note: footer location is only relevant for pixel based panels.
		if (isHorizontal)
		{
			pFooterArrangeRect.X = Math.Max(pPanelArrangeRect.X + panelDesiredSize.Width, availableSize.Width - pFooterArrangeRect.Width - (float)padding.Right);
			pFooterArrangeRect.Y = pHeaderArrangeRect.Y;
		}
		else
		{
			pFooterArrangeRect.Y = Math.Max(pPanelArrangeRect.Y + panelDesiredSize.Height, availableSize.Height - pFooterArrangeRect.Height - (float)padding.Bottom);
			pFooterArrangeRect.X = pHeaderArrangeRect.X;
		}

		// Fix header arrange location using starting size determined using inner panel's alignments
		pHeaderArrangeRect.X += startingSize.Width;
		pHeaderArrangeRect.Y += startingSize.Height;

		// Fix header and panel start location to accommodate trailing padding
		// This is when 3 > offset - inneroffset > 2
		if (DoubleUtil.GreaterThan(offset - innerPanelOffset, 2))
		{
			sizeOfFirstVisibleChild = GetSizeOfContainer();
			firstVisibleItemSize = isHorizontal ? sizeOfFirstVisibleChild.Width : sizeOfFirstVisibleChild.Height;
			offsetForTrailingPadding = -1 * DoubleUtil.Min(1, offset - innerPanelOffset - 2) * firstVisibleItemSize;
		}

		// see comment above on the modern panels.
		pPanelArrangeRect.Height = isPixelBasedPanel && !isHorizontal ? pFooterArrangeRect.Y - pPanelArrangeRect.Y : availableSize.Height;
		pPanelArrangeRect.Width = isPixelBasedPanel && isHorizontal ? pFooterArrangeRect.X - pPanelArrangeRect.X : availableSize.Width;
	}

	// Returns true if the physical orientation is horizontal.
	private bool IsHorizontal()
	{
		var orientation = PhysicalOrientation;
		return orientation == Orientation.Horizontal;
	}

#if !HAS_UNO // TODO Uno: scroll data is only maintained for IScrollInfo panels
	// Update scroll data after Measure.
	// Fixes offset, viewport and extent based on padding and header values.
	private void UpdateScrollData(bool isHorizontal, double availableDimension)
	{
		double extentX = 0;
		double extentY = 0;
		double offsetX = 0;
		double offsetY = 0;
		double innerPanelScrollableX = 0;
		double innerPanelScrollableY = 0;
		double viewportX = 0;
		double viewportY = 0;
		Size extent = default;
		Size viewport = default;
		Size sizeOfFirstVisibleChild = default;
		Size panelsDesiredSize = default;
		ScrollVector offset = default;
		double trailingPaddingSize = 0;
		double leadingPaddingSize = 0;
		double headerAndPaddingSize = 0;
		double outOfViewheaderAndPaddingSize = 0;
		double inViewHeaderAndPaddingSize = 0;
		double trailingPaddingOffset = 0;

		var spIScrollInfo = Panel as IScrollInfo;
		if (spIScrollInfo is null)
		{
			return;
		}

		m_ScrollData.m_MinOffset.X = 0.0;
		m_ScrollData.m_MinOffset.Y = 0.0;

		leadingPaddingSize = GetSentinelItemSize(LeadingPaddingSentinelIndex, isHorizontal);
		headerAndPaddingSize = GetHeaderAndLeadingPaddingPixelSize(isHorizontal);

		if (leadingPaddingSize == 0)
		{
			if (isHorizontal)
			{
				m_ScrollData.m_MinOffset.X++;
			}
			else
			{
				m_ScrollData.m_MinOffset.Y++;
			}
		}

		if (headerAndPaddingSize == 0)
		{
			if (isHorizontal)
			{
				m_ScrollData.m_MinOffset.X++;
			}
			else
			{
				m_ScrollData.m_MinOffset.Y++;
			}
		}

		if (isHorizontal)
		{
			offsetY = spIScrollInfo.VerticalOffset;
			offsetX = Math.Max(m_ScrollData.m_MinOffset.X, m_ScrollData.OffsetX);
		}
		else
		{
			offsetX = spIScrollInfo.HorizontalOffset;
			offsetY = Math.Max(m_ScrollData.m_MinOffset.Y, m_ScrollData.OffsetY);
		}

		panelsDesiredSize = Panel!.DesiredSize;

		sizeOfFirstVisibleChild = GetSizeOfContainer();
		trailingPaddingSize = GetSentinelItemSize(TrailingPaddingSentinelIndex, isHorizontal);

		extentX = spIScrollInfo.ExtentWidth;
		extentY = spIScrollInfo.ExtentHeight;
		viewportX = spIScrollInfo.ViewportWidth;
		viewportY = spIScrollInfo.ViewportHeight;
		innerPanelScrollableX = GetInnerPanelsScrollableDimension(true);
		innerPanelScrollableY = GetInnerPanelsScrollableDimension(false);

		if (isHorizontal)
		{
			extentX += 2; // 1 for padding and 1 for header

			if (DoubleUtil.GreaterThan(m_ScrollData.OffsetX - innerPanelScrollableX, 2))
			{
				// Logical offset to allow trailing padding to be scrolled into view.
				trailingPaddingOffset = sizeOfFirstVisibleChild.Width == 0 ? 0
										: (trailingPaddingSize / sizeOfFirstVisibleChild.Width);
				// Correct offset to fit trailing padding
				offsetX = DoubleUtil.Min(m_ScrollData.OffsetX, innerPanelScrollableX + 2 + trailingPaddingOffset);

				// Fix viewport in virtualizing direction if we have trailing padding in view.
				if (trailingPaddingSize > 0)
				{
					viewportX = extentX - trailingPaddingOffset;
				}
			}

			// panel is not scrollable.
			if (innerPanelScrollableX == 0)
			{
				double availableDimensionForHeaderAndPadding = availableDimension - panelsDesiredSize.Width;
				global::System.Diagnostics.Debug.Assert(availableDimensionForHeaderAndPadding >= 0, "Unexpected available dimension for header and padding.");

				// case1. padding and header are in view too
				if (availableDimensionForHeaderAndPadding - (headerAndPaddingSize + trailingPaddingSize) >= 0)
				{
					viewportX += 2 - m_ScrollData.m_MinOffset.X;
					offsetX = m_ScrollData.m_MinOffset.X;
				}
				// case2. padding and header are not in view.
				// in this case our maxOffset is 2 or less.
				else
				{
					double maxOffset = 0.0;
					double maxPixelOffset = headerAndPaddingSize + trailingPaddingSize - availableDimensionForHeaderAndPadding;

					TranslatePixelDeltaToOffset(/*currentOffset*/0, /*isHorizontal*/true, ref maxPixelOffset, out maxOffset);
					global::System.Diagnostics.Debug.Assert(maxPixelOffset == 0, "Unexpected remaining pixel offset after translation.");

					viewportX = extentX - maxOffset;
					offsetX = Math.Min(offsetX, maxOffset);
					global::System.Diagnostics.Debug.Assert(offsetX >= m_ScrollData.m_MinOffset.X, "Invalid offset after coercion.");
				}
			}
			else
			{
				// Fix viewport in virtualizing direction if we have leading padding or header in view.
				if (DoubleUtil.LessThan(offsetX, 2) && availableDimension > 0)
				{
					outOfViewheaderAndPaddingSize = GetHeaderAndLeadingPaddingPixelSize(isHorizontal, offsetX);
					inViewHeaderAndPaddingSize = headerAndPaddingSize - outOfViewheaderAndPaddingSize;

					viewportX = viewportX * (availableDimension - inViewHeaderAndPaddingSize) / availableDimension;
					viewportX += 2 - offsetX;
				}
			}
		}
		else
		{
			extentY += 2; // 1 for padding and 1 for header

			if (DoubleUtil.GreaterThan(m_ScrollData.OffsetY - innerPanelScrollableY, 2))
			{
				// Logical offset to allow trailing padding to be scrolled into view.
				trailingPaddingOffset = sizeOfFirstVisibleChild.Height == 0 ? 0
										: (trailingPaddingSize / sizeOfFirstVisibleChild.Height);
				// Correct offset to fit trailing padding
				offsetY = DoubleUtil.Min(m_ScrollData.OffsetY, innerPanelScrollableY + 2 + trailingPaddingOffset);

				// Fix viewport in virtualizing direction if we have trailing padding in view.
				if (trailingPaddingSize > 0)
				{
					viewportY = extentY - trailingPaddingOffset;
				}
			}

			// panel is not scrollable.
			if (innerPanelScrollableY == 0)
			{
				double availableDimensionForHeaderAndPadding = availableDimension - panelsDesiredSize.Height;
				global::System.Diagnostics.Debug.Assert(availableDimensionForHeaderAndPadding >= 0, "Unexpected available dimension for header and padding.");

				// case1. padding and header are in view too
				if (availableDimensionForHeaderAndPadding - (headerAndPaddingSize + trailingPaddingSize) >= 0)
				{
					viewportY += 2 - m_ScrollData.m_MinOffset.Y;
					offsetY = m_ScrollData.m_MinOffset.Y;
				}
				// case2. padding and header are not in view.
				// in this case our maxOffset is 2 or less.
				else
				{
					double maxOffset = 0.0;
					double maxPixelOffset = headerAndPaddingSize + trailingPaddingSize - availableDimensionForHeaderAndPadding;

					TranslatePixelDeltaToOffset(/*currentOffset*/0, /*isHorizontal*/false, ref maxPixelOffset, out maxOffset);
					global::System.Diagnostics.Debug.Assert(maxPixelOffset == 0, "Unexpected remaining pixel offset after translation.");

					viewportY = extentY - maxOffset;
					offsetY = Math.Min(offsetY, maxOffset);
					global::System.Diagnostics.Debug.Assert(offsetY >= m_ScrollData.m_MinOffset.Y, "Invalid offset after coercion.");
				}
			}
			else
			{
				// Fix viewport in virtualizing direction if we have leading padding or header in view.
				if (DoubleUtil.LessThan(offsetY, 2) && availableDimension > 0)
				{
					outOfViewheaderAndPaddingSize = GetHeaderAndLeadingPaddingPixelSize(isHorizontal, offsetY);
					inViewHeaderAndPaddingSize = headerAndPaddingSize - outOfViewheaderAndPaddingSize;

					viewportY = viewportY * (availableDimension - inViewHeaderAndPaddingSize) / availableDimension;
					viewportY += 2 - offsetY;
				}
			}
		}

		offset.X = offsetX;
		offset.Y = offsetY;
		extent.Height = (float)extentY;
		extent.Width = (float)extentX;
		viewport.Height = (float)viewportY;
		viewport.Width = (float)viewportX;

		SetAndVerifyScrollingData(viewport, extent, offset);
	}

	private void SetAndVerifyScrollingData(Size viewport, Size extent, ScrollVector offset)
	{
		// Detect changes to the viewport, extent, and offset
		var viewportChanged = !DoubleUtil.AreClose(viewport.Height, m_ScrollData.m_viewport.Height)
			|| !DoubleUtil.AreClose(viewport.Width, m_ScrollData.m_viewport.Width);

		var extentChanged = !DoubleUtil.AreClose(extent.Height, m_ScrollData.m_extent.Height)
			|| !DoubleUtil.AreClose(extent.Width, m_ScrollData.m_extent.Width);

		var offsetChanged = !DoubleUtil.AreClose(offset.X, m_ScrollData.m_ComputedOffset.X)
			|| !DoubleUtil.AreClose(offset.Y, m_ScrollData.m_ComputedOffset.Y);

		// Update data and fire scroll change notifications
		m_ScrollData.Offset = offset;
		if (viewportChanged || extentChanged || offsetChanged)
		{
			m_ScrollData.m_viewport = viewport;
			m_ScrollData.m_extent = extent;
			m_ScrollData.m_ComputedOffset = offset;

			OnScrollChange();
		}
	}

	// Invalidates scroll info on owner SV.
	private void OnScrollChange()
	{
		var spScrollOwner = m_ScrollData.ScrollOwner;
		if (spScrollOwner is not null)
		{
			spScrollOwner.InvalidateScrollInfo();
		}
	}
#endif

	// Ensures inner panel is hooked up: see Panel.

	// Returns header desired size.
	internal Size GetHeaderSize()
	{
		return HeaderContentControl?.DesiredSize ?? default;
	}

	// Returns panel desired size.
	private Size GetPanelSize()
	{
		return Panel?.DesiredSize ?? default;
	}

	// Returns footer desired size.
	private Size GetFooterSize()
	{
		return FooterContentControl?.DesiredSize ?? default;
	}

#if !HAS_UNO // TODO Uno: only read by the IScrollInfo and IManipulationDataProvider paths
	// Returns leading padding + header size in pixels for virtualizing direction.
	private double GetHeaderAndLeadingPaddingPixelSize(bool isHorizontal)
	{
		double size = 0;
		var headerSize = GetHeaderSize();
		var padding = PaddingInternal;

		if (isHorizontal)
		{
			size = headerSize.Width + padding.Left;
		}
		else
		{
			size = headerSize.Height + padding.Top;
		}

		return size;
	}

	// Returns leading padding + header size in pixels for virtualizing direction for given logical offset.
	private double GetHeaderAndLeadingPaddingPixelSize(bool isHorizontal, double logicalOffset)
	{
		double size = 0;

		if (DoubleUtil.GreaterThan(logicalOffset, 2))
		{
			return size;
		}

		if (DoubleUtil.GreaterThan(logicalOffset, 1))
		{
			var headerSize = GetHeaderSize();

			if (isHorizontal)
			{
				size += (logicalOffset - 1) * headerSize.Width;
			}
			else
			{
				size += (logicalOffset - 1) * headerSize.Height;
			}

			logicalOffset = 1;
		}

		var padding = PaddingInternal;
		if (isHorizontal)
		{
			size += logicalOffset * padding.Left;
		}
		else
		{
			size += logicalOffset * padding.Top;
		}

		return size;
	}
#endif

	// Gets size of leading/trailing padding, header/footer or panel.
	// (see the SpecialSentinelIndices enumeration)
	private double GetSentinelItemSize(int index, bool isHorizontal)
	{
		double size = 0;
		var padding = PaddingInternal;

		switch (index)
		{
			case PanelSentinelIndex: // panel
			{
				var panelSize = GetPanelSize();

				if (isHorizontal)
				{
					size = panelSize.Width;
				}
				else
				{
					size = panelSize.Height;
				}
			}
			break;
			case FooterSentinelIndex: // footer
			{
				var footerSize = GetFooterSize();

				if (isHorizontal)
				{
					size = footerSize.Width;
				}
				else
				{
					size = footerSize.Height;
				}

				break;
			}
			case TrailingPaddingSentinelIndex: // Trailing padding
			{
				if (isHorizontal)
				{
					size = padding.Right;
				}
				else
				{
					size = padding.Bottom;
				}

				break;
			}
			case LeadingPaddingSentinelIndex: // Leading padding
			{
				if (isHorizontal)
				{
					size = padding.Left;
				}
				else
				{
					size = padding.Top;
				}

				break;
			}
			case HeaderSentinelIndex: // header
			{
				var headerSize = GetHeaderSize();

				if (isHorizontal)
				{
					size = headerSize.Width;
				}
				else
				{
					size = headerSize.Height;
				}

				break;
			}
		}

		return size;
	}

#if !HAS_UNO // TODO Uno: the zoom factor comes from the IScrollOwner
	private float GetZoomFactor()
	{
		var zoomFactor = 1.0f;
		var spScrollOwner = m_ScrollData.ScrollOwner;

		if (spScrollOwner is not null)
		{
			zoomFactor = spScrollOwner.ZoomFactor;
		}

		return zoomFactor;
	}

	// Calculates starting offset for header's arrange
	// given available size, header size and inner panel's Verical/Horizontal children alignments
	private Size ComputeAlignmentSize(Size availableSize, Size headerSize)
	{
		Size startingSize = default;
		var spPanel = Panel;
		var spPanelAsWG = spPanel as WrapGrid;
		HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left;
		VerticalAlignment verticalAlignment = VerticalAlignment.Top;
		double startingOffset = 0;
		var spPanelAsVSWG = spPanel as VariableSizedWrapGrid;

		if (spPanelAsWG is null && spPanelAsVSWG is null)
		{
			return startingSize;
		}

		var isHorizontal = IsHorizontal();

		if (!isHorizontal)
		{
			if (spPanelAsWG is not null)
			{
				horizontalAlignment = spPanelAsWG.HorizontalChildrenAlignment;
			}
			else
			{
				horizontalAlignment = spPanelAsVSWG!.HorizontalChildrenAlignment;
			}
			startingOffset = ComputeStartingOffset((int)horizontalAlignment,
									availableSize.Width,
									headerSize.Width);
			startingSize.Width = (float)startingOffset;
		}
		else
		{
			if (spPanelAsWG is not null)
			{
				verticalAlignment = spPanelAsWG.VerticalChildrenAlignment;
			}
			else
			{
				verticalAlignment = spPanelAsVSWG!.VerticalChildrenAlignment;
			}
			startingOffset = ComputeStartingOffset((int)verticalAlignment,
									availableSize.Height,
									headerSize.Height);
			startingSize.Height = (float)startingOffset;
		}

		return startingSize;
	}

	// Calculates starting offset for header's arrange
	// given available size, header size and alignment
	private double ComputeStartingOffset(int alignment, double availableSize, double requiredSize)
	{
		double startingOffset = 0;

		if (alignment == (int)VerticalAlignment.Center)
		{
			startingOffset = DoubleUtil.Max((availableSize - requiredSize) / 2.0, 0.0);
		}
		else if (alignment == (int)VerticalAlignment.Bottom) // or HorizontalAlignment_Right
		{
			startingOffset = DoubleUtil.Max(availableSize - requiredSize, 0.0);
		}

		return startingOffset;
	}
#endif

	// Returns scroll offset for inner panel
	private double GetInnerPanelOffset(bool isHorizontal)
	{
		double offset = 0.0;

#if !HAS_UNO // TODO Uno: panels do not implement IScrollInfo
		if (Panel is IScrollInfo spIScrollInfo)
		{
			if (isHorizontal)
			{
				offset = spIScrollInfo.HorizontalOffset;
			}
			else
			{
				offset = spIScrollInfo.VerticalOffset;
			}
		}
#endif

		return offset;
	}

	// Returns maximum offset that can be set on inner panel
	private double GetInnerPanelsScrollableDimension(bool isHorizontal)
	{
		double value = 0.0;

#if !HAS_UNO // TODO Uno: panels do not implement IScrollInfo
		if (Panel is IScrollInfo spIScrollInfo)
		{
			double extent;
			double viewport;

			if (isHorizontal)
			{
				extent = spIScrollInfo.ExtentWidth;
				viewport = spIScrollInfo.ViewportWidth;
			}
			else
			{
				extent = spIScrollInfo.ExtentHeight;
				viewport = spIScrollInfo.ViewportHeight;
			}

			value = DoubleUtil.Max(0.0, extent - viewport);
		}
#endif

		return value;
	}

	private void DecreaseSizeForPaddingInNonVirtualizingDimension(ref Size pSize)
	{
		var isHorizontal = IsHorizontal();
		var padding = PaddingInternal;

		// Correct available size for non-virtualizing direction padding.
		if (isHorizontal)
		{
			pSize.Height -= (float)(padding.Top + padding.Bottom);
		}
		else
		{
			pSize.Width -= (float)(padding.Left + padding.Right);
		}
	}

	private void IncreaseSizeForPaddingInNonVirtualizingDimension(ref Size pSize)
	{
		var isHorizontal = IsHorizontal();
		var padding = PaddingInternal;

		// Correct available size for non-virtualizing direction padding.
		if (isHorizontal)
		{
			pSize.Height += (float)(padding.Top + padding.Bottom);
		}
		else
		{
			pSize.Width += (float)(padding.Left + padding.Right);
		}
	}

#if !HAS_UNO // TODO Uno: needs VirtualizingPanel.ScrollIntoView and the IScrollInfo offsets
	internal void ScrollIntoView(
		uint index,
		bool isGroupItemIndex,
		bool isHeader,
		bool forceSynchronous,
		double pixelOffsetHint,
		ScrollIntoViewAlignment alignment)
	{
		var isHorizontal = false;
		index += isHeader ? 0u : 2u;

		double logicalOffset = index;

		isHorizontal = IsHorizontal();

		// Correct available size for non-virtualizing direction padding.
		if (isHorizontal)
		{
			if (alignment == ScrollIntoViewAlignment.Default && pixelOffsetHint > 0 && m_ScrollData.OffsetX < 2)
			{
				ComputeLogicalOffset(isHorizontal, ref pixelOffsetHint, out logicalOffset);
			}

			if (logicalOffset < 2)
			{
				SetHorizontalOffset(logicalOffset);
				// to avoid using pixelOffsetHint in subsequential calls to ScrollIntoView we should have updated layout.
				// it is expensive to do but there is no way we can control 2(or more) public call to ScrollIntoView API in 1 measure cycle.
				UpdateLayout();
			}
			else
			{
				var spVirtualizingPanel = (VirtualizingPanel)Panel!;
				spVirtualizingPanel.ScrollIntoView((int)(index - 2), isGroupItemIndex, alignment);
				if (forceSynchronous)
				{
					UpdateLayout();
				}
			}
		}
		else
		{
			if (alignment == ScrollIntoViewAlignment.Default && pixelOffsetHint > 0 && m_ScrollData.OffsetY < 2)
			{
				ComputeLogicalOffset(isHorizontal, ref pixelOffsetHint, out logicalOffset);
			}

			if (logicalOffset < 2)
			{
				SetVerticalOffset(logicalOffset);
				// to avoid using pixelOffsetHint in subsequential calls to ScrollIntoView we should have updated layout.
				// it is expensive to do but there is no way we can control 2(or more) public call to ScrollIntoView API in 1 measure cycle.
				UpdateLayout();
			}
			else
			{
				var spVirtualizingPanel = (VirtualizingPanel)Panel!;
				spVirtualizingPanel.ScrollIntoView((int)(index - 2), isGroupItemIndex, alignment);
				if (forceSynchronous)
				{
					UpdateLayout();
				}
			}
		}
	}
#endif

	internal Thickness PaddingInternal
	{
		get
		{
			var value = Padding;

			if (Panel is not ModernCollectionBasePanel)
			{
				// The Win8 logical virtualizing panels had issues with padding on the end of their scrollable extent
				// So, preserve their workaround. Win8 also disabled this for StackPanel, and it's not worth quirking right now.
				var isHorizontal = IsHorizontal();

				if (isHorizontal)
				{
					value.Left = Math.Max(0, value.Left);
					value.Right = 0;
				}
				else
				{
					value.Top = Math.Max(0, value.Top);
					value.Bottom = 0;
				}
			}

			return value;
		}
	}

	// Called when an ItemsControl's ContentTemplate changes.
	// Allows a hook for us to clean up the old ItemsPresenter before layout is run with the
	// new template and new ItemsPresenter.
	// If Header is a UIElement, we must remove its association with m_tpHeader here.
	// Otherwise, applying the new template and measuring the new ItemsPresenter will throw.
	internal static void Dispose(ItemsPresenter? pItemsPresenter)
	{
		// ItemPresenter peer may already have been released when the ControlTemplate was replaced.
		if (pItemsPresenter is not null)
		{
			if (pItemsPresenter.HeaderContentControl is { } header)
			{
				header.Content = null;
				header.ContentTemplate = null;
				header.ClearValue(ContentControl.ContentTransitionsProperty);
			}

			if (pItemsPresenter.FooterContentControl is { } footer)
			{
				footer.Content = null;
				footer.ContentTemplate = null;
				footer.ClearValue(ContentControl.ContentTransitionsProperty);
			}
		}
	}

	// get_HeaderContainer and get_FooterContainer: see HeaderContentControl and FooterContentControl.

	// Returns the first visible part (header, panel or footer) of this ItemsPresenter.
	internal void GetFirstVisiblePart(out ItemsPresenterParts pPart, out double pOffset)
	{
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

		// Default return value.
		pPart = ItemsPresenterParts.Header;
		pOffset = 0.0;

		if (spScrollViewer is not null)
		{
			bool isHorizontal = false;
			double viewportOffset = 0.0;
			double headerOffset = 0.0;
			double panelOffset = 0.0;
			double footerOffset = 0.0;

			isHorizontal = IsHorizontal();

			// Retrieves offset values depending of the panel orientation.
			if (isHorizontal)
			{
				viewportOffset = spScrollViewer.HorizontalOffset;
				headerOffset = m_lastHeaderArrangeRect.X;
				panelOffset = m_lastHeaderArrangeRect.X + m_lastHeaderArrangeRect.Width;
				footerOffset = m_lastFooterArrangeRect.X;
			}
			else
			{
				viewportOffset = spScrollViewer.VerticalOffset;
				headerOffset = m_lastHeaderArrangeRect.Y;
				panelOffset = m_lastHeaderArrangeRect.Y + m_lastHeaderArrangeRect.Height;
				footerOffset = m_lastFooterArrangeRect.Y;
			}

			if (viewportOffset < panelOffset)
			{
				// pPart is already set to ItemsPresenterPart_Header.
				pOffset = viewportOffset - headerOffset;
			}
			else if (viewportOffset < footerOffset)
			{
				pPart = ItemsPresenterParts.Panel;
				pOffset = viewportOffset - panelOffset;
			}
			else
			{
				pPart = ItemsPresenterParts.Footer;
				pOffset = viewportOffset - footerOffset;
			}
		}
	}

	private static bool IsScrollInfoPanel(UIElement? spPanel)
	{
#if !HAS_UNO // TODO Uno: panels do not implement IScrollInfo
		return spPanel is IScrollInfo;
#else
		return false;
#endif
	}
}
