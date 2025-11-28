using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class AnimatedVisualPlayerAutomationPeer : FrameworkElementAutomationPeer
{
	public AnimatedVisualPlayerAutomationPeer(AnimatedVisualPlayer owner) : base(owner)
	{
		ArgumentNullException.ThrowIfNull(owner);
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Image;

	protected override string GetClassNameCore()
		=> nameof(AnimatedVisualPlayer);
}
