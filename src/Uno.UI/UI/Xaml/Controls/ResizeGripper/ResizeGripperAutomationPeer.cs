// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ResizeGripper\ResizeGripperAutomationPeer.idl, tag winui3/main, commit dc28206ea35

#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;

// Peer for the framework-internal ResizeGripper primitive, so it lives in the
// same private namespace as its owner and never reaches a shipped WinMD.
namespace Microsoft.UI.Private.Controls;

internal partial class ResizeGripperAutomationPeer : FrameworkElementAutomationPeer
{
}
