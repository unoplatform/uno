// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class ItemsRepeaterElementPreparedEventArgs
	{
		internal ItemsRepeaterElementPreparedEventArgs(UIElement element, int index)
		{
			Update(element, index);
		}

		#region IElementPreparedEventArgs

		public UIElement Element { get; private set; }

		public int Index { get; private set; }
		#endregion

		public void Update(UIElement element, int index)
		{
			Element = element;
			Index = index;
		}
	}
}
