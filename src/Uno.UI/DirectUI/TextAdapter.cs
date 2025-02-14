// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextAdapter_Partial.cpp, tag winui3/release/1.5-stable

using Windows.UI.Xaml.Controls;

namespace DirectUI;

internal class TextAdapter
{
	public TextBlock Owner { get; }

	public TextAdapter(TextBlock owner)
	{
		Owner = owner;
	}
}
