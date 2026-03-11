// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\CommandBarFlyoutCommandBarAutomationProperties.properties.cpp, tag winui3/release/1.7.3, commit 65718e2813a90fc900e8775d2ddc580b268fcc2f

using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Enables getting or setting specific automation properties for the CommandBarFlyoutCommandBar.
/// </summary>
public static partial class CommandBarFlyoutCommandBarAutomationProperties
{
	/// <summary>
	/// Retrieves the control type for the specified object.
	/// </summary>
	/// <param name="element">The object to get the control type for.</param>
	/// <returns>The control type for the specified object.</returns>
	public static AutomationControlType GetControlType(UIElement element) =>
		(AutomationControlType)element.GetValue(ControlTypeProperty);

	/// <summary>
	/// Sets the control type for the specified object.
	/// </summary>
	/// <param name="element">The object to set the control type for.</param>
	/// <param name="value">The control type for the specified object.</param>
	public static void SetControlType(UIElement element, AutomationControlType value) =>
		element.SetValue(ControlTypeProperty, value);

	/// <summary>
	/// Identifies the ControlType dependency property.
	/// </summary>
	public static DependencyProperty ControlTypeProperty
	{
		[DynamicDependency(nameof(GetControlType))]
		[DynamicDependency(nameof(SetControlType))]
		get;
	} = DependencyProperty.RegisterAttached(
			"ControlType",
			typeof(AutomationControlType),
			typeof(CommandBarFlyoutCommandBarAutomationProperties),
			new FrameworkPropertyMetadata(AutomationControlType.Custom, OnControlTypePropertyChanged));
}
