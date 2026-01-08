// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\AutoSuggestBoxSuggestionChosenEventArgs_Partial.cpp, tag winui3/release/1.7.1

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the SuggestionChosen event.
/// </summary>
public partial class AutoSuggestBoxSuggestionChosenEventArgs
{
	/// <summary>
	/// Gets a reference to the selected item.
	/// </summary>
	public object SelectedItem { get; }

	/// <summary>
	/// Initializes a new instance of the AutoSuggestBoxSuggestionChosenEventArgs class.
	/// </summary>
	public AutoSuggestBoxSuggestionChosenEventArgs() : base()
	{
	}

	/// <summary>
	/// Initializes a new instance of the AutoSuggestBoxSuggestionChosenEventArgs class with the specified selected item.
	/// </summary>
	/// <param name="selectedItem">The selected item.</param>
	internal AutoSuggestBoxSuggestionChosenEventArgs(object selectedItem)
	{
		SelectedItem = selectedItem;
	}
}
