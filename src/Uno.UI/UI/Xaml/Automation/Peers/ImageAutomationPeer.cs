// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ImageAutomationPeer_Partial.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers;

public partial class ImageAutomationPeer : FrameworkElementAutomationPeer
{
	public ImageAutomationPeer(Image owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(Image);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

	protected override string GetNameCore()
	{
		var returnValue = base.GetNameCore();

		if (string.IsNullOrEmpty(returnValue))
		{
			var image = (Image)Owner;
			returnValue = image.GetTitle();
		}

		return returnValue;
	}

	protected override string GetFullDescriptionCore()
	{
		var returnValue = base.GetFullDescriptionCore();

		if (string.IsNullOrEmpty(returnValue))
		{
			var image = (Image)Owner;
			returnValue = image.GetDescription();
		}

		return returnValue;
	}
}
