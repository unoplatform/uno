// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ResizeGripper\ResizeGripper.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Private.Controls;

// Canceled means the gesture was torn down (pointer canceled, capture lost, unloaded) rather than
// released: the host should revert to the width it captured at DragStarted, not keep the last one.
internal sealed partial class ResizeGripperDragCompletedEventArgs
{
}
