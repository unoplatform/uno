#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using DirectUI;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AppBarToggleButtonAutomationPeer = Windows.UI.Xaml.Automation.Peers.AppBarToggleButtonAutomationPeer;
using AppBarToggleButtonTemplateSettings = Windows.UI.Xaml.Controls.Primitives.AppBarToggleButtonTemplateSettings;
using System.Globalization;

namespace Windows.UI.Xaml.Controls
{
	public partial class AppBarToggleButton : ToggleButton, ICommandBarElement, ICommandBarElement2, ICommandBarElement3, ICommandBarOverflowElement, ICommandBarLabeledElement
	{
		// LabelOnRightStyle doesn't work in AppBarButton/AppBarToggleButton Reveal Style.
		// Animate the width to NaN if width is not overrided and right-aligned labels and no LabelOnRightStyle.
		Storyboard? m_widthAdjustmentsForLabelOnRightStyleStoryboard;

		CommandBarDefaultLabelPosition m_defaultLabelPosition;
		// UNO TODO
		//DirectUI::InputDeviceType m_inputDeviceTypeUsedToOpenOverflow;

		TextBlock? m_tpKeyboardAcceleratorTextLabel;

		// We won't actually set the label-on-right style unless we've applied the template,
		// because we won't have the label-on-right style from the template until we do.
		bool m_isTemplateApplied;


		// We need to adjust our visual state to account for CommandBarElements that use Icons.
		bool m_isWithIcons;

		// We need to adjust our visual state to account for CommandBarElements that have keyboard accelerator text.
		bool m_isWithKeyboardAcceleratorText;
		double m_maxKeyboardAcceleratorTextWidth;

		// If we have a keyboard accelerator attached to us and the app has not set a tool tip on us,
		// then we'll create our own tool tip.  We'll use this flag to indicate that we can unset or
		// overwrite that tool tip as needed if the keyboard accelerator is removed or the button
		// moves into the overflow section of the app bar or command bar.
		bool m_ownsToolTip;

		public AppBarToggleButton()
		{
			//m_inputDeviceTypeUsedToOpenOverflow(DirectUI::InputDeviceType::None)
			m_isTemplateApplied = false;
			m_isWithIcons = false;
			m_ownsToolTip = true;

			DefaultStyleKey = typeof(AppBarToggleButton);
		}

		internal void SetOverflowStyleParams(bool hasIcons, bool hasKeyboardAcceleratorText)
		{
			bool updateState = false;

			if (m_isWithIcons != hasIcons)
			{
				m_isWithIcons = hasIcons;
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

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);
			OnPropertyChanged(args);
		}

		protected override void OnApplyTemplate()
		{
			OnBeforeApplyTemplate();
			base.OnApplyTemplate();
			OnAfterApplyTemplate();
		}

		protected override void OnPointerEntered(PointerRoutedEventArgs args)
		{
			base.OnPointerEntered(args);
			CloseSubMenusOnPointerEntered(null);
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
				if (m_isWithIcons)
				{
					GoToState(useTransitions, "OverflowWithMenuIcons");
				}
				else
				{
					GoToState(useTransitions, "Overflow");
				}

				{
					bool isEnabled = false;
					bool isPressed = false;
					bool isPointerOver = false;
					bool isChecked;

					isEnabled = IsEnabled;
					isPressed = IsPressed;
					isPointerOver = IsPointerOver;
					isChecked = IsChecked ?? false;

					if (isChecked)
					{
						if (isPressed)
						{
							GoToState(useTransitions, "OverflowCheckedPressed");
						}
						else if (isPointerOver)
						{
							GoToState(useTransitions, "OverflowCheckedPointerOver");
						}
						else if (isEnabled)
						{
							GoToState(useTransitions, "OverflowChecked");
						}
					}
					else
					{
						if (isPressed)
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
			}

			ChangeCommonVisualStates(useTransitions);
		}


		// Create AppBarToggleButtonAutomationPeer to represent the AppBarToggleButton.
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new AppBarToggleButtonAutomationPeer(this);
		}

		protected override void OnToggle()
		{
			CommandBar.OnCommandExecutionStatic(this);
			base.OnToggle();
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

		private CommandBarDefaultLabelPosition GetEffectiveLabelPosition()
		{
			CommandBarLabelPosition labelPosition;
			labelPosition = LabelPosition;

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

			CommandBarDefaultLabelPosition effectiveLabelPosition;
			bool useOverflowStyle;

			effectiveLabelPosition = GetEffectiveLabelPosition();
			useOverflowStyle = UseOverflowStyle;

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

		private void CloseSubMenusOnPointerEntered(ISubMenuOwner? pMenuToLeaveOpen)
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
					templateSettings = new AppBarToggleButtonTemplateSettings();
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

					SetValue(ToolTipService.ToolTipProperty, string.Format(CultureInfo.CurrentCulture, toolTipFormatString, labelText, keyboardAcceleratorText));
				}
				else
				{
					ClearValue(ToolTipService.ToolTipProperty);
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
			var keyboardAcceleratorText = GetValue(KeyboardAcceleratorTextOverrideProperty) as string;

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

		private void PutKeyboardAcceleratorText(string keyboardAcceleratorText)
		{
			SetValue(KeyboardAcceleratorTextOverrideProperty, keyboardAcceleratorText);
		}

		#endregion

		private void GetTemplatePart<T>(string name, out T? element) where T : class
		{
			element = GetTemplateChild(name) as T;
		}
	}
}
