// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\PopupAutomationPeer_Partial.cpp, tag winui3/release/1.4.3, commit 685d2bf

#nullable enable

using System;
using DirectUI;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Automation.Peers;

internal class PopupAutomationPeer : FrameworkElementAutomationPeer, IWindowProvider
{
	private const string UIA_POPUP_NAME = "Popup";

	// Initializes a new instance of the PopupAutomationPeer class.
	public PopupAutomationPeer(Popup owner) : base(owner)
	{
		if (owner is null)
		{
			throw new ArgumentNullException(nameof(owner));
		}
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		var owner = Owner;
		if (patternInterface == PatternInterface.Window)
		{
			var shouldExposeWindowPattern = ((Popup)owner).GetShouldUIAPeerExposeWindowPattern();

			if (shouldExposeWindowPattern)
			{
				return this;
			}
			else
			{
				return base.GetPatternCore(patternInterface);
			}
		}
		else
		{
			return base.GetPatternCore(patternInterface);
		}
	}

	protected override string GetClassNameCore() => nameof(Popup);

	protected override string GetNameCore()
	{
		var returnValue = base.GetNameCore();
		if (string.IsNullOrEmpty(returnValue))
		{
			var strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_POPUP_NAME);
			returnValue = strAutomationName;
		}

		return returnValue;
	}

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

	bool IWindowProvider.IsModal
	{
		get
		{
			var isEnabled = IsEnabled();
			if (!isEnabled)
			{
				throw new InvalidOperationException("Element is not enabled");
			}

			var owner = Owner;
			return ((Popup)owner).IsOpen;
		}
	}

	bool IWindowProvider.IsTopmost
	{
		get
		{
			var isEnabled = IsEnabled();
			if (!isEnabled)
			{
				throw new InvalidOperationException("Element is not enabled");
			}

			var owner = Owner;
			return ((Popup)owner).IsOpen;
		}
	}

	bool IWindowProvider.Maximizable => false;

	bool IWindowProvider.Minimizable => false;

	WindowInteractionState IWindowProvider.InteractionState
	{
		get
		{
			var isEnabled = IsEnabled();
			if (isEnabled)
			{
				throw new InvalidOperationException("Element is not enabled");
			}

			var owner = Owner;
			var isOpen = ((Popup)owner).IsOpen;

			return isOpen ? WindowInteractionState.Running : WindowInteractionState.Closing;
		}
	}

	WindowVisualState IWindowProvider.VisualState => WindowVisualState.Normal;

	void IWindowProvider.Close()
	{
		var isEnabled = IsEnabled();
		if (isEnabled)
		{
			throw new InvalidOperationException("Element is not enabled");
		}

		var owner = Owner;
		((Popup)owner).LightDismiss(FocusState.Pointer);
	}
	void IWindowProvider.SetVisualState(WindowVisualState state)
	{
	}

	bool IWindowProvider.WaitForInputIdle(int milliseconds) => false;
}
