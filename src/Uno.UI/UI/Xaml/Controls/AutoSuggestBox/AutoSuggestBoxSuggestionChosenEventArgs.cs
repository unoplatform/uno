// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBoxSuggestionChosenEventArgs.idl, commit 5f9e85113

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides data for the SuggestionChosen event.
	/// </summary>
	public partial class AutoSuggestBoxSuggestionChosenEventArgs
	{
		/// <summary>
		/// Gets the item selected by the user.
		/// </summary>
		public object SelectedItem { get; internal set; }

		public AutoSuggestBoxSuggestionChosenEventArgs()
		{
		}

		internal AutoSuggestBoxSuggestionChosenEventArgs(object selectedItem)
		{
			SelectedItem = selectedItem;
		}
	}
}
