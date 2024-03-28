#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DirectUI;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media.Animation;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls
{
	public partial class AppBarButton : Button, ICommandBarElement, ICommandBarElement2, ICommandBarElement3, ICommandBarOverflowElement, ICommandBarLabeledElement, ISubMenuOwner
	{
		// LabelOnRightStyle doesn't work in AppBarButton/AppBarToggleButton Reveal Style.
		// Animate the width to NaN if width is not overrided and right-aligned labels and no LabelOnRightStyle.
		Storyboard? m_widthAdjustmentsForLabelOnRightStyleStoryboard;

		bool m_isWithToggleButtons;
		bool m_isWithIcons;
		CommandBarDefaultLabelPosition m_defaultLabelPosition;
		//DirectUI::InputDeviceType m_inputDeviceTypeUsedToOpenOverflow;

		TextBlock? m_tpKeyboardAcceleratorTextLabel;

		// We won't actually set the label-on-right style unless we've applied the template,
		// because we won't have the label-on-right style from the template until we do.
		bool m_isTemplateApplied;

		// We need to adjust our visual state to account for CommandBarElements that have keyboard accelerator text.
		bool m_isWithKeyboardAcceleratorText;
		double m_maxKeyboardAcceleratorTextWidth;

		// If we have a keyboard accelerator attached to us and the app has not set a tool tip on us,
		// then we'll create our own tool tip.  We'll use this flag to indicate that we can unset or
		// overwrite that tool tip as needed if the keyboard accelerator is removed or the button
		// moves into the overflow section of the app bar or command bar.
		bool m_ownsToolTip;

		// Helper to which to delegate cascading menu functionality.
		CascadingMenuHelper? m_menuHelper;

		// Helpers to track the current opened state of the flyout.
		bool m_isFlyoutClosing;
		SerialDisposable m_flyoutOpenedHandler = new SerialDisposable();
		SerialDisposable m_flyoutClosedHandler = new SerialDisposable();
		SerialDisposable _contentChangedHandler = new SerialDisposable();
		SerialDisposable _iconChangedHandler = new SerialDisposable();

		// Holds the last position that its flyout was opened at.
		// Used to reposition the flyout on size changed.
		Point m_lastFlyoutPosition;

		internal CascadingMenuHelper? MenuHelper => m_menuHelper;

		public AppBarButton()
		{
			m_isWithToggleButtons = false;
			m_isWithIcons = false;
			//m_inputDeviceTypeUsedToOpenOverflow(DirectUI::InputDeviceType::None)
			m_isTemplateApplied = false;
			m_ownsToolTip = true;

			m_menuHelper = new CascadingMenuHelper();
			m_menuHelper.Initialize(this);

			Click += OnClick;

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
			CommandBar.FindParentCommandBarForElement(this, out var parentCommandBar);

			if (parentCommandBar is { })
			{
				parentCommandBar.CloseSubMenus(this, false);
			}
		}

		void ISubMenuOwner.CloseSubMenu()
		{
			var flyout = Flyout;

			if (flyout is { })
			{
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

		bool ISubMenuOwner.IsSubMenuPositionedAbsolutely => false;

		void ISubMenuOwner.RaiseAutomationPeerExpandCollapse(bool isOpen)
		{
			if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
			{
				var spAutomationPeer = GetAutomationPeer();
				if (spAutomationPeer is AppBarButtonAutomationPeer appbarButtonAutomationPeer)
				{
					appbarButtonAutomationPeer.RaiseExpandCollapseAutomationEvent(isOpen);
				}
			}
		}

		#region AppBarButtonHelpers
		private void OnBeforeApplyTemplate()
		{
			if (m_isTemplateApplied)
			{
				StopAnimationForWidthAdjustments();
				m_isTemplateApplied = false;
			}
		}

		private void OnAfterApplyTemplate()
		{
			GetTemplatePart<TextBlock>("KeyboardAcceleratorTextLabel", out var keyboardAcceleratorTextLabel);
			m_tpKeyboardAcceleratorTextLabel = keyboardAcceleratorTextLabel;

			m_isTemplateApplied = true;

			// Set the initial view state
			UpdateInternalStyles();
			UpdateVisualState();
		}

		private void CloseSubMenusOnPointerEntered(ISubMenuOwner pMenuToLeaveOpen)
		{
			var isInOverflow = IsInOverflow;

			if (isInOverflow)
			{
				// If there are other buttons that have open sub-menus, then we should
				// close those on a delay, since they no longer have mouse-over.

				CommandBar.FindParentCommandBarForElement(this, out var parentCommandBar);

				if (parentCommandBar is { })
				{
					parentCommandBar.CloseSubMenus(pMenuToLeaveOpen, true /* closeOnDelay */);
				}
			}
		}

		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == IsCompactProperty
				|| args.Property == UseOverflowStyleProperty
				|| args.Property == LabelPositionProperty)
			{
				UpdateInternalStyles();
				UpdateVisualState();
			}

			if (args.Property == ToolTipService.ToolTipProperty)
			{
				var toolTipValue = GetValue(ToolTipService.ToolTipProperty);

				if (toolTipValue is { })
				{
					m_ownsToolTip = false;
				}
				else
				{
					m_ownsToolTip = true;
				}
			}
		}

		private void ChangeCommonVisualStates(bool useTransitions)
		{
			var isCompact = IsCompact;
			var useOverflowStyle = UseOverflowStyle;
			var effectiveLabelPosition = GetEffectiveLabelPosition();
			bool isKeyboardPresent = false;

			// We only care about finding if we have a keyboard if we also have a menu item with keyboard accelerator text,
			// since if we don't have any menu items with keyboard accelerator text, we won't be showing any that text anyway.
			if (m_isWithKeyboardAcceleratorText)
			{
				// UNO TODO
				// isKeyboardPresent = DXamlCore.GetCurrent().GetIsKeyboardPresent();
				isKeyboardPresent = true;
			}

			if (!useOverflowStyle)
			{
				if (effectiveLabelPosition == CommandBarDefaultLabelPosition.Right)
				{
					GoToState(useTransitions, "LabelOnRight");
				}
				else if (effectiveLabelPosition == CommandBarDefaultLabelPosition.Collapsed)
				{
					GoToState(useTransitions, "LabelCollapsed");
				}
				else if (isCompact)
				{
					GoToState(useTransitions, "Compact");
				}
				else
				{
					GoToState(useTransitions, "FullSize");
				}
			}

			GoToState(useTransitions, "InputModeDefault");
			//if (button->m_inputDeviceTypeUsedToOpenOverflow == DirectUI::InputDeviceType::Touch)
			//{
			//	IFC_RETURN(button->GoToState(useTransitions, L"TouchInputMode", &ignored));
			//}
			//else if (button->m_inputDeviceTypeUsedToOpenOverflow == DirectUI::InputDeviceType::GamepadOrRemote)
			//{
			//	IFC_RETURN(button->GoToState(useTransitions, L"GameControllerInputMode", &ignored));
			//}

			// We'll make the keyboard accelerator text visible if any element in the overflow has keyboard accelerator text,
			// as this causes the margin to be applied which reserves space, ensuring that keyboard accelerator text
			// in one button won't be at the same horizontal position as label text in another button.
			if (m_isWithKeyboardAcceleratorText && isKeyboardPresent && useOverflowStyle)
			{
				GoToState(useTransitions, "KeyboardAcceleratorTextVisible");
			}
			else
			{
				GoToState(useTransitions, "KeyboardAcceleratorTextCollapsed");
			}

		}

		private void OnCommandChangedHelper(object pOldValue, object pNewValue)
		{
			if (pOldValue is { })
			{
				if (pOldValue is XamlUICommand oldCommandAsUICommand)
				{
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, LabelProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, IconProperty);
				}
			}

			if (pNewValue is { })
			{
				if (pNewValue is XamlUICommand newCommandAsUICommand)
				{
					// The call to ButtonBase::OnCommandChanged() will have set the Content property, which we don't want -
					// it's not used anywhere in AppBar*Button, and having it be set can cause problems if an AppBarButton
					// has a ContentPresenter with a null Content property in its template, as that will be caused to pick up
					// the parent ContentControl's Content property if one exists.
					CommandingHelpers.ClearBindingIfSet(newCommandAsUICommand, this, ContentControl.ContentProperty);

					CommandingHelpers.BindToLabelPropertyIfUnset(newCommandAsUICommand, this, LabelProperty);
					CommandingHelpers.BindToIconPropertyIfUnset(newCommandAsUICommand, this, IconProperty);
				}
			}
		}

		internal void UpdateTemplateSettings(double maxKeyboardAcceleratorTextWidth)
		{
			if (m_maxKeyboardAcceleratorTextWidth != maxKeyboardAcceleratorTextWidth)
			{
				m_maxKeyboardAcceleratorTextWidth = maxKeyboardAcceleratorTextWidth;

				var templateSettings = TemplateSettings;

				if (templateSettings == null)
				{
					templateSettings = new AppBarButtonTemplateSettings();
					TemplateSettings = templateSettings;
				}

				templateSettings.KeyboardAcceleratorTextMinWidth = m_maxKeyboardAcceleratorTextWidth;
			}
		}

		private void UpdateToolTip()
		{
			if (m_ownsToolTip)
			{
				var useOverflowStyle = UseOverflowStyle;
				var keyboardAcceleratorText = KeyboardAcceleratorTextOverride;

				if (!useOverflowStyle && !string.IsNullOrWhiteSpace(keyboardAcceleratorText))
				{
					// If we're in the primary section of the app bar or command bar and have accelerator text,
					// then we should give ourselves a tool tip showing the label plus the accelerator text.
					var labelText = Label;

					var toolTipFormatString = DXamlCore.Current.GetLocalizedResourceString("KEYBOARD_ACCELERATOR_TEXT_TOOLTIP");

					this.SetValue(ToolTipService.ToolTipProperty, string.Format(CultureInfo.CurrentCulture, toolTipFormatString, labelText, keyboardAcceleratorText));
				}
				else
				{
					this.ClearValue(ToolTipService.ToolTipProperty);
				}

				// Setting the value of ToolTipService.ToolTip causes us to flag us as no longer owning the tool tip,
				// since that's the code path that an app setting the value will also take.
				// In order to ensure that we know that we still own the tool tip, we'll set this value to true here.
				m_ownsToolTip = true;
			}
		}

		internal Size GetKeyboardAcceleratorTextDesiredSize()
		{
			var desiredSize = new Size(0, 0);

			if (m_tpKeyboardAcceleratorTextLabel is { })
			{
				m_tpKeyboardAcceleratorTextLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				desiredSize = m_tpKeyboardAcceleratorTextLabel.DesiredSize;
				var margin = m_tpKeyboardAcceleratorTextLabel.Margin;

				desiredSize.Width -= (margin.Left + margin.Right);
				desiredSize.Height -= (margin.Top + margin.Bottom);
			}

			return desiredSize;
		}


		private string GetKeyboardAcceleratorText()
		{
			var keyboardAcceleratorText = this.GetValue(KeyboardAcceleratorTextOverrideProperty) as string;

			// If we have no keyboard accelerator text already provided by the app,
			// then we'll see if we can construct it ourselves based on keyboard accelerators
			// set on this item.  For example, if a keyboard accelerator with key "S" and modifier "Control"
			// is set, then we'll convert that into the keyboard accelerator text "Ctrl+S".
			if (string.IsNullOrWhiteSpace(keyboardAcceleratorText))
			{
				keyboardAcceleratorText = KeyboardAccelerator.GetStringRepresentationForUIElement(this);

				// If we were able to get a string representation from keyboard accelerators,
				// then we should now set that as the value of KeyboardAcceleratorText.
				if (!string.IsNullOrWhiteSpace(keyboardAcceleratorText))
				{
					PutKeyboardAcceleratorText(keyboardAcceleratorText);
				}
			}

			return keyboardAcceleratorText ?? string.Empty;
		}

		//UNO TODO: Remove this when ContentPresenter's implicit ContentProperty TemplateBinding is supported.
		private void SetupContentUpdate()
		{
			GetTemplatePart<ContentPresenter>("Content", out var contentPresenter);

			void UpdateContent()
			{
				if (contentPresenter != null)
				{
					contentPresenter.Content = Icon ?? Content;
				}
			}

			_contentChangedHandler.Disposable = this.RegisterDisposablePropertyChangedCallback(ContentProperty, (s, e) => UpdateContent());
			_iconChangedHandler.Disposable = this.RegisterDisposablePropertyChangedCallback(IconProperty, (s, e) => UpdateContent());
			UpdateContent();
		}

		private void PutKeyboardAcceleratorText(string keyboardAcceleratorText)
		{
			this.SetValue(KeyboardAcceleratorTextOverrideProperty, keyboardAcceleratorText);
		}

		#endregion
		private void GetTemplatePart<T>(string name, out T? element) where T : class
		{
			element = GetTemplateChild(name) as T;
		}
	}
}
