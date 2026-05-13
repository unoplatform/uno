// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsRepeaterScrollHost.h, commit 4b206bce3

using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

// TODO: move to framework level element tracking.
/// <summary>
/// Hosts an <see cref="ItemsRepeater"/> control inside a <see cref="ScrollViewer"/>, providing
/// scroll anchoring on platforms where the <see cref="ScrollViewer"/> does not implement
/// <see cref="IScrollAnchorProvider"/>.
/// </summary>
[ContentProperty(Name = nameof(ScrollViewer))]
public partial class ItemsRepeaterScrollHost : FrameworkElement, IScrollAnchorProvider, IRepeaterScrollingSurface
{
}
