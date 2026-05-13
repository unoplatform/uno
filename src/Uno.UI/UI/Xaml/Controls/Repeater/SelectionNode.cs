// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SelectionNode.h, commit 4b206bce3

namespace Microsoft.UI.Xaml.Controls;

// SelectionNode in the internal tree data structure that we keep track of for selection in
// a nested scenario. This would map to one ItemsSourceView/Collection. This node reacts
// to collection changes and keeps the selected indices up to date.
// This can either be a leaf node or a non leaf node.
internal sealed partial class SelectionNode
{
}
