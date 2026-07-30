// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Automation peer for <see cref="ImplicitTextBlock"/> — the text element a ContentPresenter
/// generates for plain string content. It mirrors WinUI: the generated content text is excluded
/// from the Control/Content UIA views when its owning ContentPresenter/ContentControl is marked
/// <c>AutomationProperties.AccessibilityView="Raw"</c> (as WinUI's Button/CheckBox/RadioButton/
/// ToggleSwitch templates do). The owner's view is read live so it is not affected by the order in
/// which the template applies the property relative to content materialization. Bare presenters that
/// keep the default (Content) view still expose their text, exactly as in WinUI.
/// </summary>
public partial class ImplicitTextBlockAutomationPeer : TextBlockAutomationPeer
{
	public ImplicitTextBlockAutomationPeer(ImplicitTextBlock owner) : base(owner)
	{
	}

	protected override bool IsControlElementCore()
		=> !IsOwnerRaw() && base.IsControlElementCore();

	protected override bool IsContentElementCore()
		=> !IsOwnerRaw() && base.IsContentElementCore();

	private bool IsOwnerRaw()
		=> Owner is ImplicitTextBlock { ContentOwner: { } owner }
			&& AutomationProperties.GetAccessibilityView(owner) == AccessibilityView.Raw;
}
