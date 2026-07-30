// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PopupAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes <see cref="Popup"/> instances to Microsoft UI Automation.
/// </summary>
public partial class PopupAutomationPeer : FrameworkElementAutomationPeer, IWindowProvider
{
	public PopupAutomationPeer(Popup owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Window && ShouldExposeWindowPattern())
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(Popup);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Window;

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		var popup = GetPopup();
		if (popup is null || popup.Child is not { } child)
		{
			return new List<AutomationPeer>();
		}

		var childPeer = child.GetOrCreateAutomationPeer();
		if (childPeer is not null)
		{
			return new List<AutomationPeer> { childPeer };
		}

		return GetAutomationPeersForChildrenOfElement(child);
	}

	protected override string GetNameCore()
	{
		var name = base.GetNameCore();
		if (!string.IsNullOrEmpty(name))
		{
			return name;
		}

		return DirectUI.DXamlCore.GetCurrentNoCreate()?.GetLocalizedResourceString("UIA_POPUP_NAME") ?? "Popup";
	}

	// IWindowProvider

	public WindowInteractionState InteractionState
	{
		get
		{
			ThrowIfDisabled();
			return GetPopup()?.IsOpen == true
				? WindowInteractionState.Running
				: WindowInteractionState.Closing;
		}
	}

	public bool IsModal
	{
		get
		{
			ThrowIfDisabled();
			return GetPopup()?.IsOpen ?? true;
		}
	}

	public bool IsTopmost
	{
		get
		{
			ThrowIfDisabled();
			return GetPopup()?.IsOpen ?? true;
		}
	}

	public bool Maximizable => false;

	public bool Minimizable => false;

	public WindowVisualState VisualState => WindowVisualState.Normal;

	public void Close()
	{
		ThrowIfDisabled();

		if (GetPopup() is { } popup)
		{
			popup.IsOpen = false;
		}
	}

	public void SetVisualState(WindowVisualState state)
	{
		// Popups don't support minimize/maximize. No-op matches WinUI returning S_FALSE.
	}

	public bool WaitForInputIdle(int milliseconds) => true;

	private Popup GetPopup() => Owner as Popup;

	private void ThrowIfDisabled()
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}
	}

	// Mirrors WinUI's Popup::GetShouldUIAPeerExposeWindowPattern: expose Window pattern only when the popup is
	// a light-dismiss surface (flyouts, ContentDialogs, submenus). Plain non-dismiss popups stay as a Window
	// control-type without the IWindowProvider pattern.
	private bool ShouldExposeWindowPattern()
	{
		if (GetPopup() is not { } popup)
		{
			return false;
		}

		return popup.IsLightDismissEnabled || popup.IsSubMenu;
	}
}
