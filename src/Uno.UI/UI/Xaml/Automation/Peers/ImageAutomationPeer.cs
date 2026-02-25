//// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ImageAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes Image types to Microsoft UI Automation.
/// </summary>
public partial class ImageAutomationPeer : FrameworkElementAutomationPeer
{
	public ImageAutomationPeer(Controls.Image owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(Controls.Image);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Image;

	protected override string GetNameCore()
	{
		var length = base.GetNameCore()?.Length ?? 0;

		if (length == 0)
		{
			var image = Owner as Controls.Image;
			return image.Name;
		}

		return null;
	}

	protected override string GetFullDescriptionCore()
	{
		var length = base.GetFullDescriptionCore()?.Length ?? 0;

		if (length == 0)
		{
			var image = Owner as Controls.Image;
			return image.Description;
		}

		return null;
	}
}
