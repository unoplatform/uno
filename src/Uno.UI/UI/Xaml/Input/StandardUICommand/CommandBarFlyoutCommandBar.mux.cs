using System.Numerics;
using Uno.UI.DataBinding;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class CommandBarFlyoutCommandBar
{
	//	public CommandBarFlyoutCommandBar()
	//	{
	//		DefaultStyleKey = typeof(CommandBarFlyoutCommandBar);

	//		SetValue(FlyoutTemplateSettingsProperty, new CommandBarFlyoutCommandBarTemplateSettings());

	//		Loaded({
	//			[this](var &, var &)

	//		{
	//#if _DEBUG
	//            COMMANDBARFLYOUT_TRACE_INFO(this, TRACE_MSG_METH_STR, METH_NAME, this, "Loaded");
	//#endif

	//				UpdateUI(!m_commandBarFlyoutIsOpening);

	//				if (var owningFlyout = m_owningFlyout)
	//            {
	//					// We only want to focus an initial element if we're opening in standard mode -
	//					// in transient mode, we don't want to be taking focus.
	//					if (owningFlyout.ShowMode() == FlyoutShowMode.Standard)
	//					{
	//						// Programmatically focus the first primary command if any, else programmatically focus the first secondary command if any.
	//						var commands = PrimaryCommands().Size() > 0 ? PrimaryCommands() : (SecondaryCommands().Size() > 0 ? SecondaryCommands() : null);

	//						if (commands)
	//						{
	//							bool usingPrimaryCommands = commands == PrimaryCommands();
	//							bool ensureTabStopUniqueness = usingPrimaryCommands || SharedHelpers.IsRS3OrHigher();
	//							var firstCommandAsFrameworkElement = commands[0] as FrameworkElement;

	//							if (firstCommandAsFrameworkElement)
	//							{
	//								if (SharedHelpers.IsFrameworkElementLoaded(firstCommandAsFrameworkElement))
	//								{
	//									FocusCommand(
	//										commands,
	//										usingPrimaryCommands ? m_moreButton : null /*moreButton*/,
	//										FocusState.Programmatic /*focusState*/,
	//										true /*firstCommand*/,
	//										ensureTabStopUniqueness);
	//								}
	//								else
	//								{
	//									m_firstItemLoadedRevoker = firstCommandAsFrameworkElement.Loaded(auto_revoke,

	//								{
	//										[this, commands, usingPrimaryCommands, ensureTabStopUniqueness](object & sender, var &)

	//									{
	//											FocusCommand(
	//												commands,
	//												usingPrimaryCommands ? m_moreButton : null /*moreButton*/,
	//												FocusState.Programmatic /*focusState*/,
	//												true /*firstCommand*/,
	//												ensureTabStopUniqueness);
	//											m_firstItemLoadedRevoker.revoke();
	//										}
	//									});
	//								}
	//							}
	//						}
	//					}
	//				}
	//			}
	//		});

	//		SizeChanged({
	//			[this](var &, var &)

	//		{
	//#if _DEBUG
	//            COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR, METH_NAME, this, "SizeChanged");
	//#endif

	//				UpdateUI(!m_commandBarFlyoutIsOpening, true /*isForSizeChange*/);
	//			}
	//		});

	//		Closing({
	//			[this](var &, var &)

	//		{
	//#if _DEBUG
	//            COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR, METH_NAME, this, "Closing");
	//#endif

	//				if (var owningFlyout = m_owningFlyout)
	//            {
	//					if (owningFlyout.AlwaysExpanded())
	//					{
	//						// Don't close the secondary commands list when the flyout is AlwaysExpanded.
	//						IsOpen(true);
	//					}
	//				}
	//			}
	//		});

	//		Closed({
	//			[this](var &, var &)

	//		{
	//#if _DEBUG
	//            COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR, METH_NAME, this, "Closed");
	//#endif

	//				m_secondaryItemsRootSized = false;

	//				if (!SharedHelpers.IsRS3OrHigher() && PrimaryCommands().Size() > 0)
	//				{
	//					// Before RS3, ensure the focus goes to a primary command when
	//					// the secondary commands are closed.
	//					EnsureFocusedPrimaryCommand();
	//				}
	//			}
	//		});

	//		RegisterPropertyChangedCallback(
	//			AppBar.IsOpenProperty(),

	//			[this](var &, var &)

	//		{
	//#if _DEBUG
	//            COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR, METH_NAME, this, "IsOpenProperty changed");
	//#endif

	//			UpdateFlowsFromAndFlowsTo();
	//			UpdateUI(!m_commandBarFlyoutIsOpening);
	//		});

	//		// Since we own these vectors, we don't need to cache the event tokens -
	//		// in fact, if we tried to remove these handlers in our destructor,
	//		// these properties will have already been cleared, and we'll nullref.
	//		PrimaryCommands().VectorChanged({
	//			[this](var &, var &)

	//		{
	//#if _DEBUG
	//            COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR, METH_NAME, this, "PrimaryCommands VectorChanged");
	//#endif

	//				EnsureLocalizedControlTypes();
	//				PopulateAccessibleControls();
	//				UpdateFlowsFromAndFlowsTo();
	//				UpdateUI(!m_commandBarFlyoutIsOpening);
	//			}
	//		});

	//		SecondaryCommands().VectorChanged({
	//			[this](var &, var &)

	//		{
	//#if _DEBUG
	//            COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR, METH_NAME, this, "SecondaryCommands VectorChanged");
	//#endif

	//				m_secondaryItemsRootSized = false;
	//				EnsureLocalizedControlTypes();
	//				PopulateAccessibleControls();
	//				UpdateFlowsFromAndFlowsTo();
	//				UpdateUI(!m_commandBarFlyoutIsOpening);
	//			}
	//		});
	//	}

	//	void OnApplyTemplate()
	//	{
	//		COMMANDBARFLYOUT_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

	//		__super.OnApplyTemplate();
	//		DetachEventHandlers();

	//		if (var overflowPopup = m_overflowPopup)
	//    {
	//			if (var overflowPopup4 = overflowPopup as IPopup4())
	//        {
	//				overflowPopup4.PlacementTarget(null);
	//				overflowPopup4.DesiredPlacement(PopupPlacementMode.Auto);
	//			}
	//		}

	//		AutomationProperties.SetLocalizedControlType(this, ResourceAccessor.GetLocalizedStringResource(SR_CommandBarFlyoutCommandBarLocalizedControlType));
	//		EnsureLocalizedControlTypes();

	//		IControlProtected thisAsControlProtected = this;

	//		m_primaryItemsRoot = (FrameworkElement)GetTemplateChild("PrimaryItemsRoot");
	//		m_overflowPopup = (Popup)GetTemplateChild("OverflowPopup");
	//		m_secondaryItemsRoot = (FrameworkElement)GetTemplateChild("OverflowContentRoot");
	//		m_moreButton = (ButtonBase)GetTemplateChild("MoreButton");
	//		m_openingStoryboard = [this, thisAsControlProtected](

	//		{
	//			if (var opacityStoryBoard = GetTemplateChildT<Storyboard>("OpeningOpacityStoryboard", thisAsControlProtected))
	//            {
	//				m_openAnimationKind = CommandBarFlyoutOpenCloseAnimationKind.Opacity;
	//				return opacityStoryBoard;
	//			}
	//			m_openAnimationKind = CommandBarFlyoutOpenCloseAnimationKind.Clip;
	//			return (Storyboard)GetTemplateChild("OpeningStoryboard");
	//		} ());
	//		m_closingStoryboard = [this, thisAsControlProtected](

	//		{
	//			if (var opacityStoryBoard = GetTemplateChildT<Storyboard>("ClosingOpacityStoryboard", thisAsControlProtected))
	//            {
	//				return opacityStoryBoard;
	//			}
	//			return (Storyboard)GetTemplateChild("ClosingStoryboard");
	//		} ());

	//		if (var overflowPopup = m_overflowPopup)
	//    {
	//			if (var overflowPopup4 = overflowPopup as IPopup4())
	//        {
	//				overflowPopup4.PlacementTarget(m_primaryItemsRoot);
	//				overflowPopup4.DesiredPlacement(PopupPlacementMode.BottomEdgeAlignedLeft);
	//			}
	//		}

	//		if (var moreButton = m_moreButton)
	//    {
	//			// Initially only the first focusable primary and secondary commands
	//			// keep their IsTabStop set to True.
	//			if (moreButton.IsTabStop())
	//			{
	//				moreButton.IsTabStop(false);
	//			}
	//		}

	//		if (SharedHelpers.Is21H1OrHigher() && m_owningFlyout)
	//		{
	//			AttachEventsToSecondaryStoryboards();
	//		}

	//		// Keep the owning FlyoutPresenter's corner radius in sync with the
	//		// primary commands's corner radius.
	//		if (SharedHelpers.IsRS5OrHigher())
	//		{
	//			BindOwningFlyoutPresenterToCornerRadius();
	//		}

	//		AttachEventHandlers();
	//		PopulateAccessibleControls();
	//		UpdateFlowsFromAndFlowsTo();
	//		UpdateUI(false /* useTransitions */);
	//		SetPresenterName(m_flyoutPresenter);
	//	}

	//	void SetOwningFlyout(
	//		CommandBarFlyout & owningFlyout)
	//	{
	//		m_owningFlyout = owningFlyout;
	//	}

	//	void AttachEventHandlers()
	//	{
	//		COMMANDBARFLYOUT_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

	//		if (var overflowPopup = m_overflowPopup)
	//    {
	//			if (var overflowPopup4 = overflowPopup as IPopup4())
	//        {
	//				m_overflowPopupActualPlacementChangedRevoker = overflowPopup4.ActualPlacementChanged(auto_revoke,

	//			{
	//					[this](var &, var &)

	//				{
	//#if _DEBUG
	//                    COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR, METH_NAME, this, "OverflowPopup ActualPlacementChanged");
	//#endif

	//						UpdateUI();
	//					}
	//				});
	//			}
	//		}

	//		if (var secondaryItemsRoot = m_secondaryItemsRoot)
	//    {
	//			m_secondaryItemsRootSizeChangedRevoker = secondaryItemsRoot.SizeChanged(auto_revoke,

	//		{
	//				[this](var &, var &)

	//			{
	//#if _DEBUG
	//                COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR, METH_NAME, this, "secondaryItemsRoot SizeChanged");
	//#endif

	//					m_secondaryItemsRootSized = true;
	//					UpdateUI(!m_commandBarFlyoutIsOpening, true /*isForSizeChange*/);
	//				}
	//			});

	//			if (SharedHelpers.IsRS3OrHigher())
	//			{
	//				m_secondaryItemsRootPreviewKeyDownRevoker = secondaryItemsRoot.PreviewKeyDown(auto_revoke,

	//			{
	//					[this](var &, KeyRoutedEventArgs & args)

	//				{
	//						COMMANDBARFLYOUT_TRACE_INFO(this, TRACE_MSG_METH_STR, METH_NAME, this,
	//							TypeLogging.KeyRoutedEventArgsToString(args).c_str());

	//						if (args.Handled)
	//						{
	//							return;
	//						}

	//						switch (args.Key())
	//						{
	//							case VirtualKey.Escape:
	//								{
	//									// In addition to closing the CommandBar if someone hits the escape key,
	//									// we also want to close the whole flyout.
	//									if (var owningFlyout = m_owningFlyout)
	//                        {
	//										owningFlyout.Hide();
	//									}
	//									break;
	//								}

	//							case VirtualKey.Down:
	//							case VirtualKey.Up:
	//								{
	//									OnKeyDown(args);
	//									break;
	//								}
	//						}
	//					}
	//				});
	//			}
	//			else
	//			{
	//				// Prior to RS3, UIElement.PreviewKeyDown is not available. Thus IsTabStop
	//				// for SecondaryCommands is left to True and the KeyDown event is used to
	//				// close the whole flyout with the escape key.
	//				// The custom Down / Up arrows handling above is skipped.
	//				m_keyDownRevoker = AddRoutedEventHandler<RoutedEventType.KeyDown>(
	//					secondaryItemsRoot as UIElement,

	//					[this](var &, var args)

	//				{
	//					if (var owningFlyout = m_owningFlyout)
	//                    {
	//						if (args.Key() == VirtualKey.Escape)
	//						{
	//							owningFlyout.Hide();
	//						}
	//					}
	//				},
	//                true /*handledEventsToo*/);
	//			}
	//		}

	//		if (m_openingStoryboard)
	//		{
	//			m_openingStoryboardCompletedRevoker =
	//				m_openingStoryboard.Completed(auto_revoke,

	//			{
	//				[this](var &, var &) { m_openingStoryboard.Stop(); }
	//			});
	//		}
	//	}

	//	void DetachEventHandlers()
	//	{
	//		COMMANDBARFLYOUT_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

	//		m_keyDownRevoker.revoke();
	//		m_secondaryItemsRootPreviewKeyDownRevoker.revoke();
	//		m_secondaryItemsRootSizeChangedRevoker.revoke();
	//		m_firstItemLoadedRevoker.revoke();
	//		m_openingStoryboardCompletedRevoker.revoke();
	//		m_closingStoryboardCompletedCallbackRevoker.revoke();
	//		m_expandedUpToCollapsedStoryboardRevoker.revoke();
	//		m_expandedDownToCollapsedStoryboardRevoker.revoke();
	//		m_collapsedToExpandedUpStoryboardRevoker.revoke();
	//		m_collapsedToExpandedDownStoryboardRevoker.revoke();
	//	}

	private bool HasOpenAnimation() => m_openingStoryboard is not null && SharedHelpers.IsAnimationsEnabled();

	private void PlayOpenAnimation()
	{
		if (m_closingStoryboard is { } closingStoryboard)
		{
			closingStoryboard.Stop();
		}

		if (m_openingStoryboard is { } openingStoryboard)
		{
			if (openingStoryboard.GetCurrentState() != ClockState.Active)
			{
				openingStoryboard.Begin();
			}
		}
	}

	private bool HasCloseAnimation() => m_closingStoryboard is not null && SharedHelpers.IsAnimationsEnabled();

	//	void PlayCloseAnimation(
	//		 weak_ref<CommandBarFlyout>& weakCommandBarFlyout,
	//		std.function<void()> onCompleteFunc)
	//	{
	//		COMMANDBARFLYOUT_TRACE_INFO(this, TRACE_MSG_METH, METH_NAME, this);

	//		if (var closingStoryboard = m_closingStoryboard)
	//    {
	//			if (closingStoryboard.GetCurrentState() != ClockState.Active)
	//			{
	//				m_closingStoryboardCompletedCallbackRevoker = closingStoryboard.Completed(auto_revoke,

	//			{
	//					[this, weakCommandBarFlyout, onCompleteFunc](var &, var &)

	//				{
	//						if (weakCommandBarFlyout)
	//						{
	//							onCompleteFunc();
	//						}
	//					}
	//				});

	//				UpdateTemplateSettings();
	//				closingStoryboard.Begin();
	//			}
	//		}

	//	else
	//		{
	//			onCompleteFunc();
	//		}
	//	}

	//	void UpdateFlowsFromAndFlowsTo()
	//	{
	//		var moreButton = m_moreButton;

	//		// Ensure there is only one focusable command with IsTabStop set to True
	//		// to enable tabbing from primary to secondary commands and vice-versa
	//		// with a single Tab keystroke.
	//		EnsureTabStopUniqueness(PrimaryCommands(), moreButton);
	//		if (SharedHelpers.IsRS3OrHigher())
	//		{
	//			EnsureTabStopUniqueness(SecondaryCommands(), null);
	//		}

	//		// Ensure the SizeOfSet and PositionInSet automation properties
	//		// for the primary commands and the MoreButton account for the
	//		// potential MoreButton.
	//		EnsureAutomationSetCountAndPosition();

	//		if (m_currentPrimaryItemsEndElement)
	//		{
	//			AutomationProperties.GetFlowsTo(m_currentPrimaryItemsEndElement).Clear();
	//			m_currentPrimaryItemsEndElement = null;
	//		}

	//		if (m_currentSecondaryItemsStartElement)
	//		{
	//			AutomationProperties.GetFlowsFrom(m_currentSecondaryItemsStartElement).Clear();
	//			m_currentSecondaryItemsStartElement = null;
	//		}

	//		// If we're not open, then we don't want to do anything special - the only time we do need to do something special
	//		// is when the secondary commands are showing, in which case we want to connect the primary and secondary command lists.
	//		if (IsOpen())
	//		{
	//			var isElementFocusable = [](ICommandBarElement & element, bool checkTabStop)

	//		{
	//				Control primaryCommandAsControl = element as Control;
	//				return IsControlFocusable(primaryCommandAsControl, checkTabStop);
	//			};

	//			var primaryCommands = PrimaryCommands();
	//			for (int i = (int)(primaryCommands.Size() - 1); i >= 0; i--)
	//			{
	//				var primaryCommand = primaryCommands[i];
	//				if (isElementFocusable(primaryCommand, false /*checkTabStop*/))
	//				{
	//					m_currentPrimaryItemsEndElement = primaryCommand as FrameworkElement;
	//					break;
	//				}
	//			}

	//			// If we have a more button and at least one focusable primary item, then
	//			// we'll use the more button as the last element in our primary items list.
	//			if (moreButton && moreButton.Visibility == Visibility.Visible && m_currentPrimaryItemsEndElement)
	//			{
	//				m_currentPrimaryItemsEndElement = moreButton;
	//			}

	//			for (var secondaryCommand : SecondaryCommands())
	//			{
	//				if (isElementFocusable(secondaryCommand, !SharedHelpers.IsRS3OrHigher() /*checkTabStop*/))
	//				{
	//					m_currentSecondaryItemsStartElement = secondaryCommand as FrameworkElement;
	//					break;
	//				}
	//			}

	//			if (m_currentPrimaryItemsEndElement && m_currentSecondaryItemsStartElement)
	//			{
	//				AutomationProperties.GetFlowsTo(m_currentPrimaryItemsEndElement).Add(m_currentSecondaryItemsStartElement);
	//				AutomationProperties.GetFlowsFrom(m_currentSecondaryItemsStartElement).Add(m_currentPrimaryItemsEndElement);
	//			}
	//		}
	//	}

	//	void UpdateUI(
	//		bool useTransitions, bool isForSizeChange)
	//	{
	//		COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_INT_INT, METH_NAME, this, useTransitions, isForSizeChange);

	//		UpdateTemplateSettings();
	//		UpdateVisualState(useTransitions, isForSizeChange);

	//		UpdateProjectedShadow();
	//	}

	//	void UpdateVisualState(
	//		bool useTransitions,
	//		bool isForSizeChange)
	//	{
	//		if (IsOpen())
	//		{
	//			// If we're currently open, have overflow items, and haven't yet sized our overflow item root,
	//			// then we want to wait until then to update visual state - otherwise, we'll be animating
	//			// to incorrect values.  Animations only retrieve values from bindings when they begin,
	//			// so if we begin an animation and then update a bound template setting, that won't take effect.
	//			if (!m_secondaryItemsRootSized)
	//			{
	//				return;
	//			}

	//			bool shouldExpandUp = false;
	//			bool hadActualPlacement = false;

	//			if (var overflowPopup = m_overflowPopup)
	//        {
	//				if (var overflowPopup4 = overflowPopup as IPopup4())
	//            {
	//					// If we have a value set for ActualPlacement, then we'll directly use that -
	//					// that tells us where our popup's been placed, so we don't need to try to
	//					// infer where it should go.
	//					if (overflowPopup4.ActualPlacement() != PopupPlacementMode.Auto)
	//					{
	//						hadActualPlacement = true;
	//						shouldExpandUp =
	//							overflowPopup4.ActualPlacement() == PopupPlacementMode.TopEdgeAlignedLeft ||
	//							overflowPopup4.ActualPlacement() == PopupPlacementMode.TopEdgeAlignedRight;
	//					}
	//				}
	//			}

	//			// If there isn't enough space to display the overflow below the command bar,
	//			// and if there is enough space above, then we'll display it above instead.
	//			if (var window = Window.Current() && !hadActualPlacement && m_secondaryItemsRoot)
	//        {
	//				double availableHeight = -1;
	//				var controlBounds = TransformToVisual(null).TransformBounds({ 0, 0, (float)(ActualWidth), (float)(ActualHeight) });

	//				try
	//				{
	//					// Note: this doesn't work right for islands scenarios
	//					// Bug 19617460: CommandBarFlyoutCommandBar isn't able to decide whether to open up or down because it doesn't know where it is relative to the monitor
	//					var view = ApplicationView.GetForCurrentView();
	//					availableHeight = view.VisibleBounds().Height;
	//				}
	//				catch (hresult_error)
	//				{
	//					// Calling GetForCurrentView on threads without a CoreWindow throws an error. This comes up in places like LogonUI.
	//					// In this circumstance, we'll just always expand down, since we can't get bounds information.
	//				}

	//				if (availableHeight >= 0)
	//				{
	//					m_secondaryItemsRoot.Measure({ std.numeric_limits<float>.infinity(), std.numeric_limits<float>.infinity() });
	//					var overflowPopupSize = m_secondaryItemsRoot.DesiredSize;

	//					shouldExpandUp =
	//						controlBounds.Y + controlBounds.Height + overflowPopupSize.Height > availableHeight &&
	//						controlBounds.Y - overflowPopupSize.Height >= 0;
	//				}
	//			}

	//			if (isForSizeChange)
	//			{
	//				// UpdateVisualState is called as a result of a size change (for instance caused by a secondary command bar element dependency property change). This CommandBarFlyoutCommandBar is already open
	//				// and expanded. Jump to the Collapsed and back to ExpandedUp/ExpandedDown state to apply all refreshed CommandBarFlyoutCommandBarTemplateSettings values.
	//				VisualStateManager.GoToState(this, "Collapsed", false);
	//			}

	//			VisualStateManager.GoToState(this, shouldExpandUp ? "ExpandedUp" : "ExpandedDown", useTransitions && !isForSizeChange);

	//			// Union of AvailableCommandsStates and ExpansionStates
	//			bool hasPrimaryCommands = (PrimaryCommands().Size() != 0.0;
	//			if (hasPrimaryCommands)
	//			{
	//				if (shouldExpandUp)
	//				{
	//					VisualStateManager.GoToState(this, "NoOuterOverflowContentRootShadow", useTransitions);
	//					VisualStateManager.GoToState(this, "ExpandedUpWithPrimaryCommands", useTransitions);
	//				}
	//				else
	//				{
	//					VisualStateManager.GoToState(this, "OuterOverflowContentRootShadow", useTransitions);
	//					VisualStateManager.GoToState(this, "ExpandedDownWithPrimaryCommands", useTransitions);
	//				}
	//			}
	//			else
	//			{
	//				VisualStateManager.GoToState(this, "OuterOverflowContentRootShadow", useTransitions);
	//				if (shouldExpandUp)
	//				{
	//					VisualStateManager.GoToState(this, "ExpandedUpWithoutPrimaryCommands", useTransitions);
	//				}
	//				else
	//				{
	//					VisualStateManager.GoToState(this, "ExpandedDownWithoutPrimaryCommands", useTransitions);
	//				}
	//			}
	//		}
	//		else
	//		{
	//			VisualStateManager.GoToState(this, "NoOuterOverflowContentRootShadow", useTransitions);
	//			VisualStateManager.GoToState(this, "Default", useTransitions);
	//			VisualStateManager.GoToState(this, "Collapsed", useTransitions);
	//		}
	//	}

	//	void UpdateTemplateSettings()
	//	{
	//		COMMANDBARFLYOUT_TRACE_INFO(this, TRACE_MSG_METH_INT, METH_NAME, this, IsOpen());

	//		if (m_primaryItemsRoot && m_secondaryItemsRoot)
	//		{
	//			var flyoutTemplateSettings = get_self<CommandBarFlyoutCommandBarTemplateSettings>(FlyoutTemplateSettings());
	//			var maxWidth = (float)(MaxWidth);

	//#if _DEBUG
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "old ExpandedWidth:", flyoutTemplateSettings.ExpandedWidth());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "old CurrentWidth:", flyoutTemplateSettings.CurrentWidth());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "old WidthExpansionDelta:", flyoutTemplateSettings.WidthExpansionDelta());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "old WidthExpansionAnimationStartPosition:", flyoutTemplateSettings.WidthExpansionAnimationStartPosition());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "old WidthExpansionAnimationEndPosition:", flyoutTemplateSettings.WidthExpansionAnimationEndPosition());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "old OpenAnimationStartPosition:", flyoutTemplateSettings.OpenAnimationStartPosition());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "old OpenAnimationEndPosition:", flyoutTemplateSettings.OpenAnimationEndPosition());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "old CloseAnimationEndPosition:", flyoutTemplateSettings.CloseAnimationEndPosition());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, "MaxWidth:", maxWidth);
	//#endif

	//			Size infiniteSize = { std.numeric_limits<float>.infinity(), std.numeric_limits<float>.infinity() };
	//			m_primaryItemsRoot.Measure(infiniteSize);
	//			Size primaryItemsRootDesiredSize = m_primaryItemsRoot.DesiredSize;
	//			float collapsedWidth = std.min(maxWidth, primaryItemsRootDesiredSize.Width);

	//			if (m_secondaryItemsRoot)
	//			{
	//				m_secondaryItemsRoot.Measure(infiniteSize);
	//				var overflowPopupSize = m_secondaryItemsRoot.DesiredSize;

	//				flyoutTemplateSettings.ExpandedWidth(std.min(maxWidth, std.max(collapsedWidth, overflowPopupSize.Width)));
	//				flyoutTemplateSettings.ExpandUpOverflowVerticalPosition(-overflowPopupSize.Height);
	//				flyoutTemplateSettings.ExpandUpAnimationStartPosition(overflowPopupSize.Height / 2);
	//				flyoutTemplateSettings.ExpandUpAnimationEndPosition(0.0;
	//				flyoutTemplateSettings.ExpandUpAnimationHoldPosition(overflowPopupSize.Height);
	//				flyoutTemplateSettings.ExpandDownAnimationStartPosition(-overflowPopupSize.Height / 2);
	//				flyoutTemplateSettings.ExpandDownAnimationEndPosition(0.0;
	//				flyoutTemplateSettings.ExpandDownAnimationHoldPosition(-overflowPopupSize.Height);
	//				// This clip needs to cover the border at the bottom of the overflow otherwise it'll 
	//				// clip the border. The measure size seems slightly off from what we eventually require
	//				// so we're going to compensate just a bit to make sure there's room for any borders.
	//				flyoutTemplateSettings.OverflowContentClipRect({ 0, 0, (float)(flyoutTemplateSettings.ExpandedWidth()), overflowPopupSize.Height + 2 });
	//			}
	//			else
	//			{
	//				flyoutTemplateSettings.ExpandedWidth(collapsedWidth);
	//				flyoutTemplateSettings.ExpandUpOverflowVerticalPosition(0.0;
	//				flyoutTemplateSettings.ExpandUpAnimationStartPosition(0.0;
	//				flyoutTemplateSettings.ExpandUpAnimationEndPosition(0.0;
	//				flyoutTemplateSettings.ExpandUpAnimationHoldPosition(0.0;
	//				flyoutTemplateSettings.ExpandDownAnimationStartPosition(0.0;
	//				flyoutTemplateSettings.ExpandDownAnimationEndPosition(0.0;
	//				flyoutTemplateSettings.ExpandDownAnimationHoldPosition(0.0;
	//				flyoutTemplateSettings.OverflowContentClipRect({ 0, 0, 0, 0 });
	//			}

	//			double expandedWidth = flyoutTemplateSettings.ExpandedWidth();

	//			// If collapsedWidth is 0, then we'll never be showing in collapsed mode,
	//			// so we'll set it equal to expandedWidth to ensure that our open/close animations are correct.
	//			if (collapsedWidth == 0)
	//			{
	//				collapsedWidth = (float)(expandedWidth);
	//			}

	//			flyoutTemplateSettings.WidthExpansionDelta(collapsedWidth - expandedWidth);
	//			flyoutTemplateSettings.WidthExpansionAnimationStartPosition(-flyoutTemplateSettings.WidthExpansionDelta() / 2.0.0;
	//			flyoutTemplateSettings.WidthExpansionAnimationEndPosition(-flyoutTemplateSettings.WidthExpansionDelta());
	//			flyoutTemplateSettings.ContentClipRect({ 0, 0, (float)(expandedWidth), primaryItemsRootDesiredSize.Height });

	//			if (IsOpen())
	//			{
	//				flyoutTemplateSettings.CurrentWidth(expandedWidth);
	//			}
	//			else
	//			{
	//				flyoutTemplateSettings.CurrentWidth(collapsedWidth);
	//			}

	//#if _DEBUG
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_FLT, METH_NAME, this, "collapsedWidth:", collapsedWidth);
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "new ExpandedWidth:", expandedWidth);
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "new CurrentWidth:", flyoutTemplateSettings.CurrentWidth());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "new WidthExpansionDelta:", flyoutTemplateSettings.WidthExpansionDelta());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "new WidthExpansionAnimationStartPosition:", flyoutTemplateSettings.WidthExpansionAnimationStartPosition());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "new WidthExpansionAnimationEndPosition:", flyoutTemplateSettings.WidthExpansionAnimationEndPosition());
	//#endif

	//			// If we're currently playing the close animation, don't update these properties -
	//			// the animation is expecting them not to change out from under it.
	//			// After the close animation has completed, the flyout will close and no further
	//			// visual updates will occur, so there's no need to update these values in that case.
	//			bool isPlayingCloseAnimation = false;

	//			if (var closingStoryboard = m_closingStoryboard)
	//        {
	//				isPlayingCloseAnimation = closingStoryboard.GetCurrentState() == ClockState.Active;
	//			}

	//			if (!isPlayingCloseAnimation)
	//			{
	//				if (IsOpen())
	//				{
	//					flyoutTemplateSettings.OpenAnimationStartPosition(-expandedWidth / 2);
	//					flyoutTemplateSettings.OpenAnimationEndPosition(0.0;
	//				}
	//				else
	//				{
	//					flyoutTemplateSettings.OpenAnimationStartPosition(flyoutTemplateSettings.WidthExpansionDelta() - collapsedWidth / 2);
	//					flyoutTemplateSettings.OpenAnimationEndPosition(flyoutTemplateSettings.WidthExpansionDelta());
	//				}

	//				flyoutTemplateSettings.CloseAnimationEndPosition(-expandedWidth);
	//			}

	//#if _DEBUG
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "new OpenAnimationStartPosition:", flyoutTemplateSettings.OpenAnimationStartPosition());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "new OpenAnimationEndPosition:", flyoutTemplateSettings.OpenAnimationEndPosition());
	//        COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH_STR_DBL, METH_NAME, this, "new CloseAnimationEndPosition:", flyoutTemplateSettings.CloseAnimationEndPosition());
	//#endif

	//			flyoutTemplateSettings.WidthExpansionMoreButtonAnimationStartPosition(flyoutTemplateSettings.WidthExpansionDelta() / 2);
	//			flyoutTemplateSettings.WidthExpansionMoreButtonAnimationEndPosition(flyoutTemplateSettings.WidthExpansionDelta());

	//			if (PrimaryCommands().Size() > 0)
	//			{
	//				flyoutTemplateSettings.ExpandDownOverflowVerticalPosition(Height);
	//			}
	//			else
	//			{
	//				flyoutTemplateSettings.ExpandDownOverflowVerticalPosition(0.0;
	//			}
	//		}
	//	}

	private void EnsureAutomationSetCountAndPosition()
	{
		//COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH, METH_NAME, this);

		var moreButton = m_moreButton;
		int sizeOfSet = 0;

		foreach (var command in PrimaryCommands)
		{
			if (command is UIElement commandAsUIElement)
			{
				// Don't count AppBarSeparator if IsTabStop is false
				if (commandAsUIElement is AppBarSeparator separator)
				{
					if (!separator.IsTabStop)
					{
						continue;
					}
				}
				else if (commandAsUIElement.Visibility == Visibility.Visible)
				{
					sizeOfSet++;
				}
			}
		}

		if (moreButton is not null && moreButton.Visibility == Visibility.Visible)
		{
			// Accounting for the MoreButton
			sizeOfSet++;
		}

		foreach (var command in PrimaryCommands)
		{
			if (command is UIElement commandAsUIElement)
			{
				// Don't count AppBarSeparator if IsTabStop is false
				if (commandAsUIElement is AppBarSeparator separator)
				{
					if (!separator.IsTabStop)
					{
						continue;
					}
				}
				else if (commandAsUIElement.Visibility == Visibility.Visible)
				{
					AutomationProperties.SetSizeOfSet(commandAsUIElement, sizeOfSet);
				}
			}
		}

		if (moreButton is not null && moreButton.Visibility == Visibility.Visible)
		{
			AutomationProperties.SetSizeOfSet(moreButton, sizeOfSet);
			AutomationProperties.SetPositionInSet(moreButton, sizeOfSet);
		}
	}

	private void EnsureLocalizedControlTypes()
	{
		//COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH, METH_NAME, this);

		foreach (var command in PrimaryCommands)
		{
			SetKnownCommandLocalizedControlTypes(command);
		}

		foreach (var command in SecondaryCommands)
		{
			SetKnownCommandLocalizedControlTypes(command);
		}
	}

	private void SetKnownCommandLocalizedControlTypes(ICommandBarElement command)
	{
		//COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH, METH_NAME, this);

		if (command is AppBarButton appBarButton)
	    {
			AutomationProperties.SetLocalizedControlType(appBarButton, ResourceAccessor.GetLocalizedStringResource(SR_CommandBarFlyoutAppBarButtonLocalizedControlType));
		}

		else if (command is AppBarToggleButton appBarToggleButton)
	    {
			AutomationProperties.SetLocalizedControlType(appBarToggleButton, ResourceAccessor.GetLocalizedStringResource(SR_CommandBarFlyoutAppBarToggleButtonLocalizedControlType));
		}
	}

	private void EnsureFocusedPrimaryCommand()
	{
		//COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH, METH_NAME, this);

		MUX_ASSERT(!SharedHelpers.IsRS3OrHigher());

		var moreButton = m_moreButton;
		var tabStopControl = GetFirstTabStopControl(PrimaryCommands);

		if (tabStopControl is null)
		{
			if (moreButton && moreButton.IsTabStop)
			{
				tabStopControl = moreButton;
			}
		}

		if (tabStopControl is not null)
		{
			if (tabStopControl.FocusState == FocusState.Unfocused)
			{
				FocusControl(
					tabStopControl /*newFocus*/,
					null /*oldFocus*/,
					FocusState.Programmatic /*focusState*/,
					false /*updateTabStop*/);
			}
		}
		else
		{
			FocusCommand(
				PrimaryCommands /*commands*/,
				moreButton /*moreButton*/,
				FocusState.Programmatic /*focusState*/,
				true /*firstCommand*/,
				true /*ensureTabStopUniqueness*/);
		}
	}

	//	void PopulateAccessibleControls()
	//	{
	//		COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH, METH_NAME, this);

	//		// The primary commands and the more button are the only controls accessible
	//		// using left and right arrow keys. All of the commands are accessible using
	//		// the up and down arrow keys.
	//		if (!m_horizontallyAccessibleControls)
	//		{
	//			MUX_ASSERT(!m_verticallyAccessibleControls);

	//			m_horizontallyAccessibleControls = new Vector<Control>();
	//			m_verticallyAccessibleControls = new Vector<Control>();
	//		}
	//		else
	//		{
	//			MUX_ASSERT(m_verticallyAccessibleControls);

	//			m_horizontallyAccessibleControls.Clear();
	//			m_verticallyAccessibleControls.Clear();
	//		}

	//		var primaryCommands = PrimaryCommands();

	//		for (ICommandBarElement & command : primaryCommands)
	//		{
	//			if (var commandAsControl = command as Control)
	//        {
	//			m_horizontallyAccessibleControls.Add(commandAsControl);
	//			m_verticallyAccessibleControls.Add(commandAsControl);
	//		}
	//	}

	//    if (var moreButton = m_moreButton)
	//    {
	//        if (primaryCommands.Size() > 0)
	//        {
	//            m_horizontallyAccessibleControls.Add(moreButton);
	//            m_verticallyAccessibleControls.Add(moreButton);
	//        }
	//    }

	//    for (ICommandBarElement & command : SecondaryCommands())
	//{
	//	if (var commandAsControl = command as Control)
	//        {
	//	m_verticallyAccessibleControls.Add(commandAsControl);
	//}
	//    }
	//}

	//void OnKeyDown(
	//	KeyRoutedEventArgs & args)
	//{
	//	COMMANDBARFLYOUT_TRACE_INFO(this, TRACE_MSG_METH_STR, METH_NAME, this,
	//		TypeLogging.KeyRoutedEventArgsToString(args).c_str());

	//	if (args.Handled)
	//	{
	//		return;
	//	}

	//	switch (args.Key())
	//	{
	//		case VirtualKey.Tab:
	//			{
	//				if (SecondaryCommands().Size() > 0 && !IsOpen())
	//				{
	//					// Ensure the secondary commands flyout is open ...
	//					IsOpen(true);

	//					// ... and focus the first focusable command
	//					FocusCommand(
	//						SecondaryCommands() /*commands*/,
	//						null /*moreButton*/,
	//						FocusState.Keyboard /*focusState*/,
	//						true /*firstCommand*/,
	//						SharedHelpers.IsRS3OrHigher() /*ensureTabStopUniqueness*/);
	//				}
	//				break;
	//			}

	//		case VirtualKey.Escape:
	//			{
	//				if (var owningFlyout = m_owningFlyout)
	//        {
	//	owningFlyout.Hide();
	//	args.Handled = true;
	//}
	//break;
	//    }

	//    case VirtualKey.Right:
	//    case VirtualKey.Left:
	//    case VirtualKey.Down:
	//    case VirtualKey.Up:
	//    {
	//	bool isRightToLeft = m_primaryItemsRoot && m_primaryItemsRoot.FlowDirection() == FlowDirection.RightToLeft;
	//	bool isLeft = (args.Key() == VirtualKey.Left && !isRightToLeft) || (args.Key() == VirtualKey.Right && isRightToLeft);
	//	bool isRight = (args.Key() == VirtualKey.Right && !isRightToLeft) || (args.Key() == VirtualKey.Left && isRightToLeft);
	//	bool isDown = args.Key() == VirtualKey.Down;
	//	bool isUp = args.Key() == VirtualKey.Up;

	//	// To avoid code duplication, we'll use the key directionality to determine
	//	// both which control list to use and in which direction to iterate through
	//	// it to find the next control to focus.  Then we'll do that iteration
	//	// to focus the next control.
	//	var accessibleControls{ isUp || isDown ? m_verticallyAccessibleControls : m_horizontallyAccessibleControls };
	//	int startIndex = isLeft || isUp ? accessibleControls.Size() - 1 : 0;
	//	int endIndex = isLeft || isUp ? -1 : accessibleControls.Size();
	//	int deltaIndex = isLeft || isUp ? -1 : 1;
	//	bool shouldLoop = isUp || isDown;
	//	Control focusedControl{ null };
	//	int focusedControlIndex = -1;

	//	for (int i = startIndex;
	//		// We'll stop looping at the end index unless we're looping,
	//		// in which case we want to wrap back around to the start index.
	//		(i != endIndex || shouldLoop) ||
	//		// If we found a focused control but have looped all the way back around,
	//		// then there wasn't another control to focus, so we should quit.
	//		focusedControlIndex > 0 && i == focusedControlIndex;
	//		i += deltaIndex)
	//	{
	//		// If we've reached the end index, that means we want to loop.
	//		// We'll wrap around to the start index.
	//		if (i == endIndex)
	//		{
	//			MUX_ASSERT(shouldLoop);

	//			if (focusedControl)
	//			{
	//				i = startIndex;
	//			}
	//			else
	//			{
	//				// If no focused control was found after going through the entire list of controls,
	//				// then we have nowhere for focus to go.  Let's early-out in that case.
	//				break;
	//			}
	//		}

	//		var control = accessibleControls[i];

	//		// If we've yet to find the focused control, we'll keep looking for it.
	//		// Otherwise, we'll try to focus the next control after it.
	//		if (!focusedControl)
	//		{
	//			if (control.FocusState() != FocusState.Unfocused)
	//			{
	//				focusedControl = control;
	//				focusedControlIndex = i;
	//			}
	//		}
	//		else if (IsControlFocusable(control, false /*checkTabStop*/))
	//		{
	//			// If the control we're trying to focus is in the secondary command list,
	//			// then we'll make sure that that list is open before trying to focus the control.
	//			if (var controlAsCommandBarElement = control as ICommandBarElement)
	//                {
	//		uint index = 0;
	//		if (SecondaryCommands().IndexOf(controlAsCommandBarElement, index) && !IsOpen())
	//		{
	//			IsOpen(true);
	//		}
	//	}

	//	if (FocusControl(
	//		accessibleControls[i] /*newFocus*/,
	//		focusedControl /*oldFocus*/,
	//		FocusState.Keyboard /*focusState*/,
	//		true /*updateTabStop*/))
	//	{
	//		args.Handled = true;
	//		break;
	//	}
	//}
	//        }

	//        if (!args.Handled)
	//{
	//	// Occurs for example with Right key while MoreButton has focus. Stay on that MoreButton.
	//	args.Handled = true;
	//}
	//break;
	//    }
	//    }

	//    __super.OnKeyDown(args);
	//}

	private bool IsControlFocusable(Control control, bool checkTabStop) =>
			control is not null &&
			control.Visibility == Visibility.Visible &&
			(control.IsEnabled || control.AllowFocusWhenDisabled()) &&
			control.IsTabStop() || (!checkTabStop && !control as AppBarSeparator); // AppBarSeparator is not focusable if IsTabStop is false

	private Control? GetFirstTabStopControl(IObservableVector<ICommandBarElement> commands)
	{
		foreach (var command in commands)
		{
			if (command is Control commandAsControl)
			{
				if (commandAsControl.IsTabStop)
				{
					return commandAsControl;
				}
			}
		}
		return null;
	}

	private bool FocusControl(
		Control newFocus,
		Control oldFocus,
		FocusState focusState,
		bool updateTabStop)
	{
		MUX_ASSERT(newFocus);

		if (updateTabStop)
		{
			newFocus.IsTabStop = true;
		}

		if (newFocus.Focus(focusState))
		{
			if (oldFocus is not null && updateTabStop is not null)
			{
				oldFocus.IsTabStop = false;
			}
			return true;
		}
		return false;
	}

	//bool FocusCommand(
	//	IObservableVector<ICommandBarElement> & commands,
	//	Control & moreButton,
	//	FocusState & focusState,
	//	bool firstCommand,
	//	bool ensureTabStopUniqueness)
	//{
	//	COMMANDBARFLYOUT_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, null);

	//	MUX_ASSERT(commands);

	//	// Give focus to the first or last focusable command
	//	Control focusedControl = null;
	//	int startIndex = 0;
	//	int endIndex = (int)(commands.Size());
	//	int deltaIndex = 1;

	//	if (!firstCommand)
	//	{
	//		deltaIndex = -1;
	//		startIndex = endIndex - 1;
	//		endIndex = -1;
	//	}

	//	for (int index = startIndex; index != endIndex; index += deltaIndex)
	//	{
	//		var command = commands[index];

	//		if (command is Control commandAsControl)
	//		{
	//			if (IsControlFocusable(commandAsControl, !ensureTabStopUniqueness /*checkTabStop*/))
	//			{
	//				if (!focusedControl)
	//				{
	//					if (FocusControl(
	//							commandAsControl /*newFocus*/,
	//							null /*oldFocus*/,
	//							focusState /*focusState*/,
	//							ensureTabStopUniqueness /*updateTabStop*/))
	//					{
	//						if (ensureTabStopUniqueness && moreButton && moreButton.IsTabStop())
	//						{
	//							moreButton.IsTabStop(false);
	//						}

	//						focusedControl = commandAsControl;

	//						if (!ensureTabStopUniqueness)
	//						{
	//							break;
	//						}
	//					}
	//				}
	//				else if (focusedControl && commandAsControl.IsTabStop())
	//				{
	//					commandAsControl.IsTabStop(false);
	//				}
	//			}
	//		}
	//	}

	//	return focusedControl != null;
	//}

	private void EnsureTabStopUniqueness(
		IObservableVector<ICommandBarElement> commands,
		Control moreButton)
	{
		//COMMANDBARFLYOUT_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, null);

		MUX_ASSERT(commands);

		bool tabStopSeen = moreButton is not null && moreButton.IsTabStop;

		if (tabStopSeen || GetFirstTabStopControl(commands))
		{
			// Make sure only one command or the MoreButton has IsTabStop set
			foreach (var command in commands)
			{
				if (command is Control commandAsControl)
				{
					if (IsControlFocusable(commandAsControl, false /*checkTabStop*/) && commandAsControl.IsTabStop)
					{
						if (!tabStopSeen)
						{
							tabStopSeen = true;
						}
						else
						{
							commandAsControl.IsTabStop = false;
						}
					}
				}
			}
		}

		else
		{
			// Set IsTabStop to first focusable command
			foreach (var command in commands)
			{
				if (command is Control commandAsControl)
				{
					if (IsControlFocusable(commandAsControl, false /*checkTabStop*/))
					{
						commandAsControl.IsTabStop = true;
						break;
					}
				}
			}
		}
	}

	private void UpdateProjectedShadow()
	{
		if (SharedHelpers.IsThemeShadowAvailable() && !SharedHelpers.Is21H1OrHigher())
		{
			if (PrimaryCommands.Count > 0)
			{
				AddProjectedShadow();
			}
			else if (PrimaryCommands.Count == 0)
			{
				ClearProjectedShadow();
			}
		}
	}

	private void AddProjectedShadow()
	{
		//This logic applies to projected shadows, which are the default on < 21H1.
		//See additional notes in CommandBarFlyout.CreatePresenter().
		//Apply Shadow on the Grid named "ContentRoot", this is the first element below
		//the clip animation of the commandBar. This guarantees that shadow respects the 
		//animation
		var grid = (Grid)GetTemplateChild("ContentRoot");

		if (grid is not null)
		{
			if (grid.Shadow is null)
			{
				Windows.UI.Xaml.Media.ThemeShadow shadow = new();
				grid.Shadow = shadow;

				var translation = new Vector3(grid.Translation.X, grid.Translation.Y, 32.0f);
				grid.Translation = translation;
			}
		}
	}

	private void ClearProjectedShadow()
	{
		// This logic applies to projected shadows, which are the default on < 21H1.
		// See additional notes in CommandBarFlyout.CreatePresenter().
		var grid = (Grid)GetTemplateChild("ContentRoot");
		if (grid is not null)
		{
			if (grid.Shadow is not null)
			{
				grid.Shadow = null;

				//Undo the elevation
				var translation = new Vector3(grid.Translation.X, grid.Translation.Y, 0.0f);
				grid.Translation = translation;
			}
		}
	}

	private void ClearShadow()
	{
		if (SharedHelpers.IsThemeShadowAvailable() && !SharedHelpers.Is21H1OrHigher())
		{
			ClearProjectedShadow();
		}
		VisualStateManager.GoToState(this, "NoOuterOverflowContentRootShadow", true/*useTransitions*/);
	}


	private void SetPresenter(FlyoutPresenter presenter)
	{
		m_flyoutPresenter = WeakReferencePool.RentWeakReference(this, presenter);
	}

	private void SetPresenterName(FlyoutPresenter presenter)
	{
		// Since DropShadows don't play well with clip entrance animations for the presenter,
		// we'll need to fade it in. This name helps us locate the element to set the fade in
		// flag in the OS code.
		if (presenter is not null)
		{
			if (OpenAnimationKind == CommandBarFlyoutOpenCloseAnimationKind.Clip)
			{
				presenter.Name("DropShadowFadeInTarget");
			}
			else
			{
				presenter.Name("");
			}
		}
	}

	private bool HasSecondaryOpenCloseAnimations() =>
		SharedHelpers.IsAnimationsEnabled() &&
		(bool)(m_expandedDownToCollapsedStoryboardRevoker ||
		m_expandedUpToCollapsedStoryboardRevoker ||
		m_collapsedToExpandedUpStoryboardRevoker ||
		m_collapsedToExpandedDownStoryboardRevoker);

	//void AttachEventsToSecondaryStoryboards()
	//{
	//	IControlProtected thisAsControlProtected = this;

	//	var addDropShadowFunc = [this](var &, var &)

	//	{
	//		if (SharedHelpers.IsAnimationsEnabled())
	//		{
	//			if (var owningFlyout = m_owningFlyout)
	//            {
	//	if (var actualFlyout = get_self<CommandBarFlyout>(owningFlyout))
	//                {
	//		actualFlyout.AddDropShadow();
	//	}
	//}
	//        }
	//    };

	//if (var expandedDownToCollapsed = GetTemplateChildT<Storyboard>("ExpandedDownToCollapsed", thisAsControlProtected))
	//    {
	//	m_expandedDownToCollapsedStoryboardRevoker = expandedDownToCollapsed.Completed(auto_revoke, addDropShadowFunc);
	//}

	//if (var expandedUpToCollapsed = GetTemplateChildT<Storyboard>("ExpandedUpToCollapsed", thisAsControlProtected))
	//    {
	//	m_expandedUpToCollapsedStoryboardRevoker = expandedUpToCollapsed.Completed(auto_revoke, addDropShadowFunc);
	//}

	//if (var collapsedToExpandedUp = GetTemplateChildT<Storyboard>("CollapsedToExpandedUp", thisAsControlProtected))
	//    {
	//	m_collapsedToExpandedUpStoryboardRevoker = collapsedToExpandedUp.Completed(auto_revoke, addDropShadowFunc);
	//}

	//if (var collapsedToExpandedDown = GetTemplateChildT<Storyboard>("CollapsedToExpandedDown", thisAsControlProtected))
	//    {
	//	m_collapsedToExpandedDownStoryboardRevoker = collapsedToExpandedDown.Completed(auto_revoke, addDropShadowFunc);
	//}
	//}

	private void BindOwningFlyoutPresenterToCornerRadius()
	{
		if (m_owningFlyout is { } actualFlyout)
		{
			if (GetTemplateChild<Grid>("LayoutRoot") is { } root)
			{
				Binding binding = new();
				binding.Source = root;
				binding.Path = new PropertyPath("CornerRadius");
				binding.Mode = BindingMode.OneWay;
				if (actualFlyout.GetPresenter() is { } presenter)
				{
					presenter.SetBinding(Control.CornerRadiusProperty, binding);
				}
			}
		}
	}

	// Invoked by CommandBarFlyout when a secondary AppBarButton or AppBarToggleButton dependency property changed.
	private void OnCommandBarElementDependencyPropertyChanged()
	{
		//COMMANDBARFLYOUT_TRACE_VERBOSE(this, TRACE_MSG_METH, METH_NAME, this);

		// Only refresh the UI when the CommandBarFlyoutCommandBar is already open since it will be refreshed anyways in the event it gets opened.
		if (IsOpen)
		{
			UpdateUI(!m_commandBarFlyoutIsOpening, true /*isForSizeChange*/);
		}
	}
}
