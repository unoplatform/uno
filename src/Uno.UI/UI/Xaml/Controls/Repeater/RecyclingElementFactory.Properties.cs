// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RecyclingElementFactory.idl, commit 4b206bce3

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class RecyclingElementFactory
{
	/// <summary>
	/// Occurs when a key is requested by the factory in order to identify the
	/// template to use for a given data context.
	/// </summary>
	public event TypedEventHandler<RecyclingElementFactory, SelectTemplateEventArgs> SelectTemplateKey;
}
