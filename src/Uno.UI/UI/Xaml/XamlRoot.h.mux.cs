// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\XamlRoot_Partial.h, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

#nullable enable

using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

partial class XamlRoot
{
	internal VisualTree VisualTree => _visualTree;

	private readonly VisualTree _visualTree;
}
