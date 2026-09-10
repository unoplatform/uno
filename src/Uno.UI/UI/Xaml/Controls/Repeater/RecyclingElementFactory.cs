// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RecyclingElementFactory.idl, commit 4b206bce3

using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents an element factory that recycles elements created from data templates.
/// </summary>
[ContentProperty(Name = nameof(Templates))]
public partial class RecyclingElementFactory : ElementFactory
{
}
