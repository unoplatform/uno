// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides values that specify how the text in an AutoSuggestBox was changed.
	/// </summary>
	public enum AutoSuggestionBoxTextChangeReason
	{
		/// <summary>The user has entered text.</summary>
		UserInput,
		/// <summary>The text was changed via assignment to the Text property.</summary>
		ProgrammaticChange,
		/// <summary>The user has selected one of the items in the auto-suggestion box.</summary>
		SuggestionChosen,
	}
}
