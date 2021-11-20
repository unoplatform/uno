// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DisplayRegionHelper.h, commit d876b4e

#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

internal struct DisplayRegionHelperInfo
{
	public TwoPaneViewMode Mode { get; set; } = TwoPaneViewMode.SinglePane;

	public Rect[] Regions { get; set; } = Array.Empty<Rect>();
}
