// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public class ItemsRepeaterElementClearingEventArgs
	{
		internal ItemsRepeaterElementClearingEventArgs(UIElement element)
		{
			Element = element;
		}

		public UIElement Element { get; }
	}
}
