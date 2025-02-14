// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AppBarToggleButton_Partial.cpp, tag winui3/release/1.6.4, commit 262a901e09

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using DirectUI;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AppBarToggleButtonAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.AppBarToggleButtonAutomationPeer;
using AppBarToggleButtonTemplateSettings = Microsoft.UI.Xaml.Controls.Primitives.AppBarToggleButtonTemplateSettings;
using System.Globalization;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class AppBarToggleButton : ToggleButton, ICommandBarElement, ICommandBarElement2, ICommandBarElement3, ICommandBarOverflowElement, ICommandBarLabeledElement
{
	/// <summary>
	/// Initializes a new instance of the AppBarToggleButton class.
	/// </summary>
	public AppBarToggleButton()
	{
		m_inputDeviceTypeUsedToOpenOverflow = InputDeviceType.None;
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

	bool ICommandBarLabeledElement.GetHasBottomLabel() =>
		GetHasLabelAtPosition(CommandBarDefaultLabelPosition.Bottom);

	bool ICommandBarLabeledElement.GetHasRightLabel() =>
		GetHasLabelAtPosition(CommandBarDefaultLabelPosition.Right);

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);
		AppBarButtonHelpers<AppBarToggleButton>.OnPropertyChanged(this, args);
	}

	protected override void OnApplyTemplate()
	{
		AppBarButtonHelpers<AppBarToggleButton>.OnBeforeApplyTemplate(this);
		base.OnApplyTemplate();
		AppBarButtonHelpers<AppBarToggleButton>.OnApplyTemplate(this);
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		base.OnPointerEntered(args);
		AppBarButtonHelpers<AppBarToggleButton>.CloseSubMenusOnPointerEntered(this, null);
	}

	// Sets the visual state to "Compact" or "FullSize" based on the value
	// of our IsCompact property.
	private protected override void ChangeVisualState(bool useTransitions)
	{
		base.ChangeVisualState(useTransitions);
		var useOverflowStyle = UseOverflowStyle;

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

		AppBarButtonHelpers<AppBarToggleButton>.ChangeCommonVisualStates(this, useTransitions);
	}


	// Create AppBarToggleButtonAutomationPeer to represent the AppBarToggleButton.
	protected override AutomationPeer OnCreateAutomationPeer() =>
		new AppBarToggleButtonAutomationPeer(this);

	private protected override void OnClick()
	{
		CommandBar.OnCommandExecutionStatic(this);
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
		AppBarButtonHelpers<AppBarToggleButton>.OnCommandChanged(this, oldValue, newValue);
	}

	private bool GetHasLabelAtPosition(CommandBarDefaultLabelPosition labelPosition)
	{
		var effectiveLabelPosition = GetEffectiveLabelPosition();

		if (effectiveLabelPosition != labelPosition)
		{
			return false;
		}

		return Label != null;
	}

	private CommandBarDefaultLabelPosition GetEffectiveLabelPosition() =>
		LabelPosition == CommandBarLabelPosition.Collapsed ?
			CommandBarDefaultLabelPosition.Collapsed :
			m_defaultLabelPosition;

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
			if (m_widthAdjustmentsForLabelOnRightStyleStoryboard is null)
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

		AppBarButtonHelpers<AppBarToggleButton>.UpdateToolTip(this);
	}

	void IAppBarButtonHelpersProvider.UpdateInternalStyles() => UpdateInternalStyles();

	private Storyboard CreateStoryboardForWidthAdjustmentsForLabelOnRightStyle()
	{
		var storyboardLocal = new Storyboard();

		var storyboardChildren = storyboardLocal.Children;

		var objectAnimation = new ObjectAnimationUsingKeyFrames();

		Storyboard.SetTarget(objectAnimation, this);
		Storyboard.SetTargetProperty(objectAnimation, nameof(Width));

		var objectKeyFrames = objectAnimation.KeyFrames;

		var discreteObjectKeyFrame = new DiscreteObjectKeyFrame();

		var keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

		discreteObjectKeyFrame.KeyTime = keyTime;
		discreteObjectKeyFrame.Value = double.NaN;

		objectAnimation.KeyFrames.Add(discreteObjectKeyFrame);
		storyboardChildren.Add(objectAnimation);

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
}
