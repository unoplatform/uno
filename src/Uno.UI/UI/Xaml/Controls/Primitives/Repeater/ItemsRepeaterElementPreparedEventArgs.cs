// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public class ItemsRepeaterElementPreparedEventArgs
	{
		internal ItemsRepeaterElementPreparedEventArgs(UIElement element, int index)
		{
			Element = element;
			Index = index;
		}

		#region IElementPreparedEventArgs

		public UIElement Element { get; }

		public int Index { get; }
		#endregion
	}
}
