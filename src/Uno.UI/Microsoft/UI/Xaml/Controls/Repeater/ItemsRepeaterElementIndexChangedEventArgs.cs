// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class ItemsRepeaterElementIndexChangedEventArgs
	{
		internal ItemsRepeaterElementIndexChangedEventArgs(
			UIElement element,
			int oldIndex,
			int newIndex)
		{
			Update(element, oldIndex, newIndex);
		}

		#region IElementPreparedEventArgs

		public UIElement Element { get; private set; }

		public int OldIndex { get; private set; }

		public int NewIndex { get; private set; }

		#endregion

		public void Update(UIElement element, in int oldIndex, in int newIndex)
		{
			Element = element;
			OldIndex = oldIndex;
			NewIndex = newIndex;
		}
	}
}
