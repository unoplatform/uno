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
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Input;
using static Uno.UI.FeatureConfiguration;
using Uno.UI.Extensions;
using Popup = Microsoft.UI.Xaml.Controls.Primitives.Popup;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBarButton : Button, ICommandBarElement, ICommandBarElement2, ICommandBarElement3, ICommandBarOverflowElement, ICommandBarLabeledElement, ISubMenuOwner, IAppBarButtonHelpersProvider
{
	DependencyProperty IAppBarButtonHelpersProvider.GetIsCompactDependencyProperty() => IsCompactProperty;
	DependencyProperty IAppBarButtonHelpersProvider.GetUseOverflowStyleDependencyProperty() => UseOverflowStyleProperty;
	DependencyProperty IAppBarButtonHelpersProvider.GetLabelPositionDependencyProperty() => LabelPositionProperty;
	DependencyProperty IAppBarButtonHelpersProvider.GetLabelDependencyProperty() => LabelProperty;
	DependencyProperty IAppBarButtonHelpersProvider.GetIconDependencyProperty() => IconProperty;
	DependencyProperty IAppBarButtonHelpersProvider.GetKeyboardAcceleratorTextDependencyProperty() => KeyboardAcceleratorTextOverrideProperty;

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

	private protected override void OnUnloaded()
	{
		base.OnUnloaded();

		m_flyoutOpenedHandler.Disposable = null;
		m_flyoutClosedHandler.Disposable = null;

		m_menuHelper = null;

		_contentChangedHandler.Disposable = null;
		_iconChangedHandler.Disposable = null;
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

	bool ICommandBarLabeledElement.GetHasBottomLabel()
	{
		CommandBarDefaultLabelPosition effectiveLabelPosition = GetEffectiveLabelPosition();
		var label = Label;

		return effectiveLabelPosition == CommandBarDefaultLabelPosition.Bottom
			&& label != null;
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		base.OnPointerEntered(args);

		bool isInOverflow = false;
		isInOverflow = IsInOverflow;

		if (isInOverflow && m_menuHelper is { })
		{
			m_menuHelper.OnPointerEntered(args);
		}

		CloseSubMenusOnPointerEntered(this);
	}

	protected override void OnPointerExited(PointerRoutedEventArgs e)
	{
		base.OnPointerExited(e);
		bool isInOverflow = false;
		isInOverflow = IsInOverflow;

		if (isInOverflow && m_menuHelper is { })
		{
			m_menuHelper.OnPointerExited(e, parentIsSubMenu: false);
		}
	}

	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		base.OnKeyDown(args);

		bool isInOverflow = false;
		isInOverflow = IsInOverflow;

		if (isInOverflow && m_menuHelper is { })
		{
			m_menuHelper.OnKeyDown(args);
		}
	}

	protected override void OnKeyUp(KeyRoutedEventArgs args)
	{
		base.OnKeyUp(args);

		bool isInOverflow = false;
		isInOverflow = IsInOverflow;

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
				m_menuHelper = new CascadingMenuHelper();
				m_menuHelper.Initialize(this);

				newFlyout.Opened += OnFlyoutOpened;
				m_flyoutOpenedHandler.Disposable = Disposable.Create(() => newFlyout.Opened -= OnFlyoutOpened);

				newFlyout.Closed += OnFlyoutClosed;
				m_flyoutClosedHandler.Disposable = Disposable.Create(() => newFlyout.Closed -= OnFlyoutClosed);
			}
		}

		OnPropertyChanged(args);
	}

	private void OnFlyoutClosed(object? sender, object e)
	{
		m_isFlyoutClosing = false;
		UpdateVisualState();
	}

	private void OnFlyoutOpened(object? sender, object e)
	{
		m_isFlyoutClosing = false;
		UpdateVisualState();
	}

	// After template is applied, set the initial view state
	// (FullSize or Compact) based on the value of our
	// IsCompact property
	protected override void OnApplyTemplate()
	{
		OnBeforeApplyTemplate();
		base.OnApplyTemplate();
		OnAfterApplyTemplate();

		SetupContentUpdate();
	}

	// Sets the visual state to "Compact" or "FullSize" based on the value
	// of our IsCompact property.
	private protected override void ChangeVisualState(bool useTransitions)
	{
		bool useOverflowStyle = false;

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


		ChangeCommonVisualStates(useTransitions);
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new AppBarButtonAutomationPeer(this);
	}

	private void OnClick(object sender, RoutedEventArgs e)
	{
		FlyoutBase spFlyoutBase;

		// Don't execute the logic on CommandBar to close the secondary
		// commands popup when we have a flyout associated with this button.
		spFlyoutBase = Flyout;
		if (spFlyoutBase == null)
		{
			CommandBar.OnCommandExecutionStatic(this);
		}
	}

	protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
	{
		base.OnVisibilityChanged(oldValue, newValue);
		CommandBar.OnCommandBarElementVisibilityChanged(this);
	}

	private protected override void OnCommandChanged(object oldValue, object newValue)
	{
		base.OnCommandChanged(oldValue, newValue);
		OnCommandChangedHelper(oldValue, newValue);
	}

	private protected override void OpenAssociatedFlyout()
	{
		bool isInOverflow = false;
		isInOverflow = IsInOverflow;

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

	private CommandBarDefaultLabelPosition GetEffectiveLabelPosition()
	{
		var labelPosition = LabelPosition;

		return labelPosition == CommandBarLabelPosition.Collapsed ? CommandBarDefaultLabelPosition.Collapsed : m_defaultLabelPosition;
	}

	private void UpdateInternalStyles()
	{
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
			&& !this.IsDependencyPropertyLocallySet(WidthProperty))
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

		UpdateToolTip();
	}

	private Storyboard CreateStoryboardForWidthAdjustmentsForLabelOnRightStyle()
	{
		var storyboardLocal = new Storyboard();

		var objectAnimation = new ObjectAnimationUsingKeyFrames();

		Storyboard.SetTarget(objectAnimation, this);
		Storyboard.SetTargetProperty(objectAnimation, "Width");

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
			ClockState currentState;
			currentState = m_widthAdjustmentsForLabelOnRightStyleStoryboard.GetCurrentState();
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

	ISubMenuOwner ISubMenuOwner.ParentOwner
	{
		get => null!;
		set => throw new NotImplementedException();
	}

	void ISubMenuOwner.PrepareSubMenu()
	{
		var flyout = Flyout;

		if (flyout is { })
		{
			// TODO: Uno specific - avoid using RootVisual on WinUI branch
			if (XamlRoot is not null)
			{
				var rootElement = XamlRoot.VisualTree.RootElement;
				flyout.OverlayInputPassThroughElement = rootElement;
			}

			if (flyout is IMenu flyoutAsMenu)
			{
				CommandBar.FindParentCommandBarForElement(this, out var parentCommandBar);

				if (parentCommandBar is { })
				{
					flyoutAsMenu.ParentMenu = parentCommandBar;
				}
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
			if (!isInOverflow)
			{
				return;
			}

			showOptions.Placement = FlyoutPlacementMode.RightEdgeAlignedTop;

			var itemWidth = ActualWidth;

			showOptions.Position = position;

			flyout.ShowAt(this, showOptions);

			if (m_menuHelper is { })
			{
				m_menuHelper.SetSubMenuPresenter(flyout.GetPresenter());
			}
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
