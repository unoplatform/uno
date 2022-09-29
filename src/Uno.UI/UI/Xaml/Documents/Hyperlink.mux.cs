// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Hyperlink.h, Hyperlink.cpp

using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Documents
{
	public partial class Hyperlink
	{
		internal bool IsFocusable()
		{
			var element = GetContainingFrameworkElement();
			return
				element != null &&
				// Concept of IsActive is currently not present in Uno
				//element.IsActive && IsActive &&
				element.IsEnabled &&
				element.Visibility == Visibility.Visible &&
				element.AreAllAncestorsVisible() &&
				IsTabStop;
		}

		internal IFocusable GetIFocusable() => _focusableHelper;
	}
}
