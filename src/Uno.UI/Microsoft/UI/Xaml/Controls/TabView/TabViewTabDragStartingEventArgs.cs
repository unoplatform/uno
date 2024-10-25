// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabView.h, commit ed31e13

using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	/// <summary>
	/// Provides data for the TabDragStarting event.
	/// </summary>
	public sealed partial class TabViewTabDragStartingEventArgs
	{
		private readonly DragItemsStartingEventArgs _args;

		internal TabViewTabDragStartingEventArgs(DragItemsStartingEventArgs args, object item, TabViewItem tab)
		{
			_args = args;
			Item = item;
			Tab = tab;
		}

		/// <summary>
		/// Gets the data payload associated with a drag action.
		/// </summary>
		public DataPackage Data => _args.Data;

		/// <summary>
		/// Gets the item that was selected for the drag action.
		/// </summary>
		public object Item { get; }

		/// <summary>
		/// Gets the TabViewItem that was selected for the drag action.
		/// </summary>
		public TabViewItem Tab { get; }

		/// <summary>
		/// Gets or sets a value that indicates whether the drag action should be cancelled.
		/// </summary>
		public bool Cancel
		{
			get => _args.Cancel;
			set => _args.Cancel = value;
		}
	}
}
