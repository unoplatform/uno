// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SortIndicator\SortIndicator.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Private.Controls;

// Framework-internal: the private namespace (not the MUX_INTERNAL attribute) is what keeps this
// out of every shipped WinMD. Move to MU_XCP_NAMESPACE + MUX_PREVIEW only if it is ever exposed.

// Policy-free sort chevron. Owns no sorting behavior: consumers position it and set Direction.
internal partial class SortIndicator : Control
{
}
