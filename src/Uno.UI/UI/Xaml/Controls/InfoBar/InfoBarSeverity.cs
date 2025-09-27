// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBar.idl, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that indicate the criticality of the InfoBar that is shown.
/// </summary>
public enum InfoBarSeverity
{
	/// <summary>
	/// Communicates that the InfoBar is displaying general information that
	/// requires the user's attention. For assistive technologies, they will
	/// follow the behavior set in the Processing_All constant.
	/// </summary>
	Informational = 0,

	/// <summary>
	/// Communicates that the InfoBar is displaying information regarding
	/// a long-running and/or background task that has completed successfully.
	/// For assistive technologies, they will follow the behavior
	/// set in the Processing_All constant.
	/// </summary>
	Success = 1,

	/// <summary>
	/// Communicates that the InfoBar is displaying information regarding
	/// a condition that might cause a problem in the future. For assistive
	/// technologies, they will follow the behavior
	/// set in the NotificationProcessing_ImportantAll constant.
	/// </summary>
	Warning = 2,

	/// <summary>
	/// Communicates that the InfoBar is displaying information regarding an error
	/// or problem that has occurred. For assistive technologies, they will follow
	/// the behavior set in the NotificationProcessing_ImportantAll constant.
	/// </summary>
	Error = 3,
}
