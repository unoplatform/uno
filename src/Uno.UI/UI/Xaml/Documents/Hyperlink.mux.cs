// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Hyperlink.h, Hyperlink.cpp

#nullable enable

using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Documents
{
	public partial class Hyperlink
	{
		internal
#if __WASM__
			new
#endif
			bool IsFocusable()
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
