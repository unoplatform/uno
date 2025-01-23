using System;
using System.Collections.Generic;
using System.Linq;
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\controls\dev\AnimatedVisualPlayer\AnimatedVisualPlayerAutomationPeer.cpp, tag winui3/release/1.6.3, commit 66d24dfff

using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes AnimatedVisualPlayer types to Microsoft UI Automation.
/// </summary>
public partial class AnimatedVisualPlayerAutomationPeer : FrameworkElementAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the AnimatedVisualPlayerAutomationPeer class.
	/// </summary>
	/// <param name="owner"></param>
	public AnimatedVisualPlayerAutomationPeer(AnimatedVisualPlayer owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(AnimatedVisualPlayer);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;
}
