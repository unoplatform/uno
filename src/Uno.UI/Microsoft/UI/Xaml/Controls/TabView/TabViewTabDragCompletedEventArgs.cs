// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabView.h, commit ed31e13

using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	/// <summary>
	/// Provides data for the TabDragCompleted event.
	/// </summary>
	public sealed partial class TabViewTabDragCompletedEventArgs
	{
		private readonly DragItemsCompletedEventArgs _args;

		internal TabViewTabDragCompletedEventArgs(DragItemsCompletedEventArgs args, object item, TabViewItem tab)
		{
			_args = args;
			Item = item;
			Tab = tab;
		}

		/// <summary>
		/// Gets a value that indicates what operation was performed on the dragged data, and whether it was successful.
		/// </summary>
		public DataPackageOperation DropResult => _args.DropResult;

		/// <summary>
		/// Gets the item that was selected for the drag action.
		/// </summary>
		public object Item { get; }

		/// <summary>
		/// Gets the TabViewItem that was selected for the drag action.
		/// </summary>
		public TabViewItem Tab { get; }
	}
}
