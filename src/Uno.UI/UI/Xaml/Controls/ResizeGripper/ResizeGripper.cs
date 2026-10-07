// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ResizeGripper\ResizeGripper.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Private.Controls;

// Framework-internal: the private namespace (not the MUX_INTERNAL attribute) is what keeps this
// out of every shipped WinMD. Move to MU_XCP_NAMESPACE + MUX_PREVIEW only if it is ever exposed.

// Drag handle. Owns the gesture, its own presentation and the keyboard step; owns no value and no
// bounds - it reports how far the user has dragged, and the host clamps that and applies it.
//
// Template contract (MIDL3 does not surface [TemplatePart], so this block is it):
//   Required parts:      none. No GetTemplateChild call; a template may omit everything.
//   State targets:       "Separator" - the visual-state setters target it by name.
//   Visual state groups: CommonStates      - Normal, PointerOver, Pressed, Disabled.
//                        OrientationStates - Horizontal, Vertical; follows DragOrientation.
//   Styling keys:        ResizeGripperSeparatorBrush, ResizeGripperSeparatorThickness,
//                        ResizeGripperSeparatorThicknessVertical.
internal partial class ResizeGripper : Control
{
}
