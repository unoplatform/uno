using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Provides static properties that are used to configure the behavior of the <seealso cref="AutomationProperties"/>
/// </summary>
public static class AutomationConfiguration
{
	/// <summary>
	/// Indicates if Accessibility features, such as VoiceOver/TalkBack, are enabled.
	/// True by default, set this to False if you need to override certain Accessibility features for things like UI Automation
	/// </summary>
	public static bool IsAccessibilityEnabled { get; set; } = true;
}
