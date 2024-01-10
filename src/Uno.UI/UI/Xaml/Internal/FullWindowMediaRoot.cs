// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FullWindowMediaRoot.cpp

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Core
{
	//TODO Uno: This is a very simplified version of the WinUI FullWindowMediaRoot.
	// TODO: Border is supposed to be sealed, so we shouldn't inherit from Border.
	internal partial class FullWindowMediaRoot : Border
	{
		public FullWindowMediaRoot()
		{
			VerticalAlignment = VerticalAlignment.Stretch;
			HorizontalAlignment = HorizontalAlignment.Stretch;
			Visibility = Visibility.Collapsed;
		}
	}
}
