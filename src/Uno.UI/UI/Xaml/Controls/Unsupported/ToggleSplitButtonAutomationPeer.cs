﻿using System;

namespace Microsoft.UI.Xaml.Controls;

[Obsolete(
	"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft.UI.Xaml.Controls.ToggleSplitButton instead.")]
public partial class ToggleSplitButtonAutomationPeer
{
	public ToggleSplitButtonAutomationPeer(ToggleSplitButton owner) : base(owner)
	{
		throw new NotImplementedException(
			"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft.UI.Xaml.Controls.ToggleSplitButton instead.");
	}
}
