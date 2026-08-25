// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\ScrollBar_Partial.cpp, tag winui3/release/1.6.5, commit 444ec52426

using System;
using DirectUI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;


namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class ScrollBar
{
	// Initializes a new instance of the ScrollBar class.
	public ScrollBar()
	{
		m_isIgnoringUserInput = false;
		m_isPointerOver = false;
		m_suspendVisualStateUpdates = false;
		m_dragValue = 0.0;
		m_blockIndicators = false;
		m_isUsingActualSizeAsExtent = false;

		Initialize();
	}

	// Prepares object's state
	private void Initialize()
	{
		DefaultStyleKey = typeof(ScrollBar);

		SizeChanged += OnSizeChanged;
#if !UNO_HAS_ENHANCED_LIFECYCLE
		LayoutUpdated += OnLayoutUpdated;
#endif
		Loaded += ReAttachEvents;
		Unloaded += DetachEvents;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == OrientationProperty)
		{
			OnOrientationChanged();
		}
		else if (args.Property == IndicatorModeProperty)
		{
			RefreshTrackLayout();
		}
		else if (args.Property == VisibilityProperty)
		{
			OnVisibilityChanged();
		}
	}

	// Update the visual states when the Visibility property is changed.
	private protected override void OnVisibilityChanged()
	{
		var visibility = Visibility;
		if (Visibility.Visible != visibility)
		{
			m_isPointerOver = false;
		}

		UpdateVisualState();
	}

	// Apply a template to the
	protected override void OnApplyTemplate()
	{
		try
		{
			string strAutomationName;
			m_suspendVisualStateUpdates = true;

			FrameworkElement spElementHorizontalTemplate;
			FrameworkElement spElementVerticalTemplate;
			FrameworkElement spElementHorizontalPanningRoot;
			FrameworkElement spElementHorizontalPanningThumb;
			FrameworkElement spElementVerticalPanningRoot;
			FrameworkElement spElementVerticalPanningThumb;

			RepeatButton spElementHorizontalLargeIncrease;
			RepeatButton spElementHorizontalLargeDecrease;
			RepeatButton spElementHorizontalSmallIncrease;
			RepeatButton spElementHorizontalSmallDecrease;

			RepeatButton spElementVerticalLargeIncrease;
			RepeatButton spElementVerticalLargeDecrease;
			RepeatButton spElementVerticalSmallIncrease;
			RepeatButton spElementVerticalSmallDecrease;

			Thumb spElementVerticalThumb;
			Thumb spElementHorizontalThumb;

			// Cleanup any existing template parts
			DetachEvents();

			m_tpElementHorizontalTemplate = null;
			m_tpElementHorizontalLargeIncrease = null;
			m_tpElementHorizontalLargeDecrease = null;
			m_tpElementHorizontalSmallIncrease = null;
			m_tpElementHorizontalSmallDecrease = null;
			m_tpElementHorizontalThumb = null;
			m_tpElementVerticalTemplate = null;
			m_tpElementVerticalLargeIncrease = null;
			m_tpElementVerticalLargeDecrease = null;
			m_tpElementVerticalSmallIncrease = null;
			m_tpElementVerticalThumb = null;
			m_tpElementVerticalSmallDecrease = null;
			m_tpElementHorizontalPanningRoot = null;
			m_tpElementHorizontalPanningThumb = null;
			m_tpElementVerticalPanningRoot = null;
			m_tpElementVerticalPanningThumb = null;

			// Apply the template to the base class
			base.OnApplyTemplate();

			// Get the parts
			spElementHorizontalTemplate = GetTemplateChildHelper<FrameworkElement>("HorizontalRoot");
			m_tpElementHorizontalTemplate = spElementHorizontalTemplate;
			spElementHorizontalLargeIncrease = GetTemplateChildHelper<RepeatButton>("HorizontalLargeIncrease");
			m_tpElementHorizontalLargeIncrease = spElementHorizontalLargeIncrease;
			if (m_tpElementHorizontalLargeIncrease != null)
			{
				strAutomationName = AutomationProperties.GetName(m_tpElementHorizontalLargeIncrease);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_HORIZONTALLARGEINCREASE");
					AutomationProperties.SetName(m_tpElementHorizontalLargeIncrease as RepeatButton, strAutomationName);
				}
			}
			spElementHorizontalSmallIncrease = GetTemplateChildHelper<RepeatButton>("HorizontalSmallIncrease");
			m_tpElementHorizontalSmallIncrease = spElementHorizontalSmallIncrease;
			if (m_tpElementHorizontalSmallIncrease != null)
			{
				strAutomationName = AutomationProperties.GetName(m_tpElementHorizontalSmallIncrease);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_HORIZONTALSMALLINCREASE");
					AutomationProperties.SetName(m_tpElementHorizontalSmallIncrease, strAutomationName);

				}
			}
			spElementHorizontalLargeDecrease = GetTemplateChildHelper<RepeatButton>("HorizontalLargeDecrease");
			m_tpElementHorizontalLargeDecrease = spElementHorizontalLargeDecrease;
			if (m_tpElementHorizontalLargeDecrease != null)
			{
				strAutomationName = AutomationProperties.GetName(m_tpElementHorizontalLargeDecrease);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_HORIZONTALLARGEDECREASE");
					AutomationProperties.SetName(m_tpElementHorizontalLargeDecrease, strAutomationName);

				}
			}
			spElementHorizontalSmallDecrease = GetTemplateChildHelper<RepeatButton>("HorizontalSmallDecrease");
			m_tpElementHorizontalSmallDecrease = spElementHorizontalSmallDecrease;
			if (m_tpElementHorizontalSmallDecrease != null)
			{
				strAutomationName = AutomationProperties.GetName(m_tpElementHorizontalSmallDecrease);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_HORIZONTALSMALLDECREASE");
					AutomationProperties.SetName(m_tpElementHorizontalSmallDecrease, strAutomationName);

				}
			}
			spElementHorizontalThumb = GetTemplateChildHelper<Thumb>("HorizontalThumb");
			m_tpElementHorizontalThumb = spElementHorizontalThumb;
			if (m_tpElementHorizontalThumb != null)
			{
				strAutomationName = AutomationProperties.GetName(m_tpElementHorizontalThumb);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_HORIZONTALTHUMB");
					AutomationProperties.SetName(m_tpElementHorizontalThumb, strAutomationName);

				}
			}

			spElementVerticalTemplate = GetTemplateChildHelper<FrameworkElement>("VerticalRoot");
			m_tpElementVerticalTemplate = spElementVerticalTemplate;

			spElementVerticalLargeIncrease = GetTemplateChildHelper<RepeatButton>("VerticalLargeIncrease");
			m_tpElementVerticalLargeIncrease = spElementVerticalLargeIncrease;
			if (m_tpElementVerticalLargeIncrease != null)
			{
				strAutomationName = AutomationProperties.GetName(m_tpElementVerticalLargeIncrease);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_VERTICALALLARGEINCREASE");
					AutomationProperties.SetName(m_tpElementVerticalLargeIncrease, strAutomationName);

				}
			}

			spElementVerticalSmallIncrease = GetTemplateChildHelper<RepeatButton>("VerticalSmallIncrease");
			m_tpElementVerticalSmallIncrease = spElementVerticalSmallIncrease;
			if (m_tpElementVerticalSmallIncrease != null)
			{
				strAutomationName = AutomationProperties.GetName(m_tpElementVerticalSmallIncrease);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_VERTICALSMALLINCREASE");
					AutomationProperties.SetName(m_tpElementVerticalSmallIncrease, strAutomationName);

				}
			}
			spElementVerticalLargeDecrease = GetTemplateChildHelper<RepeatButton>("VerticalLargeDecrease");
			m_tpElementVerticalLargeDecrease = spElementVerticalLargeDecrease;
			if (m_tpElementVerticalLargeDecrease != null)
			{
				strAutomationName = AutomationProperties.GetName(m_tpElementVerticalLargeDecrease);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_VERTICALLARGEDECREASE");
					AutomationProperties.SetName(m_tpElementVerticalLargeDecrease, strAutomationName);

				}
			}
			spElementVerticalSmallDecrease = GetTemplateChildHelper<RepeatButton>("VerticalSmallDecrease");
			m_tpElementVerticalSmallDecrease = spElementVerticalSmallDecrease;
			if (m_tpElementVerticalSmallDecrease != null)
			{
				strAutomationName = AutomationProperties.GetName(m_tpElementVerticalSmallDecrease);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_VERTICALSMALLDECREASE");
					AutomationProperties.SetName(m_tpElementVerticalSmallDecrease, strAutomationName);

				}
			}
			spElementVerticalThumb = GetTemplateChildHelper<Thumb>("VerticalThumb");
			m_tpElementVerticalThumb = spElementVerticalThumb;
			if (m_tpElementVerticalThumb != null)
			{
				strAutomationName = AutomationProperties.GetName(m_tpElementVerticalThumb);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.Current.GetLocalizedResourceString("UIA_SCROLLBAR_VERTICALTHUMB");
					AutomationProperties.SetName(m_tpElementVerticalThumb as Thumb, strAutomationName);

				}
			}

			spElementHorizontalPanningRoot = GetTemplateChildHelper<FrameworkElement>("HorizontalPanningRoot");
			m_tpElementHorizontalPanningRoot = spElementHorizontalPanningRoot;
			spElementHorizontalPanningThumb = GetTemplateChildHelper<FrameworkElement>("HorizontalPanningThumb");
			m_tpElementHorizontalPanningThumb = spElementHorizontalPanningThumb;
			spElementVerticalPanningRoot = GetTemplateChildHelper<FrameworkElement>("VerticalPanningRoot");
			m_tpElementVerticalPanningRoot = spElementVerticalPanningRoot;
			spElementVerticalPanningThumb = GetTemplateChildHelper<FrameworkElement>("VerticalPanningThumb");
			m_tpElementVerticalPanningThumb = spElementVerticalPanningThumb;

			// Attach the event handlers
			AttachEvents();

			// Updating states for parts where properties might have been updated
			// through XAML before the template was loaded.
			UpdateScrollBarVisibility();

			m_suspendVisualStateUpdates = false;
			ChangeVisualState(false);
		}
		finally
		{
			m_suspendVisualStateUpdates = false;
		}
	}

	private void DetachEvents()
	{
		if (m_tpElementHorizontalThumb != null)
		{
			m_ElementHorizontalThumbDragStartedToken.Disposable = null;
			m_ElementHorizontalThumbDragDeltaToken.Disposable = null;
			m_ElementHorizontalThumbDragCompletedToken.Disposable = null;
		}

		if (m_tpElementHorizontalLargeDecrease != null)
		{
			m_ElementHorizontalLargeDecreaseClickToken.Disposable = null;
		}

		if (m_tpElementHorizontalLargeIncrease != null)
		{
			m_ElementHorizontalLargeIncreaseClickToken.Disposable = null;
		}

		if (m_tpElementHorizontalSmallDecrease != null)
		{
			m_ElementHorizontalSmallDecreaseClickToken.Disposable = null;
		}

		if (m_tpElementHorizontalSmallIncrease != null)
		{
			m_ElementHorizontalSmallIncreaseClickToken.Disposable = null;
		}

		if (m_tpElementVerticalThumb != null)
		{
			m_ElementVerticalThumbDragStartedToken.Disposable = null;
			m_ElementVerticalThumbDragDeltaToken.Disposable = null;
			m_ElementVerticalThumbDragCompletedToken.Disposable = null;
		}

		if (m_tpElementVerticalLargeDecrease != null)
		{
			m_ElementVerticalLargeDecreaseClickToken.Disposable = null;
		}

		if (m_tpElementVerticalLargeIncrease != null)
		{
			m_ElementVerticalLargeIncreaseClickToken.Disposable = null;
		}

		if (m_tpElementVerticalSmallDecrease != null)
		{
			m_ElementVerticalSmallDecreaseClickToken.Disposable = null;
		}

		if (m_tpElementVerticalSmallIncrease != null)
		{
			m_ElementVerticalSmallIncreaseClickToken.Disposable = null;
		}
	}

	private static void ReAttachEvents(object snd, RoutedEventArgs args) // OnLoaded
	{
		if (snd is ScrollBar sb)
		{
			sb.DetachEvents(); // Do not double listen events!
			sb.AttachEvents();
		}
	}

	private void AttachEvents()
	{
		if (m_tpElementHorizontalThumb != null || m_tpElementVerticalThumb != null)
		{
			if (m_tpElementHorizontalThumb != null)
			{
				m_tpElementHorizontalThumb.DragStarted += OnThumbDragStarted;
				m_ElementHorizontalThumbDragStartedToken.Disposable = Disposable.Create(() => m_tpElementHorizontalThumb.DragStarted -= OnThumbDragStarted);
				m_tpElementHorizontalThumb.DragDelta += OnThumbDragDelta;
				m_ElementHorizontalThumbDragDeltaToken.Disposable = Disposable.Create(() => m_tpElementHorizontalThumb.DragDelta -= OnThumbDragDelta);
				m_tpElementHorizontalThumb.DragCompleted += OnThumbDragCompleted;
				m_ElementHorizontalThumbDragCompletedToken.Disposable = Disposable.Create(() => m_tpElementHorizontalThumb.DragCompleted -= OnThumbDragCompleted);
				m_tpElementHorizontalThumb.IgnoreTouchInput = true;
			}

			if (m_tpElementVerticalThumb != null)
			{
				m_tpElementVerticalThumb.DragStarted += OnThumbDragStarted;
				m_ElementVerticalThumbDragStartedToken.Disposable = Disposable.Create(() => m_tpElementVerticalThumb.DragStarted -= OnThumbDragStarted);
				m_tpElementVerticalThumb.DragDelta += OnThumbDragDelta;
				m_ElementVerticalThumbDragDeltaToken.Disposable = Disposable.Create(() => m_tpElementVerticalThumb.DragDelta -= OnThumbDragDelta);
				m_tpElementVerticalThumb.DragCompleted += OnThumbDragCompleted;
				m_ElementVerticalThumbDragCompletedToken.Disposable = Disposable.Create(() => m_tpElementVerticalThumb.DragCompleted -= OnThumbDragCompleted);
				m_tpElementVerticalThumb.IgnoreTouchInput = true;
			}
		}

		if (m_tpElementHorizontalLargeDecrease != null || m_tpElementVerticalLargeDecrease != null)
		{
			if (m_tpElementHorizontalLargeDecrease != null)
			{
				m_tpElementHorizontalLargeDecrease.Click += LargeDecrement;
				m_ElementHorizontalLargeDecreaseClickToken.Disposable = Disposable.Create(() => m_tpElementHorizontalLargeDecrease.Click -= LargeDecrement);
				m_tpElementHorizontalLargeDecrease.IgnoreTouchInput = true;
			}

			if (m_tpElementVerticalLargeDecrease != null)
			{
				m_tpElementVerticalLargeDecrease.Click += LargeDecrement;
				m_ElementVerticalLargeDecreaseClickToken.Disposable = Disposable.Create(() => m_tpElementVerticalLargeDecrease.Click -= LargeDecrement);
				m_tpElementVerticalLargeDecrease.IgnoreTouchInput = true;
			}
		}

		if (m_tpElementHorizontalLargeIncrease != null || m_tpElementVerticalLargeIncrease != null)
		{
			if (m_tpElementHorizontalLargeIncrease != null)
			{
				m_tpElementHorizontalLargeIncrease.Click += LargeIncrement;
				m_ElementHorizontalLargeIncreaseClickToken.Disposable = Disposable.Create(() => m_tpElementHorizontalLargeIncrease.Click -= LargeIncrement);
				m_tpElementHorizontalLargeIncrease.IgnoreTouchInput = true;
			}

			if (m_tpElementVerticalLargeIncrease != null)
			{
				m_tpElementVerticalLargeIncrease.Click += LargeIncrement;
				m_ElementVerticalLargeIncreaseClickToken.Disposable = Disposable.Create(() => m_tpElementVerticalLargeIncrease.Click -= LargeIncrement);
				m_tpElementVerticalLargeIncrease.IgnoreTouchInput = true;
			}
		}

		if (m_tpElementHorizontalSmallDecrease != null || m_tpElementVerticalSmallDecrease != null)
		{
			if (m_tpElementHorizontalSmallDecrease != null)
			{
				m_tpElementHorizontalSmallDecrease.Click += SmallDecrement;
				m_ElementHorizontalSmallDecreaseClickToken.Disposable = Disposable.Create(() => m_tpElementHorizontalSmallDecrease.Click -= SmallDecrement);
				m_tpElementHorizontalSmallDecrease.IgnoreTouchInput = true;
			}

			if (m_tpElementVerticalSmallDecrease != null)
			{
				m_tpElementVerticalSmallDecrease.Click += SmallDecrement;
				m_ElementVerticalSmallDecreaseClickToken.Disposable = Disposable.Create(() => m_tpElementVerticalSmallDecrease.Click -= SmallDecrement);
				m_tpElementVerticalSmallDecrease.IgnoreTouchInput = true;
			}
		}

		if (m_tpElementHorizontalSmallIncrease != null || m_tpElementVerticalSmallIncrease != null)
		{
			if (m_tpElementHorizontalSmallIncrease != null)
			{
				m_tpElementHorizontalSmallIncrease.Click += SmallIncrement;
				m_ElementHorizontalSmallIncreaseClickToken.Disposable = Disposable.Create(() => m_tpElementHorizontalSmallIncrease.Click -= SmallIncrement);
				m_tpElementHorizontalSmallIncrease.IgnoreTouchInput = true;
			}

			if (m_tpElementVerticalSmallIncrease != null)
			{
				m_tpElementVerticalSmallIncrease.Click += SmallIncrement;
				m_ElementVerticalSmallIncreaseClickToken.Disposable = Disposable.Create(() => m_tpElementVerticalSmallIncrease.Click -= SmallIncrement);
				m_tpElementVerticalSmallIncrease.IgnoreTouchInput = true;
			}
		}
	}

	// Retrieves a reference to a child template object given its name
	private T GetTemplateChildHelper<T>(string childName) where T : class
		=> GetTemplateChild(childName) as T;


	// IsEnabled property changed handler.
	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
	{
		base.OnIsEnabledChanged(e);

		var isEnabled = IsEnabled;
		if (!isEnabled)
		{
			m_isPointerOver = false;
		}

		UpdateVisualState();
	}

	// PointerEnter event handler.
	protected override void OnPointerEntered(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerEntered(pArgs);

		m_isPointerOver = true;

		if (!IsDragging)
		{
			UpdateVisualState();
		}
	}

	// PointerExited event handler.
	protected override void OnPointerExited(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerExited(pArgs);

		m_isPointerOver = false;

		if (!IsDragging)
		{
			UpdateVisualState();
		}
	}

	// PointerPressed event handler.
	protected override void OnPointerPressed(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerPressed(pArgs);

		var handled = pArgs.Handled;

		var spPointerPoint = pArgs.GetCurrentPoint(this);

		var spPointerProperties = spPointerPoint.Properties;
		var bIsLeftButtonPressed = spPointerProperties.IsLeftButtonPressed;
		if (bIsLeftButtonPressed)
		{
			if (!handled)
			{
				pArgs.Handled = true;
				var spPointer = pArgs.Pointer;
				CapturePointer(spPointer);
			}
		}
	}

	// PointerReleased event handler.
	protected override void OnPointerReleased(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerReleased(pArgs);

		var handled = pArgs.Handled;
		if (!handled)
		{
			pArgs.Handled = true;
		}
	}

	/// PointerCaptureLost event handler.
	protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerCaptureLost(pArgs);
		UpdateVisualState(true);
	}

	protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs pArgs)
	{
		base.OnDoubleTapped(pArgs);
		pArgs.Handled = true;
	}

	protected override void OnTapped(TappedRoutedEventArgs pArgs)
	{
		base.OnTapped(pArgs);
		pArgs.Handled = true;
	}

	/// <summary>
	/// Create ScrollBarAutomationPeer to represent the ScrollBar.
	/// </summary>
	/// <returns>Automation peer.</returns>
	protected override AutomationPeer OnCreateAutomationPeer() => new ScrollBarAutomationPeer(this);

	// Change to the correct visual state for the button.
	private protected override void ChangeVisualState(
		// true to use transitions when updating the visual state, false
		// to snap directly to the new visual state.
		bool bUseTransitions)
	{
		if (m_suspendVisualStateUpdates)
		{
			return;
		}

		// Uno Specific: Performance optimization - only load half of the template if orientation is fixed.
		base.ChangeVisualState(bUseTransitions);

		var scrollingIndicator = IndicatorMode;
		var isEnabled = IsEnabled;
		bool isSuccessful;
		if (!isEnabled)
		{
			VisualStateManager.GoToState(this, "Disabled", bUseTransitions);
		}
		else if (m_isPointerOver)
		{
			isSuccessful = VisualStateManager.GoToState(this, "PointerOver", bUseTransitions);
			//Default to Normal if PointerOver state isn't available.
			if (!isSuccessful)
			{
				VisualStateManager.GoToState(this, "Normal", bUseTransitions);
			}
		}
		else
		{
			VisualStateManager.GoToState(this, "Normal", bUseTransitions);
		}

		if (!m_blockIndicators && (!IsConscious() || scrollingIndicator == ScrollingIndicatorMode.MouseIndicator))
		{
			VisualStateManager.GoToState(this, "MouseIndicator", bUseTransitions);
		}
		else if (!m_blockIndicators && scrollingIndicator == ScrollingIndicatorMode.TouchIndicator)
		{
			isSuccessful = VisualStateManager.GoToState(this, "TouchIndicator", bUseTransitions);
			//Default to MouseActiveState if Panning state isn't available.
			if (!isSuccessful)
			{
				VisualStateManager.GoToState(this, "MouseIndicator", bUseTransitions);
			}
		}
		else
		{
			VisualStateManager.GoToState(this, "NoIndicator", bUseTransitions);
		}

		// Expanded/Collapsed States were added in RS3 and ExpandedWithoutAnimation/CollapsedWithoutAnimation states
		// were added in RS4. Since Expanded can exist without ExpandedWithoutAnimation (and the same for collapsed)
		// each time we try to transition  to a *WithoutAnimation state we need to check to make sure the transition was
		// successful. If it was not we fallback to the appropriate expanded or collapsed state.
		// No Quirks are required since these are new states and if the states are not present then they are no-op.
		// UseTransitions is always true since the delay behavior is defined in the transitions when
		// animations are enabled. When animations are disabled, the framework does not run transitions.
		if (!IsConscious())
		{
			VisualStateManager.GoToState(this, (isEnabled ? "Expanded" : "Collapsed"), true /* useTransitions */);
		}
		else
		{
			isSuccessful = false;
			var animate = SharedHelpers.IsAnimationsEnabled();
			if (isEnabled && m_isPointerOver)
			{
				if (!animate)
				{
					isSuccessful = VisualStateManager.GoToState(this, "ExpandedWithoutAnimation", true /* useTransitions */);
				}
				if (!isSuccessful)
				{
					VisualStateManager.GoToState(this, "Expanded", true /* useTransitions */);
				}
			}
			else
			{
				if (!animate)
				{
					isSuccessful = VisualStateManager.GoToState(this, "CollapsedWithoutAnimation", true /* useTransitions */);
				}
				if (!isSuccessful)
				{
					VisualStateManager.GoToState(this, "Collapsed", true /* useTransitions */);
				}
			}
		}
	}

	// Returns the actual length of the ScrollBar in the direction of its orientation.
	private double GetTrackLength()
	{
		var orientation = Orientation;
		double length;
		if (orientation == Orientation.Horizontal)
		{
			length = ActualWidth;
		}
		else
		{
			length = ActualHeight;
		}

		//
		// Set the track length as zero which is collapsed state if the length is greater than
		// the current viewport size in case of the layout is dirty with using actual size(Width/Height)
		// as the extent. The invalid track length setting will cause of updating the new layout size
		// on the ScrollViewer and ScrollContentPresenter that will keep to ask ScrollBar for updating
		// the track length continuously which is a layout cycle crash.
		//
		if (m_isUsingActualSizeAsExtent
			&& (IsMeasureDirty || IsArrangeDirty) // TODO Uno: Should also check IsOnMeasureDirtyPath and IsOnArrangeDirtyPath
		)
		{
			var viewport = ViewportSize;

			// Return the length as zero because of current length is greater than
			// the viewport that is a layout cycle issue. The valid length and
			// viewport will be updated after complete layout updating.
			if (!double.IsNaN(viewport) && !double.IsNaN(length) && length != 0 && length > viewport)
			{
				return 0.0f;
			}
		}

		// Added to consider the case where everything is collapsed.
		return double.IsNaN(length) ? 0.0f : length;
	}

	// Returns the combined actual length in the direction of its orientation of the ScrollBar's RepeatButtons.
	private double GetRepeatButtonsLength()
	{
		double length = 0;
		Thickness increaseMargin;
		Thickness decreaseMargin;
		var orientation = Orientation;
		double smallLength;
		if (orientation == Orientation.Horizontal)
		{
			if (m_tpElementHorizontalSmallDecrease != null)
			{
				smallLength = m_tpElementHorizontalSmallDecrease.ActualWidth;
				decreaseMargin = m_tpElementHorizontalSmallDecrease.Margin;
				length = smallLength + decreaseMargin.Left + decreaseMargin.Right;
			}
			if (m_tpElementHorizontalSmallIncrease != null)
			{
				smallLength = m_tpElementHorizontalSmallIncrease.ActualWidth;
				increaseMargin = m_tpElementHorizontalSmallIncrease.Margin;
				length += smallLength + increaseMargin.Left + increaseMargin.Right;
			}
		}
		else
		{
			if (m_tpElementVerticalSmallDecrease != null)
			{
				smallLength = m_tpElementVerticalSmallDecrease.ActualHeight;
				decreaseMargin = m_tpElementVerticalSmallDecrease.Margin;
				length = smallLength + decreaseMargin.Top + decreaseMargin.Bottom;
			}
			if (m_tpElementVerticalSmallIncrease != null)
			{
				smallLength = m_tpElementVerticalSmallIncrease.ActualHeight;
				increaseMargin = m_tpElementVerticalSmallIncrease.Margin;
				length += smallLength + increaseMargin.Top + increaseMargin.Bottom;
			}
		}

		return length;
	}

	protected override void OnValueChanged(double oldValue, double newValue)
	{
		base.OnValueChanged(oldValue, newValue);
		UpdateTrackLayout();
	}

	// Called when the Minimum value changed.
	protected override void OnMinimumChanged(
			double oldMinimum,
			double newMinimum)
	{
		base.OnMinimumChanged(oldMinimum, newMinimum);
		UpdateTrackLayout();
	}

	// Called when the Maximum value changed.
	protected override void OnMaximumChanged(
			double oldMaximum,
			double newMaximum)
	{
		base.OnMaximumChanged(oldMaximum, newMaximum);
		UpdateTrackLayout();
	}

	// Gets a value indicating whether the ScrollBar is currently dragging.
	private bool IsDragging
	{
		get
		{
			var orientation = Orientation;
			if (orientation == Orientation.Horizontal && m_tpElementHorizontalThumb != null)
			{
				return m_tpElementHorizontalThumb.IsDragging;
			}
			else if (orientation == Orientation.Vertical && m_tpElementVerticalThumb != null)
			{
				return m_tpElementVerticalThumb.IsDragging;
			}
			else
			{
				return false;
			}
		}
	}

	// Value indicating whether the ScrollBar reacts to user input or not.
	internal bool IsIgnoringUserInput
	{
		get => m_isIgnoringUserInput;
		set => m_isIgnoringUserInput = value;
	}

	// NOTE: Currently only used internally for Automation
	internal UIElement ElementHorizontalTemplate => m_tpElementHorizontalTemplate;

	internal UIElement ElementVerticalTemplate => m_tpElementVerticalTemplate;

	// Called whenever the Thumb drag operation is started.
	private void OnThumbDragStarted(
		object pSender,
		DragStartedEventArgs pArgs)
	{
		m_dragValue = Value;

		ThumbDragStarted?.Invoke(this, pArgs);
	}

	// Whenever the thumb gets dragged, we handle the event through this function to
	// update the current value depending upon the thumb drag delta.
	private void OnThumbDragDelta(
		object pSender,
		DragDeltaEventArgs pArgs)
	{
		double offset = 0.0;
		double zoom = 1.0;
		var maximum = Maximum;
		var minimum = Minimum;
		var orientation = Orientation;
		double trackLength;
		double repeatButtonsLength;
		double thumbSize;
		double change;
		if (orientation == Orientation.Horizontal &&
			m_tpElementHorizontalThumb != null)
		{
			change = pArgs.HorizontalChange;
			trackLength = GetTrackLength();
			repeatButtonsLength = GetRepeatButtonsLength();
			trackLength -= repeatButtonsLength;
			thumbSize = m_tpElementHorizontalThumb.ActualWidth;

			offset = (zoom * change) / (trackLength - thumbSize) * (maximum - minimum);
		}
		else if (orientation == Orientation.Vertical &&
			m_tpElementVerticalThumb != null)
		{
			change = pArgs.VerticalChange;
			trackLength = GetTrackLength();
			repeatButtonsLength = GetRepeatButtonsLength();
			trackLength -= repeatButtonsLength;
			thumbSize = m_tpElementVerticalThumb.ActualHeight;

			offset = (zoom * change) / (trackLength - thumbSize) * (maximum - minimum);
		}

		if (!double.IsNaN(offset) &&
			!double.IsInfinity(offset))
		{
			m_dragValue += offset;
			var newValue = Math.Min(maximum, Math.Max(minimum, m_dragValue));
			var value = Value;
			if (newValue != value)
			{
				Value = newValue;
				RaiseScrollEvent(ScrollEventType.ThumbTrack);
			}
		}
	}

	// Raise the Scroll event when teh Thumb drag is completed.
	private void OnThumbDragCompleted(
		object pSender,
		DragCompletedEventArgs pArgs)
	{
		RaiseScrollEvent(ScrollEventType.EndScroll);

		ThumbDragCompleted?.Invoke(this, pArgs);
	}

	// Handle the SizeChanged event.
	private static void OnSizeChanged(
		object pSender,
		SizeChangedEventArgs pArgs)
	{
		(pSender as ScrollBar)?.UpdateTrackLayout();
	}

	// Called whenever the SmallDecrement button is clicked.
	private void SmallDecrement(
		object pSender,
		RoutedEventArgs pArgs)
	{
		var value = Value;
		var change = SmallChange;
		var edge = Minimum;
		var newValue = Math.Max(value - change, edge);
		if (newValue != value)
		{
			Value = newValue;
			RaiseScrollEvent(ScrollEventType.SmallDecrement);
		}
	}

	// Called whenever the SmallIncrement button is clicked.
	private void SmallIncrement(
		object pSender,
		RoutedEventArgs pArgs)
	{
		var value = Value;
		var change = SmallChange;
		var edge = Maximum;
		var newValue = Math.Min(value + change, edge);
		if (newValue != value)
		{
			Value = newValue;
			RaiseScrollEvent(ScrollEventType.SmallIncrement);
		}
	}

	// Called whenever the LargeDecrement button is clicked.
	private void LargeDecrement(
		object pSender,
		RoutedEventArgs pArgs)
	{
		var value = Value;
		var change = LargeChange;
		var edge = Minimum;
		var newValue = Math.Max(value - change, edge);
		if (newValue != value)
		{
			Value = newValue;
			RaiseScrollEvent(ScrollEventType.LargeDecrement);
		}
	}

	// Called whenever the LargeIncrement button is clicked.
	private void LargeIncrement(
		object pSender,
		RoutedEventArgs pArgs)
	{
		var value = Value;
		var change = LargeChange;
		var edge = Maximum;
		var newValue = Math.Min(value + change, edge);
		if (newValue != value)
		{
			Value = newValue;
			RaiseScrollEvent(ScrollEventType.LargeIncrement);
		}
	}

	// This raises the Scroll event, passing in the scrollEventType as a parameter
	// to let the handler know what triggered this event.
	private void RaiseScrollEvent(ScrollEventType scrollEventType)
	{
		// WinUI TODO: Add tracing for small change events

		// Create the args
		var spArgs = new ScrollEventArgs();
		spArgs.ScrollEventType = scrollEventType;
		spArgs.NewValue = Value;
		spArgs.OriginalSource = this;

		// Raise the event
		Scroll?.Invoke(this, spArgs);
	}

	// Change the template being used to display this control when the orientation
	// changes.
	private void OnOrientationChanged()
	{
		Orientation orientation = Orientation;

		//Set Visible and collapsed based on orientation.
		if (m_tpElementVerticalTemplate != null)
		{
			m_tpElementVerticalTemplate.Visibility =
				orientation == Orientation.Horizontal ?
					Visibility.Collapsed :
					Visibility.Visible;
		}

		if (m_tpElementVerticalPanningRoot != null)
		{
			m_tpElementVerticalPanningRoot.Visibility =
				orientation == Orientation.Horizontal ?
					Visibility.Collapsed :
					Visibility.Visible;
		}

		if (m_tpElementHorizontalTemplate != null)
		{
			m_tpElementHorizontalTemplate.Visibility =
				orientation == Orientation.Horizontal ?
					Visibility.Visible :
					Visibility.Collapsed;
		}

		if (m_tpElementHorizontalPanningRoot != null)
		{
			m_tpElementHorizontalPanningRoot.Visibility =
				orientation == Orientation.Horizontal ?
					Visibility.Visible :
					Visibility.Collapsed;
		}

		UpdateTrackLayout();
	}

	// Update track based on panning or mouse activity
	private void RefreshTrackLayout()
	{
		UpdateTrackLayout();
		ChangeVisualState(true);
	}

	//Update scrollbar visibility based on what input device is active and the orientation
	//of the
	private void UpdateScrollBarVisibility()
	{
		OnOrientationChanged();
		RefreshTrackLayout();
	}

	// This method will take the current min, max, and value to
	// calculate and layout the current control measurements.
	private void UpdateTrackLayout()
	{
		double multiplier;
		Thickness newMargin;

		var maximum = Maximum;
		var minimum = Minimum;
		var value = Value;
		var orientation = Orientation;
		var trackLength = GetTrackLength();
		UpdateIndicatorLengths(trackLength, out var mouseIndicatorLength, out var touchIndicatorLength);
		double difference = maximum - minimum;
		//Check to make sure that its not dividing by zero.
		if (difference == 0.0)
		{
			multiplier = 0.0;
		}
		else
		{
			multiplier = (value - minimum) / difference;
		}

		var repeatButtonsLength = GetRepeatButtonsLength();
		double largeDecreaseNewSize = Math.Max(0.0, multiplier * (trackLength - repeatButtonsLength - mouseIndicatorLength));
		double indicatorOffset = Math.Max(0.0, multiplier * (trackLength - touchIndicatorLength));

		if (orientation == Orientation.Horizontal &&
			m_tpElementHorizontalLargeDecrease != null &&
			m_tpElementHorizontalThumb != null)
		{
			m_tpElementHorizontalLargeDecrease.Width = largeDecreaseNewSize;
		}
		else if (orientation == Orientation.Vertical &&
			m_tpElementVerticalLargeDecrease != null &&
			m_tpElementVerticalThumb != null)
		{
			m_tpElementVerticalLargeDecrease.Height = largeDecreaseNewSize;
		}

		if (orientation == Orientation.Horizontal &&
			m_tpElementHorizontalPanningRoot != null)
		{
			newMargin = m_tpElementHorizontalPanningRoot.Margin;
			newMargin.Left = indicatorOffset;
			m_tpElementHorizontalPanningRoot.Margin = newMargin;
		}
		else if (orientation == Orientation.Vertical &&
			m_tpElementVerticalPanningRoot != null)
		{
			newMargin = m_tpElementVerticalPanningRoot.Margin;
			newMargin.Top = indicatorOffset;
			m_tpElementVerticalPanningRoot.Margin = newMargin;
		}
	}

	// Based on the ViewportSize, the Track's length, and the Minimum and Maximum
	// values, we will calculate the length of the Thumb.
	private double ConvertViewportSizeToDisplayUnits(double trackLength)
	{
		double pThumbSize;
		var maximum = Maximum;
		var minimum = Minimum;
		var viewport = ViewportSize;

		double thumbSize = trackLength * viewport / DoubleUtil.Max(1, viewport + maximum - minimum);

		// When UseLayoutRounding is True (default), the thumb size, whether it's for touch or mouse input, needs to be rounded to
		// the nearest value taking into account the rounding step 1.0f / RootScale.GetRasterizationScaleForElement(GetHandle()).
		// Rounding to the nearest whole number would risk a situation where ElementVerticalPanningRoot.Margin.Top + ElementVerticalPanningThumb.Height
		// could grow the ScrollBar when ScrollBar.Value is at its maximum ScrollBar.Maximum.
		bool roundedThumbSizeWithLayoutRound = RoundWithLayoutRound(ref thumbSize);

		if (roundedThumbSizeWithLayoutRound)
		{
			pThumbSize = thumbSize;
		}
		else
		{
			// We need to round to the nearest whole number.
			// In the case where pThumbSize is calculated to have a fractional part of exactly .5,
			// then in UpdateTrackLayout() where we calculate the largeDecreaseNewSize we end up giving
			// largeDecreaseNewSize a size of 0.5 as well, and at this point the grid laying out
			// the mouse portion of the ScrollBar template nudges the increase repeat button 1 px.
			pThumbSize = DoubleUtil.Round(thumbSize, 0);
		}

		return pThumbSize;
	}

	// This will resize the Thumb, based on calculations with the
	// ViewportSize, the Track's length, and the Minimum and Maximum
	// values.
	private void UpdateIndicatorLengths(
		double trackLength,
		out double pMouseIndicatorLength,
		out double pTouchIndicatorLength)
	{
		double result = double.NaN;
		bool hideThumb = trackLength <= 0.0;
		bool mouseIndicatorLengthWasSet = false;
		bool touchIndicatorLengthWasSet = false;
		pMouseIndicatorLength = 0.0;
		pTouchIndicatorLength = 0.0;

		// Uno workaround: If the scrollbar is smaller than the min size of the thumb,
		//		we will try to set the visibility collapsed and then request to 'HideThumb',
		//		which will drive uno to fall in an infinite layout cycle.
		//		We instead cache the value and apply it only at the end.
		double? m_tpElementHorizontalThumbWidth = default, m_tpElementVerticalThumbHeight = default, m_tpElementHorizontalPanningThumbWidth = default, m_tpElementVerticalPanningThumbHeight = default;
		Visibility? m_tpElementHorizontalThumbVisibility = default, m_tpElementVerticalThumbVisibility = default, m_tpElementHorizontalPanningThumbVisibility = default, m_tpElementVerticalPanningThumbVisibility = default;

		if (!hideThumb)
		{
			double mouseMinSize = 0.0;
			var orientation = Orientation;
			var maximum = Maximum;
			var minimum = Minimum;
			var repeatButtonsLength = GetRepeatButtonsLength();
			var trackLengthMinusRepeatButtonsLength = trackLength - repeatButtonsLength;
			var mouseIndicatorSize = ConvertViewportSizeToDisplayUnits(trackLengthMinusRepeatButtonsLength);

			double actualSize;
			double actualSizeMinusRepeatButtonsLength;

			if (orientation == Orientation.Horizontal &&
				m_tpElementHorizontalThumb != null)
			{
				if (maximum - minimum != 0)
				{
					mouseMinSize = m_tpElementHorizontalThumb.MinWidth;
					RoundWithLayoutRound(ref mouseMinSize);
					result = Math.Max(mouseMinSize, mouseIndicatorSize);
				}

				// Hide the thumb if too big
				actualSize = ActualWidth;
				actualSizeMinusRepeatButtonsLength = actualSize - repeatButtonsLength;
				if (maximum - minimum == 0 || result > actualSizeMinusRepeatButtonsLength || trackLengthMinusRepeatButtonsLength <= mouseMinSize)
				{
					hideThumb = true;
				}
				else
				{
					m_tpElementHorizontalThumbVisibility = Visibility.Visible;
					m_tpElementHorizontalThumbWidth = result;
					mouseIndicatorLengthWasSet = true;
				}
			}
			else if (orientation == Orientation.Vertical &&
				m_tpElementVerticalThumb != null)
			{
				if (maximum - minimum != 0)
				{
					mouseMinSize = m_tpElementVerticalThumb.MinHeight;
					RoundWithLayoutRound(ref mouseMinSize);
					result = Math.Max(mouseMinSize, mouseIndicatorSize);
				}

				// Hide the thumb if too big
				actualSize = ActualHeight;
				actualSizeMinusRepeatButtonsLength = actualSize - repeatButtonsLength;
				if (maximum - minimum == 0 || result > actualSizeMinusRepeatButtonsLength || trackLengthMinusRepeatButtonsLength <= mouseMinSize)
				{
					hideThumb = true;
				}
				else
				{
					m_tpElementVerticalThumbVisibility = Visibility.Visible;
					m_tpElementVerticalThumbHeight = result;
					mouseIndicatorLengthWasSet = true;
				}
			}

			if (mouseIndicatorLengthWasSet)
			{
				//added to consider the case where everything is collapsed.
				pMouseIndicatorLength = double.IsNaN(result) ? 0.0f : result;
			}

			var touchIndicatorSize = ConvertViewportSizeToDisplayUnits(trackLength);
			double touchMinSize = 0.0;

			//Do the same for horizontal panning indicator.
			if (orientation == Orientation.Horizontal &&
				m_tpElementHorizontalPanningThumb != null)
			{
				if (maximum - minimum != 0)
				{
					touchMinSize = m_tpElementHorizontalPanningThumb.MinWidth;
					RoundWithLayoutRound(ref touchMinSize);
					result = Math.Max(touchMinSize, touchIndicatorSize);
				}

				// Hide the thumb if too big
				actualSize = ActualWidth;
				if (maximum - minimum == 0 || result > actualSize || trackLength <= touchMinSize)
				{
					hideThumb = true;
				}
				else
				{
					m_tpElementHorizontalPanningThumbVisibility = Visibility.Visible;
					m_tpElementHorizontalPanningThumbWidth = result;
					touchIndicatorLengthWasSet = true;
				}
			}
			else if (orientation == Orientation.Vertical &&
				m_tpElementVerticalPanningThumb != null)
			{
				if (maximum - minimum != 0)
				{
					touchMinSize = m_tpElementVerticalPanningThumb.MinHeight;
					RoundWithLayoutRound(ref touchMinSize);
					result = Math.Max(touchMinSize, touchIndicatorSize);
				}

				// Hide the thumb if too big
				actualSize = ActualHeight;
				if (maximum - minimum == 0 || result > actualSize || trackLength <= touchMinSize)
				{
					hideThumb = true;
				}
				else
				{
					m_tpElementVerticalPanningThumbVisibility = Visibility.Visible;
					m_tpElementVerticalPanningThumbHeight = result;
					touchIndicatorLengthWasSet = true;
				}
			}

			if (touchIndicatorLengthWasSet)
			{
				//added to consider the case where everything is collapsed.
				pTouchIndicatorLength = double.IsNaN(result) ? 0.0f : result;
			}
		}

		if (hideThumb)
		{
			if (m_tpElementHorizontalThumb != null)
			{
				m_tpElementHorizontalThumb.Visibility = Visibility.Collapsed;
			}
			if (m_tpElementVerticalThumb != null)
			{
				m_tpElementVerticalThumb.Visibility = Visibility.Collapsed;
			}
			if (m_tpElementHorizontalPanningThumb != null)
			{
				m_tpElementHorizontalPanningThumb.Visibility = Visibility.Collapsed;
			}
			if (m_tpElementVerticalPanningThumb != null)
			{
				m_tpElementVerticalPanningThumb.Visibility = Visibility.Collapsed;
			}
		}
		else
		{
			// Uno workaround: Apply the cached values
			if (m_tpElementHorizontalThumbWidth.HasValue) m_tpElementHorizontalThumb.Width = m_tpElementHorizontalThumbWidth.Value;
			if (m_tpElementVerticalThumbHeight.HasValue) m_tpElementVerticalThumb.Height = m_tpElementVerticalThumbHeight.Value;
			if (m_tpElementHorizontalPanningThumbWidth.HasValue) m_tpElementHorizontalPanningThumb.Width = m_tpElementHorizontalPanningThumbWidth.Value;
			if (m_tpElementVerticalPanningThumbHeight.HasValue) m_tpElementVerticalPanningThumb.Height = m_tpElementVerticalPanningThumbHeight.Value;
			if (m_tpElementHorizontalThumbVisibility.HasValue) m_tpElementHorizontalThumb.Visibility = m_tpElementHorizontalThumbVisibility.Value;
			if (m_tpElementVerticalThumbVisibility.HasValue) m_tpElementVerticalThumb.Visibility = m_tpElementVerticalThumbVisibility.Value;
			if (m_tpElementHorizontalPanningThumbVisibility.HasValue) m_tpElementHorizontalPanningThumb.Visibility = m_tpElementHorizontalPanningThumbVisibility.Value;
			if (m_tpElementVerticalPanningThumbVisibility.HasValue) m_tpElementVerticalPanningThumb.Visibility = m_tpElementVerticalPanningThumbVisibility.Value;
		}
	}

	// during a SemanticZoomOperation we want to be able to block the scrollbar
	// without stomping over the user value
	internal void BlockIndicatorFromShowing()
	{
		if (!m_blockIndicators)
		{
			m_blockIndicators = true;
			ChangeVisualState(false);
		}
	}

	internal void ResetBlockIndicatorFromShowing()
	{
		m_blockIndicators = false;

		// Don't change state; stay in NoIndicator. The next ScrollViewer.ShowIndicators()
		// call will drive our next GoToState() call, with transitions.
	}

	internal void AdjustDragValue(double delta)
	{

		// If somebody is calling this when not dragging, are they confused?
		var dragging = IsDragging;

		m_dragValue += delta;
	}

	// Rounds the value using CUIElement::LayoutRound when get_UseLayoutRounding returns True.
	// Returns True when rounding was performed, and False otherwise.
	private bool RoundWithLayoutRound(ref double value)
	{
		bool roundValue = UseLayoutRounding;

		if (roundValue)
		{
			value = LayoutRound(value);
		}

		return roundValue;
	}
}
