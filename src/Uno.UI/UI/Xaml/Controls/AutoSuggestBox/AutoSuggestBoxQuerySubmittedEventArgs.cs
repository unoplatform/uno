// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\AutoSuggestBoxQuerySubmittedEventArgs_Partial.cpp, tag winui3/release/1.7.1

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the AutoSuggestBox.QuerySubmitted event.
/// </summary>
public partial class AutoSuggestBoxQuerySubmittedEventArgs
{
	/// <summary>
	/// Gets the suggested result that the user chose.
	/// </summary>
	public object ChosenSuggestion { get; }

	/// <summary>
	/// Gets the query text of the current search.
	/// </summary>
	public string QueryText { get; }

	/// <summary>
	/// Initializes a new instance of the AutoSuggestBoxQuerySubmittedEventArgs class.
	/// </summary>
	public AutoSuggestBoxQuerySubmittedEventArgs() : base()
	{
	}

	/// <summary>
	/// Initializes a new instance of the AutoSuggestBoxQuerySubmittedEventArgs class with the specified suggestion and query text.
	/// </summary>
	/// <param name="chosenSuggestion">The chosen suggestion.</param>
	/// <param name="queryText">The query text.</param>
	internal AutoSuggestBoxQuerySubmittedEventArgs(object chosenSuggestion, string queryText)
	{
		ChosenSuggestion = chosenSuggestion;
		QueryText = queryText;
	}
}
