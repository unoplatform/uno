// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.h

#nullable enable

using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents an icon that uses an IconSource as its content.
/// </summary>
[ContentProperty(Name = nameof(IconSource))]
public partial class IconSourceElement : IconElement
{
	/// <summary>
	/// Initializes a new instance of the IconSourceElement class.
	/// </summary>
	public IconSourceElement()
	{
	}
}
