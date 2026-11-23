// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressBar.idl

using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ProgressBar types to Microsoft UI Automation.
/// </summary>
public partial class ProgressBarAutomationPeer : RangeBaseAutomationPeer, IRangeValueProvider
{
}
