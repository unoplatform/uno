// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference AppBarAutomationPeer_Partial.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes AppBar types to Microsoft UI Automation.
/// </summary>
public partial class AppBarAutomationPeer : FrameworkElementAutomationPeer, IToggleProvider, IExpandCollapseProvider, IWindowProvider
{
	public AppBarAutomationPeer(AppBar owner) : base(owner)
	{
	}

	/// <summary>
	/// Retrieves the toggle state of the owner AppBar.
	/// </summary>
	public ToggleState ToggleState
	{
		get
		{
			var pOwner = Owner as AppBar;
			var isOpen = pOwner.IsOpen;

			return isOpen ? ToggleState.On : ToggleState.Off;
		}
	}

	/// <summary>
	/// Gets the state, expanded or collapsed, of the control.
	/// </summary>
	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			var pOwner = Owner as AppBar;
			var isOpen = pOwner.IsOpen;

			return isOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
		}
	}

	/// <summary>
	/// Gets a Boolean value indicating if the app bar is modal.
	/// </summary>
	public bool IsModal => true;

	/// <summary>
	/// Gets a Boolean value indicating if the app bar is the topmost element in the z-order of layout.
	/// </summary>
	public bool IsTopmost => true;

	/// <summary>
	/// Gets a Boolean value that indicates whether the app bar can be maximized.
	/// </summary>
	public bool Maximizable => false;

	/// <summary>
	/// Gets a Boolean value that indicates whether the app bar can be minimized.
	/// </summary>
	public bool Minimizable => false;

	/// <summary>
	/// Gets the interaction state of the app bar, such as running, closing, and so on.
	/// </summary>
	public WindowInteractionState InteractionState => WindowInteractionState.Running;

	/// <summary>
	/// Gets the visual state of the app bar.
	/// </summary>
	public WindowVisualState VisualState => WindowVisualState.Normal;

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		var shouldReturnWindowPattern = false;
		if (patternInterface == PatternInterface.Window)
		{
			var owner = Owner as AppBar;
			var isOpen = owner.IsOpen;

			shouldReturnWindowPattern = isOpen;
		}

		object ppReturnValue;
		if ((patternInterface == PatternInterface.ExpandCollapse) ||
			(patternInterface == PatternInterface.Toggle) ||
			(shouldReturnWindowPattern))
		{
			ppReturnValue = this;
		}
		else
		{
			ppReturnValue = base.GetPatternCore(patternInterface);
		}

		return ppReturnValue;
	}

	protected override string GetClassNameCore() => "ApplicationBar";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.AppBar;

	/// <summary>
	/// Cycles through the toggle states of an AppBarAutomationPeer.
	/// </summary>
	/// <exception cref="ElementNotEnabledException"></exception>
	public void Toggle()
	{
		bool bIsEnabled;

		bIsEnabled = IsEnabled();
		if (!bIsEnabled)
		{
			// UIA_E_ELEMENTNOTENABLED;
			throw new ElementNotEnabledException();
		}

		var pOwner = Owner as AppBar;
		var isOpen = pOwner.IsOpen;
		pOwner.IsOpen = !isOpen;
	}

	public void RaiseToggleStatePropertyChangedEvent(object pOldValue, object pNewValue)
	{
		var oldValue = ToggleButtonAutomationPeer.ConvertToToggleState(pOldValue);
		var newValue = ToggleButtonAutomationPeer.ConvertToToggleState(pNewValue);

		if (oldValue != newValue)
		{
			RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldValue, newValue);
		}
	}

	// Raise events for ExpandCollapseState changes to UIAutomation Clients.
	public void RaiseExpandCollapseAutomationEvent(bool isOpen)
	{
		ExpandCollapseState oldValue;
		ExpandCollapseState newValue;

		// Converting isOpen to appropriate enumerations
		if (isOpen)
		{
			oldValue = ExpandCollapseState.Collapsed;
			newValue = ExpandCollapseState.Expanded;
		}
		else
		{
			oldValue = ExpandCollapseState.Expanded;
			newValue = ExpandCollapseState.Collapsed;
		}

		RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, oldValue, newValue);
	}

	/// <summary>
	/// Displays all child nodes, controls, or content of the control.
	/// </summary>
	/// <exception cref="ElementNotEnabledException"></exception>
	public void Expand()
	{
		bool bIsEnabled;

		bIsEnabled = IsEnabled();
		if (!bIsEnabled)
		{
			// UIA_E_ELEMENTNOTENABLED;
			throw new ElementNotEnabledException();
		}

		var pOwner = Owner as AppBar;
		pOwner.IsOpen = true;
	}

	/// <summary>
	/// Hides all nodes, controls, or content that are descendants of the control.
	/// </summary>
	/// <exception cref="ElementNotEnabledException"></exception>
	public void Collapse()
	{
		bool bIsEnabled;

		bIsEnabled = IsEnabled();
		if (!bIsEnabled)
		{
			// UIA_E_ELEMENTNOTENABLED;
			throw new ElementNotEnabledException();
		}

		var pOwner = Owner as AppBar;
		pOwner.IsOpen = false;
	}

	/// <summary>
	/// Closes the app bar.
	/// </summary>
	public void Close()
	{

	}

	/// <summary>
	/// Changes the visual state of the app bar (such as minimizing or maximizing it).
	/// </summary>
	/// <param name="state">The visual state of the app bar to change to, as a value 
	/// of the enumeration.</param>
	public void SetVisualState(WindowVisualState state)
	{

	}

	/// <summary>
	/// Blocks the calling code for the specified time or until the associated 
	/// process enters an idle state, whichever completes first.
	/// </summary>
	/// <param name="milliseconds">The amount of time, in milliseconds, to wait 
	/// for the associated process to become idle.</param>
	/// <returns>false</returns>
	public bool WaitForInputIdle(int milliseconds)
		=> false;
}
