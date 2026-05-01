// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBoxQuerySubmittedEventArgs.idl, commit 5f9e85113

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides data for the QuerySubmitted event.
	/// </summary>
	public partial class AutoSuggestBoxQuerySubmittedEventArgs
	{
		/// <summary>
		/// Gets the suggestion item selected by the user, if the query was submitted from a suggestion in the suggestion list.
		/// </summary>
		public object ChosenSuggestion { get; internal set; }

		/// <summary>
		/// Gets the query text of the AutoSuggestBox.
		/// </summary>
		public string QueryText { get; internal set; }

		public AutoSuggestBoxQuerySubmittedEventArgs()
		{
		}

		internal AutoSuggestBoxQuerySubmittedEventArgs(object chosenSuggestion, string queryText)
		{
			ChosenSuggestion = chosenSuggestion;
			QueryText = queryText;
		}
	}
}
