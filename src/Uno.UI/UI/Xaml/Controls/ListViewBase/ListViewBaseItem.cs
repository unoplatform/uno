// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference XamlOM/Model/Microsoft.UI.Xaml.Controls.cs, tag winui3/release/2.5.1

using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

// UNO ONLY: public because subclasses are public; WinUI hides it from IDL.
// All members of this class MUST be internal (or private protected).
public abstract partial class ListViewBaseItem : SelectorItem
{
	internal ListViewBaseItem()
	{
	}
}
