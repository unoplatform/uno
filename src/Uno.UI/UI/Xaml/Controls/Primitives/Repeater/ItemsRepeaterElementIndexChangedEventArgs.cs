// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsRepeaterElementIndexChangedEventArgs
	{
		internal ItemsRepeaterElementIndexChangedEventArgs(
			UIElement element,
			int oldIndex,
			int newIndex)
		{
			Element = element;
			OldIndex = oldIndex;
			NewIndex = newIndex;
		}

		#region IElementPreparedEventArgs

		public UIElement Element { get; }

		public int OldIndex { get; }

		public int NewIndex { get; }

		#endregion
	}
}
