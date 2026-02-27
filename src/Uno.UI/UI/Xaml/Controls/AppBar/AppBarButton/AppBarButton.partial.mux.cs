// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AppBarButton_Partial.cpp, tag winui3/release/1.7.1, commit 5f27a786ac96c

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DirectUI;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Animation;
using static Microsoft.UI.Xaml.Controls._Tracing;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Input;
using Uno.UI.Extensions;
using Popup = Microsoft.UI.Xaml.Controls.Primitives.Popup;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBarButton : ICommandBarElement2, ICommandBarElement3, ICommandBarOverflowElement, ICommandBarLabeledElement, ISubMenuOwner
{
	/// <summary>
	/// Initializes a new instance of the AppBarButton class.
	/// </summary>
	public AppBarButton()
	{
		m_isWithToggleButtons = false;
		m_isWithIcons = false;
		m_inputDeviceTypeUsedToOpenOverflow = InputDeviceType.None;
		m_isTemplateApplied = false;
		m_ownsToolTip = true;

		DefaultStyleKey = typeof(AppBarButton);
	}

	internal void SetOverflowStyleParams(bool hasIcons, bool hasToggleButtons, bool hasKeyboardAcceleratorText)
	{
		bool updateState = false;

		if (m_isWithIcons != hasIcons)
		{
			m_isWithIcons = hasIcons;
			updateState = true;
		}
		if (m_isWithToggleButtons != hasToggleButtons)
		{
			m_isWithToggleButtons = hasToggleButtons;
			updateState = true;
		}
		if (m_isWithKeyboardAcceleratorText != hasKeyboardAcceleratorText)
		{
			m_isWithKeyboardAcceleratorText = hasKeyboardAcceleratorText;
			updateState = true;
		}
		if (updateState)
		{
			UpdateVisualState();
		}
	}

	void ICommandBarLabeledElement.SetDefaultLabelPosition(CommandBarDefaultLabelPosition defaultLabelPosition)
	{
		if (m_defaultLabelPosition != defaultLabelPosition)
		{
			m_defaultLabelPosition = defaultLabelPosition;
			UpdateInternalStyles();
			UpdateVisualState();
		}
	}

	bool ICommandBarLabeledElement.GetHasBottomLabel() =>
		GetHasLabelInPosition(CommandBarDefaultLabelPosition.Bottom);

	bool ICommandBarLabeledElement.GetHasRightLabel() =>
		GetHasLabelInPosition(CommandBarDefaultLabelPosition.Right);

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		base.OnPointerEntered(args);

		bool isInOverflow = false;
		isInOverflow = IsInOverflow;

		if (isInOverflow && m_menuHelper is { })
		{
			m_menuHelper.OnPointerEntered(args);
		}

		AppBarButtonHelpers<AppBarButton>.CloseSubMenusOnPointerEntered(this, this);
	}

	protected override void OnPointerExited(PointerRoutedEventArgs e)
	{
		base.OnPointerExited(e);

		bool isInOverflow = IsInOverflow;

		if (isInOverflow && m_menuHelper is { })
		{
			m_menuHelper.OnPointerExited(e, parentIsSubMenu: false);
		}
	}

	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		base.OnKeyDown(args);

		bool isInOverflow = IsInOverflow;

		if (isInOverflow && m_menuHelper is { })
		{
			m_menuHelper.OnKeyDown(args);
		}
	}

	protected override void OnKeyUp(KeyRoutedEventArgs args)
	{
		base.OnKeyUp(args);

		bool isInOverflow = IsInOverflow;

		if (isInOverflow && m_menuHelper is { })
		{
			m_menuHelper.OnKeyUp(args);
		}
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == FlyoutProperty)
		{
			var oldFlyout = args.OldValue as FlyoutBase;
			var newFlyout = args.NewValue as FlyoutBase;

			if (oldFlyout is { })
			{
				m_flyoutOpenedHandler.Disposable = null;
				m_flyoutClosedHandler.Disposable = null;

				m_menuHelper = null;
			}

			if (newFlyout is { })
			{
				AttachFlyout(newFlyout);
			}
		}

		AppBarButtonHelpers<AppBarButton>.OnPropertyChanged(this, args);
	}

	// Uno specific: To allow re-attaching after Unloaded, the logic was moved to separate method.
	private void AttachFlyout(FlyoutBase newFlyout)
	{
		m_menuHelper = new CascadingMenuHelper();
		m_menuHelper.Initialize(this);

		ManagedWeakReference wrThis = WeakReferencePool.RentSelfWeakReference(this);

		void FlyoutOpenStateChangedHandler(bool isOpen)
		{
			if (wrThis.TryGetTarget<AppBarButton>(out var appBarButton))
			{
				appBarButton.m_isFlyoutClosing = false;
				appBarButton.UpdateVisualState();
			}
		}

		void OpenedHandler(object? sender, object e)
		{
			FlyoutOpenStateChangedHandler(true);
		}

		void ClosedHandler(object? sender, object e)
		{
			FlyoutOpenStateChangedHandler(false);
		}

		newFlyout.Opened += OpenedHandler;
		m_flyoutOpenedHandler.Disposable = Disposable.Create(() => newFlyout.Opened -= OpenedHandler);

		newFlyout.Closed += ClosedHandler;
		m_flyoutClosedHandler.Disposable = Disposable.Create(() => newFlyout.Closed -= ClosedHandler);
	}

	// After template is applied, set the initial view state
	// (FullSize or Compact) based on the value of our
	// IsCompact property
	protected override void OnApplyTemplate()
	{
		AppBarButtonHelpers<AppBarButton>.OnBeforeApplyTemplate(this);
		base.OnApplyTemplate();
		AppBarButtonHelpers<AppBarButton>.OnApplyTemplate(this);

#if HAS_UNO // Uno: Until ContentPresenter supports auto-fallback.
		SetupContentUpdate();
#endif
	}

	// Sets the visual state to "Compact" or "FullSize" based on the value
	// of our IsCompact property.
	private protected override void ChangeVisualState(bool useTransitions)
	{
		bool useOverflowStyle = false;

		// var endOnExit = DXamlCore.Current.GetHandle().GetThemeWalkResourceCache().BeginCachingThemeResources(); TODO:Uno: enable in issue #19381

		base.ChangeVisualState(useTransitions);
		useOverflowStyle = UseOverflowStyle;

		if (useOverflowStyle)
		{
			if (m_isWithToggleButtons && m_isWithIcons)
			{
				GoToState(useTransitions, "OverflowWithToggleButtonsAndMenuIcons");
			}
			else if (m_isWithToggleButtons)
			{
				GoToState(useTransitions, "OverflowWithToggleButtons");
			}
			else if (m_isWithIcons)
			{
				GoToState(useTransitions, "OverflowWithMenuIcons");
			}
			else
			{
				GoToState(useTransitions, "Overflow");
			}

			if (m_isWithIcons)
			{
				bool isEnabled = false;
				bool isPressed = false;
				bool isPointerOver = false;
				bool isSubMenuOpen = false;

				isEnabled = IsEnabled;
				isPressed = IsPressed;
				isPointerOver = IsPointerOver;
				isSubMenuOpen = ((ISubMenuOwner)this).IsSubMenuOpen;

				if (isSubMenuOpen && !m_isFlyoutClosing)
				{
					GoToState(useTransitions, "OverflowSubMenuOpened");
				}
				else if (isPressed)
				{
					GoToState(useTransitions, "OverflowPressed");
				}
				else if (isPointerOver)
				{
					GoToState(useTransitions, "OverflowPointerOver");
				}
				else if (isEnabled)
				{
					GoToState(useTransitions, "OverflowNormal");
				}
			}
		}

		var flyout = Flyout;

		if (flyout is { })
		{
			GoToState(useTransitions, "HasFlyout");
		}
		else
		{
			GoToState(useTransitions, "NoFlyout");
		}


		AppBarButtonHelpers<AppBarButton>.ChangeCommonVisualStates(this, useTransitions);
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new AppBarButtonAutomationPeer(this);

	private protected override void OnClick()
	{
		// Don't execute the logic on CommandBar to close the secondary
		// commands popup when we have a flyout associated with this button.
		var spFlyoutBase = Flyout;

		if (spFlyoutBase is null)
		{
			CommandBar.OnCommandExecutionStatic(this);
		}

		base.OnClick();
	}

	private protected override void OnVisibilityChanged()
	{
		base.OnVisibilityChanged();
		CommandBar.OnCommandBarElementVisibilityChanged(this);
	}

	private protected override void OnCommandChanged(object oldValue, object newValue)
	{
		base.OnCommandChanged(oldValue, newValue);
		AppBarButtonHelpers<AppBarButton>.OnCommandChanged(this, oldValue, newValue);
	}

	private protected override void OpenAssociatedFlyout()
	{
		bool isInOverflow = IsInOverflow;

		// If we call OpenSubMenu, that causes the menu not to have a light-dismiss layer, as it assumes
		// that its parent will have one.  That's not the case for AppBarButtons that aren't in overflow,
		// so we'll just open the flyout normally in this circumstance.
		if (m_menuHelper is { } && isInOverflow)
		{
			m_menuHelper.OpenSubMenu();
		}
		else
		{
			base.OpenAssociatedFlyout();
		}
	}

	private bool GetHasLabelInPosition(CommandBarDefaultLabelPosition labelPosition)
	{
		var effectiveLabelPosition = GetEffectiveLabelPosition();

		if (effectiveLabelPosition != labelPosition)
		{
			return false;
		}

		return Label != null;
	}

	private CommandBarDefaultLabelPosition GetEffectiveLabelPosition()
	{
		var labelPosition = LabelPosition;

		return labelPosition == CommandBarLabelPosition.Collapsed ? CommandBarDefaultLabelPosition.Collapsed : m_defaultLabelPosition;
	}

	private void UpdateInternalStyles()
	{
		// If the template isn't applied yet, we'll early-out,
		// because we won't have the style to apply from the
		// template yet.
		if (!m_isTemplateApplied)
		{
			return;
		}

		var effectiveLabelPosition = GetEffectiveLabelPosition();
		var useOverflowStyle = UseOverflowStyle;

		bool shouldHaveLabelOnRightStyleSet = effectiveLabelPosition == CommandBarDefaultLabelPosition.Right && !useOverflowStyle;

		// Apply/UnApply auto width animation if needed
		// only play auto width animation when the width is not overrided by local/animation setting
		// and when LabelOnRightStyle is not set. LabelOnRightStyle take high priority than animation.
		if (shouldHaveLabelOnRightStyleSet
			&& !this.HasLocalOrModifierValue(WidthProperty))
		{
			// Apply our width adjustments using a storyboard so that we don't stomp over template or user
			// provided values.  When we stop the storyboard, it will restore the previous values.
			if (m_widthAdjustmentsForLabelOnRightStyleStoryboard == null)
			{
				var storyboard = CreateStoryboardForWidthAdjustmentsForLabelOnRightStyle();
				m_widthAdjustmentsForLabelOnRightStyleStoryboard = storyboard;
			}

			StartAnimationForWidthAdjustments();
		}
		else if (!shouldHaveLabelOnRightStyleSet && m_widthAdjustmentsForLabelOnRightStyleStoryboard is { })
		{
			StopAnimationForWidthAdjustments();
		}

		AppBarButtonHelpers<AppBarButton>.UpdateToolTip(this);
	}

	private Storyboard CreateStoryboardForWidthAdjustmentsForLabelOnRightStyle()
	{
		var storyboardLocal = new Storyboard();

		var objectAnimation = new ObjectAnimationUsingKeyFrames();

		Storyboard.SetTarget(objectAnimation, this);
		Storyboard.SetTargetProperty(objectAnimation, nameof(Width));

		var discreteObjectKeyFrame = new DiscreteObjectKeyFrame();

		var keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

		discreteObjectKeyFrame.KeyTime = keyTime;
		discreteObjectKeyFrame.Value = double.NaN;

		objectAnimation.KeyFrames.Add(discreteObjectKeyFrame);
		storyboardLocal.Children.Add(objectAnimation);

		return storyboardLocal;
	}

	private void StartAnimationForWidthAdjustments()
	{
		if (m_widthAdjustmentsForLabelOnRightStyleStoryboard is { })
		{
			StopAnimationForWidthAdjustments();
			m_widthAdjustmentsForLabelOnRightStyleStoryboard.Begin();
			m_widthAdjustmentsForLabelOnRightStyleStoryboard.SkipToFill();
		}
	}

	private void StopAnimationForWidthAdjustments()
	{
		if (m_widthAdjustmentsForLabelOnRightStyleStoryboard is { })
		{
			ClockState currentState = m_widthAdjustmentsForLabelOnRightStyleStoryboard.GetCurrentState();
			if (currentState == ClockState.Active
				|| currentState == ClockState.Filling)
			{
				m_widthAdjustmentsForLabelOnRightStyleStoryboard.Stop();
			}
		}
	}

	bool ISubMenuOwner.IsSubMenuOpen
	{
		get
		{
			var flyout = Flyout;
			if (flyout is { })
			{
				return flyout.IsOpen;
			}

			return false;
		}
	}

	ISubMenuOwner? ISubMenuOwner.ParentOwner
	{
		get => null;
		set => throw new NotImplementedException();
	}

	void ISubMenuOwner.SetSubMenuDirection(bool isSubMenuDirectionUp) { }

	void ISubMenuOwner.PrepareSubMenu()
	{
		var flyout = Flyout;

		if (flyout is null)
		{
			return;
		}

		if (XamlRoot.GetImplementationForElement(flyout) is { } xamlRoot)
		{
			var rootVisual = xamlRoot.Content;
			flyout.OverlayInputPassThroughElement = rootVisual;
		}

		if (flyout is IMenu flyoutAsMenu)
		{
			var parentCommandBar = CommandBar.FindParentCommandBarForElement(this);

			if (parentCommandBar is { })
			{
				flyoutAsMenu.ParentMenu = parentCommandBar;
			}
		}
	}

	void ISubMenuOwner.OpenSubMenu(Point position)
	{
		var flyout = Flyout;
		if (flyout is { })
		{
			var showOptions = new FlyoutShowOptions();

			var isInOverflow = IsInOverflow;

			// We shouldn't be doing anything special to open flyouts for AppBarButtons
			// that are not in overflow.
			MUX_ASSERT(isInOverflow);

			showOptions.Placement = FlyoutPlacementMode.RightEdgeAlignedTop;

			// In order to avoid the sub-menu from showing on top of the menu, the FlyoutShowOptions.ExclusionRect property is set to the width
			// of the AppBarButton minus the small overlap on the left & right edges. The exclusion height is set to the height of the button.
			var itemWidth = ActualWidth;
			var itemHeight = ActualHeight;

			var overlap = (float)itemWidth - position.X;
			Rect exclusionRect = new(
				overlap,
				0,
				position.X - overlap,
				(float)itemHeight);

			showOptions.ExclusionRect = exclusionRect;

			showOptions.Position = position;

			flyout.ShowAt(this, showOptions);

			var flyoutPresenterNoRef = flyout.GetPresenter() as Control;

			MUX_ASSERT(m_menuHelper is not null);
			m_menuHelper!.SetSubMenuPresenter(flyoutPresenterNoRef);
		}

		m_lastFlyoutPosition = position;
	}

	void ISubMenuOwner.PositionSubMenu(Point position)
	{
		((ISubMenuOwner)this).CloseSubMenu();

		if (double.IsNegativeInfinity(position.X))
		{
			position.X = m_lastFlyoutPosition.X;
		}

		if (double.IsNegativeInfinity(position.Y))
		{
			position.Y = m_lastFlyoutPosition.Y;
		}

		((ISubMenuOwner)this).OpenSubMenu(position);
	}

	void ISubMenuOwner.ClosePeerSubMenus()
	{
		var parentCommandBar = CommandBar.FindParentCommandBarForElement(this);

		if (parentCommandBar is { })
		{
			parentCommandBar.CloseSubMenus(this);
		}
	}

	void ISubMenuOwner.CloseSubMenu()
	{
		var flyout = Flyout;

		if (flyout is not null)
		{
			// If the sub-menu flyout has the current keyboard focus, then we'll
			// move keyboard focus to this button in order to avoid having a period
			// where XAML keyboard focus is nowhere.
			var focusedElement = this.GetFocusedElement();

			if (focusedElement != null)
			{
				var focusedElementCoreAsUIE = focusedElement as UIElement;
				if (focusedElementCoreAsUIE != null &&
					Popup.GetClosestFlyoutAncestor(focusedElementCoreAsUIE) == flyout)
				{
					Focus(FocusState.Programmatic);
				}
			}

			flyout.Hide();

			// The Closing event is raised after the fade-out animation completes,
			// whereas we want to stop showing the sub-menu open state as soon
			// as we know we're moving out of it.  So we'll manually update the
			// visual state here.
			m_isFlyoutClosing = true;
			UpdateVisualState();
		}
	}

	void ISubMenuOwner.CloseSubMenuTree()
	{
		if (m_menuHelper is { })
		{
			m_menuHelper.CloseChildSubMenus();
		}
	}
	void ISubMenuOwner.DelayCloseSubMenu()
	{
		if (m_menuHelper is { })
		{
			m_menuHelper.DelayCloseSubMenu();
		}
	}

	void ISubMenuOwner.CancelCloseSubMenu()
	{
		if (m_menuHelper is { })
		{
			m_menuHelper.CancelCloseSubMenu();
		}
	}

	void ISubMenuOwner.RaiseAutomationPeerExpandCollapse(bool isOpen)
	{
		if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
		{
			var spAutomationPeer = GetOrCreateAutomationPeer();
			if (spAutomationPeer is AppBarButtonAutomationPeer appBarButtonAutomationPeer)
			{
				appBarButtonAutomationPeer.RaiseExpandCollapseAutomationEvent(isOpen);
			}
		}
	}

	void IAppBarButtonHelpersProvider.GoToState(bool useTransitions, string stateName) => GoToState(useTransitions, stateName);

	void IAppBarButtonHelpersProvider.UpdateInternalStyles() => UpdateInternalStyles();

	void IAppBarButtonHelpersProvider.StopAnimationForWidthAdjustments() => StopAnimationForWidthAdjustments();

	CommandBarDefaultLabelPosition IAppBarButtonHelpersProvider.GetEffectiveLabelPosition() => GetEffectiveLabelPosition();
}
