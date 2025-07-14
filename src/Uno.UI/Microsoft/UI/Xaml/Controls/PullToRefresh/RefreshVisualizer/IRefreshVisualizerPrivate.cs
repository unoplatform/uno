// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference RefreshVisualizerPrivate.idl, commit c6174f1

#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Private.Controls;

internal partial interface IRefreshVisualizerPrivate
{
	IRefreshInfoProvider InfoProvider { get; set; }

	void SetInternalPullDirection(RefreshPullDirection value);
}
