// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ResizeGripper\ResizeGripperAutomationPeer.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Private.Controls;

partial class ResizeGripperAutomationPeer
{
	// Read from the declared OwnerName property, never from Tag and never from an ancestor: Tag is a
	// general-purpose slot a host may already be using for its own bookkeeping, and a primitive that
	// walks up into whatever tree it happens to sit in would publish whatever it found to assistive
	// technology. A silent wrong name is an accessibility bug.
	private static string GetOwningHeaderText(ResizeGripper gripper) => gripper.OwnerName;

	public ResizeGripperAutomationPeer(ResizeGripper owner) : base(owner)
	{
	}

	protected override string GetClassNameCore()
	{
		// Fixed string rather than hstring_name_of<>: the type lives in Microsoft.UI.Private.Controls,
		// and an internal namespace should not surface to assistive technology.
		return "ResizeGripper";
	}

	protected override string GetNameCore()
	{
		// An explicit name wins outright; composing it with a Tag qualifier would corrupt it.
		if (base.GetNameCore() is { Length: > 0 } explicitName)
		{
			return explicitName;
		}

		// ResourceMap::GetValue throws when the control's PRI is not merged into the host app, and
		// nothing may escape a UIA callback.
		static string localized(string resourceId, string fallback)
		{
			try
			{
				if (ResourceAccessor.GetLocalizedStringResource(resourceId) is { Length: > 0 } value)
				{
					return value;
				}
			}
			catch (Exception) { }
			return fallback;
		}

		var name = localized(ResourceAccessor.SR_ResizeGripperName, "Resize gripper");

		if (Owner is ResizeGripper gripper)
		{
			var headerName = GetOwningHeaderText(gripper);
			if (!string.IsNullOrEmpty(headerName))
			{
				var nameFormat = localized(ResourceAccessor.SR_ResizeGripperNameFormat, "%1!s!, %2!s!");

				if (StringUtil.FormatString(nameFormat, headerName, name) is { Length: > 0 } formatted)
				{
					return formatted;
				}
				// A mistranslated format string yields nothing; the bare name still identifies the control.
			}
		}

		return name;
	}

	protected override string GetAutomationIdCore()
	{
		if (base.GetAutomationIdCore() is { Length: > 0 } automationId)
		{
			return automationId;
		}

		// Empty rather than a constant: AutomationId must be unique among siblings, and a host
		// stamping one gripper per column would otherwise publish the same id on every one.
		return string.Empty;
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		// Not a Slider: the gripper reports drag distance and owns no value or range. The column
		// header is the focusable element that represents the resizable thing.
		return AutomationControlType.Thumb;
	}
}
