// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RefreshContainerPrivate.idl, commit c6174f1

namespace Microsoft.UI.Private.Controls;

internal interface IRefreshContainerPrivate
{
	IRefreshInfoProviderAdapter RefreshInfoProviderAdapter { get; set; }
}
